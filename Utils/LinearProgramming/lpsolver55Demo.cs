﻿using GoE.GameLogic.EvolutionaryStrategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GoE.Utils.LPSolver55.lpsolve;

namespace GoE.Utils
{
    namespace LPSolver55
    {
        
        public class MyDemo
        {
            public static void Test()
            {
                lpsolveWrapper newSolver = new lpsolveWrapper(3);
                //newSolver.addConstraint(new List<Tuple<int, double>>() { Tuple.Create(0, 1.0) }, lpsolve_constr_types.LE, 10); // x <= 10
                //newSolver.addConstraint(new List<Tuple<int, double>>() { Tuple.Create(1, 1.0) }, lpsolve_constr_types.LE, 5); // y <= 5
                newSolver.addBound(0, 0, 10); // 0 <= x <= 10
                //newSolver.addBound(1, 0, 9); // 0 <= y <= 5
                newSolver.addConstraint(new List<Tuple<int, double>>() { Tuple.Create(1, 1.0) }, lpsolve_constr_types.EQ,3); // y = 3
                newSolver.addConstraint(new List<Tuple<int, double>>() { Tuple.Create(1, -1.0), Tuple.Create(2, 1.0) }, lpsolve_constr_types.LE, 0); // z-y <=0
                newSolver.addConstraint(new List<Tuple<int, double>>() { Tuple.Create(0, -1.0), Tuple.Create(2, 1.0) }, lpsolve_constr_types.LE, 0); // z-x <=0
                newSolver.solve(new List<Tuple<int, double>>() { Tuple.Create(2, 1.0) });
                var obj = newSolver.ObjectiveTermValue;
                var vals = newSolver.VariableValue;
            }
        }
        public class demo
        {
            //[STAThread]
            public static void test()
            {
                //System.Diagnostics.Debug.WriteLine(System.Environment.CurrentDirectory);
                lpsolve.Init(".");

                Test();

                //TestMultiThreads();
            }

            /* unsafe is needed to make sure that these function are not relocated in memory by the CLR. If that would happen, a crash occurs */
            /* go to the project property page and in “configuration properties>build” set Allow Unsafe Code Blocks to True. */
            /* see http://msdn2.microsoft.com/en-US/library/chfa2zb8.aspx and http://msdn2.microsoft.com/en-US/library/t2yzs44b.aspx */
            private /* unsafe */ static void logfunc(IntPtr lp, int userhandle, string Buf)
            {
                System.Diagnostics.Debug.Write(Buf);
            }

            private /* unsafe */ static byte ctrlcfunc(IntPtr lp, int userhandle)
            {
                /* 'If set to true, then solve is aborted and returncode will indicate this. */
                return (0);
            }

            private /* unsafe */ static void msgfunc(IntPtr lp, int userhandle, lpsolve.lpsolve_msgmask message)
            {
                System.Diagnostics.Debug.WriteLine(message);
            }

            private static void ThreadProc(object filename)
            {
                IntPtr lp;
                lpsolve.lpsolve_return ret;
                double o;

                lp = lpsolve.read_LP((string)filename, 0, "");
                ret = lpsolve.solve(lp);
                o = lpsolve.get_objective(lp);
                Debug.Assert(ret == lpsolve.lpsolve_return.OPTIMAL && Math.Round(o, 13) == 1779.4810350637485);
                lpsolve.delete_lp(lp);
            }

            private static void TestMultiThreads()
            {
                int release = 0, Major = 0, Minor = 0, build = 0;

                lpsolve.lp_solve_version(ref Major, ref Minor, ref release, ref build);

                for (int i = 1; i <= 5000; i++)
                {
                    Thread myThread = new Thread(new ParameterizedThreadStart(ThreadProc));
                    myThread.Start("ex4.lp");
                }

                Thread.Sleep(5000);
            }

