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
using GoE.Utils.Algorithms;

/// in this file we provide utilities to allow evaders to evolve/coevolve in a genetic algorithm environment.
/// It is assumed the graph has one target that is surrounded by sinks
namespace GoE.GameLogic.EvolutionaryStrategy
{

    namespace EvaderSide
    {
        /// <summary>
        /// describes an action in the action strategy table (an invokable algorithm and its parameters)
        /// NOTE1: changes to this chromosome should be done very carefully, as many
        /// methods depend on specific arrangement of data
        /// NOTE2: all actions that are parallel in the flow have a specific order in the data structure,
        /// but for any purpose this order is meaningless (and crossover should be carefully, with this assumption in mind)
        /// </summary>
        public class ActionFlow : ConcreteCompositeChromosome
        {
            public enum ShortIdx
            {
                InitialEvaderCount = 0,
                MinimalEvaderCount = 1,
                Count
            }

            private int getFlatIdx(int sequentialAxis, int parallelAxis)
            {
                return sequentialAxis * EvolutionConstants.actionFlowMaxParallelActions + parallelAxis;
            }
            private int getFlatShortIdx(int layerIdx)
            {
                return layerIdx + (int)ShortIdx.Count;
            }

            /// <summary>
            /// NOTE: returned action may be null
            /// </summary>
            /// <param name="sequentialAxis">
            /// value between 0 to EvolutionConstants.actionFlowMaxSequentialActions-1
            /// </param>
            /// <param name="parallelAxis">
            /// value between 0 to EvolutionConstants.actionFlowMaxParallelActions - 1
            /// </param>
            /// <returns>
            /// </returns>
            public ActionData Actions(int sequentialAxis, int parallelAxis) 
            {
                return (ActionData)this[Miscs,getFlatIdx(sequentialAxis,parallelAxis)];   
            }
            private void setEntryEvadersPortion(int sequentialAxis, int parallelAxis, double incommingEvadersWeight)
            {
                int fi = getFlatIdx(sequentialAxis, parallelAxis);
                this[Doubles, fi] = incommingEvadersWeight;
            }
            private void setEntry(int sequentialAxis, int parallelAxis, ActionData a, double incommingEvadersWeight)
            {
                int fi = getFlatIdx(sequentialAxis,parallelAxis);
                this[Miscs, fi] = a;
                this[Doubles, fi] = incommingEvadersWeight;
            }
            public List<IEvaderBasicAlgorithm> generateAlgorithms(int layerIdx, NotifyBasicAlgorithmSuccess notifier)
            {
                if (layerIdx >= EvolutionConstants.actionFlowMaxSequentialActions)
                    return null;

                List<IEvaderBasicAlgorithm> res = new List<IEvaderBasicAlgorithm>();
                for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                {
                    ActionData a = Actions(layerIdx,parIdx);
                    if(a.isNoAction())
                        continue;
                    var alg = EvolutionConstants.actionAlgorithmsByCode[a.algCode].CreateNew(a.algArgs);
                    res.Add(alg);
                    alg.setSuccessNotifier(notifier);
                }
                return res;
            }
            
            /// <summary>
            /// tells for how many evader rounds the layer continues
            /// </summary>
            /// <param name="layerIndex">
            /// 0 to EvolutionConstants.actionFlowMaxSequentialActions-1
            /// </param>
            /// <returns></returns>
            public ushort RoundsPerLayer(int layerIndex) 
            {
                return this[Shorts,(int)ShortIdx.Count + layerIndex];
            }
            private void setRoundPerLayer(int layerIndex, ushort rounds)
            {
                this[Shorts, (int)ShortIdx.Count + layerIndex] = rounds;
            }
            public ushort this[ShortIdx idx]
            {
                get
                {
                    return this[Shorts, (int)idx];
                }
                set
                {
                    this[Shorts, (int)idx] = value;
                }
            }
            
            
            public double EvaderCountPortionPerAction(int sequentialAxis, int parallelAxis) 
            {
                
                //double weightSum = 0;
                //for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                //    weightSum += this[Doubles, getFlatIdx(sequentialAxis, parIdx)];

                //return this[Doubles, getFlatIdx(sequentialAxis, parallelAxis)] / weightSum;   
                return this[Doubles, getFlatIdx(sequentialAxis, parallelAxis)];
            }
            
            private static int shortsCount()
            {
                return (int)ShortIdx.Count + 
                       EvolutionConstants.actionFlowMaxSequentialActions;  // roundsPerLayer
            }

