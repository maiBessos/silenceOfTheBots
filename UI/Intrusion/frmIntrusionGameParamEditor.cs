using GoE.GameLogic;
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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.UI
{
    public partial class frmIntrusionGameParamEditor : Form
    {
        // TODO: generalize this form, so we can have automated param editors (just look at the save function - it's horrible!)

        public frmIntrusionGameParamEditor(string defaultFile)
        {
            InitializeComponent();
            ParamFilePath = defaultFile;
        }
        
        private bool isDirty;
        private IntrusionGameParams param = IntrusionGameParams.getClearParams();
        private TextBox textBox2;
        private ListBox listBox1;
        private Label lblRewardArg;
        private TextBox txtRewardArg;
        private ListBox lstRewarrdDesc;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private TextBox txtRPJump;
        private Label label5;
        private Label label6;
        private TextBox txtEtaJump;
        private TextBox txtMaxEta;
        private Label label7;
        private TextBox txtRewardArgMax;
        private TextBox txtRewardArgJump;
        private Label label8;
        private Label lblrps;
        private TextBox txtrps;
        private TextBox txtResJump;
        private Label label9;
        private Label label10;
        private TextBox txtMaxRes;
        private TextBox txtRpsJump;
        private Label label11;
        private Label label12;
        private TextBox txtMaxRps;
        private TextBox txtTiJump;
        private Label label13;
        private Label label14;
        private TextBox txtMaxTi;
        private Label lblti;
        private TextBox txtti;
        private CheckBox chkSquareArea;
        private TextBox textBox1;
        //private Dictionary<string,Type> rewardFunctions = new Dictionary<string,MethodInfo>(); // populated on form load

        public string ParamFilePath { get; private set; }

        private void btnDone_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtEvadersCount_TextChanged(object sender, EventArgs e)
        {
            setParamData(); 
            isDirty = true;
        }

        private void txtPursuerCount_TextChanged(object sender, EventArgs e)
        {
            setParamData(); 
            isDirty = true;
        }

        private void txtrp_TextChanged(object sender, EventArgs e)
        {
            setParamData(); 
            isDirty = true;
        }

        private void txtre_TextChanged(object sender, EventArgs e)
        {
            setParamData();
            isDirty = true;
        }

        private void txtrs_TextChanged(object sender, EventArgs e)
        {
            
            setParamData(); 
            isDirty = true;
        }

        private void cmbRewardFunc_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            setParamData();
            isDirty = true;
        }

        private void GameParamEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(isDirty)
                switch(MessageBox.Show("Save Before Exit?", "Close",MessageBoxButtons.YesNoCancel))
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        btnSave_Click_1(sender, e); break;
                    case System.Windows.Forms.DialogResult.Cancel:
                        e.Cancel = true; break;
                }
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            foreach (object o in this.Controls)
            {
                Label l = o as Label;

                    if (l != null && l.ForeColor.ToArgb() != Color.Black.ToArgb() && l.Visible == true) // ensure all fields are "valid"
                    {
                        MessageBox.Show(l.Text + " has invalid value");
                        return;
                    }
            }
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "Intrusion Game Param Files (*." + AppConstants.FileExtensions.INTRUSION_PARAM + ")|*." + AppConstants.FileExtensions.INTRUSION_PARAM;

            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string dialogFilename = d.FileName;

                int minTI = Int32.Parse(txtti.Text);
                int minRES = Int32.Parse(txtres.Text);
                //int minRPS = Int32.Parse(txtrps.Text);
                int minRP = Int32.Parse(txtrp.Text);
                int minPsi = Int32.Parse(txtPursuerCount.Text);
                int minEta = Int32.Parse(txtEvadersCount.Text);
                //string minRewardArg = txtRewardArg.Text;

                int maxRP = minRP;
                int maxPsi = minPsi;
                int maxEta = minEta;
                int maxRES = minRES;
                //int maxRPS = minRPS;
                int maxTI = minTI;

                //string maxRewardArg = txtRewardArgMax.Text;

                if(txtMaxRP.Text.Length > 0)
                    Int32.TryParse(txtMaxRP.Text, out maxRP);
                if (txtMaxPsi.Text.Length > 0)
                    Int32.TryParse(txtMaxPsi.Text, out maxPsi);
                if (txtMaxEta.Text.Length > 0)
                    Int32.TryParse(txtMaxEta.Text, out maxEta);
                if (txtMaxRes.Text.Length > 0)
                    Int32.TryParse(txtMaxRes.Text, out maxRES);
                //if (txtMaxRps.Text.Length > 0)
               //     Int32.TryParse(txtMaxRps.Text, out maxRPS);
                if (txtMaxTi.Text.Length > 0)
                    Int32.TryParse(txtMaxTi.Text, out maxTI);

                if (maxRP == minRP && maxPsi == minPsi && maxEta == minEta
                    //&& maxRPS == minRPS 
                    && maxRES == minRES && minTI == maxTI/*&& maxRewardArg.Length == 0*/)
                {
                    param.serialize(d.FileName);
                }
                else
                {
                    //string rewardJump = txtRewardArgJump.Text;
                    int rpJump = 0, psiJump = 0, etaJump = 0, resJump = 0, rpsJump = 0, tiJump;
                    Int32.TryParse(txtPsiJump.Text, out psiJump);
                    Int32.TryParse(txtEtaJump.Text, out etaJump);
                    Int32.TryParse(txtRPJump.Text, out rpJump);
                    Int32.TryParse(txtResJump.Text, out resJump);
                    //Int32.TryParse(txtRpsJump.Text, out rpsJump);
                    Int32.TryParse(txtTiJump.Text, out tiJump);

                    rpJump = Math.Max(rpJump, 1);
                    psiJump = Math.Max(psiJump, 1);
                    etaJump = Math.Max(etaJump, 1);
                    resJump = Math.Max(resJump, 1);
                    //rpsJump = Math.Max(rpsJump, 1);
                    tiJump = Math.Max(tiJump, 1);

                    dialogFilename = dialogFilename.Substring(0, dialogFilename.LastIndexOf('.'));

                    string newFolderName = "";

                    try
                    {
                        char slash = '\\';
                        if (!dialogFilename.Contains(slash))
                            slash = '/';

                        newFolderName = dialogFilename.Substring(dialogFilename.LastIndexOf(slash) + 1);
                        Directory.CreateDirectory(dialogFilename);
                        dialogFilename += (slash + newFolderName);
                    }
                    catch (Exception) { }

                    //var rewardFuncs = param.R.getRewardFunctions(minRewardArg, maxRewardArg, rewardJump);

                    //foreach(ARewardFunction rw in rewardFuncs)
                    //for (int rps = minRPS; rps <= maxRPS; rps += rpsJump)
                    for (int res = minRES; res <= maxRES; res += resJump)
                    for (int rp  = minRP ; rp <=  maxRP;  rp += rpJump)
                    for (int psi = minPsi; psi <= maxPsi; psi += psiJump)
                    for (int eta = minEta; eta <= maxEta; eta += etaJump)
                    for (int ti = minTI; ti<= maxTI; ti += tiJump)
                    {

                        param.A_E = Evader.getAgents(eta);
                        param.A_P = Pursuer.getAgents(psi);
                        param.r_p = rp;
                        param.t_i = ti;
                        param.r_es = res;
                        //param.r_ps = rps;
                        
                        //param.R = rw;


                        string fileName =
                            dialogFilename + "_eta" + eta.ToString() +
                                        "_rp" + rp.ToString() +
                                        "_psi" + psi.ToString() +
                                        "_res" + res.ToString() +
                                        //"_rps" + rps.ToString() +
                                        "_ti" + ti.ToString() +
                            //"_" + rw.fileNameDescription() +
                                        "." + AppConstants.FileExtensions.INTRUSION_PARAM;
                        param.serialize(fileName);
                    }
                }

                isDirty = false;
                ParamFilePath = d.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private delegate bool ValCond(int val);

        /// <summary>
        /// colors 'inputLabel' either black, if value in 'input' was parsed successfully (colors it red otherwise)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputLabel"></param>
        /// <param name="val"></param>
        /// <returns>
        /// true if parsing successfull
        /// </returns>
        bool tryParseTextBox(TextBox input, Label inputLabel, out int val, ValCond cond = null)
        {
            if(Int32.TryParse(input.Text, out val))
            {
                if (cond == null || cond(val))
                {
                    inputLabel.ForeColor = Color.Black;
                    return true;
                }
            }
            inputLabel.ForeColor = Color.Red;
            return false;
        }

        /// <summary>
        /// makes sure the active param object matches the GUI
        /// </summary>
        private void setParamData()
        {
            //param.canEvadersReceiveMultipleBroadcasts = chkMultipleBroadcasts.Checked;
            //param.canSinksSensePursuers = chkSinksSensePursuers.Checked;
            //param.areSinksSafe = chkSafeSinks.Checked;

            int psi;
            if (tryParseTextBox(txtPursuerCount, lblPursuerCount, out psi))
            {
                param.A_P = Pursuer.getAgents(psi);
            }

            int evaderCount;
            if (tryParseTextBox(txtEvadersCount, lblEvaderCount, out evaderCount))
            {
                param.A_E = Evader.getAgents(evaderCount);
            }

            int rp;
            if(tryParseTextBox(txtrp, lblrp, out rp))
                param.r_p = rp;

            int re;
            // if area is square, also make sure re is valid i.e. odd value
            if (tryParseTextBox(txtre, lblre, out re, (reval) => { return !chkSquareArea.Checked || reval % 2 == 1; }))
                param.r_e = re;
            

            int res;
            if (tryParseTextBox(txtres,lblrs,out res))
                param.r_es = res;

            //int rps;
            //if (tryParseTextBox(txtrps, lblrs, out rps))
            //    param.r_ps = rps;

            int ti;
            if (tryParseTextBox(txtti, lblti, out ti))
                param.t_i = ti;


            param.IsAreaSquare = chkSquareArea.Checked;

            //try
            //{
            //    // set reward func:
            //    if (cmbRewardFunc.SelectedItem == null)
            //    {
            //        lblR.ForeColor = Color.Red;
            //        return;
            //    }

            //    param.R = ReflectionUtils.constructEmptyCtorType<ARewardFunction>((string)cmbRewardFunc.SelectedItem);
            //    lblR.ForeColor = Color.Black;
            //    lstRewarrdDesc.Items.Clear();
            //    lstRewarrdDesc.Items.AddRange(param.R.argumentsDescription().ToArray<object>());

            //    try
            //    {
            //        param.R.setArgs(txtRewardArg.Text);
            //        lblRewardArg.ForeColor = Color.Black;
            //    }
            //    catch (Exception) 
            //    {
            //        lblRewardArg.ForeColor = Color.Red;
            //    }

            //}
            //catch (Exception ex)
            //{
            //    lblR.ForeColor = Color.Red;
            //}
            
            
        }
        
        private void GameParamEditor_Load(object sender, EventArgs e)
        {
            //var rewardFunctionsList = typeof(RewardFunctions).
              //  GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            var rewardFunctionsList = ReflectionUtils.getTypeList<ARewardFunction>();
            foreach (var m in rewardFunctionsList)
            {
                try
                {
                    //if (m.CreateDelegate(typeof(RewardFunction)) != null)
                    //    cmbRewardFunc.Items.Add(m.Name);
                    //rewardFunctions[m.Key] = m.Value;
                    cmbRewardFunc.Items.Add(m.Key);
                }
                catch (Exception) { }
            }

            if(ParamFilePath != "")
            {
                try
                {
                    load(ParamFilePath);
                }
                catch (Exception) { }
            }
            
        }

        private void load(string filename)
        {
            param.deserialize(filename, null);
            txtEvadersCount.Text = param.A_E.Count().ToString();
            txtPursuerCount.Text = param.A_P.Count().ToString();
            txtre.Text = param.r_e.ToString();
            txtres.Text = param.r_es.ToString();
            txtrp.Text = param.r_p.ToString();
            txtti.Text = param.t_i.ToString();
            //chkMultipleBroadcasts.Checked = param.canEvadersReceiveMultipleBroadcasts;
            //txtRewardArg.Text = param.R.ArgsCSV;

            //for (int i = 0; i < cmbRewardFunc.Items.Count; ++i)
            //    if (cmbRewardFunc.Items[i] == param.R.GetType().Name)
            //        cmbRewardFunc.SelectedIndex = i; // note: this calls index change handler

            foreach (object o in this.Controls)
            {
                Label l = o as Label;
                if (l != null)
                    l.ForeColor = Color.Black; // mark all fields as "valid"
            }
            
            txtMaxEta.Text = "";
            txtMaxPsi.Text = "";
            txtMaxRP.Text = "";
            txtMaxTi.Text = "";
            txtPsiJump.Text = "";
            txtRPJump.Text = "";
            txtEtaJump.Text = "";
            txtTiJump.Text = "";
            setParamData();
            isDirty = false;
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Game Param Files (*.gprm)|*.gprm";
            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                load(d.FileName);
                ParamFilePath = d.FileName;
            }
            catch (Exception) { }
        }

        private void btnSave_Click_1(object sender, EventArgs e)
        {
            if (txtMaxEta.Text + txtMaxPsi.Text + txtMaxRP.Text != "")
            {
                MessageBox.Show("For saving multiple files, use 'Save As' ");
                return;
            }

            setParamData();

            if (ParamFilePath == "")
            {
                btnSave_Click(sender, e);
                return;
            }
            param.serialize(ParamFilePath);
            isDirty = false;
            
        }

        private void frmGameParamEditor_Load(object sender, EventArgs e)
        {

        }

        #region region Designer Code
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSaveAs;
        private System.Windows.Forms.TextBox txtEvadersCount;
        private System.Windows.Forms.Label lblEvaderCount;
        private System.Windows.Forms.ComboBox cmbRewardFunc;
        private System.Windows.Forms.Label lblPursuerCount;
        private System.Windows.Forms.TextBox txtPursuerCount;
        private System.Windows.Forms.Label lblrp;
        private System.Windows.Forms.TextBox txtrp;
        private System.Windows.Forms.Label lblre;
        private System.Windows.Forms.TextBox txtre;
        private System.Windows.Forms.Label lblrs;
        private System.Windows.Forms.TextBox txtres;
        private System.Windows.Forms.Label lblR;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TextBox txtMaxRP;
        private System.Windows.Forms.TextBox txtMaxPsi;
        private System.Windows.Forms.TextBox txtPsiJump;
        
        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnSaveAs = new System.Windows.Forms.Button();
            this.txtEvadersCount = new System.Windows.Forms.TextBox();
            this.lblEvaderCount = new System.Windows.Forms.Label();
            this.cmbRewardFunc = new System.Windows.Forms.ComboBox();
            this.lblPursuerCount = new System.Windows.Forms.Label();
            this.txtPursuerCount = new System.Windows.Forms.TextBox();
            this.lblrp = new System.Windows.Forms.Label();
            this.txtrp = new System.Windows.Forms.TextBox();
            this.lblre = new System.Windows.Forms.Label();
            this.txtre = new System.Windows.Forms.TextBox();
            this.lblrs = new System.Windows.Forms.Label();
            this.txtres = new System.Windows.Forms.TextBox();
            this.lblR = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.txtMaxRP = new System.Windows.Forms.TextBox();
            this.txtMaxPsi = new System.Windows.Forms.TextBox();
            this.txtPsiJump = new System.Windows.Forms.TextBox();
            this.lblRewardArg = new System.Windows.Forms.Label();
            this.txtRewardArg = new System.Windows.Forms.TextBox();
            this.lstRewarrdDesc = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRPJump = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtEtaJump = new System.Windows.Forms.TextBox();
            this.txtMaxEta = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtRewardArgMax = new System.Windows.Forms.TextBox();
            this.txtRewardArgJump = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.lblrps = new System.Windows.Forms.Label();
            this.txtrps = new System.Windows.Forms.TextBox();
            this.txtResJump = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtMaxRes = new System.Windows.Forms.TextBox();
            this.txtRpsJump = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.txtMaxRps = new System.Windows.Forms.TextBox();
            this.txtTiJump = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.txtMaxTi = new System.Windows.Forms.TextBox();
            this.lblti = new System.Windows.Forms.Label();
            this.txtti = new System.Windows.Forms.TextBox();
            this.chkSquareArea = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(1, 312);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 30;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.Location = new System.Drawing.Point(164, 312);
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Size = new System.Drawing.Size(75, 23);
            this.btnSaveAs.TabIndex = 32;
            this.btnSaveAs.Text = "Save &as";
            this.btnSaveAs.UseVisualStyleBackColor = true;
            this.btnSaveAs.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // txtEvadersCount
            // 
            this.txtEvadersCount.Location = new System.Drawing.Point(153, 2);
            this.txtEvadersCount.Name = "txtEvadersCount";
            this.txtEvadersCount.Size = new System.Drawing.Size(100, 20);
            this.txtEvadersCount.TabIndex = 0;
            this.txtEvadersCount.TextChanged += new System.EventHandler(this.txtEvadersCount_TextChanged);
            // 
            // lblEvaderCount
            // 
            this.lblEvaderCount.AutoSize = true;
            this.lblEvaderCount.ForeColor = System.Drawing.Color.Red;
            this.lblEvaderCount.Location = new System.Drawing.Point(12, 9);
            this.lblEvaderCount.Name = "lblEvaderCount";
            this.lblEvaderCount.Size = new System.Drawing.Size(104, 13);
            this.lblEvaderCount.TabIndex = 3;
            this.lblEvaderCount.Text = "\\eta (Evader Count):";
            // 
            // cmbRewardFunc
            // 
            this.cmbRewardFunc.FormattingEnabled = true;
            this.cmbRewardFunc.Location = new System.Drawing.Point(152, 216);
            this.cmbRewardFunc.Name = "cmbRewardFunc";
            this.cmbRewardFunc.Size = new System.Drawing.Size(162, 21);
            this.cmbRewardFunc.TabIndex = 5;
            this.cmbRewardFunc.Visible = false;
            this.cmbRewardFunc.SelectedIndexChanged += new System.EventHandler(this.cmbRewardFunc_SelectedIndexChanged);
            // 
            // lblPursuerCount
            // 
            this.lblPursuerCount.AutoSize = true;
            this.lblPursuerCount.ForeColor = System.Drawing.Color.Red;
            this.lblPursuerCount.Location = new System.Drawing.Point(12, 35);
            this.lblPursuerCount.Name = "lblPursuerCount";
            this.lblPursuerCount.Size = new System.Drawing.Size(104, 13);
            this.lblPursuerCount.TabIndex = 6;
            this.lblPursuerCount.Text = "\\psi (Pursuer Count):";
            // 
            // txtPursuerCount
            // 
            this.txtPursuerCount.Location = new System.Drawing.Point(153, 28);
            this.txtPursuerCount.Name = "txtPursuerCount";
            this.txtPursuerCount.Size = new System.Drawing.Size(100, 20);
            this.txtPursuerCount.TabIndex = 1;
            this.txtPursuerCount.TextChanged += new System.EventHandler(this.txtPursuerCount_TextChanged);
            // 
            // lblrp
            // 
            this.lblrp.AutoSize = true;
            this.lblrp.ForeColor = System.Drawing.Color.Red;
            this.lblrp.Location = new System.Drawing.Point(12, 64);
            this.lblrp.Name = "lblrp";
            this.lblrp.Size = new System.Drawing.Size(108, 13);
            this.lblrp.TabIndex = 8;
            this.lblrp.Text = "r_p (pursuer velocity):";
            // 
            // txtrp
            // 
            this.txtrp.Location = new System.Drawing.Point(153, 57);
            this.txtrp.Name = "txtrp";
            this.txtrp.Size = new System.Drawing.Size(100, 20);
            this.txtrp.TabIndex = 2;
            this.txtrp.TextChanged += new System.EventHandler(this.txtrp_TextChanged);
            // 
            // lblre
            // 
            this.lblre.AutoSize = true;
            this.lblre.ForeColor = System.Drawing.Color.Red;
            this.lblre.Location = new System.Drawing.Point(12, 90);
            this.lblre.Name = "lblre";
            this.lblre.Size = new System.Drawing.Size(62, 13);
            this.lblre.TabIndex = 10;
            this.lblre.Text = "area radius:";
            // 
            // txtre
            // 
            this.txtre.Location = new System.Drawing.Point(153, 83);
            this.txtre.Name = "txtre";
            this.txtre.Size = new System.Drawing.Size(100, 20);
            this.txtre.TabIndex = 3;
            this.txtre.TextChanged += new System.EventHandler(this.txtre_TextChanged);
            // 
            // lblrs
            // 
            this.lblrs.AutoSize = true;
            this.lblrs.ForeColor = System.Drawing.Color.Red;
            this.lblrs.Location = new System.Drawing.Point(12, 116);
            this.lblrs.Name = "lblrs";
            this.lblrs.Size = new System.Drawing.Size(117, 13);
            this.lblrs.TabIndex = 12;
            this.lblrs.Text = "evaders sensing range:";
            // 
            // txtres
            // 
            this.txtres.Location = new System.Drawing.Point(153, 109);
            this.txtres.Name = "txtres";
            this.txtres.Size = new System.Drawing.Size(100, 20);
            this.txtres.TabIndex = 4;
            this.txtres.TextChanged += new System.EventHandler(this.txtrs_TextChanged);
            // 
            // lblR
            // 
            this.lblR.AutoSize = true;
            this.lblR.ForeColor = System.Drawing.Color.Red;
            this.lblR.Location = new System.Drawing.Point(12, 216);
            this.lblR.Name = "lblR";
            this.lblR.Size = new System.Drawing.Size(74, 13);
            this.lblR.TabIndex = 13;
            this.lblR.Text = "Reward func,:";
            this.lblR.Visible = false;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(82, 312);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(76, 23);
            this.btnSave.TabIndex = 31;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click_1);
            // 
            // txtMaxRP
            // 
            this.txtMaxRP.Location = new System.Drawing.Point(305, 57);
            this.txtMaxRP.Name = "txtMaxRP";
            this.txtMaxRP.Size = new System.Drawing.Size(45, 20);
            this.txtMaxRP.TabIndex = 12;
            // 
            // txtMaxPsi
            // 
            this.txtMaxPsi.Location = new System.Drawing.Point(305, 28);
            this.txtMaxPsi.Name = "txtMaxPsi";
            this.txtMaxPsi.Size = new System.Drawing.Size(45, 20);
            this.txtMaxPsi.TabIndex = 10;
            // 
            // txtPsiJump
            // 
            this.txtPsiJump.Location = new System.Drawing.Point(404, 28);
            this.txtPsiJump.Name = "txtPsiJump";
            this.txtPsiJump.Size = new System.Drawing.Size(33, 20);
            this.txtPsiJump.TabIndex = 11;
            // 
            // lblRewardArg
            // 
            this.lblRewardArg.AutoSize = true;
            this.lblRewardArg.ForeColor = System.Drawing.Color.Red;
            this.lblRewardArg.Location = new System.Drawing.Point(12, 262);
            this.lblRewardArg.Name = "lblRewardArg";
            this.lblRewardArg.Size = new System.Drawing.Size(124, 13);
            this.lblRewardArg.TabIndex = 19;
            this.lblRewardArg.Text = "Reward Func. CVS args:";
            this.lblRewardArg.Visible = false;
            // 
            // txtRewardArg
            // 
            this.txtRewardArg.Location = new System.Drawing.Point(164, 262);
            this.txtRewardArg.Name = "txtRewardArg";
            this.txtRewardArg.Size = new System.Drawing.Size(97, 20);
            this.txtRewardArg.TabIndex = 7;
            this.txtRewardArg.Visible = false;
            this.txtRewardArg.TextChanged += new System.EventHandler(this.txtRewardArg_TextChanged);
            // 
            // lstRewarrdDesc
            // 
            this.lstRewarrdDesc.FormattingEnabled = true;
            this.lstRewarrdDesc.HorizontalScrollbar = true;
            this.lstRewarrdDesc.Location = new System.Drawing.Point(320, 216);
            this.lstRewarrdDesc.Name = "lstRewarrdDesc";
            this.lstRewarrdDesc.Size = new System.Drawing.Size(227, 43);
            this.lstRewarrdDesc.TabIndex = 21;
            this.lstRewarrdDesc.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(270, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(19, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "to:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(270, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(19, 13);
            this.label2.TabIndex = 23;
            this.label2.Text = "to:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(366, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 24;
            this.label3.Text = "jump:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(366, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "jump:";
            // 
            // txtRPJump
            // 
            this.txtRPJump.Location = new System.Drawing.Point(404, 57);
            this.txtRPJump.Name = "txtRPJump";
            this.txtRPJump.Size = new System.Drawing.Size(33, 20);
            this.txtRPJump.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(366, 2);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(32, 13);
            this.label5.TabIndex = 30;
            this.label5.Text = "jump:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(270, 5);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(19, 13);
            this.label6.TabIndex = 29;
            this.label6.Text = "to:";
            // 
            // txtEtaJump
            // 
            this.txtEtaJump.Location = new System.Drawing.Point(404, 2);
            this.txtEtaJump.Name = "txtEtaJump";
            this.txtEtaJump.Size = new System.Drawing.Size(33, 20);
            this.txtEtaJump.TabIndex = 9;
            // 
            // txtMaxEta
            // 
            this.txtMaxEta.Location = new System.Drawing.Point(305, 2);
            this.txtMaxEta.Name = "txtMaxEta";
            this.txtMaxEta.Size = new System.Drawing.Size(45, 20);
            this.txtMaxEta.TabIndex = 8;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(267, 262);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(19, 13);
            this.label7.TabIndex = 36;
            this.label7.Text = "to:";
            this.label7.Visible = false;
            // 
            // txtRewardArgMax
            // 
            this.txtRewardArgMax.Location = new System.Drawing.Point(292, 262);
            this.txtRewardArgMax.Name = "txtRewardArgMax";
            this.txtRewardArgMax.Size = new System.Drawing.Size(97, 20);
            this.txtRewardArgMax.TabIndex = 37;
            this.txtRewardArgMax.Visible = false;
            // 
            // txtRewardArgJump
            // 
            this.txtRewardArgJump.Location = new System.Drawing.Point(440, 262);
            this.txtRewardArgJump.Name = "txtRewardArgJump";
            this.txtRewardArgJump.Size = new System.Drawing.Size(33, 20);
            this.txtRewardArgJump.TabIndex = 38;
            this.txtRewardArgJump.Visible = false;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(402, 262);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(32, 13);
            this.label8.TabIndex = 39;
            this.label8.Text = "jump:";
            this.label8.Visible = false;
            // 
            // lblrps
            // 
            this.lblrps.AutoSize = true;
            this.lblrps.ForeColor = System.Drawing.Color.Red;
            this.lblrps.Location = new System.Drawing.Point(12, 142);
            this.lblrps.Name = "lblrps";
            this.lblrps.Size = new System.Drawing.Size(122, 13);
            this.lblrps.TabIndex = 41;
            this.lblrps.Text = "pursuers sensing range :";
            this.lblrps.Visible = false;
            // 
            // txtrps
            // 
            this.txtrps.Location = new System.Drawing.Point(153, 135);
            this.txtrps.Name = "txtrps";
            this.txtrps.Size = new System.Drawing.Size(100, 20);
            this.txtrps.TabIndex = 40;
            this.txtrps.Visible = false;
            this.txtrps.TextChanged += new System.EventHandler(this.txtrps_TextChanged);
            // 
            // txtResJump
            // 
            this.txtResJump.Location = new System.Drawing.Point(402, 113);
            this.txtResJump.Name = "txtResJump";
            this.txtResJump.Size = new System.Drawing.Size(33, 20);
            this.txtResJump.TabIndex = 43;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(364, 113);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(32, 13);
            this.label9.TabIndex = 45;
            this.label9.Text = "jump:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(268, 116);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(19, 13);
            this.label10.TabIndex = 44;
            this.label10.Text = "to:";
            // 
            // txtMaxRes
            // 
            this.txtMaxRes.Location = new System.Drawing.Point(303, 113);
            this.txtMaxRes.Name = "txtMaxRes";
            this.txtMaxRes.Size = new System.Drawing.Size(45, 20);
            this.txtMaxRes.TabIndex = 42;
            // 
            // txtRpsJump
            // 
            this.txtRpsJump.Location = new System.Drawing.Point(402, 139);
            this.txtRpsJump.Name = "txtRpsJump";
            this.txtRpsJump.Size = new System.Drawing.Size(33, 20);
            this.txtRpsJump.TabIndex = 47;
            this.txtRpsJump.Visible = false;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(364, 139);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(32, 13);
            this.label11.TabIndex = 49;
            this.label11.Text = "jump:";
            this.label11.Visible = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(268, 142);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(19, 13);
            this.label12.TabIndex = 48;
            this.label12.Text = "to:";
            this.label12.Visible = false;
            // 
            // txtMaxRps
            // 
            this.txtMaxRps.Location = new System.Drawing.Point(303, 139);
            this.txtMaxRps.Name = "txtMaxRps";
            this.txtMaxRps.Size = new System.Drawing.Size(45, 20);
            this.txtMaxRps.TabIndex = 46;
            this.txtMaxRps.Visible = false;
            // 
            // txtTiJump
            // 
            this.txtTiJump.Location = new System.Drawing.Point(402, 165);
            this.txtTiJump.Name = "txtTiJump";
            this.txtTiJump.Size = new System.Drawing.Size(33, 20);
            this.txtTiJump.TabIndex = 53;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(364, 165);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(32, 13);
            this.label13.TabIndex = 55;
            this.label13.Text = "jump:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(268, 168);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(19, 13);
            this.label14.TabIndex = 54;
            this.label14.Text = "to:";
            // 
            // txtMaxTi
            // 
            this.txtMaxTi.Location = new System.Drawing.Point(303, 165);
            this.txtMaxTi.Name = "txtMaxTi";
            this.txtMaxTi.Size = new System.Drawing.Size(45, 20);
            this.txtMaxTi.TabIndex = 52;
            // 
            // lblti
            // 
            this.lblti.AutoSize = true;
            this.lblti.ForeColor = System.Drawing.Color.Red;
            this.lblti.Location = new System.Drawing.Point(12, 168);
            this.lblti.Name = "lblti";
            this.lblti.Size = new System.Drawing.Size(89, 13);
            this.lblti.TabIndex = 51;
            this.lblti.Text = "time for intrusion :";
            // 
            // txtti
            // 
            this.txtti.Location = new System.Drawing.Point(153, 161);
            this.txtti.Name = "txtti";
            this.txtti.Size = new System.Drawing.Size(100, 20);
            this.txtti.TabIndex = 50;
            this.txtti.TextChanged += new System.EventHandler(this.txtti_TextChanged);
            // 
            // chkSquareArea
            // 
            this.chkSquareArea.AutoSize = true;
            this.chkSquareArea.Location = new System.Drawing.Point(270, 85);
            this.chkSquareArea.Name = "chkSquareArea";
            this.chkSquareArea.Size = new System.Drawing.Size(100, 17);
            this.chkSquareArea.TabIndex = 56;
            this.chkSquareArea.Text = "Rectangle Area";
            this.chkSquareArea.UseVisualStyleBackColor = true;
            this.chkSquareArea.CheckedChanged += new System.EventHandler(this.chkSquareArea_CheckedChanged);
            // 
            // frmIntrusionGameParamEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(690, 356);
            this.Controls.Add(this.chkSquareArea);
            this.Controls.Add(this.txtTiJump);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.txtMaxTi);
            this.Controls.Add(this.lblti);
            this.Controls.Add(this.txtti);
            this.Controls.Add(this.txtRpsJump);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.txtMaxRps);
            this.Controls.Add(this.txtResJump);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txtMaxRes);
            this.Controls.Add(this.lblrps);
            this.Controls.Add(this.txtrps);
            this.Controls.Add(this.txtRewardArgJump);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtRewardArgMax);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtEtaJump);
            this.Controls.Add(this.txtMaxEta);
            this.Controls.Add(this.txtRPJump);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lstRewarrdDesc);
            this.Controls.Add(this.txtRewardArg);
            this.Controls.Add(this.lblRewardArg);
            this.Controls.Add(this.txtPsiJump);
            this.Controls.Add(this.txtMaxPsi);
            this.Controls.Add(this.txtMaxRP);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblR);
            this.Controls.Add(this.lblrs);
            this.Controls.Add(this.txtres);
            this.Controls.Add(this.lblre);
            this.Controls.Add(this.txtre);
            this.Controls.Add(this.lblrp);
            this.Controls.Add(this.txtrp);
            this.Controls.Add(this.lblPursuerCount);
            this.Controls.Add(this.txtPursuerCount);
            this.Controls.Add(this.cmbRewardFunc);
            this.Controls.Add(this.lblEvaderCount);
            this.Controls.Add(this.txtEvadersCount);
            this.Controls.Add(this.btnSaveAs);
            this.Controls.Add(this.btnLoad);
            this.Name = "frmIntrusionGameParamEditor";
            this.Text = "GameParamEditor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GameParamEditor_FormClosing);
            this.Load += new System.EventHandler(this.GameParamEditor_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private void txtRewardArg_TextChanged(object sender, EventArgs e)
        {
            setParamData();
            isDirty = true;
        }
        #endregion

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void txtti_TextChanged(object sender, EventArgs e)
        {
            setParamData();
            isDirty = true;
        }

        private void txtrps_TextChanged(object sender, EventArgs e)
        {
            setParamData();
            isDirty = true;
        }

        private void chkSquareArea_CheckedChanged(object sender, EventArgs e)
        {
            param.IsAreaSquare = chkSquareArea.Checked;
        }
    }
}
