using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils.Algorithms;
using GoE.AppConstants;

namespace GoE.Policies
{
   
    class EvadersPolicyTransmitFromWithinArea : AGoEEvadersPolicy
    {
        private GridGameGraph g;
        private GoEGameParams gm;
        private IPolicyGUIInputProvider pgui;

        private int prevCurrentRound = 0;
        private GameState prevS;
        private IEnumerable<Point> prevO_d;
        private HashSet<Point> prevO_p;
        //private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();

        //bool movingIntoSensitiveArea; // tells whether the evader goes in or out

        private List<Evader> memSlotToEvader = new List<Evader>();
        private Dictionary<Evader, int> evaderToMemSlot = new Dictionary<Evader, int>();
        private List<Evader> activeEvaders = new List<Evader>();
        private List<Point> startPoints = new List<Point>();
        private Dictionary<Point, Point> startTodestPoints = new Dictionary<Point, Point>();
        private float prevAngle = 0;
        private float maxAngleBetweenEvaders;

        private Dictionary<Evader, Point> activeEvaderDestination = new Dictionary<Evader, Point>();
        private Dictionary<Evader, Point> evaderLocation = new Dictionary<Evader, Point>();
        
            
        private int usedSimultenousTransmissions = 1;
        private List<Evader> nextEvaders = new List<Evader>();

        private int sinkToTargetDist;
        private Point randomStartSink;
        private Point destTarget; // point of a target
        private Point temporaryDestination; // used in mode 2 only

        private int blocksInSensitiveArea = 0;
        private int accumulatedData = 0;
        private bool isSolution2Mode;


        private int enteredArea = 0;
        private int survivedArea = 0;
        private int enteredCircumference = 0;
        private int survivedCircumference = 0;
        private int enteredPursuit = 0;
        private int survivedPursuit = 0;

