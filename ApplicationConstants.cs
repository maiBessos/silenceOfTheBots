using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GoE.GameLogic;
using GoE.Utils;
using GoE.Utils.Extensions;
using GoE.Policies;
using static GoE.Policies.ParetoCoevolutionOptimizer;

namespace GoE
{

    
    namespace AppConstants
    {
        #region utilities

        /// <summary>
        /// ArgEntries may be set in application's arg files with pairs of key=value.
        /// ArgEntries with no default values in AcpplicationConstants.cs may be set by the user - instead, policies and optimizers
        /// Optimizers may override values given by the user
        /// Application's Output files have a form of a table, where keys are column names and different values make all the rows.
        /// </summary>
        public class ArgEntry
        {
            /// <summary>
            /// this char may not be used key or value (thus serailization/deserialization may rely on this)
            /// </summary>
            public const char CONTROL_CHAR = '$';

            public const string VARYING_VALUES_LIST_OPEN = "$[";
            public const string VARYING_VALUES_LIST_CLOSE = "$]";

            public IValueListGenerator vals;
            public string key;
            public string val;

            /// <summary>
            /// returns vals[key], or current value if missing
            /// </summary>
            /// <param name="key"></param>
            /// <param name="vals"></param>
            /// <returns></returns>
            public string tryRead(Dictionary<string,string> vals)
            {
                val = ParsingUtils.readValueOrDefault(vals, key, val);
                return val;
            }

            /// <summary>
            /// returns vals[key], or 'defaultValue' value if missing
            /// </summary>
            /// <param name="vals"></param>
            /// <param name="defaultValue"></param>
            /// <returns></returns>
            public string tryRead(Dictionary<string, string> vals, string defaultValue)
            {
                val = ParsingUtils.readValueOrDefault(vals, key, val);
                return val;
            }

            public ArgEntry(string Key, string value = "", IValueListGenerator valGenerator = null)
            {
                vals = valGenerator;
                key = Key;
                val = value;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="entryValue"></param>
            /// <returns></returns>
            public static List<string> getSplitValues(string val)
            {
                int si = val.IndexOf(VARYING_VALUES_LIST_OPEN);
                if (si == -1)
                    return new List<string>();
                int ei = val.LastIndexOf(CONTROL_CHAR); // FIXME: change LastIndexOf(VARYING_VALUES_LIST_CLOSE, si)  (strange bug prevents this from working)
                if (ei == -1)
                    return new List<string>();

                string csvVals = val.Substring(si + VARYING_VALUES_LIST_OPEN.Length, ei - si - VARYING_VALUES_LIST_CLOSE.Length);
                List<string> res = new List<string>();
                foreach (string csvVal in ParsingUtils.separateCSV(csvVals))
                    res.Add(val.Substring(0, si) + csvVal + val.Substring(ei + VARYING_VALUES_LIST_CLOSE.Length));
                return res;
            }

            /// <summary>
            /// if 'val' contains CSV values between the strings VARYING_VALUES_LIST_OPEN and VARYING_VALUES_LIST_CLOSE, 
            /// this returns a list of all the derived complete values
            /// </summary>
            /// <returns></returns>
            public List<string> getSplitValues()
            {
                return getSplitValues(val);
            }
            public static List<ArgEntry> fromDictionary(Dictionary<string,string> vals)
            {
                List<ArgEntry> res = new List<ArgEntry>();
                foreach (var v in vals)
                    res.Add(new ArgEntry(v.Key, v.Value));
                return res;
            }
        }
        /// <summary>
        /// gathers arguments needed to process a game. 
        /// At most one of the keys may have several values.
        /// Argument dictionary (each dictionary has a different value for the varying key) may be iterated using foreach()
        /// </summary>
        public class ProcessArgumentList
        {
            /// <summary>
            /// if varyingValueKey!="", vals shouldn't contain it
            /// </summary>
            Dictionary<string, string> vals = new Dictionary<string, string>();

            // one of the keys may optionally have several values
            string varyingValueKey = ""; 
            List<string> varyingValueValues = new List<string>();
            
            public string VaryingValueKey { get { return varyingValueKey; } }
            /// <summary>
            /// if data is not in correct format, detailed exception is thrown
            /// NOTE: it's a bit silly to not allow direct reading from string dictionary, but maybe
            /// we'd like to do more processing in the future
            /// </summary>
            /// <param name="filename"></param>
            public ProcessArgumentList(List<ArgEntry> entries)
            {
                
                foreach (var a in entries)
                {
                    var multivals = a.getSplitValues();
                    if (multivals.Count > 0)
                    {
                        if (varyingValueKey == "")
                        {
                            varyingValueKey = a.key;
                            varyingValueValues = multivals;
                        }
                        else
                            throw new Exception("more than one multi-valued entry found:" + varyingValueKey + " and " + a.key);
                    }
                    else
                        vals[a.key] = a.val;
                }


            }

            /// <summary>
            /// if path specifies a folder:
            /// reads all files in a directory, and treats each of them as a list of ArgEntry.
            /// It is expected that all files have the same values for all keys except for one key at most.
            /// Otherwise, exception is thrown
            /// 
            /// if path specifies a file - only that file is considered
            /// </summary>
            public ProcessArgumentList(string path)
            {
                Dictionary<string, Dictionary<string, string>> folderVals = new Dictionary<string, Dictionary<string, string>>();

                string[] files;

                if (FileUtils.TryFindingFile(path) != "")
                    files = new string[] { FileUtils.TryFindingFile(path) };
                else if (FileUtils.TryFindingFolder(path) != "")
                    files = Directory.GetFiles(FileUtils.TryFindingFolder(path));
                else
                    throw new Exception(path + "doesn't specify a directory nor a file");

                foreach (string f in files)
                    folderVals[f] = FileUtils.readValueMap(f);

                if (folderVals.Count == 0)
                    throw new Exception("no files found in folder " + path);
                
                varyingValueKey = "";
                vals = folderVals[files[0]];
                for(int di = 1; di < folderVals.Count; ++di )
                {
                    var d = folderVals[files[di]];
                    
                    if (varyingValueKey != "")
                    {
                        // we previously found varyingValueKey, so we assume every two dictionaries have this value (and only this value!) different
                        if (varyingValueValues.Contains(d[varyingValueKey]))
                            throw new Exception(files[di] + "'s key:" + varyingValueKey + " should be unique in the folder, but it isn't");
                        varyingValueValues.Add(d[varyingValueKey]);
                    }

                    foreach (var keyval in d)
                    {
                        if (!vals.ContainsKey(keyval.Key))
                            throw new Exception("File " + files[di] + " contains a key missing from " + files[0]);

                        if (varyingValueKey == "")
                        {
                            if (vals[keyval.Key] != keyval.Value)
                            {
                                // we just found the first difference between the two dictionaries. it is now the varying key
                                varyingValueKey = keyval.Key;
                                varyingValueValues.Add(vals[keyval.Key]);
                                varyingValueValues.Add(keyval.Value);
                            }
                        }
                        else if (varyingValueKey != keyval.Key && vals[keyval.Key] != keyval.Value)
                            throw new Exception("more than one varying keys found:" + keyval.Key + " and " + varyingValueKey);
                    }    
                }

                if (varyingValueKey != "")
                    vals.Remove(varyingValueKey);
                
            }

            public Dictionary<string,string> this[int idx]
            {
                get
                {
                    if (varyingValueKey == "")
                        return vals;

                    Dictionary<string, string> res = new Dictionary<string, string>(vals);
                    res[varyingValueKey] = varyingValueValues[idx];
                    return res;
                }
            }
    