            private static int doublesCount()
            {
                 return getActionsCount();// evaderCountPortionPerAction
            }
            private static int getActionsCount()
            {
                return EvolutionConstants.actionFlowMaxSequentialActions * EvolutionConstants.actionFlowMaxParallelActions;
            }

            private static ushort getMaxInitialEvaderCount()
            {
                return (ushort)EvolutionConstants.param.A_E.Count;
            }
            private static ushort[] getMaxShortVals()
            {
                ushort[] shorts =  new ushort[shortsCount()];
                shorts[(int)ShortIdx.InitialEvaderCount] = getMaxInitialEvaderCount();
                shorts[(int)ShortIdx.MinimalEvaderCount] = getMaxInitialEvaderCount();

                for(int i = 0; i < EvolutionConstants.actionFlowMaxSequentialActions; ++i)
                    shorts[(int)ShortIdx.Count + i] = (ushort)EvolutionConstants.MinSimulationRoundCount;
                return shorts;
            }

            private static double[] getMaxDoubleVals()
            {
                double[] doubles =  new double[doublesCount()];
                for(int i = 0; i < EvolutionConstants.actionFlowMaxSequentialActions; ++i)
                    for(int j = 0; j < EvolutionConstants.actionFlowMaxParallelActions; ++j)
                        doubles[j + i * EvolutionConstants.actionFlowMaxParallelActions] = 1;
                return doubles;
            }
            
            private static List<IChromosome> getMiscs()
            {
                var res = AlgorithmUtils.getRepeatingClonedValueList<IChromosome,ActionData>(new ActionData(),getActionsCount()-1,true);

                // NOTE: RegroupGoToSinkAlgArg relies on the fact that this misc chromosome is the last item
                // in miscChromosomes array
                res.Add(new GoToSinkAlg().CreateNewParam());
                return res;
            }

            public ConcreteCompositeChromosome RegroupGoToSinkAlgArg
            {
                get
                {
                    return (ConcreteCompositeChromosome)this.miscChromosomes.Last();
                }
            }

            public ActionFlow(ActionFlow src)
                : base(src) 
            {
            }

            public ActionFlow()
                : base (shortsCount(),
                        getMaxShortVals(),
                        doublesCount(),
                        getMaxDoubleVals(),
                        10,
                        EvolutionConstants.valueMutationProb,
                        getMiscs())
            {
                Generate();
            }
            
            public Dictionary<string,string> getValueMap()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["InitialEvaderCount"] = this[ShortIdx.InitialEvaderCount].ToString();
                
                for (int i = 0; i < EvolutionConstants.actionFlowMaxSequentialActions; ++i )
                    res["RoundsOnLayer" + i.ToString()] = RoundsPerLayer(i).ToString();

                for (int seqIdx = 0; seqIdx < EvolutionConstants.actionFlowMaxSequentialActions; ++seqIdx)
                    for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                    {
                        string actionIdxStr = "Seq-" + seqIdx.ToString() + ", Par-" + parIdx.ToString() + ",";

                        ActionData ad = Actions(seqIdx, parIdx);
                        if(ad.isNoAction())
                        {
                            res[actionIdxStr + "Action:"] = "null";
                            continue;
                        }
                        
                        var tmp = ad.getValueMap();
                        res[actionIdxStr + "Weight:"] = EvaderCountPortionPerAction(seqIdx, parIdx).ToString();
                        tmp.AddKeyPrefix(actionIdxStr + "Action:");
                        res.AddRange(tmp);
                    }

                res.AddRange( (new GoToSinkAlg().getValueMap(RegroupGoToSinkAlgArg)).AddKeyPrefix("Regrouping params:"));
                return res;
            }
            public override IChromosome Clone()
            {
                return new ActionFlow(this);
            }

            public override IChromosome CreateNew()
            {
                ActionFlow res = new ActionFlow();
                return res;
            }

