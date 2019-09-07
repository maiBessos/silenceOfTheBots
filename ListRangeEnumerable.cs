using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Utils
{
    public class ListRangeEnumerator<T> : IEnumerator<T>
    {
        private List<T> data;

        public int Start { get; protected set; }
        public int End { get; protected set; }
        public int CurrentIdx { get;  protected set; }

        public ListRangeEnumerator(List<T> Data, int Start, int End)
        {
            this.data = Data;
            this.Start = Start;
            this.CurrentIdx = Start - 1;
            this.End = End;
        }

        public T Current
        {
            get { return data[CurrentIdx]; }
        }

        public void Dispose()
        {

        }

        object System.Collections.IEnumerator.Current
        {
            get { return data[CurrentIdx]; }
        }

        public bool MoveNext()
        {
            return ++CurrentIdx < End;
        }

        public void Reset()
        {
            CurrentIdx = Start;
        }
    }
    public class ListRangeEnumerable<T> : IEnumerable<T>
    {
        public List<T> data { get; private set; }
        public int start { get; private set; }
        public int end { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Start"></param>
        /// <param name="End">
        /// last enumerable index + 1  (i.e. count = End-Start)
        /// </param>
        public ListRangeEnumerable(List<T> Data, int Start, int End)
        {
            this.data = Data;
            this.start = Start;
            this.end = End;

        }

        /// <summary>
        /// treats 'Data' as a list (i.e. start indices accumulate)
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Start"></param>
        /// <param name="End">
        /// last enumerable index + 1  (i.e. count = End-Start)
        /// </param>
        public ListRangeEnumerable(ListRangeEnumerable<T> Data, int Start, int End)
        {
            this.data = Data.data;
            this.start = Data.start + Start;
            this.end = Data.start + End;

        }
        public IEnumerator<T> GetEnumerator()
        {
            return new ListRangeEnumerator<T>(data, start, end);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ListRangeEnumerator<T>(data, start, end);
        }
        public int Count()
        {
            return end - start;
        }
        public T this[int index]
        {
            get
            {
                return data[index + start];
            }
            set
            {
                data[index + start] = value;
            }
        }
    }
}
