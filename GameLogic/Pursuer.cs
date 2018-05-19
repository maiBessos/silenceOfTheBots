using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic
{
    public class Pursuer : AAgent<Pursuer>
    {
       public Pursuer() 
       {
           if (!isLegalCosntruction)
               throw new Exception("Pursuers may be constructed by Pursuer.getAgents() only");
       }
    }
}
