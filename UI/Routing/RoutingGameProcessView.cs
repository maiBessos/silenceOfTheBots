using GoE.GameLogic;
using GoE.UI;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.Utils.Algorithms;
using GoE.Utils.Extensions;

namespace GoE
{
    public partial class frmFrontsGridRoutingGameProcessView : Form, IGameProcessGUI
    {
        #region designer code
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
            this.chkEvadersDontToggle = new System.Windows.Forms.CheckBox();
            this.chkPursuersDontToggle = new System.Windows.Forms.CheckBox();
            this.txtSkip = new System.Windows.Forms.TextBox();
            this.lstMarks = new System.Windows.Forms.ListBox();
            this.lstChoices = new System.Windows.Forms.ListBox();
            this.lstQuestions = new System.Windows.Forms.ListBox();
            this.lstAgents = new System.Windows.Forms.ListBox();
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
            this.pnlPolicyGUI.Controls.Add(this.chkEvadersDontToggle);
            this.pnlPolicyGUI.Controls.Add(this.chkPursuersDontToggle);
            this.pnlPolicyGUI.Controls.Add(this.txtSkip);
            this.pnlPolicyGUI.Controls.Add(this.lstMarks);
            this.pnlPolicyGUI.Controls.Add(this.lstChoices);
            this.pnlPolicyGUI.Controls.Add(this.lstQuestions);
            this.pnlPolicyGUI.Controls.Add(this.lstAgents);
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
            // lstMarks
            // 
            this.lstMarks.FormattingEnabled = true;
            this.lstMarks.HorizontalScrollbar = true;
            this.lstMarks.Location = new System.Drawing.Point(9, 169);
            this.lstMarks.Name = "lstMarks";
            this.lstMarks.Size = new System.Drawing.Size(217, 95);
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
            this.lstChoices.Size = new System.Drawing.Size(221, 95);
            this.lstChoices.TabIndex = 6;
            this.lstChoices.SelectedIndexChanged += new System.EventHandler(this.lstChoices_SelectedIndexChanged);
            // 
            // lstQuestions
            // 
            this.lstQuestions.FormattingEnabled = true;
            this.lstQuestions.HorizontalScrollbar = true;
            this.lstQuestions.Location = new System.Drawing.Point(9, 387);
            this.lstQuestions.Name = "lstQuestions";
            this.lstQuestions.ScrollAlwaysVisible = true;
            this.lstQuestions.Size = new System.Drawing.Size(218, 121);
            this.lstQuestions.TabIndex = 5;
            this.lstQuestions.SelectedIndexChanged += new System.EventHandler(this.lstQuestions_SelectedIndexChanged);
            // 
            // lstAgents
            // 
            this.lstAgents.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstAgents.FormattingEnabled = true;
            this.lstAgents.HorizontalScrollbar = true;
            this.lstAgents.Location = new System.Drawing.Point(7, 270);
            this.lstAgents.Name = "lstAgents";
            this.lstAgents.ScrollAlwaysVisible = true;
            this.lstAgents.Size = new System.Drawing.Size(219, 108);
            this.lstAgents.TabIndex = 4;
            this.lstAgents.SelectedIndexChanged += new System.EventHandler(this.lstAgents_SelectedIndexChanged);
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
            // frmRoutingGameProcessView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 668);
            this.Controls.Add(this.gameView);
            this.Controls.Add(this.pnlPolicyGUI);
            this.Controls.Add(this.pnlLog);
            this.DoubleBuffered = true;
            this.Name = "frmRoutingGameProcessView";
            this.Text = "Routing Game Process View";
            this.Load += new System.EventHandler(this.GameProcessView_Load);
            this.ResizeEnd += new System.EventHandler(this.frmRoutingGameProcessView_ResizeEnd);
            this.pnlPolicyGUI.ResumeLayout(false);
            this.pnlPolicyGUI.PerformLayout();
            this.pnlLog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private GoE.GridGameGraphViewer gameView;
        private System.Windows.Forms.Panel pnlPolicyGUI;
        private System.Windows.Forms.CheckBox chkPursuers;
        private System.Windows.Forms.CheckBox chkEvaders;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.ListBox lstChoices;
        private System.Windows.Forms.ListBox lstQuestions;
        private System.Windows.Forms.ListBox lstAgents;
        private System.Windows.Forms.ListBox lstMarks;
        private System.Windows.Forms.TextBox txtSkip;
        private System.Windows.Forms.ToolTip guiTips;
        private System.Windows.Forms.CheckBox chkEvadersDontToggle;
        private System.Windows.Forms.CheckBox chkPursuersDontToggle;
        private System.Windows.Forms.Label lblCoord;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Panel pnlLog;
        private System.Windows.Forms.ListBox lstLog;


