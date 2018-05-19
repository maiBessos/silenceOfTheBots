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

namespace GoE.GameLogic
{

    
    public class GoEGameParams : AGameParams<GoEGameParams>
    {
        /// <summary>
        /// constructing objects this way is dicouraged.
        /// use  AGameParams<GameParams>.getClearParams() instead
        /// </summary>
        public GoEGameParams() 
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
            A_E.Clear();
            A_P.Clear();
            areSinksSafe = true;
            canEvadersReceiveMultipleBroadcasts = true;
            canSinksSensePursuers = false;
            R = null;
            r_e = 0;
            r_p = 0;
            r_s = 0;
        }
        
        /// <summary>
        /// if true, evaders that get more than one transmission at the same round, know how to filter it out
        /// if they already have the data stored
        /// </summary>
        public bool canEvadersReceiveMultipleBroadcasts { get; set; }

        /// <summary>
        /// if false, gives a significant boost for game process, but won't allow evaders to sense 
        /// pursuers that are near the sinks
        /// </summary>
        public bool canSinksSensePursuers { get; set; }

        /// <summary>
        /// if false, gives significant boost for game process, but sinks are not safe for evaders. 
        /// </summary>
        public bool areSinksSafe { get; set; }

        public float EvaderCircumferenceEntranceKillProb { get; set; }

        public float EvaderCircumferenceEntranceKillProbWithPC(double p_c)
        {
            double surviveProb = (1 - EvaderCircumferenceEntranceKillProb) * (1 - p_c);
            return (float)(1 - surviveProb);
        }

        /// <summary>
        /// The set of agents in the evading-eavesdropper side (with cardinality of \eta)
        /// </summary>
        public List<Evader> A_E { get; set; }

        /// <summary>
        /// The set of agents in the pursuing-patroller side (with cardinality of \psi)
        /// </summary>
        public List<Pursuer> A_P { get; set; }

        /// <summary>
        /// Maximal distance pursuing-patroller side agents can go each round 
        /// </summary>
        public int r_p { get; set; }

        /// <summary>
        /// probability of detecting a transmissions, by pursuer
        /// </summary>
        public double p_d { get; set; }

        /// <summary>
        /// Maximal distance from a target in which evading-eavesdropper side agents can eavesdrop from
        /// </summary>
        public int r_e { get; set; }

        /// <summary>
        /// Maximal distance in which evading-eavesdropper side agents can sense the location of other agents
        /// </summary>
        public int r_s { get; set; }

        /// <summary>
        /// Non increasing function that maps an age of a data unit to the reward given to evading-eavesdropper side upon transmitting the packet to the sink
        /// </summary>
        public ARewardFunction R { get; set; }

        public override void fromValueMap(Dictionary<string,string> vals)
        {
            
            int evaderCount = Int32.Parse(vals[GameParamsValueNames.EVADERS_COUNT.key]);
            int pursuerCount = Int32.Parse(vals[GameParamsValueNames.PURSUERS_COUNT.key]);
            r_p = Int32.Parse(vals[GameParamsValueNames.PURSUERS_VELOCITY.key]);
            r_e = Int32.Parse(vals[GameParamsValueNames.EVADERS_TRANSMISSION_RANGE.key]);
            r_s = Int32.Parse(vals[GameParamsValueNames.EVADERS_SENSING_RANGE.key]);

            p_d = double.Parse(
                GameParamsValueNames.PURSUERS_DETECTION_PROB.tryRead(vals));
                

            canEvadersReceiveMultipleBroadcasts = (vals[GameParamsValueNames.EVADERS_CAN_RECEIVE_SIMULTENOUS_BROADCASTS.key] == "1");
            canSinksSensePursuers = (vals[GameParamsValueNames.SINKS_SENSE_PURSUERS.key] == "1");
            areSinksSafe = (vals[GameParamsValueNames.SINKS_SAFE_POINTS.key] == "1");

            EvaderCircumferenceEntranceKillProb = float.Parse(GameParamsValueNames.EVADERS_CIRCUMFERENCE_ENTRY_KILL_PROB.tryRead(vals));

            R = ReflectionUtils.constructEmptyCtorType<ARewardFunction>(vals[GameParamsValueNames.REWARD_FUNCTION_NAME.key]);
            R.setArgs(vals[GameParamsValueNames.REWARD_FUNCTION_ARGS.key]);

            A_E = Evader.getAgents(evaderCount);
            A_P = Pursuer.getAgents(pursuerCount);
                
        }
        public override Dictionary<string, string> toValueMap()
        {
            Dictionary<string, string> vals = new Dictionary<string, string>();
            vals[GameParamsValueNames.EVADERS_COUNT.key] = A_E.Count().ToString();
            vals[GameParamsValueNames.PURSUERS_COUNT.key] = A_P.Count().ToString();
            vals[GameParamsValueNames.PURSUERS_VELOCITY.key] = r_p.ToString();
            vals[GameParamsValueNames.EVADERS_TRANSMISSION_RANGE.key] = r_e.ToString();
            vals[GameParamsValueNames.EVADERS_SENSING_RANGE.key] = r_s.ToString();
            vals[GameParamsValueNames.PURSUERS_DETECTION_PROB.key] = p_d.ToString();
            vals[GameParamsValueNames.REWARD_FUNCTION_NAME.key] = R.GetType().Name;
            vals[GameParamsValueNames.REWARD_FUNCTION_ARGS.key] = R.ArgsCSV;
            vals.AddRange(R.ArgsData());
            vals[GameParamsValueNames.EVADERS_CAN_RECEIVE_SIMULTENOUS_BROADCASTS.key] = (canEvadersReceiveMultipleBroadcasts == true)?("1"):("0");
            vals[GameParamsValueNames.SINKS_SENSE_PURSUERS.key] = (canSinksSensePursuers == true)? ("1") : ("0");
            vals[GameParamsValueNames.SINKS_SAFE_POINTS.key] = (areSinksSafe == true) ? ("1") : ("0");
            vals[GameParamsValueNames.EVADERS_CIRCUMFERENCE_ENTRY_KILL_PROB.key] = EvaderCircumferenceEntranceKillProb.ToString();
            return vals;
        }

    }
}