        public EvadersPolicyTransmitFromWithinArea()
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
                return new List<ArgEntry>(new ArgEntry[]{ AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS});
            }
        }

        // derivative of practicalEStay()
        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        private static double practicalEStayDiff(double p_a, double p_c, double p_p,double p_d, double discountFactor, double x)
        {
            // prev code, with no p_d
            //double lnSurvival = Math.Log(1.0 - p_a, Math.E);
            //double lnDiscount = Math.Log(discountFactor, Math.E);
            //double discountX = Math.Pow(discountFactor, x);
            //double discountSink = 1; // Math.Pow(discountFactor, kappa); // we now assume the amount of rounds until sink is 0, but technichally this could be a parameter

            //double termdiv = discountSink * (discountX - 1);
            //double risk = Math.Pow((1 - p_a), x);
            //double term1 = 1.0 - risk * (lnSurvival * x + 1);
            //double term2 = ((1.0 - risk) * x + p_p) * (discountFactor - 1) * discountX * lnDiscount;
            //return term1 * (discountFactor - 1.0) / termdiv - term2 / (termdiv * (discountX - 1));

            double undetectedX = Math.Pow(1 - p_d, x);
            double lnSurvival = Math.Log(1.0-p_a, Math.E);
            double lnDiscount = Math.Log(discountFactor, Math.E);
            double discountX = Math.Pow(discountFactor, x);
            double discountSink = 1; // Math.Pow(discountFactor, kappa); // we now assume the amount of rounds until sink is 0, but technichally this could be a parameter
            
            double termdiv = discountSink * (discountX - 1);
            double risk = Math.Pow((1-p_a),x);
            double term1 = 1.0 - risk * (lnSurvival * x + 1) - p_p* undetectedX * Math.Log(1-p_d,Math.E);
            double term2 = ((1.0 - risk) * x + p_p *(1-Math.Pow(1-p_d,x))) * (discountFactor - 1) * discountX * lnDiscount;
            return term1 * (discountFactor - 1.0) / termdiv - term2 / (termdiv * (discountX - 1));

            // note that we disregard p_c, since it's effect is independent of x, happens one time and doesn't affect the extreimum of the diff function
        }

        // similar to calculation of e_stay, but assumes evaders get the risk p_a for the entire time
        /// assumes p_c includes the added risk of + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double practicalEStay(double p_a, double p_c, double p_p, double p_d, double discountFactor, double eta_tag)
        {
           double discountX = Math.Pow(discountFactor, eta_tag);
           double discountSink = 1; // Math.Pow(discountFactor, kappa); // we now assume the amount of rounds until sink is 0, but technichally this could be a parameter
           double risk = Math.Pow((1-p_a),eta_tag);
           double term1 = (1 - risk) * eta_tag + p_p *(1 - Math.Pow(1 - p_d, eta_tag));
           double term2 = 1.0 / (discountSink * (discountX - 1) / (discountFactor - 1));

           p_c = Math.Max(p_c, p_a);
           return term1 * term2 / ((1 - p_c)/(1-p_a)); // at the point of entry, we replace one risk of p_a with a risk of p_c
        }

        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double practicalEStay_pessimistic(double p_a, double p_c, double p_p, double p_d, double discountFactor, double eta_tag)
        {
            /// unlike practicalEStay(), this function also regards the evaders that die between the
            /// simulteneous tranmission. Since in avg between the transmissions we know that 'optimisticRisk'
            /// evaders die, then another 'optimisticRisk' have to enter and the surviving ones 
            /// need to wait for additional 'optimisticRisk' rounds. Tehrefore, we approximate
            /// the "accumulated danger" (from below) by assuming that all eta_tag evaders have to suffer 
            /// for additional 'optimisticRisk' rounds.
            // NOTE: to be even more accurate, we can use a recursive infinite sum, but I assume the difference would be negligable

            double discountX = Math.Pow(discountFactor, eta_tag);
            double discountSink = 1; // Math.Pow(discountFactor, kappa); // we now assume the amount of rounds until sink is 0, but technichally this could be a parameter
            //double risk = Math.Pow((1 - p_a), eta_tag); // the practicalEStay() risk that tells how many die before transmission
            double optimisticRisk = Math.Pow((1 - p_a), eta_tag);
            double optimisticAvgDeadBeforeTransmission = (1 - optimisticRisk) * eta_tag;
            double extendedTimeRisk = Math.Pow((1 - p_a), eta_tag + optimisticAvgDeadBeforeTransmission);

            double term1 = (1 - extendedTimeRisk) * eta_tag + p_p * (1 - Math.Pow(1 - p_d, eta_tag));
            double term2 = 1.0 / (discountSink * (discountX - 1) / (discountFactor - 1));

            p_c = Math.Max(p_c, p_a);
            return term1 * term2 / ((1 - p_c) / (1 - p_a)); // at the point of entry, we replace one risk of p_a with a risk of p_c
        }
        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static int findBestPracticalEtaTag(double p_a, double p_c, double p_p, double p_d, double discountFactor, int eta)
        {
            GoE.Utils.RootFinding.FunctionOfOneVariable optimalEtaTagDiffCaller =
              (double x) =>
              {
                  return practicalEStayDiff(p_a, p_c, p_p, p_d,discountFactor, x);
              };

            OptimizedObj<double> best_eta = new OptimizedObj<double>() { value = double.MaxValue };
            OptimizedObj<double> eta_1 = new OptimizedObj<double>() { value = practicalEStay(p_a, p_c, p_p, p_d,discountFactor, 1), data = 1 };
            OptimizedObj<double> eta_eta = new OptimizedObj<double>() { value = practicalEStay(p_a, p_c, p_p, p_d,discountFactor, eta), data = eta };

            if (optimalEtaTagDiffCaller(1) * optimalEtaTagDiffCaller(eta) < 0)
            {
                double extremePoint = GoE.Utils.RootFinding.NumericMethods.Brent(optimalEtaTagDiffCaller, 1, eta, 1e-7);
                double extremePointCeil = Math.Ceiling(extremePoint);
                double extremePointFloor = Math.Floor(extremePoint);
                best_eta.setIfValueDecreases(extremePointCeil, practicalEStay(p_a,p_c,p_p,p_d,discountFactor, extremePointCeil));
                best_eta.setIfValueDecreases(extremePointFloor, practicalEStay(p_a,p_c,p_p,p_d,discountFactor, extremePointFloor));
            }
            best_eta.setIfValueDecreases(eta_1);
            best_eta.setIfValueDecreases(eta_eta);

            return (int)best_eta.data;
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
                if (g.getMinDistance(p, runningAwayFrom) == dist)
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


        int roundsFromPreviousTransmission = 0;
        int readyToTransmit = -1; // start with -1, since on the first time evaders just enter the s.a. , they still don't accumulate units (but we increase readyToTransmit anyway)


        // remove dead evaders from: activeEvaderLocation, activeEvadersByAge, destPreperationLocation
        public int removeDeadEvaders()
        {
            int deadEvadersCount = 0;
            foreach (Point d in prevO_d)
            {
                var eIter = activeEvadersByAge.First;

                while (eIter != null)
                {
                    var eNextIter = eIter.Next;
                    if (d == evaderLocation[eIter.Value])
                    {
                        ++deadEvadersCount;
                        evaderLocation.Remove(eIter.Value);
                        destPreperationLocation.Remove(eIter.Value);
                        activeEvadersByAge.Remove(eIter);
                        allCapturedEvaders.Add(eIter.Value);
                    }
                    eIter = eNextIter;
                }
            }
            return deadEvadersCount;
        }
        /// <summary>
        /// if evaders still didn't assume their position near circumference, this advances them
        /// </summary>
        /// <returns></returns>
        public Dictionary<Evader, Tuple<DataUnit, Location, Location>> advancePreparingEvaders()
        {
            

            bool isBlocked;
            Dictionary<Evader, Tuple<DataUnit, Location, Location>> res;

            if(evaderLocation.Count == 0 && recruitOrder.Count > 0) 
            {
                res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
                foreach(Evader e in this.gm.A_E)
                {
                    Point evaderDest = destPreperationLocation[e];
                    float angle = GameLogic.Utils.getAngleOfGridPoint(evaderDest.subtruct(destTarget));
                    Point startPoint = destTarget.add(GameLogic.Utils.getGridPointByAngle(sinkDist,angle));
                    evaderLocation[e] = startPoint;
                    res[e] = Tuple.Create(DataUnit.NIL,new Location(startPoint),new Location(destTarget));
                }
                return res;
            }
            
            if (remainingPreperationRounds == 0)
                return null;

            res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
            bool wasAnyBlocked = false;
            foreach (Evader e in this.gm.A_E)
            {
                if (!evaderLocation.ContainsKey(e))
                    continue; // evader dieded

                Point evaderDest = destPreperationLocation[e];
                Point evaderCurrentLoc = evaderLocation[e];
                Point nextLoc = evaderDest;
                if (evaderDest != evaderCurrentLoc)
                {
                    nextLoc = advanceOnPath(evaderCurrentLoc, evaderDest, true, out isBlocked);
                    wasAnyBlocked |= isBlocked;


                }
                evaderLocation[e] = nextLoc;
                res[e] = Tuple.Create(DataUnit.NIL, new Location(nextLoc), new Location(destTarget));
            }

            if(!wasAnyBlocked)
                --remainingPreperationRounds;
            

            return res;
            
        }

        /// <summary>
        /// replenishes 'activeEvadersByAge' by taking evaders from 'recruitOrder'
        /// </summary>
        /// <returns>
        /// current active evaders that accumulate data
        /// </returns>
        private int replenishActiveEvaders()
        {
            int evadersToCall = (int)Math.Floor(((float)(usedSimultenousTransmissions - activeEvadersByAge.Count)) / (1.0-p_c)); // if 0.9 evaders that try to enter will die, for each missing evader we want to tell 10 new evaders to enter

            // @FIXED PURSUIT
            if(recruitOrder.Count > 0 && evadersToCall > 0)
            {
                --readyToTransmit; // evaders that are on the circumference will not transmit, since then their capure probability is higher there
            }

            while (recruitOrder.Count > 0 && evadersToCall > 0)
            {
                --evadersToCall;
                Evader e;
                do
                {
                    e = recruitOrder.First.Value;
                    recruitOrder.RemoveFirst();
                }
                while (!evaderLocation.ContainsKey(e)); // make sure we are not recruiting a dead evader
                activeEvadersByAge.AddFirst(e);
            }

            return activeEvadersByAge.Count;
        }

        /// <summary>
        /// set res for each evader in activeEvadersByAge
        /// </summary>
        /// <returns></returns>
        private Dictionary<Evader, Tuple<DataUnit, Location, Location>> setActiveEvadersCommands()
        {
            Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
            var eIter = activeEvadersByAge.First;
            int oldestTransmittedDataRound = int.MaxValue;
            bool isBlockedTmp;
            while (eIter != null)
            {
                DataUnit toTransmit = DataUnit.NIL;

                if (readyToTransmit >= usedSimultenousTransmissions) // if transmission cycle is done, we set 'toTransmit' for each evader
                {
                    var newestMemRange = prevS.M[prevS.MostUpdatedEvadersMemoryRound][eIter.Value].roundRanges.Last.Value; // we assume only last range is relevant, since data accumulation is sequential

                    if (newestMemRange.minRound > 0) // if evader holds any data, and it is "younger" than all following evaders - we take his newest data and transmit it
                    {
                        if (oldestTransmittedDataRound == int.MaxValue)
                        {
                            oldestTransmittedDataRound = newestMemRange.maxRound;
                            toTransmit = new DataUnit() { round = oldestTransmittedDataRound, sourceTarget = new Location(destTarget) };
                        }
                        else if (newestMemRange.isInRange((oldestTransmittedDataRound - 1)))
                        {
                            oldestTransmittedDataRound = (oldestTransmittedDataRound - 1);
                            toTransmit = new DataUnit() { round = oldestTransmittedDataRound, sourceTarget = new Location(destTarget) };
                        }
                        else if (newestMemRange.isInRange(oldestTransmittedDataRound))
                        {
                            // unfortunately, several evaders only have the same newest data. we still want to transmit, even if it's duplicate, since
                            // one of them might die before finishing transmission
                            toTransmit = new DataUnit() { round = oldestTransmittedDataRound, sourceTarget = new Location(destTarget) };
                        }
                    }
                }

                Point eveLocation = evaderLocation[eIter.Value];
                Point nextEveLocation = eveLocation;
                if (eveLocation.manDist(destTarget) >= gm.r_e) // if an evader was just recruited, make sure it entered beyond the circumference
                {
                    nextEveLocation = advanceOnPath(eveLocation, destTarget, true, out isBlockedTmp);
                    evaderLocation[eIter.Value] = nextEveLocation;
                }
                res[eIter.Value] = Tuple.Create(toTransmit, new Location(nextEveLocation), new Location(destTarget));

                eIter = eIter.Next;
            }
            return res;
        }


        public override void gameFinished()
        {
            //pgui.addLogValue(AppConstants.Policies.PatrolAndPursuit.RESULTED_P_P,
            //                 (1 - ((float)survivedPursuit / enteredPursuit)).ToString());
            pgui.addLogValue(AppConstants.Policies.PatrolAndPursuit.RESULTED_P_C.key,
                             (1 - ((float)survivedCircumference / enteredCircumference)).ToString());
            pgui.addLogValue(AppConstants.Policies.PatrolAndPursuit.RESULTED_P_A.key,
                             (1 - ((float)survivedArea / enteredArea)).ToString());
        }


        // fxime remove debug:
        Dictionary<Evader, Tuple<DataUnit, Location, Location>> prevRes = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();



        List<Evader> allCapturedEvaders = new List<Evader>();
        public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep()
        {
            Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = null;
            Random tmpRand = new ThreadSafeRandom().rand;
            bool isBlocked;

            int deadEvadersCount;
            
            //try
            //{
                deadEvadersCount = removeDeadEvaders();
            //}
            //catch(Exception ex)
            //{

            //}
            
            Dictionary<Evader, Tuple<DataUnit, Location, Location>> preperationRes = null;
            
            //try
            //{
                preperationRes = advancePreparingEvaders();
            //}
            //catch(Exception ex)
            //{

            //}
            if (preperationRes != null)
                return preperationRes;
            
            // evaders are prepared: let them advance so they can accumulate data, and transmit if time is right
            int currentActiveEvaders = 0;

            //try
            //{
                currentActiveEvaders = replenishActiveEvaders();
            //}
            //catch(Exception ex)
            //{

            //}

            usedSimultenousTransmissions = Math.Min(usedSimultenousTransmissions, currentActiveEvaders);
            //try
            //{
                res = setActiveEvadersCommands();
            //}
            //catch(Exception ex)
            //{

            //}
          
            // set res for remaining evaders:
            foreach(Evader e in recruitOrder) // set action for pending evaders
                res[e] = Tuple.Create(DataUnit.NIL, new Location(evaderLocation[e]), new Location(destTarget));
            foreach (Evader e in allCapturedEvaders)
                res[e] = Tuple.Create(DataUnit.NIL, new Location(Location.Type.Captured), new Location(Location.Type.Undefined)); // for some reason, ommiting the typles of dead evaders caused bugs. didn't have time to figure this out

            if (readyToTransmit >= usedSimultenousTransmissions)
                readyToTransmit = 0;
            ++readyToTransmit;


            // fixme debug remove loop below?
            #if EXTENSIVE_TRYCATCH
            bool didTransmit = false;
            bool transmitterDied = false;
            foreach(Evader re in prevRes.Keys) 
            {
                if (prevRes[re].Item1 != DataUnit.NIL)
                {
                    didTransmit = true;
                    if (!res.ContainsKey(re) || res[re].Item2.locationType == Location.Type.Captured)
                        transmitterDied = true;
                }
                if(prevRes[re].Item2.locationType == Location.Type.Node)
                {
                    if(prevRes[re].Item2.nodeLocation.manDist(destTarget) == this.gm.r_e)
                    {
                        ++enteredCircumference;
                        if (res.ContainsKey(re) && res[re].Item2.locationType == Location.Type.Node &&
                            res[re].Item2.nodeLocation.manDist(destTarget) == this.gm.r_e - 1)
                        {
                            ++survivedCircumference;
                        }
                    }
                    if (prevRes[re].Item2.nodeLocation.manDist(destTarget) == this.gm.r_e-1)
                    {
                        ++enteredArea;
                        if (res.ContainsKey(re) && res[re].Item2.locationType == Location.Type.Node &&
                            res[re].Item2.nodeLocation.manDist(destTarget) == this.gm.r_e - 1)
                        {
                            ++survivedArea;
                        }
                    }
                }
            }
            prevRes = res;

            if(didTransmit)
            {
                ++enteredPursuit;
                if (transmitterDied == false)
                    ++survivedPursuit;
            }
            // fixme remove above
            #endif


            return res; // code below wa used before discount factor was introduced
            /*
            res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();

            int newAvailableSlots = 0;
            foreach(Point d in prevO_d)
            {
                List<Evader> deadEvaders = new List<Evader>(prevO_d.Count());
                foreach(var e in evaderLocation)
                {
                    if (d == e.Value)
                    {
                        activeEvaders.Remove(e.Key);

                        int eMemSlot = evaderToMemSlot[e.Key];
                        if (eMemSlot < memSlotToEvader.Count) // TODO: for some reason this sometimes causes an index out of range error. could this be a bug that affects performance?
                            memSlotToEvader[eMemSlot] = null;
                        evaderToMemSlot.Remove(e.Key);
                        ++newAvailableSlots;
                        deadEvaders.Add(e.Key);
                    }
                }
                foreach (var e in deadEvaders)
                    evaderLocation.Remove(e);
            }

            if (newAvailableSlots > 0)
            {
                List<Evader> newMemSlotToEvader = new List<Evader>();
                
                int replacements = Math.Min(nextEvaders.Count, newAvailableSlots);
                for (int i = 0; i < replacements; ++i)
                    newMemSlotToEvader.Add(null);
                for (int i = 0; i < memSlotToEvader.Count; ++i)
                    if (memSlotToEvader[i] != null)
                    {
                        evaderToMemSlot[memSlotToEvader[i]] = newMemSlotToEvader.Count();
                        newMemSlotToEvader.Add(memSlotToEvader[i]);
                    }
                readyToTransmit = Math.Max(readyToTransmit - newAvailableSlots, 0);
                memSlotToEvader = newMemSlotToEvader; // we rebuilt the slot to evader mapping, so newer evaders will have the newest slots

                foreach (Evader e in activeEvaders)
                    readyToTransmit = Math.Min(readyToTransmit, prevS.M[prevCurrentRound][e].Count - 1);
            }
            while (activeEvaders.Count() < usedSimultenousTransmissions) // evader got captured/policy just initialized we send another one...
            {
                if (nextEvaders.Count == 0)
                {
                    usedSimultenousTransmissions = activeEvaders.Count;
                    break;
                }

                activeEvaders.Add(nextEvaders.Last());
                nextEvaders.RemoveAt(nextEvaders.Count - 1);

                if (memSlotToEvader.Count < usedSimultenousTransmissions) // should only happen for the first batch of evaders that come out
                {
                    int c = evaderToMemSlot.Count();
                    evaderToMemSlot[activeEvaders.Last()] = c;
                    memSlotToEvader.Add(activeEvaders.Last());
                }
                else
                {
                    memSlotToEvader[--newAvailableSlots] = activeEvaders.Last();
                    evaderToMemSlot[activeEvaders.Last()] = newAvailableSlots;
                }

                if (maxAngleBetweenEvaders < 2)
                {
                    evaderLocation[activeEvaders.Last()] =
                        destTarget.add(GoE.GameLogic.Utils.getGridPointByAngle(sinkDist, prevAngle));
                    prevAngle += (float)(2.0f * Math.Max((1.0f / sinkDist), globalRand.NextDouble()) * maxAngleBetweenEvaders);
                    
                    while (prevAngle > 4.0f)
                        prevAngle -= 4.0f;
                }
                else
                    evaderLocation[activeEvaders.Last()] =
                        startPoints[globalRand.Next(0, startPoints.Count)];

                activeEvaderDestination[activeEvaders.Last()] =
                    startTodestPoints[evaderLocation[activeEvaders.Last()]];
            }

            int evadersInArea = 0;
            foreach(Evader e in activeEvaders)
            {
                // go toward target point, if needed
                if(g.getMinDistance(evaderLocation[e], activeEvaderDestination[e]) > 0)
                {
                    evaderLocation[e] =
                        advanceOnPath(evaderLocation[e],
                                      activeEvaderDestination[e], false, out isBlocked);

                    if (isBlocked && g.getMinDistance(evaderLocation[e], activeEvaderDestination[e]) == 1)
                    {
                        // in case there are pursuit pursuers, they might be standing in place. if it's the destination place, in order to avoid
                        // getting stuck, we need to change the destination
                        
                        evaderLocation[e] = 
                            advanceOnPath(evaderLocation[e],
                                          destTarget, false, out isBlocked);

                        if (isBlocked) // if we are still blocked, then it's because the agent is walking vertically/horizontally into the sensitive area, and the path is block by a pursuer.  
                        {
                            float rotationAngle = 1.0f / (gm.r_e);
                            //if (prevO_p.Contains(activeEvaderDestination[e]))
                            activeEvaderDestination[e] = GoE.GameLogic.Utils.getRotated(activeEvaderDestination[e], destTarget, rotationAngle);

                            evaderLocation[e] =
                                advanceOnPath(evaderLocation[e],
                                              activeEvaderDestination[e], false, out isBlocked);
                        }
                        else
                            activeEvaderDestination[e] = evaderLocation[e];
                    }


                }

                if (readyToTransmit == usedSimultenousTransmissions ||
                    readyToTransmit >= activeEvaders.Count() )
                {
                    int memCount = prevS.M[prevCurrentRound][e].Count-1;
                    int slotIdx = Math.Max(1, memCount - evaderToMemSlot[e]); // if evaders die and evaderToMemSlot is rebuilt, and in some cases we give a too high slot for evaders that don't have enough information (happens only after optimalSimultenousTransmissions decreases)
                    
                    res[e] =
                           Tuple.Create(prevS.M[prevCurrentRound][e][slotIdx],
                           new Location(evaderLocation[e]),
                           new Location(destTarget));
                }
                else
                {
                    res[e] = Tuple.Create(DataUnit.NIL, new Location(evaderLocation[e]), new Location(destTarget));
                }

                if ((int)g.getMinDistance(evaderLocation[e], destTarget) <= (int)gm.r_e)
                {
                    ++evadersInArea; 
                }
            }



            if (readyToTransmit == usedSimultenousTransmissions ||
                readyToTransmit >= activeEvaders.Count())
            {
                readyToTransmit = 0; // since we just transmitted
            }

            if (evadersInArea == usedSimultenousTransmissions ||
                evadersInArea >= activeEvaders.Count())
            {
                ++readyToTransmit;
            }
            
            foreach (Evader e in nextEvaders)
                res[e] = Tuple.Create(DataUnit.NIL, new Location(Location.Type.Unset), new Location(destTarget));




          

            return res;
            */
        }
        Random globalRand = new ThreadSafeRandom().rand;

        Dictionary<Evader, Point> destPreperationLocation = new Dictionary<Evader, Point>();
        LinkedList<Evader> recruitOrder = new LinkedList<Evader>();
        LinkedList<Evader> activeEvadersByAge = new LinkedList<Evader>(); // youngest first. this tells the evaders that are actually in the sensiutive area and accumulate data
        int remainingPreperationRounds;
        bool initPreperation;


        double p_a;
        double p_c; // includes the added + gameParams.EvaderCircumferenceEntranceKillProb 
        double p_p;

        Dictionary<string, string> policyInput;
        public override bool init(AGameGraph G, GoEGameParams prm, AGoEPursuersPolicy pursuers, IPolicyGUIInputProvider gui,
            Dictionary<string, string> PolicyInput)
        {
            this.policyInput = PolicyInput;
            this.g = (GridGameGraph)G;
            this.gm = prm;
            this.pgui = gui;
            nextEvaders.AddRange(prm.A_E);

            
            usedSimultenousTransmissions = 1;
         
            List<string> typeRes;
            //if (gui.hasBoardGUI())
            //    typeRes = this.pgui.ShowDialog(new string[] { AppConstants.Policies.PatrolAndPursuit.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS_MULTIPLIER }, "transmit-from-within evader policy input one time init", new string[] { "1.0" });
            //else
            //typeRes = this.pgui.ShowDialog(new string[] { AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS_MULTIPLIER }, "transmit-from-within evader policy input one time init", null);

            float SimultenousTransmissionsMultiplier = 1;
            //float.TryParse(typeRes.Last(), out SimultenousTransmissionsMultiplier);
            
            PatrolAndPursuitLearner l = new PatrolAndPursuitLearner();
            l.init(new PursuersLearnerInitData() { g = (GridGameGraph)G, s = prm, p = pursuers });
            p_a = l.AreaPatrolCaptureProbability;
            p_p = l.PursuitCaptureProbability;
            p_c = //l.CircumferencePatrolCaptureProbability + prm.EvaderCircumferenceEntranceKillProb;
                prm.EvaderCircumferenceEntranceKillProbWithPC(l.CircumferencePatrolCaptureProbability);

            double discountFactor = 0.999999999;
            ConstantExponentialDecay discR = prm.R as ConstantExponentialDecay;
            if (discR != null)
                discountFactor = discR.oneRoundDiscountFactor;


            //PatrolAndPursuit.calculateE_Stay(prm.A_E.Count, p_a, p_p, prm.p_d,0, discountFactor, out usedSimultenousTransmissions);

            usedSimultenousTransmissions = findBestPracticalEtaTag(p_a, p_c, p_p, prm.p_d, discountFactor, prm.A_E.Count);
            usedSimultenousTransmissions = (int) (usedSimultenousTransmissions* SimultenousTransmissionsMultiplier);

            //usedSimultenousTransmissions = 
            //    (int)double.Parse(
            //    this.pgui.ShowDialog(new string[] {
            //        AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS }, "transmit-from-within evader policy input",
            //    new string[] { 
            //        usedSimultenousTransmissions.ToString()}).First());

            usedSimultenousTransmissions = (int)double.Parse(
                AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS.tryRead(policyInput, usedSimultenousTransmissions.ToString()));

            usedSimultenousTransmissions = Math.Min(usedSimultenousTransmissions, prm.A_E.Count);


            //// fixme remove now 
            //usedSimultenousTransmissions = 20;


            //int realOpt;
            //double e_stay = PatrolAndPursuit.calculateE_Stay(prm.A_E.Count(), p_a, p_p, out realOpt);
            destTarget = g.getNodesByType(NodeType.Target).First();

            var sinks = g.getNodesByType(NodeType.Sink);
            //int sinkDist = (int)g.getMinDistance(destTarget,g.getNodesByType(NodeType.Sink).Last());
            //randomStartSink = getRandomPointInDistance(destTarget,sinkDist);
            //randomStartSink = sinks[new Random().Next(0, sinks.Count)];
            sinkDist = Int32.MaxValue;
            foreach (Point p in sinks)
            {
                sinkDist = Math.Min(sinkDist, (int)g.getMinDistance(destTarget, p));
            }

            startPoints = sinks;// g.getNodesInDistance(destTarget, sinkDist);
            
            
            remainingPreperationRounds = sinkDist - (gm.r_e+1);
            // ew want to spread evaders evenly to reduce chance they stand on the same point. additionally, we don't want them to stand near leftmost and rightmost points of circumference
            float angleJump = 3.0f / gm.r_e;
            float maxAngle = (2 * gm.r_e - 1) * angleJump;
            float badAngle = 1;
            float currentAngle = angleJump; ;
            int evaderIdx = 0;
            while (evaderIdx < gm.A_E.Count)
            {
                recruitOrder.AddLast(gm.A_E[evaderIdx]);
                
                destPreperationLocation[gm.A_E[evaderIdx]] = 
                    destTarget.add(GoE.GameLogic.Utils.getGridPointByAngle(gm.r_e+1, currentAngle));
                currentAngle += angleJump;
                if (Math.Abs(currentAngle - Math.Round(currentAngle)) < 0.001) // angles 0,1,2,3 are problematic because pursuit pursuers just stand there
                    currentAngle += angleJump;
                if (currentAngle > maxAngle)
                    currentAngle = angleJump;
                

                ++evaderIdx;
            }

            return true;

            List<Point> destPoints;
            if (p_c == 0)
            {
                // no problem with staying on circumference
                destPoints = g.getNodesInDistance(destTarget, gm.r_e);
            }
            else
            {
                destPoints = g.getNodesInDistance(destTarget, gm.r_e - 1);
            }
            maxAngleBetweenEvaders = 4.0f / usedSimultenousTransmissions;

            foreach (Point s in startPoints)
            {
                int dist = Int32.MaxValue;
                Point bestP = new Point(-1, -1);
                foreach (Point d in destPoints)
                {
                    if ((int)g.getMinDistance(s, d) < dist)
                    {
                        bestP = d;
                        dist = (int)g.getMinDistance(s, d);
                    }
                    else if ((int)g.getMinDistance(s, d) == dist)
                    {
                        if (globalRand.Next(0, 2) == 0)
                        {
                            bestP = d;
                            dist = (int)g.getMinDistance(s, d);
                        }
                    }
                }
                startTodestPoints[s] = bestP;
            }

            return true;
        }

        int sinkDist = Int32.MaxValue;
    }
}