        #endregion

        private List<string> logLines = new List<string>();
        private bool isAltPressed = false;
        private bool isCtrlPressed = false;
        private bool pursuerTurn = true;
        private GridGameGraphController gridController = null;
        private GridGameGraph graph;
        private GameLogic.FrontsGridRoutingGameProcess gameProc;

        
        private List<Point> displayedEvaderLocations = new List<Point>();
        private List<Point> displayedPursuerLocations = new List<Point>();
        private Dictionary<string, List<Point>> displayedPolicyMarksLocations = new Dictionary<string, List<Point>>();
        private List<string> marklistIndexToDictionaryKey = new List<string>(); // maps from items in lstMarks to key values of 'displayedPolicyMarksLocations'
        Dictionary<IAgent, Tuple<Point, Point>> currentMovements = new Dictionary<IAgent, Tuple<Point, Point>>();

        private List<IAgent> agentRequest;
        private InputRequest latestInputRequest = null;

        Dictionary<IAgent, Point> agentLocationAnswer = new Dictionary<IAgent, Point>();
        Dictionary<Tuple<IAgent, int>, object> agentQuestionToAnswer = new Dictionary<Tuple<IAgent, int>, object>();

        public void debugStopSkippingRound() 
        {
            skipCount = Math.Min(skipCount,1);
        }
        public bool hasBoardGUI()
        {
            return true;
        }

        public void init(AGameGraph grGraph, AGameProcess p)
        {
            gameProc = (FrontsGridRoutingGameProcess) p;
            graph = (GridGameGraph)grGraph;

            foreach (Pursuer pr in gameProc.Params.A_P)
            {
                agentLocationAnswer[pr] = new Point(-1, -1);
                //agentLocationAnswer[pr].Add(new Location(GameLogic.Location.Type.Unset));
            }
            foreach (Evader ev in gameProc.Params.A_E)
            {
                agentLocationAnswer[ev] = new Point(-1, -1);
                //agentLocationAnswer[ev].Add(new Location(GameLogic.Location.Type.Unset));
            }
        }
        public frmFrontsGridRoutingGameProcessView()
        {
            InitializeComponent();
            
        }

        //public void addLogValue(string line)
        //{
        //    logLines.Add(line);
        //}

        
        public void flushLog()
        {
            string msg = "";
            foreach (string line in logLines)
                msg += line + "\n";
        }

        int skipCount = 1;

        // FIXME remove
        //HashSet<Point> prevPursuervisits = new HashSet<Point>();
        //Dictionary<Point, Pursuer> ptop = new Dictionary<Point, Pursuer>();
        //Dictionary<Point, Pursuer> prevptop = new Dictionary<Point, Pursuer>();

