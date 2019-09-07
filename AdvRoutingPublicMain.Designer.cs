namespace GoE
{
    partial class AdvRoutingPublicMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdvRoutingPublicMain));
            this.txtParams = new System.Windows.Forms.RichTextBox();
            this.chkGUI = new System.Windows.Forms.CheckBox();
            this.btnStartGame = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.txtUserInput = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnResetParams = new System.Windows.Forms.Button();
            this.txtRep = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtParams
            // 
            this.txtParams.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.txtParams.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.txtParams.Location = new System.Drawing.Point(540, 18);
            this.txtParams.Name = "txtParams";
            this.txtParams.Size = new System.Drawing.Size(191, 19);
            this.txtParams.TabIndex = 1;
            this.txtParams.Text = resources.GetString("txtParams.Text");
            this.txtParams.Visible = false;
            this.txtParams.WordWrap = false;
            // 
            // chkGUI
            // 
            this.chkGUI.AutoSize = true;
            this.chkGUI.Checked = true;
            this.chkGUI.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGUI.Location = new System.Drawing.Point(131, 7);
            this.chkGUI.Name = "chkGUI";
            this.chkGUI.Size = new System.Drawing.Size(45, 17);
            this.chkGUI.TabIndex = 17;
            this.chkGUI.Text = "GUI";
            this.chkGUI.UseVisualStyleBackColor = true;
            this.chkGUI.CheckedChanged += new System.EventHandler(this.chkGUI_CheckedChanged);
            // 
            // btnStartGame
            // 
            this.btnStartGame.Location = new System.Drawing.Point(3, 3);
            this.btnStartGame.Name = "btnStartGame";
            this.btnStartGame.Size = new System.Drawing.Size(122, 23);
            this.btnStartGame.TabIndex = 16;
            this.btnStartGame.Text = "Start";
            this.btnStartGame.UseVisualStyleBackColor = true;
            this.btnStartGame.Click += new System.EventHandler(this.btnStartGame_Click_1);
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.Location = new System.Drawing.Point(3, 217);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.Size = new System.Drawing.Size(844, 169);
            this.txtOutput.TabIndex = 18;
            this.txtOutput.Text = "";
            this.txtOutput.WordWrap = false;
            // 
            // txtUserInput
            // 
            this.txtUserInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUserInput.Location = new System.Drawing.Point(3, 49);
            this.txtUserInput.Name = "txtUserInput";
            this.txtUserInput.Size = new System.Drawing.Size(844, 149);
            this.txtUserInput.TabIndex = 19;
            this.txtUserInput.Text = resources.GetString("txtUserInput.Text");
            this.txtUserInput.WordWrap = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "Parameters:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(0, 201);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Results:";
            // 
            // btnResetParams
            // 
            this.btnResetParams.Location = new System.Drawing.Point(737, 20);
            this.btnResetParams.Name = "btnResetParams";
            this.btnResetParams.Size = new System.Drawing.Size(110, 23);
            this.btnResetParams.TabIndex = 22;
            this.btnResetParams.Text = "Reset Parameters";
            this.btnResetParams.UseVisualStyleBackColor = true;
            this.btnResetParams.Click += new System.EventHandler(this.btnResetParams_Click);
            // 
            // txtRep
            // 
            this.txtRep.Enabled = false;
            this.txtRep.Location = new System.Drawing.Point(329, 5);
            this.txtRep.Name = "txtRep";
            this.txtRep.Size = new System.Drawing.Size(100, 20);
            this.txtRep.TabIndex = 23;
            this.txtRep.Text = "50";
            this.txtRep.TextChanged += new System.EventHandler(this.txtRep_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(192, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(122, 13);
            this.label3.TabIndex = 24;
            this.label3.Text = "Experiment Repetetions:";
            // 
            // AdvRoutingPublicMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(849, 398);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtRep);
            this.Controls.Add(this.btnResetParams);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtUserInput);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.chkGUI);
            this.Controls.Add(this.btnStartGame);
            this.Controls.Add(this.txtParams);
            this.Name = "AdvRoutingPublicMain";
            this.Text = "AdvRoutingPublicMain";
            this.Load += new System.EventHandler(this.AdvRoutingPublicMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtParams;
        private System.Windows.Forms.CheckBox chkGUI;
        private System.Windows.Forms.Button btnStartGame;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.RichTextBox txtUserInput;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnResetParams;
        private System.Windows.Forms.TextBox txtRep;
        private System.Windows.Forms.Label label3;
    }
}