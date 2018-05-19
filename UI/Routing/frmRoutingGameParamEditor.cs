using GoE.AppConstants;
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
    public partial class frmRoutingGameParamEditor : Form
    {
        // TODO: generalize this form, so we can have automated param editors (just look at the save function - it's horrible!)

        public frmRoutingGameParamEditor(string defaultFile)
        {
            InitializeComponent();
            ParamFilePath = defaultFile;
        }
        
        private bool isDirty;
        private FrontsGridRoutingGameParams param = FrontsGridRoutingGameParams.getClearParams();
        private TextBox textBox2;
        private ListBox listBox1;
        private Label label1;
        private Label label3;
        private Label label5;
        private Label label6;
        private TextBox txtEtaJump;
        private TextBox txtMaxEta;
        private TextBox txtResJump;
        private Label label9;
        private Label label10;
        private TextBox txtMaxRes;
        private TextBox txtRenewalJump;
        private Label label2;
        private Label label4;
        private TextBox txtRenewalMax;
        private Label lblREnewal;
        private TextBox txtRenewal;
        private Label lblDetectionProb;
        private TextBox txtDetectionProb;
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
            d.Filter = "Routing Game Param Files (*." + AppConstants.FileExtensions.ROUTING_PARAM + ")|*." + AppConstants.FileExtensions.ROUTING_PARAM;

            try
            {
                if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string dialogFilename = d.FileName;

                
                float minRenew = float.Parse(txtRenewal.Text);
                int minRES = Int32.Parse(txtres.Text);
                //int minRPS = Int32.Parse(txtrps.Text);
                
                int minPsi = Int32.Parse(txtPursuerCount.Text);
                int minEta = Int32.Parse(txtEvadersCount.Text);
                //string minRewardArg = txtRewardArg.Text;

                
                int maxPsi = minPsi;
                int maxEta = minEta;
                int maxRES = minRES;
                //int maxRPS = minRPS;
                float maxRenew = minRenew;

                //string maxRewardArg = txtRewardArgMax.Text;

                
                if (txtMaxPsi.Text.Length > 0)
                    Int32.TryParse(txtMaxPsi.Text, out maxPsi);
                if (txtMaxEta.Text.Length > 0)
                    Int32.TryParse(txtMaxEta.Text, out maxEta);
                if (txtMaxRes.Text.Length > 0)
                    Int32.TryParse(txtMaxRes.Text, out maxRES);
                //if (txtMaxRps.Text.Length > 0)
               //     Int32.TryParse(txtMaxRps.Text, out maxRPS);
                if (txtRenewalMax.Text.Length > 0)
                    float.TryParse(txtRenewalMax.Text, out maxRenew);

                if (minRenew == maxRenew && maxPsi == minPsi && maxEta == minEta && maxRES == minRES )
                {
                    param.serialize(d.FileName);
                }
                else
                {
                    //string rewardJump = txtRewardArgJump.Text;
                    int rpJump, psiJump, etaJump, resJump, rpsJump;
                    float renewalJump;
                    Int32.TryParse(txtPsiJump.Text, out psiJump);
                    Int32.TryParse(txtEtaJump.Text, out etaJump);
                    Int32.TryParse(txtResJump.Text, out resJump);
                    float.TryParse(txtRenewalJump.Text, out renewalJump);

                    
                    psiJump = Math.Max(psiJump, 1);
                    etaJump = Math.Max(etaJump, 1);
                    resJump = Math.Max(resJump, 1);
                    renewalJump = Math.Max(renewalJump, 0);

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
                    for (int psi = minPsi; psi <= maxPsi; psi += psiJump)
                    for (int eta = minEta; eta <= maxEta; eta += etaJump)
                    for (float renewal = minRenew; renewal <= maxRenew; renewal += renewalJump)
                    {

                        param.A_E = Evader.getAgents(eta);
                        param.A_P = Pursuer.getAgents(psi);
                        param.r_e = res;
                        param.detectionProbRestraint = double.Parse(txtDetectionProb.Text);
                        param.f_e = renewal;                              
                        //param.r_ps = rps;
                        
                        //param.R = rw;


                        string fileName =
                            dialogFilename + "_eta" + eta.ToString() +
                                        "_psi" + psi.ToString() +
                                        "_res" + res.ToString() +
                                        "_fe" + renewal.ToString("0.00") +
                                        "." + AppConstants.FileExtensions.ROUTING_PARAM;
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

        private delegate bool ValCondF(float val);
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
        bool tryParseTextBox(TextBox input, Label inputLabel, out float val, ValCondF cond = null)
        {
            if (float.TryParse(input.Text, out val))
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
            
            int res;
            if (tryParseTextBox(txtres,lblrs,out res))
                param.r_e = res;

            float fe;
            if (tryParseTextBox(txtRenewal, lblREnewal, out fe))
                param.f_e = fe;

            float dp;
            if (tryParseTextBox(txtDetectionProb, lblDetectionProb, out dp))
                param.detectionProbRestraint = dp;
        }
        
        private void GameParamEditor_Load(object sender, EventArgs e)
        {
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
            txtres.Text = param.r_e.ToString();
            txtDetectionProb.Text = param.detectionProbRestraint.ToString();
            txtRenewal.Text = param.f_e.ToString();
            foreach (object o in this.Controls)
            {
                Label l = o as Label;
                if (l != null)
                    l.ForeColor = Color.Black; // mark all fields as "valid"
            }
            
            txtMaxEta.Text = "";
            txtMaxPsi.Text = "";
            txtres.Text = "";
            txtRenewal.Text = "";
            txtPsiJump.Text = "";
            txtRenewalJump.Text = "";
            txtEtaJump.Text = "";
            txtres.Text = "";
            
            setParamData();
            isDirty = false;
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Game Param Files (*." + FileExtensions.ROUTING_PARAM + ")|*." + FileExtensions.ROUTING_PARAM;
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
            if (txtMaxEta.Text + txtMaxPsi.Text + txtMaxRes.Text + txtRenewalMax.Text != "")
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
        private System.Windows.Forms.Label lblPursuerCount;
        private System.Windows.Forms.TextBox txtPursuerCount;
        private System.Windows.Forms.Label lblrs;
        private System.Windows.Forms.TextBox txtres;
        private System.Windows.Forms.Button btnSave;
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
            this.lblPursuerCount = new System.Windows.Forms.Label();
            this.txtPursuerCount = new System.Windows.Forms.TextBox();
            this.lblrs = new System.Windows.Forms.Label();
            this.txtres = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.txtMaxPsi = new System.Windows.Forms.TextBox();
            this.txtPsiJump = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtEtaJump = new System.Windows.Forms.TextBox();
            this.txtMaxEta = new System.Windows.Forms.TextBox();
            this.txtResJump = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtMaxRes = new System.Windows.Forms.TextBox();
            this.txtRenewalJump = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRenewalMax = new System.Windows.Forms.TextBox();
            this.lblREnewal = new System.Windows.Forms.Label();
            this.txtRenewal = new System.Windows.Forms.TextBox();
            this.lblDetectionProb = new System.Windows.Forms.Label();
            this.txtDetectionProb = new System.Windows.Forms.TextBox();
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(270, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(19, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "to:";
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
            // txtRenewalJump
            // 
            this.txtRenewalJump.Location = new System.Drawing.Point(404, 153);
            this.txtRenewalJump.Name = "txtRenewalJump";
            this.txtRenewalJump.Size = new System.Drawing.Size(33, 20);
            this.txtRenewalJump.TabIndex = 49;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(366, 153);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 51;
            this.label2.Text = "jump:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(270, 156);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(19, 13);
            this.label4.TabIndex = 50;
            this.label4.Text = "to:";
            // 
            // txtRenewalMax
            // 
            this.txtRenewalMax.Location = new System.Drawing.Point(305, 153);
            this.txtRenewalMax.Name = "txtRenewalMax";
            this.txtRenewalMax.Size = new System.Drawing.Size(45, 20);
            this.txtRenewalMax.TabIndex = 48;
            // 
            // lblREnewal
            // 
            this.lblREnewal.AutoSize = true;
            this.lblREnewal.ForeColor = System.Drawing.Color.Red;
            this.lblREnewal.Location = new System.Drawing.Point(14, 156);
            this.lblREnewal.Name = "lblREnewal";
            this.lblREnewal.Size = new System.Drawing.Size(101, 13);
            this.lblREnewal.TabIndex = 47;
            this.lblREnewal.Text = "evader renewal rate";
            // 
            // txtRenewal
            // 
            this.txtRenewal.Location = new System.Drawing.Point(155, 149);
            this.txtRenewal.Name = "txtRenewal";
            this.txtRenewal.Size = new System.Drawing.Size(100, 20);
            this.txtRenewal.TabIndex = 46;
            this.txtRenewal.TextChanged += new System.EventHandler(this.txtRenewal_TextChanged);
            // 
            // lblDetectionProb
            // 
            this.lblDetectionProb.AutoSize = true;
            this.lblDetectionProb.ForeColor = System.Drawing.Color.Red;
            this.lblDetectionProb.Location = new System.Drawing.Point(12, 193);
            this.lblDetectionProb.Name = "lblDetectionProb";
            this.lblDetectionProb.Size = new System.Drawing.Size(151, 13);
            this.lblDetectionProb.TabIndex = 53;
            this.lblDetectionProb.Text = "Detection Prob. restraint factor";
            // 
            // txtDetectionProb
            // 
            this.txtDetectionProb.Location = new System.Drawing.Point(187, 190);
            this.txtDetectionProb.Name = "txtDetectionProb";
            this.txtDetectionProb.Size = new System.Drawing.Size(100, 20);
            this.txtDetectionProb.TabIndex = 52;
            this.txtDetectionProb.TextChanged += new System.EventHandler(this.txtDetectionProb_TextChanged);
            // 
            // frmRoutingGameParamEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(690, 356);
            this.Controls.Add(this.lblDetectionProb);
            this.Controls.Add(this.txtDetectionProb);
            this.Controls.Add(this.txtRenewalJump);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtRenewalMax);
            this.Controls.Add(this.lblREnewal);
            this.Controls.Add(this.txtRenewal);
            this.Controls.Add(this.txtResJump);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txtMaxRes);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtEtaJump);
            this.Controls.Add(this.txtMaxEta);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtPsiJump);
            this.Controls.Add(this.txtMaxPsi);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblrs);
            this.Controls.Add(this.txtres);
            this.Controls.Add(this.lblPursuerCount);
            this.Controls.Add(this.txtPursuerCount);
            this.Controls.Add(this.lblEvaderCount);
            this.Controls.Add(this.txtEvadersCount);
            this.Controls.Add(this.btnSaveAs);
            this.Controls.Add(this.btnLoad);
            this.Name = "frmRoutingGameParamEditor";
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

        private void txtRenewal_TextChanged(object sender, EventArgs e)
        {
            setParamData();
            isDirty = true;
        }

        private void txtDetectionProb_TextChanged(object sender, EventArgs e)
        {
            setParamData();
            isDirty = true;
        }
    }
}
