using GoE.GameLogic;
using GoE.Policies;
using GoE.UI;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.Utils.Extensions;
using System;
using GoE.AppConstants;
using GoE.Policies.Intrusion.SingleTransmissionPerimOnly;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.Utils.Algorithms;
using GoE.Policies.Intrusion.SingleTransmissionPerimOnly.PhasePatrollerMovement;

namespace GoE
{
    
    //static class Tests
    //{
    //    public static bool runTests = true;

    //    //struct ObservationHistory
    //    //{
    //    //    int[] 
    //    //}
    //    //public static bool testBestObservation()
    //    //{
    //    //    int p1Loc, p2Loc, p1Dir, p2Dir;
    //    //    double stayProb = 0.1, rotateProb = 0.1;
    //    //    int T = 3;
            
    //    //}
    //    public static bool testSimpleMovement()
    //    {
    //        int n = 11;
    //        Random r = new Random();

    //        float repetitions = 500000;
    //        Dictionary<double, Dictionary<int, int>> distancePerP = new Dictionary<double, Dictionary<int, int>>();
    //        for (double pGoTowardsOther = 0.1; pGoTowardsOther < 0.5; pGoTowardsOther += 0.01)
    //        {
    //            int[] pLoc = new int[2] { r.Next() % n, r.Next() % n };
    //            distancePerP[pGoTowardsOther] = new Dictionary<int, int>();
    //            for (int i = 0; i < repetitions; ++i)
    //            {
    //                bool p1GoTowardsOther = r.NextDouble() < pGoTowardsOther;
    //                bool p2GoTowardsOther = r.NextDouble() < pGoTowardsOther;

    //                int pLocMax = 1, pLocMin = 0;
    //                if (pLoc[0] > pLoc[1])
    //                {
    //                    pLocMax = 0;
    //                    pLocMin = 1;
    //                }

    //                if (pLoc[pLocMax] - pLoc[pLocMin] <= n / 2)
    //                {
    //                    if (p1GoTowardsOther)
    //                        pLoc[pLocMax] = (pLoc[pLocMax] - 1 + n) % n;
    //                    else
    //                        pLoc[pLocMax] = (pLoc[pLocMax] + 1) % n;

    //                    if (p2GoTowardsOther)
    //                        pLoc[pLocMin] = (pLoc[pLocMin] + 1) % n;
    //                    else
    //                        pLoc[pLocMin] = (pLoc[pLocMin] - 1 + n) % n;
    //                }
    //                else
    //                {
    //                    if (p1GoTowardsOther)
    //                        pLoc[pLocMax] = (pLoc[pLocMax] + 1) % n;
    //                    else
    //                        pLoc[pLocMax] = (pLoc[pLocMax] - 1 + n) % n;

    //                    if (p2GoTowardsOther)
    //                        pLoc[pLocMin] = (pLoc[pLocMin] - 1 + n) % n;
    //                    else
    //                        pLoc[pLocMin] = (pLoc[pLocMin] + 1) % n;
    //                }

    //                int dist = Math.Min((pLoc[0] - pLoc[1] + n) % n, (pLoc[1] - pLoc[0] + n) % n);
    //                int tmp;
    //                if (distancePerP[pGoTowardsOther].TryGetValue(dist, out tmp))
    //                    distancePerP[pGoTowardsOther][dist]++;
    //                else
    //                    distancePerP[pGoTowardsOther][dist] = 1;
    //            }
    //        }

    //        Dictionary<double, double> probs = new Dictionary<double, double>();
    //        foreach(var d in distancePerP)
    //        {
    //            probs[d.Key] = d.Value[4] / repetitions;
    //        }

    //        return true;
    //    }