            public int ValuesCount
            {
                get
                {
                    return Math.Max(1, varyingValueValues.Count);
                }
            }           

            /// <summary>
            /// allows merging params from different sources, assuming at most one has a varying key
            /// (and the varying key is missing from the other source)
            /// </summary>
            /// <param name="rhs"></param>
            public void add(ProcessArgumentList rhs, bool overrideExistingValues = true)
            {
                if (rhs.varyingValueKey == "" && varyingValueKey == "")
                {
                    vals.AddRange(rhs.vals, overrideExistingValues);
                }
                else if (rhs.varyingValueKey == "")
                {
                    if (rhs.vals.ContainsKey(varyingValueKey))
                        throw new Exception("rhs contains a value for 'this's varying key: " + varyingValueKey);
                    vals.AddRange(rhs.vals, overrideExistingValues);
                }
                else if (varyingValueKey == "")
                {
                    if (vals.ContainsKey(rhs.varyingValueKey))
                        throw new Exception("'this' contains a value for rhs's varying key: " + rhs.varyingValueKey);
                    vals.AddRange(rhs.vals, overrideExistingValues);
                    varyingValueValues = new List<string>(rhs.varyingValueValues);
                }
                else
                    throw new Exception("can't merge ProcessArgumentList objects if both have a varying key : " + varyingValueKey + " and " + rhs.varyingValueKey);
                
            }
        }

      
        #endregion

        public static class CmdLineArguments
        {
            public static ArgEntry SINGLE_FILE { get { return new ArgEntry("sf", "0"); } }
            public static ArgEntry VERBOSE_LEVEL { get { return new ArgEntry("v", "0"); } }
            public static ArgEntry MAX_PARALLEL_ARGFILES { get { return new ArgEntry("pa", "1"); } }
        }
        public static class AppArgumentKeys
        {
            public static ArgEntry DB_KEYS { get { return new ArgEntry("dbKeys", ""); } } // FIXME: implement
            public static ArgEntry GAME_MODEL { get { return new ArgEntry("gm", typeof(GoE.GameLogic.GoEGameProcess).Name); } }
            public static ArgEntry POLICY_OPTIMIZER { get { return new ArgEntry("optimizer"); } }
            public static ArgEntry EVADER_POLICY { get { return new ArgEntry("EP", ""); } }
            public static ArgEntry PURSUER_POLICY { get { return new ArgEntry("PP", ""); } }
            public static ArgEntry SIMULATION_REPETETION_COUNT { get { return new ArgEntry( "runCount", "25"); } }
            public static ArgEntry PARAM_FILE_PATH { get { return new ArgEntry( "ParamFile/Folder", ""); } }
            public static ArgEntry OUTPUT_FOLDER { get { return new ArgEntry( "outFolder", ""); } } 
            public static ArgEntry GRAPH_FILE { get { return new ArgEntry( "graph", "EmptyEnvironment"); } }
            //public static ArgEntry ARGS_FILE { get { return new ArgEntry( "Existing Arg. File", ""); } }
            public static ArgEntry THREAD_COUNT { get { return new ArgEntry( "MaxDegreeOfParallelism", "16"); } }
        }
        public static class GUIDefaults
        {
            public static ArgEntry PROCESS_PARAMS { get { return new ArgEntry("ProcessParams"); } }
            public static ArgEntry LATEST_LEGAL_PROCESS_PARAMS { get { return new ArgEntry("LatestLegalProcessParams"); } }
            public static ArgEntry GRAPH_FILE { get { return new ArgEntry( "defaultGraph", ""); } }
            public static ArgEntry PARAM_FILE { get { return new ArgEntry( "defaultParam", ""); } }
            public static ArgEntry EVADERS_POLICY { get { return new ArgEntry( "deafultEvadersPolicy", ""); } }
            public static ArgEntry PURSUERS_POLICY { get { return new ArgEntry( "deafultPursuersPolicy", ""); } }
            public static ArgEntry RAD_GOE { get { return new ArgEntry("RadBoxGoE", "1"); } }
            public static ArgEntry RAD_INT { get { return new ArgEntry("RadBoxIntrusion", "0"); } }
            public static ArgEntry UTIL_CHART_X_AXIS_PARAM {  get { return new ArgEntry("frmUtilXAxisParam","");  } }
            public static ArgEntry UTIL_CHART_Y_AXIS_PARAM { get { return new ArgEntry("frmUtilYAxisParam", GameProcess.OutputFields.PRESENTABLE_UTILITY); } }
        }
        public static class FileExtensions
        {
            public const string PROCESS_OUTPUT = "OUT.txt";
            public const string PARAM = "gprm";

            public const string INTRUSION_PROCESS_OUTPUT = "INT_OUT.txt";
            public const string INTRUSION_PARAM = "igprm";

            public const string ROUTING_PROCESS_OUTPUT = "ROUT_OUT.txt";
            public const string ROUTING_PARAM = "rgprm";

            public const string ARG_LIST = "alst";

            public const string GRAPH_FILE = ".ggrp";
        }
        public static class PathLocations
        {
            public const string DB_FILE_PREFIX = "_DB.txt";
            public const string GUI_DEFAULTS_VALUEMAP_FILE = "guiDefaultGameSettings.txt";
            public const string GRAPH_FILES_FOLDER = "Graphs";
            public const string PARAM_FILES_FOLDER = "Params";
            public const string EVASION_PROB_FOLDER = "EvasionProbTable";
        }

        namespace GameLogic
        {
            public static class GameParamsValueNames
            {
                public static ArgEntry EVADERS_COUNT { get { return new ArgEntry("EvaderCount", "1"); } }
                public static ArgEntry PURSUERS_COUNT { get { return new ArgEntry("PursuersCount", "1"); } }
                public static ArgEntry PURSUERS_VELOCITY { get { return new ArgEntry("PursuersVelocity", "1"); } }
                public static ArgEntry EVADERS_TRANSMISSION_RANGE { get { return new ArgEntry("EvadersTransmissionRange", "1"); } }
                public static ArgEntry EVADERS_SENSING_RANGE { get { return new ArgEntry("EvadersSensingRange", "1"); } }
                public static ArgEntry PURSUERS_DETECTION_PROB { get { return new ArgEntry("DetectionProbability", "1"); } }
                public static ArgEntry PURSUERS_SENSING_RANGE { get { return new ArgEntry("PursuersSensingRange", "1"); } } // relevant for intrusion game only
                public static ArgEntry REWARD_FUNCTION_NAME { get { return new ArgEntry("RewardFunctionName", typeof(NoDecrease).Name); } }
                public static ArgEntry REWARD_FUNCTION_ARGS { get { return new ArgEntry("RewardFunctionCSVArgs", ""); } }
                public static ArgEntry EVADERS_CAN_RECEIVE_SIMULTENOUS_BROADCASTS { get { return new ArgEntry("CanEvadersReceiveSimultenousBroadcasts(1/0)", "1"); } }
                public static ArgEntry SINKS_SENSE_PURSUERS { get { return new ArgEntry("CanSinksSensePursuers(1/0)", "0"); } }
                public static ArgEntry SINKS_SAFE_POINTS { get { return new ArgEntry("AreSinksSafe(1/0)", "1"); } }
                public static ArgEntry EVADERS_CIRCUMFERENCE_ENTRY_KILL_PROB { get { return new ArgEntry("EvadersCircumferenceEntryKillProbability", "0"); } } // regardless of pursuers, when evaders move from non-circumference point into a circumference point, they have a chance of getting destroyed with this given probability

            }
            public static class IntrusionGameParamsValueNames
            {
                public static ArgEntry INTRUSION_TIME { get { return new ArgEntry("IntrusionTime", "1"); } } // relevant for intrusion game only
                public static ArgEntry IS_AREA_SQUARE { get { return new ArgEntry("IsAreaSquare", "0"); } } // relevant for intrusion game only
            }
            public static class FrontsGridRoutingGameParamsValueNames
            {
                public static ArgEntry DETECTION_PROBABILITY_RETRAINT { get { return new ArgEntry("DetectionProbRestraing", "0.125"); } } // for value x, detection probability is 1 - 1/(1+( (x(r_e-1))^3)) )
                public static ArgEntry EVADER_RENEWAL_RATE_PER_STEP { get { return new ArgEntry("RenewalRate", "0.5"); } }
                public static ArgEntry REWARD_PORTION_TO_CONSIDER { get { return new ArgEntry("EvaluatedRewardPortion", "0.85"); } } // value in (0,1] - tells which portion of rounds (from start to end of the game) will be considered when counting the reward, starting from last round, e.g 0.5 means we exclude the first 0.5 of the rounds will be excluded the average
            }

