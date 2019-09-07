using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils.Extensions;
using GoE.Utils;
using GoE.Utils.Algorithms;
using GoE.AppConstants;

namespace GoE.Policies
{
    /// <summary>
    /// intruders start evenly spread .
    /// all intruders simultenously (or with constant delay) go towards target, 
    /// and choose the points that don't collide with pursuers( maybe improve by maximizing the minimal distance to known pursuers)
    /// </summary>
    public class IndependentIntruderPolicy: AIntrusionEvadersPolicy
    {
        private class PathWalker
        {
            public delegate List<Point> NextPointsGetter(Point startPoint);

            public PathWalker(List<Point> startPoints, NextPointsGetter AdvanceFunc)
            {
                advanceFunc = AdvanceFunc;
                ReachablePointsPerDist = new List<PointSet>();
                ReachablePointsPerDist.Add(new PointSet(startPoints));
            }
            public List<PointSet> ReachablePointsPerDist { get; protected set; }
            public NextPointsGetter advanceFunc { get; set; }


            /// <summary>
            /// generates the next layer of reachable points into ReachablePointsPerDist
            /// 
            /// if currentPursuerPoints contains points from ReachablePointsPerDist.Last(), then these points will be removed
            /// if currentPursuerPoints contains points that are the destination of ReachablePointsPerDist.Last() points, then we don't let them advance there
            /// </summary>
            /// <param name="currentPursuerPoints"></param>
            public void addNextDist(PointSet currentPursuerPoints)
            {

                // remove killed points from ReachablePointsPerDist
                foreach (var pl in currentPursuerPoints.AllPoints)
                    foreach (Point p in pl)
                        ReachablePointsPerDist.Last().Remove(p);

                // list all next reachable points
                List<Point> nextReachablePoints = new List<Point>();
                foreach (var pl in ReachablePointsPerDist.Last().AllPoints)
                    foreach (Point p in pl)
                        nextReachablePoints.AddRange(advanceFunc(p));
                PointSet nextReachablePointsSet = new PointSet(nextReachablePoints);
                nextReachablePointsSet.removeDupliacates();

                // remove reachable points that are blocked
                foreach (var pl in currentPursuerPoints.AllPoints)
                    foreach (Point p in pl)
                        nextReachablePointsSet.Remove(p);
                ReachablePointsPerDist.Add(nextReachablePointsSet);

            }
        }
        private struct ObservationSample
        {
            public bool wasCW;
            public Point lastPursuerPointToEvaderDiff;
            public int waitTime;

            public bool nothingObserved
            {
                get
                {
                    return lastPursuerPointToEvaderDiff.X == int.MaxValue;
                }
            }
            public bool isLegal
            {
                get
                {
                    return lastPursuerPointToEvaderDiff.X != 0 ||
                        lastPursuerPointToEvaderDiff.Y != 0;
                }
            }

            // constructs an illegal sample
            public ObservationSample(int WaitTime)
            {
                lastPursuerPointToEvaderDiff = new Point(0, 0);
                waitTime = WaitTime;
                wasCW = false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="evaderPoint"></param>
            /// <param name="pursuerPoints">
            /// if null, it is assumed no pursuers were observed
            /// </param>
            /// <param name="intrusionCenter"></param>
            /// <param name="WaitTime"></param>
            public ObservationSample(Point evaderPoint, List<Point> pursuerPoints, Point intrusionCenter, int WaitTime)
            {
                waitTime = WaitTime;

                if (pursuerPoints == null)
                {
                    wasCW = false;
                    lastPursuerPointToEvaderDiff = new Point(int.MaxValue, int.MaxValue);
                    return;
                }

                lastPursuerPointToEvaderDiff = pursuerPoints.Last().subtruct(evaderPoint);
                if (pursuerPoints.Count == 1)
                    wasCW = GameLogic.Utils.getAngleOfGridPoint(lastPursuerPointToEvaderDiff) > 2.5;
                else
                {
                    float ang2 = GameLogic.Utils.getAngleOfGridPoint(pursuerPoints[pursuerPoints.Count - 1].subtruct(intrusionCenter));
                    float ang1 = GameLogic.Utils.getAngleOfGridPoint(pursuerPoints[pursuerPoints.Count - 2].subtruct(intrusionCenter));

                    GameLogic.Utils.getGridPointAngleDiff(ang2, ang1, out wasCW);
                }
            }

            public bool checkMatch(Point evaderPoint, List<Point> pursuerPoints, Point intrusionCenter)
            {
                if (pursuerPoints == null)
                {
                    return lastPursuerPointToEvaderDiff.X == int.MaxValue;
                }

                Point matchLastPursuerPointToEvaderDiff = pursuerPoints.Last().subtruct(evaderPoint);
                bool matchCW = false;
                if (pursuerPoints.Count == 1)
                    matchCW = GameLogic.Utils.getAngleOfGridPoint(matchLastPursuerPointToEvaderDiff) > 2.5;
                else
                {
                    float ang2 = GameLogic.Utils.getAngleOfGridPoint(pursuerPoints[pursuerPoints.Count - 1].subtruct(intrusionCenter));
                    float ang1 = GameLogic.Utils.getAngleOfGridPoint(pursuerPoints[pursuerPoints.Count - 2].subtruct(intrusionCenter));

                    GameLogic.Utils.getGridPointAngleDiff(ang2, ang1, out matchCW);
                }

                return (matchCW == wasCW && matchLastPursuerPointToEvaderDiff == lastPursuerPointToEvaderDiff);
            }
        }
        private class SuccessRate : IComparable<SuccessRate>
        {
            public int occurences;
            public int losses;