    //    #region testDistributions
    //    public struct CircumferencePosition // serves testDistributions()
    //    {
    //        public CircumferencePosition(int SegmentsCount, int Position = 0)
    //        {
    //            position = Position;
    //            segmentsCount = SegmentsCount;
    //        }
    //        public int position, segmentsCount;
    //        public static CircumferencePosition operator+(CircumferencePosition pos, int diff)
    //        {
    //            pos.position = (pos.position + diff + pos.segmentsCount) % pos.segmentsCount;
    //            return pos;
    //        }
    //        public static CircumferencePosition operator-(CircumferencePosition pos, int diff)
    //        {
    //            return pos + (-diff);
    //        }
    //        /// <summary>
    //        /// tells how many segments to add in order to reach from rhs to lhs
    //        /// </summary>
    //        /// <param name="rhs"></param>
    //        /// <param name="lhs"></param>
    //        /// <returns></returns>
    //        public static int operator-(CircumferencePosition lhs, CircumferencePosition rhs)
    //        {
    //            return MathEx.modDist(rhs.segmentsCount, rhs.position, lhs.position);
    //        }
    //        public int dist(CircumferencePosition to)
    //        {
    //            return Math.Min(MathEx.modDist(segmentsCount, position, to.position),
    //                            MathEx.modDist(segmentsCount, to.position, position));
    //        }
    //        // tells if going to p1->this->p2 is a clockwise walk (i.e. only increasing positions)
    //        public bool isCW(CircumferencePosition p1, CircumferencePosition p2)
    //        {
    //            return MathEx.modIsBetween(segmentsCount, p1.position, p2.position, position);
    //        }
    //    }

    //    // returns targets with dist()>=minDist
    //    public static void chooseUniformPoints(int segCount, int minDist, out CircumferencePosition target1, out CircumferencePosition target2)
    //    {
    //        Random rand = new Random();
    //        target1 = new CircumferencePosition(segCount);
    //        target2 = new CircumferencePosition(segCount);

    //        int dist = minDist + rand.Next() % (segCount - 2*minDist);
    //        int pos = rand.Next() % segCount;
    //        target1.position = pos;
    //        target2.position = (pos + dist) % segCount;
    //        //target1.position = rand.Next() % segCount;
    //        //target2.position = (target1.position + minDist + 
    //        //    (rand.Next() % (segCount- 2* minDist))) % segCount;

    //        if (target1.position == 0 && target2.position == 5)
    //        {
    //            int a = 0;
    //        }
    //        if (target1.dist(target2) < minDist)
    //        {
    //            int a = 0;
    //        }
    //    }
    //    public struct Command
    //    {
    //        public int from1, from2, to1, to2;
    //    }
    //    public struct SampledDistance
    //    {
    //        public int distance; // distance between patrollers
    //        public int timestep; // time since transition began
    //    }

    //    private static void setStepsOppositeDirection(out int p1ToAdd, out int p2ToAdd,
    //                                CircumferencePosition p1Location,
    //                                CircumferencePosition p2Location,
    //                                CircumferencePosition target1,
    //                                CircumferencePosition target2)
    //    {
    //        if (target1.isCW(p1Location, p2Location) && target2.isCW(p1Location, p2Location))
    //        {

    //            if (target1.isCW(p1Location, target2))
    //            {
    //                // situation is  p1->target1->target2->p2
    //                p1ToAdd = p1Location.dist(target1); // go CW
    //                p2ToAdd = -p2Location.dist(target2); // go CC
    //            }
    //            else
    //            {
    //                // situation is p1->target2->target1->p2
    //                p1ToAdd = p1Location.dist(target2); // go CW
    //                p2ToAdd = -p2Location.dist(target1); // go CC
    //            }
    //        }
    //        else
    //        {
    //            if (target1.isCW(p2Location, target2))
    //            {
    //                // situation is  p2->target1->target2->p1
    //                p1ToAdd = -p1Location.dist(target2); // go CC
    //                p2ToAdd = p2Location.dist(target1); // go CW
    //            }
    //            else
    //            {
    //                // situation is  p2->target2->target1->p1
    //                p1ToAdd = -p1Location.dist(target1); // go CC
    //                p2ToAdd = p2Location.dist(target2); // go CW
    //            }
    //        }
    //        if (p1ToAdd >= 15 || p2ToAdd >= 15)
    //        {
    //            int a = 0;
    //        }
    //    }
    //    private static void setStepsSameDirection(out int p1ToAdd, out int p2ToAdd, 
    //                                 CircumferencePosition p1Location, 
    //                                 CircumferencePosition p2Location, 
    //                                 CircumferencePosition target1, 
    //                                 CircumferencePosition target2)
    //    {
    //        if (p1Location.position == target1.position)
    //        {
    //            p1ToAdd = 0;
    //            if (target2.isCW(p2Location, p1Location))
    //                p2ToAdd = (target2 - p2Location);
    //            else
    //                p2ToAdd = -(p2Location - target2);
    //        }
    //        else if (p2Location.position == target2.position)
    //        {
    //            p2ToAdd = 0;
    //            if (target1.isCW(p1Location, p2Location))
    //                p1ToAdd = (target1 - p1Location);
    //            else
    //                p1ToAdd = -(p1Location - target1);
    //        }
    //        else
    //        {
    //            if (target1.isCW(p1Location, p2Location))
    //            {
    //                p1ToAdd = (target1 - p1Location);
    //                p2ToAdd = (target2 - p2Location);
    //            }
    //            else
    //            {
    //                p1ToAdd = -(p1Location - target1);
    //                p2ToAdd = -(p2Location - target2);
    //            }
    //        }

            
    //    }