        private void btnNext_Click(object sender, EventArgs e)
        {
            userSelection = false; // changes to gui may occur within this function (populating/clearing/selection in controls) - but 
                                   // we may need to separate when these actions are done by the user and when not

            if (!Int32.TryParse(txtSkip.Text, out skipCount))
            {
                txtSkip.Text = "1";
                skipCount = 1;
            }
            
            do
            {
                pursuerTurn = !pursuerTurn;

                if (skipCount == 1)
                {
                    chkEvaders_CheckedChanged(sender, e);
                    chkPursuers_CheckedChanged(sender, e);
                    lstAgents.Items.Clear();
                    lstMarks.Items.Clear();
                    displayedPolicyMarksLocations = new Dictionary<string, List<Point>>();
                    lstQuestions.Enabled = lstAgents.Enabled = lstChoices.Enabled = false;
                    currentMovements = new Dictionary<IAgent, Tuple<Point, Point>>();
                    gridController.setMovement(new Point(-1, -1), new Point(-1, -1));
                }

                try
                {
                    if (gameProc.invokeNextPolicy() == false)
                    {
                        MessageBox.Show("game ended. Evader utility: " + gameProc.GameResultReward.ToString()+ " .Remaining evaders:" + (gameProc.Params.A_E.Count - gameProc.CapturedEvaders).ToString());
                        this.Close();
                        return;
                    }
                }
                catch (AlgorithmException ex)
                {
                    MessageBox.Show(ex.Message);
                    this.Close();
                }

                if (skipCount > 1)
                    continue;

                // update check boxes/ display data :
                if (pursuerTurn)
                {
                    btnNext.Text = "Invoke Pursuers Policy";
                    if (!chkEvadersDontToggle.Checked)
                        chkEvaders.Checked = false; // invokes an event that clears  displayedEvaderLocations 
                    else
                    {
                        chkEvaders_CheckedChanged(null, null); // refresh list
                    }

                    if (!chkPursuersDontToggle.Checked)
                        chkPursuers.Checked = true;
                    else
                    {
                        chkPursuers_CheckedChanged(null, null); // refresh list
                    
                    }

                    if (pursuersMarkedItemIdx != -1)
                        lstMarks.SelectedIndex = pursuersMarkedItemIdx;
                }
                else
                {
                    btnNext.Text = "Invoke Evaders Policy";
                    
                    if(!chkEvadersDontToggle.Checked)
                        chkEvaders.Checked = true;
                    else
                    {
                        chkEvaders_CheckedChanged(null, null); // refresh list
                    }

                    if (!chkPursuersDontToggle.Checked)
                        chkPursuers.Checked = false; // invokes an event that clears displayedPursuerLocations
                    else
                    {
                        chkPursuers_CheckedChanged(null, null); // refresh list
                    }

                    if (evadersMarkedItemIdx != -1)
                        lstMarks.SelectedIndex = evadersMarkedItemIdx;

                    // fixme remove below
                    //var nextVistis= new HashSet<Point>();
                    //ptop = new Dictionary<Point,Pursuer>();
                    //foreach (var v in gameProc.LatestPursuerLocations)
                    //    foreach (var vp in v.Value)
                    //    {
                    //        nextVistis.Add(vp);
                    //        ptop[vp] = v.Key;
                    //    }
                    //if(nextVistis.Intersect(prevPursuervisits ) .Count() > 0)
                    //{
                    //    int a = 0; 
                    //    ++a;
                    //}
                    //prevPursuervisits = nextVistis;
                    //prevptop = ptop;

                }
               
            }
            while ((--skipCount) > 0);

            refreshGridView();

            userSelection = true;
        }

        bool manualZoom = false;
        private void GameProcessView_Load(object sender, EventArgs e)
        {
            
            gameView.resizeGrid(graph.WidthCellCount, graph.HeightCellCount, 10, false);
            gridController = new GridGameGraphController(gameView, graph);
            gridController.View.Invalidate();
            this.MouseWheel += frmRoutingGameProcessView_MouseWheel;

        }

        void frmRoutingGameProcessView_MouseWheel(object sender, MouseEventArgs e)
        {
            
            if (!isCtrlPressed)
                return;

            if (e.Delta > 0 && gameView.CellSize > 1)
            {
                gameView.CellSize--;
                if (isAltPressed)
                    gameView.CellSize -= 4;
                manualZoom = true;
                refreshGridView();
            }
            else if (e.Delta < 0)
            {
                gameView.CellSize++;
                if (isAltPressed)
                    gameView.CellSize += 4;
                manualZoom = true;
                refreshGridView();
            }
        }