            private static void Test()
            {
                const string NewLine = "\n";

                IntPtr lp;
                int release = 0, Major = 0, Minor = 0, build = 0;
                double[] Row;
                double[] Lower;
                double[] Upper;
                double[] Col;
                double[] Arry;

                lp = lpsolve.make_lp(0, 4);

                lpsolve.lp_solve_version(ref Major, ref Minor, ref release, ref build);

                /* let's first demonstrate the logfunc callback feature */
                lpsolve.put_logfunc(lp, new lpsolve.logfunc(logfunc), 0);
                lpsolve.print_str(lp, "lp_solve " + Major + "." + Minor + "." + release + "." + build + " demo" + NewLine + NewLine);
                lpsolve.solve(lp); /* just to see that a message is send via the logfunc routine ... */
                                   /* ok, that is enough, no more callback */
                lpsolve.put_logfunc(lp, null, 0);

                /* Now redirect all output to a file */
                lpsolve.set_outputfile(lp, "result.txt");

                /* set an abort function. Again optional */
                lpsolve.put_abortfunc(lp, new lpsolve.ctrlcfunc(ctrlcfunc), 0);

                /* set a message function. Again optional */
                lpsolve.put_msgfunc(lp, new lpsolve.msgfunc(msgfunc), 0, (int)(lpsolve.lpsolve_msgmask.MSG_PRESOLVE | lpsolve.lpsolve_msgmask.MSG_LPFEASIBLE | lpsolve.lpsolve_msgmask.MSG_LPOPTIMAL | lpsolve.lpsolve_msgmask.MSG_MILPEQUAL | lpsolve.lpsolve_msgmask.MSG_MILPFEASIBLE | lpsolve.lpsolve_msgmask.MSG_MILPBETTER));

                lpsolve.print_str(lp, "lp_solve " + Major + "." + Minor + "." + release + "." + build + " demo" + NewLine + NewLine);
                lpsolve.print_str(lp, "This demo will show most of the features of lp_solve " + Major + "." + Minor + "." + release + "." + build + NewLine);

                lpsolve.print_str(lp, NewLine + "We start by creating a new problem with 4 variables and 0 constraints" + NewLine);
                lpsolve.print_str(lp, "We use: lp = lpsolve.make_lp(0, 4);" + NewLine);

                lpsolve.set_timeout(lp, 0);

                lpsolve.print_str(lp, "We can show the current problem with lpsolve.print_lp(lp);" + NewLine);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "Now we add some constraints" + NewLine);
                lpsolve.print_str(lp, "lpsolve.add_constraint(lp, Row, lpsolve.lpsolve_constr_types.LE, 4);" + NewLine);
                // pay attention to the 1 base and ignored 0 column for constraints
                lpsolve.add_constraint(lp, new double[] { 0, 3, 2, 2, 1 }, lpsolve.lpsolve_constr_types.LE, 4);
                lpsolve.print_lp(lp);

                // check ROW array worsk
                Row = new double[] { 0, 0, 4, 3, 1 };
                lpsolve.print_str(lp, "lpsolve.add_constraint(lp, Row, lpsolve.lpsolve_constr_types.GE, 3);" + NewLine);
                lpsolve.add_constraint(lp, Row, lpsolve.lpsolve_constr_types.GE, 3);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "Set the objective function" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_obj_fn(lp, Row);" + NewLine);
                lpsolve.set_obj_fn(lp, new double[] { 0, 2, 3, -2, 3 });
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "Now solve the problem with lpsolve.solve(lp);" + NewLine);
                lpsolve.print_str(lp, lpsolve.solve(lp) + ": " + lpsolve.get_objective(lp) + NewLine);

                Col = new double[lpsolve.get_Ncolumns(lp)];
                lpsolve.get_variables(lp, Col);

                Row = new double[lpsolve.get_Nrows(lp)];
                lpsolve.get_constraints(lp, Row);

                Arry = new double[lpsolve.get_Ncolumns(lp) + lpsolve.get_Nrows(lp) + 1];
                lpsolve.get_dual_solution(lp, Arry);

                Arry = new double[lpsolve.get_Ncolumns(lp) + lpsolve.get_Nrows(lp)];
                Lower = new double[lpsolve.get_Ncolumns(lp) + lpsolve.get_Nrows(lp)];
                Upper = new double[lpsolve.get_Ncolumns(lp) + lpsolve.get_Nrows(lp)];
                lpsolve.get_sensitivity_rhs(lp, Arry, Lower, Upper);