    //    /// <summary>
    //    /// for the next T_SWAP steps, this returns an array that tells when to delay and when not to.
    //    /// 
    //    /// </summary>
    //    /// <param name="N"></param>
    //    /// <param name="delay"></param>
    //    /// <returns></returns>
    //    private static void spreadDelay(int T_SWAP, int delayCount1, int delayCount2, out List<bool> delays1, out List<bool> delays2)
    //    {
    //        // current implementation : 
    //        // 1)if all delays are at the begining, then when the patroller starts moving - an observer can decude where the patroller
    //        // is heading, and decide whether it wants to intrude   
    //        // 2) similarly, if all delays are at the end, the observer may deduce where the patroller came from, and it knows the other
    //        // patroller can't go there in the transition
    //        // solution (implemented here): choose (up to) two segments along the way, and delay only on them
            

    //        int segCount1 = T_SWAP - delayCount1 + 1;
    //        int segCount2 = T_SWAP - delayCount2 + 1;
    //        delays1 = new List<bool>(T_SWAP);
    //        delays2 = new List<bool>(T_SWAP);

    //        Random rand = new Random();

    //        int s11=0, s12=0, s21 = 0, s22 = 0;
    //        if (segCount1 > 1)
    //        {
    //            // choose 2 segments to delay for p1
    //            s11 = rand.Next() % segCount1;
    //            s12 = (s11 + rand.Next() % (segCount1 - 1)) % segCount1;
    //            // choose proportional 2 segments to delay also for p2 (proportional, to insure the patrollers never get too close to each other)
    //            s21 = (int)Math.Round((((float)s11) / segCount1) * (segCount2-1));
    //            s22 = (int)Math.Round((((float)s12) / segCount1) * (segCount2-1));
    //        }
    //        else if(segCount2 > 1)
    //        {
    //            s21 = rand.Next() % segCount2;
    //            s22 = (s21 + rand.Next() % (segCount2 - 1)) % segCount2;
    //        }

    //        if (s11 > s12)
    //            AlgorithmUtils.Swap(ref s11, ref s12);
    //        if (s21 > s22)
    //            AlgorithmUtils.Swap(ref s21, ref s22);


    //        double delayFactor = rand.NextDouble();
    //        int delay11 = (int)Math.Round(delayFactor * delayCount1);
    //        int delay12 = delayCount1 - delay11;

    //        int delay21 = (int)Math.Round(delayFactor * delayCount2);
    //        int delay22 = delayCount2 - delay21;

    //        delays1.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(false, s11));
    //        delays1.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(true, delay11));
    //        delays1.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(false, s12-s11));
    //        delays1.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(true, delay12));
    //        delays1.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(false, segCount1 - s12 - 1));

    //        delays2.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(false, s21));
    //        delays2.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(true, delay21));
    //        delays2.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(false, s22 - s21));
    //        delays2.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(true, delay22));
    //        delays2.AddRange(AlgorithmUtils.getRepeatingValueList<bool>(false, segCount2 - s22 - 1));
    //    }

    //    public static bool testDistributions()
    //    {
    //        int T = 2;
    //        int N = 30;
    //        int T_SWAP = N - 2 * T - 1;

