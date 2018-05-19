using System.Collections.Generic;
using GoE.AppConstants.GameProcess;
using System;
using GoE.UI;
using System.Threading.Tasks;
using System.Threading;
using GoE.Utils.Extensions;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using GoE.Utils.Algorithms;
using GoE.Utils;

namespace GoE.Policies
{
    public class PerformanceEstimation
    {
        private Type evadersPolicy;
        private Type pursuersPolicy;
        private GameLogic.IGameParams gameParam;
        private AGameGraph gameGraph;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="EvadersPolicy"></param>
        /// <param name="PursuersPolicy"></param>
        /// <param name="p"></param>
        /// <param name="g"></param>
        /// will be used if policies ask input in input boxes
        /// </param>
        public PerformanceEstimation(string EvadersPolicy, string PursuersPolicy,
                                     GameLogic.IGameParams p, AGameGraph g)
        {
            this.evadersPolicy = Utils.ReflectionUtils.constructEmptyCtorType<IEvadersPolicy>(EvadersPolicy).GetType(); //AEvadersPolicy.ChildrenByTypename[EvadersPolicy];
            this.pursuersPolicy = Utils.ReflectionUtils.constructEmptyCtorType<APursuersPolicy>(PursuersPolicy).GetType(); //APursuersPolicy.ChildrenByTypename[PursuersPolicy];
            this.gameParam = p;
            this.gameGraph = g;
        }

        private void processSingleGame(GameLogic.AGameProcess gameProcessType,
                                        int maxGameIterations,
                                        bool ignoreEvePolicyGiveUp,
                                        Dictionary<string, string> policyInput,
                                        int runIdx, List<ProcessGameResult> results, InitOnlyPolicyInput inputProvider)
        {
            Utils.Exceptions.ConditionalTryCatch<Exception>(() =>
            {
                GameLogic.AGameProcess gproc;
                IEvadersPolicy chosenEvaderPolicy;
                APursuersPolicy chosenPursuerPolicy;
                DateTime startTime;

                startTime = DateTime.Now;
                //gproc = new GameLogic.GameProcess(gameParam, gameGraph);
                gproc = Utils.ReflectionUtils.constructEmptyCtorTypeFromObj<GameLogic.AGameProcess>(gameProcessType);
                gproc.initParams(gameParam, gameGraph);
                chosenEvaderPolicy = (IEvadersPolicy)evadersPolicy.GetConstructor(new Type[] { }).Invoke(new object[] { });
                chosenPursuerPolicy = (APursuersPolicy)pursuersPolicy.GetConstructor(new Type[] { }).Invoke(new object[] { });

                if (!chosenPursuerPolicy.init(gameGraph, gameParam, inputProvider, policyInput))
                    results[runIdx] = new ProcessGameResult(float.PositiveInfinity, 0, 0, 0);

                if (!chosenEvaderPolicy.init(gameGraph, gameParam, chosenPursuerPolicy, inputProvider, policyInput))
                    results[runIdx] = new ProcessGameResult(float.NegativeInfinity, 0);


                gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);

                int remainingIterations = maxGameIterations;

                float leakedData = 0;

                if (policyInput.ContainsKey(AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY))
                    leakedData = float.Parse(policyInput[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY]); // (I think )this value potentially comes from theoretical evaluation

                if (float.IsInfinity(leakedData)) 
                {
                    // no point running the game if it's infinite
                    gproc.finishGame();
                    results[runIdx] =
                        new ProcessGameResult(float.PositiveInfinity, 0, 0, 0);
                    results[runIdx].AddRange(gproc.ResultValues);
                    results[runIdx].AddRange(inputProvider.LogValues);
                }
                else
                {
                    while (!gproc.IsFinished &&
                            gproc.invokeNextPolicy() && // game ended
                            --remainingIterations > 0 &&  // iteration limitation reached
                            (ignoreEvePolicyGiveUp || !chosenEvaderPolicy.GaveUp)/*evaders gave up*/ ) ;


                    if (!gproc.IsFinished)
                        gproc.finishGame();

                    chosenEvaderPolicy.gameFinished();
                    chosenPursuerPolicy.gameFinished();

                    results[runIdx] =
                        new ProcessGameResult((float)gproc.GameResultReward,
                                              (int)gproc.CapturedEvaders,
                                              gproc.currentRound,
                                              (int)DateTime.Now.Subtract(startTime).TotalMilliseconds);

                    results[runIdx].AddRange(gproc.ResultValues);
                    results[runIdx].AddRange(inputProvider.LogValues);
                }

            },
            (Exception ex) =>
            {
                MessageBox.Show(Exceptions.getFlatDesc(ex));
            });
        }

        /// <summary>
        /// leaked data of infinity means pursuers couldn't be initialized, negative infinity means
        /// pursuers were initialized but evaders couldn't
        /// </summary>
        /// <param name="repetitionCount"></param>
        /// <param name="maxThreads"></param>
        /// <returns>
        /// separate 'ProcessGameResult' for each repetition
        /// </returns>
        public virtual List<ProcessGameResult> estimatePerformance(
            GameLogic.AGameProcess gameProcessType,
            int repetitionCount,
            Dictionary<string, string> policyInput,
            int maxGameIterations = int.MaxValue,
            bool ignoreEvePolicyGiveUp = false,
            ParallelOptions maxThreadsOpt = null)
        {
            List<ProcessGameResult> results =
                Utils.Algorithms.AlgorithmUtils.getRepeatingValueList<ProcessGameResult>(null, repetitionCount); // make sure we can access random items in results List
            
            if (maxThreadsOpt == null)
            {
                maxThreadsOpt = new ParallelOptions();
                maxThreadsOpt.MaxDegreeOfParallelism = Int16.MaxValue;
            }

            Parallel.For(0, repetitionCount, maxThreadsOpt, runIdx =>
            {
                InitOnlyPolicyInput inputProvider = new InitOnlyPolicyInput();
                processSingleGame(Utils.ReflectionUtils.constructEmptyCtorTypeFromObj(gameProcessType), maxGameIterations, ignoreEvePolicyGiveUp, policyInput, runIdx, results, inputProvider);
            });

            return results; // return results!
        }

    }
}