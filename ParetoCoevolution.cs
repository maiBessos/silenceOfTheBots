using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoE.AppConstants;
using GoE.Policies;
using GoE.AppConstants.Algorithms;
using AForge.Genetic;
using GoE.GameLogic;
using GoE.Utils.Genetic;
using GoE.Utils;
using GoE.Utils.Algorithms;
using System.Linq;

namespace GoE.Policies
{
    // FIXME: consdier adding the option of steady state
    // fixme: currently assumes routing game params, but may be easily generalized for intrusion and goe
    public class ParetoCoevolutionOptimizer : APolicyOptimizer, IFitnessFunction
    {
        /// <summary>
        /// the speedup method parameter is a list of values. the first value is a numeric code doresponding the
        /// enum below. the following parameters are:
        /// 1) factor of how many agents are added in each side to create slow fields (used only when generating real fields)
        /// </summary>
        public enum SpeedUpMethods : int
        {
            GenerateGameStatePerChromosome, // Each chromosome will have 1 slow run, and some mid-run state will be uses for next evaluations. 
            MixGameStates, // similar to GenerateGameStatePerChromosome, but each chromosome is evaluated using mid-run states of all other chromosomes
            RandomFields, // instead of generating real fields, we generate initial game states by scatterring ebots at random in the field
        }

        private const int Ebots = 0;
        private const int Pbots = 1;

        GameLogic.EvolutionaryStrategy.ThreadSafeRandom myRand;
        List<Tuple<ArgEntry, List<string>>>[] BotValueLists; // ArgEntry value Lists - for Ebots and for Pbots
        FrontsGridRoutingGameParams prm;
        GridGameGraph graph;

        int milisecWithNoImprovementPerSide;
        int chromosomeCountPerSide;
        int timeLimitSec;
        int valueCountPerArg;

        double crossoverSwappingPerArgValProb;
        double mutationPerArgValProb;

        class ParetoCoevolutionChromosome : IChromosome
        {
            public ParetoCoevolutionOptimizer Manager { get; protected set; }

            public int AgentType { get; protected set; } // Ebots or Pbots
            public List<int> BotVals; //coresponds ParetoCoevolutionOptimizer.BotValueLists[AgentType]

            public ParetoCoevolutionChromosome(ParetoCoevolutionOptimizer manager, int agentType)
            {
                AgentType = agentType;
                Manager = manager;
                Generate();
                
            }

            public double Fitness
            {
                get; protected set;
            }

            public IChromosome Clone()
            {
                ParetoCoevolutionChromosome c = new ParetoCoevolutionChromosome(Manager, AgentType);
                c.BotVals = new List<int>(BotVals);
                c.Fitness = Fitness;
                return c;
            }

            public int CompareTo(object obj)
            {
                ParetoCoevolutionChromosome c = (ParetoCoevolutionChromosome)obj;
                return Fitness.CompareTo(c.Fitness);
            }

            public IChromosome CreateNew()
            {
                return new ParetoCoevolutionChromosome(Manager, AgentType);
            }

            public void Crossover(IChromosome pair)
            {
                ParetoCoevolutionChromosome p = (ParetoCoevolutionChromosome)pair;
                
                for (int i = 0; i < BotVals.Count; ++i) // for each arg entry
                {
                    // swap values according to swap prob.
                    if (Manager.myRand.NextDouble() < Manager.crossoverSwappingPerArgValProb)
                    {
                        int tmp = BotVals[i];
                        BotVals[i] = p.BotVals[i];
                        p.BotVals[i] = tmp;
                    }
                }
               
            }

            public void Evaluate(IFitnessFunction function)
            {
                #if DEBUG
                if (Manager != function)
                    throw new Exception("mismatching pareto coevolution manager");
                #endif

                Fitness = Manager.Evaluate(this);
            }

            public void Generate()
            {
                // populate BotVals
                
                BotVals = new List<int>();
                foreach (var val in Manager.BotValueLists[AgentType])
                    BotVals.Add(Manager.myRand.Next() % val.Item2.Count);
                
            }