            public static class AdvRoutingGameParamsValueNames
            {
                public static ArgEntry SINK_FOUND_REWARD_PENALTY { get { return new ArgEntry("AdvRoutingGameParams.SinkFoundRewardPenalty", "0"); } }
                public static ArgEntry SINGLE_SOURCE { get { return new ArgEntry("AdvRoutingGameParams.SingleSourceRouter(1/0)", "1"); } }
                public static ArgEntry CONTINUOUS_ROUTERS { get { return new ArgEntry("AdvRoutingGameParams.ForceContinuousTransmission(1/0)", "1"); } }
                public static ArgEntry CAN_PURSUERS_ELIMINATE { get { return new ArgEntry("AdvRoutingGameParams.CanInterceptorsEliminate(1/0)", "0"); } }
                public static ArgEntry SINK_ALWAYS_DETECTED { get { return new ArgEntry("AdvRoutingGameParams.SinkAlwaysDetected(1/0)", "1"); } }
                public static ArgEntry CAN_SENSE_ACCURATE_LOCATION { get { return new ArgEntry("AdvRoutingGameParams.AccurateInterception(1/0)", "0"); } }
            }

            public static class WSNGameParamsValueNames
            {
                public static ArgEntry DEMANDS_FROM_HULL_ONLY { get { return new ArgEntry("WSNGameParams.DemandsFromHullOnly(1/0)", "1"); } } // if true, all demands begin in router network's hull
                public static ArgEntry DEMANDS_TO_HULL_ONLY { get { return new ArgEntry("WSNGameParams.DemandsToHullOnly(1/0)", "1"); } } // if true, all demands end in router network's hull

                public static ArgEntry RECOVERY_TIME{ get { return new ArgEntry("WSNGameParams.RecoveryTime", "50"); } } // timesteps after last interloper exits, until game ends
                public static ArgEntry STARTUP_TIME { get { return new ArgEntry("WSNGameParams.StartupTime", "50"); } } // timesteps num before first interloper enters
                public static ArgEntry INTERRUPTOR_RADIUS { get { return new ArgEntry("WSNGameParams.InterruptorRadDist", "3"); } }
                public static ArgEntry INTERRUPTOR_AVG_VELOCITY { get { return new ArgEntry("WSNGameParams.InterruptorAvgVelocity", "0.5"); } } // velocity has uniform distribution. max velocity is 1
                public static ArgEntry INTERRUPTOR_NUMBER { get { return new ArgEntry("WSNGameParams.InterruptorNumber", "3"); } } 
                public static ArgEntry DEMAND_MATRIX_DEMANDS_NUM { get { return new ArgEntry("WSNGameParams.DemandsCount", "50"); } } // absolute number of demands in the demand matrix
                public static ArgEntry DEMAND_MATRIX_DEMAND_AVG_PROB { get { return new ArgEntry("WSNGameParams.DemandsAvgProb", "0.3"); } } // probability for each demand will have normal distribution between [0,1] with given average
                public static ArgEntry EDGE_CAPACITY { get { return new ArgEntry("WSNGameParams.EdgeCapacity", "3"); } } // uniform for all edges
                public static ArgEntry NODE_CAPACITY { get { return new ArgEntry("WSNGameParams.NodeCapacity", "6"); } } // uniform for all nodes
                public static ArgEntry DELAY_STRETCH_FACTOR { get { return new ArgEntry("WSNGameParams.DelayStretchFactor", "1.2"); } } // multiplied by min distance from origin and dest
            }

            public static class RewardFunctionArgNames
            {
                public static ArgEntry DISCOUNT_FACTOR { get { return new ArgEntry( "DiscountFactor", "1"); } }
            }
        }
        namespace Algorithms
        {
            public static class Evasion
            {
                public const string PROBABILITY_FILENAME_FORMAT = "uni*.csv";
            }
            public static class Optimizers
            {
                public static ArgEntry REPETITION_COUNT_KEY { get { return new ArgEntry("RepetitionsBeforeComparison", "10"); } }

                // used by EvaderEscapeTimeOptimizer // (and PatrolAndPursuitOptimizer - not anymore)
                //public static AppConstant MINIMAL_L_ESCAPE_MULTIPLIER_KEY { get { return new AppConstant( "MinLEscapeFactor", ""); } }
                //public static AppConstant MAXIMAL_L_ESCAPE_MULTIPLIER_KEY { get { return new AppConstant( "MaxLEscapeFactor", ""); } }
                //public static AppConstant JUMP_L_ESCAPE_MULTIPLIER_KEY { get { return new AppConstant( "LEscapeFactorJump", ""); } }
                //public static AppConstant MINIMAL_L_ESCAPE_MULTIPLIER_DEFAULT { get { return new AppConstant( "0", ""); } }
                //public static AppConstant MAXIMAL_L_ESCAPE_MULTIPLIER_DEFAULT { get { return new AppConstant( "2", ""); } }
                //public static AppConstant JUMP_L_ESCAPE_MULTIPLIER_DEFAULT { get { return new AppConstant( "0.1", ""); } } // not needed anymore, since we can calculate optimal l_escape anyway

                // used by PatrolAndPursuitOptimizer:
                public static ArgEntry PURSUERS_COUNT_FACTOR_JUMP { get { return new ArgEntry("Optimizer.PursuersCountFactorJump", "0.05"); } } // will be used to create pursuit/circumference/area combinations (0.1 means 11 x 11 combinations will be tested, as area pursuers# is dervied)
                                                                                                                                                //public static AppConstant MIN_SIMULTENEOUS_TRANSMISSONS_FACTOR_KEY { get { return new AppConstant( "MinEtaTagFactor", ""); } }
                                                                                                                                                //public static AppConstant MAX_SIMULTENEOUS_TRANSMISSONS_FACTOR_KEY { get { return new AppConstant( "MaxEtaTagFactor", ""); } }
                                                                                                                                                //public static AppConstant JUMP_SIMULTENEOUS_TRANSMISSONS_KEY { get { return new AppConstant( "EtaTagFactorJump", ""); } }

                //public static AppConstant MIN_SIMULTENEOUS_TRANSMISSONS_FACTOR_DEFAULT { get { return new AppConstant( "0", ""); } }
                //public static AppConstant MAX_SIMULTENEOUS_TRANSMISSONS_FACTOR_DEFAULT { get { return new AppConstant( "2", ""); } }
                //public static AppConstant JUMP_SIMULTENEOUS_TRANSMISSONS_DEFAULT { get { return new AppConstant( "0.1", ""); } }

