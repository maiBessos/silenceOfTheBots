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
using GoE.Utils.Extensions;

namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly
{
    namespace Utils
    {
        public class DistanceKeepingMethod
        {
            public Dictionary<string, string> serialize()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();

                string val = "";
                for (int i = 0; i < CWDistancesPerProb.Count - 1; ++i)
                    val += CWDistancesPerProb[i].ToString() + ",";
                val += CWDistancesPerProb.Last().ToString();

                res["DistanceDistribution"] = val;
                return res;
            }
            public DistanceKeepingMethod(DistanceKeepingMethod src)
            {
                CWDistancesPerProb = new List<int>(src.CWDistancesPerProb);
            }
            public DistanceKeepingMethod(List<int> DistancesPerProb)
            {
                this.CWDistancesPerProb = DistancesPerProb;
            }
            public override bool Equals(object obj)
            {
                DistanceKeepingMethod other = (DistanceKeepingMethod)obj;
                return distancesPerProb.Equals(other.distancesPerProb);
            }
            public override int GetHashCode()
            {
                return hashCode;
            }
            public List<int> CWDistancesPerProb // each index has a uniform probability of being chosen, and tells the desired distance. the distance is the *clockwise* distance between patroller1 to patroller2
            {
                get { return distancesPerProb; }
                set
                {
                    distancesPerProb = value;
                    hashCode = 0;
                    foreach (int v in distancesPerProb)
                        hashCode ^= v;
                }

            }
            private List<int> distancesPerProb;
            private int hashCode;

            public static List<DistanceKeepingMethod> generateMovements(int itemCount, int pointsCount)
            {

                int segmentsPerDistanceLevel; // we consider ceil(relevantDistancesCount/steps) different possible distances ( relevantDistancesCount= (n/2-1) since 0 and > n/2 are not considered)
                int relevantDistancesCount;
                int generatedItemsCount = itemCount;

                relevantDistancesCount = pointsCount / 2 - 1; // 0 is irrelevant, and >n/2 is unneeded since distance is symmetric
                segmentsPerDistanceLevel = Math.Min(1, (int)Math.Pow(itemCount, 0.33)); // we'd rather test many combinations than a higher resolution
                Random rand = new ThreadSafeRandom().rand;

                HashSet<DistanceKeepingMethod> tmpItems = new HashSet<DistanceKeepingMethod>();
                while (tmpItems.Count < itemCount)
                {
                    int ProbabilityLevelCount = relevantDistancesCount / segmentsPerDistanceLevel;
                    int[] probPerDistance = AlgorithmUtils.getRepeatingValueArr<int>(0, ProbabilityLevelCount);
                    for (int i = 0; i < ProbabilityLevelCount; ++i) // we spread 'ProbabilityLevelCount' "tokens" to decide the probability distribution
                        ++probPerDistance[rand.Next(0, ProbabilityLevelCount)];

                    var distancesPerProb = new List<int>();
                    for (int i = 0; i < ProbabilityLevelCount; ++i)
                    {
                        if (probPerDistance[i] > 0)
                        {
                            // if n=20, segmentsPerDistanceLevel = 3, then there are 3 distance levels, each with 3 segments, 
                            // and the possible distances are 2,5,8
                            for (int k = 0; k < probPerDistance[i]; ++k)
                                distancesPerProb.Add((int)(1 + Math.Floor(segmentsPerDistanceLevel * (i + 0.5))));
                        }
                    }
                    tmpItems.Add(new DistanceKeepingMethod(distancesPerProb));
                }
                return tmpItems.ToList();
            }
        }

        public class DistanceKeepingMethodManager
        {

            public float maxVelocity //  [-maxVelocity,maxVelocity] is the maximal average amount of segments an agent may go ( maxVelocity itself is in [0.1,0.9])
            {
                get; private set;
            }
            public float p // probability of both patrollers to rotate simultenously. FIXME: currently derived from maxVelocity, but should be independent
            {
                get; private set;
            }
            public float c // probability of changing distance - [0.05,0.95]
            {
                get; private set;
            }

            public Dictionary<string, string> serialize()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["rotationProb."] = p.ToString();
                res["changeCWDistanceProb."] = c.ToString();
                res["maxVelocity"] = maxVelocity.ToString();
                return res;
            }

            public static List<DistanceKeepingMethodManager> generateMovements(int itemsCount)
            {
                float probJump; // PreTransmissionMovement.p and PreTransmissionMovement.c get values: 0, probJump, 2probJump,3probJump...,1.0
                int levels;

                levels = (int)Math.Floor(Math.Sqrt(itemsCount));
                probJump = (float)(1.0 / levels);

                List<DistanceKeepingMethodManager> res = new List<DistanceKeepingMethodManager>();
                for (int velIdx = 0; velIdx < levels; ++velIdx)
                {
                    // fixme P is derived from MaxVelocity , but in fact should be independent
                    float MaxVelocity = 0.1f + 0.85f * ((float)velIdx) / (levels - 1);
                    float P = 1.0f - MaxVelocity; //(float)(1 - Math.Pow(0.001, 1.0 / MaxVelocity)); // we find the rotation probability for which the probability of not rotating before MaxVelocity is 0.001 i.e. solve (1 - p) ^ MaxVelocity = 0.001

                    for (int distIdx = 0; distIdx < levels; ++distIdx)
                    {
                        float C = 0.05f + 0.9f * ((float)distIdx) / (levels - 1);
                        res.Add(new DistanceKeepingMethodManager(MaxVelocity, P, C));
                    }
                }
                return res;
            }


            public DistanceKeepingMethodManager(DistanceKeepingMethodManager src)
            {
                maxVelocity = src.maxVelocity;
                p = src.p;
                c = src.c;
            }
            public DistanceKeepingMethodManager(float MaxVelocity, float P, float C)
            {
                maxVelocity = MaxVelocity;
                p = P;
                c = C;
            }
        }

        public class DistanceDistributionPreTransmissionMovement : PreTransmissionMovement
        {
         

            private DistanceDistributionPreTransmissionMovement() { }

            public DistanceKeepingMethod distances;
            public DistanceKeepingMethodManager distancesMgr;

            public DistanceDistributionPreTransmissionMovement(DistanceKeepingMethod Distances,
                DistanceKeepingMethodManager DistancesMgr)
            {
                distancesMgr = new DistanceKeepingMethodManager(DistancesMgr);
                distances = new DistanceKeepingMethod(Distances);
            }

            public override Dictionary<string, string> serialize()
            {
                var res = distances.serialize();
                res.AddRange(distancesMgr.serialize());
                return res;
            }
            
            protected override List<PreTransmissionMovement> generateConcreteMovements(int itemsCount, IntrusionGameParams prm)
            {
                int itemsSqrt = (int)Math.Sqrt(itemsCount);
                var allDistanceMgrs = DistanceKeepingMethodManager.generateMovements(itemsSqrt);
                var allDistances = DistanceKeepingMethod.generateMovements(itemsSqrt, prm.SensitiveAreaPointsCount());
                List<PreTransmissionMovement> res = new List<PreTransmissionMovement>();
                foreach (var m in allDistanceMgrs)
                    foreach (var d in allDistances)
                        res.Add(new DistanceDistributionPreTransmissionMovement(d, m));
                
                return res;
            }
        }

    }
}