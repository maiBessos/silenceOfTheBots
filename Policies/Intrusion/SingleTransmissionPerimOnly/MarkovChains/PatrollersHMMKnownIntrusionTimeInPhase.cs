using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using GoE.Utils.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.Utils;
using System.Windows.Forms;
using System.IO;
using GoE.Utils.Extensions;
using Mathematics;
using SparseCollections;

using Accord.Statistics;

/// <summary>
/// in this policy, the two patroller go both either clockwise, or counter clockwise.
/// they switch direction once per N-2T+1 steps ( every N-2T+1 steps are called a "phase").
/// in the begining of each phase, the policy chooses 2 destination points for the patrollers,
/// (such that both patrollers can go to cc/ccw without colliding)
/// and each patroller goes cw/ccw towards it's new destination.
/// </summary>
namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly.PhasePatrollerMovement
{
    class PatrollersHMMKnownIntrusionTimeInPhaseDeprecated : PatrollersHMM
    {
        public class CompactPatrollerState : IEquatable<CompactPatrollerState>
        {
            public static CompactPatrollerState DEAD_END_STATE // the state observer reaches after (and only after) transmitting
            {
                get
                {
                    return DEAD_END_STATE_VAL;
                }
            }

            public LocationSet Destinations
            {
                get
                {
                    return new LocationSet() { p1 = DestP1, p2 = DestP2 };
                }
            }
            
            public int Dir // -1 or 1. one round CW, one round CCW
            {
                get; protected set;
            }
            public int DestP1 // 0 to N-1, p1 and p2 always in distance >= 2T+1 from each other
            {
                get; protected set;
            }
            public int DestP2 // 0 to N-1, p1 and p2 always in distance >= 2T+1 from each other
            {
                get; protected set;
            }
            public int FirstDelaySeg // 0 to PhaseLength-1, telling the ratio  (i.e. 0 to 1) in the way of when the patroller pause. there are two segments in which the patroller will delay, one where this value points and the other in distance of additional 'delaySegDistance' segments from this segment (if a patroller moves less than delaySegDistance segments, then the delay is ON 'firstDelaySeg')
            {
                get; protected set;
            }
            public int FirstDelay // 0 to PhaseLength-1, telling the ratio  (i.e. 0 to 1) of how much of the delay will be spent on the first of the two "delay segments". if there is only 1 delay segment, this val is irrelevant
            {
                get; protected set;
            }
            public int CanCaptureIntruder
            {
                get; protected set;
            }

            private CompactPatrollerState()
            {
                practicalVals = new List<int>();
            }
            public CompactPatrollerState(CompactPatrollerState src)
            {
                this.Dir = src.Dir;
                this.DestP1 = src.DestP1;
                this.DestP2 = src.DestP2;
                this.FirstDelaySeg = src.FirstDelaySeg;
                this.FirstDelay = src.FirstDelay;
                this.CanCaptureIntruder = src.CanCaptureIntruder;
                practicalVals = new List<int>(src.practicalVals);
            }

            /// <summary>
            /// </summary>
            /// <param name="pol"></param>
            /// <param name="dir"> -1 or 1. one round CW, one round CCW</param>
            /// <param name="destP1"> 0 to N-1, p1 and p2 are always in distance >= 2T+1 from each other </param>
            /// <param name="destP2"> 0 to N-1, p1 and p2 always in distance >= 2T+1 from each other </param>
            /// <param name="firstDelaySeg"> 0 to PhaseLength-1, telling the ratio  (i.e. 0 to 1) in the way of when the patroller pause. there are two segments in which the patroller will delay, one where this value points and the other in distance of additional 'delaySegDistance' segments from this segment (if a patroller moves less than delaySegDistance segments, then the delay is ON 'firstDelaySeg') </param>
            /// <param name="firstDelay"> 0 to PhaseLength-1, telling the ratio  (i.e. 0 to 1) of how much of the delay will be spent on the first of the two "delay segments". if there is only 1 delay segment, this val is irrelevant </param>
            public CompactPatrollerState(PatrollersHMMKnownIntrusionTimeInPhaseDeprecated pol,
                                 int dir, int initP1, int initP2,
                                 int destP1, int destP2,
                                 int firstDelaySeg, int firstDelay)
            {
                this.Dir = dir;
                this.DestP1 = destP1;
                this.DestP2 = destP2;
                this.FirstDelaySeg = firstDelaySeg;
                this.FirstDelay = firstDelay;
                this.CanCaptureIntruder = CanCaptureIntruder;
                
                LocationSet patrollerLocations;
                int p1delaySeg1, p2delaySeg1, p1delaySeg2, p2delaySeg2, p1Seg1Delay, p2Seg1Delay;

                pol.patrollersAtProgress(pol.ProgressAtIntrusion, initP1, initP2, DestP1, DestP2, Dir, FirstDelaySeg, FirstDelay,
                   out patrollerLocations,
                   out p1delaySeg1,
                   out p2delaySeg1,
                   out p1delaySeg2,
                   out p1Seg1Delay,
                   out p2delaySeg2,
                   out p2Seg1Delay);

                practicalVals = new List<int>(11);
                practicalVals.Add(DestP1);
                practicalVals.Add(DestP2);

                PatrollerState start = new PatrollerState(pol, dir, initP1, initP2, destP1, destP2, firstDelaySeg, firstDelay, pol.ProgressAtIntrusion);
                PatrollerState end = new PatrollerState(pol, dir, initP1, initP2, destP1, destP2, firstDelaySeg, firstDelay, pol.ProgressAtIntrusion);


                throw new Exception("uncomment code below, and implement stuff");
                //Observation derivedObservation = pol.getObservation()
                //practicalVals.Add(p1delaySeg1);
                //practicalVals.Add(p2delaySeg1);
                //practicalVals.Add(p1delaySeg2);
                //practicalVals.Add(p2delaySeg2);
                //practicalVals.Add(p1Seg1Delay);
                //practicalVals.Add(p2Seg1Delay);
            }

            public override int GetHashCode()
            {
                int hash = practicalVals.Count;
                for (int i = 0; i < practicalVals.Count; ++i)
                    hash ^= practicalVals[i];
                return hash;
            }
            public virtual bool Equals(CompactPatrollerState y)
            {
                if (practicalVals.Count != y.practicalVals.Count)
                    return false;
                for (int i = 0; i < practicalVals.Count; ++i)
                    if (practicalVals[i] != y.practicalVals[i])
                        return false;
                return true;
            }
            
            private List<int> practicalVals; // helps trimming 'PatrollerState's with values that are identical in practice
            private static CompactPatrollerState DEAD_END_STATE_VAL = new CompactPatrollerState();
        }
        public int ProgressAtIntrusion { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allStates"></param>
        /// <param name="progressAtIntrusion">
        /// value between 0 and allStates.PhaseLength-1.
        /// tells at which patroller 'progress' time the intruder will choose to intrude (instead of allowing the intruder choose any time to intrude, we limit it, assuming the optimal policy does have some "optimal progress for intrusion")
        /// </param>
        public PatrollersHMMKnownIntrusionTimeInPhaseDeprecated(int segCount, int intrusionTimeLength, int delaySegDistance, int observerSeg, int intruderSeg,
            int progressAtIntrusion)
            : base(segCount, intrusionTimeLength, delaySegDistance, observerSeg, intruderSeg)
        {
            ProgressAtIntrusion = progressAtIntrusion;
            // we filter out all states with progress != intrusionTimeInPhase
            
#if DEBUG
            sanityTest();
#endif
        }
        public override void sanityTest()
        {

        }
        public override List<string> writePOMDP()
        {
            Dictionary<int, Dictionary<int, double>> filteredtransitionProb = new Dictionary<int, Dictionary<int, double>>();
            Dictionary<PatrollerState, int> filteredStatesToKey = new Dictionary<PatrollerState, int>();
            Dictionary<Observation, int> observationToKey = new Dictionary<Observation, int>();

            Dictionary<int, PatrollerState> keyToState = new Dictionary<int, PatrollerState>();
            foreach (var v in stateToKey)
                keyToState[v.Value] = v.Key;

            // get only relevant states:
            filteredStatesToKey[PatrollerState.DEAD_END_STATE] = filteredStatesToKey.Count;
            foreach (var s in stateToKey.Keys)
                if (s.Progress == ProgressAtIntrusion)
                    filteredStatesToKey[s] = filteredStatesToKey.Count;

            // for each non trivial transition transition (where not progress increases, but patrollerstate actually changes),
            // use the same probability but for the new filtered states
            foreach (var from in transitionProb)
                if (keyToState[from.Key].Progress == PhaseLength - 1)
                {
                    foreach (var to in from.Value)
                    {
                        var filteredFrom = new PatrollerState(keyToState[from.Key]);
                        var filteredTo = new PatrollerState(keyToState[to.Key]);
                        filteredFrom.updateProgress(this, ProgressAtIntrusion);
                        filteredTo.updateProgress(this, ProgressAtIntrusion);
                        if (!filteredtransitionProb.Keys.Contains(filteredStatesToKey[filteredFrom]))
                            filteredtransitionProb[filteredStatesToKey[filteredFrom]] = new Dictionary<int, double>();
                        filteredtransitionProb[filteredStatesToKey[filteredFrom]][filteredStatesToKey[filteredTo]] = to.Value;
                    }
                }

            // list all possible observations:
            foreach (var fsfrom in filteredStatesToKey)
                foreach (var fsto in filteredStatesToKey)
                    if (fsfrom.Key.Destinations == fsto.Key.Origins)
                    {
                        var obs = getObservation(fsfrom.Key, fsto.Key);
                        if (observationToKey.Keys.Contains(obs))
                            continue;
                        observationToKey[obs] = observationToKey.Count;
                    }



            List<string> res = new List<string>();
            res.Add("discount: 1.0");
            res.Add("values: reward");
            res.Add("states: " + filteredStatesToKey.Count.ToString());
            res.Add("actions: transmit wait"); // 2 actions - continue waiting, or transmitting now
            res.Add("observations: " + observationToKey.Count.ToString()); // tells whether the patroller is currently observed. the direction of movement is implicitly inferred according to phase direction

            // set initial state
            string initialStateProb = "";
            var initProb = getInitialStateProb();
            for (int i = 0; i < initProb.Count; ++i)
                initialStateProb += initProb[i].ToString() + " ";
            res.Add("start: " + initialStateProb);

            // set transitions
            res.Add("T: transmit : * : " + filteredStatesToKey[PatrollerState.DEAD_END_STATE].ToString()); // transmitting always leads to the dead end state
            foreach (var f in transitionProb) // probabilities in transitionProb are normal transitions, when observer waits
                foreach (var t in f.Value)
                    res.Add("T: wait : " + f.Key.ToString() + " : " + t.Key.ToString() + " " + t.Value.ToString());

            // set observations
            foreach (var s in filteredStatesToKey)
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                else
                    //else if (s.Key.P1Location == ObserverSegment || s.Key.P2Location == ObserverSegment)
                    //    res.Add("O : wait : " + s.Value.ToString() + " observed 1.0");
                    //else
                    //    res.Add("O : wait : " + s.Value.ToString() + " unobserved 1.0");

                    // set reward
                    res.Add("R : wait : * : * : * 0.0"); // waiting gives nothing
            res.Add("R : transmit : " + filteredStatesToKey[PatrollerState.DEAD_END_STATE].ToString() + " : * 0.0");
            foreach (var s in filteredStatesToKey)
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                else if (modMinDist(SegCount, s.Key.P1Location, IntruderSeg) <= IntrusionTimeLength ||
                        modMinDist(SegCount, s.Key.P2Location, IntruderSeg) <= IntrusionTimeLength)
                    res.Add("R : transmit : " + s.Value.ToString() + " : * 1.0");
                else
                    res.Add("R : transmit : " + s.Value.ToString() + " : * 0.0");

            return res;
        }
    }

    public struct PatrollersPOMDPKnownIntrusionTimeInPhaseParams
    {
        public int PhaseLength { get { return segCount - 2 * intrusionTimeLength - 1; } }
        public int segCount;
        public int intrusionTimeLength;
        public int delaySegDistance;
        public int observerSeg;
        public int intruderSeg;
        public int progressAtIntrusion;
        public Observation intrusionTrigger;
    }
    
    // this describes a phase from the point of view of the intruder
    // (i.e. the distribution of observations is determined only by the values of the ObservablePhase object.
    // observedPatroller = 0, then Observation distribution is only Observation.NO_OBSERVATION)
    public struct ObservablePhase : IEquatable<ObservablePhase>
    {
        public int observedPatroller; // 0 if no patroller is observed, 1 if p1, 2 if p2
        public int observedPstart, observedPEnd; // with respect observedPatroller, tells the start and end locations of the observed patroller, so we know what are the possible generated observations. if observedPatroller==0, this refers p1
        public int unobservedPStart;
        public int dir; // 1 for CW, -1 for CCW
        public bool didUnobservedReachObserver; // if true, the unobserved patroller reached the observer segment at the end of the phase

        public override string ToString()
        {
            string prefix = "";
            if (observedPatroller == 0)
                prefix = "UnobservedPhase_";

            int unobservedP = observedPatroller == 1 ? 2 : 1;
            return prefix + 
                   ((dir == 1) ? "CW_" : "CCW_") +
                   "Swap_" +(didUnobservedReachObserver ? "Y_": "N_") +
                   observedPatroller.ToString() + "_" + observedPstart.ToString() + "_TO_" + observedPEnd.ToString() + "_" +
                   unobservedP.ToString() + "_" + unobservedPStart.ToString();
        }

        public bool Equals(ObservablePhase other)
        {
            return observedPatroller == other.observedPatroller &&
                observedPstart == other.observedPstart &&
                observedPEnd == other.observedPEnd &&
                unobservedPStart == other.unobservedPStart &&
                dir == other.dir &&
                didUnobservedReachObserver == other.didUnobservedReachObserver;
        }
    }
    
    /// <summary>
    /// constructs the pomdp described in  img/intrusionPretransmissionPhasePolicyCompactPomdp.png
    /// </summary>
    class PatrollersPOMDPKnownIntrusionTimeInPhase
    {
        public const int CW = 1;
        public const int CCW = -1;

        #region public classes
        public class PerDir<T> where T : new()
        {
            /// <summary>
            /// dir=1 => CW
            /// dir=-1 => CCW
            /// </summary>
            /// <param name="dir"></param>
            /// <returns></returns>
            public T this[int dir]
            {
                get
                {
                    return (dir == 1) ? CW : CCW;
                }
                set
                {
                    if (dir == 1)
                        CW = value;
                    else
                        CCW = value;
                }
            }
            public T CW = new T();
            public T CCW = new T();
        }
        /// <summary>
        /// just a typedef
        /// </summary>
        public class ObservablePhaseDistribution : PerDir<Dictionary<ObservablePhase, double>>{}
        #endregion

        #region public getters
        public PatrollersPOMDPKnownIntrusionTimeInPhaseParams gameParams { get; protected set; }
        public PerDir<Dictionary<LocationSet, double>> intrusionSuccessProbPerInitialPbotLocation { get; protected set; } // probability of intrusion if intruder intrudes while this was the initial state, then observer sees the observation it waits for (populated in ctor)
        public Dictionary<LocationSet, ObservablePhaseDistribution> phaseDistributionPerStartLocation { get; protected set; }  // at phase start, given patroller locations, this is the distrbution of phases(populated in ctor)
        public Dictionary<ObservablePhase, List<KeyValuePair<LocationSet, double>>> startLocationDistributionPerPhase { get; protected set; }// (populated in ctor)
        public Dictionary<ObservablePhase, Dictionary<Observation, double>> observationDistribution { get; protected set; } // (populated in ctor)
        #endregion


        /// <summary>
        /// runs the algorithm for 'horizon' time, and finds an observation pattern which maximizes the itnruder's utility
        /// </summary>
        /// <param name="repetitions"></param>
        /// <param name="horizon"></param>
        /// <returns></returns>
        public void simulate(int repetitions, int horizon, out List<Observation> bestPattern, out double expectedIntrusionProbability)
        {
            // - if we started with any specific known state, and got no observation for two rounds, what is the beleif state then?
            // - given any observation, if we started with any specific known state, how much uncertain are we, after 1 back and forth (with any observation)

            // -compare this to naive search - each step, find the observation that will improve the beleif state 
            // as much as possible. 

            // -make many runs, and each time store the beleif best state you reached that maximizes expected reward

            //- forcefully go back and forth around observer

            populatePOMDPStates();

            double[,] transitions = new double[totalStatesCount, totalStatesCount];
            double[] initState = generateInitialDistributionVec();
            double[,] observationDist = new double[totalStatesCount, observationToKey.Keys.Count];


            // set transitions:

            // (set dead end:)
            transitions[0, 0] = 1.0;


            // if observer waits, then probabilities in transitionProb are normal transitions. 
            // if observer transmits, then the game ends (unless we enter a phase that has no triggering observation anyway)
            List<Tuple<KeyValuePair<LocationSet, int>, Dictionary<ObservablePhase, double>>> transitionPairs =
                new List<Tuple<KeyValuePair<LocationSet, int>, Dictionary<ObservablePhase, double>>>();
            //(CW Location to phase:)
            foreach (var f in stateLocToKey.CW)
                transitionPairs.Add(Tuple.Create(f, phaseDistributionPerStartLocation[f.Key].CW));
            //(CCW Location to phase:)
            foreach (var f in stateLocToKey.CCW)
                transitionPairs.Add(Tuple.Create(f, phaseDistributionPerStartLocation[f.Key].CCW));
            foreach (var ftpair in transitionPairs)
            {
                var f = ftpair.Item1;
                foreach (var t in ftpair.Item2)
                    transitions[f.Value, statePhaseToKey[t.Key]] = t.Value;
            }
            //(phase to location:)
            foreach (var f in statePhaseToKey)
                foreach (var t in startLocationDistributionPerPhase[f.Key])
                    if (f.Key.dir == CW) // (from CW to CCW:)
                        transitions[f.Value, stateLocToKey.CCW[t.Key]] = t.Value;
                    else
                        transitions[f.Value, stateLocToKey.CW[t.Key]] = t.Value;
            // set observations:
            foreach (var dest in observationDistribution)
                foreach (var o in dest.Value)
                    observationDist[statePhaseToKey[dest.Key], observationToKey[o.Key]] = o.Value;
            for(int destS = 0; destS < totalStatesCount; ++destS)
            {
                double sum = 0;
                for (int obsI = 0; obsI < observationToKey.Keys.Count; ++obsI)
                    sum += observationDist[destS, obsI];
                observationDist[destS,observationToKey[Observation.NO_OBSERVATION]] = 1.0 - sum;
            }


            //Accord.Statistics.Models.Markov.HiddenMarkovModel hmm =
            //    new Accord.Statistics.Models.Markov.HiddenMarkovModel(
            //        transitions,
            //        observationDist,
            //        initState);

            // question 1: if no observations at all, what is the current beleif state distribution?


            double[] specificInitState = new double[totalStatesCount];
            specificInitState[stateLocToKey.CW[new LocationSet() { p1 = 0, p2 = gameParams.segCount / 2 }]] = 1;

            // TODO: for some reason, compiler had mismatch with assembly. this is a temporary dirty fix
            var types = ReflectionUtils.GetTypesInAllNamespaces("Accord.Statistics");
            Type markovType = null;
            foreach (var t in types)
                if (t.Name == "Accord.Statistics.Models.Markov.HiddenMarkovModel")
                {
                    markovType = t;
                    break;
                }
            var mc = markovType.GetConstructor(new Type[] { typeof(double[,]), typeof(double[,]), typeof(double[]), typeof(bool) });
            var hmm = mc.Invoke(new object[] { transitions,observationDist,specificInitState, false });
            hmm.GetType().GetMethod("Posterior").Invoke(hmm, new object[] { new int[] { 0, 0, 0, 0 } });
            // TODO: original code before relfection:
            //Accord.Statistics.Models.Markov.HiddenMarkovModel hmm =
            //   new Accord.Statistics.Models.Markov.HiddenMarkovModel(
            //       transitions,
            //       observationDist,
            //       specificInitState);
            //var resNoObservation = hmm.Posterior(new int[] { 0, 0 ,0,0 });

            //Dictionary<Tuple<Observation, Observation>, double>



            //// observations depend on the target state

            //// assume we start with a specific location
            //LocationSet startState = new LocationSet() { p1 = 0, p2 = gameParams.segCount / 2 };
            //Dictionary<LocationSet, Dictionary<Tuple<Observation, Observation>, double>> 
            //    obsDistPerDest = new Dictionary<LocationSet, Dictionary<Tuple<Observation, Observation>, double>>();

            //Dictionary<LocationSet, Dictionary<Observation, double>> 
            //    obsDistPerCWDest = new Dictionary<LocationSet, Dictionary<Observation, double>>();
            //Dictionary<LocationSet, Dictionary<Observation, double>> 
            //    obsDistPerCCWDest = new Dictionary<LocationSet, Dictionary<Observation, double>>();


            //foreach (var cwphase in phaseDistributionPerStartLocation[startState].CW)
            //    foreach (var destcw in locationDistributionperPhase[cwphase.Key])
            //    {
            //        if (!obsDistPerCWDest.ContainsKey(destcw))
            //            obsDistPerCWDest[destcw] = new Dictionary<Observation, double>();

            //        getPossibleObservationsKnownDestinations(cwphase, unobservedSegDest, resDictionary, triggeringPaths, ref totalObservationsCount);
            //    }

            //foreach(var from in obsDistPerCWDest.Keys)
            //    foreach (var ccwphase in phaseDistributionPerStartLocation[destcw].CCW)
            //        foreach (var destccw in locationDistributionperPhase[ccwphase.Key])
            //        {
            //            if (!obsDistPerDest.ContainsKey(destccw))
            //                obsDistPerDest[destccw] = new Dictionary<Tuple<Observation, Observation>, double>();

            //            obsDistPerDest[destccw][Tuple.Create()]
            //        }


            //            //locationToLocationIdxMapping

            //// transitions is row stochastic, while locationToLocationCWToCWProbMatrix is col stochastic
            //double[,] transitions = new double[locationToLocationCWToCWProbMatrix.cols, locationToLocationCWToCWProbMatrix.cols];
            //for (int f = 0; f < locationToLocationCWToCWProbMatrix.cols; ++f)
            //    for (int t = 0; t < locationToLocationCWToCWProbMatrix.rows; ++t)
            //        transitions[f, t] = locationToLocationCWToCWProbMatrix[t, f];

            ////Dictionary<Tuple<Observation,Observation>,double>
            ////Accord.Statistics.Models.Markov.HiddenMarkovModel hmm = 
            //  //  new Accord.Statistics.Models.Markov.HiddenMarkovModel()


            bestPattern = new List<Observation>();
            expectedIntrusionProbability = 0;
        }

        public PatrollersPOMDPKnownIntrusionTimeInPhase(PatrollersPOMDPKnownIntrusionTimeInPhaseParams prm)
        {
            gameParams = prm;
            observationDistribution = new Dictionary<ObservablePhase, Dictionary<Observation, double>>();
            startLocationDistributionPerPhase = new Dictionary<ObservablePhase, List<KeyValuePair<LocationSet, double>>>();
            phaseDistributionPerStartLocation = new Dictionary<LocationSet, ObservablePhaseDistribution>();
            intrusionSuccessProbPerInitialPbotLocation = new PerDir<Dictionary<LocationSet, double>>();
            //PerDir<HashSet<ObservablePhase>> allPhases = new PerDir<HashSet<ObservablePhase>>();
            locationDistributionperPhase = new Dictionary<ObservablePhase, List<LocationSet>>(); // gets populated within getPossiblePhases()
            for (int initp1 = 0; initp1 < prm.segCount; ++initp1)
                for (int initp2 = 0; initp2 < prm.segCount; ++initp2)
                {
                    if (MathEx.minModDist(gameParams.segCount, initp1, initp2) <= 2 * gameParams.intrusionTimeLength)
                        continue;  // patrollers must always be at distance >= 2T+1 from each other

                    var currentLoc = new LocationSet() { p1 = initp1, p2 = initp2 };
                    phaseDistributionPerStartLocation[currentLoc] = new ObservablePhaseDistribution();
                    for (int dir = -1; dir <= 1; dir += 2)
                    {
                        phaseDistributionPerStartLocation[currentLoc][dir] = getPossiblePhases(currentLoc, dir);
                        //foreach (var p in phaseDistributionPerStartLocation[currentLoc][dir])
                          //  allPhases[dir].Add(p.Key);
                    }
                }
#if DEBUG
            //testphaseDistributionPerStartLocation();
#endif
            
            for (int dir = -1; dir <= 1; dir += 2)
                foreach (var loc in phaseDistributionPerStartLocation)
                {
                    double triggeringPhaseProbability = 0;
                    double intrusionSuccessProbability = 0;
                    bool isLocationRelevant = false;
                    foreach (var p in loc.Value[dir])
                    {
                        double intrusionSuccessProb;
                        observationDistribution[p.Key] = getPossibleObservations(p.Key, out intrusionSuccessProb);
                        //startLocationDistributionPerPhase[p.Key] = getLocationDistribution(p.Key);
                        startLocationDistributionPerPhase[p.Key] = new List<KeyValuePair<LocationSet, double>>();
                        foreach (var destLoc in locationDistributionperPhase[p.Key])
                            startLocationDistributionPerPhase[p.Key].Add(new KeyValuePair<LocationSet, double>(destLoc, 1.0 / locationDistributionperPhase[p.Key].Count));

                        if (observationDistribution[p.Key].ContainsKey(gameParams.intrusionTrigger))
                        {
                            // if the current phase may trigger the intrusion with one or more observations, we keep the
                            // probability of suceeding when transmitting in this phase
                            triggeringPhaseProbability += p.Value;
                            intrusionSuccessProbability += intrusionSuccessProb * p.Value;
                            isLocationRelevant |= true;
                        }
                    }
                    if (isLocationRelevant)
                        intrusionSuccessProbPerInitialPbotLocation[dir][loc.Key] = intrusionSuccessProbability/ triggeringPhaseProbability;// this location and direction won't trigger the intrusion anyway
                }
#if DEBUG
            //testobservationDistribution();
            //testcalcIntrusionFailProbIftriggered();
#endif
            //for (int dir = -1; dir <= 1; dir += 2)
            //    foreach (var p in allPhases[dir])
            //    {
            //        double intrusionFailProb;
            //        observationDistribution[p] = getPossibleObservations(p,out intrusionFailProb);
            //        startLocationDistributionPerPhase[p] = getLocationDistribution(p);

            //        var initLoc = makeLocationSetInit(p);
            //        double addedVal = intrusionFailProb * phaseDistributionPerStartLocation[initLoc][dir][p];
            //        intrusionFailProbPerInitialPbotLocation[dir].addIfExists(initLoc, addedVal, addedVal);
            //    }
        }

        public double[] generateInitialDistributionVec()
        {
            double[] res = new double[totalStatesCount];

            foreach (var s in statekeysToString)
                if (keyToLocationSetCW.ContainsKey(s.Key) &&
                    initialLocationDistribution.ContainsKey(keyToLocationSetCW[s.Key]))
                    res[s.Key] = initialLocationDistribution[keyToLocationSetCW[s.Key]];
                else
                    res[s.Key] = 0;

            return res;
        }
        public string generateInitialDistributionString()
        {
            string res = "";
            foreach (var s in statekeysToString)
                if (keyToLocationSetCW.ContainsKey(s.Key) &&
                    initialLocationDistribution.ContainsKey(keyToLocationSetCW[s.Key]))
                    res += initialLocationDistribution[keyToLocationSetCW[s.Key]].ToString() + " ";
                else
                    res += "0.0 ";

            return res.Remove(res.Length-1); // remove the last " "
        }
        public List<string> writePOMDP()
        {
            populatePOMDPStates();

            List<string> res = new List<string>();
            res.Add("discount: 1.0");
            res.Add("values: reward");
            res.Add("states: " + makeLine(statekeysToString));
            res.Add("actions: transmit wait"); // 2 actions - continue waiting, or transmitting now
            res.Add("observations: " + makeLine(observationkeysToString)); // tells whether the patroller is currently observed. the direction of movement is implicitly inferred according to phase direction

            // set initial state (assume CW)
            //SortedList<int, double> sortedInitialLocationDistribution = new SortedList<int, double>();
            //foreach (var l in initialLocationDistribution)
            //    sortedInitialLocationDistribution[stateLocToKey.CW[l.Key]] = l.Value;
            res.Add("start: " + generateInitialDistributionString());

            // set transitions:

            // (set dead end:)
            res.Add("T: wait : " + DEAD_END_STATE + " : " + DEAD_END_STATE + " 1.0");
            res.Add("T: transmit : " + DEAD_END_STATE + " : " + DEAD_END_STATE + " 1.0");

            // if observer waits, then probabilities in transitionProb are normal transitions. 
            // if observer transmits, then the game ends (unless we enter a phase that has no triggering observation anyway)
            List<Tuple<KeyValuePair<LocationSet, int>, Dictionary<ObservablePhase, double>>> transitionPairs =
                new List<Tuple<KeyValuePair<LocationSet, int>, Dictionary<ObservablePhase, double>>>();
            //(CW Location to phase:)
            foreach (var f in stateLocToKey.CW)
                transitionPairs.Add(Tuple.Create(f, phaseDistributionPerStartLocation[f.Key].CW));
            //(CCW Location to phase:)
            foreach (var f in stateLocToKey.CCW)
                transitionPairs.Add(Tuple.Create(f, phaseDistributionPerStartLocation[f.Key].CCW));

            foreach (var ftpair in transitionPairs)
            {
                var f = ftpair.Item1;
                double deadEndProb = 0;
                foreach (var t in ftpair.Item2)
                {
                    res.Add("T: wait : " + f.Value.ToString() + " : " + statePhaseToKey[t.Key].ToString() + " " + t.Value.ToString());
                    if (observationDistribution[t.Key].ContainsKey(gameParams.intrusionTrigger))
                    {
                        // if trigger is possible, transmitting leads to the dead end state. otherwise, continue normally since there was no transmission
                        deadEndProb += t.Value;
                    }
                    else // if no trigger is possible, act the same as wait
                        res.Add("T: transmit : " + f.Value.ToString() + " : " + statePhaseToKey[t.Key].ToString() + " " + t.Value.ToString());
                }
                if(deadEndProb>0)
                    res.Add("T: transmit : " + f.Value.ToString() + " : " + DEAD_END_STATE.ToString() + " " + deadEndProb.ToString());
            }

            //(phase to location:)
            foreach (var f in statePhaseToKey)
                foreach (var t in startLocationDistributionPerPhase[f.Key])
                    if (f.Key.dir == CW) // (from CW to CCW:)
                    {
                        res.Add("T: wait : " + f.Value.ToString() + " : " + stateLocToKey.CCW[t.Key] + " " + t.Value.ToString());
                        res.Add("T: transmit : " + f.Value.ToString() + " : " + stateLocToKey.CCW[t.Key] + " " + t.Value.ToString()); // transmitting does nothig
                    }
                    else // (from CCW to CW:)
                    {
                        res.Add("T: wait : " + f.Value.ToString() + " : " + stateLocToKey.CW[t.Key] + " " + t.Value.ToString());
                        res.Add("T: transmit : " + f.Value.ToString() + " : " + stateLocToKey.CW[t.Key] + " " + t.Value.ToString());// transmitting does nothig
                    }

            
            // set observations:
            foreach (var dest in observationDistribution)
                foreach(var o in dest.Value)
                    res.Add("O : wait : " + statePhaseToKey[dest.Key].ToString() + " : " + observationToKey[o.Key].ToString() + " " + o.Value.ToString());
            // (after transmission, observations are irrelevant:)
            res.Add("O : transmit : * : " + observationToKey[Observation.NO_OBSERVATION].ToString() + " 1.0");
            // (all other states give no observation:)
            res.Add("O : wait : " + DEAD_END_STATE.ToString() + " : " + observationToKey[Observation.NO_OBSERVATION].ToString() + " 1.0");
            foreach(var s in stateLocToKey.CW.Union(stateLocToKey.CCW))
                res.Add("O : wait : " + s.Value.ToString() + " : " + observationToKey[Observation.NO_OBSERVATION].ToString() + " 1.0");

            // set reward
            //res.Add("R : wait : * : * : * 0.0"); // waiting gives nothing
            //res.Add("R : transmit : * : * : * 0.0"); // transmitting in wrong times gives nothing
            foreach (var s in stateLocToKey.CW) // if intruding while patrollers are about to go CW
                if(intrusionSuccessProbPerInitialPbotLocation[CW].ContainsKey(s.Key))
                    res.Add("R : transmit : " + s.Value.ToString() + " : " + DEAD_END_STATE.ToString() + " : * " +  intrusionSuccessProbPerInitialPbotLocation[CW][s.Key].ToString());   
            
            foreach (var s in stateLocToKey.CCW) // if intruding while patrollers go CCW
                if (intrusionSuccessProbPerInitialPbotLocation[CCW].ContainsKey(s.Key))
                    res.Add("R : transmit : " + s.Value.ToString() + " : " + DEAD_END_STATE.ToString() + " : * " + intrusionSuccessProbPerInitialPbotLocation[CCW][s.Key].ToString());


            return res;
        }

        #region distribution generators
        // FIXME: functions in this region are very inefficient, and instead of counting states and observations, we generate them
        // one by one using loops. this could even exceed the maximal runtime we want, so consider fixing this



        private bool isInPath(int startSeg, int endSeg, int val, int Dir)
        {
            if(Dir == CW)
            {
                if (endSeg >= startSeg)
                    return val <= endSeg && val >= startSeg;
                return val <= endSeg || val >= startSeg;
            }
            else
            {
                return isInPath(endSeg, startSeg, val, CW);
            }

            
        }

        /// <summary>
        /// distribution of phases, assuming 'PbotLocationsAtPhaseStart' is the initial state at progress=0,
        /// and the location of each patroller is chosen at uniformly out of all the legal options
        /// </summary>
        /// <param name="PbotLocationsAtPhaseStart"></param>
        /// <param name="Dir"></param>
        /// <returns></returns>
        private Dictionary<ObservablePhase,double> getPossiblePhases(LocationSet PbotLocationsAtPhaseStart, int Dir)
        {
            Dictionary<ObservablePhase, double> resDictionary = new Dictionary<ObservablePhase, double>();
            double totalDestinationsCount = 0;

            //int p1Min = makeSeg(PbotLocationsAtPhaseStart.p1);
            //int p1Max = makeSeg(PbotLocationsAtPhaseStart.p1 + Dir * gameParams.PhaseLength);
            //if (p1Min > p1Max)
            //    AlgorithmUtils.Swap(ref p1Min, ref p1Max);

            //int p2Min = makeSeg(PbotLocationsAtPhaseStart.p2);
            //int p2Max = makeSeg(PbotLocationsAtPhaseStart.p2 + Dir * gameParams.PhaseLength);
            //if (p2Min > p2Max)
            //    AlgorithmUtils.Swap(ref p2Min, ref p2Max);

            //for (int destP1 = p1Min; destP1 <= p1Max; ++destP1)
            //  for (int destP2 = p2Min; destP2 <= p2Max; ++destP2)
            for (int destP1 = PbotLocationsAtPhaseStart.p1; makeSeg(destP1 - Dir) != PbotLocationsAtPhaseStart.p2; destP1 += Dir)
                for (int destP2 = PbotLocationsAtPhaseStart.p2; makeSeg(destP2 - Dir) != PbotLocationsAtPhaseStart.p1; destP2 += Dir)
                {
                    if(MathEx.minModDist(gameParams.segCount, makeSeg(destP1), makeSeg(destP2)) <= 2 * gameParams.intrusionTimeLength)
                        continue;  // patrollers must always be at distance >= 2T+1 from each other

                    

                    ObservablePhase obs = new ObservablePhase();
                    obs.dir = Dir;
                    obs.didUnobservedReachObserver = false;

                    bool p1Observed, p2Observed;

                    p1Observed = isInPath(PbotLocationsAtPhaseStart.p1, makeSeg(destP1), gameParams.observerSeg, Dir);
                    p2Observed = isInPath(PbotLocationsAtPhaseStart.p2, makeSeg(destP2), gameParams.observerSeg, Dir);
                    
                    if (!p1Observed && !p2Observed)
                    {
                        obs.observedPatroller = 0;
                        obs.observedPstart = PbotLocationsAtPhaseStart.p1;
                        obs.observedPEnd = makeSeg(destP1);
                        obs.unobservedPStart = PbotLocationsAtPhaseStart.p2;
                        resDictionary.addIfExists(obs, 1.0, 1.0);
                        ++totalDestinationsCount;

                        // fixme: populating locationDistributionperPhase here is a quite dirty
                        if (!locationDistributionperPhase.ContainsKey(obs))
                            locationDistributionperPhase[obs] = new List<LocationSet>();
                        locationDistributionperPhase[obs].Add(new LocationSet() { p1 = makeSeg(destP1), p2 = makeSeg(destP2)});

                        continue;
                    }
                    if (p1Observed && p2Observed)
                    {
                        // occurs if observer sees the pbots swap places at the begining and end 
                        obs.didUnobservedReachObserver = true;
                        //if (makeSeg(destP1) == PbotLocationsAtPhaseStart.p2)
                        //    obs.observedPatroller = 2; // we refer the first observed observer
                        //else
                        //    obs.observedPatroller = 1;
                        if (PbotLocationsAtPhaseStart.p1 == gameParams.observerSeg)
                            obs.observedPatroller = 1; // we refer the first observed observer
                        else
                            obs.observedPatroller = 2;
                    }
                    else if (p1Observed)
                        obs.observedPatroller = 1;
                    else
                        obs.observedPatroller = 2;

                    if (obs.observedPatroller == 1)
                    {
                        obs.observedPstart = PbotLocationsAtPhaseStart.p1;
                        obs.observedPEnd = makeSeg(destP1);
                        obs.unobservedPStart = PbotLocationsAtPhaseStart.p2;
                    }
                    else
                    {
                        obs.observedPstart = PbotLocationsAtPhaseStart.p2;
                        obs.observedPEnd = makeSeg(destP2);
                        obs.unobservedPStart = PbotLocationsAtPhaseStart.p1;
                    }
                    ++totalDestinationsCount;
                    resDictionary.addIfExists(obs, 1.0, 1.0);

                    // fixme: populating locationDistributionperPhase here is a quite dirty
                    if (!locationDistributionperPhase.ContainsKey(obs))
                        locationDistributionperPhase[obs] = new List<LocationSet>();
                    locationDistributionperPhase[obs].Add(new LocationSet() { p1 = makeSeg(destP1), p2 = makeSeg(destP2) });
                }

            // divide instance count with total instances, to get distribution
            var res = new Dictionary<ObservablePhase, double>();
            foreach(var v in resDictionary)
                res[v.Key] = v.Value / totalDestinationsCount;
            return res;
        }
        private Dictionary<Observation, double> getPossibleObservations(ObservablePhase phase, out double intrusionSuccessProbIftriggered)
        {
            var triggeringPaths = new HashSet<DelayedPath>(); // paths which create the observation the observer waits for
            var resDictionary = new Dictionary<Observation, double>();
            double totalObservationsCount = 0;


            if (phase.didUnobservedReachObserver)
            {
                getPossibleObservationsKnownDestinations(phase, phase.observedPstart, resDictionary, triggeringPaths, ref totalObservationsCount);
            }
            else
            {
                for(int unobservedSegDest = makeSeg(phase.unobservedPStart + phase.dir); makeSeg(unobservedSegDest) != phase.observedPstart; unobservedSegDest += phase.dir)
                    getPossibleObservationsKnownDestinations(phase, unobservedSegDest, resDictionary, triggeringPaths, ref totalObservationsCount);
            }

            intrusionSuccessProbIftriggered = calcIntrusionSuccessProbIftriggered(triggeringPaths);


            // divide instance count with total instances, to get distribution

            //var res =  resDictionary.ToList();
            //for (int i = 0; i < res.Count; ++i)
            //    res[i] = new KeyValuePair<Observation, double>(res[i].Key, res[i].Value / totalObservationsCount);
            //return res;
            var resDictionaryNorm = new Dictionary<Observation, double>();
            foreach (var r in resDictionary)
                resDictionaryNorm[r.Key] = r.Value / totalObservationsCount;
            return resDictionaryNorm;
        }

        private Dictionary<ObservablePhase,List<LocationSet>> locationDistributionperPhase;
        //private List<KeyValuePair<LocationSet, double>> getLocationDistribution(ObservablePhase phase)
        //{
        //    Dictionary<LocationSet, double> resDictionary = new Dictionary<LocationSet, double>();
        //    double totalObservationsCount = 0;

        //    // observed patroller's end location is already set, but the other patroller's isn't (and it's distributed uniformly amoung all options):

        //    if(phase.didUnobservedReachObserver)
        //    {
        //        // only one specific option
        //        resDictionary.addIfExists(makeLocationSetDest(phase.unobservedPStart,phase),1.0,1.0);
        //        ++totalObservationsCount;
        //    }
        //    else if (phase.observedPatroller == 0)
        //    {
        //        // unobserved can go all the way to where observer stands (Excluding last segment)
        //        for (int segStart = phase.unobservedPStart; makeSeg(segStart) != gameParams.observerSeg; segStart += phase.dir)
        //        {
        //            resDictionary.addIfExists(makeLocationSetDest(segStart, phase), 1.0, 1.0);
        //            ++totalObservationsCount;
        //        }
        //    }
        //    else
        //    {
        //        // unobserved can go all the way to where observed patroller started (Excluding last segment)
        //        for (int segStart = phase.unobservedPStart; makeSeg(segStart) != phase.observedPstart; segStart += phase.dir)
        //        {
        //            resDictionary.addIfExists(makeLocationSetDest(makeSeg(segStart), phase), 1.0, 1.0);
        //            ++totalObservationsCount;
        //        }
        //    }


        //    // divide instance count with total instances, to get distribution
        //    var res = resDictionary.ToList();
        //    for (int i = 0; i < res.Count; ++i)
        //        res[i] = new KeyValuePair<LocationSet, double>(res[i].Key, res[i].Value / totalObservationsCount);
        //    return res;
        //}
        #endregion
        #region utility functions for more compact code 
        private struct DelayedPath : IEquatable<DelayedPath>
        {
            public ObservablePhase phase;
            public int dir;
            public int unobservedDest;
            public int observedFirstDelaySeg, observedFirstDelay, unobservedFirstDelaySeg, unobservedFirstDelay;

            public bool Equals(DelayedPath other)
            {
                return phase.Equals(other.phase) &&
                    dir == other.dir &&
                    unobservedDest == other.unobservedDest &&
                    observedFirstDelaySeg == other.observedFirstDelaySeg &&
                    observedFirstDelay == other.observedFirstDelay &&
                    unobservedFirstDelaySeg == other.unobservedFirstDelaySeg &&
                    unobservedFirstDelay == other.unobservedFirstDelay;
            }
        }

        private HashSet<DelayedPath> getPaths(ObservablePhase Phase, int UnobservedDest)
        {

            var res = new HashSet<DelayedPath>();

            //int minDelaySegment = Phase.observedPstart;
            // if there will be a swap, we can't let the patroller delay in place at all - otherwise, when the phase ends, the ebots may
            // deduce with certainty where it couldn't reach:
            //if (Phase.didUnobservedReachObserver)
              //  minDelaySegment = makeSeg(Phase.observedPstart + Phase.dir);

            //int distToObserverSeg = segDist(Phase.observedPstart, gameParams.observerSeg, Phase);
            double pathDist = segDist(Phase.observedPstart, Phase.observedPEnd, Phase);
            double unobseredPathDist = segDist(Phase.unobservedPStart, UnobservedDest, Phase);

            double observedSegRatio = (pathDist) / gameParams.PhaseLength;
            double unobservedSegRatio = (unobseredPathDist) / gameParams.PhaseLength;
            
            for(int dist = 0; dist < gameParams.PhaseLength; ++dist)
            {
                int observedDelaySeg = makeSeg((int)Math.Round(Phase.observedPstart + dist * observedSegRatio * Phase.dir));
                int unobservedDelaySeg = makeSeg((int)Math.Round(Phase.unobservedPStart + dist * unobservedSegRatio * Phase.dir));

                if (pathDist < gameParams.delaySegDistance || unobseredPathDist < gameParams.delaySegDistance)
                {
                    // only 1 delay seg
                    int observedDelayLen = gameParams.PhaseLength - (int)pathDist;
                    int unobservedDelayLen = gameParams.PhaseLength - (int)unobseredPathDist;

                    res.Add(new DelayedPath() { dir = Phase.dir,
                                                phase = Phase,
                                                observedFirstDelay = observedDelayLen,
                                                observedFirstDelaySeg = observedDelaySeg,
                                                unobservedDest = UnobservedDest,
                                                unobservedFirstDelay = unobservedDelayLen,
                                                unobservedFirstDelaySeg = unobservedDelaySeg});
                }
                else
                {
                    // 2 delay segs
                    double maxObservedDelayLen = gameParams.PhaseLength - (int)pathDist;
                    double maxUnbservedDelayLen = gameParams.PhaseLength - (int)unobseredPathDist;
                    double observedDelayRatio = (maxObservedDelayLen) / gameParams.PhaseLength;
                    double unobservedDelayRatio = (maxUnbservedDelayLen) / gameParams.PhaseLength;

                    for (int delay = 0; delay < gameParams.PhaseLength; ++delay)
                    {
                        int observedDelayLen = (int)Math.Round(delay * observedDelayRatio);
                        int unobservedDelayLen = (int)Math.Round(delay * unobservedDelayRatio);

                        res.Add(new DelayedPath()
                        {
                            dir = Phase.dir,
                            phase = Phase,
                            observedFirstDelay = observedDelayLen,
                            observedFirstDelaySeg = observedDelaySeg,
                            unobservedDest = UnobservedDest,
                            unobservedFirstDelay = unobservedDelayLen,
                            unobservedFirstDelaySeg = unobservedDelaySeg
                        });
                    }
                }

            }
          
            return res;
        }

       
        private void getPossibleObservationsKnownDestinations(ObservablePhase phase, int unobservedPDestSeg,
            Dictionary<Observation, double> observationOccouranceCount,
            HashSet<DelayedPath> triggeringPaths,
            ref double totalObservationsCount)
        {
            //if (phase.observedPatroller == 0)
            //{
            //    resDictionary[Observation.NO_OBSERVATION] = 1;
            //    if (gameParams.intrusionTrigger == Observation.NO_OBSERVATION)
            //    {

            //        triggeringPaths.Add(new DelayedPath(phase, phase.observedPstart, FirstDelaySeg, firstDelay));
            //    }

            //    intrusionFailProbIftriggered = calcIntrusionFailProbIftriggered(triggeringPaths);
            //    return resDictionary.ToList();
            //}

            var allPaths = getPaths(phase, unobservedPDestSeg);

            if (phase.observedPatroller == 0)
            {
                observationOccouranceCount[Observation.NO_OBSERVATION] = 1;
                totalObservationsCount = 1;
                if(gameParams.intrusionTrigger == Observation.NO_OBSERVATION)
                    foreach (var path in allPaths)
                        triggeringPaths.Add(path); // all generated paths don't create an observation, so they all trigger
                return;
            }

            int minDelaySegment = phase.observedPstart;
            // if there will be a swap, we can't let the patroller delay in place at all - otherwise, when the phase ends, the ebots may
            // deduce with certainty where it couldn't reach:
            if (phase.didUnobservedReachObserver)
                minDelaySegment = makeSeg(phase.observedPstart + phase.dir);

            int distToObserverSeg = segDist(phase.observedPstart, gameParams.observerSeg, phase);
            int observedPathDist = segDist(phase.observedPstart, phase.observedPEnd, phase);
            int unobservedPathDist = segDist(phase.unobservedPStart, unobservedPDestSeg, phase);
            
            foreach (var path in allPaths)
            {
                Observation generatedObservation;
                // check if only 1 delay seg or two:
                if (observedPathDist < gameParams.delaySegDistance || unobservedPathDist < gameParams.delaySegDistance)
                    generatedObservation = addSingleDelaySegObservation(distToObserverSeg, phase, observationOccouranceCount, observedPathDist, path.observedFirstDelaySeg);
                else
                {
                    int seg1DelayOnlyDist = // if first delay seg is with allDelayDist < distance < seg1DelayOnlyDist, then only first segment's delay will be observed
                        segDist(phase.observedPstart, gameParams.observerSeg, phase);
                    int allDelayDist = // if first delay seg is with distance < allDelayDist, then all delay comes before we reach observer
                        seg1DelayOnlyDist - gameParams.delaySegDistance;
                    int dist1 = segDist(phase.observedPstart, path.observedFirstDelaySeg, phase); // distance to first delay seg
                    int totalDelay = gameParams.PhaseLength - observedPathDist;
                    generatedObservation = 
                        addTwoDelaySegsObservation(phase, 
                                                   observationOccouranceCount, 
                                                   distToObserverSeg, 
                                                   seg1DelayOnlyDist,
                                                   allDelayDist,
                                                   dist1,
                                                   totalDelay,
                                                   path.observedFirstDelay);
                }

                ++totalObservationsCount;
                if(generatedObservation == gameParams.intrusionTrigger)
                    triggeringPaths.Add(path);                
            }
            
            

            //if (segDist(phase.observedPstart, phase.observedPEnd, phase) < gameParams.delaySegDistance)
            //{
            //    // we have only one delay segment if movement is too small.

            //    // every segment in the path (including destination segment) may be a "delay segment" where pbot delays
            //    for (int delaySeg = minDelaySegment; makeSeg(delaySeg - phase.dir) != phase.observedPEnd; delaySeg += phase.dir)
            //    {
            //        if (phase.observedPatroller != 0)
            //        {
            //            addSingleDelaySegObservation(distToObserverSeg, phase, observationOccouranceCount, observedPathDist, delaySeg);
            //            ++totalObservationsCount;
            //        }
            //        else
            //            triggeringPathsOccouranceCount[new DelayedPath(this,phase, unobservedPDestSeg,)]
            //    }
            //}
            //else
            //{
            //    // if we have two delay segments.

            //    int seg1DelayOnlyDist = // if first delay seg is with allDelayDist < distance < seg1DelayOnlyDist, then only first segment's delay will be observed
            //        segDist(phase.observedPstart, gameParams.observerSeg, phase);
            //    int allDelayDist = // if first delay seg is with distance < allDelayDist, then all delay comes before we reach observer
            //        seg1DelayOnlyDist - gameParams.delaySegDistance;

            //    for (int delaySeg1 = minDelaySegment;
            //         makeSeg(delaySeg1) != makeSeg(phase.observedPEnd - gameParams.delaySegDistance * phase.dir);
            //         delaySeg1 += phase.dir)
            //    {
            //        int dist1 = segDist(phase.observedPstart, delaySeg1, phase); // distance to first delay seg
            //        int totalDelay = gameParams.PhaseLength - observedPathDist;

            //        for (int delayLen1 = 0; delayLen1 <= gameParams.PhaseLength - observedPathDist; ++delayLen1)
            //        {
            //            addTwoDelaySegsObservation(phase, observationOccouranceCount, distToObserverSeg, seg1DelayOnlyDist, allDelayDist, dist1, totalDelay, delayLen1);
            //            ++totalObservationsCount;
            //        }
            //    }
            //}
        }
        private double calcIntrusionSuccessProbIftriggered(HashSet<DelayedPath> triggeringPaths)
        {
            if (triggeringPaths.Count == 0)
                return double.NaN; // if no trigger, this value won't be counted anyway

            double prob = 0;
            //double totalOccourances = 0;
            foreach (var d in triggeringPaths)
            {
                prob += intrusionSuccess(d);
            }
            return prob / triggeringPaths.Count;
        }
        /// <summary>
        /// returns 1 or 0 - tells whether the patrollers are near the intruder 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="dir"></param>
        /// <param name="firstDelaySeg"></param>
        /// <param name="firstDelay"></param>
        /// <returns></returns>
        private int intrusionSuccess(DelayedPath p)
        {
            var ps = patrollersAtProgress(p);
            return  (MathEx.minModDist(gameParams.segCount, ps.X, gameParams.intruderSeg) <= gameParams.intrusionTimeLength ||
                     MathEx.minModDist(gameParams.segCount, ps.Y, gameParams.intruderSeg) <= gameParams.intrusionTimeLength)?(0):(1);
        }

        /// <summary>
        /// calculates the location of both patrollers at time 'gameParams.progressAtIntrusion'
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Point patrollersAtProgress(DelayedPath p)
        {
            int[] PathDist = new int[2]{ segDist(p.phase.observedPstart, p.phase.observedPEnd, p.phase),
                                         segDist(p.phase.unobservedPStart, p.unobservedDest, p.phase) };

            int[] DelaySeg1 = new int[2] { p.observedFirstDelaySeg, p.unobservedFirstDelaySeg };

            int[] DelaySeg2 = new int[2] { DelaySeg1[0] + gameParams.delaySegDistance, DelaySeg1[1] + gameParams.delaySegDistance };

            int[] DelaySeg1Dist = new int[2] { segDist(p.phase.observedPstart, p.observedFirstDelaySeg, p.phase),
                                               segDist(p.phase.unobservedPStart, p.unobservedFirstDelaySeg, p.phase)};
            int[] DelaySeg2Dist = new int[2] { gameParams.PhaseLength + 1, gameParams.PhaseLength + 1 };

            int[] firstDelay = new int[2] { p.observedFirstDelay, p.unobservedFirstDelay };

            int[] secondDelay = new int[2] { gameParams.PhaseLength - PathDist[0] - firstDelay[0],
                                             gameParams.PhaseLength - PathDist[1] - firstDelay[1]};

            int[] pStart = new int[2] { p.phase.observedPstart, p.phase.unobservedPStart };

            
            //int observedPathDist = segDist(p.phase.observedPstart, p.phase.observedPEnd, p.phase);
            //int observedDelaySeg1Dist = segDist(p.phase.observedPstart, p.observedFirstDelaySeg, p.phase);
            //int observedDelaySeg2Dist = gameParams.PhaseLength + 1; // assume we have only 1 delay seg

            //int unobseredPathDist = segDist(p.phase.unobservedPStart, p.unobservedDest, p.phase);
            //int unobservedDelaySeg1Dist = segDist(p.phase.unobservedPStart, p.unobservedFirstDelaySeg, p.phase);
            //int unobservedDelaySeg2Dist = gameParams.PhaseLength + 1; // assume we have only 1 delay seg


            //if (observedPathDist >= gameParams.delaySegDistance && unobseredPathDist >= gameParams.delaySegDistance)
            if (PathDist[0] >= gameParams.delaySegDistance && PathDist[1] >= gameParams.delaySegDistance)
            {
                // 2 delay segs
                DelaySeg2Dist[0] = DelaySeg1Dist[0] + gameParams.delaySegDistance;
                DelaySeg2Dist[1] = DelaySeg1Dist[1] + gameParams.delaySegDistance;
                //observedDelaySeg2Dist = observedDelaySeg1Dist + gameParams.delaySegDistance;
                //unobservedDelaySeg2Dist = unobservedDelaySeg1Dist + gameParams.delaySegDistance;
            }

            int[] res = new int[2];
            // calculate the location of both patrollers at time 'gameParams.progressAtIntrusion':
            for (int i = 0; i <= 1; ++i)
            {
                if (gameParams.progressAtIntrusion < DelaySeg1Dist[i])
                    res[i] = makeSeg(pStart[i] + p.phase.dir * gameParams.progressAtIntrusion);
                else if (gameParams.progressAtIntrusion < DelaySeg1Dist[i] + firstDelay[i])
                    res[i]  = DelaySeg1[i];
                else if (gameParams.progressAtIntrusion < DelaySeg2Dist[i] + firstDelay[i])
                    res[i] = makeSeg(pStart[i] + p.phase.dir * (gameParams.progressAtIntrusion - firstDelay[i]));
                else if (gameParams.progressAtIntrusion < DelaySeg2Dist[i] + firstDelay[i] + secondDelay[i])
                    res[i] = DelaySeg2[i];
                else
                    res[i] = makeSeg(pStart[i] + p.phase.dir * (gameParams.progressAtIntrusion - firstDelay[i] - secondDelay[i]));
            }

            return new Point(res[0], res[1]);

            //int unobservedLoc;
            //if (gameParams.progressAtIntrusion < unobseredPathDist)
            //    unobservedLoc = makeSeg(p.phase.unobservedPStart + p.phase.dir * gameParams.progressAtIntrusion);
            //else if (gameParams.progressAtIntrusion < unobseredPathDist + p.unobservedFirstDelay)
            //    unobservedLoc = p.unobservedFirstDelaySeg;
            //else if (gameParams.progressAtIntrusion < unobservedDelaySeg2Dist + p.unobservedFirstDelay)
            //    unobservedLoc = makeSeg(p.phase.unobservedPStart + p.phase.dir * (gameParams.progressAtIntrusion- p.unobservedFirstDelay));

            //int dummy;

            //PatrollersHMM.patrollersAtProgress(
            //    gameParams.segCount,
            //    gameParams.PhaseLength,
            //    Progress,
            //    gameParams.delaySegDistance,
            //    from.p1, from.p2,
            //    to.p1, to.p2,
            //    dir, firstDelaySeg, firstDelay, out res, out dummy, out dummy, out dummy, out dummy, out dummy, out dummy);


        }

        private LocationSet makeLocationSetDest(int unobservedPatrollerSeg, ObservablePhase p)
        {
            if(p.observedPatroller == 2)
                return new LocationSet() { p1 = makeSeg(unobservedPatrollerSeg), p2 = p.observedPEnd };
            
            // p.observedPatroller is 1 or 0
            return new LocationSet() { p2 = makeSeg(unobservedPatrollerSeg), p1 = p.observedPEnd };            
        }

        private static LocationSet makeLocationSetInit(ObservablePhase p)
        {
            if (p.observedPatroller == 2)
                return new LocationSet() { p1 = p.unobservedPStart, p2 = p.observedPstart };
            
            // p.observedPatroller is 1 or 0
            return new LocationSet() { p2 = p.unobservedPStart, p1 = p.observedPstart };
        }
        private int segDist(int segFrom, int segTo, ObservablePhase phase)
        {
            return MathEx.modDist(gameParams.segCount, makeSeg(segFrom), makeSeg(segTo), phase.dir);
        }
        private int makeSeg(int segVal)
        {
            return (segVal + gameParams.segCount) % gameParams.segCount;
        }

        /// <summary>
        /// utility function: add to resDictionary the observation derived from given parameters
        /// </summary>
        private static Observation addTwoDelaySegsObservation(
            ObservablePhase phase, 
            Dictionary<Observation, double> resDictionary, int distToObserverSeg, int seg1DelayOnlyDist, int allDelayDist, int dist1, int totalDelay, int delayLen1)
        {
            Observation resObs;
            int delayLen2 = totalDelay - delayLen1;

            if (dist1 < allDelayDist) // both delays happen before observation:
            {
                resObs = new Observation(distToObserverSeg + totalDelay, 0, phase.didUnobservedReachObserver);
            }
            else if (dist1 == allDelayDist) // second delay happens on observer
            {
                resObs = new Observation(distToObserverSeg + delayLen1, delayLen2, phase.didUnobservedReachObserver);
            }
            else if (dist1 > seg1DelayOnlyDist) // first delay before observation, second after
            {
                resObs = new Observation(distToObserverSeg + delayLen1, 0, phase.didUnobservedReachObserver);
            }
            else if (dist1 == seg1DelayOnlyDist) // first delay on observer
            {
                resObs = new Observation(distToObserverSeg, delayLen1, phase.didUnobservedReachObserver);
            }
            else // if (dist1 < seg1DelayOnlyDist)
            {
                resObs = new Observation(distToObserverSeg, 0, phase.didUnobservedReachObserver);
            }
            resDictionary.addIfExists(resObs, 1.0, 1.0);

            return resObs;
        }

        /// <summary>
        /// utility function: add to resDictionary the observation derived from given parameters
        /// </summary>
        private Observation addSingleDelaySegObservation(
            int distToObserverSeg, 
            ObservablePhase phase, 
            Dictionary<Observation, double> resDictionary, 
            int maxDist, 
            int delaySeg)
        {
            Observation resObs;
            int distToDelaySeg = segDist(phase.observedPstart, delaySeg, phase);
            int delayLen = gameParams.PhaseLength - maxDist;
            if (distToDelaySeg == distToObserverSeg) // delay is on observer
            {
                resObs = new Observation(distToObserverSeg, delayLen, phase.didUnobservedReachObserver);
                resDictionary.addIfExists(resObs, 1.0, 1.0);
            }
            else if (distToDelaySeg > distToObserverSeg)
            {
                // delay comes after passing by the observer
                resObs = new Observation(distToObserverSeg, 0, phase.didUnobservedReachObserver);
                resDictionary.addIfExists(
                    resObs,
                    1.0, 1.0);
            }
            else
            {
                // delay comes before passing by the observer
                resObs = new Observation(distToObserverSeg + delayLen, 0, phase.didUnobservedReachObserver);
                resDictionary.addIfExists(
                    resObs,
                    1.0, 1.0);
            }
            return resObs;
        }

        #endregion
        #region utilities for generating pomdp file
        // all populaed in populatePOMDPStates():
        private Dictionary<int, LocationSet> keyToLocationSetCW = null;
        private Dictionary<int, LocationSet> keyToLocationSetCCW = null;
        private Dictionary<int, ObservablePhase> keyToStatePhase = null;
        private PerDir<Dictionary<LocationSet, int>> stateLocToKey = null;
        private Dictionary<ObservablePhase, int> statePhaseToKey = null;
        private Dictionary<LocationSet, PerDir<Dictionary<LocationSet, double>>> locationToLocationProb = null;
        private Dictionary<LocationSet, Dictionary<LocationSet, double>> locationToLocationCWCCWProb = null; // when patrollers go CW, then go CCW 
        private Dictionary<LocationSet, double> initialLocationDistribution;// assuming intruder starts when patrolling are about to go CW
        private int DEAD_END_STATE = 0;
        private int totalStatesCount;
        MatrixD locationToLocationCWToCCWProbMatrix;
        MatrixD locationToLocationCCWToCWProbMatrix;
        MatrixD locationToLocationCWToCWProbMatrix;
        Dictionary<LocationSet, int> locationToLocationIdxMapping;
        private SortedList<int, string> statekeysToString;
        private SortedList<int, string> observationkeysToString;
        private Dictionary<Observation,int> observationToKey;

        private static string makeLine<T>(SortedList<int, T> vals)
        {
            string res = "";
            foreach (var v in vals)
                res += v.Value.ToString() + " ";
            return res.Remove(res.Length - 1);
            
        }
        private static string makeLine(SortedList<int, string> vals)
        {
            string res = "";
            foreach (var v in vals)
                res += v.Value + " ";
            return res.Remove(res.Length - 1);
        }
        
        void testGetInitialLocationDistribution()
        {
            MatrixD mc = new MatrixD(2, 2);
            mc[0, 0] = 0.3;
            mc[1, 0] = 0.7;
            //mc[0, 0] = 1;

            mc[0, 1] = 0.1;
            mc[1, 1] = 0.9;
            //mc[2, 0] = 1;

            MatrixD mcI = mc - MatrixD.IdentityMatrix(2, 2);
            mcI.MakeLU();
            mcI = mcI.U;
            mcI[1, 0] = 1;
            mcI[1, 1] = 1;

            MatrixD B = new MatrixD(2, 1);
            B[0,0] = 0;
            B[1, 0] = 1;
            
            var stable = mcI.SolveWith(B);
            var stableo = mc * stable;
            return;

            MatrixD markovChain1 = new MatrixD(2, 2);
            MatrixD markovChain2 = new MatrixD(3, 3);
            MatrixD markovChain3 = new MatrixD(3, 2);
            MatrixD markovChain4 = new MatrixD(3, 3);
            // the matrix represnts the transitions in the example: http://www.sosmath.com/matrix/markov/markov.html
            markovChain1[0, 0] = markovChain2[0, 0] = markovChain3[0, 0] = 0.6;
            markovChain1[0, 1] = markovChain2[0, 1] = markovChain3[0, 1] = 0.4;
            markovChain1[1, 0] = markovChain2[1, 0] = markovChain3[1, 0] = 0.3;
            markovChain1[1, 1] = markovChain2[1, 1] = markovChain3[1, 1] = 0.7;

            markovChain2[2, 1] = markovChain2[2, 0] = 1; // in addition to other equeations, we want the sum of vars to be 1
            markovChain3[2, 1] = markovChain3[2, 0] = 1;

            markovChain2[0, 2] = markovChain2[1, 2] = markovChain2[2, 2] = 0;

            markovChain4[0, 0] = 0.6;
            markovChain4[0, 1] = 0.4;
            markovChain4[1, 0] = 0.3;
            markovChain4[1, 1] = 0.7;


            var bVec1 = new MatrixD(2, 1);
            var bVec2 = new MatrixD(3, 1);
            var bVec3 = new MatrixD(3, 1);
            var bVec4 = new MatrixD(3, 1);


            //var bVec1 = new SparseArray<int, double>();
            //var bVec2 = new SparseArray<int, double>();
            //var bVec3 = new SparseArray<int, double>();
            //var resVec = new SparseArray<int, double>();

            bVec1[0,0] = bVec1[1, 0] = 0;

            bVec2[0, 0] = bVec2[1, 0]  = 0;
            bVec2[2, 0] = 1;

            bVec3[0, 0] = bVec3[1, 0] = 0;
            bVec3[2, 0] = 1;

            bVec4[0, 0] = 0;
            bVec4[1, 0] = 0;
            bVec4[2, 0] = 1;
            


            MatrixD s1 = (MatrixD.Transpose(markovChain1 - MatrixD.IdentityMatrix(markovChain1.rows, markovChain1.cols)));
            MatrixD s2 = (MatrixD.Transpose(markovChain2 - MatrixD.IdentityMatrix(markovChain2.rows, markovChain2.cols)));
            MatrixD s3 = (MatrixD.Transpose(markovChain3 - MatrixD.IdentityMatrix(markovChain3.rows, markovChain3.cols)));
            MatrixD s4 = (MatrixD.Transpose(markovChain4 - MatrixD.IdentityMatrix(markovChain4.rows, markovChain4.cols)));
            s4[2, 0] = s4[2, 1] = 1;
            s4[2, 2] = 0;

            var res1 = s1.SolveWith(bVec1);
            var res2 = s2.SolveWith(bVec2);
            //var res3 = s3.SolveWith(bVec3);
            var res4 = s4.SolveWith(bVec4);

            var resMult = markovChain4 * res4;

            //DenseSparse2DMatrix<double> sparse1 = new DenseSparse2DMatrix<double>(MatrixD.Transpose(markovChain1- MatrixD.IdentityMatrix(markovChain1.rows, markovChain1.cols)));
            //DenseSparse2DMatrix<double> sparse2 = new DenseSparse2DMatrix<double>(MatrixD.Transpose(markovChain2 - MatrixD.IdentityMatrix(markovChain2.rows, markovChain2.cols)));
            //DenseSparse2DMatrix<double> sparse3 = new DenseSparse2DMatrix<double>(MatrixD.Transpose(markovChain3 - MatrixD.IdentityMatrix(markovChain3.rows, markovChain3.cols)));


            //var solveState1 = LinearEquationSolver.Solve(2, sparse1, bVec1, resVec);
            //var solveState2 = LinearEquationSolver.Solve(3, sparse2, bVec2, resVec);
            //var solveState3 = LinearEquationSolver.Solve(3, sparse3, bVec3, resVec);

        }

        /// <summary>
        /// assuming that will go CW from that initial location
        /// </summary>
        /// <returns></returns>
        private Dictionary<LocationSet, double> getInitialLocationDistribution()
        {
            Dictionary<LocationSet, double> res = new Dictionary<LocationSet, double>();
            locationToLocationCWToCCWProbMatrix = new MatrixD(phaseDistributionPerStartLocation.Keys.Count, phaseDistributionPerStartLocation.Keys.Count);
            locationToLocationCCWToCWProbMatrix = new MatrixD(phaseDistributionPerStartLocation.Keys.Count, phaseDistributionPerStartLocation.Keys.Count);
            locationToLocationCWToCWProbMatrix = new MatrixD(phaseDistributionPerStartLocation.Keys.Count, phaseDistributionPerStartLocation.Keys.Count);


            locationToLocationIdxMapping =
                new Dictionary<LocationSet, int>(); // not to be confused with pomdp keys - this is just for the matrices below
            Dictionary<int, LocationSet> tempNumLocation = new Dictionary<int, LocationSet>();
            foreach (var loc in locationToLocationProb.Keys)
            {
                locationToLocationIdxMapping[loc] = locationToLocationIdxMapping.Count;
                tempNumLocation[locationToLocationIdxMapping.Count - 1] = loc;
            }

            // populate CW To CCW:
            foreach (var from in locationToLocationProb)
                foreach (var to in from.Value[CW])
                    locationToLocationCWToCCWProbMatrix[locationToLocationIdxMapping[to.Key], locationToLocationIdxMapping[from.Key]] = to.Value;
            // populate CCW To CW:
            foreach (var from in locationToLocationProb)
                foreach (var to in from.Value[CCW])
                    locationToLocationCCWToCWProbMatrix[locationToLocationIdxMapping[to.Key], locationToLocationIdxMapping[from.Key]] = to.Value;

            locationToLocationCWToCWProbMatrix = locationToLocationCWToCCWProbMatrix * locationToLocationCCWToCWProbMatrix;

            // In order to find convergence, so we solve: XRow * Mat = XRow  <==> (Mat - I) * XCol = 0
            // + constraint that sum of all variables in XCol is 1

            MatrixD B = new MatrixD(phaseDistributionPerStartLocation.Keys.Count, 1);
            MatrixD locationToLocationCWToCWProbMatrixI = locationToLocationCWToCWProbMatrix - MatrixD.IdentityMatrix(phaseDistributionPerStartLocation.Keys.Count, phaseDistributionPerStartLocation.Keys.Count);
            locationToLocationCWToCWProbMatrixI.MakeLU(); // after diagonalization of matrix, we'll have one solution (all values are 0). instead, we override the last equation and demand the sum of all vals is 1.0
            locationToLocationCWToCWProbMatrixI = locationToLocationCWToCWProbMatrixI.U;
            for (int i = 0; i < phaseDistributionPerStartLocation.Keys.Count; ++i)
            {
                locationToLocationCWToCWProbMatrixI[phaseDistributionPerStartLocation.Keys.Count-1, i] = 1; // sum of all vals...
                B[i, 0] = 0;
            }
            B[phaseDistributionPerStartLocation.Keys.Count-1, 0] = 1; // ..equals 1.0
            var resVec = locationToLocationCWToCWProbMatrixI.SolveWith(B);

            //DenseSparse2DMatrix<double> sparseCWToCWMat = new DenseSparse2DMatrix<double>(tempLocationNum.Count,
            //                                                                              tempLocationNum.Count);
            //var bVec = new SparseArray<int, double>();
            //var resVec = new SparseArray<int, double>();

            //for (int i = 0; i < tempLocationNum.Count - 1; ++i)
            //{
            //    for (int j = 0; j < tempLocationNum.Count; ++j)
            //        sparseCWToCWMat[i, j] = locationToLocationCWToCWProbMatrix[j, i]; // transpose matrix i.e. columns sum up to 1. see http://www.sosmath.com/matrix/markov/markov.html
            //    sparseCWToCWMat[i, i] -= 1;
            //    bVec[i] = 0;
            //}
            //for (int j = 0; j < tempLocationNum.Count; ++j)
            //    sparseCWToCWMat[tempLocationNum.Count - 1, j] = 1;
            //bVec[tempLocationNum.Count-1] = 1;

            //var solveState = LinearEquationSolver.Solve(tempLocationNum.Count, sparseCWToCWMat, bVec, resVec);

#if DEBUG
            for (int colI = 0; colI < locationToLocationIdxMapping.Count; ++colI)
            {
                double sumCWCCW = 0;
                double sumCCWCW = 0;
                double sumCWCW = 0;
                for (int rowI = 0; rowI < locationToLocationIdxMapping.Count; ++rowI)

                {
                    sumCWCCW += locationToLocationCWToCCWProbMatrix[rowI, colI];
                    sumCCWCW += locationToLocationCCWToCWProbMatrix[rowI, colI];
                    sumCWCW += locationToLocationCWToCWProbMatrix[rowI, colI];
                }
                if(Math.Abs(sumCWCCW-1) > 0.0001 ||
                    Math.Abs(sumCCWCW - 1) > 0.0001 ||
                    Math.Abs(sumCWCW - 1) > 0.0001)
                {
                    throw new Exception("sanity test failed: transition matrices have columns that don't sum up to 1");
                }
            }

            //testGetInitialLocationDistribution();
            
            double sum = 0;
            for (int i = 0; i < phaseDistributionPerStartLocation.Keys.Count; ++i)
                sum += resVec[i,0];
            if (Math.Abs(sum - 1.0) > 0.0001)
                throw new Exception("Sanity test failed: init state distribution doesn't sum up to 1");

            var testVec = locationToLocationCWToCWProbMatrix * resVec;
            for(int i = 0; i < phaseDistributionPerStartLocation.Keys.Count; ++i)
                if(Math.Abs(testVec[i,0]-resVec[i,0])>0.0001)
                throw new Exception("Sanity test failed: init state distribution isn't stable");

             Dictionary<LocationSet, double> testLocations = new Dictionary<LocationSet, double>();
            SortedDictionary<int, double> distanceProb = new SortedDictionary<int, double>();
            for (int i = 0; i < phaseDistributionPerStartLocation.Keys.Count; ++i)
                testLocations[tempNumLocation[i]] = resVec[i, 0];
            foreach (var l in testLocations)
                if (l.Key.p1 == 0)
                    distanceProb[l.Key.p2] = l.Value;

           
#endif

            for (int i = 0; i < phaseDistributionPerStartLocation.Keys.Count; ++i)
                res[tempNumLocation[i]] = resVec[i,0];
            
            return res;
        }

        private void populatePOMDPStates()
        {
            if (keyToLocationSetCW != null)
                return; // already populated

            keyToLocationSetCW = new Dictionary<int, LocationSet>();
            keyToLocationSetCCW = new Dictionary<int, LocationSet>();
            keyToStatePhase = new Dictionary<int, ObservablePhase>();
            stateLocToKey = new PerDir<Dictionary<LocationSet, int>>();
            statePhaseToKey = new Dictionary<ObservablePhase, int>();
            locationToLocationProb = new Dictionary<LocationSet, PerDir<Dictionary<LocationSet, double>>>();
            //locationToLocationCCWProb = new Dictionary<LocationSet, Dictionary<LocationSet, double>>();
            statekeysToString = new SortedList<int, string>();


            totalStatesCount = DEAD_END_STATE;
            statekeysToString[DEAD_END_STATE] = "END";

            ++totalStatesCount; // count deat end state

            // map LocationSet + dir into numeric states
            foreach (var v in phaseDistributionPerStartLocation.Keys)
            {
                stateLocToKey[CW][v] = totalStatesCount;
                keyToLocationSetCW[totalStatesCount] = v;
                statekeysToString[totalStatesCount] = v.ToString(CW);
                ++totalStatesCount;

                stateLocToKey[CCW][v] = totalStatesCount;
                keyToLocationSetCCW[totalStatesCount] = v;
                statekeysToString[totalStatesCount] = v.ToString(CCW);
                ++totalStatesCount;
            }
            // map ObservablePhase into numeric states
            foreach (var v in startLocationDistributionPerPhase.Keys)
            {
                statePhaseToKey[v] = totalStatesCount;
                keyToStatePhase[totalStatesCount] = v;
                statekeysToString[totalStatesCount] = v.ToString();
                ++totalStatesCount;
            }

            observationToKey = new Dictionary<Observation, int>();
            observationToKey[Observation.NO_OBSERVATION] = 0; // this way, when generating HMM, a matrix initialized to 0 is initialized to no observations
            observationkeysToString = new SortedList<int, string>();
            foreach (var p in phaseDistributionPerStartLocation.Values)
                foreach (var op in p.CW.Keys.Union(p.CCW.Keys))
                    foreach (var o in observationDistribution[op])
                    {
                        if (observationToKey.ContainsKey(o.Key))
                            continue;
                        observationkeysToString[observationToKey.Count] = o.Key.ToString();
                        observationToKey[o.Key] = observationToKey.Count;
                    }


            // in order to find initial distribution, we assume the intruder starts when movement is CW.
            // Here we calculate movement probability from every LocationSet in clockwise direction to the next locations,
            // then from every LocationSet in ccw direction to the next locations
            foreach (var fromLoc in phaseDistributionPerStartLocation.Keys)
            {
                locationToLocationProb[fromLoc] = new PerDir<Dictionary<LocationSet, double>>();

                PerDir<List<LocationSet>> locations = new PerDir<List<LocationSet>>();
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    for (int targetP1 = fromLoc.p1; makeSeg(targetP1 - dir) != fromLoc.p2; targetP1 += dir)
                        for (int targetP2 = fromLoc.p2; makeSeg(targetP2 - dir) != fromLoc.p1; targetP2 += dir)
                        {
                            if (MathEx.minModDist(gameParams.segCount, makeSeg(targetP1), makeSeg(targetP2)) <= 2 * gameParams.intrusionTimeLength)
                                continue;
                            locations[dir].Add(new LocationSet() { p1 = makeSeg(targetP1), p2 = makeSeg(targetP2) });
                        }

                    foreach (var l in locations[dir])
                        locationToLocationProb[fromLoc][dir][l] = 1.0 / locations[dir].Count;
                }

            }



#if DEBUG
            Dictionary<LocationSet, Dictionary<LocationSet, double>> test = new Dictionary<LocationSet, Dictionary<LocationSet, double>>();
            foreach (var fromLoc in phaseDistributionPerStartLocation)
            {
                test[fromLoc.Key] = new Dictionary<LocationSet, double>();
                foreach (var p in fromLoc.Value[CW])
                {
                    foreach (var l in startLocationDistributionPerPhase[p.Key])
                        test[fromLoc.Key].addIfExists(l.Key, l.Value * p.Value, l.Value * p.Value);
                }
            }
            foreach (var from in locationToLocationProb)
                foreach (var to in from.Value[CW])
                    if (Math.Abs(test[from.Key][to.Key]-to.Value) > 0.001)
                        throw new Exception("sanity test fail: locationToLocationProb[CW] probability inconsistent");
#endif

            initialLocationDistribution = getInitialLocationDistribution();
        }

        #endregion
        #region tests
        private void testphaseDistributionPerStartLocation1(List<bool> testSuccess)
        {
            var testedStartLocation1 = new LocationSet()
            {
                p1 = gameParams.observerSeg,
                p2 = makeSeg(gameParams.observerSeg + 2 * gameParams.intrusionTimeLength + 1)
            };

            var dist1 = phaseDistributionPerStartLocation[testedStartLocation1][CW];

            var expectedPhase1 = new ObservablePhase()
            {
                didUnobservedReachObserver = true,
                dir = 1,
                observedPatroller = 1,
                observedPEnd = makeSeg(gameParams.observerSeg + 2 * gameParams.intrusionTimeLength + 1),
                observedPstart = testedStartLocation1.p1,
                unobservedPStart = testedStartLocation1.p2
            };
            var expectedPhase2 = new ObservablePhase()
            {
                didUnobservedReachObserver = false,
                dir = 1,
                observedPatroller = 1,
                observedPEnd = makeSeg(gameParams.observerSeg + 2 * gameParams.intrusionTimeLength + 1),
                observedPstart = testedStartLocation1.p1,
                unobservedPStart = testedStartLocation1.p2
            };

            testSuccess.Add(dist1.ContainsKey(expectedPhase1));
            testSuccess.Add(dist1.ContainsKey(expectedPhase2));
            testSuccess.Add(dist1[expectedPhase1] < dist1[expectedPhase2]);
        }
        private void testphaseDistributionPerStartLocation2(List<bool> testSuccess)
        {
            var testedStartLocation1 = new LocationSet()
            {
                p2 = makeSeg(gameParams.observerSeg - 1),
                p1 = makeSeg(gameParams.observerSeg - 1 + 2 * gameParams.intrusionTimeLength + 1)
            };

            var dist1 = phaseDistributionPerStartLocation[testedStartLocation1][CW];
            var dist2 = phaseDistributionPerStartLocation[testedStartLocation1][CCW];

            var expectedPhase1 = new ObservablePhase()
            {
                didUnobservedReachObserver = false,
                dir = 1,
                observedPatroller = 2,
                observedPEnd = gameParams.observerSeg,
                observedPstart = testedStartLocation1.p2,
                unobservedPStart = testedStartLocation1.p1
            };
            var expectedPhase2 = new ObservablePhase()
            {
                didUnobservedReachObserver = false,
                dir = 1,
                observedPatroller = 2,
                observedPEnd = makeSeg(gameParams.observerSeg + 1),
                observedPstart = testedStartLocation1.p2,
                unobservedPStart = testedStartLocation1.p1
            };
            var expectedPhase3 = new ObservablePhase()
            {
                didUnobservedReachObserver = false,
                dir = -1,
                observedPatroller = 1,
                observedPEnd = makeSeg(gameParams.observerSeg - 1),
                observedPstart = testedStartLocation1.p1,
                unobservedPStart = testedStartLocation1.p2
            };

            testSuccess.Add(dist1.ContainsKey(expectedPhase1));
            testSuccess.Add(dist1.ContainsKey(expectedPhase2));
            testSuccess.Add(dist2.ContainsKey(expectedPhase3));
        }
        private void testphaseDistributionPerStartLocation()
        {
            List<bool> testSuccess = new List<bool>();
            List<ObservablePhase> expectedPhases = new List<ObservablePhase>();
            testphaseDistributionPerStartLocation1(testSuccess);
            testphaseDistributionPerStartLocation2(testSuccess);
            foreach (var f in testSuccess)
                if (f == false)
                    throw new Exception("testphaseDistributionPerStartLocation(): one of the tests failed");
        }
        private void testobservationDistribution1(List<bool> testSuccess)
        {
            var phase1 = new ObservablePhase()
            {
                didUnobservedReachObserver = false,
                dir = 1,
                observedPatroller = 2,
                observedPEnd = makeSeg(gameParams.observerSeg + 1),
                observedPstart = makeSeg(gameParams.observerSeg - 2),
                unobservedPStart = makeSeg(gameParams.observerSeg - 2 + 2 * gameParams.intrusionTimeLength + 1)
            };
            List<Observation> observations = observationDistribution[phase1].Keys.ToList();
            Observation obs1 = new Observation(2, 0, false);
            Observation obs2 = new Observation(2, 1, false);
            Observation obs3 = new Observation(3, 0, false);
            testSuccess.Add(observations.Contains(obs1));
            testSuccess.Add(observations.Contains(obs2));
            testSuccess.Add(observations.Contains(obs3));
        }
        private void testobservationDistribution2(List<bool> testSuccess)
        {
            var phase1 = new ObservablePhase()
            {
                didUnobservedReachObserver = true,
                dir = 1,
                observedPatroller = 2,
                observedPEnd = makeSeg(gameParams.observerSeg + 2 * gameParams.intrusionTimeLength + 1),
                observedPstart = gameParams.observerSeg,
                unobservedPStart = makeSeg(gameParams.observerSeg - 2 * gameParams.intrusionTimeLength - 1)
            };
            List<Observation> observations = observationDistribution[phase1].Keys.ToList();
            Observation obs1 = new Observation(0, 0, true);
            Observation obs2 = new Observation(0, 1, true);

            testSuccess.Add(observations.Contains(obs1));
            testSuccess.Add(observations.Contains(obs2));

        }
        private void testobservationDistribution()
        {
            List<bool> testSuccess = new List<bool>();
            testobservationDistribution1(testSuccess);
            testobservationDistribution2(testSuccess);

            foreach (var f in testSuccess)
                if (f == false)
                    throw new Exception("testobservationDistribution(): one of the tests failed");
        }
        private void testcalcIntrusionFailProbIftriggered()
        {
            //List<bool> testSuccess = new List<bool>();

            //var p1 = new HashSet<DelayedPath>();
            //var p2 = new HashSet<DelayedPath>();
            //p1.Add(new DelayedPath() {dir = 1,observedFirstDelay = 0,phase })


            //testSuccess.Add(Math.Abs(1 - calcIntrusionFailProbIftriggered(p1)) < 0.001);
            //testSuccess.Add(Math.Abs(0 - calcIntrusionFailProbIftriggered(p2)) < 0.001);
            //foreach (var f in testSuccess)
            //    if (f == false)
            //        throw new Exception("testobservationDistribution(): one of the tests failed");
        }
        #endregion
        
        // currently unused:
        private int getDestinationCombinationCount(LocationSet startPos)
        {
            //int canBothSwap = 0;
            //if (startPos.p1 == gameParams.observerSeg ||
            //    startPos.p2 == gameParams.observerSeg)
            //    canBothSwap = 1; // there is one combination in which both patrollers are visible

            // assumes dir = 1, but it's the same value for dir = -1
            return 1 + // both patrollers swap
                MathEx.modDist(gameParams.segCount, makeSeg(startPos.p1 + 1), makeSeg(startPos.p2 - 1)) + // p2 will reach startPos.p1 (and swaps it), p1 moves but doesn't swap
                MathEx.modDist(gameParams.segCount, makeSeg(startPos.p2 + 1), makeSeg(startPos.p1 - 1)) + // p1 will reach startPos.p2 (and swaps it), p2 moves but doesn't swap
                MathEx.modDist(gameParams.segCount, startPos.p1, makeSeg(startPos.p2 - 1)) *
                MathEx.modDist(gameParams.segCount, startPos.p2, makeSeg(startPos.p1 - 1));  // both move with no swap

        }

    }
}