using GoE.Policies;
using System.Collections.Generic;
using System.Drawing;
namespace GoE.GameLogic
{
    public struct PursuersLearnerInitData
    {
        public APursuersPolicy p;
        public IGameParams s;
        public GridGameGraph g;
    }

    /// <summary>
    /// base for classes that analyze the behaviour of pursuers.
    /// Even though each evaders policy may do this according to the given observations, "cheating"
    /// by using the knowledge of the specific pursuers policy may spare a lot of computation time. 
    /// </summary>
    /// <remarks>
    /// It is crucial to insure that the results of these learners are not better then what evaders could have gained 
    /// using 
    /// </remarks>
    public abstract class APursuersLearner : GoE.Utils.ReflectionUtils.DerivedTypesProvider<APursuersLearner>
    {

        /// <summary>
        /// called once, before any update() calls, and after pursuers policy was initialized
        /// </summary>
        /// <param name="d"></param>
        public abstract void init(PursuersLearnerInitData d);
        
        /// <summary>
        /// updates data/statistics according to the latest observations.
        /// Typically should be called every round
        /// NOTE: methods wasn't used yet, so it should probably look a little different before its usable
        /// </summary>
        /// <param name="O_p">
        /// locations of pursuers, as observed in the latest round
        /// </param>
        public virtual void update(HashSet<Point> O_p, List<Location> evaderLocations){}
        
    }
}