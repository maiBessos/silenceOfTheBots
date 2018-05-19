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


    public class IntrusionGameParams : AGameParams<IntrusionGameParams>
    {
        /// <summary>
        /// constructing objects this way is dicouraged.
        /// use  AGameParams<IntrusionGameParams>.getClearParams() instead
        /// </summary>
        public IntrusionGameParams() 
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
            r_e = 0; 
            r_p = 0;
            r_es = 0;
            t_i = 0;
        }

        public override void fromValueMap(Dictionary<string, string> vals)
        {

            int evaderCount = Int32.Parse(vals[GameParamsValueNames.EVADERS_COUNT.key]);
            int pursuerCount = Int32.Parse(vals[GameParamsValueNames.PURSUERS_COUNT.key]);
            r_p = Int32.Parse(vals[GameParamsValueNames.PURSUERS_VELOCITY.key]);
            r_e = Int32.Parse(vals[GameParamsValueNames.EVADERS_TRANSMISSION_RANGE.key]);
            r_es = Int32.Parse(vals[GameParamsValueNames.EVADERS_SENSING_RANGE.key]);
            t_i = Int32.Parse(vals[IntrusionGameParamsValueNames.INTRUSION_TIME.key]);
            A_E = Evader.getAgents(evaderCount);
            A_P = Pursuer.getAgents(pursuerCount);

            IsAreaSquare = ("0" != IntrusionGameParamsValueNames.IS_AREA_SQUARE.tryRead(vals));
                
        }
        public override Dictionary<string, string> toValueMap()
        {
            Dictionary<string, string> vals = new Dictionary<string, string>();
            vals[GameParamsValueNames.EVADERS_COUNT.key] = A_E.Count().ToString();
            vals[GameParamsValueNames.PURSUERS_COUNT.key] = A_P.Count().ToString();
            vals[GameParamsValueNames.PURSUERS_VELOCITY.key] = r_p.ToString();
            vals[GameParamsValueNames.EVADERS_TRANSMISSION_RANGE.key] = r_e.ToString();
            vals[GameParamsValueNames.EVADERS_SENSING_RANGE.key] = r_es.ToString();
            //vals[GameParamsValueNames.PURSUERS_SENSING_RANGE] = r_ps.ToString();
            vals[IntrusionGameParamsValueNames.INTRUSION_TIME.key] = t_i.ToString();
            //vals[GameParamsValueNames.REWARD_FUNCTION_NAME] = R.GetType().Name;
            //vals[GameParamsValueNames.REWARD_FUNCTION_ARGS] = R.ArgsCSV;
            //vals.AddRange(R.ArgsData());
            //vals[GameParamsValueNames.EVADERS_CAN_RECEIVE_SIMULTENOUS_BROADCASTS] = (canEvadersReceiveMultipleBroadcasts == true)?("1"):("0");
            //vals[GameParamsValueNames.SINKS_SENSE_PURSUERS] = (canSinksSensePursuers == true)? ("1") : ("0");
            //vals[GameParamsValueNames.SINKS_SAFE_POINTS] = (areSinksSafe == true) ? ("1") : ("0");
            vals[IntrusionGameParamsValueNames.IS_AREA_SQUARE.key] = (IsAreaSquare == true)?("1"):("0");
            return vals;
        }


        public int SensitiveAreaPointsCount()
        {
            return 4 * (2 * r_e + 1);
        }
        /// <summary>
        /// null if area is not square
        /// </summary>
        public GameLogic.Utils.Grid4Square SensitiveAreaSquare(Point intrusionAreaTarget)
        {
            if (!IsAreaSquare)
                return null;

            //Point topRight = intrusionAreaTarget.subtruct(r_e, r_e);
            return new Utils.Grid4Square(intrusionAreaTarget, r_e-1); // target is top left, r_e is the length of each edge
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
        /// Distance from the target into which evaders need to enter
        /// </summary>
        public int r_e { get; set; }

        /// <summary>
        /// Maximal distance in which evading-eavesdropper side agents can sense the location of other agents
        /// </summary>
        public int r_es { get; set; }

        ///// <summary>
        ///// Maximal distance in which pursuer-side agents can sense the location of other evaders
        ///// NOTE: currently unused because it's annoying - it may create a large "cleared area"
        ///// </summary>
        //public int r_ps { get; set; }

        /// <summary>
        /// time for intrusion (0 means that as evaders win exactly as they enter the area )
        /// </summary>
        public int t_i { get; set; }
       
        ///// <summary>
        ///// Non increasing function that maps an age of a data unit to the reward given to evading-eavesdropper side upon transmitting the packet to the sink
        ///// </summary>
        //public ARewardFunction R { get; set; }

        /// <summary>
        /// tells if the area is a "diamond" (e.g. for moving between two points on the circumference, two edges are needed),
        /// or an "axis-aligned" square
        /// </summary>
        public bool IsAreaSquare { get; set; }

    }
}
