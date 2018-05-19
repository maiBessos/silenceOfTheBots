using GoE.GameLogic;
using GoE.GameLogic.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.AppConstants;
using System.IO;
using GoE.Utils.Algorithms;
using GoE.Policies;
using GoE.Utils.Extensions;
using GoE.Utils;

namespace GoE
{
  
    public class SimArguments
    {
        public bool cmdLineOnly = false; // automatically closes after process

            
        public List<ArgFile> argFiles = new List<ArgFile>();

        public int verboseLvl = Int32.Parse(AppConstants.CmdLineArguments.VERBOSE_LEVEL.val);
        public int maxParallelArgFiles = Int32.Parse(AppConstants.CmdLineArguments.MAX_PARALLEL_ARGFILES.val);
            
        public List<string> errorLog = new List<string>();

        /// <summary>
        /// allows running a single ArgFile
        /// </summary>
        /// <param name="cmdLineArgs">
        /// parameters with keys from AppConstants.CmdLineArguments
        /// NOTE:
        /// CmdLineArguments.MAX_PARALLEL_ARGFILES is ignored (only 1 arg file will run in parallel)
        /// </param>
        /// <param name="processArgs"></param>
        public SimArguments(Dictionary<string,string> cmdLineArgs, ArgFile args)
        {
            maxParallelArgFiles = 1;
            argFiles.Add(args);
        }

        /// <summary>
        /// processes Command line arguments
        /// </summary>
        /// <param name="argv"></param>
        public SimArguments(string[] argv)
        {
            for (int ai = 0; ai < argv.Count(); ++ai)
            {
                if (argv.Count() == 0)
                    continue;

                // all arguments that don't start with '-' either follow a '-' argument (handled later),
                // or just specify a file to process
                if (argv[ai][0] != '-')
                {
                    if (FileUtils.TryFindingFile(argv[ai]) != "")
                    {
                        try
                        {
                            if (argv[ai].EndsWith(FileExtensions.ARG_LIST))
                            {
                                List<string> fileNames = new List<string>(File.ReadAllLines(FileUtils.TryFindingFile(argv[ai])));
                                foreach(var fn in fileNames)
                                    argFiles.Add(new ArgFile(fn));
                            }
                            else
                                argFiles.Add(new ArgFile(FileUtils.TryFindingFile(argv[ai])));
                        }
                        catch(Exception ex)
                        {
                            errorLog.Add("can't load ArgFile " + argv[ai] + ":" + ex.Message);
                        }
                        cmdLineOnly = true;
                    }
                    else
                        errorLog.Add(argv[ai] + " is not a file name (unrecognized argument) - ignored");
                    continue;
                }

                string argstr = argv[ai].Substring(1);
                ++ai; 
                // we now process the argument that comes after argstr:
                //if (argstr == AppConstants.CmdLineArguments.GAME_MODEL.key)
                //{
                //    if(AGameProcess.ChildrenByTypename.ContainsKey(argv[ai]))
                //        gameType = argv[ai];
                //    else
                //        errorLog.Add(argv[ai] + " is not a game type (unrecognized argument) - ignored");
                //}
                //else 
                if (argstr == AppConstants.CmdLineArguments.MAX_PARALLEL_ARGFILES.key)
                {
                    if (!Int32.TryParse(argv[ai], out maxParallelArgFiles))
                        errorLog.Add("arg files thread count couldn't be parsed. Using: " + maxParallelArgFiles.ToString());
                }
                else if(argstr == AppConstants.CmdLineArguments.VERBOSE_LEVEL.key)
                {
                    if (!Int32.TryParse(argv[ai], out verboseLvl))
                        errorLog.Add("verbose level couldn't be parsed. Using: " + verboseLvl.ToString());
                }
                else if (argstr == AppConstants.CmdLineArguments.SINGLE_FILE.key)
                {
                    string fileS = "";
                    for (int i = ai; i < argv.Count(); ++i)
                        fileS += argv[i] + " ";
                    argFiles.Add(new ArgFile(FileUtils.TryFindingFile(fileS)));
                }

                
                cmdLineOnly = true;
            }
        }
    }

