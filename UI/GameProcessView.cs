using GoE.GameLogic;
using GoE.UI;
using GoE.Utils;
using GoE.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace GoE
{
    public partial class frmGameProcessView : Form, IGameProcessGUI
    {
        private List<string> logLines = new List<string>();
        private bool isAltPressed = false;
        private bool isCtrlPressed = false;
        private bool pursuerTurn = true;
        private GridGameGraphController gridController = null;
        private GridGameGraph graph;
        private GameLogic.GoEGameProcess gameProc;

        private List<Point> displayedAgentPossibleDestinationPoints = new List<Point>();
        private List<Point> displayedEvaderLocations = new List<Point>();
        private List<Point> displayedPursuerLocations = new List<Point>();
        private List<Point> displayedSensitiveLocations = new List<Point>();
        private List<Point> displayedLocationsReachableByPursuers = new List<Point>();
        private Dictionary<string, List<Point>> displayedPolicyMarksLocations = new Dictionary<string, List<Point>>();
        private List<string> marklistIndexToDictionaryKey = new List<string>(); // maps from items in lstMarks to key values of 'displayedPolicyMarksLocations'
        Dictionary<IAgent, Tuple<Point, Point>> currentMovements = new Dictionary<IAgent, Tuple<Point, Point>>();

        private List<IAgent> agentRequest;
        private InputRequest latestInputRequest = null;

        Dictionary<IAgent, Location> agentLocationAnswer = new Dictionary<IAgent, Location>();
        Dictionary<Tuple<IAgent, int>, object> agentQuestionToAnswer = new Dictionary<Tuple<IAgent, int>, object>();

        public void debugStopSkippingRound() 
        {
            skipCount = Math.Min(skipCount,1);
        }
        public bool hasBoardGUI()
        {
            return true;
        }
        public frmGameProcessView()
        {
            InitializeComponent();
            
        }
        public void init(AGameGraph grGraph, AGameProcess p)
        {
            gameProc = (GoEGameProcess)p;
            graph = (GridGameGraph)grGraph;

            foreach (Pursuer pr in gameProc.Params.A_P)
                agentLocationAnswer[pr] = new Location(GameLogic.Location.Type.Unset);
            foreach (Evader ev in gameProc.Params.A_E)
                agentLocationAnswer[ev] = new Location(GameLogic.Location.Type.Unset);
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
        private void btnNext_Click(object sender, EventArgs e)
        {


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
                    displayedAgentPossibleDestinationPoints = new List<Point>();
                    displayedPolicyMarksLocations = new Dictionary<string, List<Point>>();
                    lstQuestions.Enabled = lstAgents.Enabled = lstChoices.Enabled = false;
                    currentMovements = new Dictionary<IAgent, Tuple<Point, Point>>();
                    gridController.setMovement(new Point(-1, -1), new Point(-1, -1));
                }

                try
                {
                    if (gameProc.invokeNextPolicy() == false)
                    {
                        MessageBox.Show("All Evadaers eliminated. Accumulated reward:" + gameProc.AccumulatedEvadersReward.ToString());
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
                        displayedEvaderLocations = new List<Point>();
                        chkEvaders_CheckedChanged(null, null); // refresh list
                    }

                    if (!chkPursuersDontToggle.Checked)
                        chkPursuers.Checked = true;
                    else
                    {
                        displayedPursuerLocations = new List<Point>();
                        chkPursuers_CheckedChanged(null, null); // refresh list
                    }
                }
                else
                {
                    btnNext.Text = "Invoke Evaders Policy";
                    
                    if(!chkEvadersDontToggle.Checked)
                        chkEvaders.Checked = true;
                    else
                    {
                        displayedEvaderLocations = new List<Point>();
                        chkEvaders_CheckedChanged(null, null); // refresh list
                    }

                    if (!chkPursuersDontToggle.Checked)
                        chkPursuers.Checked = false; // invokes an event that clears displayedPursuerLocations
                    else
                    {
                        displayedPursuerLocations = new List<Point>();
                        chkPursuers_CheckedChanged(null, null); // refresh list
                    }

                }
               
            }
            while ((--skipCount) > 0);

            refreshGridView();
        }

        private void GameProcessView_Load(object sender, EventArgs e)
        {
            
            gameView.resizeGrid(graph.WidthCellCount, graph.HeightCellCount, 10, false);
            gridController = new GridGameGraphController(gameView, graph);
            gridController.View.Invalidate();
            this.MouseWheel += frmGameProcessView_MouseWheel;
        }

        void frmGameProcessView_MouseWheel(object sender, MouseEventArgs e)
        {
            
            if (!isCtrlPressed)
                return;

            if (e.Delta > 0 && gameView.CellSize > 1)
            {
                gameView.CellSize--;
                if (isAltPressed)
                    gameView.CellSize -= 4;

                refreshGridView();
            }
            else if (e.Delta < 0)
            {
                gameView.CellSize++;
                if (isAltPressed)
                    gameView.CellSize += 4;
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
                agentLocationAnswer[a.Key] = a.Value.Item1;
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
            List<Location> res = new List<Location>();
            res.Add(agentLocationAnswer[mover]);
            return res;
        }
        private void chkEvaders_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEvaders.Checked)
            {
                foreach (Evader ev in gameProc.State.ActiveEvaders)
                {
                    //try
                    //{
                        Location dmyLoc = gameProc.State.L[gameProc.State.MostUpdatedEvadersLocationRound][ev];
                        if (dmyLoc.locationType == GameLogic.Location.Type.Node)
                            displayedEvaderLocations.Add(dmyLoc.nodeLocation);
                    //}
                    //catch(Exception)
                    //{
                    //    // if evaders were not yet set in round gameProc.currentRound, we try the previous round
                    //    try
                    //    {
                    //        Location dmyLoc = gameProc.State.L[gameProc.currentRound - 1][ev];
                    //        if (dmyLoc.locationType == GameLogic.Location.Type.Node)
                    //            displayedEvaderLocations.Add(dmyLoc.nodeLocation);
                    //    }
                    //    catch (Exception) { }
                    //}
                }
            }
            else
                displayedEvaderLocations = new List<Point>();

            refreshGridView();
        }
        private void chkPursuers_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPursuers.Checked)
            {
                foreach (Pursuer p in gameProc.Params.A_P)
                {
                    //try
                    //{
                        Location dmyLoc = gameProc.State.L[gameProc.State.MostUpdatedPursuersRound][p];
                        if (dmyLoc.locationType == GameLogic.Location.Type.Node)
                            displayedPursuerLocations.Add(dmyLoc.nodeLocation);
                    //}
                    //catch(Exception)
                    //{
                    //    try
                    //    {
                    //        // if pursuers were not yet set in round gameProc.currentRound, we try the previous round
                    //        Location dmyLoc = gameProc.State.L[gameProc.currentRound - 1][p];
                    //        if (dmyLoc.locationType == GameLogic.Location.Type.Node)
                    //            displayedPursuerLocations.Add(dmyLoc.nodeLocation);
                    //    }
                    //    catch (Exception) { }
                    //}
                }
            }
            else
                displayedPursuerLocations = new List<Point>();

            refreshGridView();
        }
        private void chkSensitive_CheckedChanged(object sender, EventArgs e)
        {
           
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
            pursuerLocations.AddRange(displayedLocationsReachableByPursuers);

            gridController.clearColors();
            gridController.setColors(
                pursuerLocations,//displayedPursuerLocations, 
                displayedEvaderLocations, 
                displayedAgentPossibleDestinationPoints,
                PolicyMarksLocations,
                displayedSensitiveLocations);
        }

        private void lstAgents_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                lstQuestions.Items.Clear();
                lstChoices.Items.Clear();

                int agentIndex = lstAgents.SelectedIndex;
                IAgent ag = agentRequest[agentIndex];

                if (latestInputRequest.ComboChoices.Keys.Contains(ag))
                    foreach (var q in latestInputRequest.ComboChoices[ag])                
                        lstQuestions.Items.Add(q.Item1);

                displayedAgentPossibleDestinationPoints = 
                    GameLogic.Utils.locationsToPoints(latestInputRequest.MovementOptions[ag].Item2);

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

        private void gameView_CellClick(object sender, uint cellX, uint cellY, MouseEventArgs m)
        {
            try
            {
                
                int agentIndex = lstAgents.SelectedIndex;
                if (agentIndex == -1)
                    return;
                IAgent ag = agentRequest[agentIndex];

                if (!displayedAgentPossibleDestinationPoints.Contains(new Point((int)cellX, (int)cellY)))
                    return;
                agentLocationAnswer[ag] = new Location(new Point((int)cellX,(int)cellY));
                gridController.setMovement(GameLogic.Utils.locationToPoint(latestInputRequest.MovementOptions[ag].Item1), GameLogic.Utils.locationToPoint(agentLocationAnswer[ag]));
                
                currentMovements[ag] = Tuple.Create(GameLogic.Utils.locationToPoint(latestInputRequest.MovementOptions[ag].Item1),
                                                    GameLogic.Utils.locationToPoint(agentLocationAnswer[ag]));
            }
            catch(Exception ex){}
        }
        
        private void gameView_Load(object sender, EventArgs e)
        {
            chkEvaders.CheckState = CheckState.Checked;
            chkPursuers.CheckState = CheckState.Checked;
            chkSensitive.CheckState = CheckState.Checked;
        }

        private void lstMarks_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshGridView();
        }

        private void chkSensitive_CheckStateChanged(object sender, EventArgs e)
        {
            List<Point> targets = graph.getNodesByType(NodeType.Target);

            if (chkSensitive.CheckState == CheckState.Checked)
            {
                displayedSensitiveLocations = new List<Point>();
                displayedSensitiveLocations.AddRange(targets);
                foreach (Point p in targets)
                {
                    displayedSensitiveLocations.AddRange(
                        graph.getNodesWithinDistance(p, gameProc.Params.r_e));
                }
            }
            else if (chkSensitive.CheckState == CheckState.Indeterminate)
                displayedSensitiveLocations = targets;
            else
                displayedSensitiveLocations = new List<Point>();


            refreshGridView();
        }

        private void gameView_MouseDown(object sender, MouseEventArgs e)
        {

            
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

        private void frmGameProcessView_Load(object sender, EventArgs e)
        {

        }

        private void chkReachable_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                displayedLocationsReachableByPursuers = new List<Point>();

                if (chkReachable.CheckState == CheckState.Checked)
                {

                    foreach (Pursuer p in gameProc.Params.A_P)
                    {
                        //try
                        //{
                            Location dmyLoc = gameProc.State.L[gameProc.State.MostUpdatedPursuersRound][p];
                            if (dmyLoc.locationType == GameLogic.Location.Type.Node)
                            {
                                displayedLocationsReachableByPursuers.AddRange(graph.getNodesWithinDistance(dmyLoc.nodeLocation, gameProc.Params.r_p * float.Parse(txtReachable.Text)));
                            }
                        //}
                        //catch (Exception)
                        //{
                        //    // we get here if pursuer was not positioned for current round, yet (or bad text box input)
                        //    Location dmyLoc = gameProc.State.L[gameProc.currentRound - 1][p];
                        //    if (dmyLoc.locationType == GameLogic.Location.Type.Node)
                        //    {
                        //        displayedLocationsReachableByPursuers.AddRange(graph.getNodesWithinDistance(dmyLoc.nodeLocation, gameProc.Params.r_p * float.Parse(txtReachable.Text)));
                        //    }
                        //}
                    }

                }



                refreshGridView();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

        private void gameView_SizeChanged(object sender, EventArgs e)
        {
            gameView.CellSize = (uint)
                    Math.Min(gameView.CellSize - (gameView.CellSize * gameView.HeightCellCount - gameView.Height) / gameView.HeightCellCount,
                             gameView.CellSize - (gameView.CellSize * gameView.WidthCellCount - gameView.Width) / gameView.WidthCellCount);

            refreshGridView();
        }
    }
}
