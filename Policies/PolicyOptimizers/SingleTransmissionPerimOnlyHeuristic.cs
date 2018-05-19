using GoE.GameLogic;
using GoE.UI;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoE.Utils.Extensions;
using GoE.AppConstants.Algorithms;
using GoE.GameLogic.Algorithms;
using GoE.Utils.Algorithms;
using GoE.AppConstants.GameProcess;
using System.Drawing;
using GoE.AppConstants;
using GoE.Policies.Intrusion.SingleTransmissionPerimOnly.Utils;
using static GoE.GameLogic.Utils;
using GoE.Policies.Intrusion.SingleTransmissionPerimOnly;
using GoE.GameLogic.EvolutionaryStrategy;
using System.IO;
using System.Windows.Forms;

namespace GoE.Policies
{
    class SingleTransmissionPerimOnlyHeuristicOptimizer : APolicyOptimizer
    {
        /// <summary>
        /// utility for process()
        /// </summary>
        private class LatestObservation
        {
            public int patrollerID = -1; // 1 or 2
            public int roundObserved = -1;
        }

        /// <summary>
        /// utility for process()
        /// tells the observations that may be produced by some PreTransmissionMovement of the patrollers.
        /// Each observation may be caused by one of several patroller states (duplicated states are caused if one state is more probable than another, proportionally)
        /// </summary>
        private class DerviedObservations
        {
            public Dictionary<IntruderObservationHistory2, List<PatrollersState>> possibleStatesPerObservation = new Dictionary<IntruderObservationHistory2, List<PatrollersState>>();
        }
        
        public override List<ArgEntry> optimizationInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(Optimizers.OBSERVATION_RECORDING_ROUNDS);
                //res.Add(Optimizers.DISTANCE_KEEPING_METHODS_COUNT);
                res.Add(Optimizers.PRE_TRANSMISSION_MOVEMENT);
                res.Add(Optimizers.PRETRANSMISSION_VARIATIONS_COUNT);
                res.Add(Optimizers.POSTTRANSMISSION_VARIATIONS_COUNT);
                res.Add(Optimizers.POSTTRANSMISSION_MAX_ROTATIONS);
                res.Add(Optimizers.INTRUDER_DELAY_JUMP);
                res.Add(Optimizers.INTRUDEROBSERVER_DISTANCE_JUMP);
                res.Add(Optimizers.OVERRIDE_PATROLLER_POLICY);
                
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
                var res = new List<string>();



