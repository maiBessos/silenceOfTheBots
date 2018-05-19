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
using GoE.Policies.Intrusion.SingleTransmissionPerimOnly.Utils;
using GoE.Utils.Algorithms;
using GoE.Utils.Algorithms.FunctionTreeNode;

namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly
{
    public class TerritoryWithRecentHistoryMChainPreTranPolicy : AIntrusionPursuersPolicy
    {
        private int N { get; set; } // # of segments
        private int T { get; set; } // time needed for intrusion
        private const int ROTATE = 0; // Function tree parameter index
        private const int STAY = 1; // Function tree parameter index


        /// <summary>
        /// see ArgEntry KEEP_PATROLLER_DIRECTION
        /// </summary>
        public bool DirectedPatrollers
        {
            get;
            protected set;
        }
        /// <summary>
        /// see ArgEntry CAN_PATROLLER_STAY
        /// </summary>
        public bool StayingPatrollers
        {
            get;
            protected set;
        }
        /// <summary>
        /// derived from intrusion time and ArgEntry MINIMAL_UNGUARDED_SEGMENTS_FACTOR
        /// </summary>
        public int MinimalUnoccupiedSegments
        {
            get;
            protected set;
        }

        public GridGameGraph gameGraph { get; protected set; }

        public IntrusionGameParams gameParams { get; protected set; }

        public override List<ArgEntry> policyInputKeys()
        {
           // get
           // {
                List<ArgEntry> res = new List<ArgEntry>();
                res.AddRange(ReflectionUtils.getStaticInstancesInClass<ArgEntry>(
                             typeof(PreTransmissionMChain)));

                res.AddRange(ReflectionUtils.getStaticInstancesInClass<ArgEntry>(
                             typeof(TerritoryWithRecentHistoryMChainPreTransmission)));

                return res;
           // }
        }

        public override bool init(AGameGraph G,
                                  IntrusionGameParams prm,
                                  IPolicyGUIInputProvider pgui,
                                  Dictionary<string, string> policyParams)
        {
            gameGraph = (GridGameGraph)G;
            gameParams = prm;

            StayingPatrollers =
                PreTransmissionMChain.CAN_PATROLLER_STAY.tryRead(policyParams) == "1";
            DirectedPatrollers =
                PreTransmissionMChain.KEEP_PATROLLER_DIRECTION.tryRead(policyParams) == "1";

            MinimalUnoccupiedSegments = 0;
            Exceptions.ConditionalTryCatch<Exception>(() =>
            {
                MinimalUnoccupiedSegments = (int)System.Math.Round(float.Parse(
                    TerritoryWithRecentHistoryMChainPreTransmission.MINIMAL_UNGUARDED_SEGMENTS_FACTOR.tryRead(policyParams)));
            });
            if (MinimalUnoccupiedSegments <= 0)
                return false;

            return true;
        }

        public override Dictionary<Pursuer, List<Point>> getNextStep()
        {
            throw new NotImplementedException();
        }
        
        public override void setGameState(int currentRound, List<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MatrixOpTree getChain()
        {
            N = gameParams.SensitiveAreaSquare(gameGraph.getNodesByType(NodeType.Target).First()).PointCount;
            T = gameParams.t_i;

            if(DirectedPatrollers && StayingPatrollers)
                return getDirectedStayingChain();

            return null;
        }
        
        // todo: move class to private
        public class DirectedStayingChainState : CompositeStateComponent
        {
            private int N;

            // all members below have values for patroller1 and patroller 2 (indices 0 and 1)
            private DirComponent[] dir = new DirComponent[2];  // 0 or 1

            private LocationComponent[] pLocation = new LocationComponent[2], 
                                        tStart = new LocationComponent[2], 
                                        tEnd = new LocationComponent[2];

            public DirectedStayingChainState(int segmentsCount, int stateVal)
            {
                N = segmentsCount;
                for (int i = 0; i < 2; ++i)
                {
                    dir[i] = new DirComponent();
                    pLocation[i] = new LocationComponent();
                    tStart[i] = new LocationComponent();
                    tEnd[i] = new LocationComponent();
                }
                pLocation[0].SegmentsCount = pLocation[1].SegmentsCount =
                   tStart[0].SegmentsCount = tStart[1].SegmentsCount =
                   tEnd[0].SegmentsCount = tEnd[1].SegmentsCount = N;
                
                components.Add(dir[0]);
                components.Add(dir[1]);
                components.Add(pLocation[0]);
                components.Add(pLocation[1]);
                components.Add(tStart[0]);
                components.Add(tStart[1]);
                components.Add(tEnd[0]);
                components.Add(tEnd[1]);

                StateVal = stateVal;
            }
            public DirectedStayingChainState(int segmentsCount,
             bool p1CCW, bool p2CCW,
             int p1Loc, int p2Loc,
             int p1TStart, int p2TStart,
             int p1TEnd,
             int p2TEnd)
            {
                N = segmentsCount;

                for (int i = 0; i < 2; ++i)
                {
                    dir[i] = new DirComponent();
                    pLocation[i] = new LocationComponent();
                    tStart[i] = new LocationComponent();
                    tEnd[i] = new LocationComponent();
                }

                dir[0].StateVal = Convert.ToInt32(p1CCW);
                dir[1].StateVal = Convert.ToInt32(p2CCW);

                pLocation[0].StateVal = p1Loc;
                tStart[0].StateVal = p1TStart;
                tEnd[0].StateVal = p1TEnd;
                pLocation[1].StateVal = p2Loc;
                tStart[1].StateVal = p2TStart;
                tEnd[1].StateVal = p2TEnd;

                pLocation[0].SegmentsCount = pLocation[1].SegmentsCount =
                    tStart[0].SegmentsCount = tStart[1].SegmentsCount =
                    tEnd[0].SegmentsCount = tEnd[1].SegmentsCount = N;

                components.Add(dir[0]);
                components.Add(dir[1]);

                components.Add(pLocation[0]);
                components.Add(pLocation[1]);
                components.Add(tStart[0]);
                components.Add(tStart[1]);
                components.Add(tEnd[0]);
                components.Add(tEnd[1]);
            }

            /// <summary>
            /// some combinations are not legal
            /// </summary>
            /// <returns></returns>
            public bool isLegal()
            {
                // make sure direction doesn't contradict territory
                for (int pi = 0; pi < 2; ++pi)
                {
                    if (pLocation[pi].StateVal == tStart[pi].StateVal && pLocation[pi].StateVal != tEnd[pi].StateVal)
                        return false; // tstart must be the oldest point in the territory, so unless territory is of size 1, then location!=start

                    if (pLocation[pi].StateVal == tEnd[pi].StateVal)
                    {
                        // if patroller is in an extreme segment in the territoty, then it must be headed outside 
                        // the territory
                        if (TerritoryDirection(pi) != Dir(pi))
                            return false;
                    }
                }

                if (pLocation[0] == pLocation[1])
                    return false;
                
                // make sure territories don't overlap
                if (isSegmentInPatrollerTerritory(tStart[0].StateVal, 1) ||
                    isSegmentInPatrollerTerritory(tEnd[0].StateVal, 1) ||
                    isSegmentInPatrollerTerritory(tStart[1].StateVal, 0))
                    return false;

               
                return true;
            }
            public bool isSegmentInPatrollerTerritory(int point, int patrollerIdx)
            {
                if (TerritoryDirection(patrollerIdx) == 1)
                    return point >= tStart[patrollerIdx].StateVal && point <= tEnd[patrollerIdx].StateVal;
                return point >= tEnd[patrollerIdx].StateVal || point <= tStart[patrollerIdx].StateVal;
            }
            public static Dictionary<int,int> getLegalStates(int segmentCount)
            {
                Dictionary<int, int> res = new Dictionary<int, int>();
                for (int p1Location = 0; p1Location < segmentCount; ++p1Location)
                for (int p2Location = 0; p2Location < segmentCount; ++p2Location)
                for (int p1StartTerritory = 0; p1StartTerritory < segmentCount; ++p1StartTerritory)
                for (int p1EndTerritory = 0; p1EndTerritory < segmentCount; ++p1EndTerritory)
                for (int p2StartTerritory = 0; p2StartTerritory < segmentCount; ++p2StartTerritory)
                for (int p2EndTerritory = 0; p2EndTerritory < segmentCount; ++p2EndTerritory)
                for (int d1 = 0; d1 < 2; ++d1)
                for (int d2 = 0; d2 < 2; ++d2)
                {
                    DirectedStayingChainState baseState = 
                        new DirectedStayingChainState(segmentCount, d1==1, d2==1, p1Location, p2Location, p1StartTerritory, p2StartTerritory, p1EndTerritory, p2EndTerritory);
                    if (!baseState.isLegal())
                        continue;
                    res[baseState.StateVal] = res.Count;
                }
                return res;
            }
       
            public static int getStateCount(int segmentsCount)
            {
                return new DirectedStayingChainState(segmentsCount, false, false, 0, 0, 0, 0, 0, 0).stateCount();
            }
         
            /// <summary>
            // -1 or 1 (=>CCW or CW resepectively)
            /// </summary>
            public int Dir(int patrollerIdx)
            {
                return dir[patrollerIdx].StateVal.ToDir();
            }

            /// <summary>
            /// 0 to N-1 (segment index, where index increases in CCW direction)
            /// </summary>
            public int Location(int patrollerIdx)
            {
                return pLocation[patrollerIdx].StateVal;
            }

            /// <summary>
            /// 0 to N-1 (segment index, where index increases in CCW direction)
            /// </summary>
            public int TerritoryStart(int patrollerIdx)
            {
                return tStart[patrollerIdx].StateVal;
            }

            /// <summary>
            /// 0 to N-1 (segment index, where index increases in CCW direction)
            /// </summary>
            public int TerritoryEnd(int patrollerIdx)
            {
                return tEnd[patrollerIdx].StateVal;
            }
            
            /// <summary>
            /// 1 if the territory is the CCW segments between P1TerritoryStart and P1TerritoryEnd,
            /// -1 if CW
            /// </summary>
            /// <param name="N"></param>
            /// <returns></returns>
            public int TerritoryDirection(int patrollerIdx)
            {
                //int startToEndDist = MathEx.modDist(N, tStart[patrollerIdx].StateVal, tEnd[patrollerIdx].StateVal);
                //int startToLocDist = MathEx.modDist(N, tStart[patrollerIdx].StateVal, pLocation[patrollerIdx].StateVal);
                //return MathEx.modIsBetween(N, tStart[patrollerIdx].StateVal, tEnd[patrollerIdx].StateVal, pLocation[patrollerIdx].StateVal).ToDir() *
                //    (startToLocDist <= startToEndDist).ToDir();
                return MathEx.modIsBetween(N, tStart[patrollerIdx].StateVal, tEnd[patrollerIdx].StateVal, pLocation[patrollerIdx].StateVal).ToDir();
            }

            public int TerritoryLength(int patrollerIdx)
            {
                if (TerritoryDirection(patrollerIdx) == 1)
                    return tEnd[patrollerIdx].StateVal - tStart[patrollerIdx].StateVal;
                return N - (tEnd[patrollerIdx].StateVal - tStart[patrollerIdx].StateVal);
            }
            
        }
        
        /// <summary>
        /// tells the probability of patrollers changing locations in node 'from'
        /// </summary>
        /// <param name="from"></param>
        /// <param name="stayProb"></param>
        /// <param name="rotateProb"></param>
        /// <param name="forwardProb"></param>
        private void getMovementProbs(DirectedStayingChainState from, 
                                      out RootFuncTreeNode[] stayProb, out RootFuncTreeNode[] rotateProb, out RootFuncTreeNode[] forwardProb)
        {
            stayProb = new RootFuncTreeNode[2];
            rotateProb = new RootFuncTreeNode[2];
            forwardProb = new RootFuncTreeNode[2];

            for (int pi = 0; pi < 2; ++pi)
            {
                int opi = 1 - pi; // other patroller index
                bool forwardAvailable = !from.isSegmentInPatrollerTerritory(from.Location(pi) + from.Dir(pi), opi);
                bool backwardAvailable = !from.isSegmentInPatrollerTerritory(from.Location(pi) + from.Dir(pi), opi);

                if (backwardAvailable)
                    rotateProb[pi] = new RootFuncTreeNode(new ParamValFuncTreeNode(ROTATE));
                else
                    rotateProb[pi] = new RootFuncTreeNode(new ConstantValFuncTreeNode(0));

                if (forwardAvailable)
                    forwardProb[pi] = new RootFuncTreeNode(new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode(ROTATE) - new ParamValFuncTreeNode(STAY));
                else
                    forwardProb[pi] = new RootFuncTreeNode(new ConstantValFuncTreeNode(0));
                
                // probability of staying increases if other options are unavialable
                if (!forwardAvailable && !backwardAvailable)
                    stayProb[pi] = new RootFuncTreeNode(new ConstantValFuncTreeNode(1));
                else if(forwardAvailable)
                    stayProb[pi] = new RootFuncTreeNode(new ParamValFuncTreeNode(ROTATE) + new ParamValFuncTreeNode(STAY));
                else // backwardAvilable
                    stayProb[pi] = new RootFuncTreeNode(new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode(ROTATE));

            }
        }

        /// <summary>
        /// serves getTerritoryChangeProbs().
        /// end state of a patroller within its territory (territory start is 0)
        /// </summary>
        private class TerritoryState
        {
            public int PatrollerLocation { get; set; }
            public int PatrollerDir { get; set; } // -1 or 1
            public int tEnd { get; set; }
            public TerritoryState(DirectedStayingChainState srcState, int patrollerIdx)
            {
                PatrollerDir = srcState.TerritoryDirection(patrollerIdx) * // we always regard territory direction as CCW, so reversing patroller direction isequivalent
                    srcState.Dir(patrollerIdx);
                if (srcState.TerritoryDirection(patrollerIdx) == 1)
                    tEnd = srcState.TerritoryLength(patrollerIdx);
                PatrollerLocation = srcState.Location(patrollerIdx);
            }
        }
        private Dictionary<TerritoryState, RootFuncTreeNode> stateProb = new Dictionary<TerritoryState, RootFuncTreeNode>();

        /// <summary>
        /// tells probability of whether tstart moves towards tend
        /// TODO: change to private
        /// TODO: perhaps this entire function can be calculated with combinatorics instead of O(n^2) work
        /// </summary>
        /// <param name="from"></param>
        /// <param name="startPointStaysProb"></param>
        public void getTerritoryChangeProbs(DirectedStayingChainState from,
                                             out RootFuncTreeNode[] startPointAdvancesProb)
        {
            startPointAdvancesProb = new RootFuncTreeNode[2];
            
            for (int pi = 0; pi < 2; ++pi)
            {
                TerritoryState ts = new TerritoryState(from, pi);
                if (stateProb.TryGetValue(ts, out startPointAdvancesProb[pi]))
                    continue;
                
                bool forwardAvailable = !from.isSegmentInPatrollerTerritory(from.Location(pi) + from.Dir(pi), pi);
                bool backwardAvailable = !from.isSegmentInPatrollerTerritory(from.Location(pi) + from.Dir(pi), pi);

                if(!forwardAvailable && !backwardAvailable)
                {
                    stateProb[ts] = startPointAdvancesProb[pi] = new RootFuncTreeNode(new ConstantValFuncTreeNode(0));
                    continue;
                }

                List<RootFuncTreeNode> occupationProb = new List<RootFuncTreeNode>();

                // we calculate the probability of the patroller of being in any of '0' to 'ts.tEnd-1' segments,
                // N-MinimalUnoccupiedSegments-1 rounds ago(the territory +current location tells where the patroller was in the 
                // latest N-MinimalUnoccupiedSegments) , under the knowledge that the intruder didn't go below 0 or beyond 'tEnd-1',
                // and it reached '0' EXACTLY (not before and not after) N-MinimalUnoccupiedSegments-1 rounds ago.
                // Even slots in the array are for direction -1, and uneven slots for direction 1.
                for (int si = 0; si < 2 * ts.tEnd; ++si)
                    occupationProb.Add(new RootFuncTreeNode(new ConstantValFuncTreeNode(0)));
                occupationProb[2 * ts.PatrollerLocation + ts.PatrollerDir.FromDir()] = new RootFuncTreeNode(new ConstantValFuncTreeNode(1));

                for(int i = 0; i < N-MinimalUnoccupiedSegments-1; ++i)
                {
                    // FIXME: remove debug
                    List<string> tmp = new List<string>();
                    foreach (var v in occupationProb)
                        tmp.Add(v.ToString());


                    List<RootFuncTreeNode> nextOccupationProb =
                        AlgorithmUtils.getRepeatingValueList(occupationProb.Count, () =>
                        { return new RootFuncTreeNode(new ConstantValFuncTreeNode(0)); });

                    // if the patroller is in the edge of territory, it either stays or rotates:

                    //nextOccupationProb[0] = // patroller at the begining,
                    //    occupationProb[0] * new ParamValFuncTreeNode(STAY) + // been there already and stayed
                    //    occupationProb[2] * (new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode(STAY) - new ParamValFuncTreeNode(ROTATE)) + // been near and continuted forward CW
                    //    occupationProb[3] * new ParamValFuncTreeNode(ROTATE); // been near and rotated

                    //nextOccupationProb[2 * ts.tEnd - 1] =
                    //    occupationProb[2 * ts.tEnd - 1] * new ParamValFuncTreeNode(STAY) + // been there already and stayed
                    //    occupationProb[2 * ts.tEnd - 3] * (new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode(STAY) - new ParamValFuncTreeNode(ROTATE)) + // been near and continuted forward CCW
                    //    occupationProb[2 * ts.tEnd - 4] * new ParamValFuncTreeNode(ROTATE); // been near and rotated

                    int startIdx = (i == (N - MinimalUnoccupiedSegments - 2)) ? 0 : 2; // we examine probability of when patroller reaches tStart only once - in the last round (N-MinimalUnoccupiedSegments-2)
                    for (int si = startIdx; si < 2 * ts.tEnd - 2; si+=2) // 
                    {
                        nextOccupationProb[si] = // patroller at the begining,
                        occupationProb[si] * new ParamValFuncTreeNode(STAY) + // been there already and stayed
                        occupationProb[si + 2] * (new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode(STAY) - new ParamValFuncTreeNode(ROTATE)) + // been near and continuted forward CW
                        occupationProb[si + 3] * new ParamValFuncTreeNode(ROTATE); // been near and rotated
                    }

                    startIdx = (i == (N - MinimalUnoccupiedSegments - 2)) ? 1 : 3; // we examine probability of when patroller reaches tStart only once - in the last round (N-MinimalUnoccupiedSegments-2)
                    for (int si = 2 * ts.tEnd - 1; si >= startIdx; si -= 2)
                    {
                        nextOccupationProb[si] =
                           occupationProb[si] * new ParamValFuncTreeNode(STAY) + // been there already and stayed
                           occupationProb[si-2] * (new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode(STAY) - new ParamValFuncTreeNode(ROTATE)) + // been near and continuted forward CCW
                           occupationProb[si-3] * new ParamValFuncTreeNode(ROTATE); // been near and rotated
                    }
                    occupationProb = nextOccupationProb;
                }

                startPointAdvancesProb[pi] = stateProb[ts] = 
                    (occupationProb[0] + occupationProb[1]); // the probability that patroller pi began the path in tStart, exactly N - MinimalUnoccupiedSegments-1 rounds ago
            }
        }
        private MatrixOpTree getDirectedStayingChain()
        {
            var allStates = DirectedStayingChainState.getLegalStates(N);
            MatrixOpTree res = new MatrixOpTree(DirectedStayingChainState.getStateCount(N), DirectedStayingChainState.getStateCount(N));

            foreach (var sv in allStates)
            {
                int fromI = sv.Value; // state index of 'from' node
                var from = new DirectedStayingChainState(N, sv.Key);

                // patroller in 'from' can either stay in place, rotate, or move forward.
                // Additionaly, (with independent probability) its 'tstart' can either stay in place, or advance towards 'tend'.
                // the probabilities below are for p1 and p2
                RootFuncTreeNode[] stayProb;
                RootFuncTreeNode[] rotateProb;
                RootFuncTreeNode[] forwardProb;
                getMovementProbs(from, out stayProb, out rotateProb, out forwardProb);

                RootFuncTreeNode[] startPointAdvancesProb;
                getTerritoryChangeProbs(from, out startPointAdvancesProb);

                



                DirectedStayingChainState to; // every node reachable from 'from'
                int toI; // state index of 'to' node

                // FIXME: this is where I stopped. rewrite this, to include the compact territory
                //to = new DirectedStayingChainState(N, sv.Key);
                //to.
                //toI = allStates[to.StateVal];
                //res[fromI, toI] = new RootFuncTreeNode(new ParamValFuncTreeNode(ROTATE));
                

            }
                

            return res;
        }
    }
}