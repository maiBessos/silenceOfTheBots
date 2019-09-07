using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Utils
{


    /// <summary>
    /// allows translating the interface of IEnumerable<TypeFrom> to IEnumerable<TypeTo>
    /// NOTE: if TypeTo derived from TypeFrom, there is no need for this utility: it already works with standard IEnumerable<>
    /// </summary>
    public class EnumerableTranslator<TypeFrom, TypeTo> : IEnumerable<TypeTo>
    {
        public delegate TypeTo objectTranslator(TypeFrom obj);

        private IEnumerable<TypeFrom> sourceEnumerable;
        private objectTranslator translator;
        private EnumerableTranslator() { }

        public static EnumerableTranslator<TypeFrom, TypeTo> translate(IEnumerable<TypeFrom> SourceEnumerable, objectTranslator t)
        {
            EnumerableTranslator<TypeFrom, TypeTo> res = new EnumerableTranslator<TypeFrom, TypeTo>();
            res.translator = t;
            res.sourceEnumerable = SourceEnumerable;
            return res;
        }
 

        /// <summary>
        /// Inner class for enumerating the rangeset.
        /// </summary>
        private class EnumerableTranslatorIterator : IEnumerator<TypeTo>
        {
            #region IEnumerator Members

            public IEnumerator<TypeFrom> it;
            objectTranslator trans;
            public EnumerableTranslatorIterator(IEnumerator<TypeFrom> src, objectTranslator t)
            {
                trans = t;
                it = src;
            }
            public void Reset()
            {
                it.Reset();
            }

            object IEnumerator.Current 
            {
                get { return trans(it.Current); }
            }

            public TypeTo Current
            {
                get
                {
                    return trans(it.Current);
                }
            }

            public bool MoveNext()
            {
                return it.MoveNext();
            }

            #endregion


            public void Dispose()
            {
                it.Dispose();
            }

        }



        public IEnumerator<TypeTo> GetEnumerator()
        {
            return new EnumerableTranslatorIterator(sourceEnumerable.GetEnumerator(), this.translator);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumerableTranslatorIterator(sourceEnumerable.GetEnumerator(), this.translator);
        }

    }


}
