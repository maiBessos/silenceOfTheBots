using GoE.GameLogic;
using GoE.GameLogic.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.AppConstants;
using System.IO;
using GoE.Utils.Algorithms;
using GoE.Policies;
using GoE.Utils.Extensions;
using GoE.Utils;

namespace GoE
{
    
    /// <summary>
    /// the executable expects either:
    /// 1) graphical: no arguments at all. will open graphical view
    /// 2) cmd line: game model (one of the classes that inherit AGameProcess), then arg files. Example: goe.exe -gm goe Args/Wisec/Rep500PA_PC_PP.txt Args/Wisec/Rep500PA_PC.txt
    /// 
    /// see class ArgFile for guidelines on writing arg files
    /// </summary>
    static class Program
    {
        static List<Dictionary<string,string>> toTable(List<ProcessOutput> lines, Func<ProcessOutput,Dictionary<string,string>> lineGetter)
        {
            List<Dictionary<string, string>> res = new List<Dictionary<string, string>>();
            foreach(var l in lines)
                res.Add(lineGetter(l));
            return res;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            
            SimArguments args = new SimArguments(argv);

            if (args.cmdLineOnly)
            {
                AppSettings.setLogWriteFunc((string s) => Console.WriteLine(s), args.verboseLvl > 0);

                foreach (string s in args.errorLog)
                    AppSettings.WriteLogLine(s);

                Utils.Exceptions.ConditionalTryCatch<Exception>(() =>
                {
                    var resultsPerArgFile = SimProcess.process(args);
                    for(int i = 0; i < resultsPerArgFile.Count; ++i)
                    {
                        int repetetionCount = int.Parse(AppConstants.AppArgumentKeys.SIMULATION_REPETETION_COUNT.
                            tryRead(args.argFiles[i].processParams[0]));
                        string outFolder = AppConstants.AppArgumentKeys.OUTPUT_FOLDER.
                            tryRead(args.argFiles[i].processParams[0]);

                        string filename = args.argFiles[i].FileName.Split(new char[] { '\\' }).Last();
                        filename = filename.Substring(0, filename.LastIndexOf("."));
                        filename = filename.Split(new char[] { '\\', '/' }).Last();

                        string processFileName = outFolder + "\\" + filename +
                                                 "_rep" + repetetionCount.ToString() + "." + AppConstants.FileExtensions.PROCESS_OUTPUT;
                        string theoryFileName = outFolder + "\\" + filename +
                                                 "_thry" + "." + AppConstants.FileExtensions.PROCESS_OUTPUT;
                        string optimizerFileName = outFolder + "\\" + filename +
                                                 "_optimizer" + "." + AppConstants.FileExtensions.PROCESS_OUTPUT;

                        FileUtils.writeTable(toTable(resultsPerArgFile[i], (ProcessOutput p) => { return p.processOutput; }), processFileName);
                        FileUtils.writeTable(toTable(resultsPerArgFile[i], (ProcessOutput p) => { return p.theoryOutput; }), theoryFileName);
                        FileUtils.writeTable(toTable(resultsPerArgFile[i], (ProcessOutput p) => { return p.optimizerOutput; }), optimizerFileName);
                    }
                },
                (Exception ex) =>
                {
                    AppSettings.WriteLogLine("Process failed due to an exception in: ");
                    AppSettings.WriteLogLine(ex.StackTrace);
                    AppSettings.WriteLogLine("\nWith Message :");
                    AppSettings.WriteLogLine(ex.Message);
                });
                return;
            }


            // if graphical:
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Utils.Exceptions.ConditionalTryCatch<Exception>(() =>
            {
#if PUBLIC_ADV_ROUTING
                Application.Run(new AdvRoutingPublicMain());
#else
                Application.Run(new frmMain(args));
#endif

            },
            (Exception ex) =>
            {
                string inner = "";
                if (ex.InnerException != null)
                    inner = ex.InnerException.Message + "," + ex.InnerException.StackTrace;
                MessageBox.Show(ex.Message + " , " + ex.StackTrace + ", inner exception:" + inner);
            }
        );
        }
    }
}
