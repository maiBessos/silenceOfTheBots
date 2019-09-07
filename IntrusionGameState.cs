using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic
{

    public class IntrusionGameState
    {

        public IntrusionGameState()
        {
            //L = new List<Dictionary<IAgent, Location>>();
            ActiveEvaders = new HashSet<Evader>();
        }

        public int MostUpdatedPursuersRound
        {
            get;
            set;
        }
        public int MostUpdatedEvadersLocationRound
        {
            get;
            set;
        }
        
        /// <summary>
        /// utility property, that returns all evaders that has a location (were set + not yet captured)
        /// </summary>
        /// <returns></returns>
        public HashSet<Evader> ActiveEvaders
        {
            get;
            protected set;
        }
        /// <summary>
        /// given IAgent (Evader or Pursuer) a, L[r][a] indicates the location at round r of the agent
        /// </summary>
        //public List<Dictionary<IAgent, Location>> L { get; set; }

        
    }
}