    public class ArgFile
    {
        /// <summary>
        /// expects a text file containing a serialized dictionary (see parseValueMap() ), 
        /// and the list of key value pairs are treated as ArgEntry objects (see class ArgEntry)
        /// 
        /// special control chars:
        /// one(at most!) of the ArgEntry objects may contain a varying value. all different values will be processed and output file will form a table
        /// 
        /// varying ArgEntry must not be the one of AppArgumentKeys constants (excluding AppArgumentKeys.PARAM_FILE_PATH)
        /// ******************
        /// AppArgumentKeys.PARAM_FILE_PATH : 
        /// must be specified in file.
        /// points to a file/directory with files, where each file contains additional ArgEntry object to be merged with main ArgEntry list.
        /// if folder with several files is given, the ArgEntry with varying value is derived.
        /// 
        /// AppArgumentKeys.GRAPH_FILE :
        /// must be specified in file
        /// 
        /// AppArgumentKeys.POLICY_OPTIMIZER :
        /// may override other parameters in the file, after process starts (depending on the specific optimizer)
        /// 
        /// constants of CmdLineArguments : 
        /// must not be included in the file
        /// </summary>
        /// <param name="argFileName"></param>
        public ArgFile(string argFileName)
        {
            argFileName = FileUtils.TryFindingFile(argFileName);
            Dictionary<string, string> rawFile = Utils.ParsingUtils.parseValueMap(Utils.ParsingUtils.clearComments(File.ReadAllLines(argFileName)));
            init(rawFile);
            FileName = argFileName;
        }

        /// <summary>
        /// if argFileData contains the key AppArgumentKeys.PARAM_FILE_PATH (and it specifies a folder),
        /// it will be used to deduce multi value key (see ArgFile(string argFileName) )
        /// </summary>
        /// <param name="argFileData"></param>
        public ArgFile(Dictionary<string, string> argFileData)
        {
            init(argFileData);
            FileName = "temporary file";
        }

        private void init(Dictionary<string, string> argFileData)
        {
            ProcessArgumentList additionalValues = null;
            string filePath = AppArgumentKeys.PARAM_FILE_PATH.tryRead(argFileData);
            if(filePath != "")
                additionalValues = new ProcessArgumentList(filePath);

            processParams = new ProcessArgumentList(ArgEntry.fromDictionary(argFileData));

            if (additionalValues != null)
            {
                processParams.add(additionalValues, false);
            }
        }


        public string FileName  { get;protected set;}
        public ProcessArgumentList processParams { get; protected set; }

    }

    public class ParallelizationManager
    {
        public ParallelOptions parallelArgFiles = new ParallelOptions();
        public ParallelOptions parallelArgValues = new ParallelOptions();
        public ParallelOptions parallelSimRuns = new ParallelOptions();

        public ParallelizationManager(SimArguments args)
        {
            parallelArgFiles.MaxDegreeOfParallelism = args.maxParallelArgFiles;

            int maxSimRunThreadCount = 1; // max allowed thread count of ALL given arg files
            foreach (var argFile in args.argFiles)
            {
                int argThreads;

                try
                {
                    argThreads = Int32.Parse(AppConstants.AppArgumentKeys.THREAD_COUNT.tryRead(argFile.processParams[0]));
                }
                catch(Exception ex)
                {
                    throw new Exception("couldn't parse field AppConstants.AppArgumentKeys.THREAD_COUNT from arg file:" + argFile.FileName);
                }

                maxSimRunThreadCount = Math.Max(maxSimRunThreadCount, argThreads);
            }
                
            if (args.argFiles.Count >= maxSimRunThreadCount * 2)
            {
                // if outer loop may be parallelized well enough, we use only it - it's the most efficient way for parallelization, perhaps because we minimize concurrent mem. allocations
                parallelArgValues.MaxDegreeOfParallelism = maxSimRunThreadCount;
                parallelSimRuns.MaxDegreeOfParallelism = 1;
            }
            else if (args.argFiles.Count == 1)
            {
                parallelArgValues.MaxDegreeOfParallelism = 1;
                parallelSimRuns.MaxDegreeOfParallelism = maxSimRunThreadCount;
            }
            else
            {
                // in this case, we might use extra threads (might be a problem if we want to use all cpu cores except for 1)
                parallelArgValues.MaxDegreeOfParallelism = 2;
                parallelSimRuns.MaxDegreeOfParallelism = maxSimRunThreadCount / 2;
            }
        }
    }

