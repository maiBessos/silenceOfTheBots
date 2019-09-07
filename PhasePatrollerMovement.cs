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

/// <summary>
/// in this policy, the two patroller go both either clockwise, or counter clockwise.
/// they switch direction once per N-2T+1 steps ( every N-2T+1 steps are called a "phase").
/// in the begining of each phase, the policy chooses 2 destination points for the patrollers,
/// (such that both patrollers can go to cc/ccw without colliding)
/// and each patroller goes cw/ccw towards it's new destination.
/// </summary>
namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly.PhasePatrollerMovement
{
    public struct Observation : IEquatable<Observation>
    {
        public static Observation UNINITIALIZED { get { return new Observation(-2, -2, false); } }
        public static Observation NO_OBSERVATION { get { return new Observation(-1,-1, false); } }
        public Observation(int time, int observedDelay, bool didObserveBothPatrollers)
        {
            this.Time = time;
            this.ObservedDelay = observedDelay;
            this.DidObserveBothPatrollers = didObserveBothPatrollers;
        }
        public int Time; // how many game steps passed since observation started
        public int ObservedDelay; // for how long the patroller delayed on that segment (0 means passing by with no delay)
        public bool DidObserveBothPatrollers; // if true, the observer observed a patroller in progress 0, and the other patroller at final progress 

        public static bool operator ==(Observation lhs, Observation rhs)
        {
            return lhs.Equals(rhs);
        }
        public static bool operator !=(Observation lhs, Observation rhs)
        {
            return !lhs.Equals(rhs);
        }
        public override string ToString()
        {
            if (this == UNINITIALIZED)
                return "UninitializedObservation";
            if (this == NO_OBSERVATION)
                return "NoObservation";

            return "Time_" + Time.ToString() + "_" +
                   "OD_" + ObservedDelay.ToString() + "_" +
                   "Swap_" + (DidObserveBothPatrollers ? "Y" : "N");
        }

        bool IEquatable<Observation>.Equals(Observation other)
        {
            return Time == other.Time &&
                   ObservedDelay == other.ObservedDelay &&
                   DidObserveBothPatrollers == other.DidObserveBothPatrollers;
        }
    }
    public struct LocationSet : IEquatable<LocationSet>
    {
        public int p1, p2;

        public bool Equals(LocationSet other)
        {
            return p1 == other.p1 && p2 == other.p2;
        }
        public static bool operator !=(LocationSet lhs, LocationSet rhs)
        {
            return lhs.p1 != rhs.p1 || lhs.p2 != rhs.p2;
        }
        public static bool operator ==(LocationSet lhs, LocationSet rhs)
        {
            return lhs.Equals(rhs);
        }
        public override string ToString()
        {
            throw new Exception("ToString unimplmented, use instead ToString(dir)");
        }
        public string ToString(int dir)
        {
            // may be used as a descriptor in pomdp file
            if (dir == 1)
                return "CW_" + "p1_" + p1.ToString() + "_p2_" + p2.ToString();
            return "CCW_" + "p1_" + p1.ToString() + "_p2_" + p2.ToString();
        }
    }

    // assumed constant parameters: 
    // N - segment count
    // T - intrusion time
    // N-(2T+1) is the length of each phase, where patrollers reach their destination
    // k(delaySegDistance) - indicates the distance between delay segments
    // observerSeg - where the observer observes
    // intruderSeg - where the intruder intrudes
    class PatrollersHMM
    {
        
        public class PatrollerState : IEquatable<PatrollerState>
        {
            public static PatrollerState DEAD_END_STATE // the state observer reaches after (and only after) transmitting
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
            public LocationSet Origins
            {
                get
                {
                    return new LocationSet() { p1 = InitP1, p2 = InitP2 };
                }
            }

            public int P1Location
            {
                get; protected set;
            }
            public int P2Location
            {
                get; protected set;
            }
            public int Dir // -1 or 1. one round CW, one round CCW
            {
                get; protected set;
            }
            public int InitP1 // 0 to N-1. p1 and p2 are always in distance >= 2T+1 from each other
            {
                get; protected set;
            }
            public int InitP2 // 0 to N-1. p1 and p2 are always in distance >= 2T+1 from each other
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
            public int Progress // 0 to PhaseLength-1, telling how many tim steps past the transition began
            {
                get; protected set;
            }

            private PatrollerState()
            {
                practicalVals = new List<int>();
            }
            public PatrollerState(PatrollerState src)
            {
                this.Dir = src.Dir;
                this.InitP1 = src.InitP1;
                this.InitP2 = src.InitP2;
                this.DestP1 = src.DestP1;
                this.DestP2 = src.DestP2;
                this.FirstDelaySeg = src.FirstDelaySeg;
                this.FirstDelay = src.FirstDelay;
                this.Progress = src.Progress;
                P1Location = src.P1Location;
                P2Location = src.P2Location;
                practicalVals = new List<int>(src.practicalVals);
            }

            /// <summary>
            /// </summary>
            /// <param name="pol"></param>
            /// <param name="dir"> -1 or 1. one round CW, one round CCW</param>
            /// <param name="initP1"> 0 to N-1. p1 and p2 are always in distance >= 2T+1 from each other</param>
            /// <param name="initP2"> 0 to N-1. p1 and p2 are always in distance >= 2T+1 from each other</param>
            /// <param name="destP1"> 0 to N-1, p1 and p2 are always in distance >= 2T+1 from each other </param>
            /// <param name="destP2"> 0 to N-1, p1 and p2 always in distance >= 2T+1 from each other </param>
            /// <param name="firstDelaySeg"> 0 to PhaseLength-1, telling the ratio  (i.e. 0 to 1) in the way of when the patroller pause. there are two segments in which the patroller will delay, one where this value points and the other in distance of additional 'delaySegDistance' segments from this segment (if a patroller moves less than delaySegDistance segments, then the delay is ON 'firstDelaySeg') </param>
            /// <param name="firstDelay"> 0 to PhaseLength-1, telling the ratio  (i.e. 0 to 1) of how much of the delay will be spent on the first of the two "delay segments". if there is only 1 delay segment, this val is irrelevant </param>
            /// <param name="progress"> may be updated </param>
            public PatrollerState(PatrollersHMM pol,
                                 int dir, int initP1, int initP2,
                                 int destP1, int destP2,
                                 int firstDelaySeg, int firstDelay,
                                 int progress)
            {
                this.Dir = dir;
                this.InitP1 = initP1;
                this.InitP2 = initP2;
                this.DestP1 = destP1;
                this.DestP2 = destP2;
                this.FirstDelaySeg = firstDelaySeg;
                this.FirstDelay = firstDelay;
                this.Progress = progress;
                updatePatrollerLocations(pol);
            }

            /// <summary>
            /// updates P1Location, P2Location
            /// </summary>
            /// <param name="pol"></param>
            public void updateProgress(PatrollersHMM pol, int ProgressVal)
            {
                Progress = ProgressVal;
                updatePatrollerLocations(pol);
            }
            public override int GetHashCode()
            {
                int hash = practicalVals.Count;
                for (int i = 0; i < practicalVals.Count; ++i)
                    hash ^= practicalVals[i];
                return hash;
            }
            public virtual bool Equals(PatrollerState y)
            {
                if (practicalVals.Count != y.practicalVals.Count)
                    return false;
                for (int i = 0; i < practicalVals.Count; ++i)
                    if (practicalVals[i] != y.practicalVals[i])
                        return false;
                return true;
            }



          
            /// <summary>
            /// given current 'progress', this calculates the location of p1
            /// </summary>
            /// <returns></returns>
            private void updatePatrollerLocations(PatrollersHMM pol)
            {
                LocationSet patrollerLocations;
                int p1delaySeg1, p2delaySeg1, p1delaySeg2, p2delaySeg2, p1Seg1Delay, p2Seg1Delay;

                pol.patrollersAtProgress(Progress, InitP1, InitP2, DestP1, DestP2, Dir, FirstDelaySeg, FirstDelay,
                    out patrollerLocations,
                    out p1delaySeg1,
                    out p2delaySeg1,
                    out p1delaySeg2,
                    out p1Seg1Delay,
                    out p2delaySeg2,
                    out p2Seg1Delay);

                practicalVals = new List<int>(11);
                practicalVals.Add(Progress);
                practicalVals.Add(InitP1);
                practicalVals.Add(InitP2);
                practicalVals.Add(DestP1);
                practicalVals.Add(DestP2);
                
                practicalVals.Add(p1delaySeg1);
                practicalVals.Add(p2delaySeg1);
                practicalVals.Add(p1delaySeg2);
                practicalVals.Add(p2delaySeg2);
                practicalVals.Add(p1Seg1Delay);
                practicalVals.Add(p2Seg1Delay);

                //if (dist11 < pol.DelaySegDistance || dist22 < pol.DelaySegDistance)
                //{
                //    // if there is no option for making a 2-segment delay, we use 1 segment delay for BOTH patrollers
                //    P1Location = getLoc(Progress, p1delaySeg1, p1TotalDelay);
                //    P2Location = getLoc(Progress, p1delaySeg1, p1TotalDelay);
                //}
                //else
                //{
                //    int p1delaySeg2 = (int)Math.Round(dist11 * ((float)FirstDelaySeg + pol.DelaySegDistance) / (pol.PhaseLength - 1));
                //    int p2delaySeg2 = (int)Math.Round(dist22 * ((float)FirstDelaySeg + pol.DelaySegDistance) / (pol.PhaseLength - 1));
                //    int p1Seg1Delay = (int)Math.Round(p1TotalDelay * ((float)FirstDelay) / (pol.PhaseLength - 1));
                //    int p2Seg1Delay = (int)Math.Round(p2TotalDelay * ((float)FirstDelay) / (pol.PhaseLength - 1));
                //    P1Location = getLoc(Progress, p1delaySeg1, p1Seg1Delay, p1delaySeg2, p1TotalDelay - p1Seg1Delay);
                //    P2Location = getLoc(Progress, p2delaySeg1, p2Seg1Delay, p2delaySeg2, p2TotalDelay - p2Seg1Delay);

                //    practicalVals.Add(p1delaySeg2);
                //    practicalVals.Add(p2delaySeg2);
                //    practicalVals.Add(p1Seg1Delay);
                //    practicalVals.Add(p2Seg1Delay);
                //}

            }

          

            private List<int> practicalVals; // helps trimming 'PatrollerState's with values that are identical in practice
            private static PatrollerState DEAD_END_STATE_VAL = new PatrollerState();

        }

        public int MinimalPatrollerDistance // patroller distance will always be >= MinimalPatrollerDistance 
        {
            get
            {
                return 2 * IntrusionTimeLength + 1;
            }
        }
        public int PhaseLength
        {
            get
            {
                return SegCount - (2 * IntrusionTimeLength + 1);
            }
        }
        public int SegCount { get; protected set; }
        public int IntrusionTimeLength { get; protected set; }
        public int DelaySegDistance { get; protected set; }
        public int ObserverSegment { get; protected set; }
        public int IntruderSeg { get; protected set; }

        
        public PatrollersHMM(int segCount, int intrusionTimeLength, int delaySegDistance, int observerSeg, int intruderSeg)
        {
            this.SegCount = segCount;
            this.IntrusionTimeLength = intrusionTimeLength;
            this.DelaySegDistance = delaySegDistance;
            this.ObserverSegment = observerSeg;
            this.IntruderSeg = intruderSeg;
            
            // generate all legal states (with progress 0), and associate an int with them
            stateToKey = new Dictionary<PatrollerState, int>();
            stateToKey[PatrollerState.DEAD_END_STATE] = stateToKey.Count; // dead end state, which observer enters after transmission

            for (int dir = -1; dir <= 1; dir +=2)
            for(int initp1 = 0; initp1 < SegCount; ++initp1)
            for (int initp2 = 0; initp2 < SegCount; ++initp2)
            for (int destp1 = 0; destp1 < SegCount; ++destp1)
            for (int destp2 = 0; destp2 < SegCount; ++destp2)
            {
                bool isTargetInner = // if true, both patrollers won't be able to go in the same direction and still reach the target (without colliding with eachother)
                    (isCW(destp1, initp1, initp2) && isCW(destp2, initp1, initp2)) ||
                    (!isCW(destp1, initp1, initp2) && !isCW(destp2, initp1, initp2));

                //bool isTargetInnerTest = // if true, both patrollers won't be able to go in the same direction and still reach the target (without colliding with eachother)
                //    (isCW(initp1, destp1, destp2) && isCW(initp2, destp1, destp2)) ||
                //    (!isCW(initp1, destp1, destp2) && !isCW(initp2, destp1, destp2));
                //if (isTargetInner != isTargetInnerTest)
                //{
                //    int a = 0;
                //}

                if (isTargetInner || 
                    modMinDist(SegCount, initp1, initp2) < MinimalPatrollerDistance ||
                    modMinDist(SegCount, destp1, destp2) < MinimalPatrollerDistance)
                    continue;

                for (int firstDelaySeg = 0; firstDelaySeg < PhaseLength; ++firstDelaySeg)
                for (int firstDelay = 0; firstDelay < PhaseLength; ++firstDelay)
                //for (int progress = 0; progress < PhaseLength; ++progress)
                {
                    var newState = new PatrollerState(this, dir, initp1, initp2, destp1, destp2, firstDelaySeg, firstDelay, 0);
                    if (stateToKey.ContainsKey(newState))
                        continue;
                    stateToKey[newState] = stateToKey.Count;
                }
            }
            // FIXME: don't let patrollers swap places if a patroller had delay in it's initial location (this breaks the early swapping rule)

            // generate states with progress >0, and populate transitionProb
            transitionProb = new Dictionary<int, Dictionary<int, double>>();
            transitionProb[stateToKey[PatrollerState.DEAD_END_STATE]] = new Dictionary<int, double>();
            transitionProb[stateToKey[PatrollerState.DEAD_END_STATE]][stateToKey[PatrollerState.DEAD_END_STATE]] = 1.0;


            List<PatrollerState> progress0StateList = stateToKey.Keys.ToList();
            
            for(int fromStateIdx = 0; fromStateIdx < progress0StateList.Count; ++fromStateIdx)
            {
                var fromState = progress0StateList[fromStateIdx];

                if (fromState == PatrollerState.DEAD_END_STATE)
                    continue;

                PatrollerState prevState = fromState;
                int from, to;
                // add transition from each state to the same state, with higher 'progress' value
                for (int progress = 1; progress < PhaseLength; ++progress)
                {
                    var newState = new PatrollerState(fromState);
                    newState.updateProgress(this,progress);
                    stateToKey[newState] = stateToKey.Count;

                    from = stateToKey[prevState];
                    to = stateToKey[newState];
                    transitionProb[from] = new Dictionary<int, double>();
                    transitionProb[from][to] = 1;
                    prevState = newState;
                }

                // add transition from each state to all other states that begin where 'from' ends.
                // Note that for every possible p1/p2 destination location there are several 'PatrollerState's, 
                // each with different First delay Seg and different delay time. The probability of every patroller destination 
                // combination is uniform, and given the destination - every delay combination is uniform
                Dictionary<LocationSet, List<PatrollerState>> configsPerDestination =
                    new Dictionary<LocationSet, List<PatrollerState>>();

                from = stateToKey[prevState];
                for (int toStateIdx = 0; toStateIdx < progress0StateList.Count; ++toStateIdx)
                {
                    var toState = progress0StateList[toStateIdx];
                    if (toState == PatrollerState.DEAD_END_STATE)
                        continue;

                    // from.progress is maximal. jump to all possible destination states with progress=0
                    if (toState.Origins != prevState.Destinations ||
                        toState.Progress != 0 ||
                        toState.Dir != -1 * prevState.Dir)
                        continue;

                    if (!configsPerDestination.ContainsKey(toState.Destinations))
                        configsPerDestination[toState.Destinations] = new List<PatrollerState>();

                    configsPerDestination[toState.Destinations].Add(toState);
                }

                transitionProb[from] = new Dictionary<int, double>();
                foreach (var dst in configsPerDestination)
                {
                    foreach (var dstConf in dst.Value)
                        transitionProb[from][stateToKey[dstConf]] = (1.0 / configsPerDestination.Count) * (1.0 / dst.Value.Count);
                }
            }


            observationToKey = new Dictionary<Observation, int>();
            observationToKey[Observation.NO_OBSERVATION] = observationToKey.Count;
            for (int t = 0; t < PhaseLength; ++t)
                for (int d = 0; d < PhaseLength; ++d)
                    observationToKey[new Observation(t, d, false)] = observationToKey.Count;
            
        }
        public virtual void sanityTest()
        {

            Dictionary<int, PatrollerState> keyToState = new Dictionary<int, PatrollerState>();
            foreach (var v in stateToKey)
                keyToState[v.Value] = v.Key;

            // make sure transitions sum up to 1
            foreach(var v in transitionProb)
            {
                double s = 0;
                foreach (var d in v.Value)
                    s += d.Value;
                if (Math.Abs(s-1) > 0.001)
                    throw new Exception("saintyTest() failed: transition won't sum to 1.0");
            }

            // make sure transition increase progress
            foreach (var f in transitionProb)
                foreach (var d in f.Value)
                {
                    if (keyToState[d.Key] == PatrollerState.DEAD_END_STATE ||
                        keyToState[f.Key] == PatrollerState.DEAD_END_STATE)
                        continue;

                    if (keyToState[d.Key].Progress !=  ((keyToState[f.Key].Progress +1) % PhaseLength))
                        throw new Exception("saintyTest() failed: illegal progress");
                }

            // make sure transition switch direction when needed
            foreach (var f in transitionProb)
                foreach (var d in f.Value)
                {
                    if (keyToState[d.Key] == PatrollerState.DEAD_END_STATE || keyToState[f.Key] == PatrollerState.DEAD_END_STATE)
                        continue;

                    if (keyToState[f.Key].Progress == (PhaseLength - 1))
                    {
                        if (keyToState[f.Key].Dir * keyToState[d.Key].Dir > 0)// when transitioning into another phase, dir must switch
                            throw new Exception("saintyTest() failed: illegal direction change");
                    }
                    else if (keyToState[f.Key].Dir * keyToState[d.Key].Dir < 0) // when progressing, dir must not change
                        throw new Exception("saintyTest() failed: illegal direction change");
                }

            // make sure generated observations make sense
            foreach(var s in stateToKey)
            {
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                
                if (getObservation(s.Key, s.Key.DestP1) == Observation.NO_OBSERVATION)
                    throw new Exception("saintyTest() failed: observation error");
                if (getObservation(s.Key, s.Key.InitP1) == Observation.NO_OBSERVATION)
                    throw new Exception("saintyTest() failed: observation error");
                if (getObservation(s.Key, s.Key.DestP2) == Observation.NO_OBSERVATION)
                    throw new Exception("saintyTest() failed: observation error");
                if (getObservation(s.Key, s.Key.InitP2) == Observation.NO_OBSERVATION)
                    throw new Exception("saintyTest() failed: observation error");
            }

            
            var probs = getInitialStateProb(); // if in debug mode, this internally checks convergence
            if (Math.Abs(probs[stateToKey[PatrollerState.DEAD_END_STATE]]) > 0.001)
                throw new Exception("saintyTest() failed: probability of reaching dead end state through HMM should be 0");
        }

        /// <summary>
        /// writes a pomdp, assuming the observer and intruder are stationary as given in CTOR,
        /// and the observer has the option of transmitting or waiting at each round, and utility is either 1.0 or 0.0
        /// file format is as specified in: 
        /// http://www.pomdp.org/code/pomdp-file-spec.html
        /// </summary>
        /// <returns></returns>
        virtual public List<string> writePOMDP()
        {
            Dictionary<int, PatrollerState> keyToState = new Dictionary<int, PatrollerState>();
            foreach (var v in stateToKey)
                keyToState[v.Value] = v.Key;

            List<string> res = new List<string>();
            res.Add("discount: 1.0");
            res.Add("values: reward");
            res.Add("states: " + stateToKey.Count.ToString());
            res.Add("actions: transmit wait"); // 2 actions - continue waiting, or transmitting now
            res.Add("observations: unobserved observed "); // tells whether the patroller is currently observed. the direction of movement is implicitly inferred according to phase direction

            // set initial state
            string initialStateProb = "";
            var initProb = getInitialStateProb();
            for (int i = 0; i < initProb.Count; ++i)
                initialStateProb += initProb[i].ToString() + " ";
            res.Add("start: " + initialStateProb);

            // set transitions
            res.Add("T: transmit : * : " + stateToKey[PatrollerState.DEAD_END_STATE].ToString()); // transmitting always leads to the dead end state
            foreach (var f in transitionProb) // probabilities in transitionProb are normal transitions, when observer waits
                foreach (var t in f.Value)
                    res.Add("T: wait : " + f.Key.ToString() + " : " + t.Key.ToString() + " " + t.Value.ToString());

            // set observations
            foreach (var s in stateToKey)
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                else if (s.Key.P1Location == ObserverSegment || s.Key.P2Location == ObserverSegment)
                    res.Add("O : wait : " + s.Value.ToString() + " observed 1.0");
                else
                    res.Add("O : wait : " + s.Value.ToString() + " unobserved 1.0");

            // set reward
            res.Add("R : wait : * : * : * 0.0"); // waiting gives nothing
            res.Add("R : transmit : " + stateToKey[PatrollerState.DEAD_END_STATE].ToString() + " : * 0.0");
            foreach (var s in stateToKey)
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                else if(modMinDist(SegCount, s.Key.P1Location, IntruderSeg) <= IntrusionTimeLength ||
                        modMinDist(SegCount, s.Key.P2Location, IntruderSeg) <= IntrusionTimeLength)
                    res.Add("R : transmit : " + s.Value.ToString() + " : * 1.0");
                else
                    res.Add("R : transmit : " + s.Value.ToString() + " : * 0.0");

            return res;
        }

        /// <summary>
        /// calculates the probability of being in each state , in the begining of the game.
        /// This is done by raising the markov chain to a high degree, and assume it converged
        /// </summary>
        /// <returns>
        /// index of each probability coresponds keys in stateToKey
        /// </returns>
        public List<double> getInitialStateProb()
        {

            // generate matrix:
            Meta.Numerics.Matrices.SquareMatrix markovChain = new Meta.Numerics.Matrices.SquareMatrix(stateToKey.Count); 

            // default matrix values are 0. set non zero values:
            foreach(var from in transitionProb)
                foreach (var to in from.Value)
                    markovChain[from.Key,to.Key] = to.Value;

            // choose some state with progress 0:
            List<int> arbitraryInitialState = new List<int>();
            int statesToFind = 1;
            #if DEBUG
            statesToFind = 2;
            #endif

            foreach (var s in stateToKey)
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                else if(s.Key.Progress == 0)
                {
                    arbitraryInitialState.Add(s.Value);
                    if(arbitraryInitialState.Count == statesToFind)
                        break;
                }

            // calculate a high degree of the matrix, and assume convergence:
            //var convergedMatrix = markovChain.Power((int)Math.Pow(PhaseLength,5));
            var convergedMatrix = markovChain.Power(PhaseLength * 10);

            // it doesn't matter what is the start state (as long as it's with progress 0), 
            // as we'll get the same end state distribution.
            var res = AlgorithmUtils.getRepeatingValueList<double>(stateToKey.Count);
            foreach (var s in stateToKey)
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                else
                    res[s.Value] = convergedMatrix[arbitraryInitialState.First(), s.Value];

//#if DEBUG
            try
            {
                List<string> lines = new List<string>();
                for (int from = 0; from < stateToKey.Count; ++from)
                {
                    string s = "";
                    for (int to = 0; to < stateToKey.Count; ++to)
                        s += convergedMatrix[from, to].ToString() + " ";
                    lines.Add(s);
                }
                File.WriteAllLines("matrixDebug10", lines.ToArray());
            }
            catch(Exception ex) 
            {
                try
                {
                    File.WriteAllText("matrixDebug", "error in writing matrix to file");
                }
                catch(Exception ex2){}
                
            }

            

            var test = AlgorithmUtils.getRepeatingValueList<double>(stateToKey.Count);
            foreach (var s in stateToKey)
                if (s.Key == PatrollerState.DEAD_END_STATE)
                    continue;
                else
                    test[s.Value] = convergedMatrix[arbitraryInitialState.Last(), s.Value];

            for (int i = 0; i < test.Count; ++i)
                if (Math.Abs(test[i] - res[i]) > 0.00001)
                {
                    MessageBox.Show("no convergence");
                    throw new Exception("no convergence");
                }

            bool nonZero = false;
            for (int i = 0; i < test.Count; ++i)
                if(Math.Abs(res[i]) > 0.01)
                {
                    nonZero = true;
                    break;
                }
            if (nonZero == false)
            {
                MessageBox.Show("unreachable state detected");
                throw new Exception("unreachable state detected");
            }
//#endif

            return res;
        }
        
        public Dictionary<Observation, int> observationToKey; // populated in ctor
        public Dictionary<PatrollerState, int> stateToKey; // populated in ctor
        public Dictionary<int, Dictionary<int, double>> transitionProb; // populated in ctor. maps PatrollerState index to transitionable PatrollerState indices (with probability distribution)

        /// <summary>
        /// considering progress 0 to 'PhaseLength-1', this generates the resulting
        /// observation from the given movement
        /// </summary>
        /// <param name="observerSeg">
        /// -1 means using this.ObserverSegment
        /// </param>
        /// <param name="phase"></param>
        /// <returns>
        /// null means no observation 
        /// </returns>
        public Observation getObservation(PatrollerState phaseMovement, int observerSeg = -1)
        {
            if (observerSeg == -1)
                observerSeg = this.ObserverSegment;
            var phaseMovementSim = new PatrollerState(phaseMovement);
            Observation res = Observation.UNINITIALIZED;
            // simulate the path p1 and p2 go from progress 0 to PhaseLength-1
            for (int progress = 0; progress < PhaseLength; ++progress)
            {
                
                phaseMovementSim.updateProgress(this, progress);
                int p1 = phaseMovementSim.P1Location, p2 = phaseMovementSim.P2Location;
                if (p1 == observerSeg || p2 == observerSeg)
                {
                    res = new Observation(phaseMovementSim.Progress,-1, false); // if the observer sees a patroller, see for how many steps (0 is just passing by)
                    while(p1 == observerSeg || p2 == observerSeg)
                    {
                        ++res.ObservedDelay;
                        ++progress;
                        phaseMovementSim.updateProgress(this, progress);
                        p1 = phaseMovementSim.P1Location;
                        p2 = phaseMovementSim.P2Location;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// tells what the observer would see in the time between start.progress and until end.progress
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Observation getObservation(PatrollerState start, 
                                          PatrollerState end)
        {

            // simulate the path p1 and p2 go from 'start' to 'end', between start's current 'progress' and until end's 'progress'
            List<PatrollerState> midStates = new List<PatrollerState>();
            midStates.Add(new PatrollerState(start));
            for (int i = start.Progress + 1; i < PhaseLength; ++i)
            {
                midStates.Add(new PatrollerState(midStates.Last()));
                midStates.Last().updateProgress(this, midStates.Last().Progress + 1);
            }
            if (end.Progress > 0)
            {
                midStates.Add(new PatrollerState(end));
                midStates.Last().updateProgress(this, 0);
                for (int i = 1; i < end.Progress; ++i)
                {
                    midStates.Add(new PatrollerState(midStates.Last()));
                    midStates.Last().updateProgress(this, midStates.Last().Progress + 1);
                }
            }


            Observation res = Observation.UNINITIALIZED;
            for (int progress = 0; progress < midStates.Count; ++progress)
            {
                var phaseMovementSim = midStates[progress];
                int p1 = phaseMovementSim.P1Location, p2 = phaseMovementSim.P2Location;
                if (p1 == ObserverSegment || p2 == ObserverSegment)
                {
                    res = new Observation(phaseMovementSim.Progress, -1, false); // if the observer sees a patroller, see for how many steps (0 is just passing by)
                    while (p1 == ObserverSegment || p2 == ObserverSegment)
                    {
                        ++res.ObservedDelay;
                        ++progress;
                        phaseMovementSim = midStates[progress];
                        p1 = phaseMovementSim.P1Location;
                        p2 = phaseMovementSim.P2Location;
                    }
                }
            }


            return res;
        }

        // serves getPLocation()
        protected static int getLoc(int progress, int delaySeg, int delayTime)
        {
            if (progress < delaySeg)
                return progress;

            if (progress < delaySeg + delayTime)
                return delaySeg;

            return progress - delayTime;
        }
        protected static int getLoc(int progress, int delaySeg1, int delayTime1, int delayseg2, int delayTime2)
        {
            if (progress < delaySeg1)
                return progress;

            if (progress < delaySeg1 + delayTime1)
                return delaySeg1;

            if (progress < delayseg2 + delayTime1)
                return progress - delayTime1;

            if (progress < delayseg2 + delayTime1 + delayTime2)
                return delayseg2;

            return progress - delayTime1 - delayTime2;
        }



        protected void patrollersAtProgress(
              int Progress,
              int InitP1, int InitP2, int DestP1, int DestP2, int Dir, int FirstDelaySeg, int FirstDelay,
              out LocationSet patrollerLocations,
              out int p1delaySeg1,
              out int p2delaySeg1,
              out int p1delaySeg2,
              out int p1Seg1Delay,
              out int p2delaySeg2,
              out int p2Seg1Delay)
        {
            PatrollersHMM.patrollersAtProgress(SegCount, PhaseLength, Progress, DelaySegDistance, 
                InitP1, InitP2, 
                DestP1, DestP2, 
                Dir,
                FirstDelaySeg,
                FirstDelay,
                out patrollerLocations, 
                out p1delaySeg1,
                out p2delaySeg1, 
                out p1delaySeg2, 
                out p1Seg1Delay, 
                out p2delaySeg2, 
                out p2Seg1Delay);
        }
        public static void patrollersAtProgress(
              int SegCount,
              int PhaseLength,
              int Progress,
              int DelaySegDistance,
              int InitP1, int InitP2, int DestP1, int DestP2, int Dir, int FirstDelaySeg, int FirstDelay,
              out LocationSet patrollerLocations,
              out int p1delaySeg1,
              out int p2delaySeg1,
              out int p1delaySeg2,
              out int p1Seg1Delay,
              out int p2delaySeg2,
              out int p2Seg1Delay)
        {
            int dist11 = MathEx.modDist(SegCount, InitP1, DestP1);
            int dist22 = MathEx.modDist(SegCount, InitP2, DestP2);
            if (Dir == -1)
            {
                dist11 = SegCount - dist11;
                dist22 = SegCount - dist22;
                //dist21 = N - dist21;
                //dist12 = N - dist12;
            }

            //if (dist21 < dist12)
            //{
            //    // p2 must advance slower than p1
            //}
            //else
            //{
            //    //p1 must advance slower than p2
            //}

            p1delaySeg1 = (int)Math.Round(dist11 * ((float)FirstDelaySeg) / (PhaseLength - 1));
            p2delaySeg1 = (int)Math.Round(dist22 * ((float)FirstDelaySeg) / (PhaseLength - 1));
            int p1TotalDelay = PhaseLength - dist11;
            int p2TotalDelay = PhaseLength - dist22;

            if (dist11 < DelaySegDistance || dist22 < DelaySegDistance)
            {
                // if there is no option for making a 2-segment delay, we use 1 segment delay for BOTH patrollers
                patrollerLocations = new LocationSet()
                {
                    p1 = getLoc(Progress, p1delaySeg1, p1TotalDelay),
                    p2 = getLoc(Progress, p1delaySeg1, p1TotalDelay)
                };
                p1delaySeg2 = p2delaySeg2 = p1Seg1Delay = p2Seg1Delay = -1;
            }
            else
            {
                p1delaySeg2 = (int)Math.Round(dist11 * ((float)FirstDelaySeg + DelaySegDistance) / (PhaseLength - 1));
                p2delaySeg2 = (int)Math.Round(dist22 * ((float)FirstDelaySeg + DelaySegDistance) / (PhaseLength - 1));
                p1Seg1Delay = (int)Math.Round(p1TotalDelay * ((float)FirstDelay) / (PhaseLength - 1));
                p2Seg1Delay = (int)Math.Round(p2TotalDelay * ((float)FirstDelay) / (PhaseLength - 1));
                patrollerLocations = new LocationSet()
                {
                    p1 = getLoc(Progress, p1delaySeg1, p1Seg1Delay, p1delaySeg2, p1TotalDelay - p1Seg1Delay),
                    p2 = getLoc(Progress, p2delaySeg1, p2Seg1Delay, p2delaySeg2, p2TotalDelay - p2Seg1Delay)
                };
            }

        }
        protected bool isCW(int position, int p1, int p2)
        {
            return MathEx.modIsBetween(SegCount, p1, p2, position);
        }
        /// <summary>
        /// if dir=1, identical to modDist()
        /// otherwise, assumes opposite direction
        /// </summary>
        /// <param name="dir">
        /// 1 or -1
        /// </param>
        /// <param name="mod"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        protected static int modDistDirected(int dir, int mod, int from, int to)
        {
            if (dir == 1)
                return MathEx.modDist(mod, from, to);
            return MathEx.modDist(mod, to, from);
        }
        protected static int modMinDist(int mod, int from, int to)
        {
            return Math.Min(MathEx.modDist(mod, from, to),
                            MathEx.modDist(mod, to, from));
        }

    }

    
    class BoundableRandomPatroller : AIntrusionPursuersPolicy
    {

        public override List<ArgEntry> policyInputKeys()
        {
           // get
            //{
                throw new NotImplementedException();
            //}
        }

        /// <summary>
        /// tells the minimal probability for a patroller being in distance <=T
        /// from the intruder when intrusion begins
        /// </summary>
        /// <returns></returns>
        public double getMinimalPatrollerPresenceProb()
        {
            throw new NotImplementedException();
        }
        public override Dictionary<Pursuer, List<Point>> getNextStep()
        {
            throw new NotImplementedException();
        }

        public override bool init(AGameGraph G, IntrusionGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            throw new NotImplementedException();
        }

        public override void setGameState(int currentRound, List<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            throw new NotImplementedException();
        }
    }
}