            public override void Crossover(IChromosome pair)
            {
                ActionFlow p = (ActionFlow)pair;

                // since it's hard to decide what initialEvaderCount should be (too high and too low
                // affect performance drastically) - so we choose a random number between the two vals

                ushort s1 = this[ShortIdx.InitialEvaderCount], s2 = p[ShortIdx.InitialEvaderCount];
                EvolutionUtils.CrossValues(ref s1, ref s2);
                this[ShortIdx.InitialEvaderCount] = s1;
                p[ShortIdx.InitialEvaderCount] = s2;

                s1 = this[ShortIdx.MinimalEvaderCount]; 
                s2 = p[ShortIdx.MinimalEvaderCount];
                EvolutionUtils.CrossValues(ref s1, ref s2);
                // also make sure new minimal evaders values are not bigger than InitialEvaderCount
                s1 = Math.Min(s1, this[ShortIdx.InitialEvaderCount]);
                s2 = Math.Min(s2, p[ShortIdx.InitialEvaderCount]);
                this[ShortIdx.MinimalEvaderCount] = s1;
                p[ShortIdx.MinimalEvaderCount] = s2;


                // TODO: perhaps not only layers should be switched, but also specific actions 
                // we use two - point crossover, where the minimum is replacing one layer

                int minSeqIdx = EvolutionUtils.threadSafeRand.rand.Next(0, EvolutionConstants.actionFlowMaxSequentialActions - 1);
                int maxSeqIdx =
                    // avoid the case where we replace EVERYTHING
                    EvolutionUtils.threadSafeRand.rand.Next(minSeqIdx, EvolutionConstants.actionFlowMaxSequentialActions - ((minSeqIdx == 0)?(2):(1)) );

                for (int seqIdx = minSeqIdx; seqIdx < maxSeqIdx; ++seqIdx)
                {
                    // now we do swaps between the chromosomes : (can't use GoE.Utils.AlgorithmUtils.Swap() on properties)

                    // swap rounds count:
                    ushort tmpRndCnt = this.RoundsPerLayer(0);
                    this.setRoundPerLayer(seqIdx, p.RoundsPerLayer(seqIdx));
                    p.setRoundPerLayer(seqIdx, tmpRndCnt);

                    for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                    {
                        int flatIdx = getFlatIdx(seqIdx,parIdx);
                        
                        // swap actions:
                        IChromosome tmpI = this.miscChromosomes[flatIdx];
                        this.miscChromosomes[flatIdx] = p.miscChromosomes[flatIdx];
                        p.miscChromosomes[flatIdx] = tmpI;

                        // swap action incomming evader weight:
                        double tmpWeight = this[Doubles, flatIdx];
                        this[Doubles, flatIdx] = p[Doubles, flatIdx];
                        p[Doubles, flatIdx] = tmpWeight;
                    }

                    this.insureLegalLayer(seqIdx);
                    p.insureLegalLayer(seqIdx);
                }

                RegroupGoToSinkAlgArg.Crossover(p.RegroupGoToSinkAlgArg);
            }

