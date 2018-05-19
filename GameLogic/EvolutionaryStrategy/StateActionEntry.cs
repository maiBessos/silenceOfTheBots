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
        /// a chromosome composed of arbitrary world state, action, 
        /// and additional double weights doubles to decide getEntryProbability()
        /// </summary>
        public class StateActionEntry : ConcreteCompositeChromosome
        {
            public WorldState state { get; protected set; }
            public ActionData action { get; protected set; }

            public override IChromosome CreateNew()
            {
                StateActionEntry res = new StateActionEntry();
                res.Generate();
                return res;
            }
            public override IChromosome Clone()
            {
                return new StateActionEntry(this);
            }

            // the first WorldState.VAL_COUNT doubles are used as impact factor weights when comparing world states,
            // and the next two doubles are maxProbability maxWorldStateDistance that also help 
            // to calculate getEntryProbability()
            public StateActionEntry()
                : base(0, null, 
                       6 + WorldState.VAL_COUNT, getMaxDoubleVals(), 101,
                       EvolutionConstants.valueMutationProb,
                initializeChromosomes()) 
            {
                state = (WorldState)this[Miscs,0];
                action = (ActionData)this[Miscs,1];

                if(EvolutionConstants.initialImpactFactorWeightValue > 0)
                    for (int i = 0; i < WorldState.VAL_COUNT; ++i)
                        this[Doubles, i] = EvolutionConstants.initialImpactFactorWeightValue;
                
                if (EvolutionConstants.initialMaxEntryDistance >= 0)
                    this[Doubles, maxWorldStateDistanceIdx] = EvolutionConstants.initialMaxEntryDistance;
                if (EvolutionConstants.initialMaxEntryProbability >= 0)
                    this[Doubles, maxProbabilityIdx] = EvolutionConstants.initialMaxEntryProbability;
            }
            
            public StateActionEntry(StateActionEntry src)
                : base(src)
            {
                state = (WorldState)this[Miscs, 0];
                action = (ActionData)this[Miscs, 1];
            }


          
            public Dictionary<string, string> getValueMap()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["MxProb"] = this[Doubles, maxProbabilityIdx].ToString("0.000");
                res["Rwrd"] = this[Doubles, AddedWorthPerRewardIdx].ToString("00.000");
                res["Pnlty"] = this[Doubles, LostWorthPerRoundIdx].ToString("00.000");
                res["InitWorth"] = this[Doubles, initialWorthIdx].ToString("00.000");
                res["Ceil"] = this[Doubles, LostWorthPerRoundIdx].ToString("00.000");
                res["MaxRelevantStateDist"] = this[Doubles, maxWorldStateDistanceIdx].ToString("0.000");
                res.AddRange(state.getValueMap(this));
                res.AddRange(action.getValueMap());
                return res;
            }
            public override string ToString()
            {
                string res = "";
                res += "MxProb: " + this[Doubles, maxProbabilityIdx].ToString("0.000") + " ||" +
                       "Rwrd: " + this[Doubles, AddedWorthPerRewardIdx].ToString("00.000") + " ||" +
                       "Pnlty: " + this[Doubles, LostWorthPerRoundIdx].ToString("00.000") + " ||" +
                       "Init: " + this[Doubles, initialWorthIdx].ToString("00.000") + " ||" +
                       "Ceil: " + this[Doubles, LostWorthPerRoundIdx].ToString("00.000") + " ||" +
                       "StateDist: " + this[Doubles, LostWorthPerRoundIdx].ToString("00.000") + " ||" + Environment.NewLine;
                
                
                res += "*When:" + Environment.NewLine + state.ToString(this) + "*Do:" + Environment.NewLine + action.ToString() + Environment.NewLine;
                
                return res;
            }
            /// <summary>
            /// given current world state, this tells what should be the probability of invoking this action
            /// </summary>
            /// <returns></returns>
            public double getEntryProbability(WorldState currentWorldState)
            {
                // if current world state and entry state are identical, this action should be used with probability 'maxProbability'.
                // if distance is larger than maxWorldStateDistance, probability is 0.
                // otherwise, probability drops linearly. TODO: probably shouldn't drop linearly
                double dist = WorldState.distanceSqr(currentWorldState, state, this);
                return Math.Max(0, this[Doubles, maxProbabilityIdx] * (1 - dist / this[Doubles,maxWorldStateDistanceIdx]));
            }
            public WorldStateToActionEvaderManagerPolicy.ManagedBasicAlgorithm activate(NotifyBasicAlgorithmSuccess successNotifier)
            {
                if (!action.IsInitialized)
                    action.Generate();

                WorldStateToActionEvaderManagerPolicy.ManagedBasicAlgorithm res = 
                    WorldStateToActionEvaderManagerPolicy.CreateNew(
                        EvolutionConstants.actionAlgorithmsByCode[action.algCode],
                        action.algArgs,
                        this[Doubles, initialWorthIdx],
                        this[Doubles, AddedWorthPerRewardIdx],
                        this[Doubles, LostWorthPerRoundIdx], 
                        this[Doubles, basicAlgorithmRepairingWorthCeilingIdx],
                        successNotifier);

                return res;
            }
            static private List<IChromosome> initializeChromosomes()
            {
                List<IChromosome> res =  new List<IChromosome>();
                res.Add(new WorldState(EvolutionConstants.areaStatesCount));
                res.Add(new ActionData());
                return res;
            }
            static private double[] getMaxDoubleVals()
            {
                double[] res = new double[6 + WorldState.VAL_COUNT];
                for (int i = 0; i < WorldState.VAL_COUNT; ++i)
                    res[i] = 1;

                res[maxProbabilityIdx] = 1;
                res[maxWorldStateDistanceIdx] = AreaState.maxAreaStateSqrDistance();

                res[initialWorthIdx] = 2 * EvolutionConstants.basicAlgorithmRepairingWorthCeiling; // double the ceiling, so it can both build the team and still have spare
                res[AddedWorthPerRewardIdx] = EvolutionConstants.basicAlgorithmRepairingWorthCeiling;
                res[LostWorthPerRoundIdx] = EvolutionConstants.basicAlgorithmRepairingWorthCeiling;
                res[basicAlgorithmRepairingWorthCeilingIdx] = EvolutionConstants.basicAlgorithmRepairingWorthCeiling;
                
                return res;
            }
            
            // TODO: these index constants below are the ugliest thing in seven bloody hells. There should be some trick for extending enums, instead (or defining another indexer)
            private static int maxProbabilityIdx
            {
                get
                {
                    return WorldState.VAL_COUNT;
                }
            }
            private static int maxWorldStateDistanceIdx
            {
                get
                {
                    return WorldState.VAL_COUNT + 1;
                }
            }
            private static int initialWorthIdx
            {
                get
                {
                    return WorldState.VAL_COUNT + 2;
                }
            }
            private static int AddedWorthPerRewardIdx
            {
                get
                {
                    return WorldState.VAL_COUNT + 3;
                }
            }
            private static int LostWorthPerRoundIdx
            {
                get
                {
                    return WorldState.VAL_COUNT + 4;
                }
            }
            private static int basicAlgorithmRepairingWorthCeilingIdx
            {
                get
                {
                    return WorldState.VAL_COUNT + 5;
                }
            }
            
        
        }
    }
}