    //        ThreadSafeRandom rand = new ThreadSafeRandom();
    //        CircumferencePosition p1Location = new CircumferencePosition(N,0), 
    //                              p2Location = new CircumferencePosition(N, N/2),
    //                              target1, 
    //                              target2;

    //        int measurements = 10000000;

    //        Dictionary<Tuple<int, int>, int> locationCombinationPracticeCount = new Dictionary<Tuple<int, int>, int>();
    //        Dictionary<int, Dictionary<int, int>> locationCombinationCount = new Dictionary<int, Dictionary<int, int>>();
    //        Dictionary<SampledDistance, int> occourancesPerTimeDistance = new Dictionary<SampledDistance, int>();
    //        Dictionary<int, int> occourancesPerDistance = new Dictionary<int, int>();
    //        for (int i = 0; i < N; ++i)
    //            occourancesPerDistance[i] = 0;

    //        // otherPatrollerPerObservation[i], this tells how many times a patroller is in location 1,2,3,4,5 , if the other patroller
    //        // just got observed in segment i
    //        Dictionary<int, SortedDictionary<int, int>> otherPatrollerPerObservation = new Dictionary<int, SortedDictionary<int, int>>();
    //        for (int i = 0; i < N; ++i)
    //        {
    //            otherPatrollerPerObservation[i] = new SortedDictionary<int, int>();
    //            locationCombinationCount[i] = new Dictionary<int, int>();
    //        }


    //        while (measurements-- > 0)
    //        {
    //            chooseUniformPoints(N, 2 * T + 1, out target1, out target2);
    //            locationCombinationCount[target1.position].addIfExists(target2.position, 1, 1);


    //            int p1ToAdd, p2ToAdd;

    //            bool p1LocationSwap = p1Location.position == target1.position ||
    //                                  p1Location.position == target2.position; ;
    //            bool p2LocationSwap = p2Location.position == target1.position ||
    //                                  p2Location.position == target2.position;

    //            bool isTargetInner = (target1.isCW(p1Location, p2Location) && target2.isCW(p1Location, p2Location)) ||// tells if both targets are in the same area
    //                                 (!target1.isCW(p1Location, p2Location) && !target2.isCW(p1Location, p2Location));

    //            int initialP1 = p1Location.position;
    //            int initialP2 = p2Location.position;
    //            int initialP1ToAdd, initialP2ToAdd;
    //            if (p1LocationSwap || p2LocationSwap || !isTargetInner)
    //            {
    //                if(rand.Next()==1)
    //                {
    //                    // p1 takes target1 , p2 takes target2
    //                    setStepsSameDirection(out p1ToAdd, out p2ToAdd, p1Location, p2Location, target1, target2);
    //                }
    //                else
    //                {
    //                    // p1 takes target2 , p2 takes target1
    //                    setStepsSameDirection(out p1ToAdd, out p2ToAdd, p1Location, p2Location, target2, target1);
    //                }
    //            }
    //            else
    //            {
    //                setStepsOppositeDirection(out p1ToAdd, out p2ToAdd, p1Location, p2Location, target1, target2);
    //            }

    //            initialP1ToAdd = p1ToAdd;
    //            initialP2ToAdd = p2ToAdd;

    //            if (p1ToAdd > N-2*T-1 || p2ToAdd > N - 2 * T - 1)
    //            {
    //                int a = 0;
    //            }


    //            int add1 = 0;
    //            if (p1ToAdd > 0)
    //                add1 = 1;
    //            if (p1ToAdd < 0)
    //                add1 = -1;

    //            int add2 = 0;
    //            if (p2ToAdd > 0)
    //                add2 = 1;
    //            if (p2ToAdd < 0)
    //                add2 = -1;

    //            int time = 0;
    //            //Utils.Algorithms.PatternRandomizer delays1 = new Utils.Algorithms.PatternRandomizer(T_SWAP, T_SWAP - Math.Abs(p1ToAdd));
    //            //Utils.Algorithms.PatternRandomizer delays2 = new Utils.Algorithms.PatternRandomizer(T_SWAP, T_SWAP - Math.Abs(p2ToAdd));
    //            //delays1.Randomize(rand,true,-1,true);
    //            //delays2.Randomize(rand, true, -1, true);
    //            List<bool> delays1, delays2;
    //            spreadDelay(T_SWAP, 
    //                        T_SWAP - Math.Abs(p1ToAdd), 
    //                        T_SWAP - Math.Abs(p2ToAdd), 
    //                        out delays1, out delays2);
                
