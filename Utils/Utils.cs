using GoE.GameLogic.EvolutionaryStrategy;
using GoE.Utils.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.Utils
{

    /// <summary>
    /// generates up to 'valueCount' different legal values that may be used.
    /// This is used for optimization of the parameter, so if the range of the parameter is [0,1],
    /// it is expected to return (for valueCount=11) : 0.0,0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8,0.9,1.0
    /// </summary>
    /// <param name="valueCount"></param>
    /// <returns></returns>
    public interface IValueListGenerator
    {
        List<string> generate(int valueCount);
    }
    public class UniformValueList : IValueListGenerator
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public bool IntegerValsOnly { get; set; }

        /// <summary>
        /// includes max value
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="integerValsOnly"></param>
        public UniformValueList(double min, double max, bool integerValsOnly = false)
        {
            Min = min;
            Max = max;
            IntegerValsOnly = integerValsOnly;
        }
        public List<string> generate(int valueCount)
        {
            List<string> res = new List<string>();
            double diff = (Max - Min) / (valueCount - 1);
            if (IntegerValsOnly == false)
            {
                double val;
                for ( val = Min; val <= Max; val += diff)
                    res.Add(val.ToString());

                if (Math.Abs((val - diff) - Max) > 0.00001) // make sure the last added value wasn't already very near Max
                    res.Add(Max.ToString());

                return res;
            }

            if (diff < 1)
            {
                for (double val = Min; val <= Max; val += diff)
                    res.Add(((int)Math.Round(val)).ToString());
                return res;
            }
            
            int intMax = (int)Math.Floor(Max);
            for (double val = Min; val <= Math.Max(valueCount + Min, intMax); val += diff)
                res.Add(((int)Math.Round(val)).ToString());
            res.Add(intMax.ToString());
            
            return res;
        }
    }

    public class DiscreteValueList<T> : IValueListGenerator
    {
        public List<T> Values { get; set; }
        public bool PrioritizeFirstValues;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="prioritizeFirstValues">
        /// if true, first values will be returned in case requested valueCount < vals.count
        /// (otherwise choose values in uniform indices)
        /// </param>
        public DiscreteValueList(IEnumerable<T> vals, bool prioritizeFirstValues)
        {
            Values = new List<T>(vals);
        }
        public List<string> generate(int valueCount)
        {
            if (valueCount >= Values.Count)
                return Values.ConvertAll((T val) => { return val.ToString(); });

            List<string> res = new List<string>();
            if (PrioritizeFirstValues)
            {
                for (int i = 0; i < valueCount && i < Values.Count; ++i)
                    res.Add(Values[i].ToString());
                return res;
            }

            double diff = ((double)Values.Count) / (valueCount - 1);
            for (double index = 0; res.Count < valueCount-1; index += diff)
                res.Add(((int)Math.Round(index)).ToString());
            res.Add(Values.Last().ToString());
            return res;
        }
    }


    public static class GraphicUtils
    {
        //public static Color colorMixer(Color c1, Color c2)
        //{

        //    int _r = (c1.R + c2.R) / 2;
        //    int _g = (c1.G + c2.G) / 2;
        //    int _b = (c1.B + c2.B) / 2;

        //    return Color.FromArgb(255, Convert.ToByte(_r), Convert.ToByte(_g), Convert.ToByte(_b));
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="c1Weight">
        /// number in (0,1), tells how significant is c1
        /// </param>
        /// <returns></returns>
        public static Color colorMixer(Color c1, Color c2, float c1Weight = 0.5f)
        {

            int _r = Math.Min(255, (int)Math.Round(c1.R * c1Weight + c2.R * (1- c1Weight)));
            int _g = Math.Min(255, (int)Math.Round(c1.G * c1Weight + c2.G * (1 - c1Weight)));
            int _b = Math.Min(255, (int)Math.Round(c1.B * c1Weight + c2.B * (1 - c1Weight)));

            return Color.FromArgb(255, Convert.ToByte(_r), Convert.ToByte(_g), Convert.ToByte(_b));
        }

        public static Color mostDistantColor(Color c)
        {
            int _r = (c.R < 128) ? (255) : (0);
            int _g = (c.G < 128) ? (255) : (0);
            int _b = (c.B < 128) ? (255) : (0);
            return Color.FromArgb(255, Convert.ToByte(_r), Convert.ToByte(_g), Convert.ToByte(_b));
        }
    }
    public static class ReflectionUtils
    {

        /// <summary>
        /// base class for classes that want to provide the property ChildrenByTypename
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class DerivedTypesProvider<T>
        {
            static DerivedTypesProvider()
            {
                ChildrenByTypename = ReflectionUtils.getTypeList<T>();
            }

            public static Dictionary<string, Type> ChildrenByTypename { get; private set; }
        }

        /// <summary>
        /// similar to DerivedTypesProvider, but provides a list of actual instances - one
        /// of each kind. It is assumed empty c'tor is available
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class DerivedInstancesProvider<T>
        {
            static DerivedInstancesProvider()
            {
                ChildrenByTypename = new Dictionary<string, T>();
                var typesByTypename = ReflectionUtils.getTypeList<T>();
                foreach (var t in typesByTypename)
                    ChildrenByTypename.Add(t.Key, ReflectionUtils.constructEmptyCtorType<T>(t.Key));
            }

            public static Dictionary<string, T> ChildrenByTypename { get; private set; }
        }

        public static string GetObjStaticTypeName<T>(T obj)
        {
            return typeof(T).Name;
        }

        /// <summary>
        /// returns a dictionary of class name->class type, for all classes that inherit 'BaseType'
        /// </summary>
        /// <typeparam name="BaseType">
        /// interface of base class
        /// </typeparam>
        /// <returns></returns>
        public static Dictionary<string, Type> getTypeList<BaseType>() 
        {
            Dictionary<string, Type> res = new Dictionary<string, Type>();
            IEnumerable<Type> typeList;
            if(typeof(BaseType).IsInterface)
            {
                typeList = Assembly.GetAssembly(typeof(BaseType)).GetTypes().Where(t => t.GetInterfaces().Contains(typeof(BaseType)) && !t.IsAbstract);
            }
            else
            {
                typeList = Assembly.GetAssembly(typeof(BaseType)).GetTypes().Where(t => t.IsSubclassOf(typeof(BaseType)) && !t.IsAbstract);
            }

            foreach (Type t in typeList)
                res[t.Name] = t;

            return res;
        }

        public static Dictionary<string, Type> getTypeList(Type baseType)
        {
            Dictionary<string, Type> res = new Dictionary<string, Type>();
            IEnumerable<Type> typeList;
            if (baseType.IsInterface)
            {
                typeList = Assembly.GetAssembly(baseType).GetTypes().Where(t => t.GetInterfaces().Contains(baseType) && !t.IsAbstract);
            }
            else
            {
                typeList = Assembly.GetAssembly(baseType).GetTypes().Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract);
            }

            foreach (Type t in typeList)
                res[t.Name] = t;

            return res;
        }

        /// <summary>
        /// reates a new object of the same type as srcObj
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="srcObj"></param>
        /// <returns></returns>
        public static T constructEmptyCtorTypeFromObj<T>(T srcObj) 
        {
            var chosenCtor = srcObj.GetType().GetConstructor(new Type[] { });
            return (T)chosenCtor.Invoke(new object[] { });
        }
        public static T constructEmptyCtorType<T>(string typename)
        {
            Type Ttype = typeof(T);
            var typesList = Assembly.GetAssembly(Ttype).GetTypes();
            var chosenType = typesList.Where(t => !t.IsAbstract && !t.IsInterface && t.Name.Contains(typename) && ( t.GetInterfaces().Contains(Ttype) ||  t.IsSubclassOf(Ttype) || t.IsEquivalentTo(Ttype))).First();
            var chosenCtor = chosenType.GetConstructor(new Type[] { });
            return (T)chosenCtor.Invoke(new object[] { });
        }

        public static T constructEmptyCtorTypeFromTid<T>(Type Ttype)
        {
            var chosenCtor = Ttype.GetConstructor(new Type[] { });
            return (T)chosenCtor.Invoke(new object[] { });
        }
        
        public static Type[] GetTypesInNamespace(string nameSpace)
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }
        public static Type[] GetTypesInAllNamespaces(string topNamespace)
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace !=null && t.Namespace.StartsWith(topNamespace)).ToArray();
        }

        public static List<T> getStaticInstancesInClass<T>(Type staticClassType)
        {
            List<T> res = new List<T>();

            foreach (var p in staticClassType.GetProperties())
            {
                try
                {
                    T val = (T)p.GetValue(null, null);
                    res.Add(val);
                }
                catch (Exception) { }
            }
            return res;
        }
        public static List<T> getStaticInstancesInNameSpace<T>(string nameSpace)
        {
            List<Type> types = new List<Type>(GetTypesInAllNamespaces(nameSpace));
            var res = new List<T>();
            HashSet<Type> processedTypes = new HashSet<Type>(); ;
            for(int i = 0; i < types.Count(); ++i)
            {
                if (processedTypes.Contains(types[i]))
                    continue;
                processedTypes.Add(types[i]);
                res.AddRange(getStaticInstancesInClass<T>(types[i]));
                types.AddRange(types[i].GetNestedTypes());
            }
            return res;
        }
    }
    namespace Extensions
    {
        public static class SortedSet
        {
            public static T popMinAndIncrease<T>(this SortedSet<Tuple<T, int>> sortedItems)
            {
                var res = sortedItems.Min;
                sortedItems.Remove(sortedItems.Min);
                sortedItems.Add(new Tuple<T, int>(res.Item1, res.Item2 + 1));
                return res.Item1;
            }

            public static T popMinAndIncrease<T>(this SortedSet<Tuple<T, int>> sortedItems, int maxVal)
            {
                var res = sortedItems.Min;
                sortedItems.Remove(sortedItems.Min);
                sortedItems.Add(new Tuple<T, int>(res.Item1, Math.Min(maxVal,res.Item2 + 1)));
                return res.Item1;
            }

        }

        public static class Lists
        {
            /// <summary>
            /// clears current list, and returns a shuffled copy
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="lst"></param>
            /// <param name="r"></param>
            /// <returns></returns>
            public static List<T> moveAndShuffle<T>(this List<T> lst, Random r) 
            {
                List<T> res = new List<T>();
                long start = r.Next();
                long mult = r.Next();
                while(lst.Count > 0)
                {
                    int i = (int)((r.Next() * mult + start) % lst.Count);
                    res.Add(lst[i]);
                    lst[i] = lst.Last();
                    lst.RemoveAt(lst.Count - 1);
                }
                return res;
            }
            public static List<PointF> toPointFList(this List<Point> vals)
            {
                List<PointF> res = new List<PointF>();
                foreach (var v in vals)
                    res.Add(new PointF(v.X, v.Y));
                return res;
            }
            public static List<Point> toPointList(this List<PointF> vals)
            {
                List<Point> res = new List<Point>();
                foreach (var v in vals)
                    res.Add(new Point((int)v.X, (int)v.Y));
                return res;
            }
            public static void Resize<T>(this List<T> list, int sz, T c)
            {
                int cur = list.Count;
                if (sz < cur)
                    list.RemoveRange(sz, cur - sz);
                else if (sz > cur)
                {
                    if (sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                        list.Capacity = sz;
                    list.AddRange(Enumerable.Repeat(c, sz - cur));
                }
            }
            public static void Resize<T>(this List<T> list, int sz) where T : new()
            {
                Resize(list, sz, new T());
            }
        }
        public static class Dictionaries
        {
            
            public static Dictionary<string, List<Point>> toPointMarkings(this Dictionary<string, List<PointF>> vals)
            {
                var res = new Dictionary<string, List<Point>>();
                foreach (var v in vals)
                    res[v.Key] = v.Value.toPointList();
                return res;
            }
            public static Dictionary<string, List<PointF>> toPointFMarkings(this Dictionary<string, List<Point>> vals)
            {
                var res = new Dictionary<string, List<PointF>>();
                foreach (var v in vals)
                    res[v.Key] = v.Value.toPointFList();
                return res;
            }
            
            public static TValue getOrAllocate<TKey,TValue>(this IDictionary<TKey, TValue> into, TKey key) where TValue : new()
            {
                TValue res;
                if (into.TryGetValue(key, out res))
                    return res;
                return into[key] = new TValue();
            }
            public static TValue getOrAllocate<TKey, TValue>(this IDictionary<TKey, TValue> into, TKey key, Func<TValue> allocate)
            {
                TValue res;
                if (into.TryGetValue(key, out res))
                    return res;
                return into[key] = allocate();
            }
            public static TValue getOrAllocate<TKey, TValue>(this IDictionary<TKey, TValue> into, TKey key, TValue allocatedVal)
            {
                TValue res;
                if (into.TryGetValue(key, out res))
                    return res;
                return into[key] = allocatedVal;
            }

            // http://stackoverflow.com/questions/32664/is-there-a-constraint-that-restricts-my-generic-method-to-numeric-types/4834066#4834066
            public static void addIfExists<TKey>(this IDictionary<TKey, int> into, TKey key, int valToAdd, int valIfDoesntExist ) 
            {
                if (into.ContainsKey(key))
                    into[key] += valToAdd;
                else
                    into[key] = valIfDoesntExist;
            }

            /// <summary>
            /// if key doesnt exist, it is initialized to valToAdd
            /// </summary>
            public static void addIfExists<TKey>(this IDictionary<TKey, double> into, TKey key, double valToAdd)
            {
                if (into.ContainsKey(key))
                    into[key] += valToAdd;
                else
                    into[key] = valToAdd;
            }
            public static void addIfExists<TKey>(this IDictionary<TKey, double> into, TKey key, double valToAdd, double valIfDoesntExist)
            {
                if (into.ContainsKey(key))
                    into[key] += valToAdd;
                else
                    into[key] = valIfDoesntExist;
            }
            
            /// <summary>
            /// when values of a dictionary are lists, they need to be initalized before used.
            /// if list isn't already initialized, it will be initialized before adding new item
            /// </summary>
            /// <typeparam name="TKey"></typeparam>
            /// <typeparam name="TEnumedType"></typeparam>
            /// <param name="into"></param>
            /// <param name="key"></param>
            /// <param name="valToAdd"></param>
            public static bool addToList<TKey, TEnumedType>(this IDictionary<TKey, List<TEnumedType>> into, TKey key, TEnumedType itemToAdd)
            {
                bool isNew = false;
                if (!into.ContainsKey(key))
                {
                    into[key] = new List<TEnumedType>();
                    isNew = true;
                }
                into[key].Add(itemToAdd);
                return isNew;
            }

            public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> into, IDictionary<TKey, TValue> from, bool overrideExistingValues = true)
            {
                foreach (var item in from)
                {
                    if (item.Key != null && (overrideExistingValues || !into.ContainsKey(item.Key)) )
                        into[item.Key] = item.Value;
                }
            }
            public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> into, IEnumerable<TKey> keys, IEnumerable<TValue> values)
            {
                IEnumerator<TValue> vIt = values.GetEnumerator();
                vIt.MoveNext();
                foreach(var k in keys)
                {
                    into[k] = vIt.Current;
                    vIt.MoveNext();
                }
            }
            public static IDictionary<string, TValue> AddKeyPrefix<TValue>(this IDictionary<string, TValue> val, string prefix)
            {
                Dictionary<string, TValue> tmp = new Dictionary<string, TValue>(val);
                val.Clear();
                foreach (var v in tmp)
                    val[prefix + v.Key] = v.Value;
                return val;
            }
            
            public static List<V> getValuesOf<K,V>(this IDictionary<K,V> vals, IEnumerable<K> keys)
            {
                List<V> res = new List<V>(keys.Count());
                foreach(var k in keys)
                    res.Add(vals[k]);
                return res;
            }
            /// <summary>
            /// returns null if the same key has multiple values in tuple list
            /// </summary>
            /// <typeparam name="K"></typeparam>
            /// <typeparam name="V"></typeparam>
            /// <param name="tupList"></param>
            /// <returns></returns>
            public static Dictionary<K,V> fromTupleList<K,V>(List<Tuple<K,V>> tupList, ref K dupeKey)
            {
                var res = new Dictionary<K, V>();
                foreach (var t in tupList)
                {
                    if (res.ContainsKey(t.Item1))
                    {
                        dupeKey = t.Item1;
                        return null;
                    }
                    res[t.Item1] = t.Item2;
                }
                return res;
            }
            
        }
        
        
    }
}
