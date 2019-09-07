using GoE.GameLogic;
using GoE.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoE.Utils.Extensions;
using System.IO;
using GoE.AppConstants;

namespace GoE.Policies
{
    /// <summary>
    /// interface for all classes that attempt improving the performance of one or two policies
    /// TODO: this interface really isn't very usefull, apart for reflection. Consider at least adding generics
    /// </summary>
    public abstract class APolicyOptimizer : ReflectionUtils.DerivedTypesProvider<APolicyOptimizer>
    {

        /// <summary>
        /// tells which keys will be in optimizationOutput (including *optionally* overriden keys)
        /// </summary>
        public abstract List<string> optimizationOutputKeys { get; }

        /// <summary>
        /// tells keys expected in 'input' for init() calls
        /// </summary>
        public abstract List<ArgEntry> optimizationInputKeys { get; }

        protected string logID;
        protected int repetitionCount;
        protected AGameGraph gameGraph;
        protected IGameParams igameParams;
        protected Dictionary<string, string> policyInput;
        
        protected void writeLog(string line)
        {
            AppSettings.WriteLogLine(line);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="pp"></param>
        /// <param name="gameGraph"></param>
        /// <param name="input">
        /// includes policies input + optimizer specific parameters
        /// </param>
        /// <paparam name="log">
        /// slow optimizers may report progress using this log, each line will have 'LogID' appended
        /// </paparam>
        public void init(AGameGraph GameGraph,
                         IGameParams GameParams,
                         Dictionary<string, string> input)
        {
            policyInput = new Dictionary<string, string>(input);
            gameGraph = GameGraph;
            igameParams = GameParams;

            repetitionCount =
                int.Parse(GoE.AppConstants.Algorithms.Optimizers.REPETITION_COUNT_KEY.tryRead(policyInput));

            initEx();
        }

        public abstract void process(ParallelOptions opt = null);

        /// <summary>
        /// called automatically after init() is called, and all protected members are initialized
        /// </summary>
        protected abstract void initEx();
        
        

        /// <summary>
        /// may be accessed after process() is done.
        /// tells the estimated leaked data bound in the practical simulation, and the policy parameters needed for both sides to reach it
        /// </summary>
        public abstract GameResult optimizationOutput { get; protected set; }
    }
}