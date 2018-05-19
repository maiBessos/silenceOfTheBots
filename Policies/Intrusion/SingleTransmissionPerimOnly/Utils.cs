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

namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly
{
    namespace Utils
    {
        public struct PatrollersState
        {
            public object tag; // FIXME: quite dirty. serves APatrollerPolicyProcess implementations
            public int p1, p2; // segment 0 to n-1, where segments are counted clockwise
            public int p1Dir, p2Dir; // -1 CCW, 1 CW

            public override bool Equals(object obj)
            {
                // we ignore tag when checking state equality
                PatrollersState o = (PatrollersState)obj;
                return p1 == o.p1 && p2 == o.p2 && p1Dir == o.p1Dir && p2Dir == o.p2Dir;
            }
            public override int GetHashCode()
            {
                // we ignore tag when comparing states
                return p1.GetHashCode() ^ p2.GetHashCode() ^ p1Dir.GetHashCode() ^ p2Dir.GetHashCode();
            }
            public void updatePatrollersPositions(int p1Moves, int p2Moves, int newP1Dir, int newP2Dir, int circumferencePointCount)
            {
                p1 += newP1Dir * p1Moves;
                p1Dir = newP1Dir;
                p2 += newP2Dir * p2Moves;
                p2Dir = newP2Dir;

                p1 = (p1 + circumferencePointCount) % circumferencePointCount;
                p2 = (p2 + circumferencePointCount) % circumferencePointCount;
            }
//            // FIXMEe debug
//#if DEBUG
//            public int prevDistance;
//            public int prevp1, prevp2;
//            public int prevp1dir, prevp2dor;
//#endif
        }
        
        /// <summary>
        /// represents an observation history of the latest two agents passing by a segment
        /// 
        /// sameAgent = 1 and timeDiff = 0 means the agent rotated in place
        /// </summary>
        public struct IntruderObservationHistory2
        {
            public int firstPassingPatroller, secondPassingPatroller;
            public int timeDiff;
        }
        
        /// <summary>
        /// assumes derived class has a private empty ctor
        /// </summary>
        public abstract class PreTransmissionMovement
        {
            public static List<PreTransmissionMovement> generateMovements(string ConcretePreTransmissionMovementClassName, int itemsCount, IntrusionGameParams prm)
            {
                PreTransmissionMovement empty = 
                    GoE.Utils.ReflectionUtils.constructEmptyCtorType<PreTransmissionMovement>(ConcretePreTransmissionMovementClassName);
                return empty.generateConcreteMovements(itemsCount,prm);
            }
            public abstract Dictionary<string, string> serialize();
            
            protected abstract List<PreTransmissionMovement> generateConcreteMovements(int itemsCount, IntrusionGameParams prm);
        }
       
        public struct PostTransmissionMovementProperties
        {
            public int rotationsCount;
            public int lastPossibleRotationRound;
            public Dictionary<string,string> serialize()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["rotationsCount"] = rotationsCount.ToString();
                res["lastPossibleRotationRound"] = lastPossibleRotationRound.ToString();
                return res;
            }
            /// <summary>
            /// may return null
            /// </summary>
            /// <param name="rand"></param>
            /// <returns></returns>
            public List<int> generateRotations(ThreadSafeRandom rand)
            {
                var patRand =
                       new PatternRandomizer(lastPossibleRotationRound,
                                             rotationsCount);
                patRand.Randomize(rand, true,-1,true);
                var newList = patRand.CurrentlyUsedPoints;

                for (int r = 1; r < newList.Count; ++r)
                    if (newList[r] == newList[r - 1] + 1)
                    {
                        // we don't want to rotations twice in a row - this means patroller actually won't move for 2 rounds
                        return null;
                    }
                return newList;
            }
        }

        public class PostTransmissionMovement
        {
            public int LastReadIdx { get; set; }
            public List<int> RoundsToRotate
            {
                get
                {
                    return roundsToRotate;
                }
                set
                {
                    roundsToRotate = value;
                    hashCode = 0;
                    foreach (int v in roundsToRotate)
                        hashCode ^= v;
                }
            }
            public override int GetHashCode()
            {
                return hashCode;
            }
            public override bool Equals(object obj)
            {
                PostTransmissionMovement other = (PostTransmissionMovement)obj;
                return roundsToRotate.Equals(obj);
            }
            public PostTransmissionMovement(List<int> RoundsToRotate)
            {
                this.RoundsToRotate = RoundsToRotate;
                LastReadIdx = -1;
            }
            public PostTransmissionMovement(PostTransmissionMovement src)
            {
                roundsToRotate = src.roundsToRotate;
                hashCode = src.hashCode;
                LastReadIdx = -1;
            }
            private int hashCode;
            private List<int> roundsToRotate;

            
            public static Dictionary<PostTransmissionMovementProperties, List<PostTransmissionMovement>> generateMovementes(int itemCount, int LastPossibleRotationRound, int MaxRotations)
            {
                
                int absoluteLastPossibleRotationRound; // patrollers never rotate after this (value is 2k+t/2)
                int absoluteMaxRotations; // patrollers never make more rotations than that
                absoluteLastPossibleRotationRound = LastPossibleRotationRound;
                absoluteMaxRotations = MaxRotations;

                ThreadSafeRandom rand = new ThreadSafeRandom();

                var allMovements =
                    new Dictionary<PostTransmissionMovementProperties, List<PostTransmissionMovement>>();
                List<PostTransmissionMovementProperties> props = new List<PostTransmissionMovementProperties>();

                int propCount = (int)Math.Sqrt(itemCount);

                // generate rotation pattern generators:
                while(allMovements.Count < propCount)
                {
                    int rotCount = rand.Next(1, absoluteMaxRotations + 1);

                    PostTransmissionMovementProperties prop = new PostTransmissionMovementProperties()
                    {
                        rotationsCount = rotCount,
                        lastPossibleRotationRound = rand.Next(2 * rotCount, absoluteLastPossibleRotationRound + 1)
                    };

                    if (!allMovements.ContainsKey(prop))
                    {
                        var newList = prop.generateRotations(rand);
                        int retries = 5;
                        while (newList == null && retries-- > 0)
                            newList = prop.generateRotations(rand);
                        if (newList == null)
                            continue;

                        allMovements[prop] = new List<PostTransmissionMovement>();
                        props.Add(prop);
                        allMovements[prop].Add(new PostTransmissionMovement(newList));
                    }
                }
                
                // randomly choose a generator, then use it to create patterns
                for(int moves = 0; moves < itemCount; ++moves)
                {
                    int propIdx = rand.Next(0, props.Count);
                    var prop = props[propIdx];

                    //var patRand = 
                    //    new PatternRandomizer(prop.lastPossibleRotationRound,
                    //                          prop.rotationsCount);
                    //patRand.Randomize(rand, true);
                    //var newList = patRand.CurrentlyUsedPoints;

                    //for (int r = 1; r < newList.Count; ++r)
                    //    if(newList[r] == newList[r-1] + 1 )
                    //    {
                    //        newList = null;
                    //        break; // we don't want to rotations twice in a row - this means patroller actually won't move for 2 rounds
                    //    }
                    var newList = prop.generateRotations(rand);
                    if (newList == null)
                        continue;
                    allMovements[prop].Add( new PostTransmissionMovement(newList));
                }

                return allMovements;
            }
            

        }


        /// <summary>
        /// represents patroller direction (0 or 1)
        /// </summary>
        public class DirComponent : AStateComponent
        {
            public DirComponent() { }
            override public int StateVal { get; set; }
            public override int stateCount()
            {
                return 2;
            }
        }

        // 0 to 'SegmentsCount'
        public class LocationComponent : AStateComponent
        {
            override public int StateVal { get; set; }
            public int SegmentsCount { get; set; }

            public LocationComponent(int segmentsCount = -1)
            {
                SegmentsCount = segmentsCount;
            }

            public override int stateCount()
            {
                return SegmentsCount;
            }
        }
    }
}