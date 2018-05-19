using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Utils
{
    namespace RootFinding
    {
        
        public delegate double FunctionOfOneVariable(double x);

        public class NumericMethods
        {
            // original code taken from http://www.codeproject.com/Articles/79541/Three-Methods-for-Root-finding-in-C
            const int maxIterations = 100;

            public static double Bisect
            (
                FunctionOfOneVariable f,
                double left,
                double right,
                double tolerance = 1e-6,
                double target = 0.0
            )
            {
                // extra info that callers may not always want
                int iterationsUsed;
                double errorEstimate;

                return Bisect(f, left, right, tolerance, target, out iterationsUsed, out errorEstimate);
            }

            public static double Bisect
            (
                FunctionOfOneVariable f,
                double left,
                double right,
                double tolerance,
                double target,
                out int iterationsUsed,
                out double errorEstimate
            )
            {
                if (tolerance <= 0.0)
                {
                    string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                    throw new ArgumentOutOfRangeException(msg);
                }

                iterationsUsed = 0;
                errorEstimate = double.MaxValue;

                // Standardize the problem.  To solve f(x) = target,
                // solve g(x) = 0 where g(x) = f(x) - target.
                FunctionOfOneVariable g = delegate(double x) { return f(x) - target; };


                double g_left = g(left);  // evaluation of f at left end of interval
                double g_right = g(right);
                double mid;
                double g_mid;
                if (g_left * g_right >= 0.0)
                {
                    string str = "Invalid starting bracket. Function must be above target on one end and below target on other end.";
                    string msg = string.Format("{0} Target: {1}. f(left) = {2}. f(right) = {3}", str, g_left + target, g_right + target);
                    throw new ArgumentException(msg);
                }

                double intervalWidth = right - left;

                for
                (
                    iterationsUsed = 0;
                    iterationsUsed < maxIterations && intervalWidth > tolerance;
                    iterationsUsed++
                )
                {
                    intervalWidth *= 0.5;
                    mid = left + intervalWidth;

                    if ((g_mid = g(mid)) == 0.0)
                    {
                        errorEstimate = 0.0;
                        return mid;
                    }
                    if (g_left * g_mid < 0.0)           // g changes sign in (left, mid)    
                        g_right = g(right = mid);
                    else                            // g changes sign in (mid, right)
                        g_left = g(left = mid);
                }
                errorEstimate = right - left;
                return left;
            }

            public static double Brent
            (
                FunctionOfOneVariable f,
                double left,
                double right,
                double tolerance = 1e-6,
                double target = 0.0
            )
            {
                // extra info that callers may not always want
                int iterationsUsed;
                double errorEstimate;

                return Brent(f, left, right, tolerance, target, out iterationsUsed, out errorEstimate);
            }

            public static double Brent
            (
                FunctionOfOneVariable g,
                double left,
                double right,
                double tolerance,
                double target,
                out int iterationsUsed,
                out double errorEstimate
            )
            {
                if (tolerance <= 0.0)
                {
                    string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                    throw new ArgumentOutOfRangeException(msg);
                }

                errorEstimate = double.MaxValue;

                // Standardize the problem.  To solve g(x) = target,
                // solve f(x) = 0 where f(x) = g(x) - target.
                FunctionOfOneVariable f = delegate(double x) { return g(x) - target; };

                // Implementation and notation based on Chapter 4 in
                // "Algorithms for Minimization without Derivatives"
                // by Richard Brent.

                double c, d, e, fa, fb, fc, tol, m, p, q, r, s;

                // set up aliases to match Brent's notation
                double a = left; double b = right; double t = tolerance;
                iterationsUsed = 0;

                fa = f(a);
                fb = f(b);

                if (fa * fb > 0.0)
                {
                    string str = "Invalid starting bracket. Function must be above target on one end and below target on other end.";
                    string msg = string.Format("{0} Target: {1}. f(left) = {2}. f(right) = {3}", str, target, fa + target, fb + target);
                    throw new ArgumentException(msg);
                }

            label_int:
                c = a; fc = fa; d = e = b - a;
            label_ext:
                if (Math.Abs(fc) < Math.Abs(fb))
                {
                    a = b; b = c; c = a;
                    fa = fb; fb = fc; fc = fa;
                }

                iterationsUsed++;

                tol = 2.0 * t * Math.Abs(b) + t;
                errorEstimate = m = 0.5 * (c - b);
                if (Math.Abs(m) > tol && fb != 0.0) // exact comparison with 0 is OK here
                {
                    // See if bisection is forced
                    if (Math.Abs(e) < tol || Math.Abs(fa) <= Math.Abs(fb))
                    {
                        d = e = m;
                    }
                    else
                    {
                        s = fb / fa;
                        if (a == c)
                        {
                            // linear interpolation
                            p = 2.0 * m * s; q = 1.0 - s;
                        }
                        else
                        {
                            // Inverse quadratic interpolation
                            q = fa / fc; r = fb / fc;
                            p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                            q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                        }
                        if (p > 0.0)
                            q = -q;
                        else
                            p = -p;
                        s = e; e = d;
                        if (2.0 * p < 3.0 * m * q - Math.Abs(tol * q) && p < Math.Abs(0.5 * s * q))
                            d = p / q;
                        else
                            d = e = m;
                    }
                    a = b; fa = fb;
                    if (Math.Abs(d) > tol)
                        b += d;
                    else if (m > 0.0)
                        b += tol;
                    else
                        b -= tol;
                    if (iterationsUsed == maxIterations)
                        return b;

                    fb = f(b);
                    if ((fb > 0.0 && fc > 0.0) || (fb <= 0.0 && fc <= 0.0))
                        goto label_int;
                    else
                        goto label_ext;
                }
                else
                    return b;
            }

            public static double Newton
            (
                FunctionOfOneVariable f,
                FunctionOfOneVariable fprime,
                double guess,
                double tolerance = 1e-6,
                double target = 0.0
            )
            {
                // extra info that callers may not always want
                int iterationsUsed;
                double errorEstimate;

                return Newton(f, fprime, guess, tolerance, target, out iterationsUsed, out errorEstimate);
            }

            public static double Newton
            (
                FunctionOfOneVariable f,
                FunctionOfOneVariable fprime,
                double guess,
                double tolerance,
                double target,
                out int iterationsUsed,
                out double errorEstimate
            )
            {
                if (tolerance <= 0)
                {
                    string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
                    throw new ArgumentOutOfRangeException(msg);
                }

                iterationsUsed = 0;
                errorEstimate = double.MaxValue;

                // Standardize the problem.  To solve f(x) = target,
                // solve g(x) = 0 where g(x) = f(x) - target.
                // Note that f(x) and g(x) have the same derivative.
                FunctionOfOneVariable g = delegate(double x) { return f(x) - target; };

                double oldX, newX = guess;
                for
                (
                    iterationsUsed = 0;
                    iterationsUsed < maxIterations && errorEstimate > tolerance;
                    iterationsUsed++
                )
                {
                    oldX = newX;
                    double gx = g(oldX);
                    double gprimex = fprime(oldX);
                    double absgprimex = Math.Abs(gprimex);
                    if (absgprimex > 1.0 || Math.Abs(gx) < double.MaxValue * absgprimex)
                    {
                        // The division will not overflow
                        newX = oldX - gx / gprimex;
                        errorEstimate = Math.Abs(newX - oldX);
                    }
                    else
                    {
                        newX = oldX;
                        errorEstimate = double.MaxValue;
                        break;
                    }
                }
                return newX;
            }
        }
    }    
    namespace MathExExtensions
    {
        public static class Scalar
        {
            /// <summary>
            /// translates boolean with value 0 or 1 to values -1 or 1
            /// </summary>
            /// <param name="val"></param>
            /// <returns></returns>
            public static int ToDir(this bool val)
            {
                return -1 + Convert.ToInt32(val) * 2;
            }

            /// <summary>
            /// see ToDir(bool)
            /// </summary>
            /// <param name="val"></param>
            /// <returns></returns>
            public static int ToDir(this int val)
            {
                return -1 + val * 2;
            }

            /// <summary>
            /// expectes value -1 or 1, and translates it to 0 or 1  (respectively)
            /// </summary>
            /// <param name="val"></param>
            /// <returns></returns>
            public static int FromDir(this int val)
            {
                return Convert.ToInt32(val == 1);
            }


        }
    }
    public static class MathEx
    {

        public static bool makeFinite(ref float v)
        {
            if (float.IsPositiveInfinity(v))
                v = float.MaxValue;
            else if (float.IsNegativeInfinity(v))
                v = -float.MaxValue;
            return !float.IsNaN(v);
        }

        /// <summary>
        /// for small y values, faster (significnalty!) than Math.pow()
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static double PowInt(double x, int y)
        {
            double x3, x4;

            switch(y)
            {
                case 1: return x;
                case 2: return x * x;
                case 3: return x * x * x;
                case 4: return x * x * x * x;
                case 5: return x * x * x * x * x;
                case 6: x3 = x * x * x; return x3 * x3;
                case 7: x3 = x * x * x; return x3 * x3 * x;
                case 8: x4 = x * x * x * x; return x4 * x4;
                case 9: x3 = x * x * x; return x3 * x3 * x3;
                case 10: x3 = x * x * x; return x3 * x3 * x3 * x;
                case 11: x3 = x * x * x; return x3 * x3 * x3 * x * x;
                case 12: x4 = x * x * x * x; return x4*x4*x4;
                default: return Math.Pow(x, y);
            }
        }


       
        /// <summary>
        /// assumes all values in values 0 to mod-1 
        /// tells how many increments to 'from' are needed to get 'to'
        /// </summary>
        public static int modDist(int mod, int from, int to)
        {
            if (to >= from)
                return to - from;
            return mod - from + to;
        }

        /// <summary>
        /// assumes all values in values 0 to mod-1 
        /// tells how many "increments" to 'from' are needed to get 'to'
        /// </summary>
        /// <param name="increment">
        /// if 1, acts like modDist(int mod, int from, int to)
        /// if -1, assumes increments are in the opposite direction
        /// </param>
        public static int modDist(int mod, int from, int to, int increment)
        {
            if (increment == -1)
                return modDist(mod, to, from);
            
            return modDist(mod, from, to);
        }

        public static int minModDist(int mod, int from, int to)
        {
            return Math.Min(modDist(mod, to, from), modDist(mod, from, to));
        }

        /// <summary>
        /// assumes all values in values 0 to mod - 1. 
        /// Tests whether in order to get from min to max (with increments only), the values go through val 
        /// (e.g. in a clock from 1 to 12, if min=9 and max=4 => return true for val = 1, return false for val = 8)
        /// // FIXME: why do we need the 'mod' parameter?
        /// </summary>
        public static bool modIsBetween(int mod, int min, int max, int val)
        {
            if (max > min)
                return val <= max && val >= min;
            return val <= max || val >= min;
        }
     

        /// <summary>
        /// modulu for floats
        /// (modfloat)
        /// </summary>
        /// <returns></returns>
        public static float modf(float val, float maxModVal)
        {
            return val -= (float)Math.Floor(val / maxModVal) * maxModVal;
        }

        // if a1 is the first value, and each following value increases by diff, and n2>n, 
        // this returns sum(1,...,n2) - sum(1,...n)

        public static double sum(double a1, double diff, double n, double n2)
        {
            return (n2 * (2 * a1 + (n2 - 1) * diff) / 2) -
                   (n * (2 * a1 + (n - 1) * diff) / 2);
        }

        public static T LimitRange<T>(this T value, T minValue, T maxValue) where T : IComparable
        {
            if (value.CompareTo(minValue) < 0)
                return minValue;
            if (value.CompareTo(maxValue) > 0)
                return maxValue;
            return value;
        }
        public static double Sqr(double v) 
        {
            return v * v; // should be faster tham Math.Pow(v,2)
        }

        /// <summary>
        /// gets two values, and sets outMin to be the smaller one, and outMax to be the bigger
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="outMin"></param>
        /// <param name="outMax"></param>
        public static void MinMax<T>(ref T outMin, ref T outMax) where T:IComparable<T>
        {
            if (outMin.CompareTo(outMax) <= 0)
                return;
            T tmp = outMin;
            outMin = outMax;
            outMax = tmp;
            
        }

        
    }
}

