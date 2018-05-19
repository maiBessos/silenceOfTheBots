using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic
{
    public abstract class IAgent { }

    public class AAgent<Inheritor> : IAgent where Inheritor : AAgent<Inheritor>, new()
    {
        [ThreadStatic]
        protected static bool isLegalCosntruction = false; // TODO: this is so dirty! in c++ there could be a MUCH more elegant solution

        [ThreadStatic] 
        static int counter = 0;
        int id = counter++;

        public static int TotalAllocatedCount
        {
            get
            {
                return counter;
            }
        }
        public int ID
        {
            get
            {
                return id;
            }
        }

        public static List<Inheritor> getAgents(int count)
        {
            isLegalCosntruction = true;
            counter = 0;
            List<Inheritor> newAgents = new List<Inheritor>(count);
            for (int i = 0; i < count; ++i)
                newAgents.Add(new Inheritor());
            isLegalCosntruction = false;

            return newAgents;
        }
    }
}
