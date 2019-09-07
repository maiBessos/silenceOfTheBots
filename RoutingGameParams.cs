using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using GoE.Utils.Extensions;
using GoE.AppConstants.GameLogic;
using System.Drawing;

namespace GoE.GameLogic
{


    public class FrontsGridRoutingGameParams : AGameParams<FrontsGridRoutingGameParams>
    {
        /// <summary>
        /// constructing objects this way is dicouraged.
        /// use  AGameParams<RoutingGameParams>.getClearParams() instead
        /// </summary>
        public FrontsGridRoutingGameParams() 
        {
            A_E = new List<Evader>();
            A_P = new List<Pursuer>();
        }

        /// <summary>
        /// replaces this thread's previous params, with a clear instance
        /// </summary>
        /// <returns></returns>
        protected override void initClearParams()
        {
            detectionProbRestraint = double.Parse(FrontsGridRoutingGameParamsValueNames.DETECTION_PROBABILITY_RETRAINT.val);
            RewardPortion = float.Parse(FrontsGridRoutingGameParamsValueNames.REWARD_PORTION_TO_CONSIDER.val);
            A_E.Clear();
            A_P.Clear();
            r_e = 0; 
            f_e = float.Parse(FrontsGridRoutingGameParamsValueNames.EVADER_RENEWAL_RATE_PER_STEP.val);
        }

        public override void fromValueMap(Dictionary<string, string> vals)
        {
            detectionProbRestraint = double.Parse(FrontsGridRoutingGameParamsValueNames.DETECTION_PROBABILITY_RETRAINT.tryRead(vals));
            RewardPortion = float.Parse(FrontsGridRoutingGameParamsValueNames.REWARD_PORTION_TO_CONSIDER.tryRead(vals));
            int evaderCount = Int32.Parse(vals[GameParamsValueNames.EVADERS_COUNT.key]);
            int pursuerCount = Int32.Parse(vals[GameParamsValueNames.PURSUERS_COUNT.key]);
            r_e = Int32.Parse(vals[GameParamsValueNames.EVADERS_TRANSMISSION_RANGE.key]);
            A_E = Evader.getAgents(evaderCount);
            A_P = Pursuer.getAgents(pursuerCount);
            f_e = float.Parse(FrontsGridRoutingGameParamsValueNames.EVADER_RENEWAL_RATE_PER_STEP.tryRead(vals));
        }
        public override Dictionary<string, string> toValueMap()
        {
            Dictionary<string, string> vals = new Dictionary<string, string>();
            vals[GameParamsValueNames.EVADERS_COUNT.key] = A_E.Count().ToString();
            vals[GameParamsValueNames.PURSUERS_COUNT.key] = A_P.Count().ToString();
            vals[GameParamsValueNames.EVADERS_TRANSMISSION_RANGE.key] = r_e.ToString();
            vals[FrontsGridRoutingGameParamsValueNames.EVADER_RENEWAL_RATE_PER_STEP.key] = f_e.ToString();
            vals[FrontsGridRoutingGameParamsValueNames.REWARD_PORTION_TO_CONSIDER.key] = RewardPortion.ToString();
            vals[FrontsGridRoutingGameParamsValueNames.DETECTION_PROBABILITY_RETRAINT.key] = detectionProbRestraint.ToString();

            return vals;
        }

        public float RewardPortion { get; set; }
        /// <summary>
        /// The set of agents in the evading-eavesdropper side (with cardinality of \eta)
        /// </summary>
        public List<Evader> A_E { get; set; }

        /// <summary>
        /// The set of agents in the pursuing-patroller side (with cardinality of \psi)
        /// </summary>
        public List<Pursuer> A_P { get; set; }
        
        /// <summary>
        /// Maximal Distance between transmitting evaders
        /// </summary>
        public int r_e { get; set; }

        public double detectionProbRestraint
        {
            get; set;
        }

        /// <summary>
        /// value in (0, inf) - how many additional evaders are added to the game each game step
        /// </summary>
        public float f_e { get; set; }
        
    }
}
