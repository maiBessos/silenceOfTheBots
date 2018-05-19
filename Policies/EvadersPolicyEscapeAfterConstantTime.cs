using GoE.AppConstants;
using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.Policies
{
    /// <summary>
    /// evaders come out, one by one, from a sink to sensitive area (1 target is chosen at random when policy starts).
    /// They accumulate data, then escape back to the sink (and transmit from a sink point).
    /// NOTE: it is assumed all point with large enough distance from the target are sinks
    /// 
    /// Mode 1: 
    /// if p_a and p_c are provided upon init(), the evader assumes Solution 2 is used, and therefore stays in the circumference 
    /// (or 1 ring deeper)
    /// 
    /// Mode 2:
    /// otherwise, the policy asks for how many units to accumulate - X, then:
    /// the evader walks randomly within the sensitive area, calculates the probability of being blocked each round (within the sensitive area), then attempts to 
    /// escape when it expects to get outside exactly after accumulating X units, then runs away to the nearest sink
    /// 
    /// FIXME: separate the two modes into separate policy classes
    /// </summary>
    class EvadersPolicyEscapeAfterConstantTime : AGoEEvadersPolicy
    {
        private GridGameGraph g;
        private GoEGameParams gm;
        private IPolicyGUIInputProvider input;
        private Dictionary<string, string> policyParams;

        private int prevCurrentRound = 0;
        private GameState prevS;
        private IEnumerable<Point> prevO_d;
        private HashSet<Point> prevO_p;
        //private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();

        //bool movingIntoSensitiveArea; // tells whether the evader goes in or out
        private Point currentActiveEvaderLocation;
        private Evader activeEvader = null;
        private List<Evader> nextEvaders = new List<Evader>();

        private int sinkToTargetDist;
        //private Point randomStartSink;
        private Point destTarget; // point of a target
        private Point temporaryDestination; // used in mode 2 only

        private int blocksInSensitiveArea = 0;
        private int accumulatedData = 0;
        private bool isSolution2Mode;

        public EvadersPolicyEscapeAfterConstantTime()
        {

        }
        public override void setGameState(int currentRound, IEnumerable<Point> O_d, HashSet<Point> O_p, GameState s)
        {
            prevCurrentRound = currentRound;
            prevS = s;
            prevO_d = O_d;
            prevO_p = O_p;
        }
        public int DataToAccumulateBeforeEscaping { get; private set; }

        protected override List<ArgEntry> PolicyParamsInput
        {
            get
            {
                return new List<ArgEntry>(new ArgEntry[] { AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE});
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="takeFirst">
        /// if true, the first found point (out of the two-at-most) will be returned.
        /// Otherwise, the last point is returned
        /// </param>
        /// <param name="isBlocked">
        /// tells whether the agent couldn't advance since it was blocked by pursuers occupying all
        /// relevant locations
        /// </param>
        /// <returns>
        ///  returns one of the two (at most) points in distance 1 from "from", that is closest to "to"
        /// (on failure, returns "from")
        /// </returns>
        private Point advanceOnPath(Point from, Point to, bool returnFirst, out bool isBlocked)
        {
            var options = g.getAdjacentIDs(from);
            Point res = from;
            int dist = (int)g.getMinDistance(from, to) - 1;
            isBlocked = true;
            foreach (Point p in options)
                if (g.getMinDistance(p, to) == dist && !prevO_p.Contains(p))
                {
                    isBlocked = false;
                    if (returnFirst)
                        return p;
                    returnFirst = true;
                    res = p;
                }

            return res;
        }
        /// <summary>
        /// opposite of advanceOnPath i.e. increases the distance between currentLocation
        /// and runningAwayFrom
        /// </summary>
        /// <param name="currentLocation"></param>
        /// <param name="runningAwayFrom"></param>
        /// <param name="isBlocked"></param>
        /// <returns></returns>
        private Point runAwayFrom(Point currentLocation, Point runningAwayFrom, bool returnFirst, out bool isBlocked)
        {
            var options = g.getAdjacentIDs(currentLocation);
            Point res = currentLocation;
            int dist = (int)g.getMinDistance(currentLocation, runningAwayFrom) + 1;
            isBlocked = true;
            foreach (Point p in options)
                if (g.getMinDistance(p, runningAwayFrom) == dist && !prevO_p.Contains(p))
                {
                    isBlocked = false;

                    if (returnFirst)
                        return p;
                    returnFirst = true;
                    res = p;
                }

            return res;
        }

        /// <summary>
        /// returns a random point that is withing the sensitive area around destTarget
        /// and within distance 1 from current location of the evader
        /// </summary>
        /// <returns></returns>
        private Point getRandomMovementInSensitiveArea(out bool isBlocked)
        {
            Random pointSelector = new ThreadSafeRandom().rand;

            if (g.getMinDistance(currentActiveEvaderLocation, destTarget) == gm.r_e)
            {
                // point is on the edge of the sensitive area. either stay in place, or walk towards 
                // the target

                int res = pointSelector.Next(3);


                if (res == 0) // we stay in place
                {
                    isBlocked = false;
                    return currentActiveEvaderLocation;
                }
                return advanceOnPath(currentActiveEvaderLocation, destTarget, (res == 1), out isBlocked);
            }

            var options = g.getAdjacentIDs(currentActiveEvaderLocation);
            int startIdx = pointSelector.Next(0, options.Count);
            for (int i = 0; i < options.Count; ++i)
            {
                // go through all optional points, starting from "startIdx", until you find an
                // unoccupied point
                Point pointToTest = options[(startIdx + i) % options.Count];
                if (!prevO_p.Contains(pointToTest))
                {
                    isBlocked = false;
                    return pointToTest;
                }
            }

            isBlocked = true;
            return currentActiveEvaderLocation;
        }


        /// <summary>
        /// generates a random point in distance "distance" from point "from"
        /// </summary>
        /// <param name="from"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private Point getRandomPointInDistance(Point from, int distance)
        {
            Random r = new ThreadSafeRandom().rand;
            int xDiff = r.Next(-distance, distance + 1);
            int yDiff = (-1 + 2 * r.Next(0, 2)) * (distance - Math.Abs(xDiff)); // either -1 or 1, multiplied by remaining distance to cover

            int resX = from.X + xDiff;
            int resY = from.Y + yDiff;

            // if coord's is outside legal graph points, we choose another point "cyclicly"
            if (resX < 0)
                resX += 2 * distance;
            if (resX >= g.WidthCellCount)
                resX -= 2 * distance;
            if (resY < 0)
                resY += 2 * distance;
            if (resY >= g.HeightCellCount)
                resY -= 2 * distance;

            return new Point(resX, resY);
        }

        private int enteredArea = 0;
        private int survivedArea = 0;
        private int enteredCircumference = 0;
        private int survivedCircumference = 0;
        
        //int totalAgentData = 0; // fixme remove

        private bool isNearStichingPoint()
        {
            // TODO: this method was useful for when circumference patrol was in groups. density ofpursuers is still not entirely uniform and
            // evaders can wait for the best time to enter, but this may take many rounds + slightly harder to calculate
            return false;


            if (maxPursuersDiff != -1)
            {
                float minDiffCW = 5, minDiffCWAngle = 0;
                float minDiffCCW = 5, minDiffCCWAngle = 0;
                float evaderAngle = GoE.GameLogic.Utils.getAngleOfGridPoint(currentActiveEvaderLocation.subtruct(destTarget));
                bool iscw;

                // search the nearest two pursuers to the evader - from CW and CCW directions:
                foreach (Point p in prevO_p)
                {
                    if (p.manDist(destTarget) < gm.r_e)
                        continue; // we only care for circumference pursuers

                    float pursuerAngle =
                        GoE.GameLogic.Utils.getAngleOfGridPoint(p.subtruct(destTarget));

                    float diff = GoE.GameLogic.Utils.getGridPointAngleDiff(pursuerAngle, evaderAngle, out iscw);
                    if (iscw)
                    {
                        if (minDiffCW > diff)
                        {
                            minDiffCW = diff;
                            minDiffCWAngle = pursuerAngle;
                        }
                    }
                    else
                    {
                        if (minDiffCCW > diff)
                        {
                            minDiffCCW = diff;
                            minDiffCCWAngle = pursuerAngle;
                        }

                    }
                }
                // if pursuers are two close, we are in fact near the "stiching group", which lowers the probability for entering the sensitive area
                // we add 1 to maxPursuersDiff, since angle diff considers the "middle of the pursuers", and its as if it adds two halfs pursuers from each direction
                if (GoE.GameLogic.Utils.getGridPointAngleDiff(minDiffCWAngle, minDiffCCWAngle, out iscw) * gm.r_e < maxPursuersDiff + 1 - 0.0001)
                    return true;
            }

            return false;
        }
        
        public override void gameFinished() 
        {
            input.addLogValue(AppConstants.Policies.PatrolAndPursuit.RESULTED_P_A.key,
                             (1 - ((float)survivedArea / enteredArea)).ToString());
            input.addLogValue(AppConstants.Policies.PatrolAndPursuit.RESULTED_P_C.key,
                             (1 - ((float)survivedCircumference / enteredCircumference)).ToString());
        }

        List<Evader> deadEvaders = new List<Evader>();
        public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep()
        {
            Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
            Random tmpRand = new ThreadSafeRandom().rand;
            bool isBlocked;
            
            if (prevO_d.Count() > 0 || activeEvader == null) // evader got captured/policy just initialized - we send another ones...
            {
                if (activeEvader != null)
                    deadEvaders.Add(activeEvader);

                activeEvader = nextEvaders.Last();
                nextEvaders.RemoveAt(nextEvaders.Count - 1);
                //movingIntoSensitiveArea = true;
                accumulatedData = 0;
                blocksInSensitiveArea = 0;

                currentActiveEvaderLocation = sinks[tmpRand.Next() % sinks.Count()];
                sinkToTargetDist = (int)g.getMinDistance(currentActiveEvaderLocation, destTarget);
                res[activeEvader] = Tuple.Create(DataUnit.NIL, new Location(currentActiveEvaderLocation), new Location(destTarget));

                foreach (Evader e in nextEvaders)
                    res[e] = Tuple.Create(DataUnit.NIL, new Location(Location.Type.Unset), new Location(destTarget));

                foreach (Evader e in deadEvaders)
                    res[e] = Tuple.Create(DataUnit.NIL, new Location(Location.Type.Captured), new Location(Location.Type.Undefined));

                return res;
            }

            //if (accumulatedData == DataToAccumulateBeforeEscaping - 1) // FIXME remove
            //{
            //    pgui.debugStopSkippingRound();
            //}

            //if ((int)g.getMinDistance(currentActiveEvaderLocation, destTarget) == sinkToTargetDist &&
            //   accumulatedData > 0)
            //{

            //    // evader is on a sink - transmit data until nothing new remains
            //    //var evaderMem = prevS.M[prevS.MostUpdatedEvadersMemoryRound][activeEvader];
            //    //res[activeEvader] = Tuple.Create(
            //    //    evaderMem[evaderMem.Count - accumulatedData],
            //    //    new Location(currentActiveEvaderLocation),
            //    //    new Location(Location.Type.Undefined));
            //    //--accumulatedData;

            //    //movingIntoSensitiveArea = true; 
            //    // just insuring that when we start walking back towards the target, 
            //    // this bool is true
            //}

            int distFromTarget = (int)g.getMinDistance(currentActiveEvaderLocation, destTarget);

            int isAccumulatingDataNow = (distFromTarget <= gm.r_e) ? (1) : (0);
            
            
            if (distFromTarget == (sinkToTargetDist - 1) && 
                (accumulatedData + isAccumulatingDataNow) >= DataToAccumulateBeforeEscaping)
            {
                // we can now flush all data immediately, as we enter the sink
                Point nextP = runAwayFrom(currentActiveEvaderLocation, destTarget, tmpRand.Next(0, 2) == 0, out isBlocked);
                res[activeEvader] = Tuple.Create(
                    DataUnit.Flush,
                    new Location(nextP),
                    new Location(destTarget));
                accumulatedData = 0;
            }
            else if (accumulatedData >= DataToAccumulateBeforeEscaping)
            {

                Point nextP = runAwayFrom(currentActiveEvaderLocation, destTarget, tmpRand.Next(0, 2) == 0, out isBlocked);

                //// if we are just exiting, make sure that when comming out to th circumference, we don't go into stiching point
                //if (!isBlocked && (int)g.getMinDistance(currentActiveEvaderLocation, destTarget) == ((int)gm.r_e - 1) && isNearStichingPoint())
                //    nextP = currentActiveEvaderLocation;

                // walk towards sink
                res[activeEvader] =
                    Tuple.Create(DataUnit.NIL,
                    new Location(nextP),
                    new Location(destTarget));
            }
            else
            {
                if (distFromTarget > ((int)gm.r_e))
                {
                    Point nextP = advanceOnPath(currentActiveEvaderLocation, destTarget, tmpRand.Next(0, 2) == 0, out isBlocked);


                    // if the opponent uses pursuers for pursuit , they may just stand in place on the circumference and
                    // permanently block the way. Therefore, if way is entirely blocked, the evader should move away
                    if (isBlocked)
                        nextP = runAwayFrom(currentActiveEvaderLocation, destTarget, false, out isBlocked);


                    //// if we are going to enter circumference of sensitive area, make sure we enter where pursuers are slightly less dense
                    //if (!isBlocked && (int)g.getMinDistance(currentActiveEvaderLocation, destTarget) == ((int)gm.r_e + 1) && isNearStichingPoint())
                    //    nextP = currentActiveEvaderLocation;
                    
                    // go towards target:
                    res[activeEvader] =
                        Tuple.Create(DataUnit.NIL,
                        new Location(nextP),
                        new Location(destTarget));
                }
                
                if ((int)g.getMinDistance(currentActiveEvaderLocation, destTarget) <= (int)gm.r_e)
                {
                    // we were in the sensitive area - data was accumulated
                    ++accumulatedData;

                    if (isSolution2Mode)
                    {
                        if (accumulatedData >= DataToAccumulateBeforeEscaping)
                        {

                            // we just took enough data units - now walk back towards sink
                            res[activeEvader] =
                                Tuple.Create(DataUnit.NIL,
                                new Location(runAwayFrom(currentActiveEvaderLocation, destTarget, tmpRand.Next(0, 2) == 0, out isBlocked)),
                                new Location(destTarget));
                        }
                        else if (DataToAccumulateBeforeEscaping > 2 &&
                                 (int)g.getMinDistance(currentActiveEvaderLocation, destTarget) == (int)gm.r_e)
                        {
                            // we are on the circumference, but prefer to continue accumulating data from 
                            // more inner ring, so go towards target:
                            res[activeEvader] =
                                Tuple.Create(DataUnit.NIL,
                                new Location(advanceOnPath(currentActiveEvaderLocation, destTarget, tmpRand.Next(0, 2) == 0, out isBlocked)),
                                new Location(destTarget));
                        }
                        else
                        {
                            // we are accumulating data from within the sensitive area, so just stay in place
                            res[activeEvader] =
                                Tuple.Create(DataUnit.NIL,
                                new Location(currentActiveEvaderLocation),
                                new Location(destTarget));
                        }
                    }
                    else // if not solution 2:
                    {
                        // in this mode, the evader repeatedly chooses a random point in the sensitive area,
                        // until its time to escape

                        int neededRoundsToEscape =
                            (int)(gm.r_e - g.getMinDistance(currentActiveEvaderLocation, destTarget));// +blocksInSensitiveArea;

                        if (accumulatedData + neededRoundsToEscape >= DataToAccumulateBeforeEscaping)
                        {
                            // walk towards sink
                            res[activeEvader] =
                                Tuple.Create(DataUnit.NIL,
                                new Location(runAwayFrom(currentActiveEvaderLocation, destTarget, tmpRand.Next(0, 2) == 0, out isBlocked)),
                                new Location(destTarget));
                        }
                        else
                        {

                            // in order to avoid situations where we stay a long time in the circumference, 
                            // we choose a random destination and walk there

                            if ((int)g.getMinDistance(currentActiveEvaderLocation, destTarget) == (int)gm.r_e ||
                                res[activeEvader].Item2.nodeLocation == temporaryDestination)
                            {
                                // we either just entered the sensitive area, or reached the previously chosen 
                                // temporaryDestination
                                temporaryDestination =
                                    getRandomPointInDistance(destTarget, tmpRand.Next(0, gm.r_e - 1));
                            }

                            // walk towards temporaryDestination
                            Point nextLocation =
                                advanceOnPath(currentActiveEvaderLocation,
                                              temporaryDestination,
                                              tmpRand.Next(0, 2) == 0, out isBlocked);
                            res[activeEvader] =
                                Tuple.Create(DataUnit.NIL,
                                new Location(nextLocation),
                                new Location(destTarget));
                        }

                        //else
                        //{
                        //    // walk randomly
                        //    res[activeEvader] =
                        //        Tuple.Create(DataUnit.NIL,
                        //        new Location(runAwayFrom(currentActiveEvaderLocation, destTarget, out isBlocked)),
                        //        new Location(destTarget));

                        //}

                        //if (accumulatedData + neededRoundsToEscape >= DataToAccumulateBeforeEscaping)
                        //{
                        //     res[activeEvader] =
                        //        Tuple.Create(DataUnit.NIL,
                        //        new Location(runAwayFrom(currentActiveEvaderLocation, destTarget)),
                        //        new Location(destTarget));
                        //}


                        //if(movingIntoSensitiveArea && isBlocked)
                        //    blocksInSensitiveArea++; // serves as a rough estimation for 
                    }
                }

            }

            
            
            if(currentActiveEvaderLocation.manDist(destTarget) == gm.r_e &&
                (res[activeEvader].Item2.nodeLocation.manDist(destTarget) == (gm.r_e-1) || 
                res[activeEvader].Item2.nodeLocation.manDist(destTarget) == gm.r_e+1))
            {
                ++survivedCircumference;
            }

            if(res[activeEvader].Item2.nodeLocation.manDist(destTarget) == gm.r_e)
            {
                ++enteredCircumference ;
            }

            if (res[activeEvader].Item2.nodeLocation.manDist(destTarget) == gm.r_e - 1)
            {
                ++enteredArea;
            }

            if (currentActiveEvaderLocation.manDist(destTarget) == gm.r_e-1 && 
                ( res[activeEvader].Item2.nodeLocation.manDist(destTarget) == gm.r_e-1 ||
                    res[activeEvader].Item2.nodeLocation.manDist(destTarget) == gm.r_e))
            {
                ++survivedArea;
            }
            
            currentActiveEvaderLocation = res[activeEvader].Item2.nodeLocation;
            

            foreach (Evader e in nextEvaders)
                res[e] = Tuple.Create(DataUnit.NIL, new Location(Location.Type.Unset), new Location(destTarget));

            return res;
        }

        double p_a = 0.1, 
            p_c = 0; // FIXME make temporary in Init() method. p_c includes gameParams.EvaderCircumferenceEntranceKillProb 


        public override bool init(AGameGraph G, 
            GoEGameParams prm, 
            AGoEPursuersPolicy pursuers, 
            IPolicyGUIInputProvider input,
            Dictionary<string,string> PolicyParams)
        {
            

            this.g = (GridGameGraph)G;
            this.gm = prm;
            this.input = input;
            this.policyParams = PolicyParams;
            nextEvaders.AddRange(prm.A_E);

            List<string> typeRes;
            
            //typeRes = input.ShowDialog(new string[] { AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE_MULTIPLIER }, "crawl only policy input", new string[] { "1.0" });
            
            float l_escapeMultiplier = 1;
            //float.TryParse(typeRes[0], out l_escapeMultiplier);

            PatrolAndPursuitLearner l = new PatrolAndPursuitLearner();
            l.init(new PursuersLearnerInitData(){g=(GridGameGraph)G,s=prm,p=pursuers});
            p_a = l.AreaPatrolCaptureProbability;
            p_c = //l.CircumferencePatrolCaptureProbability + prm.EvaderCircumferenceEntranceKillProb ;
                prm.EvaderCircumferenceEntranceKillProbWithPC(l.CircumferencePatrolCaptureProbability);

            double discountFactor = 0.999999999;
            ConstantExponentialDecay discR = prm.R as ConstantExponentialDecay;
            if (discR != null)
                discountFactor = discR.oneRoundDiscountFactor;


            destTarget = g.getNodesByType(NodeType.Target).First();
            sinks = g.getNodesByType(NodeType.Sink);

            int l_escape = 1;
            if (p_c > 0)
            {
                //l_escape = PatrolAndPursuit.calculateIntegerL_Escape(p_a, p_c);
                int timeBeforeCrawl = (int)(sinks.First().manDist(destTarget) - 1);
                l_escape = (int)PatrolAndPursuit.calculatel_escape(p_a, p_c, discountFactor, timeBeforeCrawl);
                double e_escape;
                e_escape = PatrolAndPursuit.calculateE_Escape(p_a, p_c, discountFactor, timeBeforeCrawl);
                isSolution2Mode = true;
            }
            else
                isSolution2Mode = false;

            DataToAccumulateBeforeEscaping = (int)float.Parse(
                AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE.tryRead(policyParams, (l_escapeMultiplier * l_escape).ToString()));
            //DataToAccumulateBeforeEscaping =
            //  (int)float.Parse(input.ShowDialog(new string[] { AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE }, "input", new string[] { (l_escapeMultiplier * l_escape).ToString() }).First());

            DataToAccumulateBeforeEscaping = Math.Max(DataToAccumulateBeforeEscaping, 1);

                
                // TODO: since this algorithm is supposed to be optimal against solution 2, and we know that with solution 2, area pursuers may sometimes visit bottom circumference , we prefer go up (also, stiching group of circumference algorithm has higher density)
                // TODO2: since right now the algorithm allows only symmetric division, this is unimportant. after we use the improved
                // circular alg., the evaders might want to avoid this.
                //Int32.TryParse(APursuersPolicy.defaults["MaximalPursueresDiff"], out maxPursuersDiff); // set by solution2's policy
                //if (maxPursuersDiff != -1)
                //{
                //    for (int i = 0; i < sinks.Count; ++i)
                //        if (sinks[i].Y > destTarget.Y)
                //        {
                //            sinks[i] = sinks.Last();
                //            --i;
                //            sinks.RemoveAt(sinks.Count - 1);
                //        }
                //}


                //int sinkDist = (int)g.getMinDistance(destTarget,g.getNodesByType(NodeType.Sink).Last());
                //// we remove the extreme sinks, because they force the evaders to move in a straight line to the senstivie area, which may get them stuck (moevemnt functions don't know how to go around)
                //for (int i = 0; i < sinks.Count; ++i)
                //    if (Math.Abs(sinks[i].X - destTarget.X) == sinkDist ||
                //        Math.Abs(sinks[i].Y - destTarget.Y) == sinkDist)
                //    {
                //        sinks[i] = sinks.Last();
                //        sinks.RemoveAt(sinks.Count-1);
                //    }



                //randomStartSink = getRandomPointInDistance(destTarget,sinkDist);
                //randomStartSink = sinks[new Random().Next(0, sinks.Count)];
            return true;   
        }
        
        private List<Point> sinks = new List<Point>();
        int maxPursuersDiff = -1;
    }
}
