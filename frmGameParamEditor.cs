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
    public partial class frmGameParamEditor : Form
    {
        
        public frmGameParamEditor(string defaultFile)
        {
            InitializeComponent();
            ParamFilePath = defaultFile;
        }
        
        private bool isDirty;
        private GoEGameParams param = GoEGameParams.getClearParams();
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
        private CheckBox chkMultipleBroadcasts;
        private CheckBox chkSinksSensePursuers;
        private CheckBox chkSafeSinks;
        private Label label7;
        private TextBox txtRewardArgMax;
        private TextBox txtRewardArgJump;
        private Label label8;
        private Label lblpd;
        private TextBox txtpd;
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

                    if (l != null && l.ForeColor.ToArgb() != Color.Black.ToArgb()) // ensure all fields are "valid"
                    {
                        MessageBox.Show(l.Text + " has invalid value");
                        return;
                    }
            }
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "Game Param Files (*." + AppConstants.FileExtensions.PARAM + ")|*." + AppConstants.FileExtensions.PARAM;

            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string dialogFilename = d.FileName;

                int minRP = Int32.Parse(txtrp.Text);
                int minPsi = Int32.Parse(txtPursuerCount.Text);
                int minEta = Int32.Parse(txtEvadersCount.Text);
                string minRewardArg = txtRewardArg.Text;

                int maxRP = minRP;
                int maxPsi = minPsi;
                int maxEta = minEta;
                string maxRewardArg = txtRewardArgMax.Text;

                if(txtMaxRP.Text.Length > 0)
                    Int32.TryParse(txtMaxRP.Text, out maxRP);
                if (txtMaxPsi.Text.Length > 0)
                    Int32.TryParse(txtMaxPsi.Text, out maxPsi);
                if (txtMaxEta.Text.Length > 0)
                    Int32.TryParse(txtMaxEta.Text, out maxEta);

                if (maxRP == minRP && maxPsi == minPsi && maxEta == minEta && maxRewardArg.Length == 0)
                    param.serialize(d.FileName);
                else
                {
                    string rewardJump = txtRewardArgJump.Text;
                    int rpJump = 0, psiJump = 0, etaJump = 0;
                    Int32.TryParse(txtPsiJump.Text, out psiJump);
                    Int32.TryParse(txtEtaJump.Text, out etaJump);
                    Int32.TryParse(txtRPJump.Text, out rpJump);

                    rpJump = Math.Max(rpJump, 1);
                    psiJump = Math.Max(psiJump, 1);
                    etaJump = Math.Max(etaJump, 1);

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

                    var rewardFuncs = param.R.getRewardFunctions(minRewardArg, maxRewardArg, rewardJump);
                    
                    foreach(ARewardFunction rw in rewardFuncs)
                    for(int rp = minRP  ; rp <= maxRP; rp += rpJump)
                    for(int psi = minPsi; psi <= maxPsi; psi += psiJump)
                    for(int eta = minEta; eta <= maxEta; eta += etaJump)
                        {

                            param.A_E = Evader.getAgents(eta);
                            param.A_P = Pursuer.getAgents(psi);
                            param.r_p = rp;
                            param.R = rw;
                            
                            
                            string fileName = 
                                dialogFilename + "_eta" + eta.ToString() + 
                                            "_rp"  + rp.ToString() + 
                                            "_psi" + psi.ToString() + 
                                            "_" + rw.fileNameDescription() +
                                            "." + AppConstants.FileExtensions.PARAM;
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

        /// <summary>
        /// colors 'inputLabel' either black, if value in 'input' was parsed successfully (colors it red otherwise)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputLabel"></param>
        /// <param name="val"></param>
        /// <returns>
        /// true if parsing successfull
        /// </returns>
        bool tryParseTextBox(TextBox input, Label inputLabel, out int val)
        {
            if(Int32.TryParse(input.Text, out val))
            {
                inputLabel.ForeColor = Color.Black;
                return true;
            }
            inputLabel.ForeColor = Color.Red;
            return false;
        }

        bool tryParseTextBox(TextBox input, Label inputLabel, out float val)
        {
            if (float.TryParse(input.Text, out val))
            {
                inputLabel.ForeColor = Color.Black;
                return true;
            }
            inputLabel.ForeColor = Color.Red;
            return false;
        }

        /// <summary>
        /// makes sure the active param object matches the GUI
        /// </summary>
        private void setParamData()
        {
            param.canEvadersReceiveMultipleBroadcasts = chkMultipleBroadcasts.Checked;
            param.canSinksSensePursuers = chkSinksSensePursuers.Checked;
            param.areSinksSafe = chkSafeSinks.Checked;

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
            if (tryParseTextBox(txtre,lblre,out re))
                param.r_e = re;

            int rs;
            if (tryParseTextBox(txtrs,lblrs,out rs))
                param.r_s = rs;

            float pd;
            if (tryParseTextBox(txtpd, lblpd, out pd))
                param.p_d = pd;

            try
            {
                // set reward func:
                if (cmbRewardFunc.SelectedItem == null)
                {
                    lblR.ForeColor = Color.Red;
                    return;
                }

                param.R = ReflectionUtils.constructEmptyCtorType<ARewardFunction>((string)cmbRewardFunc.SelectedItem);
                lblR.ForeColor = Color.Black;
                lstRewarrdDesc.Items.Clear();
                lstRewarrdDesc.Items.AddRange(param.R.argumentsDescription().ToArray<object>());

                try
                {
                    param.R.setArgs(txtRewardArg.Text);
                    lblRewardArg.ForeColor = Color.Black;
                }
                catch (Exception) 
                {
                    lblRewardArg.ForeColor = Color.Red;
                }

                //// if reward func isn't undefined, check argument validity:
                //int givenArgs;
                //if (txtRewardArg.Text == "")
                //    givenArgs = 0;
                //else
                //    givenArgs = txtRewardArg.Text.Split(new char[] { ',' }).Count() + 1;

                //if (givenArgs == param.R.argumentsDescription().Count)
                //    lblRewardArg.ForeColor = Color.Black;
                //else
                //    lblRewardArg.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                lblR.ForeColor = Color.Red;
            }
            
            
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
            txtrs.Text = param.r_s.ToString();
            txtrp.Text = param.r_p.ToString();
            txtpd.Text = param.p_d.ToString();
            chkSafeSinks.Checked = param.areSinksSafe;
            chkMultipleBroadcasts.Checked = param.canEvadersReceiveMultipleBroadcasts;
            txtRewardArg.Text = param.R.ArgsCSV;

            for (int i = 0; i < cmbRewardFunc.Items.Count; ++i)
                if (cmbRewardFunc.Items[i] == param.R.GetType().Name)
                    cmbRewardFunc.SelectedIndex = i; // note: this calls index change handler

            foreach (object o in this.Controls)
            {
                Label l = o as Label;
                if (l != null)
                    l.ForeColor = Color.Black; // mark all fields as "valid"
            }
            
            txtMaxEta.Text = "";
            txtMaxPsi.Text = "";
            txtMaxRP.Text = "";
            txtPsiJump.Text = "";
            txtRPJump.Text = "";
            txtEtaJump.Text = "";
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
        private System.Windows.Forms.TextBox txtrs;
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
            this.txtrs = new System.Windows.Forms.TextBox();
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
            this.chkMultipleBroadcasts = new System.Windows.Forms.CheckBox();
            this.chkSinksSensePursuers = new System.Windows.Forms.CheckBox();
            this.chkSafeSinks = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtRewardArgMax = new System.Windows.Forms.TextBox();
            this.txtRewardArgJump = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.lblpd = new System.Windows.Forms.Label();
            this.txtpd = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(15, 367);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 30;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.Location = new System.Drawing.Point(178, 367);
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
            this.cmbRewardFunc.Location = new System.Drawing.Point(156, 193);
            this.cmbRewardFunc.Name = "cmbRewardFunc";
            this.cmbRewardFunc.Size = new System.Drawing.Size(162, 21);
            this.cmbRewardFunc.TabIndex = 5;
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
            this.lblrp.Size = new System.Drawing.Size(28, 13);
            this.lblrp.TabIndex = 8;
            this.lblrp.Text = "r_p :";
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
            this.lblre.Size = new System.Drawing.Size(25, 13);
            this.lblre.TabIndex = 10;
            this.lblre.Text = "r_e:";
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
            this.lblrs.Size = new System.Drawing.Size(24, 13);
            this.lblrs.TabIndex = 12;
            this.lblrs.Text = "r_s:";
            // 
            // txtrs
            // 
            this.txtrs.Location = new System.Drawing.Point(153, 109);
            this.txtrs.Name = "txtrs";
            this.txtrs.Size = new System.Drawing.Size(100, 20);
            this.txtrs.TabIndex = 4;
            this.txtrs.TextChanged += new System.EventHandler(this.txtrs_TextChanged);
            // 
            // lblR
            // 
            this.lblR.AutoSize = true;
            this.lblR.ForeColor = System.Drawing.Color.Red;
            this.lblR.Location = new System.Drawing.Point(16, 193);
            this.lblR.Name = "lblR";
            this.lblR.Size = new System.Drawing.Size(74, 13);
            this.lblR.TabIndex = 13;
            this.lblR.Text = "Reward func,:";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(96, 367);
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
            this.lblRewardArg.Location = new System.Drawing.Point(18, 240);
            this.lblRewardArg.Name = "lblRewardArg";
            this.lblRewardArg.Size = new System.Drawing.Size(124, 13);
            this.lblRewardArg.TabIndex = 19;
            this.lblRewardArg.Text = "Reward Func. CVS args:";
            // 
            // txtRewardArg
            // 
            this.txtRewardArg.Location = new System.Drawing.Point(170, 240);
            this.txtRewardArg.Name = "txtRewardArg";
            this.txtRewardArg.Size = new System.Drawing.Size(97, 20);
            this.txtRewardArg.TabIndex = 7;
            this.txtRewardArg.TextChanged += new System.EventHandler(this.txtRewardArg_TextChanged);
            // 
            // lstRewarrdDesc
            // 
            this.lstRewarrdDesc.FormattingEnabled = true;
            this.lstRewarrdDesc.HorizontalScrollbar = true;
            this.lstRewarrdDesc.Location = new System.Drawing.Point(324, 193);
            this.lstRewarrdDesc.Name = "lstRewarrdDesc";
            this.lstRewarrdDesc.Size = new System.Drawing.Size(227, 43);
            this.lstRewarrdDesc.TabIndex = 21;
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
            // chkMultipleBroadcasts
            // 
            this.chkMultipleBroadcasts.AutoSize = true;
            this.chkMultipleBroadcasts.Checked = true;
            this.chkMultipleBroadcasts.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMultipleBroadcasts.Location = new System.Drawing.Point(18, 281);
            this.chkMultipleBroadcasts.Name = "chkMultipleBroadcasts";
            this.chkMultipleBroadcasts.Size = new System.Drawing.Size(293, 17);
            this.chkMultipleBroadcasts.TabIndex = 33;
            this.chkMultipleBroadcasts.Text = "Evaders may receive multiple transmissions simultenously";
            this.chkMultipleBroadcasts.UseVisualStyleBackColor = true;
            this.chkMultipleBroadcasts.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // chkSinksSensePursuers
            // 
            this.chkSinksSensePursuers.AutoSize = true;
            this.chkSinksSensePursuers.Location = new System.Drawing.Point(18, 304);
            this.chkSinksSensePursuers.Name = "chkSinksSensePursuers";
            this.chkSinksSensePursuers.Size = new System.Drawing.Size(148, 17);
            this.chkSinksSensePursuers.TabIndex = 34;
            this.chkSinksSensePursuers.Text = "Can Sinks sense pursuers";
            this.chkSinksSensePursuers.UseVisualStyleBackColor = true;
            // 
            // chkSafeSinks
            // 
            this.chkSafeSinks.AutoSize = true;
            this.chkSafeSinks.Location = new System.Drawing.Point(18, 327);
            this.chkSafeSinks.Name = "chkSafeSinks";
            this.chkSafeSinks.Size = new System.Drawing.Size(124, 17);
            this.chkSafeSinks.TabIndex = 35;
            this.chkSafeSinks.Text = "Sinks are safe points";
            this.chkSafeSinks.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(273, 240);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(19, 13);
            this.label7.TabIndex = 36;
            this.label7.Text = "to:";
            // 
            // txtRewardArgMax
            // 
            this.txtRewardArgMax.Location = new System.Drawing.Point(298, 240);
            this.txtRewardArgMax.Name = "txtRewardArgMax";
            this.txtRewardArgMax.Size = new System.Drawing.Size(97, 20);
            this.txtRewardArgMax.TabIndex = 37;
            // 
            // txtRewardArgJump
            // 
            this.txtRewardArgJump.Location = new System.Drawing.Point(446, 240);
            this.txtRewardArgJump.Name = "txtRewardArgJump";
            this.txtRewardArgJump.Size = new System.Drawing.Size(33, 20);
            this.txtRewardArgJump.TabIndex = 38;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(408, 240);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(32, 13);
            this.label8.TabIndex = 39;
            this.label8.Text = "jump:";
            // 
            // lblpd
            // 
            this.lblpd.AutoSize = true;
            this.lblpd.ForeColor = System.Drawing.Color.Red;
            this.lblpd.Location = new System.Drawing.Point(12, 148);
            this.lblpd.Name = "lblpd";
            this.lblpd.Size = new System.Drawing.Size(31, 13);
            this.lblpd.TabIndex = 41;
            this.lblpd.Text = "p_d :";
            // 
            // txtpd
            // 
            this.txtpd.Location = new System.Drawing.Point(153, 141);
            this.txtpd.Name = "txtpd";
            this.txtpd.Size = new System.Drawing.Size(100, 20);
            this.txtpd.TabIndex = 40;
            this.txtpd.TextChanged += new System.EventHandler(this.txtpd_TextChanged);
            // 
            // frmGameParamEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 411);
            this.Controls.Add(this.lblpd);
            this.Controls.Add(this.txtpd);
            this.Controls.Add(this.txtRewardArgJump);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtRewardArgMax);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.chkSafeSinks);
            this.Controls.Add(this.chkSinksSensePursuers);
            this.Controls.Add(this.chkMultipleBroadcasts);
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
            this.Controls.Add(this.txtrs);
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
            this.Name = "frmGameParamEditor";
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

        private void txtpd_TextChanged(object sender, EventArgs e)
        {
            setParamData();
            isDirty = true;
        }
    }
}
