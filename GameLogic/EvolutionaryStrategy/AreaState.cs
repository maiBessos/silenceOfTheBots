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
        /// characterizes a certain area (often a range of circles around the target) from evader's POV
        /// 
        /// NOTE: regarding avg distance and distance variance:
        /// we sort angles of agents in the circle.
        /// we want the distance between each agents will maximized (average) - so they are "well scatterred".
        /// also, if variance of distances is high, it's a little bad (although less important than avg.)
        /// imagine the best case: perfect scatter, means max. average distance, 0 variance. 
        /// another, slightly less good case is two distant clusters. the average is much smaller, but variance is still small
        /// a worse case is a cluster and only 1 distant point. same average as two clusters, but higher variance
        /// and wven worst case is just one cluster, where we have minimal average (and also 0 variance, but average is more important)
        /// </summary>
        public class AreaState : ConcreteCompositeChromosome
        {
            public enum ShortsIndex : int
            {
                evaderCount = 0,
                uniqueDataUnits, // data stored in evaders memory that is still not in sink
                estimatedPursuersCount
            }
            public enum DoublesIndex : int
            {
                evadersAvgDistanceVariance = 0,
                estimatedPursuersAvgDistance, // we sort agents by angle, then check average distance from one agent to the next
                // TODO: consider adding another criteria: variance of distances (the higher it is, the less well-scattered are the agents)
                dirtySetPointsRatio, //[0,1], tells how much of the of points in the area, the pursuers know an evader may be, in the last EvolutionConstants.assumedMaxPursuitRoundCount rounds
                evaderCountInDirtySetPercent //[0,1], refers only the area that is dirty, and tells how many of it's points are populated by evaders (note: this property is misleading for evaders that stand in the same point, but no reason for that anyway)
            }

            public void set(AreaState src)
            {
                for (int i = 0; i < ShortsCount; ++i)
                    this[Shorts, i] = src[Shorts, i];
                for (int i = 0; i < DoublesCount; ++i)
                    this[Doubles, i] = src[Doubles, i];
            }
            public Dictionary<string, string> getValueMap(ConcreteCompositeChromosome valImpactFactor = null)
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                
                if (valImpactFactor != null)
                {
                    res["EveCnt"] = this[Shorts, (int)ShortsIndex.evaderCount].ToString() + " X " + valImpactFactor[Doubles, 0].ToString("0.00");
                    res["PrsCnt"] = this[Shorts, (int)ShortsIndex.estimatedPursuersCount].ToString() + " X " + valImpactFactor[Doubles, 2].ToString("0.00");
                    res["EveDistVar"] = this[Doubles, (int)DoublesIndex.evadersAvgDistanceVariance].ToString("00.0") + " X " + valImpactFactor[Doubles, 3].ToString("0.00");
                    res["PrsDst"] = this[Doubles, (int)DoublesIndex.estimatedPursuersAvgDistance].ToString("00.0") + " X " + valImpactFactor[Doubles, 4].ToString("0.00");
                    res["DirtyRatio"] = this[Doubles, (int)DoublesIndex.dirtySetPointsRatio].ToString("00.0") + " X " + valImpactFactor[Doubles, 5].ToString("0.00");
                    res["EvesInDirty%"] = (100.0 * this[Doubles, (int)DoublesIndex.dirtySetPointsRatio]).ToString("000.0") + " X " + valImpactFactor[Doubles, 6].ToString("0.00");
                }
                else
                {
                    res["EveCnt"] = this[Shorts, (int)ShortsIndex.evaderCount].ToString();
                    res["PrsCnt"] = this[Shorts, (int)ShortsIndex.estimatedPursuersCount].ToString();
                    res["EveDistVar"] = this[Doubles, (int)DoublesIndex.evadersAvgDistanceVariance].ToString("00.0");
                    res["PrsDst"] = this[Doubles, (int)DoublesIndex.estimatedPursuersAvgDistance].ToString("00.0");
                    res["DirtyRatio"] = this[Doubles, (int)DoublesIndex.dirtySetPointsRatio].ToString("00.0");
                    res["EvesInDirty%"] = (100.0 * this[Doubles, (int)DoublesIndex.dirtySetPointsRatio]).ToString("000.0");
                }

                return res;
            }
            public string ToString(ConcreteCompositeChromosome valImpactFactor)
            {
                string res = "";
                res += "EveCnt:" + this[Shorts, (int)ShortsIndex.evaderCount].ToString() + " X " + valImpactFactor[Doubles,0].ToString("0.00") + " |" +
                       "NewData:" + this[Shorts, (int)ShortsIndex.uniqueDataUnits].ToString() + " X " + valImpactFactor[Doubles, 1].ToString("0.00") + " |" +
                       "PrsCnt:" + this[Shorts, (int)ShortsIndex.estimatedPursuersCount].ToString() + " X " + valImpactFactor[Doubles, 2].ToString("0.00") + " |" + 
                       "EveDst:" + this[Doubles, (int)DoublesIndex.evadersAvgDistanceVariance].ToString("00.0") + " X " + valImpactFactor[Doubles, 3].ToString("0.00") + " |" +
                       "PrsDst:" + this[Doubles, (int)DoublesIndex.estimatedPursuersAvgDistance].ToString("00.0") + " X " + valImpactFactor[Doubles, 4].ToString("0.00") + " |" +
                       "DirtyRatio:" + this[Doubles, (int)DoublesIndex.dirtySetPointsRatio].ToString("00.0") + " X " + valImpactFactor[Doubles, 5].ToString("0.00") + " |" +
                       "EvesInDirty%:" + (100.0 * this[Doubles, (int)DoublesIndex.dirtySetPointsRatio]).ToString("000.0") + " X " + valImpactFactor[Doubles,6].ToString("0.00") + Environment.NewLine;

                return res;
            }
            /// <summary>
            /// tells the maximal values that may return from getDistanceSqr() calls
            /// </summary>
            /// <returns></returns>
            public static double maxAreaStateSqrDistance()
            {
                ushort[] ushorts = getMaxUshorts();
                double[] doubles = getMaxDoubles();
                double res = 0;
                for (int i = 0; i < AreaState.SHORTS_VAL_COUNT; ++i)
                    res += MathEx.Sqr(ushorts[i]);
                for (int i = 0; i < AreaState.DOUBLES_VAL_COUNT; ++i)
                    res += MathEx.Sqr(doubles[i]);
                return res;
            }
            
            /// <summary>
            /// estimates a "distance" between two area states (assumed to represent the same area)
            /// </summary>
            /// <param name="s1"></param>
            /// <param name="s2"></param>
            /// <param name="valImpactFactor">
            /// chromosome with doubles array of positive [0,1] weights, of size  AreaState.VAL_COUNT, telling how important each property is when evaluating 
            /// the distance
            /// </param>
            /// <returns></returns>
            public static double distanceSqr(AreaState s1, AreaState s2, ConcreteCompositeChromosome valImpactFactor)
            {
                double res = 0;
                for (int i = 0; i < AreaState.SHORTS_VAL_COUNT; ++i)
                    res += MathEx.Sqr(valImpactFactor[Doubles,i] * (s1[Shorts, i] - s2[Shorts, i]));
                for (int i = 0; i < AreaState.DOUBLES_VAL_COUNT; ++i)
                    res += MathEx.Sqr(valImpactFactor[Doubles, AreaState.SHORTS_VAL_COUNT + i] * (s1[Doubles, i] - s2[Doubles, i]));
                return res;
            }

            private static ushort[] getMaxUshorts()
            {
                return new ushort[] { (ushort)EvolutionConstants.param.A_E.Count, 
                                      (ushort)EvolutionConstants.MaxSimulationRoundCount, 
                                      (ushort)EvolutionConstants.param.A_P.Count };
            }
            private static double[] getMaxDoubles()
            {
                return new double[] {2 * Math.Sqrt(EvolutionConstants.graph.Nodes.Count), // max distance is two most extreme of maximal ring in the graph (we assume square graph)
                                     2 * Math.Sqrt(EvolutionConstants.graph.Nodes.Count), 
                                     1, 
                                     1 };
            }
            public AreaState()
                : base(SHORTS_VAL_COUNT, getMaxUshorts(),
                       DOUBLES_VAL_COUNT,getMaxDoubles(),11,
                       EvolutionConstants.valueMutationProb,
                       null)
            { 
            }

            private AreaState(ConcreteCompositeChromosome src) 
                : base(src)
            {
            }

            public override IChromosome Clone()
            {
                return new AreaState((ConcreteCompositeChromosome)base.Clone());
            }

            public override IChromosome CreateNew()
            {
                AreaState res = new AreaState();
                res.Generate();
                return res;
            }

            public static int SHORTS_VAL_COUNT = Enum.GetNames(typeof(ShortsIndex)).Length; // we avoid doing this on every obj's c'tor
            public static int DOUBLES_VAL_COUNT = Enum.GetNames(typeof(DoublesIndex)).Length; // we avoid doing this on every obj's c'tor
            public static int VAL_COUNT = Enum.GetNames(typeof(ShortsIndex)).Length + Enum.GetNames(typeof(DoublesIndex)).Length;
        }
    }
}