    //            int p1ToWait = T_SWAP - Math.Abs(p1ToAdd);
    //            int p2ToWait = T_SWAP - Math.Abs(p2ToAdd);
    //            while (time < T_SWAP)
    //            {
    //                if (delays1[time] == false)
    //                    p1Location += add1;
    //                if (delays2[time] == false)
    //                    p2Location += add2;
    //                //if (!delays1.CurrentlyUsedPoints.Contains(time))
    //                //{
    //                //    p1Location += add1;
    //                //    p1ToAdd -= add1;
    //                //}
    //                //if (!delays2.CurrentlyUsedPoints.Contains(time))
    //                //{
    //                //    p2Location += add1;
    //                //    p2ToAdd -= add2;
    //                //}

    //                //if (p1ToWait != 0)
    //                //    p1ToWait -= 1;
    //                //else
    //                //    p1Location += add1;

    //                //if (p2ToWait != 0)
    //                //    p2ToWait -= 1;
    //                //else
    //                //    p2Location += add2;

    //                if (p1Location.dist(p2Location) <= 2*T)
    //                {
    //                    int a = 0;
    //                }
    //                else if (p1Location.dist(p2Location) > N/2)
    //                {
    //                    int a = 0;
    //                }


    //                //occourancesPerDistance.addIfExists(p1Location.dist(p2Location),1,1);
    //                //occourancesPerTimeDistance.addIfExists(new SampledDistance() { distance = p1Location.dist(p2Location), timestep = time }, 1,1);

    //                ++time;
    //            }
    //            otherPatrollerPerObservation[p1Location.position].addIfExists(p2Location.position, 1,1);
    //            //locationCombinationPracticeCount.addIfExists(Tuple.Create(p1Location.position, p2Location.position), 1, 1);


    //        }

    //        return true;
    //    }
    //    #endregion

    //    public static bool testTransitions()
    //    {
    //        TerritoryWithRecentHistoryMChainPreTranPolicy p = new TerritoryWithRecentHistoryMChainPreTranPolicy();
    //        p.getChain();

    //        return true;
    //    }
    //    public static bool testDirectedStayingChainState()
    //    {

