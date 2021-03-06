using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using GoE.Utils;
using GoE.AppConstants.Policies.SingleTransmissionPerimOnlyPatrollerPolicy;
using GoE.Utils.MathExExtensions;

using GoE.Utils.Algorithms;
using GoE.Utils.Algorithms.FunctionTreeNode;
using GoE.Utils.LPSolver55;
using System.Windows.Forms;

namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly.PreProcessInvariantMaximization
{
    public class TransitionStateMapMethod2
    {
        public int SegmentsCount { get; protected set; }
        public int MinSwapStepCount { get; protected set; } // for 8 segments and T = 1, MinSwapStepCount is at least 8-2*t-1=5, which is also interval length

        /// <summary>
        /// utility function. adds a state if its legal. 
        /// patrollers can only move minSwapStepCount segments between one interval start and the next interval start
        /// </summary>
        /// <param name="s"></param>
        //private void addStateIfLegal(TransitionState s, bool p1IncreasingSegments, bool p2IncreasingSegments)
        //{
        //    if ((p1IncreasingSegments && MathEx.modDist(SegmentsCount, s.p1Start, s.p1End) > MinSwapStepCount)||
        //        (!p1IncreasingSegments && MathEx.modDist(SegmentsCount, s.p1End, s.p1Start) > MinSwapStepCount)||
        //        (p2IncreasingSegments && MathEx.modDist(SegmentsCount, s.p2Start, s.p2End) > MinSwapStepCount) ||
        //         (!p2IncreasingSegments && MathEx.modDist(SegmentsCount, s.p2End, s.p2Start) > MinSwapStepCount))
        //        return;
        //    stateToIdx[s] = stateToIdx.Count;
        //    idxToState[idxToState.Count] = s;
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="segmentsCount"></param>
        /// <param name="minSwapStepCount"></param>
        /// <param name="forceSymmetricPatrollers">
        /// if true, removes states transitions with similar start and end value (only with opposite patrollers)
        /// </param>
        /// <param name="forceSymmetricRotation">
        /// if true, transitions with similar values, only translated (i.e. rotated) by some scalar are removed
        /// (we assume untranslated values all have p1Start=0)
        /// </param>
        public TransitionStateMapMethod2(int segmentsCount, 
                                  int minSwapStepCount,
                                  // bool forceSymmetricPatrollers, // we don't allow filtering symmetric states, since it a bit tedious to program and only cuts by 2 the amount of variables
                                  bool forceSymmetricRotation)
        {
            SegmentsCount = segmentsCount;
            MinSwapStepCount = minSwapStepCount;

            stateToIdx = new Dictionary<TransitionState, int>();
            idxToState = new Dictionary<int, TransitionState>();
          
            for (int P1Start = 0; P1Start < segmentsCount; ++P1Start)
            {
                for (int P1End = 0; P1End < segmentsCount; ++P1End)
                    for (int P2Start = 0; P2Start < segmentsCount; ++P2Start)
                        for (int P2End = 0; P2End < segmentsCount; ++P2End)
                        {
                            var s = new TransitionState() { p1Start = P1Start, p1End = P1End, p2Start = P2Start, p2End = P2End };

                            if (P1Start == P2Start || P1End == P2End)
                                continue;

                            // since paths can't overlap (apart for the end segment), make sure
                            // whether segments increase or decrease with the path from start end,
                            // and make sure that the other patroller doesn't interfere in that path
                            bool p1IncreasingSegments = ((P1Start <= P1End) && (P2Start >= P1End || P2Start < P1Start)) ||
                                                        ((P1Start >= P1End) && (P2Start > P1End && P2Start < P1Start));
                            bool p1DecreasingSegments = ((P1Start <= P1End) && (P2Start > P1End && P2Start < P1Start)) ||
                                                        ((P1Start >= P1End) && (P2Start >= P1End || P2Start < P1Start));

                            bool p2IncreasingSegments = ((P2Start <= P2End) && (P1Start >= P2End || P1Start < P2Start)) ||
                                                        ((P2Start >= P2End) && (P1Start > P2End && P1Start < P2Start));
                            bool p2DecreasingSegments = ((P2Start <= P2End) && (P1Start > P2End && P1Start < P2Start)) ||
                                                        ((P2Start >= P2End) && (P1Start >= P2End || P1Start < P2Start));


                            int p1Min = P1Start, p1Max = P1End;
                            int p2Min = P2Start, p2Max = P2End;
                            if (p1DecreasingSegments)
                                AlgorithmUtils.Swap(ref p1Max, ref p1Min);
                            if (p2DecreasingSegments)
                                AlgorithmUtils.Swap(ref p2Max, ref p2Min);

                            // make sure no overlap in territories:
                            if ((p1IncreasingSegments || p1DecreasingSegments) &&
                                (p2IncreasingSegments || p2DecreasingSegments) &&
                                (P1Start == P2End || MathEx.modIsBetween(segmentsCount, p1Min, p1Max, P2End)) &&
                                (P2Start == P1End || MathEx.modIsBetween(segmentsCount, p2Min, p2Max, P1End)))
                            {
                                /// patrollers can only move minSwapStepCount segments between one interval start and the next interval start
                                if ((p1IncreasingSegments && MathEx.modDist(SegmentsCount, s.p1Start, s.p1End) > MinSwapStepCount) ||
                                   (!p1IncreasingSegments && MathEx.modDist(SegmentsCount, s.p1End, s.p1Start) > MinSwapStepCount) ||
                                   (p2IncreasingSegments && MathEx.modDist(SegmentsCount, s.p2Start, s.p2End) > MinSwapStepCount) ||
                                    (!p2IncreasingSegments && MathEx.modDist(SegmentsCount, s.p2End, s.p2Start) > MinSwapStepCount))
                                    continue;

                                stateToIdx[s] = stateToIdx.Count;
                                idxToState[idxToState.Count] = s;
                            }
                            
                            //if (P1Start == P2Start ||
                            //    (P1End != P2Start && MathEx.modIsBetween(segmentsCount, P1Start, P1End, P2Start)) || // no overlap between p1 and p2's path, unless it's only in the begining of the next interval
                            //    (P2End != P1Start && MathEx.modIsBetween(segmentsCount, P1Start, P1End, P2End)) ||
                            //   )
                            //    continue;
                            
                        }

                if (forceSymmetricRotation)
                    return; // we only generates states with p1start=0
            }
            
            
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// for each key, the list of values contains all transitions with similar values, only translated (i.e. rotated) by some scalar
        /// (we assume untranslated values all have p1Start=0)
        /// </returns>
        public Dictionary<TransitionState, List<TransitionState>> getTranslatedTransitions()
        {
            Dictionary<TransitionState, List<TransitionState>> res = 
                new Dictionary<TransitionState, List<TransitionState>>();

            foreach (var s in stateToIdx)
            {
                TransitionState untranslatedState = s.Key;
                int translation = untranslatedState.p1Start;
                untranslatedState.p1Start -= translation;
                untranslatedState.p1End -= translation;
                untranslatedState.p2Start -= translation;
                untranslatedState.p2End -= translation;

                if (!res.ContainsKey(untranslatedState))
                    res[untranslatedState] = new List<TransitionState>();

                res[untranslatedState].Add(s.Key);
            }

            return res;
        }
        
        
        /// <summary>
        /// returns all transitions with similar start and end value (only with opposite patrollers)
        /// </summary>
        /// <returns></returns>
        public List<Tuple<TransitionState, TransitionState>> getDualTransitions()
        {
            List<Tuple<TransitionState, TransitionState>> res = new List<Tuple<TransitionState, TransitionState>>();
            HashSet<TransitionState> processedStates = new HashSet<TransitionState>();

            foreach(var s in stateToIdx)
            {
                
                if (processedStates.Contains(s.Key))
                    continue;

                TransitionState oppositeState = s.Key;
                GoE.Utils.Algorithms.AlgorithmUtils.Swap(ref oppositeState.p1Start, ref oppositeState.p2Start);
                GoE.Utils.Algorithms.AlgorithmUtils.Swap(ref oppositeState.p1End, ref oppositeState.p2End);

                processedStates.Add(s.Key);
                processedStates.Add(oppositeState);
                res.Add(Tuple.Create(s.Key, oppositeState));
            }

            return res;
        }

        /// <summary
        /// </summary>
        /// <returns>
        /// all transitions for which p1start=p2end, or p2start=p1end, grouped by start point
        /// </returns>
        public void getTransitionsPerSwap(out Dictionary<Point, List<TransitionState>> P1REplacesP2,
                                          out Dictionary<Point, List<TransitionState>> P2REplacesP1)
        {
            P2REplacesP1 = new Dictionary<Point, List<TransitionState>>();
            P1REplacesP2 = new Dictionary<Point, List<TransitionState>>();
            foreach (var ts in stateToIdx)
            {
                if (ts.Key.p1Start == ts.Key.p2End)
                {
                    var P2REplacesP1key = new Point(ts.Key.p1Start, ts.Key.p2Start);
                    if (!P2REplacesP1.ContainsKey(P2REplacesP1key))
                        P2REplacesP1[P2REplacesP1key] = new List<TransitionState>();
                    P2REplacesP1[P2REplacesP1key].Add(ts.Key);
                }

                if (ts.Key.p2Start == ts.Key.p1End)
                {
                    var P1REplacesP2key = new Point(ts.Key.p2Start, ts.Key.p1End);
                    if (!P1REplacesP2.ContainsKey(P1REplacesP2key))
                        P1REplacesP2[P1REplacesP2key] = new List<TransitionState>();
                    P1REplacesP2[P1REplacesP2key].Add(ts.Key);
                }
            }
        }

        /// <summary
        /// </summary>
        /// <returns>
        /// all transitions that share the same start p1,p2 values
        /// </returns>
        public Dictionary<Point, List<TransitionState>> getTransitionsPerStartPoint()
        {
            Dictionary<Point, List<TransitionState>> transitionPerStartPoint =
                new Dictionary<Point, List<TransitionState>>();

            foreach (var ts in stateToIdx)
            {
                var t = new Point(ts.Key.p1Start, ts.Key.p2Start);
                if (!transitionPerStartPoint.ContainsKey(t))
                    transitionPerStartPoint[t] = new List<TransitionState>();
                transitionPerStartPoint[t].Add(ts.Key);
            }

            return transitionPerStartPoint;
        }

        
        public Dictionary<TransitionState, int> stateToIdx;
        public Dictionary<int, TransitionState> idxToState;
    }

    /// <summary>
    /// this linear program calculates the transition probabilities between each two-patroller joint-locations, given 
    /// that between start joint location and end joint location, exactly 'minSwapTime" steps pass i.e. one time interval.
    /// Each variable represents a legal transition (where patrollers don't overlap in that time interval).
    /// we maximize the minimum probability of one patroller being in any 2T+1 sized area, n-2T+x steps after the other patroller took its place
    /// Optional Constraint 1: transitions with same start/end vals are similar, but translated
    /// Constraint 1: For all variables with the same start point, all possible endpoints sum up to 1.
    /// Constraint 2: all variables are in [0,1]
    /// Constraint 3: for all variables that represent  
    /// the same swap(i.e.all variables that represents a case where patroller 1 in segment S replaces patroller 2), 
    /// we maximize the minimum probability of any 2T+1 sized area not being occupied by the other patroller.
    /// We assume the observer knows the start location of the transition, but we obfuscate the end location
    /// </summary>
    public class TerritoryTransitionProgramMethod2 // failed, since in constraint 3 we either assume oberver has no previous beleif state, or strictly assume it knows the exact current state
    {
        private TransitionStateMapMethod2 stateMap;
        
        private int N { get; set; } // # of segments
        private int T { get; set; } // time needed for intrusion
        private int l { get; set; } // min. steps count before one patroller is allowed to visit the same place other patrolelr did
        private int u { get; set; } // size of the area we want to maximize the probability of the patroller being in
        //private bool ForceSymmetricPatrollers { get; set; } // if true, transitions with similar start/end values(but opposite patrollers) are forced to have even values
        private bool ForceSymmetricRotation{ get; set; } // if true, transitions with same start/end vals are similar, but translated

        private double[] lpOutput; // see getTransitionProbability()
        private int areaBoundVariableIdx { get; set; }

        /// <summary>
        /// tells whether transitions with similar start/end values (but with opposite patrollers) 
        /// have identical probabilities 
        /// </summary>
        /// <returns></returns>
        public bool testOutputIdenticalPatrollerMovement()
        {
            const double EPSILON = 0.001;
            var dualStates = stateMap.getDualTransitions();

            foreach (var ss in dualStates)
            {
                if (System.Math.Abs(lpOutput[stateMap.stateToIdx[ss.Item1]] - lpOutput[stateMap.stateToIdx[ss.Item2]]) > EPSILON)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// tells whether transition are translation-independent, regardless of 'ForceSymmetricRotation' value
        /// </summary>
        /// <returns></returns>
        public bool testOutputSymmetricRotation()
        {
            const double EPSILON = 0.001;
            var symmetricStates = stateMap.getTranslatedTransitions();
            foreach (var similarStates in symmetricStates)
                foreach (var comparedState in similarStates.Value)
                    if (System.Math.Abs(lpOutput[stateMap.stateToIdx[comparedState]] -
                        lpOutput[stateMap.stateToIdx[similarStates.Key]]) > EPSILON)
                        return false;
            return true;
        }
        public double ObjectiveValue { get; protected set; }
        public double getTransitionProbability(TransitionState s)
        {
            return lpOutput[stateMap.stateToIdx[s]];
        }
        
        /// <summary>
        /// utility func, serving TerritoryTransitionProgram()
        /// </summary>
        /// <param name="sortedstates">
        /// sorted by either p1end or p2end, such that every 'u' consecutive states represent an area where the patroller should be
        /// (with maximized probability)
        /// </param>
        /// <param name="lp"></param>
        /// <param name="areaBoundVariableIdx"></param>
        private void addAreaBoundConstraint(List<TransitionState> sortedstates, lpsolveWrapper lp, int areaBoundVariableIdx, int variablesCount)
        {
            for (int i = 0; i < N; ++i)
            {
                var cfPerVarIdx = new List<Tuple<int, double>>();
                //double[] areaVarIndices = new double[variablesCount + 1];
                for (int ai = i; ai < i + u; ++ai)
                    cfPerVarIdx.Add(Tuple.Create(stateMap.stateToIdx[sortedstates[ai % N]],-1.0));
                cfPerVarIdx.Add(Tuple.Create(areaBoundVariableIdx, 1.0));
                lp.addConstraint(cfPerVarIdx, lpsolve.lpsolve_constr_types.LE, 0); // area bound is LE than any area
                //    areaVarIndices[1 + stateMap.stateToIdx[sortedstates[ai % N]]] = 1;
                //areaVarIndices[1 + areaBoundVariableIdx] = -1; // area bound is LE than any area
                //lpsolve.add_constraint(lp, areaVarIndices.ToArray(), lpsolve.lpsolve_constr_types.LE, 0);
            }
        }

        void testConstraint1(double[] vals)
        {
            // Constraint 1: For all variables with the same start point, all possible endpoints sum up to 1.
            foreach (var sp in stateMap.getTransitionsPerStartPoint())
            {
                // all transitions in sp.value share the same start point sp.key
                // note: each constraint's first value should always be 0
                //double[] sameStartPointVarIndices = new double[variablesCount + 1];
                var valPerVar = new List<double>();
                double sum = 0;
                foreach (var spstate in sp.Value)
                {
                    sum += vals[stateMap.stateToIdx[spstate]];
                    valPerVar.Add(vals[stateMap.stateToIdx[spstate]]);
                }
                if(System.Math.Abs(sum-1) > 0.001)
                {
                    int a = 0;
                }
                
            }
        }
        void testConstraint2(double[] vals)
        {
            for(int i =0; i < vals.Count();++i)
            {
                if(vals[i] < 0 || vals[i] > 1)
                {
                    int a = 0;
                }
            }
        }
        void testConstraint3(double[] vals)
        {
            Dictionary<Point, List<TransitionState>> P1REplacesP2;
            Dictionary<Point, List<TransitionState>> P2REplacesP1;
            stateMap.getTransitionsPerSwap(out P1REplacesP2, out P2REplacesP1);
            foreach (var v in P1REplacesP2)
            {
                v.Value.Sort((TransitionState lhs, TransitionState rhs) => { return lhs.p2End.CompareTo(rhs.p2End); });


                for (int i = 0; i < N; ++i)
                {
                    var cfPerVarIdx = new List<double>();
                    double sum = 0;
                    for (int ai = i; ai < i + u; ++ai)
                    {
                        cfPerVarIdx.Add(lpOutput[stateMap.stateToIdx[v.Value[ai % N]]]);
                        sum += lpOutput[stateMap.stateToIdx[v.Value[ai % N]]];
                    }
                    if(sum > ObjectiveValue)
                    {
                        int a = 0;
                    }
                }
            }
            foreach (var v in P2REplacesP1)
            {
                v.Value.Sort((TransitionState lhs, TransitionState rhs) => { return lhs.p1End.CompareTo(rhs.p1End); });
                for (int i = 0; i < N; ++i)
                {
                    var cfPerVarIdx = new List<double>();
                    double sum = 0;
                    for (int ai = i; ai < i + u; ++ai)
                    {
                        cfPerVarIdx.Add(lpOutput[stateMap.stateToIdx[v.Value[ai % N]]]);
                        sum += lpOutput[stateMap.stateToIdx[v.Value[ai % N]]];
                    }
                    if (sum > ObjectiveValue)
                    {
                        int a = 0;
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="intrusionTime">
        /// // time needed for intrusion
        /// </param>
        /// <param name="segmentCount"></param>
        /// <param name="minSwapStepCount">
        /// min. steps count before one patroller is allowed to visit the same place other patrolelr did
        /// </param>
        /// <param name="coveredAreaSegCount">
        /// size of the area we want to maximize the probability of a patroller being present there
        /// </param>
        /// <param name="forceSymmetricRotation">
        /// 
        /// </param>
        public TerritoryTransitionProgramMethod2(int intrusionTime, 
                                          int segmentCount, 
                                          int minSwapStepCount, 
                                          int coveredAreaSegCount,
                                          //bool forceSymmetricPatrollers,
                                          bool forceSymmetricRotation)
        {
            const double EPSILON = 0.0001;

            u = coveredAreaSegCount;
            N = segmentCount;
            T = intrusionTime;
            l = minSwapStepCount;
            ForceSymmetricRotation = forceSymmetricRotation;

            // optional constraints 1 is not an actual constraint, and instead it just just reduces the amount of variables.
            // TransitionStateMapMethod2() already does this for us
            stateMap = new TransitionStateMapMethod2(N, minSwapStepCount, forceSymmetricRotation);

            // used variables:
            // stateMap.stateToIdx.Count variables : one per transition
            // 1 variable : bounds from below the probability of 1 patroller not being in a certain 2T+1 sized area, when the other patroller
            //              just took it's place after 'MinPatrollerSwapSteps' steps.
            areaBoundVariableIdx = stateMap.stateToIdx.Count;
            int variablesCount = stateMap.stateToIdx.Count + 1;

            lpsolveWrapper lp = new lpsolveWrapper(variablesCount);

           

            // Constraint 1: For all variables with the same start point, all possible endpoints sum up to 1.
            foreach (var sp in stateMap.getTransitionsPerStartPoint())
            {
                // all transitions in sp.value share the same start point sp.key
                // note: each constraint's first value should always be 0
                //double[] sameStartPointVarIndices = new double[variablesCount + 1];
                var cfPerVarIdx = new List<Tuple<int, double>>();
                foreach (var spstate in sp.Value)
                    cfPerVarIdx.Add(Tuple.Create(stateMap.stateToIdx[spstate], 1.0));
                //    sameStartPointVarIndices[1 + stateMap.stateToIdx[spstate]] = 1;
                //lpsolve.add_constraint(lp, sameStartPointVarIndices.ToArray(), lpsolve.lpsolve_constr_types.EQ, 1);
                lp.addConstraint(cfPerVarIdx, lpsolve.lpsolve_constr_types.EQ, 1);
            }
            /// Constraint 2: all variables are in [0,1]
            foreach ( var sidx in stateMap.idxToState)
            {
                lp.addBound(sidx.Key, 0, 1.0);
                //lpsolve.set_bounds(lp, 1 + sidx.Key, 0, 1);
                //var sanityConstraint = new double[variablesCount + 1];
                //sanityConstraint[1 + sidx.Key] = 1;
                //lpsolve.add_constraint(lp, sanityConstraint, lpsolve.lpsolve_constr_types.GE, 1); // at most 1
                //lpsolve.add_constraint(lp, sanityConstraint, lpsolve.lpsolve_constr_types.LE, EPSILON); // at least epsilon (actual 0 might cause unexpected behaviour)
            }
            //lp.addBound(areaBoundVariableIdx, 0, 1.0);

            /// Constraint 3: for all variables that represent the same swap
            /// (i.e. all variables that represents a case where patroller 1 in segment S replaces patroller 2), 
            /// we maximize the minimum probability of any 2T+1 sized area not being occupied by the other patroller
            Dictionary<Point, List<TransitionState>> P1REplacesP2;
            Dictionary<Point, List<TransitionState>> P2REplacesP1;
            stateMap.getTransitionsPerSwap(out P1REplacesP2, out P2REplacesP1);
            foreach (var v in P1REplacesP2)
            {
                v.Value.Sort((TransitionState lhs, TransitionState rhs) => { return lhs.p2End.CompareTo(rhs.p2End); });
                addAreaBoundConstraint(v.Value, lp, areaBoundVariableIdx, variablesCount);
            }
            foreach (var v in P2REplacesP1)
            {
                v.Value.Sort((TransitionState lhs, TransitionState rhs) => { return lhs.p1End.CompareTo(rhs.p1End); });
                addAreaBoundConstraint(v.Value, lp, areaBoundVariableIdx, variablesCount);
            }


            // objsective function: just maximize the variable of areaBoundVariableIdx
            //var objFunc = new double[variablesCount + 1]; // objFunc[0] should always be 0
            //objFunc[1 + areaBoundVariableIdx] = 1;
            //lpsolve.set_obj_fn(lp, objFunc);
            lp.solve(new List<Tuple<int, double>> { Tuple.Create(areaBoundVariableIdx, 1.0) }, 60);

            //lpsolve.solve(lp);

            //lpOutput = new double[1 + variablesCount];
            //lpsolve.get_primal_solution(lp, lpOutput);

            //MessageBox.Show(this.getMaximizedValue().ToString());
            lpOutput = lp.VariableValue;
            ObjectiveValue = lp.ObjectiveTermValue;
            MessageBox.Show(lp.ObjectiveTermValue.ToString());
            testConstraint1(lp.VariableValue);
            testConstraint2(lp.VariableValue);
            testConstraint3(lp.VariableValue);
            testOutputIdenticalPatrollerMovement();
            testOutputSymmetricRotation();
        }
    }
}