            /// <summary>
            /// tells how good this sample is.
            /// 1st priority criteria  - x is success ratio
            /// 2nd priority criteria - y increases with the total amount of occurences 
            /// </summary>


            public SuccessRate(int Occurences = 0, int Losses = 0)
            {
                occurences = Occurences;
                losses = Losses;
            }

            public int CompareTo(SuccessRate other)
            {
                if (occurences == 0)
                {
                    if (other.occurences == 0)
                        return 0;
                    return (-1).CompareTo(1); // anything is better than undefined sample
                }
                //if (losses == 0)
                //{
                //    if (other.losses == 0)
                //        return occurences.CompareTo(other.occurences);
                //    return 1.CompareTo(-1);
                //}

                if (other.occurences == 0)
                    return (1).CompareTo(-1);

                float winRatio = 1.0f - (((float)losses) / ((float)occurences));
                float otherWinRatio = 1.0f - (((float)other.losses) / ((float)other.occurences));

                if (winRatio == otherWinRatio)
                    return occurences.CompareTo(other.occurences);
                return winRatio.CompareTo(otherWinRatio);
            }
        }

        GridGameGraph gr;
        IntrusionGameState gs;
        IntrusionGameParams prm;
        IPolicyGUIInputProvider ui;
        Point intrusionCenter;
        Random myrand;
        Dictionary<Evader, List<List<Point>>> blockedPoints;
        PointSet allBlockedPoints;
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS);
                res.Add(AppConstants.Policies.IndependentIntruderPolicy.TIME_TO_LEARN);
                return res;
            }
        }
        List<Point> learningEvaderObs;
        Evader learningEvader;
        List<GameLogic.Utils.PursuerPathObservation> Op;
        int roundsToLearn = 10000;
        List<Dictionary<ObservationSample, SuccessRate>> successPerObservation_perstartTime_perPathIdx = new List<Dictionary<ObservationSample, SuccessRate>>();
        List<PathWalker> pathFinders = new List<PathWalker>();
        
        List<List<Point>> possiblePaths = new List<List<Point>>(); // all evaders start from the same point. each of the paths is examined separately

        // latestObservations holds a list of 2 * prm.r_es + prm.t_i latest obervations. each observation tells the evader to 
        // start moving in 0 to (prm.r_es rounds-1) rounds, after it gets observed
        List<ObservationSample> latestObservations = new List<ObservationSample>(); 

        Dictionary<Evader, Point> res = new Dictionary<Evader, Point>();
        Dictionary<Evader, int> timeToEnter = new Dictionary<Evader, int>();

        int bestPathIdx;
        ObservationSample bestObservation;

        Evader activeEvader;
        Point intrudersStartPoint;

        private int maxLatestObservationCount
        {
            get
            {
                return 2 * prm.r_es + prm.t_i;
            }
        }
        //private List<string> argNames
        //{
        //    get
        //    {
        //        List<string> res = new List<string>();
        //        res.Add(AppConstants.Policies.IndependentIntruderPolicy.TIME_TO_LEARN);
        //        return res;
        //    }
        //}
 
        public override bool init(AGameGraph G, IntrusionGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui,
                                  Dictionary<string, string> policyParams = null)
        {
            //myrand = new ThreadSafeRandom().rand;
            myrand = new Random((int)DateTime.Now.Ticks);
            this.gr = (GridGameGraph)G;
            intrusionCenter = gr.getNodesByType(NodeType.Target).First();
            this.prm = prm;
            this.ui = pgui;

            //if (policyParams == null)
            //    policyParams = new Dictionary<string, string>();
            //if (pgui.hasBoardGUI())
            //    policyParams.AddRange(argNames, pgui.ShowDialog(argNames.ToArray(), "IndependentIntruderPolicy init", null));

            //roundsToLearn = int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.IndependentIntruderPolicy.TIME_TO_LEARN,
            //        AppConstants.Policies.IndependentIntruderPolicy.TIME_TO_LEARN_DEFAULT));
            roundsToLearn = int.Parse(
                AppConstants.Policies.IndependentIntruderPolicy.TIME_TO_LEARN.tryRead(policyParams));

            
            return true;
        }
        public override void setGameState(int currentRound, IEnumerable<GameLogic.Utils.CapturedObservation> O_d, List<GameLogic.Utils.PursuerPathObservation> O_p, IntrusionGameState s)
        {
            gs = s;

            blockedPoints = new Dictionary<Evader, List<List<Point>>>();

            List<Point> allBlockedPointsList = new List<Point>();
            foreach (var P in O_p)
            {
                if (!blockedPoints.ContainsKey(P.observer))
                    blockedPoints[P.observer] = new List<List<Point>>();
                blockedPoints[P.observer].Add(P.observedPursuerPath);

                foreach (var X in P.observedPursuerPath)
                    allBlockedPointsList.Add(X);
            }
            allBlockedPoints = new PointSet(allBlockedPointsList);

            Op = O_p;
            learningEvaderObs = null;
            foreach (var e in O_p)
                if (e.observer == learningEvader)
                    learningEvaderObs = e.observedPursuerPath;

            addGUIMarks(O_p, ui, s, gr, prm, res);
        }
        public override List<Evader> communicate()
        {
            return new List<Evader>();
        }
        /// <summary>
        /// used for PathFinder
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        private List<Point> pathwalkerAdvanceFunc(Point start)
        {
            List<Point> res = new List<Point>();
            int startManDist = start.manDist(intrusionCenter);
            if (startManDist == prm.r_e)
            {
                // if the start point is an intrusion point, there is no more where to advance to
                // (and the path should "continue" by staying in place a few more rounds)
                res.Add(start);
                return res;
            }

            var options = gr.getAdjacentIDs(start);
            
            foreach (Point op in options)
                if (op.manDist(intrusionCenter) < startManDist &&
                   op.manDist(intrudersStartPoint) <= prm.r_es)
                    res.Add(op);
            return res;
        }
        
        /// <summary>
        /// populates pathFinders before learn() can be used
        /// </summary>
        void choosePossiblePaths(PathWalker fullpath)
        {
            possiblePaths.Add(new List<Point>());
            
            Point destPoint = new Point();
            foreach(var pl in fullpath.ReachablePointsPerDist.Last().AllPoints)
                if(pl.Count > 0)
                    destPoint = pl.First();

            possiblePaths.Last().Add(destPoint);
            // generate a path that necessarily ends with survival
            for(int i = fullpath.ReachablePointsPerDist.Count - 2;
                i >= 0; --i)
            {
                for (int pl = 0; pl < fullpath.ReachablePointsPerDist[i].AllPoints.Count; ++pl )
                    foreach (Point p in fullpath.ReachablePointsPerDist[i].AllPoints[pl])
                    {
                        if (pathwalkerAdvanceFunc(p).Contains(destPoint))
                        {
                            possiblePaths.Last().Add(destPoint);
                            destPoint = p; // next time we'll search the point that lead to this one
                            pl = fullpath.ReachablePointsPerDist[i].AllPoints.Count; // ends outer loop
                            break;
                        }
                    }
            }

            possiblePaths.Last().Reverse();
            while (possiblePaths.Last()[possiblePaths.Last().Count - 2].manDist(intrusionCenter) == prm.r_e) 
                possiblePaths.Last().RemoveAt(possiblePaths.Last().Count - 1); 
            // the path is generated in a way that the last point (intrusion point) repeats itself several times. in the learning process, it is assumed that this point
            // appears only once
            

            //for(int i = 0; i < fullpath.ReachablePointsPerDist.Count; ++i)
            {
                //Console.WriteLine("fullpath dist:" + i.ToString());
                //foreach(var pl in fullpath.ReachablePointsPerDist[i].AllPoints)
                    //foreach(var p in pl)
                        //Console.Write("(" + p.X.ToString() +"," + p.Y.ToString() +")");
                //Console.WriteLine("");
            }
            //Console.WriteLine("chosen path:");
            //for(int i = 0; i < possiblePaths.Last().Count();++i)
                //Console.Write("(" + possiblePaths.Last()[i].X.ToString() + "," + possiblePaths.Last()[i].Y.ToString() +")");

            //Console.WriteLine("");
        }
        /// <summary>
        /// populates pathFinders before choosePossiblePaths () can be used
        /// </summary>
        void updatePathFinders()
        {
            int pathLength = prm.r_es + prm.t_i+1;
            HashSet<Point> noBlockPoints = new HashSet<Point>();
            List<Point> startPoints = new List<Point>();
            startPoints.Add(intrudersStartPoint);
            
            if (pathFinders.Count < pathLength)
                pathFinders.Add(new PathWalker(startPoints, new PathWalker.NextPointsGetter(pathwalkerAdvanceFunc)));

            for (int i = 0; i < pathFinders.Count; ++i )
            {
                pathFinders[i].addNextDist(allBlockedPoints);
                if (pathFinders[i].ReachablePointsPerDist.Last().Count == 0)
                {
                    // the entire path is doomed - remove it, and hope we'll find a path later
                    pathFinders[i] = pathFinders.Last();
                    pathFinders.RemoveAt(pathFinders.Count - 1);
                    --i;
                    continue;
                }
                if (pathFinders[i].ReachablePointsPerDist.Count == pathLength)
                {
                    choosePossiblePaths(pathFinders[i]);
                    for (int pathIdx = 0; pathIdx < possiblePaths.Count; ++pathIdx)
                        successPerObservation_perstartTime_perPathIdx.Add(new Dictionary<ObservationSample, SuccessRate>());
                    return;
                }
            }

            //possiblePaths = new List<List<Point>>();
            //bool isBlocked;
            //bool returnfirst = false;

            //for (int turnsCount = 0; turnsCount <= prm.r_es; ++turnsCount)
            //{
            //    Point currentPoint = nextRes[prm.A_E.First()];
            //    int remainingTurns = turnsCount;
            //    possiblePaths.Add(new List<Point>());

            //    do
            //    {

            //        if (remainingTurns >= (prm.r_es - possiblePaths.Last().Count))
            //        {
            //            returnfirst = false;
            //            --remainingTurns;
            //        }
            //        else
            //        {
            //            if (remainingTurns > 0 && myrand.Next(2) == 1)
            //            {
            //                returnfirst = false;
            //                --remainingTurns;
            //            }
            //            else
            //                returnfirst = true;
            //        }

            //        possiblePaths.Last().Add(
            //            GameLogic.Utils.advanceOnPath(gr, currentPoint, intrusionCenter, returnfirst, noBlockPoints, out isBlocked));
            //        currentPoint = possiblePaths.Last().Last();
            //    }
            //    while (possiblePaths.Last().Last().manDist(intrusionCenter) > prm.r_e);


            //}
        }
        /// <summary>
        /// called after learn()ing is done
        /// </summary>
        private void findBestObservation()
        {
            // find the rule that maximizes chance to win
            SuccessRate bestObservationRate = new SuccessRate();
            for (int pathIdx = 0; pathIdx < possiblePaths.Count; ++pathIdx)
            {
                foreach (var o in successPerObservation_perstartTime_perPathIdx[pathIdx])
                {

                    //Console.WriteLine("PathIdx=" + pathIdx.ToString());
                    //Console.WriteLine("o.key=" + o.Key.lastPursuerPointToEvaderDiff.X.ToString() + "," + o.Key.lastPursuerPointToEvaderDiff.Y.ToString());
                    //Console.WriteLine("o.occurences=" + o.Value.occurences.ToString());
                    //Console.WriteLine("o.losses=" + o.Value.losses.ToString());

                    if (bestObservationRate.CompareTo(o.Value) < 0)
                    {
                        bestObservationRate = o.Value;
                        bestObservation = o.Key;
                        bestPathIdx = pathIdx;
                    }
                    else if (bestObservationRate.CompareTo(o.Value) == 0)
                    {
                        if (bestObservation.nothingObserved) // we don't like the nothing observed observation, since it's too common and may be misleading
                        {
                            bestObservationRate = o.Value;
                            bestObservation = o.Key;
                            bestPathIdx = pathIdx;
                        }
                    }
                }
            }

            //Console.WriteLine("bestPathIdx=" + bestPathIdx.ToString());
            //Console.WriteLine("bestObservationRate.occurences=" + bestObservationRate.occurences.ToString());
            //Console.WriteLine("bestObservationRate.losses=" + bestObservationRate.losses.ToString());
            //Console.WriteLine("bestObservation=" + bestObservation.lastPursuerPointToEvaderDiff.X.ToString() + "," + bestObservation.lastPursuerPointToEvaderDiff.Y.ToString());
            //Console.WriteLine("bestObservation.WaitTime =" + bestObservation.waitTime.ToString());
        }
        /// <summary>
        /// populates successPerObservation_perstartTime_perPathIdx
        /// </summary>
        private void learn()
        {
            if (latestObservations.Count >= maxLatestObservationCount)
                for (int pathIdx = 0; pathIdx < possiblePaths.Count; ++pathIdx)
                {
                    var path = possiblePaths[pathIdx];
                    for (int pathPoint = 0; pathPoint < path.Count; ++pathPoint)
                    {
                        if (blockedPoints.ContainsKey(learningEvader))
                        {
                            foreach (var pl in blockedPoints[learningEvader])
                            {
                                if (pl.Contains(path[pathPoint]))
                                {
                                    for (int i = 0; i <= prm.r_es; ++i)
                                    {
                                        for (int pPoint = pathPoint; pPoint >= pathPoint - 1 && pPoint >= 0; --pPoint) // evader in path point is killed, and in pathPoint-1 gets stuck (practically it's just as bad, since in some cases you'll necessarily die afterwards but this ruins our calculation)
                                        {
                                            int badObsIdx = (latestObservations.Count - 1) - pPoint - i;

                                            if (pathPoint == path.Count - 1)
                                            {
                                                for (int t = 0; t < prm.t_i; ++t)
                                                {

                                                    if (badObsIdx - t < 0)
                                                        continue;
                                                    ObservationSample badObs = latestObservations[badObsIdx - t];
                                                    badObs.waitTime = i;
                                                    ++successPerObservation_perstartTime_perPathIdx[pathIdx][badObs].losses; // technichally we can lose "multiple times" on the same path
                                                }
                                            }
                                            else
                                            {
                                                if (badObsIdx < 0)
                                                    continue;
                                                ObservationSample badObs = latestObservations[badObsIdx];
                                                badObs.waitTime = i;
                                                ++successPerObservation_perstartTime_perPathIdx[pathIdx][badObs].losses;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            latestObservations.Add(new ObservationSample(res[learningEvader], learningEvaderObs, intrusionCenter, 0));
            if (latestObservations.Count > maxLatestObservationCount)
                latestObservations.RemoveAt(0);

            // add observation to "possible observations DB"
            ObservationSample tmpSample = latestObservations.Last();
            tmpSample.waitTime = 0;

            for (int pathIdx = 0; pathIdx < possiblePaths.Count; ++pathIdx)
            {
                for (int i = 0; i <= prm.r_es; ++i)
                {
                    tmpSample.waitTime = i;
                    if (successPerObservation_perstartTime_perPathIdx[pathIdx].ContainsKey(tmpSample))
                        ++successPerObservation_perstartTime_perPathIdx[pathIdx][tmpSample].occurences;
                    else
                        successPerObservation_perstartTime_perPathIdx[pathIdx].Add(tmpSample, new SuccessRate(1, 0));
                }
            }
        }
        public override Dictionary<Evader, Point> getNextStep()
        {
            Dictionary<Evader, Point> nextRes = new Dictionary<Evader, Point>();
            if (gs.ActiveEvaders.Count == 0)
            {
                float ang = 0.2f;

                intrudersStartPoint = intrusionCenter.add(GameLogic.Utils.getGridPointByAngle(prm.r_e + prm.r_es, ang));
                foreach (Evader e in prm.A_E)
                    nextRes[e] = intrudersStartPoint;

                learningEvader = prm.A_E.First();
            }
            else
            {
                if (possiblePaths.Count == 0)
                {
                    --roundsToLearn;
                    
                    if(roundsToLearn <= 0)
                        GiveUp(); // we can't find any safe route, so it's probably a lost cause
                    
                    updatePathFinders();

                    foreach (Evader e in gs.ActiveEvaders)
                        nextRes[e] = res[e]; // evaders don't move while learning
                }
                else if (roundsToLearn > 0)
                {
                    --roundsToLearn;
                    learn();

                    foreach (Evader e in gs.ActiveEvaders)
                        nextRes[e] = res[e]; // evaders don't move while learning
                }
                else // if  roundsToLearn is 0:
                {
                    // we now wait until we get the best possible observation, then enter
                    if (!bestObservation.isLegal)
                        findBestObservation();

                    foreach (Evader e in gs.ActiveEvaders)
                    {
                        nextRes[e] = res[e]; // default choice

                        if (e != gs.ActiveEvaders.First())
                            continue;

                        if (timeToEnter.ContainsKey(e))
                        {
                            --timeToEnter[e];

                            if (timeToEnter[e] > 0 || /*need to wait a few more rounds*/
                                res[e].manDist(intrusionCenter) == prm.r_e /*already intrudin*/)
                            {
                                continue;     // intruder should stay in place
                            }
                        }
                        else
                        {
                            List<Point> pursuersPaths = null;

                            if (blockedPoints.ContainsKey(e))
                            {
                                ObservationSample currentState = new ObservationSample(res[e], blockedPoints[e].First(), intrusionCenter, 0);
                                //Console.WriteLine("currentState.key=" + currentState.lastPursuerPointToEvaderDiff.X.ToString() + "," + currentState.lastPursuerPointToEvaderDiff.Y.ToString());

                                // if 1 pursuer was observed, check if it matches best observation
                                if (blockedPoints[e].Count == 1 && bestObservation.checkMatch(res[e], blockedPoints[e].First(), intrusionCenter))
                                {
                                    timeToEnter[e] = bestObservation.waitTime;

                                    //Console.WriteLine("observations match! starting path:");
                                    //for (int pi = 0; pi < possiblePaths[bestPathIdx].Count; ++pi)
                                        //Console.Write("(" + possiblePaths[bestPathIdx][pi].X.ToString() + "," + possiblePaths[bestPathIdx][pi].Y.ToString() + ")");
                                }
                            }
                            else
                            {
                                ObservationSample currentState = new ObservationSample(res[e], pursuersPaths, intrusionCenter, 0);
                                //Console.WriteLine("currentState.key=" + currentState.lastPursuerPointToEvaderDiff.X.ToString() + "," + currentState.lastPursuerPointToEvaderDiff.Y.ToString());

                                // if no pursuers were observed, check if this matches the best observation
                                if (bestObservation.checkMatch(res[e], pursuersPaths, intrusionCenter))
                                {
                                    timeToEnter[e] = bestObservation.waitTime;
                                    //Console.WriteLine("observations match! starting path:");
                                    //for (int pi = 0; pi < possiblePaths[bestPathIdx].Count; ++pi)
                                        //Console.Write("(" + possiblePaths[bestPathIdx][pi].X.ToString() + "," + possiblePaths[bestPathIdx][pi].Y.ToString() + ")");
                                }
                            }
                        }

                        if (timeToEnter.ContainsKey(e) && timeToEnter[e] <= 0)
                        {
                            // advance, if the path isn't blocked
                            Point nextP = possiblePaths[bestPathIdx][-timeToEnter[e]];
                            if (!allBlockedPoints.Contains(nextP))
                            {
                                nextRes[e] = nextP;

                            }
                            else
                                ++timeToEnter[e]; // if path was blocked, make sure we don't skip a point
                        }
                    }
                }
            } // every round except for first one
            res = nextRes;
            return nextRes;
        }
    }
}