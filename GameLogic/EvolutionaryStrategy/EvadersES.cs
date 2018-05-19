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
        ///// <summary>
        ///// allows algorithms to select the best suited evaders. Generally corresponds AreaState's properties
        ///// </summary>
        //class EvaderState : AForge.Genetic.ShortArrayChromosome
        //{
        //    private enum ValueIndex : int
        //    {
        //        lastTransmissionTime = 0, // how many rounds passed since last transmission by that evader. UShort.MAX is used for no transmissions
        //        uniqueDataUnits,
        //        distanceFromTarget,
        //        evadersWithinReceptionArea // how many evaders are in distance r_e or less
        //    }
        //    public EvaderState()
        //        : base(VAL_COUNT) { }

        //    public ushort this[ValueIndex idx]
        //    {
        //        get
        //        {
        //            return val[(int)idx];
        //        }
        //        set
        //        {
        //            val[(int)idx] = value;
        //        }
        //    }

        //    /// <summary>
        //    /// 
        //    /// </summary>
        //    /// <param name="s1"></param>
        //    /// <param name="s2"></param>
        //    /// <param name="valImpactFactor">
        //    /// array of size VAL_COUNT, telling how important each value is for distance calculation
        //    /// </param>
        //    /// <returns></returns>
        //    public static double distance(EvaderState s1, EvaderState s2, double[] valImpactFactor)
        //    {
        //        double res = 0;

        //        for (int i = 0; i < EvaderState.VAL_COUNT; ++i)
        //                res += MathEx.Sqr(valImpactFactor[i] * (s1.Value[i] - s2.Value[i]));

        //        return Math.Sqrt(res);
        //    }
        //    public static int VAL_COUNT = Enum.GetNames(typeof(ValueIndex)).Length;
        //}

        /// <summary>
        /// all parameters must be set before the objects in EvaderSide NS may be used
        /// </summary>
        static class EvolutionConstants
        {
            public static GoEGameParams param = null;

            public static GridGameGraph graph
            {
                get
                {
                    return assignedGraph;
                }
                set
                {
                    assignedGraph = value;
                    var sinkPoints = assignedGraph.getNodesByType(NodeType.Sink);
                    assignedGraphRightmostSinkPoint = sinkPoints.First();
                    foreach(System.Drawing.Point p in sinkPoints)
                    {
                        if (p.X < assignedGraphRightmostSinkPoint.X)
                            assignedGraphRightmostSinkPoint = p;
                    }
                    assignedGraphTargetPoint = assignedGraph.getNodesByType(NodeType.Target).First();
                    if (assignedGraph.getNodesByType(NodeType.Target).Count != 1)
                        throw new Exception("EvolutionConstants.graph may be initialized with a graph with 1 target only!");

                    assignedRadius = assignedGraphTargetPoint.manDist(assignedGraphRightmostSinkPoint);
                }
            }
            
            /// <summary>
            /// TODO: in everywhere we rely on this single target point, it will be a problem if we don't have a target that
            /// is surrounded by sinks
            /// </summary>
            public static Point targetPoint
            {
                get { return assignedGraphTargetPoint;  }
                
            }

            /// <summary>
            /// tells the distance between target and a sink
            /// </summary>
            public static int radius
            {
                get { return assignedRadius; }

            }
            public static Point rightmostSinkPoint
            {
                get { return assignedGraphRightmostSinkPoint;  }
                
            }

            public static double basicAlgorithmRepairingWorthCeiling = -1; // it's hard to decide this number, but maybe evaders count would be ok

            public static double initialImpactFactorWeightValue = -1;
            public static double initialMaxEntryDistance = -1;
            public static double initialMaxEntryProbability = -1;
            public static WorldState initialWorldState = null; // new strategies will have one of their entries initialized with this state, since this is the state we always start in

            public static int populationCount = 200;
            public static float valueMutationProb = 0.2f; // since we have many value-arrays, we give each value this probability for changing
            public static float switchAlgorithmCodeMutationProb = 0.1f; // this is the only value who's change also affects the meaning of other parts in the chromosome, and therefore should maybe have a different mutation prob
            public static float strategyMutationRate = 0.2f; // tells how many mutant policy chromosomes will be added in each generation (1.0 means the population gets doubled, with half the pop. are mutants)

            //public static float mutateStrategyEntryMutationProb = 0.01f;

            public static int maxSimultenousActiveBasicAlgorithms = -1;

            public static int MaxSimulationRoundCount = -1;
            public static int MinSimulationRoundCount = -1;

            public static int CurrentSimulationRoundCount = -1; // tells after how many game rounds we stop the simulation (always between max/min-SimulationRoundCount)

            public static int EvaluationRepetitions = -1;

            // TODO: consider replace SimulationMaxRoundCount and AccumulatedDataSimulationMaxRoundCount with a random amount of rounds for each fitness evaluation, so the alg. won't be able to "cheat" very well anyway

            /// <summary>
            /// evaders assume that after this amount of rounds, a transmission observation gets disregarded
            /// </summary>
            public static int assumedMaxPursuitRoundCount = -1;

            //public static int maxAgentTypesPerAction = -1; // typically a small number (2-5), telling how many different *kinds* of evaders an invoked algorithm steals/buys

            /// <summary>
            /// used by WorldState
            /// </summary>
            public static int areaStatesCount = -1; 

            /// <summary>
            /// tells how many state-action entries a strategy should have (may gradually increase if genetic alg. can't improve further)
            /// TODO: implement this! right now, the entry count is constant
            /// </summary>
            public static int initialStrategyTableSize = -1;

            public static int actionFlowMaxSequentialActions = -1;
            public static int actionFlowMaxParallelActions = -1;
            
            /// <summary>
            /// lists the repetitive state algorithm types available for evaders
            /// </summary>
            public static List<IEvaderBasicAlgorithm> actionAlgorithmsByCode = null;

            private static GridGameGraph assignedGraph = null;
            private static Point assignedGraphTargetPoint;
            private static Point assignedGraphRightmostSinkPoint;
            private static int assignedRadius;
        }
    }
}
