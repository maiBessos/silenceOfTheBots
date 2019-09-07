namespace GoE
{
    partial class frmGameProcessView
    {
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlPolicyGUI = new System.Windows.Forms.Panel();
            this.btnLog = new System.Windows.Forms.Button();
            this.lblCoord = new System.Windows.Forms.Label();
            this.txtReachable = new System.Windows.Forms.TextBox();
            this.chkEvadersDontToggle = new System.Windows.Forms.CheckBox();
            this.chkPursuersDontToggle = new System.Windows.Forms.CheckBox();
            this.txtSkip = new System.Windows.Forms.TextBox();
            this.chkReachable = new System.Windows.Forms.CheckBox();
            this.lstMarks = new System.Windows.Forms.ListBox();
            this.lstChoices = new System.Windows.Forms.ListBox();
            this.lstQuestions = new System.Windows.Forms.ListBox();
            this.lstAgents = new System.Windows.Forms.ListBox();
            this.chkSensitive = new System.Windows.Forms.CheckBox();
            this.chkPursuers = new System.Windows.Forms.CheckBox();
            this.chkEvaders = new System.Windows.Forms.CheckBox();
            this.btnNext = new System.Windows.Forms.Button();
            this.guiTips = new System.Windows.Forms.ToolTip(this.components);
            this.gameView = new GoE.GridGameGraphViewer();
            this.pnlLog = new System.Windows.Forms.Panel();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.pnlPolicyGUI.SuspendLayout();
            this.pnlLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlPolicyGUI
            // 
            this.pnlPolicyGUI.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPolicyGUI.Controls.Add(this.btnLog);
            this.pnlPolicyGUI.Controls.Add(this.lblCoord);
            this.pnlPolicyGUI.Controls.Add(this.txtReachable);
            this.pnlPolicyGUI.Controls.Add(this.chkEvadersDontToggle);
            this.pnlPolicyGUI.Controls.Add(this.chkPursuersDontToggle);
            this.pnlPolicyGUI.Controls.Add(this.txtSkip);
            this.pnlPolicyGUI.Controls.Add(this.chkReachable);
            this.pnlPolicyGUI.Controls.Add(this.lstMarks);
            this.pnlPolicyGUI.Controls.Add(this.lstChoices);
            this.pnlPolicyGUI.Controls.Add(this.lstQuestions);
            this.pnlPolicyGUI.Controls.Add(this.lstAgents);
            this.pnlPolicyGUI.Controls.Add(this.chkSensitive);
            this.pnlPolicyGUI.Controls.Add(this.chkPursuers);
            this.pnlPolicyGUI.Controls.Add(this.chkEvaders);
            this.pnlPolicyGUI.Controls.Add(this.btnNext);
            this.pnlPolicyGUI.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlPolicyGUI.Location = new System.Drawing.Point(267, 0);
            this.pnlPolicyGUI.Name = "pnlPolicyGUI";
            this.pnlPolicyGUI.Size = new System.Drawing.Size(233, 668);
            this.pnlPolicyGUI.TabIndex = 1;
            // 
            // btnLog
            // 
            this.btnLog.Location = new System.Drawing.Point(155, 3);
            this.btnLog.Name = "btnLog";
            this.btnLog.Size = new System.Drawing.Size(75, 23);
            this.btnLog.TabIndex = 14;
            this.btnLog.Text = "show log >>";
            this.btnLog.UseVisualStyleBackColor = true;
            this.btnLog.Click += new System.EventHandler(this.btnLog_Click);
            // 
            // lblCoord
            // 
            this.lblCoord.AutoSize = true;
            this.lblCoord.Location = new System.Drawing.Point(186, 646);
            this.lblCoord.Name = "lblCoord";
            this.lblCoord.Size = new System.Drawing.Size(35, 13);
            this.lblCoord.TabIndex = 13;
            this.lblCoord.Text = "label1";
            // 
            // txtReachable
            // 
            this.txtReachable.Location = new System.Drawing.Point(202, 106);
            this.txtReachable.Name = "txtReachable";
            this.txtReachable.Size = new System.Drawing.Size(30, 20);
            this.txtReachable.TabIndex = 12;
            this.txtReachable.Text = "0.5";
            this.guiTips.SetToolTip(this.txtReachable, "if text box value is x, marked reachable area  assumes r_p = xr_p");
            // 
            // chkEvadersDontToggle
            // 
            this.chkEvadersDontToggle.AutoSize = true;
            this.chkEvadersDontToggle.Checked = true;
            this.chkEvadersDontToggle.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEvadersDontToggle.Location = new System.Drawing.Point(18, 39);
            this.chkEvadersDontToggle.Name = "chkEvadersDontToggle";
            this.chkEvadersDontToggle.Size = new System.Drawing.Size(15, 14);
            this.chkEvadersDontToggle.TabIndex = 11;
            this.guiTips.SetToolTip(this.chkEvadersDontToggle, "if checked, no auto toggle for mark evaders");
            this.chkEvadersDontToggle.UseVisualStyleBackColor = true;
            // 
            // chkPursuersDontToggle
            // 
            this.chkPursuersDontToggle.AutoSize = true;
            this.chkPursuersDontToggle.Checked = true;
            this.chkPursuersDontToggle.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPursuersDontToggle.Location = new System.Drawing.Point(18, 62);
            this.chkPursuersDontToggle.Name = "chkPursuersDontToggle";
            this.chkPursuersDontToggle.Size = new System.Drawing.Size(15, 14);
            this.chkPursuersDontToggle.TabIndex = 10;
            this.guiTips.SetToolTip(this.chkPursuersDontToggle, "if checked, no auto toggle for mark pursuers");
            this.chkPursuersDontToggle.UseVisualStyleBackColor = true;
            // 
            // txtSkip
            // 
            this.txtSkip.Location = new System.Drawing.Point(147, 609);
            this.txtSkip.Name = "txtSkip";
            this.txtSkip.Size = new System.Drawing.Size(74, 20);
            this.txtSkip.TabIndex = 9;
            this.txtSkip.Text = "AutoClicks#";
            this.txtSkip.TextChanged += new System.EventHandler(this.txtSkip_TextChanged);
            this.txtSkip.Enter += new System.EventHandler(this.txtSkip_Enter);
            // 
            // chkReachable
            // 
            this.chkReachable.AutoSize = true;
            this.chkReachable.Location = new System.Drawing.Point(37, 107);
            this.chkReachable.Name = "chkReachable";
            this.chkReachable.Size = new System.Drawing.Size(168, 17);
            this.chkReachable.TabIndex = 8;
            this.chkReachable.Text = "Mark Pursuer-Reachable area";
            this.chkReachable.UseVisualStyleBackColor = true;
            this.chkReachable.CheckedChanged += new System.EventHandler(this.chkReachable_CheckedChanged);
            // 
            // lstMarks
            // 
            this.lstMarks.FormattingEnabled = true;
            this.lstMarks.HorizontalScrollbar = true;
            this.lstMarks.Location = new System.Drawing.Point(4, 252);
            this.lstMarks.Name = "lstMarks";
            this.lstMarks.Size = new System.Drawing.Size(217, 56);
            this.lstMarks.TabIndex = 7;
            this.lstMarks.SelectedIndexChanged += new System.EventHandler(this.lstMarks_SelectedIndexChanged);
            // 
            // lstChoices
            // 
            this.lstChoices.FormattingEnabled = true;
            this.lstChoices.HorizontalScrollbar = true;
            this.lstChoices.Location = new System.Drawing.Point(7, 514);
            this.lstChoices.Name = "lstChoices";
            this.lstChoices.ScrollAlwaysVisible = true;
            this.lstChoices.Size = new System.Drawing.Size(214, 95);
            this.lstChoices.TabIndex = 6;
            this.lstChoices.SelectedIndexChanged += new System.EventHandler(this.lstChoices_SelectedIndexChanged);
            // 
            // lstQuestions
            // 
            this.lstQuestions.FormattingEnabled = true;
            this.lstQuestions.HorizontalScrollbar = true;
            this.lstQuestions.Location = new System.Drawing.Point(7, 413);
            this.lstQuestions.Name = "lstQuestions";
            this.lstQuestions.ScrollAlwaysVisible = true;
            this.lstQuestions.Size = new System.Drawing.Size(214, 95);
            this.lstQuestions.TabIndex = 5;
            this.lstQuestions.SelectedIndexChanged += new System.EventHandler(this.lstQuestions_SelectedIndexChanged);
            // 
            // lstAgents
            // 
            this.lstAgents.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstAgents.FormattingEnabled = true;
            this.lstAgents.HorizontalScrollbar = true;
            this.lstAgents.Location = new System.Drawing.Point(7, 312);
            this.lstAgents.Name = "lstAgents";
            this.lstAgents.ScrollAlwaysVisible = true;
            this.lstAgents.Size = new System.Drawing.Size(212, 95);
            this.lstAgents.TabIndex = 4;
            this.lstAgents.SelectedIndexChanged += new System.EventHandler(this.lstAgents_SelectedIndexChanged);
            // 
            // chkSensitive
            // 
            this.chkSensitive.AutoSize = true;
            this.chkSensitive.Location = new System.Drawing.Point(37, 84);
            this.chkSensitive.Name = "chkSensitive";
            this.chkSensitive.Size = new System.Drawing.Size(121, 17);
            this.chkSensitive.TabIndex = 3;
            this.chkSensitive.Text = "Mark Sensitive Area";
            this.chkSensitive.ThreeState = true;
            this.chkSensitive.UseVisualStyleBackColor = true;
            this.chkSensitive.CheckStateChanged += new System.EventHandler(this.chkSensitive_CheckStateChanged);
            // 
            // chkPursuers
            // 
            this.chkPursuers.AutoSize = true;
            this.chkPursuers.Location = new System.Drawing.Point(37, 61);
            this.chkPursuers.Name = "chkPursuers";
            this.chkPursuers.Size = new System.Drawing.Size(106, 17);
            this.chkPursuers.TabIndex = 2;
            this.chkPursuers.Text = "Mark all pursuers";
            this.chkPursuers.UseVisualStyleBackColor = true;
            this.chkPursuers.CheckedChanged += new System.EventHandler(this.chkPursuers_CheckedChanged);
            // 
            // chkEvaders
            // 
            this.chkEvaders.AutoSize = true;
            this.chkEvaders.Location = new System.Drawing.Point(37, 38);
            this.chkEvaders.Name = "chkEvaders";
            this.chkEvaders.Size = new System.Drawing.Size(104, 17);
            this.chkEvaders.TabIndex = 1;
            this.chkEvaders.Text = "Mark all evaders";
            this.chkEvaders.UseVisualStyleBackColor = true;
            this.chkEvaders.CheckedChanged += new System.EventHandler(this.chkEvaders_CheckedChanged);
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(7, 607);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(134, 23);
            this.btnNext.TabIndex = 0;
            this.btnNext.Text = "Invoke Pursuer Policy";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // guiTips
            // 
            this.guiTips.Popup += new System.Windows.Forms.PopupEventHandler(this.guiTips_Popup);
            // 
            // gameView
            // 
            this.gameView.CellSize = ((uint)(32u));
            this.gameView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gameView.HeightCellCount = ((uint)(8u));
            this.gameView.Location = new System.Drawing.Point(0, 0);
            this.gameView.Name = "gameView";
            this.gameView.Size = new System.Drawing.Size(267, 668);
            this.gameView.TabIndex = 0;
            this.guiTips.SetToolTip(this.gameView, "ctrl + mouse wheel for zoom (+Alt for speedup)");
            this.gameView.WidthCellCount = ((uint)(8u));
            this.gameView.CellClick += new GoE.GridGameGraphViewer.CellClickHandler(this.gameView_CellClick);
            this.gameView.Load += new System.EventHandler(this.gameView_Load);
            this.gameView.SizeChanged += new System.EventHandler(this.gameView_SizeChanged);
            this.gameView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.gameView_KeyDown);
            this.gameView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.gameView_KeyUp);
            this.gameView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.gameView_MouseDown);
            this.gameView.MouseHover += new System.EventHandler(this.gameView_MouseHover);
            this.gameView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.gameView_MouseMove);
            // 
            // pnlLog
            // 
            this.pnlLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLog.Controls.Add(this.lstLog);
            this.pnlLog.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlLog.Location = new System.Drawing.Point(500, 0);
            this.pnlLog.Name = "pnlLog";
            this.pnlLog.Size = new System.Drawing.Size(327, 668);
            this.pnlLog.TabIndex = 2;
            this.pnlLog.Visible = false;
            this.pnlLog.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pnlLog_MouseMove);
            // 
            // lstLog
            // 
            this.lstLog.Cursor = System.Windows.Forms.Cursors.Default;
            this.lstLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lstLog.FormattingEnabled = true;
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.Location = new System.Drawing.Point(0, 12);
            this.lstLog.Name = "lstLog";
            this.lstLog.Size = new System.Drawing.Size(325, 654);
            this.lstLog.TabIndex = 18;
            // 
            // frmGameProcessView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 668);
            this.Controls.Add(this.gameView);
            this.Controls.Add(this.pnlPolicyGUI);
            this.Controls.Add(this.pnlLog);
            this.DoubleBuffered = true;
            this.Name = "frmGameProcessView";
            this.Text = "GameProcessView";
            this.Load += new System.EventHandler(this.GameProcessView_Load);
            this.pnlPolicyGUI.ResumeLayout(false);
            this.pnlPolicyGUI.PerformLayout();
            this.pnlLog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private GoE.GridGameGraphViewer gameView;
        private System.Windows.Forms.Panel pnlPolicyGUI;
        private System.Windows.Forms.CheckBox chkSensitive;
        private System.Windows.Forms.CheckBox chkPursuers;
        private System.Windows.Forms.CheckBox chkEvaders;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.ListBox lstChoices;
        private System.Windows.Forms.ListBox lstQuestions;
        private System.Windows.Forms.ListBox lstAgents;
        private System.Windows.Forms.ListBox lstMarks;
        private System.Windows.Forms.CheckBox chkReachable;
        private System.Windows.Forms.TextBox txtSkip;
        private System.Windows.Forms.ToolTip guiTips;
        private System.Windows.Forms.CheckBox chkEvadersDontToggle;
        private System.Windows.Forms.CheckBox chkPursuersDontToggle;
        private System.Windows.Forms.TextBox txtReachable;
        private System.Windows.Forms.Label lblCoord;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Panel pnlLog;
        private System.Windows.Forms.ListBox lstLog;
    }
}