                public static ArgEntry CAN_PURSUERS_PURSUE { get { return new ArgEntry("Optimizer.CanPursuersPursue", "1"); } }
                //public static ArgEntry CAN_EVADERS_TRANSMIT { get { return new ArgEntry( "Optimizer.CanEvadersTransmit", "1"); } }
                //public static ArgEntry CAN_EVADERS_CRAWL { get { return new ArgEntry( "Optimizer.CanEvadersCrawl", "1"); } }
                public static ArgEntry CAN_PURSUERS_PATROL_CIRCUMFERENCE { get { return new ArgEntry("Optimizer.CanPursuersPatrolCircumference", "1"); } }
                public static ArgEntry CAN_PURSUERS_PATROL_AREA { get { return new ArgEntry("Optimizer.CanPursuersPatrolArea", "1"); } }
                public static ArgEntry OPTIMIZER_UNAWARE { get { return new ArgEntry("Optimizer.UnawareOf"); } } // csv of: forced optimizer output values e.g. simultenous_transmissions, game params e.g. CanEvadersCrawl and app params e.g. EVADER_POLICY (see each specific optimizer for possible values). Specified values will be treated as if unspecified in the global input

                public static ArgEntry ESTIMATION_REPETITION_COUNT { get { return new ArgEntry("Optimizer.EstimationRepetitionCount", "30"); } }
                public static ArgEntry ETA_TAG_ESTIMATION_FACTOR_JUMP { get { return new ArgEntry("Optimizer.EtaTagFactorJump", "0.2"); } }

                public static ArgEntry MINIMAL_CIRCUMFERENCE_PATROLLERS { get { return new ArgEntry("Optimizer.MinCircumferencePatrollers", "0"); } }

                // used by PatrolRingsOptimizer
                public static ArgEntry MAX_RING { get { return new ArgEntry("InnerPatrolRingFromCircumference", "2"); } } // 1 means only circumference will have a ring patrol
                public static ArgEntry MIN_RING { get { return new ArgEntry("OuterPatrolRingFromCircumference", "-1"); } } // -1 means 1 ring outside sensitive area will be patrolled


                // used by SingleTransmissionPerimOnlyOptimizer
                public static ArgEntry OBSERVATION_RECORDING_ROUNDS { get { return new ArgEntry("Optimizer.ObservationRecordingRounds", "500"); } }
                //public static ArgEntry DISTANCE_KEEPING_METHODS_COUNT { get { return new ArgEntry("Optimizer.DistanceKeepingMethodsCount", "10"); } }//"20"); } } 
                public static ArgEntry PRETRANSMISSION_VARIATIONS_COUNT { get { return new ArgEntry("Optimizer.PreTransmissionVariationsCount", "500"); } }//"20"); } } 
                public static ArgEntry POSTTRANSMISSION_VARIATIONS_COUNT { get { return new ArgEntry("Optimizer.PostTransmissionVariationsCount", "100"); } }//"20"); } } 
                public static ArgEntry POSTTRANSMISSION_MAX_ROTATIONS { get { return new ArgEntry("Optimizer.PostTransmissionMaxRotationsCount", "3"); } }
                public static ArgEntry INTRUDER_DELAY_JUMP { get { return new ArgEntry("Optimizer.IntruderDelayJump", "1"); } }
                public static ArgEntry INTRUDEROBSERVER_DISTANCE_JUMP { get { return new ArgEntry("Optimizer.IntruderObserverDistanceJump", "1"); } }
                public static ArgEntry OVERRIDE_PATROLLER_POLICY { get { return new ArgEntry("Optimizer.OverridePatrollerPolicy", "0"); } }
                public static ArgEntry OMIT_SINGULAR_OBSERVATION_STATES { get { return new ArgEntry("Optimizer.OmitSingularObservationState", "1"); } }
                public static ArgEntry PRE_TRANSMISSION_MOVEMENT { get { return new ArgEntry("Optimizer.PreTransmissionMovement", typeof(GoE.Policies.Intrusion.SingleTransmissionPerimOnly.Utils.DistanceDistributionPreTransmissionMovement).Name); } }

                // used by all genetic optimizers
                public static ArgEntry CHROMOSOME_COUNT { get { return new ArgEntry("Optimizer.ChromosomeCount", "100"); } }
                public static ArgEntry GENERATIONS_COUNT { get { return new ArgEntry("Optimizer.GenerationsCount", "1000"); } }
                public static ArgEntry TIME_LIMIT_SEC { get { return new ArgEntry("Optimizer.TimeLimit(seconds)", "600"); } }
                public static ArgEntry INJECTED_INITIAL_CHROMOSOME { get { return new ArgEntry("Optimizer.InjectecInitialChromosome", ""); } }// GoE.Policies.GeneticWindowFunctionEvadersPolicy.WindowFunction.RowBiased.ToString()); } }

