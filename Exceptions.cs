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
    public static class Exceptions
    {
        /// <summary>
        /// if EXTENSIVE_TRYCATCH flag is defined, this will act like a normal try/catch/finally.
        /// otherwise, only the code in tryAction will be invoked (unwrapped by "try") , then finallyAction
        /// and thrown exceptions will propagate
        /// </summary>
        public static void ConditionalTryCatch<ExType>(Action tryAction, Action<ExType> errAction = null, Action finallyAction = null) where ExType : Exception
        {
#if EXTENSIVE_TRYCATCH

            try { tryAction(); }
            catch (ExType ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
                if (errAction != null)
                    errAction(ex);
            }
            finally
            {
                if (finallyAction != null)
                    finallyAction();
            }
#else
            tryAction();
            if (finallyAction != null)
                finallyAction();
#endif
        }

        
        public static RetVal ConditionalTryCatch<ExType, RetVal>(RetVal failureReturnedVal, Func<RetVal> tryAction, Action<ExType> errAction = null, Action finallyAction = null) where ExType : Exception
        {
#if EXTENSIVE_TRYCATCH

            try { return tryAction(); }
            catch (ExType ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
                if (errAction != null)
                    errAction(ex);
                return failureReturnedVal;
            }
            finally
            {
                if (finallyAction != null)
                    finallyAction();
            }
#else
            RetVal v = tryAction();
            if (finallyAction != null)
                finallyAction();
            return v;
#endif
        }
        public static string getFlatDesc(Exception ex)
        {
            try
            {
                if (ex is AggregateException)
                {
                    AggregateException aex = (AggregateException)ex;
                    string allEx = "";
                    allEx += "AGGREGATE EXCEPTION: " + aex.StackTrace + "====================" + aex.Message + "||||||||||||||||||||||||||||||||||||||||||||";
                    foreach (var ae in aex.InnerExceptions)
                        allEx += ae.StackTrace + "====================" + ae.ToString() + "||||||||||||||||||||||||||||||||||||||||||||";

                    return allEx;
                }
            }
            catch (Exception) { }

            return ex.StackTrace + "====================" + ex.Message;
             
        }
    }
}