    //        List<TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState> arr = 
    //            new List<TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState>();
    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, true, false, 0, 0, 0, 0, 0, 0));
    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, true, 0, 0, 0, 0, 0, 0));
    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, true, true, 0, 0, 0, 0, 0, 0));

    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 1, 0, 0, 0, 0, 0));
    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 0, 1, 0, 0, 0, 0));
    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 1, 1, 0, 0, 0, 0));

    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 0, 0, 1, 0, 0, 0));
    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 0, 0, 0, 1, 0, 0));
    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 0, 0, 1, 1, 0, 0));

    //        arr.Add(new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 0, 0, 0, 0, 0, 1));

    //        int v = new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 1, 2, 3, 4, 4, 4).StateVal;
    //        TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState s =
    //            new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(5, false, false, 0, 0, 0, 0, 0, 0);
    //        s.StateVal = v;

    //        List<int> vals = new List<int>();
    //        foreach (var a in arr)
    //            vals.Add(a.StateVal);

    //        TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState dirTester =
    //            new TerritoryWithRecentHistoryMChainPreTranPolicy.DirectedStayingChainState(10, true, true, 1,3,8,3,2,5);
    //        int d1 = dirTester.TerritoryDirection(0);
    //        int d2 = dirTester.TerritoryDirection(1);

    //        return true;
    //    }


    //    //public bool CheckConstantsIllegalChar()
    //    //{
    //    //    AppConstants.AppConstant.ILLEGAL_CHAR
    //    //}
    //    //public bool CheckConstantsCollision()
    //    //{
    //    //Type[] constantGroups = Utils.ReflectionUtils.GetTypesInNamespace("AppConstants");
    //    //foreach(Type t in constantGroups)
    //    //    t.GetMembers()
    //    //}

    //    /// <summary>
    //    /// runs theoretical, optimistic and pessimistic optimizers and makes sure 
    //    /// performance improves with each value
    //    /// </summary>
    //    /// <param name="args"></param>
    //    public static void checkOptimizersSainity(ProcessArgumentList args)
    //    {
    //        checkOptimizerSainity(args, "PatrolAndPursuitOptimizerTheory");
    //        checkOptimizerSainity(args, "PatrolAndPursuitOptimizerPracticalPessimistic");
    //        checkOptimizerSainity(args, "PatrolAndPursuitOptimizerPractical");
    //    }


    //    class SequenceComparer<T> : IEqualityComparer<IEnumerable<T>>
    //    {
    //        public bool Equals(IEnumerable<T> seq1, IEnumerable<T> seq2)
    //        {
    //            return seq1.SequenceEqual(seq2);
    //        }

    //        public int GetHashCode(IEnumerable<T> seq)
    //        {
    //            int hash = 1234567;
    //            foreach (T elem in seq)
    //                hash = hash * 37 + elem.GetHashCode();
    //            return hash;
    //        }
    //    }


    //    private static int LOC_COUNT = 15;
    //    public static void getUniqeTimeLocationsCount()
    //    {
            
    //        Dictionary<int, int> timeLocCountPerN = new Dictionary<int, int>();
    //        for (int i = 5; i < 45; i += 2)
    //        {
    //            LOC_COUNT = i;

    //            List<int> locations = new List<int>(Enumerable.Repeat(0, LOC_COUNT));
    //            HashSet<List<int>> timeLocations = new HashSet<List<int>>(new SequenceComparer<int>());
    //            List<int> currentPath = new List<int>();
    //            currentPath.Add(0);
    //            timeLocCountPerN[i] = countBothWays(timeLocations, currentPath, LOC_COUNT / 2);
    //        }

    //        string outs = "";
    //        foreach (var v in timeLocCountPerN)
    //            outs += v.Value + ",";

    //        InputBox.ShowDialog("res", "res", outs);
    //    }

    //    private static int countBothWays(HashSet<List<int>> allTimeLocations, List<int> currentPath, int remainingPathLen)
    //    {

    //        if (remainingPathLen > 0)
    //        {
    //            List<int> pathRight = new List<int>(currentPath);
    //            pathRight.Add(currentPath.Last() + 1);
    //            List<int> pathLeft = new List<int>(currentPath);
    //            pathLeft.Add(currentPath.Last() -1);
    //            return countBothWays(allTimeLocations, pathRight, remainingPathLen - 1) +
    //                   countBothWays(allTimeLocations, pathLeft, remainingPathLen - 1);
    //        }

    //        List<int> timeLoc = new List<int>(Enumerable.Repeat(-1, LOC_COUNT));
    //        for (int i = 0; i < currentPath.Count; ++i)
    //            timeLoc[currentPath[i]+ LOC_COUNT / 2] = i;

    //        return allTimeLocations.Add(timeLoc)?(1):(0);

    //    }

    //    private class ExtractedVals
    //    {
    //        public float PresentableReward, TransmitReward, EscapeReward;
    //        public float lEscape, simulteneousTransmissions;
    //        public ExtractedVals(Dictionary<string,string> vals)
    //        {
    //            PresentableReward = float.Parse(vals[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY]);
    //            TransmitReward = float.Parse(vals[AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.TRANSMIT_REWARD.key]);
    //            simulteneousTransmissions = float.Parse(vals[AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS.key]);
    //            EscapeReward = float.Parse(vals[AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.ESCAPE_REWARD.key]);
    //            lEscape = float.Parse(vals[AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE.key]);
    //        }
    //    }
    //    private static void checkOptimizerSainity(ProcessArgumentList args,string optimizer)
    //    {
    //        GridGameGraph g;
    //        g = new GridGameGraph(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(args[0]));
    //        ParallelOptions po = new ParallelOptions();
    //        po.MaxDegreeOfParallelism = Int32.Parse(AppConstants.AppArgumentKeys.THREAD_COUNT.tryRead(args[0]));
    //        List<ProcessOutput> res = new List<ProcessOutput>();

    //        Dictionary<string, string> prevTmpVals = null;
    //        for (int i = 0; i < args.ValuesCount; ++i)
    //        {
    //            var tmpVals = args[i];
    //            tmpVals[AppConstants.AppArgumentKeys.SIMULATION_REPETETION_COUNT.key] = "1";
    //            tmpVals[AppConstants.AppArgumentKeys.POLICY_OPTIMIZER.key] = optimizer;
    //            res.Add(SimProcess.processParams(po, tmpVals, g));

    //            if (i > 0)
    //            {
    //                ProcessOutput prev = res[res.Count - 2];
    //                ProcessOutput last = res.Last();
    //                ExtractedVals prevVals = new ExtractedVals(prev.optimizerOutput);
    //                ExtractedVals lastVals = new ExtractedVals(last.optimizerOutput);

    //                if (prevVals.PresentableReward < lastVals.PresentableReward ||
    //                    prevVals.TransmitReward < lastVals.TransmitReward||
    //                    prevVals.EscapeReward < lastVals.EscapeReward)
    //                {   
    //                    SimProcess.processParams(po, prevTmpVals, g);
    //                    SimProcess.processParams(po, tmpVals, g);
    //                }
    //            }
    //            prevTmpVals = new Dictionary<string, string>(tmpVals);

    //        }
    //    }

    //    struct POMDPParam
    //    {
            
    //        public PatrollersPOMDPKnownIntrusionTimeInPhaseParams param;
    //        public override string ToString()
    //        {
    //            return "S" + param.segCount.ToString() + "T" + param.intrusionTimeLength.ToString() +
    //            "dsd" + param.delaySegDistance.ToString() + "O" + param.observerSeg.ToString() + "pi" + param.progressAtIntrusion.ToString() +
    //            "trT" + param.intrusionTrigger.Time.ToString() + "trD" + param.intrusionTrigger.ObservedDelay.ToString() + "trSwp" + (param.intrusionTrigger.DidObserveBothPatrollers ? 1 : 0).ToString();
    //        }
    //    }
    //    public static void writePOMDP()
    //    {
    //        //PatrollersHMM pGen = new PatrollersHMM(6, 1, 1, 0, 3);
    //        //pGen.sanityTest();
    //        //var pfile = pGen.writePOMDP();
    //        //File.WriteAllLines("c:\\n12T1.txt", pfile);

    //        //PatrollersHMMKnownIntrusionTimeInPhaseDeprecated pGen = 
    //        //    new PatrollersHMMKnownIntrusionTimeInPhaseDeprecated(11, 1, 1, 0, 3,0);
    //        //pGen.sanityTest();
    //        //var pfile = pGen.writePOMDP();
    //        //File.WriteAllLines("c:\\n11T1.txt", pfile);
    //        POMDPParam p1 = new POMDPParam()
    //        {
    //            param = new PatrollersPOMDPKnownIntrusionTimeInPhaseParams()
    //            {
    //                delaySegDistance = 2,
    //                intruderSeg = 0,
    //                intrusionTimeLength = 1,
    //                intrusionTrigger = new Observation() { DidObserveBothPatrollers = false, ObservedDelay = 1, Time = 1 },
    //                observerSeg = 4,
    //                progressAtIntrusion = 2,
    //                segCount = 13
    //            }
    //        };
    //        //POMDPParam p2 = new POMDPParam()
    //        //{
    //        //    param = new PatrollersPOMDPKnownIntrusionTimeInPhaseParams()
    //        //    {
    //        //        delaySegDistance = 2,
    //        //        intruderSeg = 0,
    //        //        intrusionTimeLength = 1,
    //        //        intrusionTrigger = new Observation() { DidObserveBothPatrollers = false, ObservedDelay = 1, Time = 1 },
    //        //        observerSeg = 2,
    //        //        progressAtIntrusion = 1,
    //        //        segCount = 8
    //        //    }
    //        //};

    //        PatrollersPOMDPKnownIntrusionTimeInPhase pGen = new PatrollersPOMDPKnownIntrusionTimeInPhase(p1.param);
    //        File.WriteAllLines(p1.ToString() + ".txt", pGen.writePOMDP());
    //        //File.WriteAllLines(p2.ToString() + ".txt", pGen.writePOMDP());
    //    }
    //}
}