                // used by GeneticWindowFunctionOptimizer
                public static ArgEntry IS_5_POINTS_WINDOW { get { return new ArgEntry("Optimizer.Is5PointsWindow", "1"); } } // if "1", the chromosome will regard only the energy of current points, and top/bottom/
                public static ArgEntry IS_ADDONLY_WINDOW { get { return new ArgEntry("Optimizer.IsAddOnly", "0"); } } // if "1", the only op associated with each point type is only 'add' (no multiplication)
                public static ArgEntry IS_Y_AXIS_SYMMETRIC { get { return new ArgEntry("Optimizer.IsYAxisSymmetric", "0"); } } // if "1", chromsomes will always be symmetric on y axis
                
            }
            public static class AdvRoutingSegmentedRouteRouterPolicyOptimizer
            {
                public static ArgEntry EXPECTED_RUNTIME_REPETETION_COUNT { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicyOptimizer.ExpectedRuntimeRepetitions", "0"); } } // relevant if some heuristic is used. tells how many times to run the alg. in order to estimate the expected run time. If 0, a simple heuristic is used (e.g. 10*routers count)
                public static ArgEntry HEURISTIC_TIME_STEPS_FACTOR { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicyOptimizer.HeuristicTimeStepFactor", "0.1"); } } // relevant if some heuristic is used. stops the run after factor of the expected run time for full run
                public static ArgEntry HEURISTIC { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicyOptimizer.Heuristic(none/minSinkDist/avgSinkDist)", "none"); } }
                public static ArgEntry REPETETION_COUNT { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicyOptimizer.EvaluationRepetetion", "20"); } } // how many times to run the evaluation method
            }
            public static class ParetoCoevolutionOptimizer
            {
                public static ArgEntry TIME_PER_SIDE_FACTOR { get { return new ArgEntry("Optimizer.StagnationTimeLimitPerSideFactor", "0.05"); } } // relative to TIME_LIMITATION_SEC . tells how much timeeach side is allowed to keep not evolving (best fitness stays the same)
                public static ArgEntry TIME_LIMITATION_SEC { get { return new ArgEntry("ParetoCoevolutionOptimizer.TimeLimitSec", "600"); } }
                public static ArgEntry DIFFERENT_VALUES_PER_ARGENTRY { get { return new ArgEntry("ParetoCoevolutionOptimizer.ValCountPerArg","10"); } }
                public static ArgEntry EBOT_ARGETNRIES_TO_OPTIMIZE { get { return new ArgEntry("ParetoCoevolutionOptimizer.EbotArgsToOptimizeCSV", ""); }}
                public static ArgEntry PBOT_ARGETNRIES_TO_OPTIMIZE { get { return new ArgEntry("ParetoCoevolutionOptimizer.PbotArgsToOptimizeCSV", ""); } }
                public static ArgEntry CHROMOSOME_COUNT { get { return new ArgEntry("ParetoCoevolutionOptimizer.ChromosomeCountPerSide", "10"); } }
                public static ArgEntry MUTATION_PROB { get { return new ArgEntry("ParetoCoevolutionOptimizer.ArgValMutationProb", "0.1"); }}
                public static ArgEntry CROSSOVER_SWAP_PROB { get { return new ArgEntry("ParetoCoevolutionOptimizer.ArgValCrossoverSwapProb", "0.1"); } }
                public static ArgEntry SPEED_UP_METHOD { get { return new ArgEntry("ParetoCoevolutionOptimizer.SpeedUpMethod", ((int)SpeedUpMethods.GenerateGameStatePerChromosome).ToString() + ", 5"); } }
            }
        }      
        namespace GameProcess
        {
            public static class OutputFields
            {
                public const string CAPTURED_EVES = "capturedEves";
                public const string PRESENTABLE_UTILITY = "presentable reward"; // depends on game (e.g. in goe it's utility per eve, in intrusion game it's either 1 for success or 0 for fail)
                public const string PROCESS_TIME_MS = "time(MS)";
                public const string ROUNDS_COUNT = "rounds";
                public const string GENERATION_COUNT = "Generations";
            }
            public static class Statistics
            {
                public const string CONF_INTERVAL = "ConfInterval.99%";
                public const string STANDARD_DEVIATION_PREFIX = "StdDev.";
                public const string DISTRIBUTION = "Distribution";
                public const string MAX = "Max";
            }
        }
        namespace Policies
        {
            public static class EvadersPolicyTransmitFromWithinArea
            {
                public static ArgEntry TRANSMIT_REWARD { get { return new ArgEntry("BestPossibleRewardOfTransmitting", "-1"); } }
                //public static ArgEntry EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS_MULTIPLIER { get { return new ArgEntry( "Sim. Transmissions Multiplier", ""); } }
                public static ArgEntry EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS { get { return new ArgEntry( "simultenous_transmissions"); } }
                //public static ArgEntry EXPECTED_LEAKED_DATA { get { return new ArgEntry( "ExpTransmittingLeakedData", ""); } }
            }
            public static class EvadersPolicyEscapeAfterConstantTime
            {
                public static ArgEntry ESCAPE_REWARD {  get { return new ArgEntry("BestPossibleRewardOfEscaping", "-1"); } }
                public static ArgEntry L_ESCAPE { get { return new ArgEntry( "l_escape(DataToAccumulateBeforeEscaping)"); } }
                //public static ArgEntry L_ESCAPE_MULTIPLIER { get { return new ArgEntry( "optimal l_escape multiplier", ""); } }
                //public static ArgEntry EXPECTED_LEAKED_DATA { get { return new ArgEntry( "ExpCrawlingLeakedData", ""); } }
            }
            public static class PatrolAndPursuit
            {
                //public static ArgEntry MAX_PURSUIT_CAPTURE_PROB { get { return new ArgEntry( "MaxPursuitCaptureProb", ""); } }

                //public static ArgEntry CAN_EVADERS_TRANSMIT { get { return new ArgEntry( "can Evaders Transmit (0/1)", "1"); } }
                //public static ArgEntry CAN_EVADERS_CRAWL { get { return new ArgEntry( "can evaders crawl (0/1)", "1"); } }
                //public static ArgEntry CIRCUMFERENCE_PATROL_ALLOWED { get { return new ArgEntry( "circumference patrol allowed (0/1)", "1"); } }
                //public static ArgEntry AREA_PATROL_ALLOWED { get { return new ArgEntry( "area patrol allowed (0/1)", "1"); } }
                //public static ArgEntry PURSUIT_ALLOWED { get { return new ArgEntry( "pursuit allowed (0/1)", "1"); } }

                public static ArgEntry CIRCUMFERENCE_PATROL_CAPTURE_PROB { get { return new ArgEntry( "p_c", ""); } }
                public static ArgEntry AREA_PATROL_CAPTURE_PROB { get { return new ArgEntry( "p_a", ""); } }
                public static ArgEntry PURSUIT_CAPTURE_PROB { get { return new ArgEntry( "p_p", ""); } }

                public static ArgEntry RESULTED_P_A { get { return new ArgEntry( "resulted p_a", ""); } }
                public static ArgEntry RESULTED_P_C { get { return new ArgEntry( "resulted p_c", ""); } }
                public static ArgEntry RESULTED_P_P { get { return new ArgEntry( "resulted p_p", ""); } }
                public static ArgEntry RESULTED_P_D { get { return new ArgEntry( "resulted p_d", ""); } }
                
                public static ArgEntry AREA_PATROL_PURSUERS_COUNT { get { return new ArgEntry( "uniform area patrol pursuers #", ""); } }
                public static ArgEntry CIRCUMFERENCE_PATROL_PURSUERS_COUNT { get { return new ArgEntry( "circumference patrol pursuers #(0 extends area patrol)", ""); } }
                public static ArgEntry PURSUIT_PURSUERS_COUNT { get { return new ArgEntry( "pursuit pursuers #", ""); } }
                public static ArgEntry AREA_PATROL_VELOCITY { get { return new ArgEntry( "areaPatrol_r_p", ""); } }
                public static ArgEntry CIRCUMFERENCE_PATROL_VELOCITY { get { return new ArgEntry( "circumferencePatrol_r_p", ""); } }

            }
        
            public static class DeviatingIntrusionPursuerPolicy
            {
                public static ArgEntry PURSUIT_PROB { get { return new ArgEntry( "PursuitProbability", ""); } }
                public static ArgEntry SYNC_PURSUERS { get { return new ArgEntry( "SynchronizedPursuers(1/0)", ""); } }
                public static ArgEntry TURNING_PROB { get { return new ArgEntry( "TurningProbability", ""); } }
                public static ArgEntry DEVIATE_OUTWARDS_PROB { get { return new ArgEntry( "DeviateOutwardsProbability", ""); } }
                public static ArgEntry DEVIATE_INWARDS_PROB { get { return new ArgEntry( "DeviateInwardsProbability", ""); } }
                public static ArgEntry MAX_DISTANCE_FROM_CIRCUMFERENCE { get { return new ArgEntry( "MaxDistFromIntrusionCircumference", ""); } }

                public static ArgEntry PURSUIT_PROB_DEFAULT { get { return new ArgEntry( "0", ""); } }
                public static ArgEntry TURNING_PROB_DEFAULT { get { return new ArgEntry( "0.5", ""); } }
                public static ArgEntry DEVIATE_OUTWARDS_PROB_DEFAULT { get { return new ArgEntry( "0.1", ""); } }//"0.1", ""); } }
                public static ArgEntry DEVIATE_INWARDS_PROB_DEFAULT { get { return new ArgEntry( "0.1", ""); } }//"0.2", ""); } }
                public static ArgEntry MAX_DISTANCE_FROM_CIRCUMFERENCE_DEFAULT { get { return new ArgEntry( "4", ""); } }
                public static ArgEntry SYNC_PURSUERS_DEFAULT { get { return new ArgEntry( "0", ""); } }

            }
            public static class StraightForwardIntruderPolicy
            {
                public static ArgEntry DELAY_BETWEEN_INTRUSIONS { get { return new ArgEntry( "StraightForwardIntruderPolicy.DelayBetweenIntrusions", ""); } }
                public static ArgEntry DELAY_BETWEEN_INTRUSIONS_DEFAULT { get { return new ArgEntry( "0", ""); } }
            }
            public static class IndependentIntruderPolicy
            {
                public static ArgEntry TIME_TO_LEARN { get { return new ArgEntry( "IndependentIntruder.TimeToLearn", ""); } }
                public static ArgEntry TIME_TO_LEARN_DEFAULT { get { return new ArgEntry( "1000", ""); } }
            }
            public static class CoordinatedIntruderPolicy
            {
                public static ArgEntry TIME_TO_LEARN { get { return new ArgEntry( "CoordinatedIntruderPolicy.TimeToLearn", ""); } }
                public static ArgEntry DELAY_BETWEEN_INTRUSIONS { get { return new ArgEntry( "CoordinatedIntruderPolicy.DelayBetweenIntrusions", ""); } }

                public static ArgEntry TIME_TO_LEARN_DEFAULT { get { return new ArgEntry( "500", ""); } }
                public static ArgEntry DELAY_BETWEEN_INTRUSIONS_DEFAULT { get { return new ArgEntry( "0", ""); } }
            }
            namespace SingleTransmissionPerimOnlyPatrollerPolicy
            {
                public static class DistanceKeepingMethod
                {
                    public static ArgEntry DISTANCES_PER_PROB { get { return new ArgEntry("DistanceKeeping.DistancesList"); } }
                }

                public static class PreTransmissionMChain
                {
                    
                    public static ArgEntry CAN_PATROLLER_STAY { get { return new ArgEntry("PreTransmissionMChain.CanStay","1"); } } // if true, patrollers not only move forward and backwards, but may also stand in the same segment with some probability
                    public static ArgEntry KEEP_PATROLLER_DIRECTION { get { return new ArgEntry("PreTransmissionMChain.KeepPatrollersDirection","1"); } } // if true, markov chains include the last direction of the patroller, and with some probability switches that direction (otherwise, the probability param tells prob. of going CW vs CCW)
                }
                public static class InvariantMaximization
                {
                    public static ArgEntry MIN_PATROLLER_SWAP_STEPS { get { return new ArgEntry("InvariantMaximization.MinPatrollerSwapStepsAddition", "0"); } } // for value x, different patrollers can ocupy the same segment (i.e. swap location) only if N-2T-1+x game steps have passed
                    public static ArgEntry FORCE_SYMMETRIC_ROTATION { get { return new ArgEntry("InvariantMaximization.ForceSymmetricRotationProbabilities", "0"); } } // if 1, transitions with same start/end vals are similar(but translated) are forced to have similar values
                    public static ArgEntry MINIMAL_PATROLLER_PRESENCE_AREA_SIZE { get { return new ArgEntry("InvariantMaximization.MinPatrollerPresenceAreaSizeFactor","1"); } } // value in [0,1]. Tells the size of the area we want to maximize the probability of a patroller being in. For value 'x', the area size is 1 + 2T * x
                }

                // serve class Intrusion.SingleTransmissionPerimOnly.TerritoryWithRecentHistoryMChainPreTranPolicy
                public static class TerritoryWithRecentHistoryMChainPreTransmission
                {
                    public static ArgEntry MINIMAL_UNGUARDED_SEGMENTS_FACTOR { get { return new ArgEntry("PreTransmissionMChain.UnguardedSegmentsFactor", "1"); } } // value of 0 <= x <= 1 means at most x *(2T-1) segments will be unguarded at any time
                }


            }

            namespace Routing
            {
                public static class WeightedColumnRowRandomRoutingEvaders
                {
                    public static ArgEntry PURSUERS_HIT_INITIAL_PENALTY_FACTOR { get { return new ArgEntry("WeightedColumnRowRandomRouting.PbotHitInitialPenalty", "0.25"); } } // value in [0,1] , refers to a ratio from the column/row, which will be the amount of penalty, relatively to graph width (i.e. for a row of length 10, the ratio can be anywhere from 0 to 10)
                    public static ArgEntry PURSUERS_HIT_PENALTY_DISCOUNT_FACTOR { get { return new ArgEntry("WeightedColumnRowRandomRouting.PbotHitPenaltyDiscountFactor", "0.75"); }} // value in [0,1), refers to how fast 
                    public static ArgEntry ADD_BY_COLUM_PROB { get { return new ArgEntry("WeightedColumnRowRandomRoutingEvadersPolicy.AddColumnProb", "0.1"); } }
                    public static ArgEntry ENERGY_POWER_FACTOR { get { return new ArgEntry("WeightedColumnRowRandomRoutingEvadersPolicy.EnergyPowerFactor", "2.0"); } } // value in [1,inf). before we choose row/column, we choose via roulette per row/column. this tells how much we scale the energy in the roulette
                }
                public static class AttractionRepulsionRoutingEvaders
                {
                    public static ArgEntry PHANTOM_COLUMN_WEIGHT{ get { return new ArgEntry("AttractionRepulsionRoutingEvaders.EdgeColumnAttractionWeight", "0.5", new UniformValueList(0,5)); } } // value in (0,inf) - tells the "occupiedbonus" of edge points in columns: x=-1 and x=graph.width.
                    public static ArgEntry X_AXIS_DIST_BIAS { get { return new ArgEntry("AttractionRepulsionRoutingEvaders.XAxisDistBias", "100", new DiscreteValueList<int>(new int[]{1,100,1000,5000,10000,50000,100000},false)); } } // value in [1,inf), tells how much more important the distance on X axis than dist on Y axis
                    public static ArgEntry PURSUERS_HIT_INITIAL_PENALTY_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingEvaders.PbotHitInitialPenalty", "0.1", new UniformValueList(0,1)); } } // value in [0,1] , refers to a ratio from the column/row, which will be the amount of penalty, relatively to graph width (i.e. for a row of length 10, the ratio can be anywhere from 0 to 10)
                    public static ArgEntry PURSUERS_HIT_PENALTY_DISCOUNT_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingEvaders.PbotHitPenaltyDiscountFactor", "0.5", new UniformValueList(0, 0.95)); } } // value in [0,1), refers to how fast 
                    public static ArgEntry ENERGY_POWER_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingEvaders.EnergyPowerFactor", "4", new UniformValueList(1, 11)); } } // integer value in [1,inf). before we choose row/column, we choose via roulette per row/column. this tells how much we scale the energy in the roulette
                    public static ArgEntry OPTIMIZE_ENERGY_CALCULATION { get { return new ArgEntry("AttractionRepulsionRoutingEvaders.FastEnergyCalculation", "1"); } } // if 1, calculation of each point will be less accurate, but significantly faster
                }
                public static class AttractionRepulsionRoutingTransmittingEvaders
                {
                    public static ArgEntry OPTIMAL_EVENT_DISTANCE_FACTOR{ get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.OptEventDistanceFactor", "1.0", new DiscreteValueList<double>(new double[] {0.5,0.75,1.0,1.05,1.1,1.2,1.5,2,3},false)); } } // factor is relative to r_e. if value is x, then if a point got a penalty/bonus, then all points in distance x will get the penalty/bonus. points 
                    public static ArgEntry TRANSMISSION_INITIAL_PENALTY_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.TransmissionInitialPenalty", "0.1", new UniformValueList(0, 1)); } } // value in [0,1] , refers to a ratio from the column/row, which will be the amount of penalty, relatively to graph width (i.e. for a row of length 10, the ratio can be anywhere from 0 to 10)
                    public static ArgEntry TRANSMISSION_PENALTY_DISCOUNT_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.TransmissionPenaltyDiscountFactor", "0.5", new UniformValueList(0, 0.95)); } } // value in [0,1), refers to how fast transmission penalty diminishes
                    public static ArgEntry OPTIMAL_BACKWARD_CONNECTIVITY { get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.OptimalConnectivityPenalty", "1.1", new UniformValueList(1, 4)); } } // 0 isOccupiedBonus bonus is given for nodes with this backwardConnectivity (full bonus for 0 connectivity, proportional penalty for higher values)
                    public static ArgEntry OPTIMAL_FORWARD_CONNECTIVITY { get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.OptimalConnectivityPenalty", "1", new UniformValueList(0, 1)); } } // value relative to OPTIMAL_BACKWARD_CONNECTIVITY  (i.e. always smaller than it)

                    public static ArgEntry MAX_TRANSMISSION_REDUDANCY_MAIN_ROUTE_DISTANCE_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.MaxReduandantRoutesDistanceFactor", "0.5", new UniformValueList(0, 5)); } } //  relative to r_E. for value x: after finding the main transmitting route, only nodes that are in distance r_e*x will be used to construct additional redundant routes
                    public static ArgEntry MAX_REDUNDANT_ROUTES { get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.MaxRedundantRoutes", "2", new UniformValueList(0, 5)); } } // limits max redundant routes. even though the more the better, it makes the heuristic much slower
                    public static ArgEntry COMMUNICATION_HEURISTIC_CODE { get { return new ArgEntry("AttractionRepulsionRoutingTransmittingEvaders.RoutingHeuristicAlgCode","0", new UniformValueList(0,Enum.GetNames(typeof(AttractionRepulsionTransmittingEvadersPolicy.RoutingHeuristic)).Length-1,true )); } } // coresponds AttractionRepulsionTransmittingEvadersPolicy.RoutingHeuristic
                }

                // parameters used by BiasedRandomRoutingEvadersPolicy
                public static class BiasedRandomRoutingEvaders
                {
                    public static ArgEntry INCREASE_ON_ROW { get { return new ArgEntry("BiasedRandomRoutingEvadersPolicy.IncreaseOnRow", "1"); } } // val p in [0,1]: when placing evader, this increases (with p probability) the probability of placing other evaders in the same row increases
                    public static ArgEntry INCREASE_ON_COLUMN { get { return new ArgEntry("BiasedRandomRoutingEvadersPolicy.IncreaseOnCol", "0"); } } // val p in [0,1] : if probability on row increases, then (with p probability) the probability pf placing other evaders in the same row increases. experiments show this is mostly harmful for evaders
                    public static ArgEntry DECREASE_AROUND_CAPTURED { get { return new ArgEntry("BiasedRandomRoutingEvadersPolicy.DecreaseAroundCaptured", "0.5"); } } // val p in [0,1] : if an evader was captured, probability of placing evader in adjacent  (+-row and +-coloum)
                }
                public static class AttractionRepulsionRoutingPursuers
                {
                    public static ArgEntry X_AXIS_DIST_BIAS { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.XAxisDistBias", "100", new DiscreteValueList<int>(new int[] { 1, 100, 1000, 5000, 10000, 50000, 100000 }, false)); } } // value in [1,inf), tells how much more important the distance on X axis than dist on Y axis (only affects PURSUERS_HIT_INITIAL_BONUS_FACTOR consideration)
                    //public static ArgEntry PURSUER_VISIT_PENALTY_SIGNIFICANCE{get{return new ArgEntry("AttractionRepulsionRoutingPursuers.VisitPenaltySignificance", "0.5", new UniformValueList(0.01,2));}} // relatively to the bonus. I forgot why I added this and this looks unneeded, so currently it's removed

                    public static ArgEntry TRANSMISSION_DETECTION_INITIAL_BONUS_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.PbotTransmissionDetectionBonusFactor", "1", new UniformValueList(0, 1)); } } // value in [0,1] , refers to a ratio from the column/row, which will be the amount of bonux, relatively to graph width (i.e. for a row of length 10, the ratio can be anywhere from 0 to 10)
                    

                    public static ArgEntry PURSUERS_VISIT_INITIAL_PENALTY_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.PbotVisitInitialPenalty", "0.2", new UniformValueList(0, 1)); } } // value in [0,1] , refers to a ratio from the column/row, which will be the amount of penalty, relatively to graph width (i.e. for a row of length 10, the ratio can be anywhere from 0 to 10)
                    public static ArgEntry PURSUERS_VISIT_PENALTY_DISCOUNT_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.PbotVisitPenaltyDiscountFactor", "0.25", new UniformValueList(0, 0.95)); } } // value in [0,1), refers to how fast 

                    public static ArgEntry ENERGY_POWER_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.EnergyPowerFactor", "4.0", new UniformValueList(1, 11)); } } // value in [1,inf). before we choose row/column, we choose via roulette per row/column. this tells how much we scale the energy in the roulette

                    public static ArgEntry OPTIMIZE_ENERGY_CALCULATION { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.FastEnergyCalculation", "1"); } } // if 1, calculation of each point will be less accurate, but significantly faster

                    // used only if ebots can transmit:
                    public static ArgEntry OPTIMAL_EVENT_DISTANCE_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.OptEventDistanceFactor", "1.0", new DiscreteValueList<double>(new double[] { 0.5, 0.75, 1.0, 1.05, 1.1, 1.2, 1.5, 2, 3 }, false)); } } // factor is relative to r_e. if value is x, then if a point got a penalty/bonus, then all points in distance x will get the penalty/bonus. points 
                    public static ArgEntry DETECTED_BACKWARDS_BONUS_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.PbotDetectedForewardInitialBonusFactor", "1", new UniformValueList(0, 1)); } } // value in [0,1], relatively to graph width 
                    public static ArgEntry DETECTED_FORWARDS_BONUS_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.PbotDetectedBackwardInitialBonusFactor", "1", new UniformValueList(0, 1)); } } // value in [0,1] , relative to TRANSMISSION_BACKWARDS_BONUS_FACTOR (i.e. it's always smaller!)
                    public static ArgEntry DETECTED_BONUS_DISCOUNT_FACTOR { get { return new ArgEntry("AttractionRepulsionRoutingPursuers.PbotDetectedHitBonusDiscountFactor", "0.1", new UniformValueList(0, 0.95)); } } 
                }

                public static class ScanningRoutingPursuers
                {
                    public static ArgEntry DIAGONAL_SCAN { get { return new ArgEntry("ScanningRoutingPursuers.DiagonalScan", "0"); } } // if 1, scan will be on diagonal lines
                }
                public static class ColumnProbabalisticRoutingPursuers
                {
                    public static ArgEntry REPEAT_SUCCUSSFULL_COLUMN_HIT { get { return new ArgEntry("ColumnProbabalisticRoutingPursuers.RepeatSuccessfullColumnHit", "0.5"); } } //val p in [0,1] if a patroller captured an evader, with probability p the patroller will hit the same column again, without choosing column in random
                    public static ArgEntry PROBABILITY_PER_COLUMN { get { return new ArgEntry("ColumnProbabalisticRoutingPursuers.ProbPerColum", "0.5,0.1,0.1,0.1,0.1,0.1"); } } // divides columns to groups according to the amount of provided probabilities count
                }
                public static class RowDestroyingPursuerPolicy
                {
                    public static ArgEntry STOP_SCAN_ONFAILURE_PROBABILITY { get { return new ArgEntry("RowDestroyingPursuerPolicy.StopScanOnFailureProb","0.5"); } }
                    public static ArgEntry DUAL_DIRECTION_SEACH_PROB { get { return new ArgEntry("RowDestroyingPursuerPolicy.DualDirProb","0.5"); } }
                }
                public static class GeneticWindowFunctionEvadersPolicy
                {
                    public static ArgEntry WINDOW_CHROMOSOME { get { return new ArgEntry("GeneticWindowFunctionEvadersPolicy.Chromosome", GoE.Policies.GeneticWindowFunctionEvadersPolicy.WindowFunction.RowBiased.ToString()); } }
                }
            }

            public static class AdvRoutingPursuersPolicy
            {
                //public static ArgEntry UNIFORM_VISIT_GRAPH_SEARCH { get { return new ArgEntry("AdvRoutingPursuersPolicy.UseUniformVisitsGraphSearch(0/1)", "0"); } }
                //public static ArgEntry NAIVE_GRAPH_SEARCH { get { return new ArgEntry("AdvRoutingPursuersPolicy.UseNaiveGraphSearch(0/1)", "0"); } }
                //public static ArgEntry SMART_CONTINUOUS_SEARCH { get { return new ArgEntry("AdvRoutingPursuersPolicy.UseSmartContinuousSearch(0/1)", "0"); } }
                //public static ArgEntry EXHAUSTIVE_SEARCH { get { return new ArgEntry("AdvRoutingPursuersPolicy.UseExhaustiveSerach(0/1)", "1"); } }
                //public static ArgEntry CONTINUOUS_SEARCH { get { return new ArgEntry("AdvRoutingPursuersPolicy.UseContinuousSerach(0/1)", "0"); } }
                //public static ArgEntry CONTINUOUS_SEARCH_RESET_COUNTERS { get { return new ArgEntry("AdvRoutingPursuersPolicy.ContinuousSerachResetCounters(0/1)", "1"); } }
                //public static ArgEntry ARBITRARY_SMART_SEARCH { get { return new ArgEntry("AdvRoutingPursuersPolicy.UseSmartArbitrarySerach(0/1)", "0"); } }
                //public static ArgEntry ARBITRARY_SEARCH { get { return new ArgEntry("AdvRoutingPursuersPolicy.UseArbitrarySerach(0/1)", "0"); } }
            }
            public static class AdvRoutingPursuersPolicySimpleRecursiveSearchParams
            {
                // apparently nothing beats  MAX_EDGELEN_FOR_EXHAUSTIVE  = 2
                //public static ArgEntry MAX_EDGELEN_FOR_EXHAUSTIVE { get { return new ArgEntry("AdvRoutingPursuersPolicySimpleRecursiveSearchParams.MaxEdgeLenForExhaustiveSearch", "Log(N)"); } }
                public static ArgEntry MAX_COUNTER { get { return new ArgEntry("AdvRoutingPursuersPolicySimpleRecursiveSearchParams.MaxCounterValue", "65536"); } }
                public static ArgEntry ASSUME_TRANSMISSION_CONSTRAINT { get { return new ArgEntry("AdvRoutingPursuersPolicySimpleRecursiveSearchParams.AssumeTransmissionConstraint(cont/non/dflt)", "dflt"); } }
            }

            
            public static class AdvRoutingContInterKillerRouterPolicy
            {
                public static ArgEntry FALSE_TRANSMISSIONS_FIRST_HALF { get { return new ArgEntry("AdvRoutingContInterKillerRouterPolicy.FalseTransmissionFirstHalf", "5"); } } // for value x, there will be nlog^x(n) transmissions
                public static ArgEntry FALSE_TRANSMISSIONS_SECOND_HALF { get { return new ArgEntry("AdvRoutingContInterKillerRouterPolicy.FalseTransmissionSecondHalf", "3"); } } // for value x, there will be nlog^x(n) transmissions
            }
            public static class AdvRoutingAssumedRateInterKillerRouterPolicy
            {
                public static ArgEntry TOTAL_TIME_SPAN { get { return new ArgEntry("AdvRoutingAssumedRateInterKillerRouterPolicy.TotalTimeExp", "2"); } } // for value x, the time until game ends is (router count)^x
                public static ArgEntry REAL_TRANSMISSION_RATE { get { return new ArgEntry("AdvRoutingAssumedRateInterKillerRouterPolicy.RealTransmissionRateFactor", "0.5"); } } // 0 means continuous transmission, 1 means (total time span)/ nlog^2(n) (no point in lower rate, since recursive arbitrary alg. will find sink after nlog^2n anyway)
            }
            public static class AdvRoutingEnsureLowerBoundPolicy
            {
                public static ArgEntry FAKE_ROUTES{get{return new ArgEntry("AdvRoutingEnsureLowerBoundPolicy.FakeRoutes","1");}}

            }
            public static class AdvRoutingSegmentedRouteRouterPolicy
            {
                public static ArgEntry TRANSMIT_CONTINUOUSLY { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicy.TransmitContinuously(0/1)", "1"); } }
                public static ArgEntry STRATEGY_CODE { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicy.StrategyCode", "disconnected 0, totroutes 1, totrouters 1, falseroutes 0, falserouters 0, spreading -1"); } } // sepearates main route into segments with different number of parallel routes, and also tells the average length of false routes

                //"disconnected 0, totroutes 1, totrouters 1, falseroutes 0, falserouters 0"
                public static ArgEntry SEGMENTS_COUNT { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicy.SegmentsCount", "1",new UniformValueList(1,3, true)); } } //  sepearates main route into segments with different number of parallel routes, and also tells the average length of false routes

                public static ArgEntry ASSUME_EXHAUSTIVE_SEARCH { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicy.AssumeExhaustiveSerach", "1"); } }
                public static ArgEntry ASSUME_CONTINUOUS_SEARCH { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicy.AssumeExhaustiveSerach", "0"); } }
                //public static ArgEntry FORGETFUL_CONTINUOUS_SEARCH { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicy.AssumeForgetfulContinuousSerach", "0"); } }
                public static ArgEntry ASSUME_ARBITRARY_SEARCH { get { return new ArgEntry("AdvRoutingSegmentedRouteRouterPolicy.AssumeArbitrarySerach", "0"); } }
            }
            public static class AdvRoutingRoutersPolicyOptimizer
            {
                //public static ArgEntry MAX_SEGMENT_COUNT { get { return new ArgEntry("AdvRoutingRoutersPolicyOptimizer.MaxSegments", "3"); } }
                //public static ArgEntry MAX_SEGMENT_COUNT { get { return new ArgEntry("AdvRoutingRoutersPolicyOptimizer.MaxSegments", "3"); } }

            }
            public static class WSNRouterPolicyGridLP
            {
                public static ArgEntry ROUTER_STATISTICS_LENGTH
                {
                    get
                    {
                        return new ArgEntry("WSNRouterPolicyGridLP.RouterStatisticsLen", "5");
                    }
                }

                public static ArgEntry OPTIMIZE_TARGET
                {
                    get
                    {
                        return new ArgEntry("WSNRouterPolicyGridLP.OptimizedTarget(0_transmissionsCount,1_maximizeTotalFlow)", 
                            ((int)WSN.WSNRouterPolicyGridLP.OptimizationTarget.MaximizeTotalFlow).ToString(),
                            new UniformValueList(0, Enum.GetNames(typeof(WSN.WSNRouterPolicyGridLP.OptimizationTarget)).Length - 1, true));
                    }
                }
            }
        }
    }

}