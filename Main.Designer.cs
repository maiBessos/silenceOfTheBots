namespace GoE
{
    partial class frmMain
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
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.chkExceptions = new System.Windows.Forms.CheckBox();
            this.btnGenParams = new System.Windows.Forms.Button();
            this.txtSearchParams = new System.Windows.Forms.TextBox();
            this.btnSrchParam = new System.Windows.Forms.Button();
            this.btnTests = new System.Windows.Forms.Button();
            this.cmdGeneticPoliciesManager = new System.Windows.Forms.Button();
            this.chkGUI = new System.Windows.Forms.CheckBox();
            this.btnUtils = new System.Windows.Forms.Button();
            this.btnStartGame = new System.Windows.Forms.Button();
            this.btnLoadGraph = new System.Windows.Forms.Button();
            this.btnGenGraph = new System.Windows.Forms.Button();
            this.btnGenParam = new System.Windows.Forms.Button();
            this.btnLoadParam = new System.Windows.Forms.Button();
            this.pnlText = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cmbGameType = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbOptimizer = new System.Windows.Forms.ComboBox();
            this.cmbEvaderPolicy = new System.Windows.Forms.ComboBox();
            this.cmbPursuerPolicy = new System.Windows.Forms.ComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tabControlProcess = new System.Windows.Forms.TabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.txtParams = new System.Windows.Forms.RichTextBox();
            this.tabPageOut = new System.Windows.Forms.TabPage();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.tabPageOutputTable = new System.Windows.Forms.TabPage();
            this.tblOutput = new System.Windows.Forms.DataGridView();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.mainToolTips = new System.Windows.Forms.ToolTip(this.components);
            this.pnlButtons.SuspendLayout();
            this.pnlText.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabControlProcess.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPageOut.SuspendLayout();
            this.tabPageOutputTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tblOutput)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.chkExceptions);
            this.pnlButtons.Controls.Add(this.btnGenParams);
            this.pnlButtons.Controls.Add(this.txtSearchParams);
            this.pnlButtons.Controls.Add(this.btnSrchParam);
            this.pnlButtons.Controls.Add(this.btnTests);
            this.pnlButtons.Controls.Add(this.cmdGeneticPoliciesManager);
            this.pnlButtons.Controls.Add(this.chkGUI);
            this.pnlButtons.Controls.Add(this.btnUtils);
            this.pnlButtons.Controls.Add(this.btnStartGame);
            this.pnlButtons.Controls.Add(this.btnLoadGraph);
            this.pnlButtons.Controls.Add(this.btnGenGraph);
            this.pnlButtons.Controls.Add(this.btnGenParam);
            this.pnlButtons.Controls.Add(this.btnLoadParam);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlButtons.Location = new System.Drawing.Point(0, 0);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(179, 590);
            this.pnlButtons.TabIndex = 7;
            this.pnlButtons.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlButtons_Paint);
            // 
            // chkExceptions
            // 
            this.chkExceptions.AutoSize = true;
            this.chkExceptions.Location = new System.Drawing.Point(14, 359);
            this.chkExceptions.Name = "chkExceptions";
            this.chkExceptions.Size = new System.Drawing.Size(150, 17);
            this.chkExceptions.TabIndex = 21;
            this.chkExceptions.Text = "catch top level exceptions";
            this.chkExceptions.UseVisualStyleBackColor = true;
            // 
            // btnGenParams
            // 
            this.btnGenParams.Location = new System.Drawing.Point(0, 529);
            this.btnGenParams.Name = "btnGenParams";
            this.btnGenParams.Size = new System.Drawing.Size(122, 23);
            this.btnGenParams.TabIndex = 20;
            this.btnGenParams.Text = "Generate Parm Files";
            this.btnGenParams.UseVisualStyleBackColor = true;
            this.btnGenParams.Click += new System.EventHandler(this.btnGenParams_Click);
            // 
            // txtSearchParams
            // 
            this.txtSearchParams.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtSearchParams.Location = new System.Drawing.Point(0, 570);
            this.txtSearchParams.Name = "txtSearchParams";
            this.txtSearchParams.Size = new System.Drawing.Size(179, 20);
            this.txtSearchParams.TabIndex = 19;
            this.txtSearchParams.Visible = false;
            this.txtSearchParams.TextChanged += new System.EventHandler(this.txtSearchParams_TextChanged);
            this.txtSearchParams.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearchParams_KeyDown);
            // 
            // btnSrchParam
            // 
            this.btnSrchParam.Location = new System.Drawing.Point(0, 500);
            this.btnSrchParam.Name = "btnSrchParam";
            this.btnSrchParam.Size = new System.Drawing.Size(160, 23);
            this.btnSrchParam.TabIndex = 18;
            this.btnSrchParam.Text = "Search Params";
            this.btnSrchParam.UseVisualStyleBackColor = true;
            this.btnSrchParam.Click += new System.EventHandler(this.btnSrchParam_Click);
            // 
            // btnTests
            // 
            this.btnTests.Location = new System.Drawing.Point(4, 421);
            this.btnTests.Name = "btnTests";
            this.btnTests.Size = new System.Drawing.Size(75, 23);
            this.btnTests.TabIndex = 17;
            this.btnTests.Text = "run tests";
            this.btnTests.UseVisualStyleBackColor = true;
            this.btnTests.Click += new System.EventHandler(this.btnTests_Click);
            // 
            // cmdGeneticPoliciesManager
            // 
            this.cmdGeneticPoliciesManager.Location = new System.Drawing.Point(2, 446);
            this.cmdGeneticPoliciesManager.Name = "cmdGeneticPoliciesManager";
            this.cmdGeneticPoliciesManager.Size = new System.Drawing.Size(162, 23);
            this.cmdGeneticPoliciesManager.TabIndex = 16;
            this.cmdGeneticPoliciesManager.Text = "Genetic Policies Manager";
            this.cmdGeneticPoliciesManager.UseVisualStyleBackColor = true;
            this.cmdGeneticPoliciesManager.Click += new System.EventHandler(this.cmdGeneticPoliciesManager_Click);
            // 
            // chkGUI
            // 
            this.chkGUI.AutoSize = true;
            this.chkGUI.Checked = true;
            this.chkGUI.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGUI.Location = new System.Drawing.Point(128, 391);
            this.chkGUI.Name = "chkGUI";
            this.chkGUI.Size = new System.Drawing.Size(45, 17);
            this.chkGUI.TabIndex = 15;
            this.chkGUI.Text = "GUI";
            this.chkGUI.UseVisualStyleBackColor = true;
            // 
            // btnUtils
            // 
            this.btnUtils.Location = new System.Drawing.Point(4, 471);
            this.btnUtils.Name = "btnUtils";
            this.btnUtils.Size = new System.Drawing.Size(160, 23);
            this.btnUtils.TabIndex = 14;
            this.btnUtils.Text = "Utilities";
            this.btnUtils.UseVisualStyleBackColor = true;
            this.btnUtils.Click += new System.EventHandler(this.btnUtils_Click);
            // 
            // btnStartGame
            // 
            this.btnStartGame.Location = new System.Drawing.Point(4, 391);
            this.btnStartGame.Name = "btnStartGame";
            this.btnStartGame.Size = new System.Drawing.Size(122, 23);
            this.btnStartGame.TabIndex = 11;
            this.btnStartGame.Text = "Start New Game";
            this.btnStartGame.UseVisualStyleBackColor = true;
            this.btnStartGame.Click += new System.EventHandler(this.btnStartGame_Click);
            // 
            // btnLoadGraph
            // 
            this.btnLoadGraph.Location = new System.Drawing.Point(0, 95);
            this.btnLoadGraph.Name = "btnLoadGraph";
            this.btnLoadGraph.Size = new System.Drawing.Size(160, 23);
            this.btnLoadGraph.TabIndex = 10;
            this.btnLoadGraph.Text = "Load Graph File";
            this.btnLoadGraph.UseVisualStyleBackColor = true;
            this.btnLoadGraph.Click += new System.EventHandler(this.btnLoadGraph_Click);
            // 
            // btnGenGraph
            // 
            this.btnGenGraph.Location = new System.Drawing.Point(0, 63);
            this.btnGenGraph.Name = "btnGenGraph";
            this.btnGenGraph.Size = new System.Drawing.Size(160, 23);
            this.btnGenGraph.TabIndex = 9;
            this.btnGenGraph.Text = "Generate Graph File";
            this.btnGenGraph.UseVisualStyleBackColor = true;
            this.btnGenGraph.Click += new System.EventHandler(this.btnGenGraph_Click);
            // 
            // btnGenParam
            // 
            this.btnGenParam.Location = new System.Drawing.Point(0, 5);
            this.btnGenParam.Name = "btnGenParam";
            this.btnGenParam.Size = new System.Drawing.Size(160, 23);
            this.btnGenParam.TabIndex = 8;
            this.btnGenParam.Text = "Generate Game Params File";
            this.btnGenParam.UseVisualStyleBackColor = true;
            this.btnGenParam.Click += new System.EventHandler(this.btnGenParam_Click);
            // 
            // btnLoadParam
            // 
            this.btnLoadParam.Location = new System.Drawing.Point(0, 34);
            this.btnLoadParam.Name = "btnLoadParam";
            this.btnLoadParam.Size = new System.Drawing.Size(160, 23);
            this.btnLoadParam.TabIndex = 7;
            this.btnLoadParam.Text = "Load Game Params File";
            this.btnLoadParam.UseVisualStyleBackColor = true;
            this.btnLoadParam.Click += new System.EventHandler(this.btnLoadParam_Click);
            // 
            // pnlText
            // 
            this.pnlText.Controls.Add(this.panel1);
            this.pnlText.Controls.Add(this.panel2);
            this.pnlText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlText.Location = new System.Drawing.Point(179, 0);
            this.pnlText.Name = "pnlText";
            this.pnlText.Size = new System.Drawing.Size(629, 590);
            this.pnlText.TabIndex = 8;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cmbGameType);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.cmbOptimizer);
            this.panel1.Controls.Add(this.cmbEvaderPolicy);
            this.panel1.Controls.Add(this.cmbPursuerPolicy);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 149);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(629, 143);
            this.panel1.TabIndex = 20;
            // 
            // cmbGameType
            // 
            this.cmbGameType.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbGameType.FormattingEnabled = true;
            this.cmbGameType.Location = new System.Drawing.Point(160, 103);
            this.cmbGameType.Name = "cmbGameType";
            this.cmbGameType.Size = new System.Drawing.Size(460, 21);
            this.cmbGameType.TabIndex = 27;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 109);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "Game Type";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 13);
            this.label3.TabIndex = 24;
            this.label3.Text = "Optimizer";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 23;
            this.label2.Text = "Evader Policy:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "Pursuer Policy:";
            // 
            // cmbOptimizer
            // 
            this.cmbOptimizer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbOptimizer.FormattingEnabled = true;
            this.cmbOptimizer.Location = new System.Drawing.Point(160, 76);
            this.cmbOptimizer.Name = "cmbOptimizer";
            this.cmbOptimizer.Size = new System.Drawing.Size(460, 21);
            this.cmbOptimizer.TabIndex = 21;
            // 
            // cmbEvaderPolicy
            // 
            this.cmbEvaderPolicy.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbEvaderPolicy.FormattingEnabled = true;
            this.cmbEvaderPolicy.Location = new System.Drawing.Point(160, 49);
            this.cmbEvaderPolicy.Name = "cmbEvaderPolicy";
            this.cmbEvaderPolicy.Size = new System.Drawing.Size(460, 21);
            this.cmbEvaderPolicy.TabIndex = 20;
            // 
            // cmbPursuerPolicy
            // 
            this.cmbPursuerPolicy.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPursuerPolicy.FormattingEnabled = true;
            this.cmbPursuerPolicy.Location = new System.Drawing.Point(160, 22);
            this.cmbPursuerPolicy.Name = "cmbPursuerPolicy";
            this.cmbPursuerPolicy.Size = new System.Drawing.Size(460, 21);
            this.cmbPursuerPolicy.TabIndex = 19;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tabControlProcess);
            this.panel2.Controls.Add(this.lstLog);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 292);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(629, 298);
            this.panel2.TabIndex = 19;
            // 
            // tabControlProcess
            // 
            this.tabControlProcess.Controls.Add(this.tabPage2);
            this.tabControlProcess.Controls.Add(this.tabPageOut);
            this.tabControlProcess.Controls.Add(this.tabPageOutputTable);
            this.tabControlProcess.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlProcess.Location = new System.Drawing.Point(0, 0);
            this.tabControlProcess.Name = "tabControlProcess";
            this.tabControlProcess.SelectedIndex = 0;
            this.tabControlProcess.Size = new System.Drawing.Size(394, 298);
            this.tabControlProcess.TabIndex = 19;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.txtParams);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(386, 272);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Process Params";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // txtParams
            // 
            this.txtParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtParams.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.txtParams.Location = new System.Drawing.Point(3, 3);
            this.txtParams.Name = "txtParams";
            this.txtParams.Size = new System.Drawing.Size(380, 266);
            this.txtParams.TabIndex = 0;
            this.txtParams.Text = "";
            this.txtParams.WordWrap = false;
            this.txtParams.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtParams_KeyDown);
            // 
            // tabPageOut
            // 
            this.tabPageOut.Controls.Add(this.txtOutput);
            this.tabPageOut.Location = new System.Drawing.Point(4, 22);
            this.tabPageOut.Name = "tabPageOut";
            this.tabPageOut.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOut.Size = new System.Drawing.Size(386, 272);
            this.tabPageOut.TabIndex = 2;
            this.tabPageOut.Text = "Output";
            this.tabPageOut.UseVisualStyleBackColor = true;
            // 
            // txtOutput
            // 
            this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOutput.Location = new System.Drawing.Point(3, 3);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.Size = new System.Drawing.Size(380, 266);
            this.txtOutput.TabIndex = 0;
            this.txtOutput.Text = "";
            this.mainToolTips.SetToolTip(this.txtOutput, "use ctrl+c/ctrl+v to populate \'OutputTable\' tab");
            this.txtOutput.WordWrap = false;
            this.txtOutput.TextChanged += new System.EventHandler(this.txtOutput_TextChanged);
            this.txtOutput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtOutput_KeyDown);
            // 
            // tabPageOutputTable
            // 
            this.tabPageOutputTable.Controls.Add(this.tblOutput);
            this.tabPageOutputTable.Location = new System.Drawing.Point(4, 22);
            this.tabPageOutputTable.Name = "tabPageOutputTable";
            this.tabPageOutputTable.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOutputTable.Size = new System.Drawing.Size(386, 272);
            this.tabPageOutputTable.TabIndex = 3;
            this.tabPageOutputTable.Text = "OutputTable";
            this.tabPageOutputTable.UseVisualStyleBackColor = true;
            // 
            // tblOutput
            // 
            this.tblOutput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tblOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblOutput.Location = new System.Drawing.Point(3, 3);
            this.tblOutput.Name = "tblOutput";
            this.tblOutput.Size = new System.Drawing.Size(380, 266);
            this.tblOutput.TabIndex = 0;
            this.tblOutput.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.tblOutput_CellContentClick);
            // 
            // lstLog
            // 
            this.lstLog.Dock = System.Windows.Forms.DockStyle.Right;
            this.lstLog.FormattingEnabled = true;
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.Location = new System.Drawing.Point(394, 0);
            this.lstLog.Name = "lstLog";
            this.lstLog.ScrollAlwaysVisible = true;
            this.lstLog.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lstLog.Size = new System.Drawing.Size(235, 298);
            this.lstLog.TabIndex = 18;
            this.mainToolTips.SetToolTip(this.lstLog, "use ctrl+c to copy multi-line selection");
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(808, 590);
            this.Controls.Add(this.pnlText);
            this.Controls.Add(this.pnlButtons);
            this.Name = "frmMain";
            this.Text = "Main";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.pnlButtons.ResumeLayout(false);
            this.pnlButtons.PerformLayout();
            this.pnlText.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.tabControlProcess.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPageOut.ResumeLayout(false);
            this.tabPageOutputTable.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tblOutput)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnLoadGraph;
        private System.Windows.Forms.Button btnGenGraph;
        private System.Windows.Forms.Button btnGenParam;
        private System.Windows.Forms.Button btnLoadParam;
        private System.Windows.Forms.Panel pnlText;
        private System.Windows.Forms.Button btnUtils;
        private System.Windows.Forms.Button cmdGeneticPoliciesManager;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox cmbOptimizer;
        private System.Windows.Forms.ComboBox cmbEvaderPolicy;
        private System.Windows.Forms.ComboBox cmbPursuerPolicy;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TabControl tabControlProcess;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPageOut;
        private System.Windows.Forms.ListBox lstLog;
        private System.Windows.Forms.CheckBox chkGUI;
        private System.Windows.Forms.Button btnStartGame;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbGameType;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TabPage tabPageOutputTable;
        private System.Windows.Forms.DataGridView tblOutput;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.RichTextBox txtParams;
        private System.Windows.Forms.Button btnTests;
        private System.Windows.Forms.Button btnSrchParam;
        private System.Windows.Forms.TextBox txtSearchParams;
        private System.Windows.Forms.Button btnGenParams;
        private System.Windows.Forms.CheckBox chkExceptions;
        private System.Windows.Forms.ToolTip mainToolTips;
    }
}