                return res;
            }
        }

        private struct IntruderStrategy
        { 
            public Dictionary<string,string> serialize()
            {
                Dictionary<string, string> res = new Dictionary<string, string>();
                res["IntruderCWDistFromObserver"] = IntruderCWDistFromObserver.ToString();
                res["IntrusionDelay"] = IntrusionDelay.ToString();
                return res;
            }
            public int IntruderCWDistFromObserver;
            public int IntrusionDelay;
        }

        private class AvgSum
        {
            public void add(double val)
            {
                sum += val;
                ++additionsCount;
            }

            public double sum = 0;
            public double avg
            {
                get
                {
                    if (additionsCount == 0)
                        return double.NaN;
                    return sum / additionsCount;
                }
            }
            public int additionsCount = 0;
        }

        bool overridePatrollersPolicy = false;
        bool omitSingularObservationState = true;

        private void testSpecificPolicy(IntrusionGameParams prm, Grid4Square gameCircumference,
                                       ParallelOptions opt)
        {
            List<List<PerformanceRec>> performancePerCombination = new List<List<PerformanceRec>>();

            PostTransmissionMovementProperties myMovementProps =
                    new PostTransmissionMovementProperties()
                    {
                        lastPossibleRotationRound = gameCircumference.PointCount,
                        rotationsCount = 2
                    };
            
            List<PreTransmissionMovement> preTransmissionMovements = new List<PreTransmissionMovement>();
            preTransmissionMovements.Add(new RotatingPatrollerBoundPreTransmissionMovement(0.3f, 3));

            List<PostTransmissionMovement> mypostTransmissionStrategies = new List<PostTransmissionMovement>();
            for (int i = 0; i < 50; ++i)
            {
                var attempt = myMovementProps.generateRotations(new ThreadSafeRandom());
                if (attempt != null)
                    mypostTransmissionStrategies.Add(new PostTransmissionMovement(attempt));
            }

            KeyValuePair<PostTransmissionMovementProperties, List<PostTransmissionMovement>>
                mypostTransmissionStrategiesPair = new KeyValuePair<PostTransmissionMovementProperties, List<PostTransmissionMovement>>(myMovementProps, mypostTransmissionStrategies);

            var allObservations = generateDerivedObservations(preTransmissionMovements, prm, gameCircumference);

            List<KeyValuePair<IntruderObservationHistory2, HashSet<PatrollersState>>> worstObservations =
                new List<KeyValuePair<IntruderObservationHistory2, HashSet<PatrollersState>>>();

            foreach (var obs in allObservations[preTransmissionMovements[0]].possibleStatesPerObservation)
            {
                if(obs.Value.Count > 1)
                    worstObservations.Add(new KeyValuePair<IntruderObservationHistory2, HashSet<PatrollersState>>(obs.Key, new HashSet<PatrollersState>(obs.Value)));
            }
            Comparison<KeyValuePair<IntruderObservationHistory2, HashSet<PatrollersState>>> obsCountComp =
                (obs1, obs2) => obs1.Value.Count.CompareTo(obs2.Value.Count);
            worstObservations.Sort(obsCountComp); // tells which observations have least different states mapped to them


            foreach (var triggeringObservation in allObservations[preTransmissionMovements[0]].possibleStatesPerObservation)
            {
                evaluatePatrollerPolicy(prm, gameCircumference,
                                            performancePerCombination,
                                            preTransmissionMovements[0],
                                            triggeringObservation,
                                            mypostTransmissionStrategiesPair,
                                            1,
                                            1,
                                            0);
            }
        }

        GridGameGraph gridGameGraph;
        public override void process(ParallelOptions opt = null)
        {
            IntrusionGameParams prm = (IntrusionGameParams)igameParams;
            Grid4Square gameCircumference = prm.SensitiveAreaSquare(gridGameGraph.getNodesByType(NodeType.Target)[0]);
            //new Grid4Square(this.gameGraph.getNodesByType(NodeType.Target)[0].subtruct(prm.r_e, prm.r_e), 2 * prm.r_e);
            
            omitSingularObservationState = Optimizers.OMIT_SINGULAR_OBSERVATION_STATES.tryRead(policyInput) == "1";
            overridePatrollersPolicy = Optimizers.OVERRIDE_PATROLLER_POLICY.tryRead(policyInput) == "1";
            if (overridePatrollersPolicy)
                testSpecificPolicy(prm, gameCircumference, opt);

            
            Dictionary<PostTransmissionMovementProperties, List<PostTransmissionMovement>> postTransmissionStrategies =
                PostTransmissionMovement.generateMovementes(
                    int.Parse(Optimizers.POSTTRANSMISSION_VARIATIONS_COUNT.tryRead(policyInput)),
                    gameCircumference.PointCount,
                    int.Parse(Optimizers.POSTTRANSMISSION_MAX_ROTATIONS.tryRead(policyInput)));

            //var distanceDistributions = 
            //    DistanceKeepingMethod.generateMovements(int.Parse(Optimizers.DISTANCE_KEEPING_METHODS_COUNT.tryReadingFromDictionary(policyInput)),
            //                                            gameCircumference.PointCount);

            var preTransmissionMovements =
                PreTransmissionMovement.generateMovements(
                    Optimizers.PRE_TRANSMISSION_MOVEMENT.tryRead(policyInput),
                    int.Parse(Optimizers.PRETRANSMISSION_VARIATIONS_COUNT.tryRead(policyInput)), 
                    prm);

            // tells what observation we may get for each pre-transmission strategy, and what states may actually occur in any observation
            Dictionary<PreTransmissionMovement, DerviedObservations> preTransmissionStrategies =
                generateDerivedObservations(preTransmissionMovements, prm, gameCircumference);

            optimizationOutput = new GameResult(); // fixme: populate this instead

            var performance =
                testCombinationPerformance(prm, gameCircumference, preTransmissionStrategies, postTransmissionStrategies,opt);

            //var performancePerDistanceMethods = new Dictionary<DistanceKeepingMethod, AvgSum>();
            var performancePerPreTransmissionMethod = new Dictionary<PreTransmissionMovement, AvgSum>();
            var performancePerPostTransmissionMethod = new Dictionary<PostTransmissionMovementProperties, AvgSum>();
            var performancePerIntruderCWDistFromObserver = new Dictionary<int, AvgSum>();
            var performancePerIntrusionDelay = new Dictionary<int, AvgSum>();

            var performancePerIntrusionStrategy = new Dictionary<IntruderStrategy, AvgSum>();
            //var performancePerPatrollerStrategyWorstIntruderResponse = 
            //    new Dictionary<Tuple<DistanceDistributionPreTransmissionMovement, PostTransmissionMovementProperties>, AvgSum>();
            var perofrmancePerCombination = new Dictionary<GameSettings, AvgSum>();
            foreach (var r in performance)
            {
                //accumulate(performancePerDistanceMethods, r.Item1.Item2, r.Item2);
                accumulate(performancePerPreTransmissionMethod, r.Record.PreTransmissionSettings, r.Performance);
                accumulate(performancePerPostTransmissionMethod, r.Record.PostTransmissionSettings, r.Performance);
                //accumulate(performancePerIntruderCWDistFromObserver, r.Record.Item4.IntruderCWDistFromObserver, r.Performance);
                accumulate(performancePerIntrusionDelay, r.Record.IntruderSettings.IntrusionDelay, r.Performance);
                accumulate(performancePerIntrusionStrategy, r.Record.IntruderSettings, r.Performance);

                //getMax(performancePerPatrollerStrategyWorstIntruderResponse,
                //    Tuple.Create(r.Record.PostTransmissionSettings, r.Record.PreTransmissionSettings,r.Record.IntruderSettings), r.Performance);

                accumulate(perofrmancePerCombination, r.Record, r.Performance);

                //if (!performancePerDistanceMethods.ContainsKey(r.Item1.Item2))
                //    performancePerDistanceMethods[r.Item1.Item2] = new AvgSum();
                //performancePerDistanceMethods[r.Item1.Item2].add(r.Item2);

                //if (!performancePerPreTransmissionMethod.ContainsKey(r.Item1.Item1))
                //    performancePerPreTransmissionMethod[r.Item1.Item1] = new AvgSum();
                //performancePerPreTransmissionMethod[r.Item1.Item1].add(r.Item2);

                //if (!performancePerPostTransmissionMethod.ContainsKey(r.Item1.Item3))
                //    performancePerPostTransmissionMethod[r.Item1.Item3] = new AvgSum();
                //performancePerPostTransmissionMethod[r.Item1.Item3].add(r.Item2);

                //if (!performancePerIntruderCWDistFromObserver.ContainsKey(r.Item1.Item4.IntruderCWDistFromObserver))
                //    performancePerIntruderCWDistFromObserver[r.Item1.Item4.IntruderCWDistFromObserver] = new AvgSum();
                //performancePerIntruderCWDistFromObserver[r.Item1.Item4.IntruderCWDistFromObserver].add(r.Item2);

                //if (!performancePerIntrusionDelay.ContainsKey(r.Item1.Item4.IntrusionDelay))
                //    performancePerIntrusionDelay[r.Item1.Item4.IntrusionDelay] = new AvgSum();
                //performancePerIntrusionDelay[r.Item1.Item4.IntrusionDelay].add(r.Item2);


            }


            //saveTable("performancePerCWDistanceDist_" + prm.t_i.ToString() + ".txt", performancePerDistanceMethods,
            //(DistanceKeepingMethod v) =>
            //{
            //    return v.serialize();
            //});

            saveTable("performancePerAvgBoundAdvance_" + prm.t_i.ToString() + ".txt", performancePerPreTransmissionMethod,
                (PreTransmissionMovement v) =>
                {
                    RotatingPatrollerBoundPreTransmissionMovement mv = (RotatingPatrollerBoundPreTransmissionMovement)v;
                    Dictionary<string, string> res = new Dictionary<string, string>();

                    float n = mv.BoundAdvancingLimitationNumerator(prm);
                    float d = mv.BoundAdvancingLimitationDenominator(prm);
                    res["AvgBoundAdvance"] = (n/ d).ToString();
                    return res;
                });
            saveTable("performancePerSecSize_" + prm.t_i.ToString() + ".txt", performancePerPreTransmissionMethod,
               (PreTransmissionMovement v) =>
               {
                   RotatingPatrollerBoundPreTransmissionMovement mv = (RotatingPatrollerBoundPreTransmissionMovement)v;
                   Dictionary<string, string> res = new Dictionary<string, string>();
                   
                   res["SecSize"] = (mv.Db * 2 + 1).ToString();
                   return res;
               });


            saveTable("performancePerPreTransmissionMethod_" + prm.t_i.ToString() + ".txt", performancePerPreTransmissionMethod,
                (PreTransmissionMovement v) =>
                {
                    return v.serialize();
                });

            saveTable("performancePerPostTransmissionMethod_" + prm.t_i.ToString() + ".txt", performancePerPostTransmissionMethod,
                (PostTransmissionMovementProperties v) =>
                {
                    return v.serialize();
                });
            saveTable("performancePerIntruderCWDistFromObserver_" + prm.t_i.ToString() + ".txt", performancePerIntruderCWDistFromObserver,
              (int v) =>
              {
                  Dictionary<string, string> res = new Dictionary<string, string>();
                  res["IntruderCWDistanceFromObserver"] = v.ToString();
                  return res;
              });

            saveTable("performancePerIntrusionDelay_" + prm.t_i.ToString() + ".txt", performancePerIntrusionDelay,
              (int v) =>
              {
                  Dictionary<string, string> res = new Dictionary<string, string>();
                  res["performancePerIntrusionDelay"] = v.ToString();
                  return res;
              });

            saveTable("performancePerIntrusionStrategy_" + prm.t_i.ToString() + ".txt", performancePerIntrusionStrategy,
              (v) =>
              {
                  return v.serialize();
              });
            saveTable("perofrmancePerCombination_" + prm.t_i.ToString() + ".txt", perofrmancePerCombination,
              (v) =>
              {
                  Dictionary<string, string> res = new Dictionary<string, string>();
                  res.AddRange(v.PreTransmissionSettings.serialize());
                  res.AddRange(v.PostTransmissionSettings.serialize());
                  res.AddRange(v.IntruderSettings.serialize());
                  //res.AddRange(v.Item4.serialize());
                  return res;
              });
            //saveTable("performancePerPatrollerStrategyWorstIntruderResponse_" + prm.t_i.ToString() + ".txt", performancePerPatrollerStrategyWorstIntruderResponse,
            //  (v) =>
            //  {
            //      Dictionary<string, string> res = new Dictionary<string, string>();
            //      res.AddRange(v.Item1.serialize());
            //      res.AddRange(v.Item2.serialize());
            //      //res.AddRange(v.Item3.serialize());
            //      return res;
            //  });

        }
        private void accumulate<T>(Dictionary<T, AvgSum> into, T key, float val)
        {
            if (!into.ContainsKey(key))
                into[key] = new AvgSum();
            into[key].add(val);
        }

        /// <summary>
        /// keeps the worst value for each key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="into"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        private void getMax<T>(Dictionary<T, AvgSum> into, T key, float val)
        {
            if (!into.ContainsKey(key))
            {
                into[key] = new AvgSum();
                into[key].sum = 1;
                into[key].additionsCount = 1;
            }
            into[key].sum = Math.Max(val, into[key].sum);
        }
        void saveTable<T>(string filenameOut, Dictionary<T,AvgSum> vals, Func<T,Dictionary<string, string>> valSerializer)
        {
            List<Tuple<AvgSum, T>> valsList = new List<Tuple<AvgSum, T>>();
            foreach (var v in vals)
                valsList.Add(Tuple.Create(v.Value, v.Key));

            Comparison<Tuple<AvgSum, T>> c = (c1,c2) => c1.Item1.avg.CompareTo(c2.Item1.avg);
            valsList.Sort(c);
            List<Dictionary<string, string>> resultsTable = new List<Dictionary<string, string>>();
            foreach(var v in valsList)
            {
                Dictionary<string, string> rec = valSerializer(v.Item2);
                rec[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY] = v.Item1.avg.ToString();
                resultsTable.Add(rec);
            }
            FileUtils.writeTable(resultsTable, filenameOut);
        }

        struct GameSettings
        {
            public PreTransmissionMovement PreTransmissionSettings;
            public PostTransmissionMovementProperties PostTransmissionSettings;
            public IntruderStrategy IntruderSettings;
        }
        struct PerformanceRec
        {
            public float Performance;
            public GameSettings Record;
        }
        private List<PerformanceRec>
            testCombinationPerformance(IntrusionGameParams prm, Grid4Square gameCircumference,
                                       Dictionary<PreTransmissionMovement, DerviedObservations> preTransmissionStrategies,
                                       Dictionary<PostTransmissionMovementProperties, List<PostTransmissionMovement>> postTransmissionStrategies,
                                       ParallelOptions opt)
        {

            int intrusionDelayJump = int.Parse(Optimizers.INTRUDER_DELAY_JUMP.tryRead(policyInput));
            int observerIntruderDistJump = int.Parse(Optimizers.INTRUDEROBSERVER_DISTANCE_JUMP.tryRead(policyInput));

            // highest-level list helps separating between threads
            List<List<PerformanceRec>> performancePerCombination = new List<List<PerformanceRec>>();

            List<Tuple<PreTransmissionMovement, DerviedObservations>> preTransmissionStrategiesList = new List<Tuple<PreTransmissionMovement, DerviedObservations>>();
            foreach (var v in preTransmissionStrategies)
            {
                preTransmissionStrategiesList.Add(Tuple.Create(v.Key, v.Value));
                performancePerCombination.Add(new List<PerformanceRec>());
            }


            

            // generate several patroller strategies
            Parallel.For(0, preTransmissionStrategiesList.Count, (int strategyIdx) =>
                {
                    

                    var preTransmissionStrategy = preTransmissionStrategiesList[strategyIdx];

                    foreach (var triggeringObservation in preTransmissionStrategy.Item2.possibleStatesPerObservation)
                    {
                        foreach (var postStrategy in postTransmissionStrategies)
                        {
                            evaluatePatrollerPolicy(prm, gameCircumference,
                                                    performancePerCombination,
                                                    preTransmissionStrategy.Item1,
                                                    triggeringObservation,
                                                    postStrategy,
                                                    intrusionDelayJump,
                                                    observerIntruderDistJump,
                                                    strategyIdx);
                        }
                    }
                });

            
            // merrge results from different threads:
            var perfOut = new List<PerformanceRec>();
            foreach (var r in performancePerCombination)
            {
                perfOut.AddRange(r);
            }
            return perfOut;
        }

        void evaluatePatrollerPolicy(
            IntrusionGameParams prm, Grid4Square gameCircumference,
            List<List<PerformanceRec>> performancePerCombination,
            PreTransmissionMovement preTransmissionStrategy,
            KeyValuePair<IntruderObservationHistory2, List<PatrollersState>> triggeringObservation,
            KeyValuePair<PostTransmissionMovementProperties,List<PostTransmissionMovement>> postStrategy,
            int intrusionDelayJump, 
            int observerIntruderDistJump,
            int strategyIdx)
        {
            // fixme: gameCircumference.PointCount * 2  limitation is aribtrary and not necessarily optimal for intruder!
            for (int intruderCWDistFromObserver = 0; intruderCWDistFromObserver < (gameCircumference.PointCount - 1); intruderCWDistFromObserver += observerIntruderDistJump)
            {
                for (int intrusionDelay = 0; intrusionDelay < gameCircumference.PointCount * 2; intrusionDelay += intrusionDelayJump)
                {
                    Dictionary<Tuple<PostTransmissionMovement, PatrollersState>, float> resultPerTest = new Dictionary<Tuple<PostTransmissionMovement, PatrollersState>, float>();
                    float intrusionsCount = 0;
                    int intrusionTestsCount = 0;
                    foreach (var selectedPostStrategy in postStrategy.Value)
                    {

                        BasicPatrollerPolicy p = new BasicPatrollerPolicy();
                        SingleTransmissionPerimOnlyHeuristicIntruderPolicy ip = new SingleTransmissionPerimOnlyHeuristicIntruderPolicy();
                        p.init(gridGameGraph, prm, null, policyInput);
                        ip.init(gridGameGraph, prm, p, null, policyInput);

                        foreach (var selectedTriggeringObservation in triggeringObservation.Value)
                        {
                            var selectedComb = Tuple.Create(selectedPostStrategy, selectedTriggeringObservation);
                            ++intrusionTestsCount;
                            if (resultPerTest.ContainsKey(selectedComb))
                            {
                                // since PatrollersState and PostTransmissionMovement values may repeat, no point
                                // in re-testing them. 
                                intrusionsCount += resultPerTest[selectedComb];
                                continue;
                            }

                            p.initOpt(preTransmissionStrategy,
                                      new PostTransmissionMovement(selectedPostStrategy), // make sure "lastReadIdx" gets reset
                                      selectedTriggeringObservation);

                            if (triggeringObservation.Key.secondPassingPatroller == 1) // intruder observed patroller 1 when countdown started
                                ip.initOpt(selectedTriggeringObservation.p1,
                                    (intruderCWDistFromObserver + selectedTriggeringObservation.p1) % gameCircumference.PointCount, 0, intrusionDelay);
                            else
                                ip.initOpt(selectedTriggeringObservation.p2,
                                    (intruderCWDistFromObserver + selectedTriggeringObservation.p2) % gameCircumference.PointCount, 0, intrusionDelay);

                            IntrusionGameProcess gp = new IntrusionGameProcess();
                            gp.initParams(prm, gridGameGraph);
                            gp.init(p, ip);
                            while (!ip.GaveUp && gp.invokeNextPolicy()) ;

                            if (ip.GaveUp == true)
                                resultPerTest[selectedComb] = 0;
                            else
                            {
                                intrusionsCount += 1.0f;
                                resultPerTest[selectedComb] = 1.0f;
                            }
                        }
                    }

                    performancePerCombination[strategyIdx].Add(new PerformanceRec() {
                                Record = new GameSettings() { 
                                    PreTransmissionSettings = preTransmissionStrategy,
                                    PostTransmissionSettings = postStrategy.Key,
                                    IntruderSettings = new IntruderStrategy()
                                            {
                                                IntruderCWDistFromObserver = intruderCWDistFromObserver,
                                                IntrusionDelay = intrusionDelay
                                            }},
                                Performance = intrusionsCount / intrusionTestsCount});
                }
            }
        }


        private Dictionary<PreTransmissionMovement, DerviedObservations>
            generateDerivedObservations(List<PreTransmissionMovement> preTransmissionMovements, 
                                        IntrusionGameParams prm,
                                        Grid4Square gameCircumference)
        {
            Dictionary<PreTransmissionMovement, DerviedObservations> preTransmissionStrategies =
                new Dictionary<PreTransmissionMovement, DerviedObservations>();
            int observationRecordingRoundCount = Int32.Parse(Optimizers.OBSERVATION_RECORDING_ROUNDS.tryRead(policyInput));
            

            foreach (var preMovement in preTransmissionMovements)
                {
                    BasicPatrollerPolicy p = new BasicPatrollerPolicy();
                    p.init(gridGameGraph, prm, null, policyInput);
                    p.initOpt(preMovement, null);

                    List<LatestObservation> observationPerLocation =
                        AlgorithmUtils.getRepeatingValueList<LatestObservation>(gameCircumference.PointCount);


                    //var strategy = Tuple.Create(movement, distanceDist);
                    preTransmissionStrategies[preMovement] = new DerviedObservations();

                    //int roundsToWaitBeforeObserving = 2 * prm.SensitiveAreaSquare(new Point(0, 0)).PointCount;
                    for (int r = 0; r < observationRecordingRoundCount; ++r)
                    {
                        p.setGameState(r, new List<Point>(), new List<CapturedObservation>());
                        p.getNextStep();
                        var ps = p.PatrollersState;

                        //if (r < roundsToWaitBeforeObserving)
                        //    continue;

                        updateObservationList(observationPerLocation, ps, preTransmissionStrategies, preMovement, r, ps.p1, 1);
                        updateObservationList(observationPerLocation, ps, preTransmissionStrategies, preMovement, r, ps.p2, 2);
                        observationPerLocation[ps.p1].patrollerID = 1;
                        observationPerLocation[ps.p1].roundObserved = r;
                        observationPerLocation[ps.p2].patrollerID = 2;
                        observationPerLocation[ps.p2].roundObserved = r;
                    }
                }


            // some observations occur only once, and are either extremely rare or may occur in a specific round of the game 
            // when patrollers begin with a specific initial state. Since when an observation is mapped to a single state
            // it means intruder always win, such observation-state entries are unfair to patrollers, as in practice the intruder
            // couldn't / shouldn't wait for them
            if (omitSingularObservationState) 
            {

                foreach (var st in preTransmissionStrategies)
                {
                    List<IntruderObservationHistory2> obsToRemove = new List<IntruderObservationHistory2>();
                    foreach(var obsStateEntry in st.Value.possibleStatesPerObservation)
                        if (obsStateEntry.Value.Count == 1)
                            obsToRemove.Add(obsStateEntry.Key);
                    foreach (var k in obsToRemove)
                        st.Value.possibleStatesPerObservation.Remove(k);
                }
            }
            return preTransmissionStrategies;
        }

        private void updateObservationList(List<LatestObservation> observationPerLocation, 
                                            PatrollersState ps,
                                            Dictionary<PreTransmissionMovement, DerviedObservations> preTransmissionStrategies,
                                            PreTransmissionMovement strategy, 
                                            int currentRound, int currentPatrollerLocation, int currentPatrollerID)
        {
            if (observationPerLocation[currentPatrollerLocation].patrollerID != -1)
            {

                IntruderObservationHistory2 observation = new IntruderObservationHistory2()
                {
                    firstPassingPatroller = observationPerLocation[currentPatrollerLocation].patrollerID,
                    secondPassingPatroller = currentPatrollerID,
                    timeDiff = currentRound - observationPerLocation[currentPatrollerLocation].roundObserved
                };
                
                if (!preTransmissionStrategies[strategy].possibleStatesPerObservation.ContainsKey(observation))
                    preTransmissionStrategies[strategy].possibleStatesPerObservation[observation] = new List<PatrollersState>();
                preTransmissionStrategies[strategy].possibleStatesPerObservation[observation].Add(ps);

            }
        }
        
        protected override void initEx()
        {
            this.gridGameGraph = (GridGameGraph)base.gameGraph;
        }
    }
}