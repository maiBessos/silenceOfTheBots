using GoE.GameLogic.EvolutionaryStrategy;
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
    namespace Algorithms
    {
        namespace FunctionTreeNode
        {
            

            /// <summary>
            /// describes a general double value to double value function tree.
            /// The node may be used in a code to replace an actual numeric variable, and instead
            /// keeping the result value - the mathematical operations will be accumulated
            /// </summary>
            public abstract class AFunctionTreeNode 
            {
                /// <summary>
                /// TODO: current implementation is naive - consider simplifying the func, deriving, then
                /// finding max using numerical methods
                /// </summary>
                /// <param name="minX"></param>
                /// <param name="maxX"></param>
                /// <param name="sampleDiff"></param>
                /// <param name="findMax">if true, finds max Y. otherwise, find min Y</param>
                /// <returns></returns>
                public PointF findExtremePoint(float minX, float maxX, float sampleDiff, bool findMax = true)
                {
                    Algorithms.OptimizedObj<float> bestP = new OptimizedObj<float>();
                    bestP.data = minX;
                    bestP.value = float.NegativeInfinity;

                    if (findMax)
                        for (float currentSamp = minX; currentSamp < maxX; currentSamp += sampleDiff)
                            bestP.setIfValueIncreases(currentSamp, Evaluate(AlgorithmUtils.getRepeatingValueList<double>(currentSamp, 1)));
                    else
                        for (float currentSamp = minX; currentSamp < maxX; currentSamp += sampleDiff)
                            bestP.setIfValueDecreases(currentSamp, Evaluate(AlgorithmUtils.getRepeatingValueList<double>(currentSamp, 1)));

                    return new PointF((float)bestP.data, (float)bestP.value);
                }


                public abstract double Evaluate(List<double> param);

                /// <summary>
                /// assumes only 1 param is used
                /// </summary>
                public List<PointF> sample(double minParam, double maxParam, double sampleDiff)
                {
                    
                    List<PointF> res = new List<PointF>();
                    for (double currentSamp = minParam; currentSamp < maxParam; currentSamp += sampleDiff)
                    {
                        res.Add(new PointF((float)currentSamp, (float)Evaluate(AlgorithmUtils.getRepeatingValueList<double>(currentSamp,1))));
                    }

                    res.Add(new PointF((float)maxParam, (float)Evaluate(AlgorithmUtils.getRepeatingValueList<double>(maxParam, 1))));
                    return res;
                }

                public static SumFuncTreeNode operator +(AFunctionTreeNode lhs, AFunctionTreeNode rhs)
                {
                    return new SumFuncTreeNode(lhs, rhs);
                }
                public static SubtractFuncTreeNode operator -(AFunctionTreeNode lhs, AFunctionTreeNode rhs)
                {
                    return new SubtractFuncTreeNode(lhs, rhs);
                }
                public static DivFuncTreeNode operator /(AFunctionTreeNode lhs, AFunctionTreeNode rhs)
                {
                    return new DivFuncTreeNode(lhs, rhs);
                }
                public static MultFuncTreeNode operator *(AFunctionTreeNode lhs, AFunctionTreeNode rhs)
                {
                    return new MultFuncTreeNode(lhs, rhs);
                }

                public static SumFuncTreeNode operator +(AFunctionTreeNode lhs, double rhs)
                {
                    return new SumFuncTreeNode(lhs, new ConstantValFuncTreeNode(rhs));
                }
                public static SubtractFuncTreeNode operator -(AFunctionTreeNode lhs, double rhs)
                {
                    return new SubtractFuncTreeNode(lhs, new ConstantValFuncTreeNode(rhs));
                }
                public static DivFuncTreeNode operator /(AFunctionTreeNode lhs, double rhs)
                {
                    return new DivFuncTreeNode(lhs, new ConstantValFuncTreeNode(rhs));
                }
                public static MultFuncTreeNode operator *(AFunctionTreeNode lhs, double rhs)
                {
                    return new MultFuncTreeNode(lhs, new ConstantValFuncTreeNode(rhs));
                }
               
                public static bool operator!=(AFunctionTreeNode lhs, AFunctionTreeNode rhs)
                {
                    return !(lhs == rhs);
                }
               public static bool operator==(AFunctionTreeNode lhs, AFunctionTreeNode rhs)
               {
                   string s1 = lhs.ToString();
                   string s2 = rhs.ToString();
                   return s1 == s2;
               }

                //public abstract string ToString();

            }
            /// <summary>
            /// encapsulates an assignable tree node
            /// </summary>
            public class RootFuncTreeNode : AFunctionTreeNode
            {
                public AFunctionTreeNode encapsulatedNode;

                public override string ToString()
                {
                    return encapsulatedNode.ToString();
                }
                public RootFuncTreeNode(AFunctionTreeNode n = null)
                {
                    encapsulatedNode = n;
                }
                public RootFuncTreeNode(double val)
                {
                    encapsulatedNode = new ConstantValFuncTreeNode(val);
                }
                public static implicit operator RootFuncTreeNode(SumFuncTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }
                public static implicit operator RootFuncTreeNode(SubtractFuncTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }
                public static implicit operator RootFuncTreeNode(DivFuncTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }
                public static implicit operator RootFuncTreeNode(MultFuncTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }
                public static implicit operator RootFuncTreeNode(PowerTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }
                public static implicit operator RootFuncTreeNode(AbsTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }
                public static implicit operator RootFuncTreeNode(ConstantValFuncTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }
                public static implicit operator RootFuncTreeNode(ParamValFuncTreeNode n)
                {
                    return new RootFuncTreeNode(n);
                }

                public static implicit operator RootFuncTreeNode(double value)
                {
                    return new RootFuncTreeNode(value);
                }
                public static implicit operator RootFuncTreeNode(float value)
                {
                    return new RootFuncTreeNode(value);
                }
                public static implicit operator RootFuncTreeNode(int value)
                {
                    return new RootFuncTreeNode(value);
                }

                public override double Evaluate(List<double> param)
                {
                    return encapsulatedNode.Evaluate(param);
                }

                public static bool operator==(RootFuncTreeNode lhs, double rhs)
                {
                    ConstantValFuncTreeNode n = lhs.encapsulatedNode as ConstantValFuncTreeNode;
                    return (n != null && n.val == rhs);
                }
                public static bool operator!=(RootFuncTreeNode lhs, double rhs)
                {
                    ConstantValFuncTreeNode n = lhs.encapsulatedNode as ConstantValFuncTreeNode;
                    return (n == null || n.val != rhs);
                }

            }

            /// <summary>
            /// evaluate(s) to a given constant value
            /// </summary>
            public class ConstantValFuncTreeNode : AFunctionTreeNode
            {
                public override string ToString()
                {
                    return val.ToString();
                }
                public ConstantValFuncTreeNode(double ConstantVal) { val = ConstantVal; }
                public override double Evaluate(List<double> param) { return val; }
                public double val;
            }

            /// <summary>
            /// evaluate()s to input value
            /// </summary>
            public class ParamValFuncTreeNode : AFunctionTreeNode
            {
                int paramIdx;
                public override string ToString()
                {
                    return "" + 'a' + (('x' - 'a' + paramIdx) % 'z' - 'a'); // parameters are "x,y,z,a,b,c..."
                }
                public ParamValFuncTreeNode(int ParamIdx = 0)
                {
                    paramIdx = ParamIdx;
                }

                public override double Evaluate(List<double> param)
                {
                    return param[paramIdx];
                }
            }

            /// <summary>
            /// evaluate()s to sum of two AFunctionTreeNode-s
            /// </summary>
            public class SumFuncTreeNode : AFunctionTreeNode
            {
                public override string ToString()
                {
                    return "(" + lhs.ToString() + "+" + rhs.ToString() + ")";
                }
                public SumFuncTreeNode(AFunctionTreeNode Lhs, AFunctionTreeNode Rhs)
                {
                    lhs = Lhs;
                    rhs = Rhs;
                }
                public override double Evaluate(List<double> param)
                {
                    return lhs.Evaluate(param) + rhs.Evaluate(param);
                }
                AFunctionTreeNode lhs, rhs;
            }
            /// <summary>
            /// evaluate()s to subtraction between two AFunctionTreeNode-s
            /// </summary>
            public class SubtractFuncTreeNode : AFunctionTreeNode
            {
                public override string ToString()
                {
                    return "(" + lhs.ToString() + "-" + rhs.ToString() + ")";
                }
                public SubtractFuncTreeNode(AFunctionTreeNode Lhs, AFunctionTreeNode Rhs)
                {
                    lhs = Lhs;
                    rhs = Rhs;
                }
                public override double Evaluate(List<double> param)
                {
                    return lhs.Evaluate(param) - rhs.Evaluate(param);
                }
                AFunctionTreeNode lhs, rhs;
            }
            /// <summary>
            /// evaluate()s to multiplication of two AFunctionTreeNode-s
            /// </summary>
            public class MultFuncTreeNode : AFunctionTreeNode
            {
                public override string ToString()
                {
                    return "(" + lhs.ToString() + "*" + rhs.ToString() + ")";
                }
                public MultFuncTreeNode(AFunctionTreeNode Lhs, AFunctionTreeNode Rhs)
                {
                    lhs = Lhs;
                    rhs = Rhs;
                }
                public override double Evaluate(List<double> param)
                {
                    return lhs.Evaluate(param) * rhs.Evaluate(param);
                }
                AFunctionTreeNode lhs, rhs;
            }
            /// <summary>
            /// evaluate()s to division between two AFunctionTreeNode-s
            /// </summary>
            public class DivFuncTreeNode : AFunctionTreeNode
            {
                public override string ToString()
                {
                    return "(" + lhs.ToString() + "/" + rhs.ToString() + ")";
                }
                public DivFuncTreeNode(AFunctionTreeNode Lhs, AFunctionTreeNode Rhs)
                {
                    lhs = Lhs;
                    rhs = Rhs;
                }
                public override double Evaluate(List<double> param)
                {
                    return lhs.Evaluate(param) / rhs.Evaluate(param);
                }
                AFunctionTreeNode lhs, rhs;
            }
            /// <summary>
            /// evaluate()s to power of one AFunctionTreeNode x in another AFunctionTreeNode y 
            /// i.e. x^y
            /// </summary>
            public class PowerTreeNode : AFunctionTreeNode
            {
                public override string ToString()
                {
                    return "(" + lhs.ToString() + "^" + rhs.ToString() + ")";
                }
                public PowerTreeNode(AFunctionTreeNode x, AFunctionTreeNode y)
                {
                    lhs = x;
                    rhs = y;
                }
                public override double Evaluate(List<double> param)
                {
                    return System.Math.Pow(lhs.Evaluate(param), rhs.Evaluate(param));
                }
                AFunctionTreeNode lhs, rhs;
            }

            /// <summary>
            /// evaluate()s absolute
            /// </summary>
            public class AbsTreeNode : AFunctionTreeNode
            {
                AFunctionTreeNode n;
                public override string ToString()
                {
                    return "|" + n.ToString() + "|";
                }
                public AbsTreeNode(AFunctionTreeNode innerNode)
                {
                    n = innerNode;
                }
                public override double Evaluate(List<double> param)
                {
                    return System.Math.Abs(n.Evaluate(param));
                }
            }
            public static class Math
            {
                public static AbsTreeNode Abs(AFunctionTreeNode val)
                {
                    return new AbsTreeNode(val);
                }
                public static PowerTreeNode Pow(AFunctionTreeNode x, AFunctionTreeNode y)
                {
                    return new PowerTreeNode(x, y);
                }
            }
        }
    }
}