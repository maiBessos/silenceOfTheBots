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
        /// describes an action in the action strategy table (an invokable algorithm and its parameters)
        /// </summary>
        public class ActionData : IChromosome, ICloneable
        {
            public ActionData(bool doGenerate = true) 
            {
                if(doGenerate)
                    Generate();
            }
            public bool IsInitialized
            {
                get
                {
                    return ushort.MaxValue != algCode;
                }
            }
            public ushort algCode = ushort.MaxValue; // an index for a EvolutionConstants.actionAlgorithmsByCode array
            public AForge.Genetic.IChromosome algArgs; // the args externalized in coresponding IEvaderBasicAlgorithm instance's IEvaderBasicAlgorithm.CreateNewParam()

            //public EvaderState[] preferredEvaderTypes;// array of size EvolutionConstants.maxAgentTypesPerAction
            //public AForge.Genetic.DoubleArrayChromosome[] valImpactFactor; // coresponds preferredEvaderTypes, and may be used for EvaderState.Distance
            //public AForge.Genetic.DoubleArrayChromosome[] preferredEvaderTypeCount; // arr of size preferredEvaderTypes.count()-1, tells how many of the evaders are allocated for each type
            //public AForge.Genetic.DoubleArrayChromosome[] preferredEvaderAvgDistance;

            public Dictionary<string,string> getValueMap()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["Action"] = EvolutionConstants.actionAlgorithmsByCode[algCode].GetType().Name;
                if (algArgs != null)
                    res.AddRange(EvolutionConstants.actionAlgorithmsByCode[algCode].getValueMap(algArgs).AddKeyPrefix("ActionParams"));

                return res;
            }
            public void setNoAction()
            {
                algCode = ushort.MaxValue;
                algArgs = null;
            }
            public bool isNoAction()
            {
                return algCode == ushort.MaxValue;
            }
            public IChromosome Clone()
            {
                ActionData res = new ActionData(false);
                res.algCode = this.algCode;
                if (this.algArgs != null)
                    res.algArgs = this.algArgs.Clone();
                else
                    res.algArgs = null;
                return res;
            }

            public IChromosome CreateNew()
            {
                ActionData res = new ActionData();
                return res;
            }

            public void Crossover(IChromosome pair)
            {
                // this is currently not used, since we replace world state and action data together, between chromosomes

                //// TODO: perhaps we need to do a more sophisticated crossover
                //ActionData p = (ActionData)pair;
                ////if(p.algCode == algCode)
                ////    algArgs.Crossover(p.algArgs);
                ////else
                //{
                //    // this and pair objects are chosen randomly, so we arbitrary make this instance dominant
                //    p.algCode = algCode;
                //    if (algArgs != null)
                //        p.algArgs = algArgs.Clone();
                //}
            }

            public void Evaluate(IFitnessFunction function)
            {
                throw new NotImplementedException();
            }


            public void Generate(ushort newAlgCode)
            {
                if (algArgs != null && algCode == newAlgCode)
                    algArgs.Generate();
                else
                {
                    algArgs = EvolutionConstants.actionAlgorithmsByCode[newAlgCode].CreateNewParam();
                    algCode = newAlgCode;
                }
            }
            public void Generate()
            {
                ushort newAlgCode =
                    (ushort)(EvolutionUtils.threadSafeRand.rand.Next() % EvolutionConstants.actionAlgorithmsByCode.Count);
                Generate(newAlgCode);
            }

            public void Mutate()
            {
                if (EvolutionUtils.getRandomDecision(EvolutionConstants.switchAlgorithmCodeMutationProb))
                    Generate();
                else
                    algArgs.Mutate();
            }

            public double Fitness
            {
                get { throw new NotImplementedException(); }
            }
            public int CompareTo(object obj)
            {
                throw new NotImplementedException();
            }

            object ICloneable.Clone()
            {
                return this.Clone();
            }
        }
    }
}
