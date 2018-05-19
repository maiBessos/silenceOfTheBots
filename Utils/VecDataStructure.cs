using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils.Algorithms;

namespace GoE.Utils
{
    /// <summary>
    /// wrapper for List<>, to allow syntax conforming with GoE pseudo code 
    /// (-1 is legal index, and the list extends automatically)
    /// </summary>
    /// <typeparam name="ObjType"></typeparam>
    public class Vec<ObjType> where ObjType : new()
    {
        public Vec()
        {
            
            Data = new List<ObjType>();
        }

        protected List<ObjType> Data{ get; set; }
        
        /// <summary>
        /// if key is out of range 'Data' is increased automatically by adding new instances of 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ObjType this[int key]
        {
            get
            {
                if(key+1 >= Data.Count)
                {
                    // we double the array's size
                    for (int i =  (key + 1); i >= 0; --i)
                        Data.Add(new ObjType());
                }
                return Data[key+1];
            }
            set
            {
                if (key + 1 >= Data.Count)
                {
                    // we double the array's size
                    for (int i = (key + 1); i >= 0; --i)
                        Data.Add(new ObjType());
                }
                Data[key + 1] = value;

            }
        }

        

    }

    /// <summary>
    /// wrapper for List<>, where CyclicList[0] is the oldest object in the list, and CyclicList[vecSize-1] is the newest
    /// (addNext() will override the current oldest)
    /// </summary>
    /// <typeparam name="ObjType"></typeparam>
    public class CyclicList<ObjType> //where ObjType : new()
    {
        /// <summary>
        /// takes ownership over param 'initialValues'
        /// </summary>
        /// <param name="vecSize"></param>
        /// <param name="initialValues">
        /// </param>
        public CyclicList(List<ObjType> initialValues)
        {
            nextIdx = 0;
            this.VecSize = initialValues.Count;
            Data = initialValues;
        }

        public List<ObjType> Data { get; protected set; }
        
        public void advance()
        {
            ++nextIdx;
        }
        virtual public void addNext(ObjType val)
        {
            this[nextIdx++] = val;
        }

        /// <summary>
        /// allows iterating the list like a normally ordered list
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public ObjType this[int idx]
        {
            get
            {
                return Data[(nextIdx + idx) % Data.Count];
            }
            set
            {
                Data[(nextIdx + idx) % Data.Count] = value;
            }
        }
        public ObjType Newest { get { return Data[(nextIdx + VecSize - 1) % Data.Count]; } }
        public ObjType Oldest { get { return Data[nextIdx % Data.Count]; } }

        protected int nextIdx { get; set; }
        public int VecSize { get; protected set; }
    }

    public class CyclicDoubleList : CyclicList<double>
    {
        public CyclicDoubleList(List<double> initialValues)
            : base(initialValues)
        {
            sum = 0;
        }
        public double Average
        {
            get
            {
                return sum / Data.Count;
            }
            
        }
        public override void addNext(double val)
        {
            sum += (val - Oldest);
            this[nextIdx++] = val;
        }
        private double sum;
    }
}