    public class ProcessOutput
    {
        public Dictionary<string, string> processOutput = null;
        public Dictionary<string, string> theoryOutput = null;
        public Dictionary<string, string> optimizerOutput = null;
    }

    public static class SimProcess
    { 
        public static APolicyOptimizer getOptimizedPolicyInput(ParallelOptions parallelOptInner,
                                                                            Dictionary<string, string> processArgs,
                                                                            AGameGraph graph,
                                                                            AGameProcess game,
                                                                            IGameParams gameParams)
        {
            // use optimizer, if specified
            string optimizerName = AppConstants.AppArgumentKeys.POLICY_OPTIMIZER.tryRead(processArgs);
            
            if (optimizerName == "")
                return null;

            Type policyOptimizerType = APolicyOptimizer.ChildrenByTypename[optimizerName];

            //AppSettings.WriteLogLine("invoking optimizer ...");

            APolicyOptimizer opt = (APolicyOptimizer)policyOptimizerType.GetConstructor(new Type[] { }).Invoke(new object[] { });
            opt.init(graph, gameParams, processArgs);
            opt.process(parallelOptInner);
            return opt;
        }

        /// <summary>
        /// tells which keys in processArgs (which's values aren't "") will be be used when calling Process.processParams(),
        /// even though optimizer and pursuers policy also produce these key-values (and will be overridden by the existing values in 'processArgs')
        /// </summary>
        /// <param name="processArgs">
        /// keys with "" value are ignored and won't be listed in return value
        /// </param>
        /// <returns></returns>
        public static List<string> getCollidingKeys(Dictionary<string, string> processArgs)
        {
            string currentOptimizer = AppConstants.AppArgumentKeys.POLICY_OPTIMIZER.tryRead(processArgs);
            string currentPursuerPolicy = AppConstants.AppArgumentKeys.POLICY_OPTIMIZER.tryRead(processArgs);
            
            
            List<string> outVals = null, inVals = null;
            List<string> res = new List<string>();
            try{
                // if there's an optimizer 
                outVals = ReflectionUtils.constructEmptyCtorType<APolicyOptimizer>(currentOptimizer).optimizationOutputKeys;
                inVals = ReflectionUtils.constructEmptyCtorType<APolicyOptimizer>(currentOptimizer).optimizationInputKeys.ConvertAll<string>((ArgEntry a)=> { return a.key; });
            }
            catch (Exception)
            {
                try
                {
                    // if there's no optimizer, theoretical optimization from estimator (which means from pursuers policy),
                    // but these will *not* override values in policy input
                    outVals = ReflectionUtils.constructEmptyCtorType<APursuersPolicy>(currentPursuerPolicy).constructTheoreticalOptimizer().optimizationOutputKeys;
                    inVals = ReflectionUtils.constructEmptyCtorType<APolicyOptimizer>(currentPursuerPolicy).optimizationInputKeys.ConvertAll<string>((ArgEntry a) => { return a.key; });
                }
                catch (Exception)
                {
                    return res;
                }
            }
            

            // if value is no overridden, or the optimizer expects it to be overridden, then there is no problem
            foreach (string s in outVals)
                if (processArgs.ContainsKey(s) && !inVals.Contains(s) && processArgs[s] != "")
                    res.Add(s);
            return res;
        }

        /// <summary>
        /// constructs pursuers policy object, and returns its theoretical optimizer's output
        /// </summary>
        /// <param name="pursuersPolicy"></param>
        /// <param name="graph"></param>
        /// <param name="gameParams"></param>
        /// <param name="processArgs"></param>
        /// <returns></returns>
        public static Dictionary<string, string> getPursuersPolicyTheoreticalOptimizerResult(string pursuersPolicy,
                                                                                                AGameGraph graph,
                                                                                                IGameParams gameParams,
                                                                                                Dictionary<string, string> processArgs)
        {
            // construct a dummy policy, just to make the one-time calculation of preprocess
            APursuersPolicy initPP = ReflectionUtils.constructEmptyCtorType<APursuersPolicy>(pursuersPolicy);
            var theoryOptimizer = initPP.constructTheoreticalOptimizer();

            if (theoryOptimizer == null)
                return new Dictionary<string, string>();

            theoryOptimizer.init(graph, gameParams, processArgs);
            theoryOptimizer.process();
            return theoryOptimizer.optimizationOutput;
        }

