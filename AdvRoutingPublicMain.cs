using GoE.AdvRouting;
using GoE.AppConstants;
using GoE.AppConstants.Policies;
using GoE.GameLogic;
using GoE.Policies;
using GoE.Utils;
using GoE.Utils.Algorithms;
using GoE.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE
{
    public partial class AdvRoutingPublicMain : Form
    {
        string latestLegalTxtParams = "";
        Thread paramTxtChecker;
        Type gameProcessType;
        Type gameParamsType;
        Dictionary<string, string> inputArgs = null;
        Thread processThread = null;

        string selectedPursuersPolicy = "";
        string selectedEvadersPolicy = "";

        public AdvRoutingPublicMain()
        {
            InitializeComponent();
        }

        private void AdvRoutingPublicMain_Load(object sender, EventArgs e)
        {
            initialParams = new List<string>(txtUserInput.Lines);
            var vals = ParsingUtils.parseValueMap(ParsingUtils.clearComments(txtParams.Lines));
            updateActiveGameType(vals);
        }

        private void runGUIDefault(Dictionary<string, string> processParams, AGameGraph g)
        {
            Dictionary<string, string> policyInput = new Dictionary<string, string>(processParams);
            
            AGameProcess gproc = Utils.ReflectionUtils.constructEmptyCtorTypeFromTid<AGameProcess>(gameProcessType);

            IGameParams gp = gproc.constructGameParams();
            gp.deserialize(AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(policyInput), policyInput);

            //FrontsGridRoutingGameParams gp;
            //gp = FrontsGridRoutingGameParams.getParamsFromFile(
            //    AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(policyInput), policyInput);


            gproc.initParams(gp, g);
            IGameProcessGUI processView = gproc.constructGUI();
            processView.init(g, gproc);

            IEvadersPolicy chosenEvaderPolicy;

            try
            {
                chosenEvaderPolicy = (IEvadersPolicy)
                    ReflectionUtils.getTypeList<IEvadersPolicy>()[AppConstants.AppArgumentKeys.EVADER_POLICY.tryRead(policyInput)].
                    //AGoEEvadersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.EVADER_POLICY.tryRead(policyInput)].
                    GetConstructor(new Type[] { }).Invoke(new object[] { });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't load evaders policy:" + ex.Message);
                return;
            }

            APursuersPolicy chosenPursuerPolicy;
            try
            {
                chosenPursuerPolicy = (APursuersPolicy)
                    APursuersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(policyInput)].
                    GetConstructor(new Type[] { }).Invoke(new object[] { });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't load pursuers policy:" + ex.Message);
                return;
            }

            chosenPursuerPolicy.init(g, gp, processView, policyInput);
            chosenEvaderPolicy.init(g, gp, chosenPursuerPolicy, processView, policyInput);

            gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);
            ((Form)processView).Show();

        }
        private void runGUI(Dictionary<string, string> processParams)
        {

            if (gameProcessType == typeof(GoEGameProcess))
            {
                GridGameGraph g =
                    new GridGameGraph(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(processParams));

                GoEGameParams gp =
                    GoEGameParams.getParamsFromFile(
                        AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(processParams), processParams);

                GameLogic.GoEGameProcess gproc = new GameLogic.GoEGameProcess();
                gproc.initParams(gp, g);
                frmGameProcessView processView = new frmGameProcessView();
                processView.init(g, gproc);

                AGoEPursuersPolicy chosenPursuerPolicy;

                try
                {
                    chosenPursuerPolicy = (AGoEPursuersPolicy)
                        AGoEPursuersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(processParams)].
                        GetConstructor(new Type[] { }).Invoke(new object[] { });
                }
                catch (Exception)
                {
                    MessageBox.Show("Couldn't load pursuers policy");
                    return;
                }

                Dictionary<string, string> policyInput = new Dictionary<string, string>(processParams);
                
                //policyInput.AddRange(chosenPursuerPolicy.preProcess(g, gp, policyInput));
                // FIXME: instead of invoking theoretical optimizer, check if another optimizer was specified
                policyInput.AddRange(SimProcess.getPursuersPolicyTheoreticalOptimizerResult(chosenPursuerPolicy.GetType().Name, g, gp, policyInput), false);

                // we read evaders policy only after optimizer is invoked, since it may choose the evaders policy
                AGoEEvadersPolicy chosenEvaderPolicy;
                try
                {
                    chosenEvaderPolicy = (AGoEEvadersPolicy)AGoEEvadersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.EVADER_POLICY.tryRead(policyInput)].
                    GetConstructor(new Type[] { }).Invoke(new object[] { });
                }
                catch (Exception)
                {
                    MessageBox.Show("Couldn't load evaders policy");
                    return;
                }

                chosenPursuerPolicy.init(g, gp, processView, policyInput);
                chosenEvaderPolicy.init(g, gp, chosenPursuerPolicy, processView, policyInput);

                gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);
                processView.Show();
            }
            else
            {
                AGameGraph g = new EmptyEnvironment();
                runGUIDefault(processParams, g);
            }
        }

        /// <summary>
        /// if key isn't found or parsing fails, an exception is thrown
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        int tryParse(Dictionary<string, string> vals, string key, string errorMessage)
        {
            int res;
            
            if (!vals.ContainsKey(key) ||
                !Int32.TryParse(vals[key], out res))
            {
                MessageBox.Show(key + " should be a (int) numeric value " + errorMessage);
                throw new Exception();
            }
            return res;
        }
        float tryParseF(Dictionary<string, string> vals, string key, string errorMessage)
        {
            float res;

            if (!vals.ContainsKey(key) ||
                !float.TryParse(vals[key], out res))
            {
                MessageBox.Show(key + " should be a (float) numeric value " + errorMessage);
                throw new Exception();
            }
            return res;
        }
        Dictionary<string, string> getOverridingInputStrings()
        {
            string intAlgStr = "InterceptorsAlg";
            string srnumStr = "SRoutersNumber";
            string srsegNumStr = "SRoutersSegmentNumber";
            string segPrefixStr = "Seg#";
            string srRandStr = "RandomScatteredSRoutersNumberFraction";
            
            Func<int,string> srnumFracStr = (int i)=> { return segPrefixStr + i.ToString() + "SRoutersNumberFraction"; };
            Func<int, string> srRealStr = (int i) => { return segPrefixStr + i.ToString() + "RealRoutes"; };
            Func<int, string> srdeadStr = (int i) => { return segPrefixStr + i.ToString() + "DeadEndRoutes"; };
            Func<int, string> srDeadRealRatio = (int i) => { return segPrefixStr + i.ToString() + "DeadEndToRealRoutersNumberRatio"; };
            

            var res = new Dictionary<string, string>();

            int segNum;
            int routersNum;

            string dupeKey = "";
            var userVals = 
                Dictionaries.fromTupleList(ParsingUtils.getKeyValuePairs(ParsingUtils.clearComments(txtUserInput.Lines)), ref dupeKey);

            if (userVals == null)
            {
                MessageBox.Show("The value of:" + dupeKey + " is assigned multiple times");
                return null;
            }

            int intersAlg = tryParse(userVals, intAlgStr," [0:4]");
            switch (intersAlg)
            {
                case 0:
                    res[AppConstants.AppArgumentKeys.PURSUER_POLICY.key] =
                        typeof(AdvRoutingPursuersPolicyExhaustiveSearch).Name; break;
                case 1:
                    res[AppConstants.AppArgumentKeys.PURSUER_POLICY.key] =
                        typeof(AdvRoutingPursuersPolicyNaiveGraphSearch).Name; break;
                case 2:
                    res[AppConstants.AppArgumentKeys.PURSUER_POLICY.key] =
                        typeof(AdvRoutingPursuersPolicyUniformGraphSearch).Name; break;
                case 3:
                    res[AppConstants.AppArgumentKeys.PURSUER_POLICY.key] =
                        typeof(AdvRoutingPursuersPolicySimpleRecursiveSearch).Name; 
                    res[AdvRoutingPursuersPolicySimpleRecursiveSearchParams.ASSUME_TRANSMISSION_CONSTRAINT.key] = "non";
                    break;
                case 4:
                    res[AppConstants.AppArgumentKeys.PURSUER_POLICY.key] =
                        typeof(AdvRoutingPursuersPolicySimpleRecursiveSearch).Name;
                    res[AdvRoutingPursuersPolicySimpleRecursiveSearchParams.ASSUME_TRANSMISSION_CONSTRAINT.key] = "cont";
                    break;
                default:
                    MessageBox.Show(intAlgStr + " should be a numeric value [0:4]");
                    return null;
            }
            routersNum = tryParse(userVals, srnumStr, " >=4");
            segNum = tryParse(userVals, srsegNumStr, " >=1");

            float routersFraction = tryParseF(userVals, srRandStr, " [0,1)");

            string segNetParams = "0.1, "; // randomly scattered routers are always in distance 0.3N
            
            for (int segI = 1; segI <= segNum; ++segI)
            {
                //Real Routes Number, Dead End RouteNumber, and the Dead End To Real Routers Number Ratio
                float routersSegFraction = tryParseF(userVals, srnumFracStr(segI), " >0");
                float deadRealRatio = tryParseF(userVals, srDeadRealRatio(segI), " >0");
                float deadRoutes = tryParse(userVals, srdeadStr(segI), " >=0");
                float liveRoutes = tryParse(userVals, srRealStr(segI), " >=1");

                if(liveRoutes <1)
                {
                    MessageBox.Show(srRealStr(segI) + " must be >=1");
                    return null;
                }

                segNetParams += (deadRoutes + liveRoutes).ToString() + ","; // total routes
                segNetParams += routersSegFraction.ToString("0.00") + ","; // routers number (fraction)
                segNetParams +=  (deadRoutes/(deadRoutes+liveRoutes)).ToString("0.00") +","; // how many dead routes (ratio from all routes)
                segNetParams += deadRealRatio.ToString("0.00") + ","; // ratio between real and dead routers number
                segNetParams += "0";
                
                if (segI < segNum)
                    segNetParams += ",";

                routersFraction += routersSegFraction;
            }

            if (Math.Abs(routersFraction - 1) > 0.0001)
            {
                MessageBox.Show("values of " + srRandStr + " and all segments " + srnumFracStr(0) + " must sum to 1.0");
                return null;
            }
            res[AppConstants.Policies.AdvRoutingSegmentedRouteRouterPolicy.STRATEGY_CODE.key] =
                segNetParams;
            return res;
        }
        Dictionary<string, string> getRawParamTextBox()
        {
            List<ArgEntry> entries = new List<ArgEntry>();
            Dictionary<string, string> processParams = new Dictionary<string, string>();
            List<string> paramLines = new List<string>();
            List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();

            try
            {
                pairs = ParsingUtils.getKeyValuePairs(ParsingUtils.clearComments(txtParams.Lines));
            }
            catch (Exception) { }


            Dictionary<string, string> overridingPAirs = null;
            try
            {
                overridingPAirs = getOverridingInputStrings();
            }
            catch (Exception) { }
            if (overridingPAirs == null)
                return null;

            if(chkGUI.Checked == false)
            {
                int runCount;
                if(!int.TryParse(txtRep.Text,out runCount))
                {
                    MessageBox.Show("Repetition Number must be integer >= 1");
                    return null;
                }
                overridingPAirs[AppConstants.AppArgumentKeys.SIMULATION_REPETETION_COUNT.key]=
                    runCount.ToString();
            }
            // override vals of 'pairs':
            foreach (var op in overridingPAirs)
                for (int i = 0; i < pairs.Count; ++i)
                    if (pairs[i].Item1 == op.Key)
                        pairs[i] = Tuple.Create(op.Key,op.Value);
            
            foreach (var p in pairs)
            {
                if (processParams.ContainsKey(p.Item1))
                {
                    processParams = null;
                    MessageBox.Show("multiple values for the key:" + p.Item1);
                    return null;
                }
                processParams[p.Item1] = p.Item2;
            }
            return processParams;
        }
        ProcessArgumentList getProcessArgumentList()
        {
            var processParams = getRawParamTextBox();

            ProcessArgumentList res = null;
            try
            {
                res = new ArgFile(processParams).processParams;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Invalid values");
            }
            return res;
        }

        /// <summary>
        /// utility function
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private string tryGetTextBoxParam(ArgEntry e)
        {
            var vals = getRawParamTextBox();

            var valList = ParsingUtils.separateCSV(vals[e.key]);
            if (valList.Count > 1)
                return valList[0];
            return e.tryRead(vals);
        }

        private void startGame(ProcessArgumentList processParams)
        {
            if (chkGUI.Checked)
            {
                if (processParams.ValuesCount > 1)
                {
                    if (MessageBox.Show("the key: " + processParams.VaryingValueKey + " has multiple values. Run GUI with value:' " + processParams[0][processParams.VaryingValueKey] + "' ?", "Continue?", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        return;
                }
                runGUI(processParams[0]);
            }
            else
            {
                if (processThread != null)
                {
                    processThread.Join();
                    while (processThread.IsAlive) ;
                }

                processThread = new Thread(new ThreadStart(() =>
                {
                    processNoGUI(processParams);
                }));

               
                
                processThread.Start();
            }
        }
        private void btnStartGame_Click(object sender, EventArgs e)
        {
           
        }


        private void processNoGUI(ProcessArgumentList processParams)
        {
            AGameGraph g = new EmptyEnvironment();

            ParallelOptions noParallelization = new ParallelOptions();
            noParallelization.MaxDegreeOfParallelism = 1;

            ParallelOptions po = new ParallelOptions();

            try
            {
                po.MaxDegreeOfParallelism = Int32.Parse(
                    AppConstants.AppArgumentKeys.THREAD_COUNT.tryRead(processParams[0]));
            }
            catch (Exception)
            {
                MessageBox.Show("can't parse key:" + AppConstants.AppArgumentKeys.THREAD_COUNT.key);
                return;
            }

            List<ProcessOutput> res = new List<ProcessOutput>();

            res = AlgorithmUtils.getRepeatingValueList<ProcessOutput>(null, processParams.ValuesCount);
            
            // FIXME: we use the same parallel options at several places, and maybe
            // this means 2 threads becomes 2*2 or even 8 threads
            Parallel.For(0, processParams.ValuesCount, po, i =>
            {
                try
                {
                    res[i] = SimProcess.processParams(po, processParams[i], g);
                        
                }
                catch (Exception ex)
                {
                    AppSettings.handleGameException(ex);
                }
            });


            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => {

                    txtOutput.Clear();
                    txtOutput.Text = getResultText("sim. results output", res, (ProcessOutput r) => { return r.processOutput; });

                }));
            }
            else
            {
                txtOutput.Clear();
                txtOutput.Text = getResultText("sim. results output", res, (ProcessOutput r) => { return r.processOutput; });
            }
            MessageBox.Show("Average Outcome:" + res.Last().processOutput[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY].ToString());
        }
        private string getResultText(string resultsHeader, List<ProcessOutput> results, Func<ProcessOutput, Dictionary<string, string>> resultsGetter)
        {
            string res = "***" + resultsHeader + "***" + Environment.NewLine;

            if (results == null)
                return res;


            List<Dictionary<string, string>> vals = new List<Dictionary<string, string>>();
            foreach (var l in results)
                vals.Add(resultsGetter(l));

            var tableLines = ParsingUtils.ToTable(vals);
            foreach (var tableLine in tableLines)
            {
                res += tableLine + Environment.NewLine;
            }

            return res + Environment.NewLine;
        }
        private void populateOutputTable(DataGridView target, List<Dictionary<string, string>> tableLines)
        {
            target.RowCount = tableLines.Count + 1;
            target.ColumnCount = tableLines[0].Count;

            int colI = 0;
            foreach (var column in tableLines[0])
            {
                target[colI, 0].Value = column.Key;

                if (column.Key == AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY)
                {
                    var s = new DataGridViewCellStyle();
                    s.BackColor = Color.Red;
                    target[colI, 0].Style = s;
                }

                for (int rowI = 0; rowI < tableLines.Count; ++rowI)
                    target[colI, rowI + 1].Value = tableLines[rowI][column.Key];

                ++colI;
            }
        }

        void updateActiveGameType(Dictionary<string, string> vals)
        {
            if (vals == null)
                return;

            string newGameType = AppConstants.AppArgumentKeys.GAME_MODEL.tryRead(vals);

            if (gameProcessType == null || newGameType != gameProcessType.Name)
            {
                var tmpGameProcess = ReflectionUtils.constructEmptyCtorType<AGameProcess>(newGameType);
                gameProcessType = tmpGameProcess.GetType();
                gameParamsType = tmpGameProcess.constructGameParams().GetType();

                
            }
        }

        private void btnStartGame_Click_1(object sender, EventArgs e)
        {
            var processParams = getProcessArgumentList();
            if (processParams == null)
            {
                return;
            }
            var colliding = SimProcess.getCollidingKeys(processParams[0]);
            if (colliding.Count > 0)
            {
                string allKeys = "";
                foreach (string s in colliding)
                    allKeys += s + " , ";
                if (MessageBox.Show("some keys ( " + allKeys + ") override the values the optimizer will set. Continue anyway?", "Continue?", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;
            }

            try
            {
                startGame(processParams);
            }
            catch (Exception ex)
            {
                AppSettings.handleGameException(ex);
            }
        }

        List<string> initialParams;
        private void btnResetParams_Click(object sender, EventArgs e)
        {
            txtUserInput.Lines = initialParams.ToArray();
        }

        private void chkGUI_CheckedChanged(object sender, EventArgs e)
        {
            txtRep.Enabled = !chkGUI.Checked;
        }

        private void txtRep_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
