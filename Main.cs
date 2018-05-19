using GoE.GameLogic;
using GoE.Policies;
using GoE.UI;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.Utils.Extensions;
using GoE.Utils.Algorithms;
using static GoE.Program;
using GoE.AppConstants;
using GoE.AdvRouting;
using GoE.WSN;

namespace GoE
{
    public partial class frmMain : Form
    {
        Type gameProcessType;
        Type gameParamsType;
        Dictionary<string, string> inputArgs = null;
        Thread processThread = null;

        string selectedPursuersPolicy = "";
        string selectedEvadersPolicy = "";

        private void populatePolicies()
        {
            // FIXME generalize
            cmbPursuerPolicy.Items.Clear();
            cmbEvaderPolicy.Items.Clear();
            cmbOptimizer.Items.Clear();
            if (gameProcessType == typeof(GoEGameProcess))
            {
                // add pursuers policies
                foreach (var t in AGoEPursuersPolicy.ChildrenByTypename)
                    cmbPursuerPolicy.Items.Add(t.Key);
                // add evaders policies
                foreach (var t in AGoEEvadersPolicy.ChildrenByTypename)
                    cmbEvaderPolicy.Items.Add(t.Key);
            }
            else if(gameProcessType == typeof(IntrusionGameProcess) )
            {
                // add pursuers policies
                foreach (var t in AIntrusionPursuersPolicy.ChildrenByTypename)
                    cmbPursuerPolicy.Items.Add(t.Key);
                // add evaders policies
                foreach (var t in AIntrusionEvadersPolicy.ChildrenByTypename)
                    cmbEvaderPolicy.Items.Add(t.Key);

                var appValues = ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.GameLogic.IntrusionGameParamsValueNames));
                foreach (var v in appValues)
                    setParamTextBoxKeyValue(v.key, v.val, false);
            }
            else if(gameProcessType == typeof(FrontsGridRoutingGameProcess))
            {
                // add pursuers policies
                foreach (var t in AFrontsGridRoutingPursuersPolicy.ChildrenByTypename)
                    cmbPursuerPolicy.Items.Add(t.Key);
                // add evaders policies
                foreach (var t in AFrontsGridRoutingEvadersPolicy.ChildrenByTypename)
                    cmbEvaderPolicy.Items.Add(t.Key);

                var appValues = ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.GameLogic.FrontsGridRoutingGameParamsValueNames));
                foreach (var v in appValues)
                    setParamTextBoxKeyValue(v.key, v.val, false);
            }
            else if(gameProcessType == typeof(AdvRoutingGameProcess))
            {
                // add pursuers policies
                Type pt = ReflectionUtils.constructEmptyCtorTypeFromTid<AdvRoutingGameProcess>(gameProcessType).getPursuerPolicyBaseType();
                foreach(var t in ReflectionUtils.getTypeList(pt))
                    cmbPursuerPolicy.Items.Add(t.Key);

                // add evaders policies
                pt = ReflectionUtils.constructEmptyCtorTypeFromTid<AdvRoutingGameProcess>(gameProcessType).getRouterPolicyBaseType();
                foreach (var t in ReflectionUtils.getTypeList(pt))
                    cmbEvaderPolicy.Items.Add(t.Key);
                

                var appValues = ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.GameLogic.AdvRoutingGameParamsValueNames));
                foreach (var v in appValues)
                    setParamTextBoxKeyValue(v.key, v.val, false);                
            }
            else if(gameProcessType == typeof(WSNGameProcess))
            {
                // add pursuers policies
                Type pt = ReflectionUtils.constructEmptyCtorTypeFromTid<WSNGameProcess>(gameProcessType).getPursuerPolicyBaseType();
                foreach (var t in ReflectionUtils.getTypeList(pt))
                    cmbPursuerPolicy.Items.Add(t.Key);

                // add evaders policies
                pt = ReflectionUtils.constructEmptyCtorTypeFromTid<WSNGameProcess>(gameProcessType).getRouterPolicyBaseType();
                foreach (var t in ReflectionUtils.getTypeList(pt))
                    cmbEvaderPolicy.Items.Add(t.Key);
                
                var appValues = ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.GameLogic.WSNGameParamsValueNames));
                foreach (var v in appValues)
                    setParamTextBoxKeyValue(v.key, v.val, false);
            }

            foreach (var t in APolicyOptimizer.ChildrenByTypename)
                cmbOptimizer.Items.Add(t.Key);

            cmbPursuerPolicy.SelectedIndex = -1;
            cmbEvaderPolicy.SelectedIndex = -1;
            cmbOptimizer.SelectedIndex = -1;
        }

        void runTests()
        {
            //Tests.writePOMDP();
            //Tests.testDistributions();
            //if (!GameLogicUtilityTests.runTests)
            //  return;

            //Tests.testDirectedStayingChainState();
            //Tests.testSimpleMovement();
            //Tests.testTransitions();


            //Tests.getUniqeTimeLocationsCount();
            //Tests.checkOptimizersSainity(getClearParamTextBox());

        }
        public frmMain(SimArguments args)
        {
            InitializeComponent();
            
            foreach (var gameProcessType in ReflectionUtils.getTypeList<AGameProcess>())
                cmbGameType.Items.Add(gameProcessType.Key);

            // for some reason, the event handlers below keep being removed by the editor, so they are added
            // AFTER InitializeComponent():
            cmbEvaderPolicy.SelectedIndexChanged += cmbEvaderPolicy_SelectedIndexChanged;
            cmbPursuerPolicy.SelectedIndexChanged += cmbPursuerPolicy_SelectedIndexChanged;
            cmbOptimizer.SelectedIndexChanged += cmbOptimizer_SelectedIndexChanged;
            cmbGameType.SelectedIndexChanged += CmbGameType_SelectedIndexChanged;
            txtParams.TextChanged += txtParams_TextChanged;
            lstLog.KeyDown += LstLog_KeyDown;
            //txtParams.KeyDown += textBoxes_KeyDown;
            //txtOutput.KeyDown += textBoxes_KeyDown;
            tblOutput.KeyDown += TblOutput_KeyDown;

            loadSettings(); // calls populatePolicies(), which implicitly invokes saveSettings, so loadSettings() must come before everything else
        }

        private void LstLog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.C)
            {
                string selectedText = "";
                if(lstLog.SelectedItem != null)
                    selectedText += lstLog.SelectedItem.ToString() + Environment.NewLine;

                if (lstLog.SelectedItems.Count >= 1)
                {
                    selectedText = "";
                    foreach (var s in lstLog.SelectedItems)
                        selectedText += s.ToString() + Environment.NewLine;
                    Clipboard.SetText(selectedText);
                }
            }
        }

        private void TblOutput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.V))
            {
                if (sender != null)
                {
                    try
                    {
                        populateOutputTable((DataGridView)sender, ParsingUtils.readTable(ParsingUtils.splitToLines(Clipboard.GetText()).ToArray()));
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("can't parse table from clipboard text:" + ex.Message);
                        tblOutput.RowCount = 0;
                    }
                }
                e.Handled = true;
            }
        }

        private void CmbGameType_SelectedIndexChanged(object sender, EventArgs e)
        {
            replaceInputParamKeys((AGameProcess p) => { return new List<ArgEntry>(); },
                                 AppConstants.AppArgumentKeys.GAME_MODEL,
                                 cmbGameType);

           
            // the text changes and should automatically change currenct game process, but may fail if text can't be parsed, so just in case:
            try
            {

                var prevGameProcess = gameProcessType;

                Dictionary<string, string> gameModelVal = new Dictionary<string, string>();
                string selectedGM = (string)cmbGameType.SelectedItem;
                if (selectedGM != "")
                {
                    gameModelVal[AppConstants.AppArgumentKeys.GAME_MODEL.key] = selectedGM;
                    updateActiveGameType(gameModelVal);
                }

                // replace game type parameters in settings box:
                try
                {
                    var tmpPrevGameProc = (AGameProcess)prevGameProcess.GetConstructor(new Type[] { }).Invoke(new object[] { });
                    var tmpPrevGameParam = tmpPrevGameProc.constructGameParams();
                    foreach (var v in tmpPrevGameParam.toValueMap())
                        removeParamTextBoxKeyValue(v.Key);
                }
                catch (Exception) { }
                var tmpGameProcess = ReflectionUtils.constructEmptyCtorType<AGameProcess>(selectedGM);
                var tmpGM = tmpGameProcess.constructGameParams();
                foreach (var v in tmpGM.toValueMap())
                    setParamTextBoxKeyValue(v.Key, v.Value, true);
                
            }
            catch (Exception) { }
        }

        /// <summary>
        /// invokes selected game's param-file editor form, then sets the new filepath in process params textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenParam_Click(object sender, EventArgs e)
        {
            var vals = getClearParamTextBox();
            string dialogRes;
            string currentParamPath = AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(vals);

            if (gameParamsType == typeof(GoEGameParams))
            {
                frmGameParamEditor paramLoader = new frmGameParamEditor(currentParamPath);
                paramLoader.ShowDialog();
                dialogRes = paramLoader.ParamFilePath;
            }
            else if(gameParamsType == typeof(IntrusionGameParams))
            {
                frmIntrusionGameParamEditor paramLoader = new frmIntrusionGameParamEditor(currentParamPath);
                paramLoader.ShowDialog();
                dialogRes = paramLoader.ParamFilePath;
            }
            else if(gameParamsType == typeof(FrontsGridRoutingGameParams))
            {
                frmRoutingGameParamEditor paramLoader = new frmRoutingGameParamEditor(currentParamPath);
                paramLoader.ShowDialog();
                dialogRes = paramLoader.ParamFilePath;
            }
            else
            {
                IGameParams gp = (IGameParams)gameParamsType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                dialogRes = gp.generateFileShowDialog();
            }


            if (dialogRes != null)
                setParamTextBoxKeyValue(AppConstants.AppArgumentKeys.PARAM_FILE_PATH.key,
                                        dialogRes);
        }

        /// <summary>
        /// sets a chosen filepath (via OpenFileDialog) in process params textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadParam_Click(object sender, EventArgs e)
        {

            OpenFileDialog d = new OpenFileDialog();

            if (gameParamsType == typeof(GoEGameParams))
                d.Filter = "Game Param Files (*." + AppConstants.FileExtensions.PARAM + ")|*." + AppConstants.FileExtensions.PARAM;
            else if(gameParamsType == typeof(IntrusionGameParams))
                d.Filter = "Game Param Files (*." + AppConstants.FileExtensions.INTRUSION_PARAM + ")|*." + AppConstants.FileExtensions.INTRUSION_PARAM;
            else
                d.Filter = "Game Param Files (*." + FileExtensions.ROUTING_PARAM + ")|*." + FileExtensions.ROUTING_PARAM;
            
            try
            {
                var vals = getClearParamTextBox();
                string currentParamPath = AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(vals);
                d.InitialDirectory = currentParamPath;
            }
            catch (Exception) { }

            try
            {

                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;


                if (Directory.Exists(d.FileName))
                    setParamTextBoxKeyValue(AppConstants.AppArgumentKeys.PARAM_FILE_PATH.key, d.FileName);
                else
                {
                    var newArgs = FileUtils.readValueMap(d.FileName);
                    foreach (var a in newArgs)
                        setParamTextBoxKeyValue(a.Key, a.Value, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        /// <summary>
        /// opens the graph editor form, then sets the new file in process params textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenGraph_Click(object sender, EventArgs e)
        {
            frmGridGraphEditor graphLoader = new frmGridGraphEditor();
            graphLoader.ShowDialog();
            if (graphLoader.GraphPath != null)
                setParamTextBoxKeyValue(AppConstants.AppArgumentKeys.GRAPH_FILE.key, graphLoader.GraphPath);
        }

        private void btnLoadGraph_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Graph Files (*.ggrp)|*.ggrp";
            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                setParamTextBoxKeyValue(AppConstants.AppArgumentKeys.GRAPH_FILE.key, d.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void runGUIDefault(Dictionary<string, string> processParams, AGameGraph g)
        {
            Dictionary<string, string> policyInput = new Dictionary<string, string>(processParams);
            clearEmptyEntries(policyInput);

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
            catch(Exception ex)
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
            catch(Exception ex)
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
                clearEmptyEntries(policyInput);
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
                AGameGraph g;
                if (AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(processParams) == AppConstants.AppArgumentKeys.GRAPH_FILE.val)
                    g = new EmptyEnvironment();
                else
                    g= AGameGraph.loadGraph(File.ReadAllLines(FileUtils.TryFindingFile(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(processParams))));
                runGUIDefault(processParams, g);
            }

            //else if(gameProcessType == typeof(IntrusionGameProcess)) // FIXME IntrusionGameProcess and RoutingGameProcess have the excat same code, only with slightly different types. generalize this ASAP
            //{
            //    GridGameGraph g =
            //        new GridGameGraph(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(processParams));

            //    Dictionary<string, string> policyInput = new Dictionary<string, string>(processParams);
            //    clearEmptyEntries(policyInput);

            //    IntrusionGameParams gp = IntrusionGameParams.getParamsFromFile(
            //        AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(policyInput), policyInput);

            //    GameLogic.IntrusionGameProcess gproc = new GameLogic.IntrusionGameProcess();
            //    gproc.initParams(gp, g);
            //    frmIntrusionGameProcessView processView = new frmIntrusionGameProcessView(g, gproc);

            //    AIntrusionEvadersPolicy chosenEvaderPolicy = (AIntrusionEvadersPolicy)
            //        AIntrusionEvadersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.EVADER_POLICY.tryRead(policyInput)].
            //        GetConstructor(new Type[] { }).Invoke(new object[] { });

            //    AIntrusionPursuersPolicy chosenPursuerPolicy = (AIntrusionPursuersPolicy)
            //        AIntrusionPursuersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(policyInput)].
            //        GetConstructor(new Type[] { }).Invoke(new object[] { });

            //    chosenPursuerPolicy.init(g, gp, processView, policyInput);
            //    chosenEvaderPolicy.init(g, gp, chosenPursuerPolicy, processView, policyInput);

            //    gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);
            //    processView.Show();
            //}
            //else
            //{
            //    Dictionary<string, string> policyInput = new Dictionary<string, string>(processParams);
            //    clearEmptyEntries(policyInput);

            //    FrontsGridRoutingGameParams gp = FrontsGridRoutingGameParams.getParamsFromFile(
            //        AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(policyInput), policyInput);

            //    GameLogic.FrontsGridRoutingGameProcess gproc = new GameLogic.FrontsGridRoutingGameProcess();
            //    gproc.initParams(gp, g);
            //    frmFrontsGridRoutingGameProcessView processView = new frmFrontsGridRoutingGameProcessView(g, gproc);

            //    AFrontsGridRoutingEvadersPolicy chosenEvaderPolicy = (AFrontsGridRoutingEvadersPolicy)
            //        AFrontsGridRoutingEvadersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.EVADER_POLICY.tryRead(policyInput)].
            //        GetConstructor(new Type[] { }).Invoke(new object[] { });

            //    AFrontsGridRoutingPursuersPolicy chosenPursuerPolicy = (AFrontsGridRoutingPursuersPolicy)
            //        AFrontsGridRoutingPursuersPolicy.ChildrenByTypename[AppConstants.AppArgumentKeys.PURSUER_POLICY.tryRead(policyInput)].
            //        GetConstructor(new Type[] { }).Invoke(new object[] { });

            //    chosenPursuerPolicy.init(g, gp, processView, policyInput);
            //    chosenEvaderPolicy.init(g, gp, chosenPursuerPolicy, processView, policyInput);

            //    gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);
            //    processView.Show();
            //}
        }

        private void clearEmptyEntries(Dictionary<string, string> policyInput)
        {
            List<string> keysToRemove = new List<string>();
            foreach (var v in policyInput)
                if (v.Value == "")
                    keysToRemove.Add(v.Key);
            foreach (var s in keysToRemove)
                policyInput.Remove(s);
        }

        //enum GameType : int
        //{
        //    Uninitialized = -1,
        //    GoE = 0,
        //    Intrusion = 1
        //}
        //static GameType gameTypeToRun = GameType.Uninitialized;

        //private void runProcess(bool silentRun = false, string argFilename = "")
        //{
        //    string filename = "";
        //    bool msgBoxResult = false; // if no output file was specified, then "reward" will be just presented in msgbox 

        //    if (!silentRun)
        //    {
        //        if (radIntrusion.Checked)
        //            gameTypeToRun = GameType.Intrusion;
        //        else
        //            gameTypeToRun = GameType.GoE;
        //    }
        //    else
        //    {
        //        if (gameTypeToRun == GameType.Uninitialized)
        //        {
        //            var gameRes = InputBox.ShowDialog("0 - GoE, 1 - Intrusion", "Choose Game Type", null);
        //            if (gameRes.First() == "1")
        //                gameTypeToRun = GameType.Intrusion;
        //            else
        //                gameTypeToRun = GameType.GoE;
        //        }
        //    }

        //    string paramFileType = "";
        //    switch (gameTypeToRun)
        //    {
        //        case GameType.GoE: 
        //            gameProcessType = typeof(GoEGameProcess);
        //            gameParamsType = typeof(GoEGameParams);
        //            paramFileType = AppConstants.FileExtensions.PARAM; break;
        //        case GameType.Intrusion: 
        //            gameProcessType = typeof(IntrusionGameProcess);
        //            gameParamsType = typeof(IntrusionGameParams);
        //            paramFileType = AppConstants.FileExtensions.INTRUSION_PARAM; break;
        //    }


        //    if (!silentRun)
        //    {

        //        string[] inputStrings = new string[]{AppConstants.AppArgumentKeys.THREAD_COUNT,
        //                                             AppConstants.AppArgumentKeys.OUTPUT_FOLDER,
        //                                             AppConstants.AppArgumentKeys.SIMULATION_REPETETION_COUNT, 
        //                                             AppConstants.AppArgumentKeys.PARAM_FILE_PATH,
        //                                             AppConstants.AppArgumentKeys.ARGS_FILE};
        //        while (true)
        //        {
        //            var res = InputBox.ShowDialog(
        //                                 inputStrings,
        //                                 "run params",
        //                                 null);//new string[] { "Args", "10", txtParamFile.Text, "" });
        //            if (res == null)
        //                return;

        //            if (res.Last().Length > 0)
        //            {
        //                for(int entryI = 0; entryI < res.Count-1; ++entryI)
        //                    if (res[entryI].Length > 0)
        //                    {
        //                        MessageBox.Show(AppConstants.AppArgumentKeys.ARGS_FILE + " field overrides all other parameters, and must be filled exclusively");
        //                        continue;
        //                    }
        //                try
        //                {
        //                    inputArgs = FileUtils.readValueMap(res.Last());
        //                    break;
        //                }
        //                catch (Exception ex)
        //                {
        //                    MessageBox.Show(ex.Message);
        //                    continue;
        //                }
        //            }
        //            else
        //            {
        //                inputArgs = new Dictionary<string, string>();
        //                inputArgs.AddRange(inputStrings, res);
        //                if (inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] == "")
        //                {
        //                    //filename = inputArgs[AppConstants.AppArgumentKeys.PARAM_FILE_PATH];
        //                    //inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] = filename.Substring(0, filename.LastIndexOf('\\'));

        //                    //filename = filename.Substring(0, filename.LastIndexOf("."));
        //                    //filename = filename.Split(new char[] { '\\' }).Last();

        //                    string paramPath = inputArgs[AppConstants.AppArgumentKeys.PARAM_FILE_PATH];
        //                    if (paramPath != "")
        //                        inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] = paramPath.Substring(0, paramPath.LastIndexOf('\\'));
        //                    else
        //                        msgBoxResult = true;
        //                }
        //                break;
        //            }
        //        }

        //        inputArgs[AppConstants.AppArgumentKeys.GRAPH_FILE] = txtGraphFile.Text;
        //        inputArgs[AppConstants.AppArgumentKeys.EVADER_POLICY] = (string)cmbEvaderPolicy.SelectedItem;
        //        inputArgs[AppConstants.AppArgumentKeys.PURSUER_POLICY] = (string)cmbPursuerPolicy.SelectedItem;
        //    }
        //    else // of if(!silentRun)
        //    {
        //        if (inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] == "")
        //            inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] = argFilename.Substring(0, argFilename.LastIndexOf("\\"));

        //        filename = argFilename.Substring(0, argFilename.LastIndexOf("."));
        //        filename = filename.Split(new char[] { '\\' }).Last();

        //    }

        //    if (filename == "")
        //    {
        //        filename = inputArgs[AppConstants.AppArgumentKeys.PARAM_FILE_PATH];
        //        if (filename.LastIndexOf(".") > 0)
        //            filename = filename.Substring(0, filename.LastIndexOf(".")).Split(new char[] { '\\' }).Last();
        //        else
        //            filename = filename.Split(new char[] { '\\' }).Last();
        //    }

        //    if (!msgBoxResult)
        //    {
        //        if (inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER].Last() == '\\')
        //            inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] =
        //                inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER].Substring(0, inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER].Length - 1);
        //    }
        //    int repetetionCount = int.Parse(inputArgs[AppConstants.AppArgumentKeys.SIMULATION_REPETETION_COUNT]);



        //    string graphFile = inputArgs[AppConstants.AppArgumentKeys.GRAPH_FILE];
        //    if (!File.Exists(graphFile))
        //        graphFile = AppConstants.PathLocations.GRAPH_FILES_FOLDER + "\\" + graphFile;
        //    GridGameGraph graph = new GridGameGraph(graphFile);

        //    string inputParams = inputArgs[AppConstants.AppArgumentKeys.PARAM_FILE_PATH];
        //    List<string> paramFiles = new List<string>();

        //    if (inputParams.EndsWith(paramFileType))
        //    {
        //        if (!File.Exists(inputParams))
        //            inputParams = AppConstants.PathLocations.PARAM_FILES_FOLDER + "\\" + inputParams;
        //        paramFiles.Add(inputParams);
        //    }
        //    else
        //    {
        //        if (!Directory.Exists(inputParams))
        //            inputParams = AppConstants.PathLocations.PARAM_FILES_FOLDER + "\\" + inputParams;

        //        paramFiles =
        //            new List<string>(Directory.GetFiles(inputParams, "*." + paramFileType));
        //    }

        //    if (paramFiles.Count == 0 && msgBoxResult)
        //        paramFiles.Add(txtParamFile.Text);



        //    Type policyOptimizerType = null;
        //    if (inputArgs.ContainsKey(AppConstants.AppArgumentKeys.POLICY_OPTIMIZER))
        //        policyOptimizerType = APolicyOptimizer.ChildrenByTypename[inputArgs[AppConstants.AppArgumentKeys.POLICY_OPTIMIZER]];



        //    progress = 0;

        //    int threadCount = 16;
        //    if (inputArgs.ContainsKey(AppConstants.AppArgumentKeys.THREAD_COUNT))
        //    {
        //        try
        //        {
        //            threadCount = int.Parse(inputArgs[AppConstants.AppArgumentKeys.THREAD_COUNT]);
        //        }
        //        catch (Exception)
        //        {
        //            throw new Exception("could not parse MaxDegreeOfParallelism in args file (must be int)");
        //        }
        //    }


        //    TextWriter log = null;

        //    try
        //    {
        //        if(!Directory.Exists("logs"))
        //            Directory.CreateDirectory("logs");

        //        StreamWriter sw = new StreamWriter("logs\\log_" + filename + ".txt");
        //        sw.AutoFlush = true;
        //        log = TextWriter.Synchronized(sw);
        //    }
        //    catch (Exception) { }



        //    if(!silentRun) // if not silent run, we give the user the opportunity to initialize the one-time parameters that apply for all runs
        //    {
        //       try
        //        { 
        //            string pp = inputArgs[AppConstants.AppArgumentKeys.PURSUER_POLICY];
        //            InitOnlyPolicyInput ii = new InitOnlyPolicyInput(silentRun, new Dictionary<string, string>());

        //            var inputStringsList = Utils.ReflectionUtils.constructEmptyCtorType<IPursuersPolicy>(pp).
        //                globalPolicyInputArgs;

        //            if (inputStringsList != null)
        //            {
        //                string[] inputStrings = inputStringsList.ToArray<string>();
        //                ii.ShowDialog(inputStrings, "global pursuer policy input", null);
        //                inputArgs.AddRange(ii.inputForNULLDefaultVals);
        //            }
        //        }
        //        catch(Exception ex)
        //        {
        //            MessageBox.Show("can't init pursuer's policy: " + ex.Message);
        //        }
        //    }

        //    ParallelOptions parallelOptOuter = new ParallelOptions();
        //    ParallelOptions parallelOptInner = new ParallelOptions();


        //    if (paramFiles.Count >= threadCount * 2)
        //    {
        //        // if outer loop may be parallelized well enough, we use only it - it's the most efficient way for parallelization, perhaps because we minimize concurrent mem. allocations
        //        parallelOptOuter.MaxDegreeOfParallelism = threadCount;
        //        parallelOptInner.MaxDegreeOfParallelism = 1;
        //    }
        //    else if(paramFiles.Count == 1)
        //    {
        //        parallelOptOuter.MaxDegreeOfParallelism = 1;
        //        parallelOptInner.MaxDegreeOfParallelism = threadCount;
        //    }
        //    else 
        //    {
        //        // in this case, we might use extra threads (might be a problem if we want to use all cpu cores except for 1)
        //        parallelOptOuter.MaxDegreeOfParallelism = 2;
        //        parallelOptInner.MaxDegreeOfParallelism = threadCount / 2;
        //    }

        //    // FIXME: document the syntax of arg files ASAP!! - 
        //    // we now break down inputArgs - all list values of the form $x,y,z$ will be broken, as if each of the separate values was inserte
        //    // currently we support only one "sepcial" argument
        //    int changingValCount = paramFiles.Count;
        //    List<string> values = new List<string>();
        //    string key = inputArgs.First().Key;
        //    foreach (var entry in inputArgs) // if no value is "special", 'key' and 'values' will just contain one arbitrary key-value pair
        //    {
        //        values = ParsingUtils.splitList(entry.Value,"$");
        //        key = entry.Key;

        //        if (values.Count > 1)
        //        {
        //            changingValCount = values.Count;
        //            break;
        //        }
        //    }



        //    List<Dictionary<string, string>> processOutput = AlgorithmUtils.getRepeatingValueList(new Dictionary<string, string>(), changingValCount);
        //    List<Dictionary<string, string>> theoryOutput = AlgorithmUtils.getRepeatingValueList(new Dictionary<string, string>(), changingValCount);
        //    List<Dictionary<string, string>> optimizerOutput = AlgorithmUtils.getRepeatingValueList(new Dictionary<string, string>(), changingValCount);

        //    //for (int valIdx = 0; valIdx < values.Count; ++valIdx )
        //    Parallel.For(0, values.Count, parallelOptOuter, valIdx =>
        //    {
        //        inputArgs[key] = values[valIdx]; // processParamFile() internally relies on inputArgs, and assumes values don't have '$$' in them

        //        Parallel.For(0, paramFiles.Count, parallelOptOuter, fileIdx =>
        //        {
        //            if (values.Count == 1)
        //                valIdx = fileIdx;
        //            processParamFile(parallelOptInner, paramFiles, fileIdx, valIdx, policyOptimizerType, log, graph, silentRun, repetetionCount, theoryOutput, processOutput, optimizerOutput);
        //        }); // Parallel.For
        //    });


        //    if (!msgBoxResult)
        //    {
        //        string processFileName = inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] + "\\" + filename +
        //                                 "_rep" + repetetionCount.ToString() + "." + AppConstants.FileExtensions.PROCESS_OUTPUT;
        //        string theoryFileName = inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] + "\\" + filename +
        //                                 "_thry" + "." + AppConstants.FileExtensions.PROCESS_OUTPUT;
        //        string optimizerFileName = inputArgs[AppConstants.AppArgumentKeys.OUTPUT_FOLDER] + "\\" + filename +
        //                                 "_optimizer" + "." + AppConstants.FileExtensions.PROCESS_OUTPUT;

        //        while (true)
        //        {
        //            try
        //            {
        //                Utils.FileUtils.writeTable(processOutput, processFileName);
        //                Utils.FileUtils.writeTable(theoryOutput, theoryFileName);
        //                Utils.FileUtils.writeTable(optimizerOutput, optimizerFileName);

        //                break;
        //            }
        //            catch (Exception ex)
        //            {
        //                if (MessageBox.Show("can't save results:" + ex.Message + ", retry?", "Error", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
        //                    break;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // if no output file was specified, just print the reward
        //        foreach(var line in processOutput)
        //        {
        //            if (line.ContainsKey(AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY))
        //                MessageBox.Show("Sim. result:" + AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY + ":" +
        //                                line[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY]);
        //        }
        //        foreach (var line in theoryOutput)
        //        {
        //            if (line.ContainsKey(AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY))
        //                MessageBox.Show("Thry. result:" + AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY + ":" +
        //                                line[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY]);
        //        }
        //    }

        //    ////if(!FileUtils.prepareFolder("processRun"))
        //    //  //  return;

        //    //List<string> paramRes;

        //    //string outputFile = "";
        //    //if (!silentRun)
        //    //{
        //    //    paramRes = InputBox.ShowDialog(
        //    //         new string[] { "Run count:", "param list file" },
        //    //         "run params",
        //    //         new string[] { "10", "" });

        //    //    outputFile = paramRes[1];
        //    //}
        //    //else
        //    //{
        //    //    // FIXME NOW: argPath may either be a param file list or a single param file
        //    //    paramRes = new List<string>();
        //    //    paramRes.Add(argRunCount.ToString());
        //    //    paramRes.Add(argParamFile);

        //    //    outputFile = argPath + "_" + argParamFile.Split('\\').Last();
        //    //}
        //    //int runCount = Int32.Parse(paramRes[0]);

        //    //// FIXME: use multithreading
        //    //List<string> paramFiles = new List<string>();
        //    //bool isSingleTest = false;


        //    //if (paramRes[1] != "")
        //    //{

        //    //    APursuersPolicy.defaults["results_path"] = outputFile;
        //    //    try
        //    //    {
        //    //        string[] r = File.ReadAllLines(paramRes[1]);
        //    //        foreach (string s in r)
        //    //            paramFiles.Add(s);
        //    //    }
        //    //    catch(Exception)
        //    //    {
        //    //        try
        //    //        {
        //    //            paramRes[1] = Directory.GetFiles(paramRes[1], "*." + AppConstants.FileExtensions.PARAM_LIST).First();
        //    //            paramFiles = new List<string>();
        //    //            string[] r = File.ReadAllLines(paramRes[1]);
        //    //            foreach (string s in r)
        //    //                paramFiles.Add(s);
        //    //        }
        //    //        catch(Exception)
        //    //        {
        //    //            if (!silentRun)
        //    //                MessageBox.Show("could not find a parameter list file in specified path");
        //    //            else
        //    //                Console.WriteLine("could not open param list in:" + paramRes[1]);
        //    //        }
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    isSingleTest = true;
        //    //    paramFiles.Add(txtParamFile.Text);

        //    //    EvadersPolicyEscapeAfterConstantTime.survivedArea = 0; // FIXME remove
        //    //    EvadersPolicyEscapeAfterConstantTime.survivedCircumference = 0;
        //    //    EvadersPolicyEscapeAfterConstantTime.enteredArea = 0;
        //    //    EvadersPolicyEscapeAfterConstantTime.enteredCircumference = 0;
        //    //}


        //    //Tuple<Tuple<int,int>,double>[,] results = new Tuple<Tuple<int,int>,double>[paramFiles.Count, runCount];

        //    //int paramFileIdx = 0;
        //    //string evaderPolicyName; 
        //    //string pursuerPolicyName;

        //    //if(!silentRun)
        //    //{
        //    //    evaderPolicyName = (string)cmbEvaderPolicy.SelectedItem;
        //    //    pursuerPolicyName = (string)cmbPursuerPolicy.SelectedItem;
        //    //}
        //    //else
        //    //{
        //    //    evaderPolicyName = argEvadersPolicy;
        //    //    pursuerPolicyName= argPursuerPolicy;
        //    //}

        //    //GridGameGraph g = null;
        //    //if(silentRun)
        //    //    g = new GridGameGraph(argGraphFile);
        //    //else
        //    //    g = new GridGameGraph(txtGraphFile.Text);

        //    //lstLog.Items.Add(DateTime.Now.ToLongTimeString() + "| process start");
        //    //int evadersCount = 0;

        //    //int prevRP = -1;
        //    //int prevPsi = -1; // try to figure if the difference between param files is in psi or in rp
        //    //bool rpChanges = false;
        //    //bool psiChanges = false;

        //    //foreach (string paramFile in paramFiles)
        //    //{
        //    //    List<float> multsToCheck = new List<float>();

        //    //    int l_escape_search_effort = 10;
        //    //    if (argsearch_l_escape_multiplier)
        //    //    {
        //    //        for (int i = 2; i <= l_escape_search_effort/2; ++i)
        //    //            multsToCheck.Add(i );

        //    //        for (int i = 1; i <= l_escape_search_effort/2; ++i)
        //    //            multsToCheck.Add(i * 1.0f / (l_escape_search_effort/2) );
        //    //    }

        //    //    lstLog.Items.Add(DateTime.Now.ToLongTimeString() + "| starting " + paramFile);

        //    //    //for (int runIdx = 0; runIdx < runCount; ++runIdx)
        //    //    GameParams gp = new GameParams(paramFile);

        //    //    if (prevRP == -1)
        //    //        prevRP = gp.r_p;
        //    //    else
        //    //        rpChanges = (gp.r_p != prevRP);


        //    //    if (prevPsi == -1)
        //    //        prevPsi = gp.A_P.Count;
        //    //    else
        //    //        psiChanges = (gp.A_P.Count != prevPsi);




        //    //    // TODO: this is a dirty fix. Right now, policies that ask GUI questions, show questions only in their first initialization (that's how
        //    //    // class InitOnlyPolicyInput works). Since we use multithreading, the GUI thread doesn't work properly, and we have to initialize the policy, once,
        //    //    // before multithreading starts. This whole intiailization and input mechanisms needs some revising
        //    //    InitOnlyPolicyInput tmpInp = new InitOnlyPolicyInput(inputArgs);
        //    //    AEvadersPolicy initEP = (AEvadersPolicy)evaderPolicies[evaderPolicyName].GetConstructor(new Type[] { }).Invoke(new object[] { });
        //    //    APursuersPolicy initPP = (APursuersPolicy)pursuerPolicies[pursuerPolicyName].GetConstructor(new Type[] { }).Invoke(new object[] { });

        //    //    try
        //    //    {
        //    //        initPP.init(g, gp, tmpInp);
        //    //    }
        //    //    catch(Exception)
        //    //    {
        //    //        // we assume this may fail because policy can't be used, and therefore evaders get infinity!
        //    //        for (int r = 0; r < runCount; ++r )
        //    //            results[paramFileIdx, r] = Tuple.Create(Tuple.Create(gp.r_p, gp.A_P.Count), double.PositiveInfinity);
        //    //        ++paramFileIdx;
        //    //        continue;
        //    //    }
        //    //    initEP.init(g, gp, tmpInp);
        //    //    initEP = null;
        //    //    initPP = null;
        //    //    evadersCount = gp.A_E.Count;

        //    //    float[] l_escapeResults = null;
        //    //    float l_escapeMult = 0;

        //    //    for (int li = 0; li <= multsToCheck.Count; ++li) // if we don't search optimal multiplier, we still run for 1 iteration
        //    //    {
        //    //        int realRunCount = runCount;

        //    //        if (argsearch_l_escape_multiplier)
        //    //        {
        //    //            if(li > 0)
        //    //            {
        //    //                float sum = 0;
        //    //                for (int j = 0; j < l_escape_search_effort; ++j)
        //    //                    sum += l_escapeResults[j];
        //    //                if(sum > chosenLEscapeMultPerformance)
        //    //                {
        //    //                    chosenLEscapeMultPerformance = sum;
        //    //                    chosenLEscapeMult = l_escapeMult;
        //    //                }
        //    //            }
        //    //            if (li == multsToCheck.Count)
        //    //            {
        //    //                // after we did all iterations, we can choose best l_escape
        //    //                l_escapeMult = chosenLEscapeMult;
        //    //            }
        //    //            else
        //    //            {
        //    //                l_escapeMult = multsToCheck[li];
        //    //                realRunCount = l_escape_search_effort;
        //    //            }

        //    //            tmpInp.setDefaultValue(AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE_MULTIPLIER, l_escapeMult.ToString());
        //    //            l_escapeResults = new float[realRunCount];
        //    //        }

        //    //        Parallel.For(0, realRunCount, runIdx =>
        //    //        //for (int runIdx = 0; runIdx < runCount; ++runIdx)
        //    //        {
        //    //            //InitOnlyPolicyInput tmpInp = new InitOnlyPolicyInput();

        //    //            //GridGameGraph g = new GridGameGraph(txtGraphFile.Text);
        //    //            //GameParams gp = new GameParams(paramFile);
        //    //            GameLogic.GameProcess gproc = new GameLogic.GameProcess(gp, g);

        //    //            AEvadersPolicy chosenEvaderPolicy = (AEvadersPolicy)
        //    //                evaderPolicies[evaderPolicyName].
        //    //                GetConstructor(new Type[] { }).Invoke(new object[] { });

        //    //            APursuersPolicy chosenPursuerPolicy = (APursuersPolicy)
        //    //                pursuerPolicies[pursuerPolicyName].
        //    //                GetConstructor(new Type[] { }).Invoke(new object[] { });

        //    //            try
        //    //            {
        //    //                chosenPursuerPolicy.init(g, gp, tmpInp);
        //    //            }
        //    //            catch (Exception ex)
        //    //            {
        //    //                results[paramFileIdx, runIdx] = Tuple.Create(Tuple.Create(gp.r_p, gp.A_P.Count), double.PositiveInfinity);
        //    //                //continue;
        //    //                return;
        //    //            }

        //    //            chosenEvaderPolicy.init(g, gp, tmpInp);
        //    //            gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);

        //    //            int i = 0;
        //    //            while (gproc.invokeNextPolicy()) ;

        //    //            if(argsearch_l_escape_multiplier && li != multsToCheck.Count)
        //    //            {
        //    //                // we are currently estimating how good l_escapeMult is
        //    //                l_escapeResults[runIdx] = (float)gproc.AccumulatedEvadersReward;
        //    //            }
        //    //            else
        //    //                results[paramFileIdx, runIdx] = Tuple.Create(Tuple.Create(gp.r_p, gp.A_P.Count), gproc.AccumulatedEvadersReward);

        //    //        }); // Parallel.For
        //    //    }

        //    //    ++paramFileIdx;
        //    //}

        //    //lstLog.Items.Add(DateTime.Now.ToLongTimeString() + "| process ended");


        //    //if (isSingleTest)
        //    //{
        //    //    double total = 0;
        //    //    foreach (var t in results)
        //    //    {
        //    //        lstLog.Items.Add("result:" + t.Item2.ToString());
        //    //        total += t.Item2;
        //    //    }
        //    //    lstLog.Items.Add("Average:" + (total / runCount).ToString());
        //    //    lstLog.Items.Add("Average Per Evader:" + (total / (runCount * evadersCount) ).ToString());

        //    //    //if (pursuerPolicyName == "EvadersPolicyEscapeAfterConstantTime") // TODO: should be rewritten and generalized
        //    //    {
        //    //        lstLog.Items.Add("p_a:" + (1 - ((float)EvadersPolicyEscapeAfterConstantTime.survivedArea / EvadersPolicyEscapeAfterConstantTime.enteredArea)).ToString());
        //    //        lstLog.Items.Add("p_c:" + (1 - ((float)EvadersPolicyEscapeAfterConstantTime.survivedCircumference / EvadersPolicyEscapeAfterConstantTime.enteredCircumference)).ToString());
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    try
        //    //    {
        //    //        Dictionary<int, double> resDictionary = new Dictionary<int, double>();

        //    //        for (int i = 0; i < paramFiles.Count; ++i)
        //    //        {
        //    //            if (rpChanges)
        //    //            {
        //    //                resDictionary[results[i, 0].Item1.Item1] = 0;
        //    //                for (int j = 0; j < runCount; ++j)
        //    //                    resDictionary[results[i, j].Item1.Item1] += results[i, j].Item2;
        //    //            }
        //    //            else
        //    //            {
        //    //                resDictionary[results[i, 0].Item1.Item2] = 0;
        //    //                for (int j = 0; j < runCount; ++j)
        //    //                    resDictionary[results[i, j].Item1.Item2] += results[i, j].Item2;
        //    //            }
        //    //        }

        //    //        List<string> outputLines = new List<string>();
        //    //        outputLines.Add("prsrs rsrcs,\tLeakedPerEvader");
        //    //        foreach (var v in resDictionary)
        //    //        {
        //    //            outputLines.Add(v.Key.ToString("0.000") + ",\t\t" + (v.Value / (runCount * evadersCount)).ToString("0.000"));
        //    //        }
        //    //        try
        //    //        {
        //    //            File.WriteAllLines(outputFile + "_" + runCount.ToString() + "runs_results.txt", outputLines.ToArray());
        //    //        }
        //    //        catch (Exception) { }
        //    //    }
        //    //    catch (Exception) { }
        //    //}

        //    //if (silentRun)
        //    //{
        //    //    this.Close();
        //    //    Application.Exit();
        //    //}

        //}


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

            foreach (var p in pairs)
            {
                if (processParams.ContainsKey(p.Item1))
                {
                    processParams = null;
                    return null;
                }
                processParams[p.Item1] = p.Item2;
            }
            return processParams;
        }


        List<string> getParamsVaryingKeys()
        {
            List<string> varyingKeys = new List<string>();
            var raw = getRawParamTextBox();
            foreach (var rawLine in raw)
            {
                if (ArgEntry.getSplitValues(rawLine.Value).Count > 0)
                    varyingKeys.Add(rawLine.Key);
            }
            return varyingKeys;
        }

        List<string> getParamsVaryingValues(string key)
        {
            var raw = getRawParamTextBox();
            foreach (var rawLine in raw)
                if (rawLine.Key == key)
                    return ArgEntry.getSplitValues(rawLine.Value);
            return new List<string>();
        }

        /// <summary>
        /// "open" for each multi-variable in getRawParamTextBox(),= i.e.
        /// one List<string> for every combination (cartesian product) of the multi-variable lines in getRawParamTextBox
        /// 
        /// </summary>
        /// <param name="separateByKey">
        /// results are separated for different values under this key
        /// </param>
        /// <returns></returns>
        //Dictionary<string,List<List<string>>> getExpandedParams(string separateByKey)
        //{
        //    // for each value (of 'separateByKey' key), hold a List<string> per param combination
        //    Dictionary<string, List<List<string>>> expandedParams = new Dictionary<string, List<List<string>>>();
        //    var raw = getRawParamTextBox();

        //    var separatingValues = getParamsVaryingValues(separateByKey);

        //    foreach(var sv in separatingValues)
        //    {
        //        expandedParams[sv] = new List<List<string>>();
        //        expandedParams[sv].Add(new List<string>());
        //        expandedParams[sv].Last().Add(ParsingUtils.serializeKeyValue(separateByKey, sv));
        //    }

        //    foreach (var rawLine in raw)
        //    {
        //        if (rawLine.Key == separateByKey)
        //            continue; // separation already done before this loop

        //        var vals = ArgEntry.getSplitValues(rawLine.Value);
        //        if (vals.Count == 0)
        //            vals.Add(rawLine.Value);

        //        Dictionary<string, List<List<string>>> newExpandedParams = new Dictionary<string, List<List<string>>>();

        //        foreach (var p in expandedParams)
        //        {
        //            newExpandedParams[p.Key] = new List<List<string>>();
        //            foreach (var expandedLineVal in vals)
        //            {
        //                foreach (var partialParamFile in p.Value)
        //                {
        //                    newExpandedParams[p.Key].Add(new List<string>(partialParamFile));
        //                    newExpandedParams[p.Key].Last().Add(ParsingUtils.serializeKeyValue(rawLine.Key, expandedLineVal));
        //                }
        //            }
        //        }
        //        expandedParams = newExpandedParams;
        //    }

        //    return expandedParams;
        //}


        List<List<string>> getExpandedParams(string keyToIgnore)
        {
            // each param has a different combination of values
            List<List<string>> expandedParams = new List<List<string>>();

            var raw = getRawParamTextBox();

            expandedParams.Add(new List<string>()); // each time we add variable, we add it to each of the previous values, so there has to be at least 1 previous value
            foreach (var rawLine in raw)
            {
                if (rawLine.Key == keyToIgnore)
                    continue; 

                var vals = ArgEntry.getSplitValues(rawLine.Value);
                if (vals.Count == 0)
                    vals.Add(rawLine.Value);

                List<List<string>> newExpandedParams = new List<List<string>>();

                
                newExpandedParams = new List<List<string>>();
                foreach (var expandedLineVal in vals)
                {
                    foreach (var partialParamFile in expandedParams)
                    {
                        newExpandedParams.Add(new List<string>(partialParamFile));
                        newExpandedParams.Last().Add(ParsingUtils.serializeKeyValue(rawLine.Key, expandedLineVal));
                    }
                }
                
                expandedParams = newExpandedParams;
            }

            return expandedParams;
        }

        /// <summary>
        /// makes sure no duplicate keys are in text box, 
        /// and returns a usable process Params key value map (if a key has value of "", the key is omitted)
        /// If a multi-value is used, then the first value in the series is returned
        /// </summary>
        /// <returns>
        /// null if text couldn't be parsed/duplicates found
        /// </returns>
        //ProcessArgumentList getClearParamTextBox()
        Dictionary<string,string> getClearParamTextBox()
        {
            var processParams = getRawParamTextBox();
            
            //return res;
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
            var vals = getClearParamTextBox();

            var valList = ParsingUtils.separateCSV(vals[e.key]);
            if (valList.Count > 1)
                return valList[0];
            return e.tryRead(vals);
        }
        /// <summary>
        /// removes all instances of a given key from param text box
        /// </summary>
        /// <param name="key"></param>
        void removeParamTextBoxKeyValue(string key)
        {
            List<string> lines = new List<string>(txtParams.Lines);
            for (int i = 0; i < lines.Count; ++i)
            {
                try
                {
                    if (ParsingUtils.isComment(txtParams.Lines[i]))
                        continue;
                    var keyVals = ParsingUtils.getKeyValuePairs(new string[] { ParsingUtils.clearLineSeparators(txtParams.Lines[i]) });
                    if (keyVals[0].Item1 == key)
                        lines.RemoveAt(i);
                }
                catch (Exception) { }
            }
            txtParams.Lines = lines.ToArray();
        }
        /// <summary>
        /// finds lines in the text box that specify a value for 'key', then replaces the value with
        /// the new given 'value'.
        /// if not found, adds to end of the text box the new key-value pair.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void setParamTextBoxKeyValue(string key, string value, bool forceValueChange = true)
        {
            
            bool found = false;
            List<string> lines = new List<string>(txtParams.Lines);
            for (int i = 0; i < lines.Count; ++i)
            {
                if (ParsingUtils.isComment(txtParams.Lines[i]))
                    continue;

                try
                {
                    var keyVals = ParsingUtils.getKeyValuePairs(new string[] { ParsingUtils.clearLineSeparators(txtParams.Lines[i]) });
                    if (keyVals[0].Item1 == key)
                    {
                        lines[i] = ParsingUtils.clearLineSeparators(lines[i]); // if there are newlines at the end of the line, the value won't be replaced correctly
                        lines[i] = lines[i].Remove(lines[i].Count() - keyVals[0].Item2.Count(), keyVals[0].Item2.Count());
                        lines[i] = lines[i] + value;
                        found = true;
                    }
                }
                catch (Exception) { }
            }

            if (!found)
                txtParams.Text += Environment.NewLine + ParsingUtils.serializeKeyValue(key, value);
            else if (forceValueChange)
                txtParams.Lines = lines.ToArray();


            saveSettings();
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

                    if (this.InvokeRequired)
                        this.Invoke(new Action(() =>
                        {
                            tabControlProcess.SelectedTab = tabPageOut;
                            tabPageOut.Focus();
                        }));
                    else
                    {
                        tabControlProcess.SelectedTab = tabPageOut;
                        tabPageOut.Focus();
                    }
                }));

                AppSettings.setLogWriteFunc((s) =>
                {

                    if (this.InvokeRequired)
                        this.Invoke(new Action(() =>
                        {
                            lstLog.Items.Add(s);
                        }));
                    else
                        lstLog.Items.Add(s);
                }, true);
                AppSettings.WriteLogLine("***starting process***" + DateTime.Now.ToShortTimeString());
                processThread.Start();
            }
        }
        private void btnStartGame_Click(object sender, EventArgs e)
        {
            var processParams = getProcessArgumentList();
            if (processParams == null)
            {
                MessageBox.Show("multiple values for the same key(s) in process params textbox, or invalid format");
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

            #if !EXTENSIVE_TRYCATCH
            if (!chkExceptions.Checked) // try/catch make everything slower. use only if necessary
                startGame(processParams);           
            else
            #endif
            {
                try
                {
                    startGame(processParams);
                    // FIXME remove:
                    //Exception ex1 = new Exception("1");
                    //Exception ex2 = new Exception("2");
                    //throw new AggregateException("frr", new List<Exception> { ex1, ex2 });
                }
                catch(Exception ex)
                {
                    AppSettings.handleGameException(ex);
                }
            }
          
            
        }

        
        private void processNoGUI(ProcessArgumentList processParams)
        {
            AGameGraph g;
            
            try
            {
                if (AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(processParams[0]) == AppConstants.AppArgumentKeys.GRAPH_FILE.val)
                    g = new EmptyEnvironment();
                else
                    g = AGameGraph.loadGraph(File.ReadAllLines(FileUtils.TryFindingFile(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(processParams[0]))));
            }
            catch (Exception)
            {
                MessageBox.Show("can't load graph file");
                return;
            }

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

#if EXTENSIVE_TRYCATCH
            for(int i = 0; i < processParams.ValuesCount; ++i)
                res[i] = SimProcess.processParams(po, processParams[i], g);
#else
            // FIXME: we use the same parallel options at several places, and maybe
            // this means 2 threads becomes 2*2 or even 8 threads
            Parallel.For(0, processParams.ValuesCount, po, i =>
            {
                if (!chkExceptions.Checked)
                {
                    res[i] = SimProcess.processParams(po, processParams[i], g);
                    
                    if(processParams.ValuesCount > 1)
                        AppSettings.WriteLogLine("done processing val" + 
                            processParams[i][processParams.VaryingValueKey]);
                }
                else
                {
                    try
                    {
                        res[i] = SimProcess.processParams(po, processParams[i], g);
                        if (processParams.ValuesCount > 1)
                            AppSettings.WriteLogLine("done processing val" +
                                processParams[i][processParams.VaryingValueKey]);
                    }
                    catch (Exception ex)
                    {
                        AppSettings.handleGameException(ex);
                    }
                }
            });
#endif

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(()=> {

                    txtOutput.Clear();
                    txtOutput.Text = getResultText("Optimizer output", res, (ProcessOutput r) => { return r.optimizerOutput; }) +
                                     getResultText("Theoretical results output", res, (ProcessOutput r) => { return r.theoryOutput; }) +
                                     getResultText("simu. results output", res, (ProcessOutput r) => { return r.processOutput; });

                }));
            }
            else
            {
                txtOutput.Clear();
                txtOutput.Text = getResultText("Optimizer output", res, (ProcessOutput r) => { return r.optimizerOutput; }) +
                                 getResultText("Theoretical results output", res, (ProcessOutput r) => { return r.theoryOutput; }) +
                                 getResultText("simu. results output", res, (ProcessOutput r) => { return r.processOutput; });


            }

        }
        private string getResultText(string resultsHeader, List<ProcessOutput> results, Func<ProcessOutput, Dictionary<string, string>> resultsGetter)
        {
            string res = "***" + resultsHeader + "***" + Environment.NewLine;

            if (results == null)
                return res;


            List<Dictionary<string, string>> vals = new List<Dictionary<string, string>>();
            foreach(var l in results)
                vals.Add( resultsGetter(l));

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

                for (int rowI = 0; rowI < tableLines.Count;++rowI)
                    target[colI, rowI+1].Value = tableLines[rowI][column.Key];
                
                ++colI;
            }
        }

        string latestLegalTxtParams = "";
        private void saveSettings()
        {
            if (saveLoadProtection)
                return;

            saveLoadProtection = true;

            try
            {


                //Dictionary<string,string> vals = new Dictionary<string,string>();
                //vals[AppConstants.GUIDefaults.GRAPH_FILE] = txtGraphFile.Text;
                //vals[AppConstants.GUIDefaults.PARAM_FILE] = txtParamFile.Text;
                //vals[AppConstants.GUIDefaults.EVADERS_POLICY] = (string)cmbEvaderPolicy.SelectedItem;
                //vals[AppConstants.GUIDefaults.PURSUERS_POLICY] = (string)cmbPursuerPolicy.SelectedItem;
                //vals[AppConstants.GUIDefaults.RAD_GOE] = Convert.ToInt32(radGoE.Checked).ToString();
                //vals[AppConstants.GUIDefaults.RAD_INT] = Convert.ToInt32(radIntrusion.Checked).ToString();
                //Utils.FileUtils.updateValueMap(vals, AppConstants.PathLocations.GUI_DEFAULTS_VALUEMAP_FILE);
                AppSettings.SaveMultilineString(AppConstants.GUIDefaults.PROCESS_PARAMS.key, txtParams.Text);
                AppSettings.SaveMultilineString(AppConstants.GUIDefaults.LATEST_LEGAL_PROCESS_PARAMS.key, latestLegalTxtParams);

            }
            catch (Exception) { }
            saveLoadProtection = false;
        }

        bool saveLoadProtection = false;
        void loadSettings()
        {
            if (saveLoadProtection)
                return;
            saveLoadProtection = true;

            // try loading a previously used parameter set
            try
            {
                txtParams.Text =
                    AppSettings.LoadMultilineString(AppConstants.GUIDefaults.PROCESS_PARAMS.key);
            }
            catch (Exception) { }

            Dictionary<string, string> vals = new Dictionary<string, string>();

            try
            {
                vals = ParsingUtils.parseValueMap(ParsingUtils.clearComments(txtParams.Lines));
            }
            catch (Exception) { }

            //Dictionary<string, string> vals = Utils.FileUtils.readValueMap(AppConstants.PathLocations.GUI_DEFAULTS_VALUEMAP_FILE);
            //txtGraphFile.Text = vals[AppConstants.GUIDefaults.GRAPH_FILE];
            //txtParamFile.Text = vals[AppConstants.GUIDefaults.PARAM_FILE];
            //selectedPursuersPolicy = vals[AppConstants.GUIDefaults.PURSUERS_POLICY];
            //selectedEvadersPolicy = vals[AppConstants.GUIDefaults.EVADERS_POLICY];
            //cmbPursuerPolicy.SelectedIndex = cmbPursuerPolicy.Items.IndexOf(vals[AppConstants.GUIDefaults.PURSUERS_POLICY]);
            //cmbEvaderPolicy.SelectedIndex = cmbEvaderPolicy.Items.IndexOf(vals[AppConstants.GUIDefaults.EVADERS_POLICY]);
            
            var appValues = ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AppConstants.AppArgumentKeys));
            foreach (var v in appValues)
                setParamTextBoxKeyValue(v.key, v.val, false);
            removeParamTextBoxKeyValue(AppConstants.AppArgumentKeys.OUTPUT_FOLDER.key); // since output is redirected to output console
            
            updateActiveGameType(vals);
            populatePolicies(); // before changing the comboboxes selected index, we need to make sure the loaded value is avilable for selection
            
            saveLoadProtection = false;
        }


        Thread paramTxtChecker;
        private void frmMain_Load(object sender, EventArgs e)
        {
            paramTxtChecker = new Thread(new ThreadStart(() =>
            {
                while (!this.IsDisposed)
                {
                    var processParams = getRawParamTextBox();

                    // warn if there's an error in text:
                    try
                    {
                        ProcessArgumentList res = new ArgFile(processParams).processParams;
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() => { try { lstLog.Items.Add(ex.Message); } catch (Exception) { } }));
                    }
                    Thread.Sleep(5000);
                }

            }));
        }


        private void replaceInputParamKeys<T>(Func<T, List<ArgEntry>> keysGetter,
            AppConstants.ArgEntry inputAskerKey,
            ComboBox newInputAsker)
        {
            if (saveLoadProtection)
                return;

            var vals = getClearParamTextBox();
            string currentAsker = inputAskerKey.tryRead(vals);
            string newAsker = (string)newInputAsker.SelectedItem;
            List<ArgEntry> keysToRemove = new List<ArgEntry>();

            try
            {
                if (currentAsker != "")
                    keysToRemove = keysGetter(ReflectionUtils.constructEmptyCtorType<T>(currentAsker));
            }
            catch (Exception) { /*if previous type's name has changed, this could throw an exception*/}


            List<ArgEntry> keysToAdd = new List<ArgEntry>();
            if (newAsker != null && newAsker != "")
                keysToAdd = keysGetter(ReflectionUtils.constructEmptyCtorType<T>(newAsker));

            foreach (var s in keysToRemove)
                removeParamTextBoxKeyValue(s.key);
            foreach (var s in keysToAdd)
                setParamTextBoxKeyValue(s.key, s.val);

            setParamTextBoxKeyValue(inputAskerKey.key, newAsker);

            newInputAsker.SelectedItem = 0;

        }

        private void cmbPursuerPolicy_SelectedIndexChanged(object sender, EventArgs e)
        {
            replaceInputParamKeys((APursuersPolicy p) => p.policyInputKeys(),
                                  AppConstants.AppArgumentKeys.PURSUER_POLICY,
                                  cmbPursuerPolicy);
        }


        private void btnUtils_Click(object sender, EventArgs e)
        {
            new frmUtils().Show();
        }

        private void cmdGeneticPoliciesManager_Click(object sender, EventArgs e)
        {
            try
            {
                var vals = getClearParamTextBox();
                GridGameGraph g = new GridGameGraph(AppConstants.AppArgumentKeys.GRAPH_FILE.tryRead(vals));
                GoEGameParams gp = GoEGameParams.getParamsFromFile(AppConstants.AppArgumentKeys.PARAM_FILE_PATH.tryRead(vals), null);
                new GoE.GameLogic.frmGeneticPoliciesManager(g, gp).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cmbEvaderPolicy_SelectedIndexChanged(object sender, EventArgs e)
        {
            replaceInputParamKeys((IEvadersPolicy p) => p.policyInputKeys,
                                  AppConstants.AppArgumentKeys.EVADER_POLICY,
                                  cmbEvaderPolicy);
        }

        private void pnlButtons_Paint(object sender, PaintEventArgs e)
        {

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

                populatePolicies();
            }
        }
        
        private void txtParams_TextChanged(object sender, EventArgs e)
        {
            var vals = getClearParamTextBox();
            if (vals != null)
            {
                latestLegalTxtParams = txtParams.Text;
                updateActiveGameType(vals);
            }
            saveSettings();
        }

        private void cmbOptimizer_SelectedIndexChanged(object sender, EventArgs e)
        {
            replaceInputParamKeys((APolicyOptimizer p) => p.optimizationInputKeys,
                                 AppConstants.AppArgumentKeys.POLICY_OPTIMIZER,
                                 cmbOptimizer);
        }

        private void textBoxes_KeyDown(object sender, KeyEventArgs e)
        {
            // textbox shortcut ctrl+A doesn't work when multiline=true, so this fixes it
            if (e.Control && (e.KeyCode == Keys.A))
            {
                if (sender != null)
                    ((TextBox)sender).SelectAll();
                e.Handled = true;
            }
        }

        private void btnTests_Click(object sender, EventArgs e)
        {
            runTests();
        }

        

        private void tblOutput_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void txtOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtOutput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.C))
            {
                Clipboard.SetText(txtOutput.SelectedText);
                if (sender != null)
                {
                    try
                    {
                        populateOutputTable(tblOutput, ParsingUtils.readTable(ParsingUtils.splitToLines(txtOutput.SelectedText).ToArray()));
                        tabPageOutputTable.Focus();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("can't parse table from clipboard text:" + ex.Message);
                        try
                        {
                            tblOutput.RowCount = 0;
                        }
                        catch (Exception) { }
                    }
                }
                e.Handled = true;
            }
            else if (e.Control && (e.KeyCode == Keys.V))
            {
                
                if (sender != null)
                {
                    try
                    {
                        populateOutputTable(tblOutput, ParsingUtils.readTable(ParsingUtils.splitToLines(Clipboard.GetText()).ToArray()));
                        tabPageOutputTable.Focus();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("can't parse table from clipboard text:" + ex.Message);
                        try
                        {
                            tblOutput.RowCount = 0;
                        }
                        catch (Exception) { }
                    }
                }
                e.Handled = true;
            }
        }

        private void searchParams(string str)
        {
            try
            {
                
                var matches = txtParams.Text.ToLower().AllIndexesOf(str);

                // cancel bold text:
                txtParams.Select(0, txtParams.Text.Length);
                txtParams.SelectionFont = new Font(txtParams.Font, FontStyle.Regular);

                // bold found matches:
                foreach (var m in matches)
                {
                    txtParams.Select(m, str.Length);
                    txtParams.SelectionFont = new Font(txtParams.Font, FontStyle.Bold);
                }
            }
            catch (Exception) { }
        }

        private void btnSrchParam_Click(object sender, EventArgs e)
        {
            try
            {
                string str = InputBox.ShowDialog("Search Term", "Search Term").First();
                searchParams(str); // TODO: make this advanced search
            }
            catch (Exception) { }
        }

        private void txtParams_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                txtSearchParams.Visible = true;
                txtSearchParams.SelectAll();
                txtSearchParams.Focus();
            }
        }

        private void txtSearchParams_TextChanged(object sender, EventArgs e)
        {
            searchParams(txtSearchParams.Text);
        }

        private void txtSearchParams_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                txtSearchParams.Visible = false;
            }
        }

        private void btnGenParams_Click(object sender, EventArgs e)
        {
            List<string> varyingKeys = getParamsVaryingKeys();
            string separator = InputBox.ShowMultiChoiceDialog("Select X Axis", varyingKeys);
            if (separator == null)
                return;
            var allParms = getExpandedParams(separator);
            var batchName = InputBox.ShowDialog("Batch Name", "Batch Name", "");

            
            FolderBrowserDialog foldChoose = new FolderBrowserDialog();
            if (foldChoose.ShowDialog() != DialogResult.OK)
                return;

            string baseFolder = foldChoose.SelectedPath;
            string folderPrefix = "", batchFolderName= "";
            if (batchName != null && batchName.Count > 0)
            {
                batchFolderName = batchName.First();
                baseFolder = 
                    FileUtils.concatenateFullFilePath(foldChoose.SelectedPath, batchFolderName);
                FileUtils.prepareFolder(baseFolder);
                folderPrefix = FileUtils.concatenateFullFilePath(batchFolderName, "");
            }


            var separatingValues = getParamsVaryingValues(separator);

            // make folder for each param set combination (excluding combinations of separator),
            // and within each folder make param file for each value (under 'separator' key)
            varyingKeys.Remove(separator);
            foreach (var param in allParms)
            {
                string FolderName = "By" + separator;
                if (varyingKeys.Count > 0)
                    FolderName = getUniqueName(param, varyingKeys);

                string paramListPath  =
                    FileUtils.concatenateFullFilePath(foldChoose.SelectedPath, "paramList" + "_" + batchFolderName+"_" + FolderName + "." + FileExtensions.ARG_LIST);
                string dirPath =
                    FileUtils.concatenateFullFilePath(baseFolder, FolderName);
                FileUtils.prepareFolder(dirPath);

                List<string> paramFileList = new List<string>();
                // in the folder we keep a param file per separator value
                foreach(var sepVal in separatingValues)
                {
                    string filename = ParsingUtils.AbbreviateName(separator) + sepVal + "__"  + getUniqueName(param, varyingKeys) + "." + FileExtensions.PARAM;
                    string filePath = FileUtils.concatenateFullFilePath(dirPath, filename);
                    var finalParam = new List<string>(param);
                    finalParam.Add(ParsingUtils.serializeKeyValue(separator, sepVal));


                    var valmap = ParsingUtils.parseValueMap(finalParam.ToArray());
                    valmap[AppConstants.AppArgumentKeys.OUTPUT_FOLDER.key] = batchFolderName;
                    FileUtils.writeValueMap(valmap, filePath);
                    
                    paramFileList.Add(FileUtils.concatenateFullFilePath(folderPrefix + FolderName, filename)); // relative path of param files
                }
                File.WriteAllLines(paramListPath, paramFileList.ToArray());
            }
        }

        string getUniqueName(List<string> allParams, List<string> varyingKeys)
        {
            string res = "";
            var paramDictionary = ParsingUtils.parseValueMap(allParams.ToArray());
            foreach(var v in varyingKeys)
                res += ParsingUtils.AbbreviateName(v) + paramDictionary[v] + "_";
            return res;

        }
    }
}
