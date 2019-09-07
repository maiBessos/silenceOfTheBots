using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using GoE.Utils;
using System.Collections.Generic;

namespace GoE.Policies
{
    public abstract class APursuersPolicy : ReflectionUtils.DerivedTypesProvider<APursuersPolicy>
    {
        /// <summary>
        /// called once after object is constructed
        /// </summary>
        /// <param name="G"></param>
        /// <param name="prm"></param>
        /// <param name="pgui"></param>
        /// <param name="PreprocessResult"> 
        /// expected to include results from preProcess(), or from some other source (with the same keys e.g. a compatible optimizer)
        /// </param>
        /// <returns>
        /// false if game parameters don't allow using this policy
        /// </returns>
        public abstract bool init(AGameGraph G, IGameParams prm, IPolicyGUIInputProvider pgui, 
                  Dictionary<string, string> policyParams);

        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        public abstract void gameFinished();

        /// <summary>
        /// tells keys expected in 'input' for init() calls
        /// i.e. the names of the parameters needed by the policy before it executes,
        /// excluding values from game parameters file
        /// </summary>
        /// </summary>
        public abstract List<ArgEntry> policyInputKeys();

        /// <summary>
        /// constructs an uninitialized optimizer, that generates data to use as 'policyParams' in init()
        /// (this is the "default" optimizer).
        /// if NULL returned, no additional preprocess will be done before calling init()
        /// </summary>
        /// <returns></returns>
        public abstract APolicyOptimizer constructTheoreticalOptimizer();

    }
}