            public override void Generate()
            {
                for (int seqIdx = 0; seqIdx < EvolutionConstants.actionFlowMaxSequentialActions; ++seqIdx)
                {
                    // we insure at least the first entry is not "noAction"
                    ActionData ad = new ActionData(false);
                    ad.Generate((ushort)(EvolutionUtils.threadSafeRand.Next(0, EvolutionConstants.actionAlgorithmsByCode.Count)));
                    setEntry(seqIdx, 0, ad, EvolutionUtils.threadSafeRand.NextDouble());

                    for (int parIdx = 1; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                    {
                        ushort algCode = (ushort)EvolutionUtils.threadSafeRand.rand.Next(0, EvolutionConstants.actionAlgorithmsByCode.Count + 1);
                        ad = new ActionData(false);
                        double evaderCountWeight = 0;

                        if (algCode == EvolutionConstants.actionAlgorithmsByCode.Count)
                            ad.setNoAction();
                        else
                        {
                            ad.Generate(algCode);
                            evaderCountWeight =
                                EvolutionUtils.threadSafeRand.NextDouble();
                        }
                        
                        setEntry(seqIdx, parIdx, ad, evaderCountWeight);
                    }
                }
                GenerateShorts();

                for (int seqIdx = 0; seqIdx < EvolutionConstants.actionFlowMaxSequentialActions; ++seqIdx)
                    insureLegalLayer(seqIdx);

                RegroupGoToSinkAlgArg.Generate();
            }

            public override void Mutate()
            {

                RegroupGoToSinkAlgArg.Mutate();

                // mutate the base properties of the flow:
                for (int idxIt = 0; idxIt < (int)ShortIdx.Count; ++idxIt )
                    if (EvolutionUtils.getRandomDecision(EvolutionConstants.valueMutationProb))
                        MutateShortNormalDistAddition(idxIt);
                this[ShortIdx.InitialEvaderCount] = (ushort)Math.Max(((ushort)1),this[ShortIdx.InitialEvaderCount]);
                this[ShortIdx.MinimalEvaderCount] = (ushort)Math.Min(this[ShortIdx.InitialEvaderCount],
                                                                     this[ShortIdx.MinimalEvaderCount]);


                

                // mutate one layer of actions :
                int seqIdx = EvolutionUtils.threadSafeRand.rand.Next(0, EvolutionConstants.actionFlowMaxSequentialActions);
                bool nonNullAction = false; // we must insure each layer has at least 1 non null action


                //// TODO: remove below
                //int bah = 0;
                //ActionFlow dupe = new ActionFlow(this);
                //for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                //{
                //    if (!Actions(seqIdx, parIdx).isNoAction())
                //        break;
                //    ++bah;
                //}
                //if (bah == EvolutionConstants.actionFlowMaxParallelActions)
                //{
                //    while (true) ;
                //}
                


                for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                {
                    ActionData ad = Actions(seqIdx, parIdx);
                    int flatIdx = getFlatIdx(seqIdx, parIdx);

                    if (!EvolutionUtils.getRandomDecision(EvolutionConstants.valueMutationProb) && 
                        (nonNullAction == true || (parIdx + 1 < EvolutionConstants.actionFlowMaxParallelActions)))
                    {
                        nonNullAction |= (!ad.isNoAction());
                        continue;
                    }

                    
                    //if ((nonNullAction == true || (parIdx + 1 < EvolutionConstants.actionFlowMaxParallelActions)) ||
                    //    EvolutionUtils.getRandomDecision(EvolutionConstants.switchAlgorithmCodeMutationProb))
                    //{
                    //    ad.setNoAction();
                    //    setEntryEvadersPortion(seqIdx, parIdx, 0);
                    //    continue;
                    //}


                    if (!ad.isNoAction())
                    {
                        // if the action wasn't null already  AND this is not our last chance to have a non-null action,
                        // then with some probability, turn the alg into null
                        if ((nonNullAction == true || (parIdx + 1 < EvolutionConstants.actionFlowMaxParallelActions)) &&
                            EvolutionUtils.getRandomDecision(EvolutionConstants.switchAlgorithmCodeMutationProb))
                        {
                            ad.setNoAction();
                            setEntryEvadersPortion(seqIdx, parIdx, 0);
                            continue;
                        }
                        Actions(seqIdx, parIdx).Mutate();
                        MutateDoubleNormalDistAddition(flatIdx);
                        nonNullAction = true;
                        continue;
                    }

                    // either if we must or if probability dictates, we'll switch a null action into non-null action:
                    if ((nonNullAction == false && (parIdx == (EvolutionConstants.actionFlowMaxParallelActions - 1))) ||
                        EvolutionUtils.getRandomDecision(EvolutionConstants.switchAlgorithmCodeMutationProb))
                    {
                        ad.Generate();
                        setEntryEvadersPortion(seqIdx, parIdx, getGeneratedDouble(flatIdx));
                        nonNullAction = true;
                    }
                }
                
                //// TODO: remove below:
                //if (!nonNullAction)
                //{
                //    while (true) ;
                //}

                insureLegalLayer(seqIdx);

                // mutate amount of rounds of the layer:
                if (!EvolutionUtils.getRandomDecision(EvolutionConstants.valueMutationProb))
                    MutateShortNormalDistAddition(getFlatShortIdx(seqIdx));
            }

            /// <summary>
            /// assumes the chromosome has done changing(mutate/cross/generate) - and
            /// we just need to insure the parallel doubles of the layer make up a legal list
            /// of evader count portions
            /// </summary>
            /// <param name="layerIdx"></param>
            protected void insureLegalLayer(int layerIdx)
            {
                // fix the doubles, to insure they represent portions that sum to 1.0 :
                List<double> portions = new List<double>();
                int notNullalgCount = 0;
                for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                {
                    if (Actions(layerIdx, parIdx).isNoAction())
                        continue;

                    portions.Add(EvaderCountPortionPerAction(layerIdx,parIdx));
                    ++notNullalgCount;
                }

                int algIdx = 0;
                double minimalPortion = 1.0 / Math.Max(this[ShortIdx.MinimalEvaderCount], notNullalgCount);
                portions = AlgorithmUtils.translateWeightsToPortions(portions, minimalPortion);
                for (int parIdx = 0; parIdx < EvolutionConstants.actionFlowMaxParallelActions; ++parIdx)
                {
                    if (!Actions(layerIdx, parIdx).isNoAction())
                        setEntryEvadersPortion(layerIdx, parIdx, portions[algIdx++]);
                }
            }
        }

        
    }
}
