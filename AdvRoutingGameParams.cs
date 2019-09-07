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
using System.Windows.Forms;

namespace GoE.GameLogic
{


    public class AdvRoutingGameParams : AGameParams<AdvRoutingGameParams>
    {
        /// <summary>
        /// constructing objects this way is dicouraged.
        /// use  AGameParams<GameParams>.getClearParams() instead
        /// </summary>
        public AdvRoutingGameParams()
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
            R = null;
            r_p = 0;
            sinkAlwaysDetectable = true;
            canEliminate = false;
        }
        
        /// <summary>
        /// The set of agents in the eouting side (with cardinality of N)
        /// </summary>
        public List<Evader> A_E { get; set; }

        /// <summary>
        /// The set of agents in the pursuing side (with cardinality of M)
        /// </summary>
        public List<Pursuer> A_P { get; set; }

        /// <summary>
        /// Maximal distance pursuer side agents can go each round 
        /// </summary>
        public int r_p { get; set; }

        /// <summary>
        /// probability of detecting a transmissions, by pursuer
        /// </summary>
        public double p_d { get; set; }
        
        /// <summary>
        /// if true, pursuers can eliminate routers at the point they visit
        /// </summary>
        public bool canEliminate { get; set; }

        /// <summary>
        /// if true, pursuers will detect sink when they visit it even if it doesn't transmit
        /// </summary>
        public bool sinkAlwaysDetectable { get; set; }

        public bool accurateInterception { get; set; }

        public bool forceContinuousTransmission { get; set; }

        public bool singleSourceRouter { get; set; }

        /// <summary>
        /// Non increasing function that maps an age of a data unit to the reward given to evading-eavesdropper side upon transmitting the packet to the sink
        /// </summary>
        public ARewardFunction R { get; set; }

        public override void fromValueMap(Dictionary<string, string> vals)
        {
            // FIXME: instead of having multiple functions reading/writing into a value map, automate this


            int evaderCount = Int32.Parse(vals[GameParamsValueNames.EVADERS_COUNT.key]);
            int pursuerCount = Int32.Parse(vals[GameParamsValueNames.PURSUERS_COUNT.key]);
            r_p = Int32.Parse(vals[GameParamsValueNames.PURSUERS_VELOCITY.key]);

            p_d = double.Parse(
                GameParamsValueNames.PURSUERS_DETECTION_PROB.tryRead(vals));

            canEliminate = (1 == int.Parse(AdvRoutingGameParamsValueNames.CAN_PURSUERS_ELIMINATE.tryRead(vals)));
            sinkAlwaysDetectable = (1 == int.Parse(AdvRoutingGameParamsValueNames.SINK_ALWAYS_DETECTED.tryRead(vals)));
            accurateInterception = (1 == int.Parse(AdvRoutingGameParamsValueNames.CAN_SENSE_ACCURATE_LOCATION.tryRead(vals)));

            string rewardFuncName = GameParamsValueNames.REWARD_FUNCTION_NAME.tryRead(vals);
            R = ReflectionUtils.constructEmptyCtorType<ARewardFunction>(rewardFuncName);
            R.setArgs(GameParamsValueNames.REWARD_FUNCTION_ARGS.tryRead(vals));

            A_E = Evader.getAgents(evaderCount);
            A_P = Pursuer.getAgents(pursuerCount);

            singleSourceRouter = "1" == AdvRoutingGameParamsValueNames.SINGLE_SOURCE.tryRead(vals);
            forceContinuousTransmission = "1" == AdvRoutingGameParamsValueNames.CONTINUOUS_ROUTERS.tryRead(vals);
        
        }
        public override Dictionary<string, string> toValueMap()
        {
            Dictionary<string, string> vals = new Dictionary<string, string>();
            vals[GameParamsValueNames.EVADERS_COUNT.key] = A_E.Count().ToString();
            vals[GameParamsValueNames.PURSUERS_COUNT.key] = A_P.Count().ToString();
            vals[GameParamsValueNames.PURSUERS_VELOCITY.key] = r_p.ToString();
            vals[GameParamsValueNames.PURSUERS_DETECTION_PROB.key] = p_d.ToString();
            
            vals[AdvRoutingGameParamsValueNames.CAN_PURSUERS_ELIMINATE.key] = canEliminate ? "1" : "0";
            vals[AdvRoutingGameParamsValueNames.SINK_ALWAYS_DETECTED.key] = sinkAlwaysDetectable ? "1" : "0";
            vals[AdvRoutingGameParamsValueNames.CAN_SENSE_ACCURATE_LOCATION.key] = accurateInterception ? "1" : "0";

            vals[AdvRoutingGameParamsValueNames.SINGLE_SOURCE.key] = singleSourceRouter? "1" : "0";
            vals[AdvRoutingGameParamsValueNames.CONTINUOUS_ROUTERS.key] = forceContinuousTransmission ? "1" : "0";

             


            if (R != null)
            {
                vals[GameParamsValueNames.REWARD_FUNCTION_NAME.key] = R.GetType().Name;
                vals[GameParamsValueNames.REWARD_FUNCTION_ARGS.key] = R.ArgsCSV;
                vals.AddRange(R.ArgsData());
            }
            else
            {
                vals[GameParamsValueNames.REWARD_FUNCTION_NAME.key] = GameParamsValueNames.REWARD_FUNCTION_NAME.val;
                vals[GameParamsValueNames.REWARD_FUNCTION_ARGS.key] = GameParamsValueNames.REWARD_FUNCTION_ARGS.val;
            }
            
            return vals;
        }
        public override string generateFileShowDialog()
        {
            List<string> valNames = new List<string>() {
                GameParamsValueNames.EVADERS_COUNT.key,
            GameParamsValueNames.PURSUERS_COUNT.key,
            GameParamsValueNames.PURSUERS_VELOCITY.key,
            GameParamsValueNames.PURSUERS_DETECTION_PROB.key,
            GameParamsValueNames.REWARD_FUNCTION_NAME.key,
            GameParamsValueNames.REWARD_FUNCTION_ARGS.key,
            AdvRoutingGameParamsValueNames.CAN_PURSUERS_ELIMINATE.key,
            AdvRoutingGameParamsValueNames.SINK_ALWAYS_DETECTED.key,
            AdvRoutingGameParamsValueNames.CAN_SENSE_ACCURATE_LOCATION.key,
            AdvRoutingGameParamsValueNames.SINGLE_SOURCE.key,
            AdvRoutingGameParamsValueNames.CONTINUOUS_ROUTERS.key
        };

            List<string> defaults = new List<string>() {
                "200","1","-1","1", typeof(NoDecrease).Name,"","0","1"};

            List<string> vals = 
                InputBox.ShowDialog(valNames.ToArray(), "Set Adv routing params", defaults.ToArray());


            SaveFileDialog d = new SaveFileDialog();

            d.Filter = "Game Param Files (*.gprm)|*.gprm";
            d.InitialDirectory = GoE.AppConstants.PathLocations.PARAM_FILES_FOLDER;
            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return null;

                Dictionary<string, string> allVals = new Dictionary<string, string>();
                allVals.AddRange(valNames, vals);
                fromValueMap(allVals);
                serialize(d.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return d.FileName;
        }

    }
}