            public void Mutate()
            {
                
                for (int i = 0; i < BotVals.Count; ++i) // for each arg entry
                {
                    double mutationAction = Manager.myRand.NextDouble();

                    // if mutation is a go, with 50% chance either increase of decrease to the next/prev item in the list
                    if (mutationAction < Manager.crossoverSwappingPerArgValProb/2)
                        BotVals[i] = (BotVals[i] + 1) % Manager.BotValueLists[AgentType][i].Item2.Count;
                    else if (mutationAction < Manager.crossoverSwappingPerArgValProb)
                        BotVals[i] = (BotVals[i] - 1 + Manager.BotValueLists[AgentType][i].Item2.Count) % Manager.BotValueLists[AgentType][i].Item2.Count;
                }
            }
        }
        
        public override List<ArgEntry> optimizationInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(AppArgumentKeys.SIMULATION_REPETETION_COUNT); // repetitions before chromosome evaluation is done
                res.Add(AppArgumentKeys.EVADER_POLICY);
                res.Add(AppArgumentKeys.PURSUER_POLICY);
                res.AddRange(ReflectionUtils.getStaticInstancesInClass<ArgEntry>(
                              typeof(AppConstants.Algorithms.ParetoCoevolutionOptimizer)));
                return res;
            }
        }

        public override GameResult optimizationOutput
        {
            get;
            protected set;
        }

        public override List<string> optimizationOutputKeys
        {
            get
            {
                //TODO: we can't list here the full output, since it includes varying fields (Depends on input values)
                return new List<string>();
            }
        }

        int currentlyEvolvingPopulation; //either Ebots or Pbots
        MultiThreadEvaluationPopulation<ParetoCoevolutionChromosome>[] BotPop; // ebot and pbot populations
        public override void process(ParallelOptions opt = null)
        {
            ParetoCoevolutionChromosome initialEbotChromosome = new ParetoCoevolutionChromosome(this, Ebots);
            ParetoCoevolutionChromosome initialPbotChromosome = new ParetoCoevolutionChromosome(this, Pbots);


            BotPop =
                new MultiThreadEvaluationPopulation<ParetoCoevolutionChromosome>[2] {
                    new MultiThreadEvaluationPopulation<ParetoCoevolutionChromosome>(
                        chromosomeCountPerSide,initialEbotChromosome,this,new RankSelection(), new Utils.CustomThreadPool(opt.MaxDegreeOfParallelism),false),
                    new MultiThreadEvaluationPopulation<ParetoCoevolutionChromosome>(
                        chromosomeCountPerSide,initialPbotChromosome,this,new RankSelection(),new Utils.CustomThreadPool(opt.MaxDegreeOfParallelism),false)};

            for(int bi = 0; bi <2; ++bi)
            {
                BotPop[bi].MutationRate = 0.9;
                BotPop[bi].CrossoverRate = 0.9;
            }

            currentlyEvolvingPopulation = Ebots;
            //int genCount = int.Parse(Optimizers.GENERATIONS_COUNT.tryRead(policyInput));
            //int logGenCount = Math.Max(1,Math.Min(30, genCount / 10));

            DateTime start = DateTime.Now;
            DateTime latestLogTime = start;
            DateTime switchSideTime = start.AddMilliseconds(milisecWithNoImprovementPerSide);
            
            while ((DateTime.Now - start).TotalSeconds < timeLimitSec)
            {
                if (DateTime.Now.Subtract(switchSideTime).TotalMilliseconds <= 0)
                {
                    currentlyEvolvingPopulation = 1 - currentlyEvolvingPopulation;
                    switchSideTime = DateTime.Now.AddMilliseconds(milisecWithNoImprovementPerSide);
                }

                double currentFitness = BotPop[currentlyEvolvingPopulation].BestChromosome.Fitness;
                BotPop[currentlyEvolvingPopulation].runEpochParallel();
                
                if ((DateTime.Now - latestLogTime).TotalSeconds > 10)
                {
                    latestLogTime = DateTime.Now;
                    this.writeLog("best reward (latest pop-" + currentlyEvolvingPopulation.ToString() + "):" + BotPop[currentlyEvolvingPopulation].BestChromosome.Fitness.ToString());
                }

                if (BotPop[currentlyEvolvingPopulation].BestChromosome.Fitness > currentFitness) // since chromosomes are actually improving, we give them more time
                    switchSideTime = DateTime.Now.AddMilliseconds(milisecWithNoImprovementPerSide);
            }
            
            optimizationOutput = new GameResult((float)BotPop[currentlyEvolvingPopulation].BestChromosome.Fitness);

            ParetoCoevolutionChromosome[] bestbotc =
                new ParetoCoevolutionChromosome[2] {
                (ParetoCoevolutionChromosome)BotPop[Ebots].BestChromosome,
                (ParetoCoevolutionChromosome)BotPop[Pbots].BestChromosome};
            for (int bi = 0; bi < 2; ++bi)
                for (int i = 0; i < BotValueLists[bi].Count; ++i)
                {
                    optimizationOutput[BotValueLists[bi][i].Item1.key] = BotValueLists[bi][i].Item2[bestbotc[bi].BotVals[i]];
                    this.writeLog(BotValueLists[bi][i].Item1.key +"=" + BotValueLists[bi][i].Item2[bestbotc[bi].BotVals[i]]);
                }
            
        }

        SpeedUpMethods evaluationSpeedUpMethod;
        int speedUpPbotCount, speedUpEbotCount;
        int realPbotCount, realEbotCount;


        // FIXME apparently List<> .last() function is much slower than referring to item [.count-1]
        protected override void initEx()
        {
            chromosomeCountPerSide = int.Parse(
                AppConstants.Algorithms.ParetoCoevolutionOptimizer.CHROMOSOME_COUNT.tryRead(policyInput));

            timeLimitSec = int.Parse(
                AppConstants.Algorithms.ParetoCoevolutionOptimizer.TIME_LIMITATION_SEC.tryRead(policyInput));

            crossoverSwappingPerArgValProb = double.Parse(
                AppConstants.Algorithms.ParetoCoevolutionOptimizer.CHROMOSOME_COUNT.tryRead(policyInput));

            mutationPerArgValProb = double.Parse(
                AppConstants.Algorithms.ParetoCoevolutionOptimizer.MUTATION_PROB.tryRead(policyInput));

            chromosomeCountPerSide = int.Parse(
                AppConstants.Algorithms.ParetoCoevolutionOptimizer.CHROMOSOME_COUNT.tryRead(policyInput));

            valueCountPerArg = int.Parse(
                AppConstants.Algorithms.ParetoCoevolutionOptimizer.DIFFERENT_VALUES_PER_ARGENTRY.tryRead(policyInput));

            milisecWithNoImprovementPerSide = (int)(1000 *
                timeLimitSec * double.Parse(AppConstants.Algorithms.ParetoCoevolutionOptimizer.TIME_PER_SIDE_FACTOR.tryRead(policyInput)));
                

            prm = new FrontsGridRoutingGameParams();
            prm.fromValueMap(policyInput);
            graph = new GridGameGraph(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(policyInput));

            
            var speedUpArgs = ParsingUtils.separateCSV(AppConstants.Algorithms.ParetoCoevolutionOptimizer.SPEED_UP_METHOD.tryRead(policyInput));
            evaluationSpeedUpMethod = (SpeedUpMethods)int.Parse(speedUpArgs[0]);
            if(evaluationSpeedUpMethod != SpeedUpMethods.RandomFields)
            {
                double speedUpFactor = double.Parse(speedUpArgs.Last());
                speedUpPbotCount = (int)Math.Round(prm.A_P.Count * speedUpFactor);
                speedUpEbotCount = (int)Math.Round(prm.A_E.Count * speedUpFactor);
                realEbotCount = prm.A_E.Count;
                realPbotCount = prm.A_P.Count;
            }


            HashSet <string> ebotArgEntryNames = new HashSet<string>(
                ParsingUtils.separateCSV(AppConstants.Algorithms.ParetoCoevolutionOptimizer.EBOT_ARGETNRIES_TO_OPTIMIZE.tryRead(policyInput)));

            HashSet<string> pbotArgEntryNames = new HashSet<string>(
                ParsingUtils.separateCSV(AppConstants.Algorithms.ParetoCoevolutionOptimizer.PBOT_ARGETNRIES_TO_OPTIMIZE.tryRead(policyInput)));

            var AllArgEntries = ReflectionUtils.getStaticInstancesInNameSpace<ArgEntry>("GoE.AppConstants");
            BotValueLists = new List<Tuple<ArgEntry, List<string>>>[2];
            BotValueLists[Ebots] = new List<Tuple<ArgEntry, List<string>>>();
            BotValueLists[Pbots] = new List<Tuple<ArgEntry, List<string>>>();
            foreach (var ae in AllArgEntries)
            {
                if (ebotArgEntryNames.Contains(ae.key))
                    BotValueLists[Ebots].Add(Tuple.Create(ae, ae.vals.generate(valueCountPerArg)));
                if (pbotArgEntryNames.Contains(ae.key))
                    BotValueLists[Pbots].Add(Tuple.Create(ae, ae.vals.generate(valueCountPerArg)));
            }
            
        }
        


        // FIXME: in real pareto coevolution, we evaluate each chromosome with actual pareto score
        // i.e. if a chromosome has some opponent against which it is the best, then it is pareto 0 (best).
        // if it is only second best, then it is pareto 1 etc. 
        // In here, we don't really do this pareto - only let chromosomes fight all vs all
        public double Evaluate(IChromosome c)
        {
            if (BotPop == null) // happens in initialization, where both populations are not yet initialized
                return 1;
            ParetoCoevolutionChromosome currentCompetitor = (ParetoCoevolutionChromosome)c;
            Dictionary<string, string> tmpInput = new Dictionary<string, string>(policyInput);

            for (int i = 0; i < BotValueLists[currentlyEvolvingPopulation].Count; ++i)
                tmpInput[BotValueLists[currentlyEvolvingPopulation][i].Item1.key] = BotValueLists[currentlyEvolvingPopulation][i].Item2[currentCompetitor.BotVals[i]];

            // the competitor is pit against every enemy chromosome
            double rewardSum = 0;
            int enemyPop = 1 - currentlyEvolvingPopulation;
            for (int ei = 0; ei < chromosomeCountPerSide; ++ei)
            {
                ParetoCoevolutionChromosome enemyC = (ParetoCoevolutionChromosome)BotPop[enemyPop][ei];
                for (int i = 0; i < BotValueLists[enemyPop].Count; ++i)
                    tmpInput[BotValueLists[enemyPop][i].Item1.key] = 
                        BotValueLists[enemyPop][i].Item2[ enemyC.BotVals[i]];

                rewardSum += double.Parse(
                SimProcess.getEstimatedResultsAverage(new ParallelOptions()
                    { MaxDegreeOfParallelism = 1 },
                    AppConstants.AppArgumentKeys.EVADER_POLICY.tryRead(tmpInput),
                    AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(tmpInput),
                    prm, 
                    graph, 
                    tmpInput, 
                    new FrontsGridRoutingGameProcess(), 
                    false) [AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY]);
            }

            if(currentlyEvolvingPopulation == Ebots)
                return rewardSum/chromosomeCountPerSide;
            else
                return -rewardSum / chromosomeCountPerSide;

        }

        public ParetoCoevolutionOptimizer()
        {
            myRand = new GameLogic.EvolutionaryStrategy.ThreadSafeRandom();
        }
    }

}