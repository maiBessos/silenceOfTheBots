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
using GoE.AdvRouting;
using System.Drawing.Imaging;
using System.IO;
using System.Security.AccessControl;

namespace GoE
{
    public partial class frmAdvRoutingGameProcessView : Form, IGameProcessGUI
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
            this.chkShowRound = new System.Windows.Forms.CheckBox();
            this.chkRecord = new System.Windows.Forms.CheckBox();
            this.chkRouters = new System.Windows.Forms.CheckBox();
            this.chkInterVisits = new System.Windows.Forms.CheckBox();
            this.btnViewSink = new System.Windows.Forms.Button();
            this.btnLog = new System.Windows.Forms.Button();
            this.lblCoord = new System.Windows.Forms.Label();
            this.txtSkip = new System.Windows.Forms.TextBox();
            this.lstMarks = new System.Windows.Forms.ListBox();
            this.lstChoices = new System.Windows.Forms.ListBox();
            this.lstQuestions = new System.Windows.Forms.ListBox();
            this.lstAgents = new System.Windows.Forms.ListBox();
            this.btnNext = new System.Windows.Forms.Button();
            this.guiTips = new System.Windows.Forms.ToolTip(this.components);
            this.pnlLog = new System.Windows.Forms.Panel();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.gameView = new GoE.PointsGameGraphViewer();
            this.pnlPolicyGUI.SuspendLayout();
            this.pnlLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlPolicyGUI
            // 
            this.pnlPolicyGUI.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPolicyGUI.Controls.Add(this.chkShowRound);
            this.pnlPolicyGUI.Controls.Add(this.chkRecord);
            this.pnlPolicyGUI.Controls.Add(this.chkRouters);
            this.pnlPolicyGUI.Controls.Add(this.chkInterVisits);
            this.pnlPolicyGUI.Controls.Add(this.btnViewSink);
            this.pnlPolicyGUI.Controls.Add(this.btnLog);
            this.pnlPolicyGUI.Controls.Add(this.lblCoord);
            this.pnlPolicyGUI.Controls.Add(this.txtSkip);
            this.pnlPolicyGUI.Controls.Add(this.lstMarks);
            this.pnlPolicyGUI.Controls.Add(this.lstChoices);
            this.pnlPolicyGUI.Controls.Add(this.lstQuestions);
            this.pnlPolicyGUI.Controls.Add(this.lstAgents);
            this.pnlPolicyGUI.Controls.Add(this.btnNext);
            this.pnlPolicyGUI.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlPolicyGUI.Location = new System.Drawing.Point(267, 0);
            this.pnlPolicyGUI.Name = "pnlPolicyGUI";
            this.pnlPolicyGUI.Size = new System.Drawing.Size(233, 668);
            this.pnlPolicyGUI.TabIndex = 1;
            // 
            // chkShowRound
            // 
            this.chkShowRound.AutoSize = true;
            this.chkShowRound.Location = new System.Drawing.Point(114, 638);
            this.chkShowRound.Name = "chkShowRound";
            this.chkShowRound.Size = new System.Drawing.Size(102, 17);
            this.chkShowRound.TabIndex = 19;
            this.chkShowRound.Text = "Display Round#";
            this.chkShowRound.UseVisualStyleBackColor = true;
            this.chkShowRound.CheckedChanged += new System.EventHandler(this.chkShowRound_CheckedChanged);
            // 
            // chkRecord
            // 
            this.chkRecord.AutoSize = true;
            this.chkRecord.Location = new System.Drawing.Point(114, 618);
            this.chkRecord.Name = "chkRecord";
            this.chkRecord.Size = new System.Drawing.Size(118, 17);
            this.chkRecord.TabIndex = 18;
            this.chkRecord.Text = "Record Screenshot";
            this.chkRecord.UseVisualStyleBackColor = true;
            // 
            // chkRouters
            // 
            this.chkRouters.AutoSize = true;
            this.chkRouters.Checked = true;
            this.chkRouters.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRouters.Location = new System.Drawing.Point(9, 105);
            this.chkRouters.Name = "chkRouters";
            this.chkRouters.Size = new System.Drawing.Size(63, 17);
            this.chkRouters.TabIndex = 17;
            this.chkRouters.Text = "Routers";
            this.chkRouters.UseVisualStyleBackColor = true;
            // 
            // chkInterVisits
            // 
            this.chkInterVisits.AutoSize = true;
            this.chkInterVisits.Checked = true;
            this.chkInterVisits.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkInterVisits.Location = new System.Drawing.Point(9, 82);
            this.chkInterVisits.Name = "chkInterVisits";
            this.chkInterVisits.Size = new System.Drawing.Size(127, 17);
            this.chkInterVisits.TabIndex = 16;
            this.chkInterVisits.Text = "Prev. Inter. visitations";
            this.chkInterVisits.UseVisualStyleBackColor = true;
            // 
            // btnViewSink
            // 
            this.btnViewSink.Location = new System.Drawing.Point(9, 43);
            this.btnViewSink.Name = "btnViewSink";
            this.btnViewSink.Size = new System.Drawing.Size(134, 23);
            this.btnViewSink.TabIndex = 15;
            this.btnViewSink.Text = "Move View To Sink";
            this.btnViewSink.UseVisualStyleBackColor = true;
            this.btnViewSink.Click += new System.EventHandler(this.btnViewSink_Click);
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
            this.lblCoord.Location = new System.Drawing.Point(45, 622);
            this.lblCoord.Name = "lblCoord";
            this.lblCoord.Size = new System.Drawing.Size(19, 13);
            this.lblCoord.TabIndex = 13;
            this.lblCoord.Text = "__";
            // 
            // txtSkip
            // 
            this.txtSkip.Location = new System.Drawing.Point(147, 592);
            this.txtSkip.Name = "txtSkip";
            this.txtSkip.Size = new System.Drawing.Size(74, 20);
            this.txtSkip.TabIndex = 9;
            this.txtSkip.Text = "AutoClicks#";
            this.guiTips.SetToolTip(this.txtSkip, "Number of steps to skip forward (2 steps = 1 round)");
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
            this.lstChoices.Size = new System.Drawing.Size(221, 69);
            this.lstChoices.TabIndex = 6;
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
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(7, 589);
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
            // gameView
            // 
            this.gameView.BackColor = System.Drawing.Color.White;
            this.gameView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gameView.Location = new System.Drawing.Point(0, 0);
            this.gameView.Name = "gameView";
            this.gameView.RadiusColor = System.Drawing.Color.Empty;
            this.gameView.Size = new System.Drawing.Size(267, 668);
            this.gameView.TabIndex = 3;
            this.gameView.Zoom = 1F;
            this.gameView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.gameView_KeyDown);
            this.gameView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.gameView_KeyUp);
            this.gameView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.gameView_MouseDown);
            this.gameView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.gameView_MouseMove);
            // 
            // frmAdvRoutingGameProcessView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 668);
            this.Controls.Add(this.gameView);
            this.Controls.Add(this.pnlPolicyGUI);
            this.Controls.Add(this.pnlLog);
            this.DoubleBuffered = true;
            this.Name = "frmAdvRoutingGameProcessView";
            this.Text = "Routing Game Process View";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.GameProcessView_Load);
            this.ResizeEnd += new System.EventHandler(this.frmRoutingGameProcessView_ResizeEnd);
            this.Enter += new System.EventHandler(this.frmAdvRoutingGameProcessView_Enter);
            this.pnlPolicyGUI.ResumeLayout(false);
            this.pnlPolicyGUI.PerformLayout();
            this.pnlLog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        
        private System.Windows.Forms.Panel pnlPolicyGUI;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.ListBox lstChoices;
        private System.Windows.Forms.ListBox lstQuestions;
        private System.Windows.Forms.ListBox lstAgents;
        private System.Windows.Forms.ListBox lstMarks;
        private System.Windows.Forms.TextBox txtSkip;
        private System.Windows.Forms.ToolTip guiTips;
        private System.Windows.Forms.Label lblCoord;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Panel pnlLog;
        private System.Windows.Forms.ListBox lstLog;
        private PointsGameGraphViewer gameView;
        private System.Windows.Forms.Button btnViewSink;

        #endregion

        private List<string> logLines = new List<string>();
        private bool isAltPressed = false;
        private bool isCtrlPressed = false;
        private bool pursuerTurn = false;

        private EmptyEnvironment graph;
        private AdvRoutingGameProcess gameProc;

        //private List<Point> displayedEvaderLocations = new List<Point>();
        //private List<Point> displayedPursuerLocations = new List<Point>();
        private Dictionary<string, List<PointF>> displayedPolicyMarksLocations = new Dictionary<string, List<PointF>>();
        private CheckBox chkRouters;
        private CheckBox chkInterVisits;
        private CheckBox chkRecord;
        private CheckBox chkShowRound;
        private List<string> marklistIndexToDictionaryKey = new List<string>(); // maps from items in lstMarks to key values of 'displayedPolicyMarksLocations'
        //Dictionary<IAgent, Tuple<Point, Point>> currentMovements = new Dictionary<IAgent, Tuple<Point, Point>>();

        //private List<IAgent> agentRequest;
        //private InputRequest latestInputRequest = null;

        //Dictionary<IAgent, Point> agentLocationAnswer = new Dictionary<IAgent, Point>();
        //Dictionary<Tuple<IAgent, int>, object> agentQuestionToAnswer = new Dictionary<Tuple<IAgent, int>, object>();

        public void debugStopSkippingRound()
        {
            skipCount = Math.Min(skipCount, 1);
        }
        public bool hasBoardGUI()
        {
            return true;
        }

        public void init(AGameGraph grGraph, AGameProcess p)
        {
            gameProc = (AdvRoutingGameProcess)p;
            graph = (EmptyEnvironment)grGraph;

            //foreach (Pursuer pr in gameProc.Params.A_P)
            //{
            //    agentLocationAnswer[pr] = new Point(-1, -1);
            //   
            //}
            //foreach (Evader ev in gameProc.Params.A_E)
            //{
            //    agentLocationAnswer[ev] = new Point(-1, -1);
            //   
            //}
        }
        public frmAdvRoutingGameProcessView()
        {
            InitializeComponent();
            this.gameView.Zoom = 100;
        }

        public void flushLog()
        {
            string msg = "";
            foreach (string line in logLines)
                msg += line + "\n";
        }

        int skipCount = 1;
        
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
                    lstAgents.Items.Clear();
                    lstMarks.Items.Clear();
                    displayedPolicyMarksLocations = new Dictionary<string, List<PointF>>();
                    lstQuestions.Enabled = lstAgents.Enabled = lstChoices.Enabled = false;
                    //currentMovements = new Dictionary<IAgent, Tuple<Point, Point>>();
                    //gridController.setMovement(new Point(-1, -1), new Point(-1, -1));
                }

                try
                {
                    if (gameProc.invokeNextPolicy() == false)
                    {
                        //MessageBox.Show("game ended. Evader utility: " + gameProc.GameResultReward.ToString());
                        //this.Close();
                        btnNext.Enabled = false;
                        if (chkRecord.Checked)
                            recordNextRefresh = true;
                        refreshGridView();
                        return;
                    }

                    if(!pursuerTurn) // pursuer just moved
                        previouslyVisitedNodes.Add(gameProc.pursuerLoc);
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
                   
                    //if (pursuersMarkedItemIdx != -1)
                    //    lstMarks.SelectedIndex = pursuersMarkedItemIdx;
                }
                else
                {
                    btnNext.Text = "Invoke Evaders Policy";
                    
                    //if (evadersMarkedItemIdx != -1)
                    //    lstMarks.SelectedIndex = evadersMarkedItemIdx;

                }

            }
            while ((--skipCount) > 0);

            if (chkRecord.Checked)
                recordNextRefresh = true;
            refreshGridView();

            userSelection = true;
        }
        bool recordNextRefresh = false;

        bool manualZoom = false;
        private void GameProcessView_Load(object sender, EventArgs e)
        {

            gameView.RadiusColor = Color.FromArgb(30, Color.Pink);
            gameView.Invalidate();
            this.MouseWheel += frmRoutingGameProcessView_MouseWheel;

#if PUBLIC_ADV_ROUTING
            lstAgents.Visible = false;
            lstChoices.Visible = false;
            btnLog.Visible = false;
            txtSkip.Text = "2";
            chkShowRound.Visible = false;
            btnNext_Click(sender, e);
#endif
        }

        void frmRoutingGameProcessView_MouseWheel(object sender, MouseEventArgs e)
        {
            //if (isCtrlPressed)
            {
                gameView.Zoom *= (float)Math.Pow(1.1, e.Delta / 120);
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

            displayedPolicyMarksLocations = new Dictionary<string, List<PointF>>(locations);
            displayedPolicyMarksLocations.Add("Hide policy's marked points", new List<PointF>());

            //    displayedPolicyMarksLocations["Transmitted E-bots"] = gameProc.AllTransmittingEvaders;
            //    displayedPolicyMarksLocations["Detected E-bots"] = gameProc.DetectedEvaders;

            List<string> marksToSort = new List<string>();
            foreach (var s in displayedPolicyMarksLocations)
            {
                string visibleString = "Mark " + s.Key + " - [" + s.Value.Count.ToString() + "]";
                marklistIndexToDictionaryKey.Add(s.Key);
                marksToSort.Add(visibleString);                
            }
            marksToSort.Sort();
            marklistIndexToDictionaryKey.Sort();
            foreach (string s in marksToSort)
                lstMarks.Items.Add(s);

            try
            {
                lstMarks.SelectedIndex = 0;
            }
            catch (Exception) { }

            refreshGridView();
        }
        
        public void setInputRequest(InputRequest req)
        {
            //latestInputRequest = req;
            //lstQuestions.Enabled = lstAgents.Enabled = lstChoices.Enabled = true;

            //lstQuestions.Items.Clear();
            //lstChoices.Items.Clear();

            //agentQuestionToAnswer = new Dictionary<Tuple<IAgent, int>, object>();
            //agentRequest = new List<IAgent>();
            //int i = 0;
            //foreach (var a in req.MovementOptions)
            //{

            //    agentLocationAnswer[a.Key] = latestInputRequest.MovementOptions[a.Key].Item1.nodeLocation;

            //    agentRequest.Add(a.Key);
            //    ++i;

            //    if (a.Key is Pursuer)
            //        lstAgents.Items.Add("Pursuer" + i.ToString());
            //    else
            //        lstAgents.Items.Add("Evader" + i.ToString());
            //}



        }
        public List<string> ShowDialog(string[] text, string caption, string[] defaultValues)
        {
            return InputBox.ShowDialog(text, caption, defaultValues);
        }
        public object getChoice(GameLogic.IAgent chooser, int choiceKey)
        {
            //var key = Tuple.Create(chooser, choiceKey);
            //if (agentQuestionToAnswer.Keys.Contains(key))
            //    return agentQuestionToAnswer[key];
            //return latestInputRequest.ComboChoices[chooser][choiceKey].Item3;
            throw new NotImplementedException();
        }

        public List<Location> getMovement(GameLogic.IAgent mover)
        {
            //var res = new List<Location>();
            //res.Add(new Location(agentLocationAnswer[mover]));
            //return res;
            throw new NotImplementedException();
        }

        bool preInit = true;
        HashSet<NodeView> graphNodes;
        List<PointF> previouslyVisitedNodes = new List<PointF>();
        
        private void refreshGridView()
        {

            List<PointF> PolicyMarksLocations = new List<PointF>();
            if (lstMarks.SelectedIndex != -1)
                PolicyMarksLocations =
                    displayedPolicyMarksLocations[marklistIndexToDictionaryKey[lstMarks.SelectedIndex]];

            graphNodes = new HashSet<NodeView>();


            if (gameProc.transmittingRouters == null)
                return; // routers didn't initialize network yet

            //draw first detected node:
            // fixme uncomment below
            //graphNodes.Add(new NodeView()
            //{
            //    center = gameProc.InitialDetectedRouter,
            //    circleColor = new List<Color>() { Color.Blue, Color.Blue },
            //    Fill = new List<bool>() { false, false },
            //    Rads = new List<float>() { 0.3f, 0.4f },
            //    text = "(0,0)"//"Init.Detected Router"
            //});
            graphNodes.Add(new NodeView()
            {
                center = gameProc.InitialDetectedRouter,
                circleColor = new List<Color>() { Color.Black},
                Fill = new List<bool>() { false},
                Rads = new List<float>() { 1 },
                text = "(0,0)"//"Init.Detected Router"
            });


            // draw each previously visited points:
            if (chkInterVisits.Checked)
                foreach (var v in previouslyVisitedNodes)
                    graphNodes.Add(new NodeView()
                    {
                        center = v,
                        circleColor = new List<Color>() { Color.FromArgb(128, 0, 0, 255)},
                        Fill = new List<bool>() { false },
                        Rads = new List<float>() { 0.98f}
                    });

            for (int i = 0; i < gameProc.routerPointToIdx.Count; ++i)
            {
                if(gameProc.transmittingRouters.Contains(i))
                {
                    // FIXME consider uncommenting, remove below
                    //graphNodes.Add(new NodeView()
                    //{ center = gameProc.routerIdxToPoint[i],
                    //  circleColor = new List<Color>() { Color.FromArgb(100,255,0,0), Color.Red, Color.Red},
                    //  Fill = new List<bool>() { true,false,false},
                    //    Rads = new List<float>() { 0.1f,0.12f,1f} 
                    //});
                    graphNodes.Add(new NodeView()
                    {
                        center = gameProc.routerIdxToPoint[i],
                        circleColor = new List<Color>() { Color.Black, Color.FromArgb(100, 200, 200, 200), Color.Gray },
                        Fill = new List<bool>() { true, true, false},
                        Rads = new List<float>() { 0.2f, 0.99f, 1 }
                    });
                }
                else
                {
                    if(chkRouters.Checked)
                        //graphNodes.Add(new NodeView() // fixme uncomment, remove below
                        //{
                        //    center = gameProc.routerIdxToPoint[i],
                        //    circleColor = new List<Color>() { Color.FromArgb(20, 255, 0, 0), Color.Red, Color.Black },
                        //    Fill = new List<bool>() { true, false, false },
                        //    Rads = new List<float>() { 0.1f, 0.12f, 1f }
                        //});
                        graphNodes.Add(new NodeView()
                        {
                            center = gameProc.routerIdxToPoint[i],
                            circleColor = new List<Color>() { Color.FromArgb(200, 200, 200, 200), Color.FromArgb(100, 200, 200, 200), Color.Gray },
                            Fill = new List<bool>() { false, true, false },
                            Rads = new List<float>() { 0.2f, 0.99f, 1 }
                        });
                }
            }
            
            //graphNodes.Add(new NodeView() //fixme uncomment
            //{
            //    center = gameProc.sink,
            //    circleColor = new List<Color>() { Color.Red, Color.Red, Color.Red },
            //    Fill = new List<bool>() { true, false, false },
            //    Rads = new List<float>() { 0.1f, 0.12f, 1f },
            //    text = "D"
            //});
            graphNodes.Add(new NodeView()
            {
                center = gameProc.sink,
                circleColor = new List<Color>() { Color.Black },
                Fill = new List<bool>() {  false },
                Rads = new List<float>() { 1f },
                text = "D"
            });

            //graphNodes.Add(new NodeView()
            //{
            //    center = gameProc.pursuerLoc,
            //    circleColor = new List<Color>() { Color.Blue },
            //    Fill = new List<bool>() { true },
            //    Rads = new List<float>() { 1 },
            //    text = "Int"
            //});
            graphNodes.Add(new NodeView()
            {
                center = gameProc.pursuerLoc,
                circleColor = new List<Color>() { Color.Blue , Color.Black },
                Fill = new List<bool>() { true , true},
                Rads = new List<float>() { 1 , 0.3f },
                shapes = new List<NodeView.NodeShape>() { NodeView.NodeShape.Circle, NodeView.NodeShape.Diamond },
                text = "Interceptor"
            });

            foreach (var v in PolicyMarksLocations)
            {
                graphNodes.Add(new NodeView()
                {
                    center = v,
                    circleColor = new List<Color>() { Color.Black },
                    Fill = new List<bool>() { true },
                    Rads = new List<float>() { 0.2f },
                    //text = v.X.ToString() + "," + v.Y.ToString()

                });
            }

            gameView.setWorldRect(new RectangleF(gameProc.sink .X - 2*gameProc.Params.A_E.Count, 
                gameProc.sink .Y- 2*gameProc.Params.A_E.Count,
                4 * gameProc.Params.A_E.Count, 
                4 * gameProc.Params.A_E.Count));
            gameView.resetElements(graphNodes.ToList());
            

           


            if (preInit)
            {
                gameView.setScrollers(gameView.getScollersMaxVals().mult(0.5f)); // set scrollers to center
                preInit = false;
            }
            gameView.Invalidate();

            if (recordNextRefresh)
            {
                recordNextRefresh = false;
                Bitmap currentFrame = new Bitmap(1024, 768);
                try
                {
                    try
                    {
                        Directory.CreateDirectory("recordings");
                    }
                    catch (Exception) { }
                    
                    gameView.DrawToBitmap(currentFrame, new Rectangle(0, 0, this.Size.Width, this.Size.Height));

                    // crop out vscroll:
                    Rectangle cropRect = new Rectangle(0, 0, this.Size.Width, this.Size.Height-50);
                    Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.DrawImage(currentFrame, new Rectangle(0, 0, target.Width, target.Height-50),
                                         cropRect,
                                         GraphicsUnit.Pixel);
                        if (chkShowRound.Checked)
                            g.DrawString("Round:" + gameProc.currentRound.ToString(), new Font(FontFamily.GenericSerif,20,GraphicsUnit.Pixel), Brushes.Orange, 20, 20);
                    }

                    target.Save("recordings/" + picCounter + ".png", ImageFormat.Png);
                    ++picCounter;
                }
                catch (Exception) { }
            }
        }
        int picCounter = 1;

        //private void lstAgents_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        lstQuestions.Items.Clear();
        //        lstChoices.Items.Clear();


        //        int agentIndex = lstAgents.SelectedIndex;
        //        IAgent ag = agentRequest[agentIndex];

        //        if (agentLocationAnswer.ContainsKey(ag) && agentLocationAnswer[ag] != null)
        //            currentLocation = agentLocationAnswer[ag];
        //        else
        //            currentLocation = new Point(-1, -1);

        //        if (latestInputRequest.ComboChoices.Keys.Contains(ag))
        //            foreach (var q in latestInputRequest.ComboChoices[ag])
        //                lstQuestions.Items.Add(q.Item1);

        //        if (currentMovements.Keys.Contains(ag))
        //            gridController.setMovement(currentMovements[ag].Item1, currentMovements[ag].Item2);
        //        else
        //            gridController.setMovement(new Point(-1, -1), new Point(-1, -1));

        //        try
        //        {
        //            lstQuestions.SelectedIndex = 0;
        //        }
        //        catch (Exception) { }

        //        refreshGridView();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        //private void lstQuestions_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        lstChoices.Items.Clear();

        //        int agentIndex = lstAgents.SelectedIndex;
        //        IAgent ag = agentRequest[agentIndex];
        //        List<Object> options =
        //            latestInputRequest.ComboChoices[ag][lstQuestions.SelectedIndex].Item2;
        //        foreach (var o in options)
        //            lstChoices.Items.Add(o);

        //        if (agentQuestionToAnswer.Keys.Contains(Tuple.Create(ag, lstQuestions.SelectedIndex)))
        //            lstChoices.SelectedItem = agentQuestionToAnswer[Tuple.Create(ag, lstQuestions.SelectedIndex)];
        //    }
        //    catch (Exception) { }
        //}

        //private void lstChoices_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    try
        //    {

        //        int agentIndex = lstAgents.SelectedIndex;
        //        IAgent ag = agentRequest[agentIndex];

        //        agentQuestionToAnswer[Tuple.Create(ag, lstQuestions.SelectedIndex)] = lstChoices.SelectedItem;
        //    }
        //    catch (Exception) { }

        //}

        private Point currentLocation;
        private void gameView_CellClick(object sender, uint cellX, uint cellY, MouseEventArgs m)
        {
            //try
            //{

            //    int agentIndex = lstAgents.SelectedIndex;
            //    if (agentIndex == -1)
            //        return;

            //    IAgent ag = agentRequest[agentIndex];
            //    Point newp = new Point((int)cellX, (int)cellY);

            //    if (pursuerTurn)
            //    {

            //        if (m.Button == System.Windows.Forms.MouseButtons.Right)
            //        {
            //            currentLocation = new Point(-1, -1);
            //        }
            //        else
            //            currentLocation = newp;
            //    }
            //    else
            //    {
            //        currentLocation = newp;
            //    }
            //    agentLocationAnswer[ag] = currentLocation;

            //    gridController.setMovement(GameLogic.Utils.locationToPoint(latestInputRequest.MovementOptions[ag].Item1),
            //                                agentLocationAnswer[ag]);

            //    currentMovements[ag] =
            //        Tuple.Create(GameLogic.Utils.locationToPoint(latestInputRequest.MovementOptions[ag].Item1),
            //                                        agentLocationAnswer[ag]);

            //}
            //catch (Exception ex) { }
        }

        private void gameView_Load(object sender, EventArgs e)
        {
            //chkEvaders.CheckState = CheckState.Checked;
            //chkPursuers.CheckState = CheckState.Checked;

            refreshGridView();
        }

        int pursuersMarkedItemIdx = -1;
        int evadersMarkedItemIdx = -1;
        bool userSelection = true;
        private void lstMarks_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (userSelection)
            //{
            //    if (pursuerTurn)
            //        pursuersMarkedItemIdx = lstMarks.SelectedIndex;
            //    else
            //        evadersMarkedItemIdx = lstMarks.SelectedIndex;
            //}
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

        Point translateStart, scrollerStartVal; // when mouse starts dragging the form, we keep the initial values
        private void gameView_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                translateStart = e.Location;
                scrollerStartVal = gameView.getScollersVals();
            }
        }
        private void gameView_MouseMove(object sender, MouseEventArgs e)
        {
            //try
            //{
            //    PointF mouseLof = new PointF(e.Location.X, e.Location.Y);
            //    mouseLof.add(gameView.)
            //    float mindist = float.MaxValue;
            //    PointF minPoint = new PointF();
            //    foreach (var p in displayedPolicyMarksLocations[marklistIndexToDictionaryKey[lstMarks.SelectedIndex]])
            //        if (mouseLof.distance(p) < mindist)
            //        {
            //            mindist = mouseLof.distance(p);
            //            minPoint = p;
            //        }
            //    lblCoord.Text = minPoint.X.ToString() + "," + minPoint.Y.ToString();

            //}
            //catch (Exception) { }


            
            if (e.Button == MouseButtons.Right)
            {
                gameView.setScrollers(scrollerStartVal.subtruct(e.Location.subtruct(translateStart)));
                gameView.Invalidate();
            }
            
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
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                pnlLog.Width = this.Width - this.PointToClient(pnlLog.PointToScreen(e.Location)).X;
            }
        }

        private void frmRoutingGameProcessView_ResizeEnd(object sender, EventArgs e)
        {

        }

        private void frmAdvRoutingGameProcessView_Enter(object sender, EventArgs e)
        {
            gameView.Focus();
        }

        private void btnViewSink_Click(object sender, EventArgs e)
        {
            gameView.setScrollers(gameView.getScollersMaxVals().mult(0.5f)); // set scrollers to center
            gameView.Invalidate();
        }

        
        private void chkShowRound_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void gameView_SizeChanged(object sender, EventArgs e)
        {
            //if (!manualZoom)
            //{
            //    gameView.CellSize = (uint)
            //        Math.Min(gameView.CellSize - (gameView.CellSize * gameView.HeightCellCount - gameView.Height) / gameView.HeightCellCount,
            //                 gameView.CellSize - (gameView.CellSize * gameView.WidthCellCount - gameView.Width) / gameView.WidthCellCount);

            //    refreshGridView();
            //}
            refreshGridView();
        }


    }
}
