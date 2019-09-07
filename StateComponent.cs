using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Utils
{
    /// <summary>
    /// allows merging several variables into a single variable
    /// </summary>
    public abstract class AStateComponent
    {
        /// <summary>
        /// amount of possible states of this component
        /// </summary>
        /// <returns></returns>
        public abstract int stateCount();
        
        /// <summary>
        /// current state value of this component
        /// </summary>
        /// <returns></returns>
        abstract public int StateVal { get; set; }
        
        /// <summary>
        /// returns the concatenation of the value of 'lesserComponent' to val
        /// (maybe undone with operator/=)
        /// </summary>
        public static int operator *(int val, AStateComponent lesserComponent)
        {
            return lesserComponent.StateVal + val * lesserComponent.stateCount();
        }

        /// <summary>
        /// returns the concatenation of the value of 'lesserComponent' to 'greaterComponent'
        /// </summary>
        /// <param name="lesserComponent">
        /// will be extracted from val before 'greaterComponent'
        /// </param>
        /// <param name="greaterComponent"></param>
        /// <returns></returns>
        public static int operator *(AStateComponent greaterComponent, AStateComponent lesserComponent)
        {
            return lesserComponent.StateVal + greaterComponent.StateVal * lesserComponent.stateCount();
        }
        
        /// <summary>
        /// sets lesserComponent.State, and returns the ramaining values
        /// </summary>
        /// <param name="val"></param>
        /// <param name="lesserComponent"></param>
        /// <returns></returns>
        public static int operator /(int val, AStateComponent lesserComponent)
        {
            lesserComponent.StateVal = val % lesserComponent.stateCount();
            return val / lesserComponent.stateCount();
        }
    }

    public class CompositeStateComponent : AStateComponent
    {
        protected List<AStateComponent> components = new List<AStateComponent>();

        public override int StateVal
        {
            get
            {
                int res = components[components.Count-1] * components[components.Count-2];
                for (int ci = components.Count-3; ci >= 0; --ci)
                    res = res * components[ci];
                return res;
            }

            set
            {
                int val = value;
                for(int ci = 0; ci < components.Count; ++ci)
                {
                    val = val / components[ci];
                }
            }
        }

        public override int stateCount()
        {
            int res = 1;
            foreach (var c in components)
                res *= c.stateCount();
            return res;
        }
    }

}

