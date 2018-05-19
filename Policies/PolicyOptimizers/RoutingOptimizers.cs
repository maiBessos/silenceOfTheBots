using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoE.AppConstants;
using GoE.Policies;
using GoE.AppConstants.Algorithms;
using AForge.Genetic;
using GoE.GameLogic;
using GoE.Utils.Genetic;

namespace GoE.Policies
{
    class GeneticWindowFunctionOptimizer : APolicyOptimizer, IFitnessFunction
    {
        int is5Points;
        int isyAxisSymmetric;
        int isAddOnly;
        FrontsGridRoutingGameParams prm;
        GridGameGraph graph; 
        public override List<ArgEntry> optimizationInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(AppArgumentKeys.SIMULATION_REPETETION_COUNT); // repetitions before chromosome evaluation is done
                res.Add(Optimizers.IS_5_POINTS_WINDOW);
                res.Add(Optimizers.IS_Y_AXIS_SYMMETRIC);
                res.Add(Optimizers.IS_ADDONLY_WINDOW);
                res.Add(Optimizers.INJECTED_INITIAL_CHROMOSOME);
                res.Add(Optimizers.GENERATIONS_COUNT);
                res.Add(Optimizers.CHROMOSOME_COUNT);
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
                List<string> res = new List<string>();
                res.Add(AppConstants.Policies.Routing.GeneticWindowFunctionEvadersPolicy.WINDOW_CHROMOSOME.key);
                return res;
            }
        }

        public override void process(ParallelOptions opt = null)
        {
            GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome initialChromosome =
                new GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome();

            try
            {
                // if no chromosome to inject was specific, an exception will be thrown
                initialChromosome.FromWindowFunction(
                    GeneticWindowFunctionEvadersPolicy.WindowFunction.FromString(
                        Optimizers.INJECTED_INITIAL_CHROMOSOME.tryRead(policyInput)));
            }
            catch (Exception) { }

            MultiThreadEvaluationPopulation<GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome> pop =
                new MultiThreadEvaluationPopulation<GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome>(
                    int.Parse(Optimizers.CHROMOSOME_COUNT.tryRead(policyInput)), 
                    initialChromosome, 
                    this, 
                    new RankSelection(),
                    new Utils.CustomThreadPool(opt.MaxDegreeOfParallelism),
                    false);

            int genCount = int.Parse(Optimizers.GENERATIONS_COUNT.tryRead(policyInput));
            int logGenCount = Math.Max(1,Math.Min(30, genCount / 10));
            for (int gen = 0; gen < genCount; ++gen)
            {
                pop.runEpochParallel();
                if (gen % logGenCount == 0)
                {
                    this.writeLog("best reward:" + pop.BestChromosome.Fitness.ToString() + " || " +
                        ((GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome)pop.BestChromosome).ToWindowFunction().ToString());
                }
            }

            optimizationOutput = new GameResult((float)pop.BestChromosome.Fitness);
            optimizationOutput[AppConstants.Policies.Routing.GeneticWindowFunctionEvadersPolicy.WINDOW_CHROMOSOME.key] =
                ((GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome)pop.BestChromosome).ToWindowFunction().ToString();

        }

        protected override void initEx()
        {
            is5Points = int.Parse(Optimizers.IS_5_POINTS_WINDOW.tryRead(policyInput));
            isAddOnly = int.Parse(Optimizers.IS_ADDONLY_WINDOW.tryRead(policyInput));
            isyAxisSymmetric = int.Parse(Optimizers.IS_ADDONLY_WINDOW.tryRead(policyInput));

            prm = new FrontsGridRoutingGameParams();
            prm.fromValueMap(policyInput);
            graph = new GridGameGraph(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(policyInput));
        }
        public double Evaluate(IChromosome c)
        {
            GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome wfc = (GeneticWindowFunctionEvadersPolicy.WindowFunctionChromosome)c;

            GeneticWindowFunctionEvadersPolicy p = new GeneticWindowFunctionEvadersPolicy();
            Dictionary<string, string> vals = new Dictionary<string, string>();

            var f = wfc.ToWindowFunction();
            f.addOpsOnly = isAddOnly;
            vals[AppConstants.Policies.Routing.GeneticWindowFunctionEvadersPolicy.WINDOW_CHROMOSOME.key] = f.ToString();


            var res = 
            SimProcess.getEstimatedResultsAverage(new ParallelOptions() { MaxDegreeOfParallelism = 1 },
                typeof(GeneticWindowFunctionEvadersPolicy).Name,
                AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(policyInput), 
                prm, graph, policyInput, new FrontsGridRoutingGameProcess(), false);

            return double.Parse(res[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY]);
                        
        }
    }

}