                Lower = new double[lpsolve.get_Ncolumns(lp) + 1];
                Upper = new double[lpsolve.get_Ncolumns(lp) + 1];
                lpsolve.get_sensitivity_obj(lp, Lower, Upper);

                lpsolve.print_str(lp, "The value is 0, this means we found an optimal solution" + NewLine);
                lpsolve.print_str(lp, "We can display this solution with lpsolve.print_solution(lp);" + NewLine);
                lpsolve.print_objective(lp);
                lpsolve.print_solution(lp, 1);
                lpsolve.print_constraints(lp, 1);

                lpsolve.print_str(lp, "The dual variables of the solution are printed with" + NewLine);
                lpsolve.print_str(lp, "lpsolve.print_duals(lp);" + NewLine);
                lpsolve.print_duals(lp);

                lpsolve.print_str(lp, "We can change a single element in the matrix with" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_mat(lp, 2, 1, 0.5);" + NewLine);
                lpsolve.set_mat(lp, 2, 1, 0.5);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "If we want to maximize the objective function use lpsolve.set_maxim(lp);" + NewLine);
                lpsolve.set_maxim(lp);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "after solving this gives us:" + NewLine);
                lpsolve.solve(lp);
                lpsolve.print_objective(lp);
                lpsolve.print_solution(lp, 1);
                lpsolve.print_constraints(lp, 1);
                lpsolve.print_duals(lp);

                lpsolve.print_str(lp, "Change the value of a rhs element with lpsolve.set_rh(lp, 1, 7.45);" + NewLine);
                lpsolve.set_rh(lp, 1, 7.45);
                lpsolve.print_lp(lp);
                lpsolve.solve(lp);
                lpsolve.print_objective(lp);
                lpsolve.print_solution(lp, 1);
                lpsolve.print_constraints(lp, 1);

                lpsolve.print_str(lp, "We change C4 to the integer type with" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_int(lp, 4, true);" + NewLine);
                lpsolve.set_int(lp, 4, 1);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "We set branch & bound debugging on with lpsolve.set_debug(lp, true);" + NewLine);

                lpsolve.set_debug(lp, 1);
                lpsolve.print_str(lp, "and solve..." + NewLine);

                lpsolve.solve(lp);
                lpsolve.print_objective(lp);
                lpsolve.print_solution(lp, 1);
                lpsolve.print_constraints(lp, 1);

