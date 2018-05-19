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
        /// maps from a world state into an action, with some probability
        /// </summary>
        public class WorldStateEvaderStrategyChromosome : ConcreteCompositeChromosome
        {
            
            public WorldStateEvaderStrategyChromosome(WorldStateEvaderStrategyChromosome src)
                : base(src)
            {
                
            }
            public WorldStateEvaderStrategyChromosome()
                : base(1, new ushort[] {(ushort) EvolutionConstants.param.A_E.Count},
                       0, null, 0,
                       EvolutionConstants.valueMutationProb,
                       getStateActionEntries())
            {
                
            }

            public override IChromosome CreateNew()
            {
                WorldStateEvaderStrategyChromosome res = new WorldStateEvaderStrategyChromosome();
                res.Generate();
                if (EvolutionConstants.initialWorldState != null)
                {
                    this[new ThreadSafeRandom().rand.Next() % MiscCount].state.
                        setAreaStates(EvolutionConstants.initialWorldState);
                }
                return res;
            }
            public override IChromosome Clone()
            {
                return new WorldStateEvaderStrategyChromosome(this);
            }
            /// <summary>
            /// assuming evaders are spread initially with even distances from each other on one of the sinks,
            /// this tells how many evaders should be located in each sink
            /// </summary>
            public ushort initialPositionClusterSize
            {
                get
                {
                    return this[Shorts,0]; // 0 to amount of evaders - 1 (since 1 evaders is the minimal value)
                }
            }
            
            public int entryCount
            {
                get
                {
                    return MiscCount;
                }
            }
            public StateActionEntry this[int idx]
            {
                get
                {
                    return (StateActionEntry)this[Miscs,idx];
                }
                protected set
                {
                    this[Miscs, idx] = value;
                }
            }
            public List<WorldStateToActionEvaderManagerPolicy.ManagedBasicAlgorithm> getNewAlgorithms(WorldState currentWorldState, NotifyBasicAlgorithmSuccess successNotifier)
            {
                List<WorldStateToActionEvaderManagerPolicy.ManagedBasicAlgorithm> res = new List<WorldStateToActionEvaderManagerPolicy.ManagedBasicAlgorithm>();
                for(int i = 0; i < entryCount; ++i)
                {
                    double prob = this[i].getEntryProbability(currentWorldState);
                    if(EvolutionUtils.threadSafeRand.rand.NextDouble() < prob)
                        res.Add(this[i].activate(successNotifier));
                }
                return res;
            }
            public override void Crossover(IChromosome pair)
            {
                WorldStateEvaderStrategyChromosome p = (WorldStateEvaderStrategyChromosome)pair;
                CrossoverShorts(p);
                CrossoverDoubles(p);


                // crossover of table strategy simly swaps two state-action entries between the two chromosomes
                int idx1 = EvolutionUtils.threadSafeRand.rand.Next() % MiscCount;
                int idx2 = EvolutionUtils.threadSafeRand.rand.Next() % p.MiscCount;

                StateActionEntry tmp = this[idx1];
                this[idx1] = p[idx2];
                p[idx2] = tmp;   
            }
            public Dictionary<string, string> getValueMap()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["initialPositionClusterSize"] = initialPositionClusterSize.ToString();
                for (int i = 0; i < MiscCount; ++i)
                {
                    var tmpRes = this[i].getValueMap();
                    tmpRes.AddKeyPrefix("TableEntry" + i.ToString());
                    res.AddRange(tmpRes);
                }
                return res;
            }
            public Dictionary<string, string> getValueMap(WorldState currentState)
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["initialPositionClusterSize"] = initialPositionClusterSize.ToString();
                for (int i = 0; i < MiscCount; ++i)
                {
                    var tmpRes = this[i].getValueMap();
                    tmpRes["Distance from current entry"] = 
                        WorldState.distanceSqr(currentState, this[i].state, this[i]).ToString("000.00");

                    tmpRes["Probability entering entry"] = 
                        this[i].getEntryProbability(currentState).ToString();

                    tmpRes.AddKeyPrefix("TableEntry" + i.ToString());
                    res.AddRange(tmpRes);
                }
                return res;
            }

            /// <summary>
            /// returns a detailed description of this object and it's internal objects, where
            /// each line is separated by Environment.NewLine
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string res = "";

                res = "initialPositionClusterSize=" + initialPositionClusterSize.ToString() + Environment.NewLine;
                for (int i = 0; i < MiscCount; ++i )
                    res += this[Miscs, i].ToString() + Environment.NewLine;
                
                return res;
            }

            /// <summary>
            /// similar to ToString(), but also displays current distance (from currentState) from each StateAction entry,
            /// and probability of entering it
            /// </summary>
            /// <param name="currentState"></param>
            /// <returns></returns>
            public string ToString(WorldState currentState)
            {
                string res = "";

                res = "initialPositionClusterSize=" + initialPositionClusterSize.ToString() + Environment.NewLine;
                for (int i = 0; i < MiscCount; ++i)
                {
                    res += this[Miscs, i].ToString() + Environment.NewLine;
                    res += "Distance from current entry:" + WorldState.distanceSqr(currentState, this[i].state, this[i]).ToString("000.00") + Environment.NewLine;
                    res += "**Probability entering entry:" + this[i].getEntryProbability(currentState) + Environment.NewLine + Environment.NewLine;
                }

                return res;
            }

            //public override void Mutate()
            //{
            //    base.Mutate();
            //    sortMiscs(Comparer<IChromosome>.Create(new Comparison<IChromosome>((IChromosome c1, IChromosome c2)=>
            //    { 
            //        StateActionEntry e1 = (StateActionEntry) c1;
            //        StateActionEntry e2 = (StateActionEntry) c2;
            //        return e1.action.algCode.CompareTo(e2.action.algCode);
            //    })));
            //}
            private static List<IChromosome> getStateActionEntries()
            {
                List<IChromosome> res = new List<IChromosome>();
                for(int i = 0; i < EvolutionConstants.initialStrategyTableSize; ++i)
                    res.Add(new StateActionEntry());
                return res;
            }
        }
     
    }
}
