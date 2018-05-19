using AForge.Genetic;
using AForge.Math.Random;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils.Extensions;

/// in this file we provide utilities to allow evaders to evolve/coevolve in a genetic algorithm environment.
/// It is assumed the graph has one target that is surrounded by sinks
namespace GoE.GameLogic.EvolutionaryStrategy
{

    namespace EvaderSide
    {
        /// <summary>
        /// a compact world sate description, from evaders' POV
        /// The world state is composed of multiple AreaState instances, and a list of how many evaders are used in each
        /// algorithm type
        /// </summary>
        public class WorldState : ConcreteCompositeChromosome
        {
            public _ShortIndices EvaderCountInAlgCode;// may be used with indexer, to access evader counts

            /// <summary>
            /// world state "summary" also tells how many evaders are allocated for each tasks right now
            /// </summary>
            /// <returns></returns>
            private static ushort[] maxEvadersPerActionType()
            {
                ushort[] arr = new ushort[EvolutionConstants.actionAlgorithmsByCode.Count];
                for(int i = 0; i < EvolutionConstants.actionAlgorithmsByCode.Count; ++i)
                    arr[i] = (ushort)EvolutionConstants.param.A_E.Count;
                return arr;
            }

            
            public void setAreaStates(WorldState src)
            {
                for(int i = 0 ; i< MiscCount; ++i)
                {
                    this[i].set(src[i]);
                }
            }
            public WorldState(int areaStatesCount)
                : base(EvolutionConstants.actionAlgorithmsByCode.Count, maxEvadersPerActionType(), 
                       0, null, 0,
                       EvolutionConstants.valueMutationProb,
                       GoE.Utils.Algorithms.AlgorithmUtils.getRepeatingValueList<IChromosome>(new AreaState(), areaStatesCount))
            {
                
            }

            
            public Dictionary<string, string> getValueMap(ConcreteCompositeChromosome valImpactFactor = null)
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                for (int r = 0; r < statesCount; ++r)
                {
                    
                    var ringRes = this[r].getValueMap(valImpactFactor);
                    ringRes.AddKeyPrefix("Ring" + r.ToString());
                    res.AddRange(ringRes);
                }
                return res;
            }
            public string ToString(ConcreteCompositeChromosome valImpactFactor)
            {
                string res = "";
                for(int r = 0; r < statesCount; ++r)
                {
                    res += "ring " + r.ToString() + ":" + Environment.NewLine + this[r].ToString(valImpactFactor);
                }
                return res;
            }

            private WorldState(ConcreteCompositeChromosome src)
                : base(src) {}

            public int statesCount
            {
                get
                {
                    return MiscCount;
                }
            }
            public AreaState this[int idx]
            {
                get
                {
                    return (AreaState)this[Miscs,idx];
                }
                set
                {
                    this[Miscs, idx] = value;
                }
            }

           public override IChromosome Clone()
           {
                return new WorldState((ConcreteCompositeChromosome)base.Clone());
           }

            public override IChromosome CreateNew()
            {
                WorldState res = new WorldState(MiscCount);
                res.Generate();
                return res;
            }

            /// <summary>
            /// estimates a "distance" between two world states (assumed to have coresponding States array).
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <param name="valImpactFactor">
            /// a chromosome that contains doubles of positive [0,1] weights, 
            /// of size  WorldState.VAL_COUNT, telling how important each property is when evaluating 
            /// the distance
            /// </param>
            /// <returns></returns>
            public static double distanceSqr(WorldState s1, WorldState s2, ConcreteCompositeChromosome valImpactFactor)
            {
                double res = 0;

                for(int sIdx = 0; sIdx < s1.MiscCount; ++sIdx)
                {
                    res += AreaState.distanceSqr((AreaState)s1[Miscs,sIdx], (AreaState)s2[Miscs,sIdx], valImpactFactor);
                }

                for(int i = 0; i < s1.ShortsCount; ++i)
                {
                    res += MathEx.Sqr(valImpactFactor[Doubles,i+AreaState.VAL_COUNT] * (s1[Shorts,i] - s2[Shorts,i]));
                }
                
                return res;
            }     

            public static int VAL_COUNT
            {
                get
                {
                    return AreaState.VAL_COUNT + EvolutionConstants.actionAlgorithmsByCode.Count;
                }
            }
        }
    }
}
