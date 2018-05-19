using System;
using System.Linq;
using System.Linq.Expressions;
//static class Program
//{
//    static void Main()
//    {
//        Foo foo = new Foo { Id = 16, Name = "Fred", DoB = DateTime.Today };
//        Foo bar = foo.Clone();
        
//    }
//}
//class Foo
//{
//    //[Column(IsPrimaryKey = true)] // PK
//    public int Id { get; set; }
//    //[Column] // test with non-PK ColumnAttribute
//    public string Name { get; set; }
//    // test w/o ColumnAttribute
//    public DateTime DoB { get; set; }
//}

namespace GoE.Utils
{
    namespace ShallowCloneEx
    {
        public static class ObjectExt
        {
            public static T Clone<T>(this T obj) where T : new()
            {
                return ObjectExtCache<T>.Clone(obj);
            }
            static class ObjectExtCache<T> where T : new()
            {
                private static readonly Func<T, T> cloner;
                static ObjectExtCache()
                {
                    ParameterExpression param = Expression.Parameter(typeof(T), "in");

                    var bindings = from prop in typeof(T).GetProperties()
                                   where prop.CanRead && prop.CanWrite
                                   //let column = Attribute.GetCustomAttribute(prop, typeof(ColumnAttribute)) as ColumnAttribute
                                   //where column == null || !column.IsPrimaryKey
                                   select (MemberBinding)Expression.Bind(prop,
                                       Expression.Property(param, prop));
                    
                    cloner = Expression.Lambda<Func<T, T>>(
                        Expression.MemberInit(
                            Expression.New(typeof(T)), bindings), param).Compile();


                }
                public static T Clone(T obj)
                {
                    return cloner(obj);
                }

            }
        }
    }
}