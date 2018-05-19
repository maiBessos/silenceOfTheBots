//using GoE.GameLogic;
//using GoE.UI;
//using GoE.Utils;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using GoE.Utils.Extensions;

//namespace GoE.Policies
//{
//    /// <summary>
//    /// finds the value of AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE_MULTIPLIER that
//    /// maximizes evaders' performance
//    /// </summary>
//    public class EvaderEscapeTimeOptimizer : APolicyOptimizer
//    {
//        private static string PursuersPolicy = typeof(PatrolAndPursuit).Name;
//        private static string EvadersPolicy = typeof(EvadersPolicyEscapeAfterConstantTime).Name;

//        protected override void initEx()
//        {
//            minFactor = float.Parse(
//                Utils.ParsingUtils.readValueOrDefault(this.policyInput,
//                AppConstants.Algorithms.Optimizers.MINIMAL_L_ESCAPE_MULTIPLIER_KEY,
//                AppConstants.Algorithms.Optimizers.MINIMAL_L_ESCAPE_MULTIPLIER_DEFAULT));

//            maxFactor = float.Parse(
//                Utils.ParsingUtils.readValueOrDefault(this.policyInput,
//                AppConstants.Algorithms.Optimizers.MAXIMAL_L_ESCAPE_MULTIPLIER_KEY,
//                AppConstants.Algorithms.Optimizers.MAXIMAL_L_ESCAPE_MULTIPLIER_DEFAULT));

//            factorJump = float.Parse(
//                Utils.ParsingUtils.readValueOrDefault(this.policyInput,
//                AppConstants.Algorithms.Optimizers.JUMP_L_ESCAPE_MULTIPLIER_KEY,
//                AppConstants.Algorithms.Optimizers.JUMP_L_ESCAPE_MULTIPLIER_DEFAULT));
            
//            repetitionCount = int.Parse(
//                Utils.ParsingUtils.readValueOrDefault(this.policyInput,
//                AppConstants.Algorithms.Optimizers.REPETITION_COUNT_KEY,
//                AppConstants.Algorithms.Optimizers.REPETITION_COUNT_DEFAULT));
//        }

//        public override void process(ParallelOptions opt = null)
//        {
//            float bestLEscapeFactor = minFactor;
//            float bestLEscapeFactorLeakedData = 0;
            
//            Dictionary<string,string> pursuersPreprocessResult =
//                ((APursuersPolicy)APursuersPolicy.ChildrenByTypename[PursuersPolicy].GetConstructor(new Type[] { }).Invoke(new object[] { })).
//                preProcess(gameGraph, gameParams, new InitOnlyPolicyInput(true, policyInput) );

//            for (float currentFactor = minFactor; currentFactor <= maxFactor; currentFactor += factorJump)
//            {
//                policyInput[AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE_MULTIPLIER] =
//                    currentFactor.ToString();

//                PerformanceEstimation estimator =
//                    new PerformanceEstimation(EvadersPolicy, PursuersPolicy, gameParams, gameGraph, policyInput);
                
//                var results = estimator.estimatePerformance((int)repetitionCount, int.MaxValue, false, pursuersPreprocessResult);

//                float currentFactorLeakedData = Utils.AlgorithmUtils.Average<ProcessGameResult>(results).leakedData;
//                if(currentFactorLeakedData > bestLEscapeFactorLeakedData)
//                {
//                    bestLEscapeFactorLeakedData = currentFactorLeakedData;
//                    bestLEscapeFactor = currentFactor;
//                }
//            }

//            optimizedPolicyInput = new Dictionary<string, string>();
//            optimizedPolicyInput[AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE_MULTIPLIER] =
//                bestLEscapeFactor.ToString();
//        }

//        protected float minFactor, maxFactor, factorJump, repetitionCount;
//    }
//}