namespace GoE
{
    partial class frmGridGraphEditor
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
            this.mouseMenuGridGraphEditor = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setTargetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setSinkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setBlockedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setNormalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gridGraphView = new GoE.GridGameGraphViewer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnSinks = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnResize = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblCoord = new System.Windows.Forms.Label();
            this.mouseMenuGridGraphEditor.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // mouseMenuGridGraphEditor
            // 
            this.mouseMenuGridGraphEditor.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setTargetMenuItem,
            this.setSinkMenuItem,
            this.setBlockedMenuItem,
            this.setNormalToolStripMenuItem});
            this.mouseMenuGridGraphEditor.Name = "strpGridGraphEditor";
            this.mouseMenuGridGraphEditor.Size = new System.Drawing.Size(136, 92);
            // 
            // setTargetMenuItem
            // 
            this.setTargetMenuItem.Name = "setTargetMenuItem";
            this.setTargetMenuItem.Size = new System.Drawing.Size(135, 22);
            this.setTargetMenuItem.Text = "Set Target";
            this.setTargetMenuItem.Click += new System.EventHandler(this.resizeGridToolStripMenuItem_Click);
            // 
            // setSinkMenuItem
            // 
            this.setSinkMenuItem.Name = "setSinkMenuItem";
            this.setSinkMenuItem.Size = new System.Drawing.Size(135, 22);
            this.setSinkMenuItem.Text = "Set Sink";
            this.setSinkMenuItem.Click += new System.EventHandler(this.setSinkMenuItem_Click);
            // 
            // setBlockedMenuItem
            // 
            this.setBlockedMenuItem.Name = "setBlockedMenuItem";
            this.setBlockedMenuItem.Size = new System.Drawing.Size(135, 22);
            this.setBlockedMenuItem.Text = "Set Blocked";
            this.setBlockedMenuItem.Click += new System.EventHandler(this.setBlockedMenuItem_Click);
            // 
            // setNormalToolStripMenuItem
            // 
            this.setNormalToolStripMenuItem.Name = "setNormalToolStripMenuItem";
            this.setNormalToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.setNormalToolStripMenuItem.Text = "Set Normal";
            this.setNormalToolStripMenuItem.Click += new System.EventHandler(this.setNormalToolStripMenuItem_Click);
            // 
            // gridGraphView
            // 
            this.gridGraphView.CellSize = ((uint)(32u));
            this.gridGraphView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridGraphView.HeightCellCount = ((uint)(8u));
            this.gridGraphView.Location = new System.Drawing.Point(0, 0);
            this.gridGraphView.Name = "gridGraphView";
            this.gridGraphView.Size = new System.Drawing.Size(813, 412);
            this.gridGraphView.TabIndex = 3;
            this.gridGraphView.WidthCellCount = ((uint)(8u));
            this.gridGraphView.CellClick += new GoE.GridGameGraphViewer.CellClickHandler(this.gridGraphView_CellClick);
            this.gridGraphView.Load += new System.EventHandler(this.frmGridGraphEditor_load);
            this.gridGraphView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.gridGraphView_MouseMove);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblCoord);
            this.panel1.Controls.Add(this.btnSinks);
            this.panel1.Controls.Add(this.btnDone);
            this.panel1.Controls.Add(this.btnResize);
            this.panel1.Controls.Add(this.btnLoad);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 412);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(813, 34);
            this.panel1.TabIndex = 4;
            // 
            // btnSinks
            // 
            this.btnSinks.Location = new System.Drawing.Point(95, 7);
            this.btnSinks.Name = "btnSinks";
            this.btnSinks.Size = new System.Drawing.Size(75, 23);
            this.btnSinks.TabIndex = 5;
            this.btnSinks.Text = "Sink Circle";
            this.btnSinks.UseVisualStyleBackColor = true;
            this.btnSinks.Click += new System.EventHandler(this.btnSinks_Click);
            // 
            // btnDone
            // 
            this.btnDone.Location = new System.Drawing.Point(352, 6);
            this.btnDone.Name = "btnDone";
            this.btnDone.Size = new System.Drawing.Size(75, 23);
            this.btnDone.TabIndex = 4;
            this.btnDone.Text = "Done";
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // btnResize
            // 
            this.btnResize.Location = new System.Drawing.Point(13, 7);
            this.btnResize.Name = "btnResize";
            this.btnResize.Size = new System.Drawing.Size(75, 23);
            this.btnResize.TabIndex = 3;
            this.btnResize.Text = "Resize";
            this.btnResize.UseVisualStyleBackColor = true;
            this.btnResize.Click += new System.EventHandler(this.btnResize_Click);
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(190, 6);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 2;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(271, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblCoord
            // 
            this.lblCoord.AutoSize = true;
            this.lblCoord.Location = new System.Drawing.Point(726, 11);
            this.lblCoord.Name = "lblCoord";
            this.lblCoord.Size = new System.Drawing.Size(35, 13);
            this.lblCoord.TabIndex = 6;
            this.lblCoord.Text = "label1";
            // 
            // frmGridGraphEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(813, 446);
            this.Controls.Add(this.gridGraphView);
            this.Controls.Add(this.panel1);
            this.Name = "frmGridGraphEditor";
            this.Text = "GridGraphEditor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmGridGraphEditor_FormClosing);
            this.Load += new System.EventHandler(this.frmGridGraphEditor_load);
            this.mouseMenuGridGraphEditor.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip mouseMenuGridGraphEditor;
        private System.Windows.Forms.ToolStripMenuItem setTargetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setSinkMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setBlockedMenuItem;
        private GoE.GridGameGraphViewer gridGraphView;
        private System.Windows.Forms.ToolStripMenuItem setNormalToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnResize;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnSinks;
        private System.Windows.Forms.Label lblCoord;
    }
}