                lpsolve.print_str(lp, "We can set bounds on the variables with" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_lowbo(lp, 2, 2); & lpsolve.set_upbo(lp, 4, 5.3);" + NewLine);
                lpsolve.set_lowbo(lp, 2, 2);
                lpsolve.set_upbo(lp, 4, 5.3);
                lpsolve.print_lp(lp);

                lpsolve.solve(lp);
                lpsolve.print_objective(lp);
                lpsolve.print_solution(lp, 1);
                lpsolve.print_constraints(lp, 1);

                lpsolve.print_str(lp, "Now remove a constraint with lpsolve.del_constraint(lp, 1);" + NewLine);
                lpsolve.del_constraint(lp, 1);
                lpsolve.print_lp(lp);
                lpsolve.print_str(lp, "Add an equality constraint" + NewLine);
                Row = new double[] { 0, 1, 2, 1, 4 };
                lpsolve.add_constraint(lp, Row, lpsolve.lpsolve_constr_types.EQ, 8);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "A column can be added with:" + NewLine);
                lpsolve.print_str(lp, "lpsolve.add_column(lp, Col);" + NewLine);
                lpsolve.add_column(lp, new double[] { 3, 2, 2 });
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "A column can be removed with:" + NewLine);
                lpsolve.print_str(lp, "lpsolve.del_column(lp, 3);" + NewLine);
                lpsolve.del_column(lp, 3);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "We can use automatic scaling with:" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_scaling(lp, lpsolve.lpsolve_scales.SCALE_MEAN);" + NewLine);
                lpsolve.set_scaling(lp, lpsolve.lpsolve_scales.SCALE_MEAN);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "The function lpsolve.get_mat(lp, row, column); returns a single" + NewLine);
                lpsolve.print_str(lp, "matrix element" + NewLine);
                lpsolve.print_str(lp, "lpsolve.get_mat(lp, 2, 3); lpsolve.get_mat(lp, 1, 1); gives " + lpsolve.get_mat(lp, 2, 3) + ", " + lpsolve.get_mat(lp, 1, 1) + NewLine);
                lpsolve.print_str(lp, "Notice that get_mat returns the value of the original unscaled problem" + NewLine);

                lpsolve.print_str(lp, "If there are any integer type variables, then only the rows are scaled" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_int(lp, 3, false);" + NewLine);
                lpsolve.set_int(lp, 3, 0);
                lpsolve.print_lp(lp);

                lpsolve.solve(lp);
                lpsolve.print_str(lp, "print_solution gives the solution to the original problem" + NewLine);
                lpsolve.print_objective(lp);
                lpsolve.print_solution(lp, 1);
                lpsolve.print_constraints(lp, 1);

                lpsolve.print_str(lp, "Scaling is turned off with lpsolve.unscale(lp);" + NewLine);
                lpsolve.unscale(lp);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "Now turn B&B debugging off and simplex tracing on with" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_debug(lp, false); lpsolve.set_trace(lp, true); and lpsolve.solve(lp);" + NewLine);
                lpsolve.set_debug(lp, 0);
                lpsolve.set_trace(lp, 1);

                lpsolve.solve(lp);
                lpsolve.print_str(lp, "Where possible, lp_solve will start at the last found basis" + NewLine);
                lpsolve.print_str(lp, "We can reset the problem to the initial basis with" + NewLine);
                lpsolve.print_str(lp, "default_basis lp. Now solve it again..." + NewLine);

                lpsolve.default_basis(lp);
                lpsolve.solve(lp);

                lpsolve.print_str(lp, "It is possible to give variables and constraints names" + NewLine);
                lpsolve.print_str(lp, "lpsolve.set_row_name(lp, 1, \"speed\"); lpsolve.set_col_name(lp, 2, \"money\");" + NewLine);
                lpsolve.set_row_name(lp, 1, "speed");
                lpsolve.set_col_name(lp, 2, "money");
                lpsolve.print_lp(lp);
                lpsolve.print_str(lp, "As you can see, all column and rows are assigned default names" + NewLine);
                lpsolve.print_str(lp, "If a column or constraint is deleted, the names shift place also:" + NewLine);

                lpsolve.print_str(lp, "lpsolve.del_column(lp, 1);" + NewLine);
                lpsolve.del_column(lp, 1);
                lpsolve.print_lp(lp);

                lpsolve.write_lp(lp, "lp.lp");
                lpsolve.write_mps(lp, "lp.mps");

                lpsolve.set_outputfile(lp, null);

                lpsolve.delete_lp(lp);

                lp = lpsolve.read_LP("lp.lp", 0, "test");
                if (lp == (IntPtr)0)
                {
                    MessageBox.Show("Can't find lp.lp, stopping");
                    return;
                }

                lpsolve.set_outputfile(lp, "result2.txt");

                lpsolve.print_str(lp, "An lp structure can be created and read from a .lp file" + NewLine);
                lpsolve.print_str(lp, "lp = lpsolve.read_LP(\"lp.lp\", 0, \"test\");" + NewLine);
                lpsolve.print_str(lp, "The verbose option is disabled" + NewLine);

                lpsolve.print_str(lp, "lp is now:" + NewLine);
                lpsolve.print_lp(lp);

                lpsolve.print_str(lp, "solution:" + NewLine);
                lpsolve.set_debug(lp, 1);
                lpsolve.lpsolve_return statuscode = lpsolve.solve(lp);
                //string status = lpsolve.get_statustext(lp, (int)statuscode);
                //Debug.WriteLine(status);

                lpsolve.set_debug(lp, 0);
                lpsolve.print_objective(lp);
                lpsolve.print_solution(lp, 1);
                lpsolve.print_constraints(lp, 1);

                lpsolve.write_lp(lp, "lp.lp");
                lpsolve.write_mps(lp, "lp.mps");

                lpsolve.set_outputfile(lp, null);

                lpsolve.delete_lp(lp);
            }   //Test
        }
        public class DemoEx
        {
            public static int Demo()
            {
                lpsolve.Init(".");
                IntPtr lp;
                int Ncol;
                int[] colno;
                int j, ret = 0;
                double[] row;

                /* We will build the model row by row */
                /* So we start with creating a model with 0 rows and 2 columns */
                Ncol = 2; /* there are two variables in the model */
                lp = lpsolve.make_lp(0, Ncol);
                if (lp == null)
                    ret = 1; /* couldn't construct a new model... */

                if (ret == 0)
                {
                    /* let us name our variables. Not required, but can be useful for debugging */
                    lpsolve.set_col_name(lp, 1, "x");
                    lpsolve.set_col_name(lp, 2, "y");
                }

                /* create space large enough for one row */
                colno = new int[Ncol];
                row = new double[Ncol];

                if (ret == 0)
                {
                    lpsolve.set_add_rowmode(lp, 1); /* makes building the model faster if it is done rows by row */

                    /* construct first row (120 x + 210 y <= 15000) */
                    j = 0;

                    colno[j] = 1; /* first column */
                    row[j++] = 120;

                    colno[j] = 2; /* second column */
                    row[j++] = 210;

                    /* add the row to lpsolve */
                    if (lpsolve.add_constraintex(lp, j, row, colno, lpsolve.lpsolve_constr_types.LE, 15000) == 0)
                        ret = 3;
                }

                if (ret == 0)
                {
                    /* construct second row (110 x + 30 y <= 4000) */
                    j = 0;

                    colno[j] = 1; /* first column */
                    row[j++] = 110;

                    colno[j] = 2; /* second column */
                    row[j++] = 30;

                    /* add the row to lpsolve */
                    if (lpsolve.add_constraintex(lp, j, row, colno, lpsolve.lpsolve_constr_types.LE, 4000) == 0)
                        ret = 3;
                }

                if (ret == 0)
                {
                    /* construct third row (x + y <= 75) */
                    j = 0;

                    colno[j] = 1; /* first column */
                    row[j++] = 1;

                    colno[j] = 2; /* second column */
                    row[j++] = 1;

                    /* add the row to lpsolve */
                    if (lpsolve.add_constraintex(lp, j, row, colno, lpsolve.lpsolve_constr_types.LE, 75) == 0)
                        ret = 3;
                }

                if (ret == 0)
                {
                    lpsolve.set_add_rowmode(lp, 0); /* rowmode should be turned off again when done building the model */

                    /* set the objective function (143 x + 60 y) */
                    j = 0;

                    colno[j] = 1; /* first column */
                    row[j++] = 143;

                    colno[j] = 2; /* second column */
                    row[j++] = 60;

                    /* set the objective in lpsolve */
                    if (lpsolve.set_obj_fnex(lp, j, row, colno) == 0)
                        ret = 4;
                }

                if (ret == 0)
                {
                    lpsolve.lpsolve_return s;

                    /* set the object direction to maximize */
                    lpsolve.set_maxim(lp);

                    /* just out of curioucity, now show the model in lp format on screen */
                    /* this only works if this is a console application. If not, use write_lp and a filename */
                    lpsolve.write_lp(lp, "model.lp");

                    /* I only want to see important messages on screen while solving */
                    lpsolve.set_verbose(lp, 3);

                    /* Now let lpsolve calculate a solution */
                    s = lpsolve.solve(lp);
                    if (s == lpsolve.lpsolve_return.OPTIMAL)
                        ret = 0;
                    else
                        ret = 5;
                }

                if (ret == 0)
                {
                    /* a solution is calculated, now lets get some results */

                    /* objective value */
                    //AppSettings.WriteLogLine("Objective value: " + lpsolve.get_objective(lp));
                    double obj = lpsolve.get_objective(lp);

                    /* variable values */
                    lpsolve.get_variables(lp, row);
                    //for (j = 0; j < Ncol; j++)
                      //  AppSettings.WriteLogLine("var " + j.ToString() + ": " + row[j]);

                    /* we are done now */
                }

                /* free allocated memory */

                if (lp != null)
                {
                    /* clean up such that all used memory by lpsolve is freed */
                    lpsolve.delete_lp(lp);
                }

                return (ret);
            } //Demo
        }
        
    }
}