        /// <summary>
        /// assumes 'processArgs' had a specific optimizer and/or policies, and uses them for the process
        /// </summary>
        /// <param name="parallelOptInner"></param>
        /// <param name="initialProcessArgs">
        /// empty values (i.e. keys with empty strings) will be ignored
        /// </param>
        /// <param name="graph"></param>
        /// <param name="optimizeInitialProcessArgs">
        /// if false, theoryOutput and optimizerOutput will not be populated
        /// and no optimizer will be used to override any of the values in optimizeInitialProcessArgs
        /// </param>
        /// <returns></returns>
        public static ProcessOutput processParams(ParallelOptions parallelOptInner,
                                                  Dictionary<string, string> initialProcessArgs,
                                                  AGameGraph graph,
                                                  bool optimizeInitialProcessArgs = true,
                                                  bool calcStdDev = true)
        {
            ProcessOutput res = new ProcessOutput();
            var processArgs = new Dictionary<string, string>(initialProcessArgs);
            foreach (var val in initialProcessArgs)
                if (val.Value == "")
                    processArgs.Remove(val.Key);
            
            // allocate game process
            string gameType = AppConstants.AppArgumentKeys.GAME_MODEL.tryRead(processArgs);
            AGameProcess game = Utils.ReflectionUtils.constructEmptyCtorType<AGameProcess>(gameType);

            // load game params file. processArgs may override some of the loaded values
            IGameParams gameParams = game.constructGameParams();
            gameParams.deserialize(AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(processArgs), processArgs);

            APolicyOptimizer preferredOptimizer = null;// null if no optimizer specified
            if (optimizeInitialProcessArgs)
                preferredOptimizer = getOptimizedPolicyInput(parallelOptInner, processArgs, graph, game, gameParams);
            
            // if an optimizer was specified, it may have the freedom to choose from several policies, and also provide theoretical bounds *given this freedom*
            // note that the optimizer may attempt choosing policies, but can't override values in init file. 
            string evadersPolicy, pursuersPolicy;
            if (preferredOptimizer != null)
            {
                res.theoryOutput = new Dictionary<string, string>(processArgs);

                // FIXME: for creating theoryOutput, if pursuers policy wasn't specificed in input params, we throw an exception.
                // this is not necessarily the correct way to do this, since theoretically the optimizer is the one that decides what pursuers policy
                // to use
                res.theoryOutput.AddRange(getPursuersPolicyTheoreticalOptimizerResult(AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(processArgs,""), graph, gameParams, processArgs));

                processArgs.AddRange(preferredOptimizer.optimizationOutput, false);
                
                res.optimizerOutput = new Dictionary<string, string>(processArgs);
                res.optimizerOutput.AddRange(preferredOptimizer.optimizationOutput);
            }
            
            // evadersPolicy and pursuersPolicy are extracted after the optimizer is used, since the optimizer may choose them
            evadersPolicy = AppConstants.AppArgumentKeys.EVADER_POLICY.tryRead(processArgs);
            pursuersPolicy = AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(processArgs);

            // if no optimizer specified, the theoretical results are calculated by the estimator (which assumes specific evaders policy and pursuers policy are given in processArgs)
            // similarly to the optimizer, the pursuer's policy can't override previous data in processArgs
            if (preferredOptimizer == null && optimizeInitialProcessArgs)
            {
                res.theoryOutput = new Dictionary<string, string>(processArgs);
                var theoreticalOptimizerRes = getPursuersPolicyTheoreticalOptimizerResult(pursuersPolicy, graph, gameParams, processArgs);
                res.theoryOutput.AddRange(theoreticalOptimizerRes);
                processArgs.AddRange(theoreticalOptimizerRes, false);
            }
            res.processOutput = 
                getEstimatedResultsAverage(parallelOptInner, evadersPolicy, pursuersPolicy, gameParams, graph, processArgs, game,calcStdDev);
            
            return res;
        }