        /// <summary>
        /// lists the marks added by the policy
        /// </summary>
        public void markLocations(Dictionary<string, List<PointF>> locations)
        {
            if (skipCount > 1)
                return; // no point in doing gui stuff, if we are doing a few rounds in a row

            lstMarks.Items.Clear();
            marklistIndexToDictionaryKey = new List<string>();

            displayedPolicyMarksLocations = locations.toPointMarkings();
            string noMarksKeyString = "Hide policy's marked points";
            displayedPolicyMarksLocations.Add("Hide policy's marked points", new List<Point>());

            displayedPolicyMarksLocations["Transmitted E-bots"] = gameProc.AllTransmittingEvaders;
            displayedPolicyMarksLocations["Detected E-bots"] = gameProc.DetectedEvaders;

            foreach (var s in displayedPolicyMarksLocations)
            {
                string visibleString = "Mark " + s.Key + " - [" + s.Value.Count.ToString() + "]";
                marklistIndexToDictionaryKey.Add(s.Key);
                lstMarks.Items.Add(visibleString);
            }

            try
            {
                lstMarks.SelectedIndex = 0;
            } catch (Exception) { }

            
            

            refreshGridView();
        }
        List<Point> createPath(Point from, Point to)
        {
            List<Point> res = new List<Point>();
            int dirx = (from.X <= to.X)?(1):(-1);
            int diry = (from.Y <= to.Y)?(1):(-1);

            for (int x = from.X; x != to.X; x += dirx)
                res.Add(new Point(x, from.Y));
            
            for (int y = from.Y; y != to.Y; y += diry)
                res.Add(new Point(to.X, y));

            res.Add(to);

            return res;
        }
        public void setInputRequest(InputRequest req)
        {
            latestInputRequest = req;
            lstQuestions.Enabled = lstAgents.Enabled = lstChoices.Enabled = true;

            lstQuestions.Items.Clear();
            lstChoices.Items.Clear();

            agentQuestionToAnswer = new Dictionary<Tuple<IAgent, int>, object>();
            agentRequest = new List<IAgent>();
            int i = 0;
            foreach (var a in req.MovementOptions)
            {

                agentLocationAnswer[a.Key] = latestInputRequest.MovementOptions[a.Key].Item1.nodeLocation;
                                             
                agentRequest.Add(a.Key);
                ++i;

                if(a.Key is Pursuer)
                    lstAgents.Items.Add("Pursuer" + i.ToString());
                else
                    lstAgents.Items.Add("Evader" + i.ToString());
            }

            
            
        }
        public List<string> ShowDialog(string[] text, string caption, string[] defaultValues)
        {
            return InputBox.ShowDialog(text, caption, defaultValues);
        }
        public object getChoice(GameLogic.IAgent chooser, int choiceKey)
        {
            var key = Tuple.Create(chooser, choiceKey);
            if (agentQuestionToAnswer.Keys.Contains(key))
                return agentQuestionToAnswer[key];
            return latestInputRequest.ComboChoices[chooser][choiceKey].Item3;
        }

        public List<Location> getMovement(GameLogic.IAgent mover)
        {
            var res = new List<Location>();
            res.Add(new Location(agentLocationAnswer[mover]));
            return res;
        }
        private void chkEvaders_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEvaders.Checked)
            {
                displayedEvaderLocations = new List<Point>();
                if (gameProc.MostUpdatedEvadersRound >= 0)
                    foreach (var ev in gameProc.AllEvaderLocations)
                        displayedEvaderLocations.Add(ev);
            }
            else
                displayedEvaderLocations = new List<Point>();

