using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic
{
    public class Evader : AAgent<Evader>
    {
        public Evader() 
        {
            if (!isLegalCosntruction)
                throw new Exception("Evaders may be constructed by Evader.getAgents() only");
        }
    }
}