        /// <summary>
        /// processes the specific scenario, and runs simulation according to params in 'policyInput'
        /// NOTE: 1) 'policyInput' is assumed to include AppArgumentKeys.SIMULATION_REPETETION_COUNT
        /// NOTE: 2) results will contain policyInput
        /// </summary>
        /// <param name="parallelOptInner"></param>
        /// <param name="evadersPolicy"></param>
        /// <param name="pursuersPolicy"></param>
        /// <param name="p"></param>
        /// <param name="g"></param>
        /// <param name="policyInput"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public static Dictionary<string,string> getEstimatedResultsAverage(ParallelOptions parallelOptInner,
                                                                    string evadersPolicy, string pursuersPolicy, GameLogic.IGameParams p, AGameGraph g, 
                                                                    Dictionary<string,string> policyInput,
                                                                    AGameProcess game, 
                                                                    bool calcStdDev)
        {
            
            int repetetionCount = int.Parse(AppConstants.AppArgumentKeys.SIMULATION_REPETETION_COUNT.tryRead(policyInput));
            if (repetetionCount <= 0)
                return null;

            PerformanceEstimation estimator = new PerformanceEstimation(evadersPolicy,
                                                                        pursuersPolicy,
                                                                        p,
                                                                        g);
            
            var res = new Dictionary<string, string> (policyInput);
            res.AddRange(
                Utils.Algorithms.AlgorithmUtils.Average(
                    estimator.estimatePerformance(
                    Utils.ReflectionUtils.constructEmptyCtorTypeFromObj<AGameProcess>(game), 
                    repetetionCount, policyInput, int.MaxValue, false, parallelOptInner),calcStdDev));
            
            return res;
        }

        public static List<List<ProcessOutput>> process(SimArguments args)
        {
            ParallelizationManager trdMgr = new ParallelizationManager(args);
            List<List<ProcessOutput>> res = new List<List<ProcessOutput>>();

            Parallel.For(0, args.argFiles.Count, trdMgr.parallelArgFiles, fileIdx =>
            {
                ArgFile af = args.argFiles[fileIdx];
                AppSettings.WriteLogLine("Processing Arg File:" + af.FileName);

                // load graph file
                AGameGraph graph = null;

                AppSettings.WriteLogLine("loading graph...");
                string graphVal = af.processParams[0][AppConstants.AppArgumentKeys.GRAPH_FILE.key];
                Utils.Exceptions.ConditionalTryCatch<Exception>(() =>
                {

                    if (graphVal.EndsWith(FileExtensions.GRAPH_FILE))
                    {
                        graph = AGameGraph.loadGraph(File.ReadAllLines(FileUtils.TryFindingFile(graphVal)));
                        //graph = new GridGameGraph(af.processParams[0][AppConstants.AppArgumentKeys.GRAPH_FILE.key]);
                    }
                    else
                        graph = AGameGraph.loadGraph(graphVal);

                },
                (Exception ex) =>
                {
                    AppSettings.WriteLogLine("couldn't load graph file:" + ex.Message);
                });

                AppSettings.WriteLogLine("graph loaded. processing...");
                // populate output table (ValuesCount is the amount of different values from param file, each should have separate theory/sim/optimizer process)
                res.Add(AlgorithmUtils.getRepeatingValueList<ProcessOutput>(af.processParams.ValuesCount));
                populateResults(res.Last(), af.processParams, trdMgr.parallelArgValues, trdMgr.parallelSimRuns, graph);
            });

            return res;
        }

        public static void populateResults(List<ProcessOutput> res, 
                                           ProcessArgumentList gameParams,
                                           ParallelOptions parallelArgValues,
                                           ParallelOptions parallelSimRuns,
                                           AGameGraph graph)
        {
            Parallel.For(0, gameParams.ValuesCount, parallelArgValues, valIdx =>
            {
                Dictionary<string, string> processArgs = gameParams[valIdx];
                res[valIdx] =
                    processParams(parallelSimRuns, processArgs, graph);
            });
        }

    }
}