            refreshGridView();
        }
        private void chkPursuers_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPursuers.Checked)
            {
                //if ((gameProc.State.L.Count > gameProc.currentRound && gameProc.State.L[gameProc.currentRound].Count > 0) ||
                //    (gameProc.State.L.Count > gameProc.currentRound - 1 && gameProc.State.L[gameProc.currentRound - 1].Count > 0))
                //{
                //    displayedPursuerLocations = new List<Point>();
                //}
                displayedPursuerLocations = new List<Point>();

                if (gameProc.MostUpdatedPursuersRound >= 0)
                    foreach(var p in gameProc.LatestPursuerLocations)
                        displayedPursuerLocations.Add(p);
                    
                    //foreach (Pursuer p in gameProc.Params.A_P)
                    //{
                    //    Location dmyLoc = gameProc.State.L[gameProc.State.MostUpdatedPursuersRound][p];
                    //    if (dmyLoc.locationType == GameLogic.Location.Type.Node)
                    //        displayedPursuerLocations.Add(dmyLoc.nodeLocation);
                    //}
            }
            else
                displayedPursuerLocations = new List<Point>();

            refreshGridView();
        }

        private void refreshGridView()
        {
            if (gridController == null)
                return;


            List<Point> PolicyMarksLocations = new List<Point>();
            if (lstMarks.SelectedIndex != -1)
                PolicyMarksLocations =
                    displayedPolicyMarksLocations[marklistIndexToDictionaryKey[lstMarks.SelectedIndex]];

            List<Point> pursuerLocations = new List<Point>();
            pursuerLocations.AddRange(displayedPursuerLocations);

            gridController.clearColors();
            gridController.setColors(
                pursuerLocations,//displayedPursuerLocations, 
                displayedEvaderLocations, 
                new List<Point>(),
                PolicyMarksLocations,
                new List<Point>());
        }

        private void lstAgents_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                lstQuestions.Items.Clear();
                lstChoices.Items.Clear();
                
                
                int agentIndex = lstAgents.SelectedIndex;
                IAgent ag = agentRequest[agentIndex];

                if (agentLocationAnswer.ContainsKey(ag) && agentLocationAnswer[ag] != null)
                    currentLocation = agentLocationAnswer[ag];
                else
                    currentLocation = new Point(-1, -1);

                if (latestInputRequest.ComboChoices.Keys.Contains(ag))
                    foreach (var q in latestInputRequest.ComboChoices[ag])                
                        lstQuestions.Items.Add(q.Item1);
                
                if (currentMovements.Keys.Contains(ag))
                    gridController.setMovement(currentMovements[ag].Item1, currentMovements[ag].Item2);
                else
                    gridController.setMovement(new Point(-1, -1), new Point(-1, -1));

                try
                {
                    lstQuestions.SelectedIndex = 0;
                }
                catch (Exception) { }

                refreshGridView();
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void lstQuestions_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                lstChoices.Items.Clear();

                int agentIndex = lstAgents.SelectedIndex;
                IAgent ag = agentRequest[agentIndex];
                List<Object> options = 
                    latestInputRequest.ComboChoices[ag][lstQuestions.SelectedIndex].Item2;
                foreach (var o in options)
                    lstChoices.Items.Add(o);

                if (agentQuestionToAnswer.Keys.Contains(Tuple.Create(ag, lstQuestions.SelectedIndex)))
                    lstChoices.SelectedItem = agentQuestionToAnswer[Tuple.Create(ag, lstQuestions.SelectedIndex)];
            }
            catch (Exception) { }
        }

        private void lstChoices_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                
                int agentIndex = lstAgents.SelectedIndex;
                IAgent ag = agentRequest[agentIndex];

                agentQuestionToAnswer[Tuple.Create(ag,lstQuestions.SelectedIndex)] = lstChoices.SelectedItem;
            }
            catch (Exception) { }

        }

        private Point currentLocation;
        private void gameView_CellClick(object sender, uint cellX, uint cellY, MouseEventArgs m)
        {
            try
            {
                
                int agentIndex = lstAgents.SelectedIndex;
                if (agentIndex == -1)
                    return;

                IAgent ag = agentRequest[agentIndex];
                Point newp = new Point((int)cellX, (int)cellY);

                if (pursuerTurn)
                {
                    
                    if (m.Button == System.Windows.Forms.MouseButtons.Right)
                    {
                        currentLocation = new Point(-1,-1);
                    }
                    else
                        currentLocation = newp;
                }
                else
                {
                    currentLocation = newp;
                }
                agentLocationAnswer[ag] = currentLocation;
                
                gridController.setMovement(GameLogic.Utils.locationToPoint(latestInputRequest.MovementOptions[ag].Item1),
                                            agentLocationAnswer[ag]);

                currentMovements[ag] = 
                    Tuple.Create(GameLogic.Utils.locationToPoint(latestInputRequest.MovementOptions[ag].Item1),
                                                    agentLocationAnswer[ag]);
                
            }
            catch(Exception ex){}
        }
        
        private void gameView_Load(object sender, EventArgs e)
        {
            chkEvaders.CheckState = CheckState.Checked;
            chkPursuers.CheckState = CheckState.Checked;

            refreshGridView();
        }

        int pursuersMarkedItemIdx = -1;
        int evadersMarkedItemIdx = -1;
        bool userSelection = true;
        private void lstMarks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (userSelection)
            {
                if (pursuerTurn)
                    pursuersMarkedItemIdx = lstMarks.SelectedIndex;
                else
                    evadersMarkedItemIdx = lstMarks.SelectedIndex;
            }
            refreshGridView();
        }

       
        private void gameView_KeyDown(object sender, KeyEventArgs e)
        {
            isCtrlPressed = e.Control;
            isAltPressed = e.Alt;
        }

        private void gameView_KeyUp(object sender, KeyEventArgs e)
        {
            isCtrlPressed = e.Control;
            isAltPressed = e.Alt;
        }

        private void gameView_MouseMove(object sender, MouseEventArgs e)
        {
            Point cell = gameView.getCellCoord(e);
            lblCoord.Text = cell.X.ToString() + "," + cell.Y.ToString();
        }

        private void gameView_MouseHover(object sender, EventArgs e)
        {
            
        }

        private void frmRoutingGameProcessView_Load(object sender, EventArgs e)
        {

        }

        private void txtSkip_Enter(object sender, EventArgs e)
        {
            int val;
            if (!Int32.TryParse(txtSkip.Text, out val))
            {
                txtSkip.Text = "2";
            }
        }

        private void guiTips_Popup(object sender, PopupEventArgs e)
        {

        }
        
        public void addLogValue(string key, string value)
        {
            logLines.Add(key + "=" + value);
        }

        private void txtSkip_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void txtSkip_TextChanged(object sender, EventArgs e)
        {

        }


        public void addCurrentRoundLog(List<string> logLines)
        {
            lstLog.Items.Clear();
            lstLog.Items.AddRange(logLines.ToArray<object>());
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            if (!pnlLog.Visible)
                btnLog.Text = "hide log <<";
            else
                btnLog.Text = "show log >>";
            
            pnlLog.Visible = !pnlLog.Visible;
        }

        private void pnlLog_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                pnlLog.Width = this.Width - this.PointToClient(pnlLog.PointToScreen(e.Location)).X;
            }
        }

        private void frmRoutingGameProcessView_ResizeEnd(object sender, EventArgs e)
        {
           
        }

        private void gameView_SizeChanged(object sender, EventArgs e)
        {
            if (!manualZoom)
            {
                gameView.CellSize = (uint)
                    Math.Min(gameView.CellSize - (gameView.CellSize * gameView.HeightCellCount - gameView.Height) / gameView.HeightCellCount,
                             gameView.CellSize - (gameView.CellSize * gameView.WidthCellCount - gameView.Width) / gameView.WidthCellCount);
   
                refreshGridView();
         }
        }

        
    }
}
