using AForge.Genetic;
using AForge.Math.Random;
using GoE.GameLogic.Algorithms;
using GoE.Policies;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using GoE.Utils.Extensions;
namespace GoE.GameLogic.EvolutionaryStrategy
{
    namespace EvaderSide
    {

        public delegate void NotifyBasicAlgorithmSuccess();

        /// <summary>
        /// describes a location from which routes start, and wait for other evaders to trasmit data into
        /// </summary>
        public class LocalSink
        {
            public int esimatedRoundsForListening { get; set; } // tells how many rounds are needed before evader is available to receive data
            public Point receivingAgentLocation { get; set; } // either current evader location, or the expected point in which the evader will be in 
            public Evader receivingAgent { get; set; } // if null, agent is not avilable to receive right now
            public Evader currentSendingAgent { get; set; } // if not null, then this evader is sending data, this round, into this sink
        }

        /// <summary>
        /// lets IEvaderBasicAlgorithm store/transfer the evaders it ownes, and letting
        /// the managing policy attach custom management data (by ineritance, if needed)
        /// </summary>
        public class TaggedEvader
        {
            public TaggedEvader() { }
            public TaggedEvader(TaggedEvader src)
            {
                e = src.e;
            }
            public TaggedEvader(Evader Eve) 
            {
                e = Eve;
            }

            public Evader e 
            {
                get; set;
            }
        }

        public class EvaluatedEvader
        {
            public EvaluatedEvader() { }
            public EvaluatedEvader(EvaluatedEvader src)
            {
                value = src.value;
                e = src.e;
            }
            
            public double value; // utility/cost
            public TaggedEvader e;
        }
        
        /// <summary>
        /// this struct is used only by WorldStateToActionEvaderManagerPolicy, but basic algorithms must 
        /// implement the method getRepairingNeeds() (so they are compatible with that evader manager policy)
        /// </summary>
        public struct RepairingNeeds
        {
            // TODO: do we also want a minimal utility limitation? e.g. a route that may be able to fix itself, but only by utilizing two very far (and cheap) evaders. maybe it's just not worth it?

            /// <summary>
            /// if we can't get this amount of evaders, repairing is impossible
            /// -1 means the algorithm decided to "self-destruct"
            /// </summary>
            public int minEvadersCount;
            public int maxEvadersCount; // the algorithm doesn't need more than this amount of evaders
            public List<EvaluatedEvader> priorityWeightPerEvader; // tells how suitable each evader is for repairing the alg. may be null iff minEvadersCount = 0
        }
       
        /// <summary>
        /// base class for evader basic algorithms.
        /// TODO: consider using special types for worth management, so we can't make mistakes.
        /// </summary>
        /// <remarks>
        /// the deriving class is obligated to call getRewardWorth() each time its successfull
        /// </remarks>
        public abstract class IEvaderBasicAlgorithm
        {

            public Dictionary<Evader, TaggedEvader> Evaders
            {
                get;
                protected set;
            }

            
            protected IEvaderBasicAlgorithm()
            {
                Evaders = new Dictionary<Evader, TaggedEvader>();
            }

            /// <summary>
            /// utility that helps manager policy to attach additional data to each IEvaderBasicAlgorithm
            /// it manages
            /// </summary>
            public object ManagerTag { get; set; }

            public void setSuccessNotifier(NotifyBasicAlgorithmSuccess successNotifier)
            {
                notifier = successNotifier;
            }
 
            /// <summary>
            /// tells what agents are suitable for the algorithm. called after worthPerEvader and budget data was updated
            /// </summary>
            public virtual RepairingNeeds getRepairingNeeds(IEnumerable<TaggedEvader> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, PursuerStatistics ps)
            {
                throw new Exception("getRepairingNeeds wasn't implemented!");
            }
            
            public abstract List<EvaluatedEvader> getEvaderEvaluations(IEnumerable<TaggedEvader> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, PursuerStatistics ps);

            /// <summary>
            /// readable description of what are the parameters and values  'param' describes
            /// (used as a "static" function - the result is same for all instances of the same type)
            /// </summary>
            /// <param name="param">
            /// populated chromosome, as generated from CreateNewParam()
            /// </param>
            public abstract Dictionary<string, string> getValueMap(AForge.Genetic.IChromosome param);

            /// <summary>
            /// each time the algorithm does its job successfully, it gets some reward by calling this method
            /// e.g. for an evader group that routes, each routed packet is considered success,
            /// for evaders that eavesdrop and then transmit data to sink/local sink is considered success,
            /// for an evader that is supposed to transmit noise then just evade - if evasion was successfull, 
            /// it's also a success.
            /// 
            /// The exact nature of the reward is determined by the evader manager policy
            /// </summary>
            protected void reportSuccess()
            {
                if (notifier != null)
                    notifier();
            }

            /// <summary>
            /// called automatically by the evader manager policy after a team of evaders were selected and added
            /// to this algorithm (and RepairBudget, EvadersWorth and worthPerEvader are already updated including new gained evaders)
            /// </summary>
            /// <remarks>
            /// NOTE: should be called by the managing policy only
            /// </remarks>
            public void handleNewEvaders(List<TaggedEvader> gainedEvaders)
            {
                foreach (var e in gainedEvaders)
                    handleNewEvader(e);
            }
            public virtual void handleNewEvader(TaggedEvader gainedEvader)
            {
                Evaders[gainedEvader.e] = gainedEvader;
            }

            /// <summary>
            /// tells the algorithm it must give up one of its evaders (because it was either destroyed, or moved to another repetitive state)
            /// The algorithm will later have a chance to repair itself by getting evaders from other algorithms.
            /// </summary>
            /// <remarks>
            /// NOTE: should be called by the managing policy only
            /// </remarks>
            public virtual void loseEvader(Evader lostEvader)
            {
                Evaders.Remove(lostEvader);
            }

            /// <summary>
            /// may be used by manager to rearrange which evader goes to which algorithm
            /// </summary>
            public virtual void loseAllEvaders()
            {
                Evaders.Clear();
            }


            /// <summary>
            /// coresponds to similar to EvadersPolicy's method - advances evaders, increases the worth of the alg, and updates evaders worth by calling getRewardWorth(), if needed
            /// TODO: worth shouldn't be increased by the state, but instead the manager should provide it
            /// </summary>
            public abstract Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep(GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, 
                HashSet<Point> O_d,
                HashSet<Point> O_p, 
                PursuerStatistics ps);

            /// <summary>
            /// generates a new, randomized input that is compatible for this action
            /// </summary>
            /// <returns></returns>
            public abstract AForge.Genetic.IChromosome CreateNewParam();

            /// <summary>
            /// creates a new managing object
            /// </summary>
            /// <returns>
            /// If using this action is not possible, return null
            /// </returns>
            public abstract IEvaderBasicAlgorithm CreateNew(AForge.Genetic.IChromosome param);

            /// <summary>
            /// invoked when this algorithm does its job successfully
            /// </summary>
            private NotifyBasicAlgorithmSuccess notifier = null;
        }
    }
}