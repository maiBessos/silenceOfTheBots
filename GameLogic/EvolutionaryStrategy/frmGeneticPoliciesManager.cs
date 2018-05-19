using AForge.Genetic;
using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.GameLogic.EvolutionaryStrategy.EvaderSide;
using GoE.Policies;
using GoE.UI;
using GoE.Utils;
using GoE.Utils.Genetic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.GameLogic
{
    public partial class frmGeneticPoliciesManager : Form
    {
        public frmGeneticPoliciesManager(GridGameGraph graphToUse, GoEGameParams paramsToUse)
        {
            InitializeComponent();
            g = graphToUse;
            gp = paramsToUse;
        }

        static GoEGameParams gp;
        private Button btnWorldState;
        private Label label1;
        private TextBox txtIterationCount;
        private ListBox listBox1;
        private Label label2;
        private Panel panel1;
        private RichTextBox txtLog2;
        private RichTextBox txtLog1;
        private TextBox txtThreads;
        private Label label3;
        private Button btnActionFlow;
        private Label label4;
        private TextBox txtPopSize;
        static GridGameGraph g; // TODO: shouldn't be static. seriously, this whole form class is designed poorly!


        private void InitializeComponent()
        {
            this.btnWorldState = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtIterationCount = new System.Windows.Forms.TextBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.txtLog2 = new System.Windows.Forms.RichTextBox();
            this.txtLog1 = new System.Windows.Forms.RichTextBox();
            this.txtThreads = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnActionFlow = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPopSize = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnWorldState
            // 
            this.btnWorldState.Location = new System.Drawing.Point(12, 12);
            this.btnWorldState.Name = "btnWorldState";
            this.btnWorldState.Size = new System.Drawing.Size(113, 37);
            this.btnWorldState.TabIndex = 0;
            this.btnWorldState.Text = "world state evader evolver";
            this.btnWorldState.UseVisualStyleBackColor = true;
            this.btnWorldState.Click += new System.EventHandler(this.btnWorldStateEvaders_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(148, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(18, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "vs";
            // 
            // txtIterationCount
            // 
            this.txtIterationCount.Location = new System.Drawing.Point(438, 59);
            this.txtIterationCount.Name = "txtIterationCount";
            this.txtIterationCount.Size = new System.Drawing.Size(35, 20);
            this.txtIterationCount.TabIndex = 2;
            this.txtIterationCount.Text = "30";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Items.AddRange(new object[] {
            "Solution2"});
            this.listBox1.Location = new System.Drawing.Point(188, 43);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(82, 17);
            this.listBox1.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(334, 59);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "generations to add:";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.txtLog2);
            this.panel1.Controls.Add(this.txtLog1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 111);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(932, 435);
            this.panel1.TabIndex = 7;
            // 
            // txtLog2
            // 
            this.txtLog2.Dock = System.Windows.Forms.DockStyle.Right;
            this.txtLog2.Location = new System.Drawing.Point(411, 0);
            this.txtLog2.Name = "txtLog2";
            this.txtLog2.Size = new System.Drawing.Size(521, 435);
            this.txtLog2.TabIndex = 7;
            this.txtLog2.Text = "";
            // 
            // txtLog1
            // 
            this.txtLog1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog1.Location = new System.Drawing.Point(0, 0);
            this.txtLog1.Name = "txtLog1";
            this.txtLog1.Size = new System.Drawing.Size(932, 435);
            this.txtLog1.TabIndex = 6;
            this.txtLog1.Text = "";
            // 
            // txtThreads
            // 
            this.txtThreads.Location = new System.Drawing.Point(624, 56);
            this.txtThreads.Name = "txtThreads";
            this.txtThreads.Size = new System.Drawing.Size(35, 20);
            this.txtThreads.TabIndex = 8;
            this.txtThreads.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(559, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Threads# :";
            // 
            // btnActionFlow
            // 
            this.btnActionFlow.Location = new System.Drawing.Point(12, 59);
            this.btnActionFlow.Name = "btnActionFlow";
            this.btnActionFlow.Size = new System.Drawing.Size(113, 37);
            this.btnActionFlow.TabIndex = 10;
            this.btnActionFlow.Text = "Action Flow Evader Evolver";
            this.btnActionFlow.UseVisualStyleBackColor = true;
            this.btnActionFlow.Click += new System.EventHandler(this.btnActionFlow_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(334, 36);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Pop. Size:";
            // 
            // txtPopSize
            // 
            this.txtPopSize.Location = new System.Drawing.Point(399, 36);
            this.txtPopSize.Name = "txtPopSize";
            this.txtPopSize.Size = new System.Drawing.Size(35, 20);
            this.txtPopSize.TabIndex = 11;
            this.txtPopSize.Text = "100";
            // 
            // frmGeneticPoliciesManager
            // 
            this.ClientSize = new System.Drawing.Size(932, 546);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtPopSize);
            this.Controls.Add(this.btnActionFlow);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtThreads);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.txtIterationCount);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnWorldState);
            this.Name = "frmGeneticPoliciesManager";
            this.Load += new System.EventHandler(this.frmGeneticPoliciesManager_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void frmGeneticPoliciesManager_Load(object sender, EventArgs e)
        {
            txtThreads.Text = (Environment.ProcessorCount * 2).ToString();
        }

        
        class WorldStateEvadersEvolutionFitness : 
            EvadersEvolutionFitness<WorldStateToActionEvaderManagerPolicy, WorldStateEvaderStrategyChromosome>
        {
            public WorldState getWorldState(IChromosome strategy, int iterations)
            {
                GameLogic.GoEGameProcess gproc = new GameLogic.GoEGameProcess();
                gproc.initParams(gp, g);


                WorldStateToActionEvaderManagerPolicy chosenEvaderPolicy = new WorldStateToActionEvaderManagerPolicy();
                chosenEvaderPolicy.initializeChromosome((WorldStateEvaderStrategyChromosome)strategy, null);

                AGoEPursuersPolicy chosenPursuerPolicy = new PatrolAndPursuit();

                chosenPursuerPolicy.init(g, gp, inp,null);
                chosenEvaderPolicy.init(g, gp, chosenPursuerPolicy, inp,null);
                gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);

                while (iterations-- > 0 && gproc.invokeNextPolicy()) ;

                return chosenEvaderPolicy.currentWorldState;
            }
        }
        class EvadersEvolutionFitness<EvaderManagerType, ChromosomeType> : IFitnessFunction
            where EvaderManagerType : EvaderManagerPolicy<ChromosomeType>, new()
            where ChromosomeType : IChromosome, new()
        {
            protected InitOnlyPolicyInput inp = new InitOnlyPolicyInput();//(false, new Dictionary<string, string>());
            
            public double Evaluate(IChromosome chromosome)
            {
                double avgReward = 0;
                for (int rep = 0; rep < EvolutionConstants.EvaluationRepetitions; ++rep)
                {
                    GameLogic.GoEGameProcess gproc = new GameLogic.GoEGameProcess();
                    gproc.initParams(gp, g);

                    int basicAlgSuccessCounter = 0;
                    EvaderManagerType chosenEvaderPolicy = new EvaderManagerType();
                    chosenEvaderPolicy.initializeChromosome((ChromosomeType)chromosome, () => { ++basicAlgSuccessCounter; });

                    AGoEPursuersPolicy chosenPursuerPolicy = new PatrolAndPursuit();

                    chosenPursuerPolicy.init(g, gp, inp,null);
                    chosenEvaderPolicy.init(g, gp, chosenPursuerPolicy, inp,null);
                    gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);

                    int iteration = 0;

                    EvolutionConstants.CurrentSimulationRoundCount = EvolutionUtils.threadSafeRand.rand.Next(EvolutionConstants.MinSimulationRoundCount, EvolutionConstants.MaxSimulationRoundCount);
                    while (gproc.invokeNextPolicy() && ++iteration < EvolutionConstants.CurrentSimulationRoundCount) ;

                    if (iteration == EvolutionConstants.CurrentSimulationRoundCount)
                        avgReward += (chosenEvaderPolicy.dataInSink.Count - 2) +  // -2 to exclude Data.NOISE and Data.NIL
                                     1 - (1.0 / (1 + basicAlgSuccessCounter)); // basic alg. success is always secondary to actual leaked data
                    else
                        avgReward += gproc.AccumulatedEvadersReward + 1 - (1.0 / (1 + basicAlgSuccessCounter)); 
                }

                return avgReward / EvolutionConstants.EvaluationRepetitions;
            }
        }

        Dictionary<Type, MultiThreadEvaluationPopulation<IChromosome>> evadersPopulation =
            new Dictionary<Type, MultiThreadEvaluationPopulation<IChromosome>>();

        int generation = 0;

        private void evolveEvaders<EvaderManagerType, ChromosomeType>(Button btn) where EvaderManagerType : EvaderManagerPolicy<ChromosomeType> , new()
                                                                                  where ChromosomeType : IChromosome, new()
        {
            int tcount;
            if (!Int32.TryParse(txtThreads.Text, out tcount))
            {
                MessageBox.Show("Illegal thread count");
                return;
            }


            if (!evadersPopulation.ContainsKey(typeof(ChromosomeType)))
            {
                ChromosomeType initialChromosome = new ChromosomeType();

                EvadersEvolutionFitness<EvaderManagerType, ChromosomeType> worldInitEvaluation = new EvadersEvolutionFitness<EvaderManagerType, ChromosomeType>();

                //EvolutionConstants.initialWorldState = worldInitEvaluation.getWorldState(initialChromosome, 3);


                evadersPopulation[typeof(ChromosomeType)] =
                    new MultiThreadEvaluationPopulation<IChromosome>(EvolutionConstants.populationCount,
                    initialChromosome,
                    worldInitEvaluation,
                    new RankSelection(),
                    new CustomThreadPool(tcount, () => { EvolutionUtils.threadSafeRand = new ThreadSafeRandom(); }),false);

                evadersPopulation[typeof(ChromosomeType)].MutationRate = EvolutionConstants.strategyMutationRate;

            }


            int iterations = Int32.Parse(txtIterationCount.Text);
            int printIterations = Math.Max(5, iterations / 100);

            Dictionary<string, string> prevVals = null;

            btn.Enabled = false;
            ThreadPool.QueueUserWorkItem(new WaitCallback((object o) =>
            {
                EvolutionUtils.threadSafeRand = new ThreadSafeRandom();
                while (iterations-- > 0)
                {
                    EvolutionConstants.CurrentSimulationRoundCount = EvolutionUtils.threadSafeRand.rand.Next(EvolutionConstants.MinSimulationRoundCount, EvolutionConstants.MaxSimulationRoundCount);

                    evadersPopulation[typeof(ChromosomeType)].runEpochParallel();
                    ++generation;

                    if (iterations % printIterations != 0)
                        continue;

                    this.Invoke(new Action(() =>
                    {

                        txtLog1.Clear();
                        txtLog1.Text = txtLog2.Text;
                        txtLog2.Clear();

                        // TODO: consider letting the relevant chromsomes inherit an interface that forces them 
                        // to implement getValueMap()
                        Dictionary<string, string> currentVals =
                            (Dictionary<string, string>)typeof(ChromosomeType).GetMethod("getValueMap").Invoke(evadersPopulation[typeof(ChromosomeType)].BestChromosome, null);

                        txtLog2.AppendText("Generation:" + generation.ToString() + Environment.NewLine, Color.Green);
                        txtLog2.AppendText("Reward:" + evadersPopulation[typeof(ChromosomeType)].BestChromosome.Fitness.ToString() + Environment.NewLine, Color.Green);
                        txtLog2.AppendText("Game rounds limitation:" + EvolutionConstants.CurrentSimulationRoundCount.ToString() + Environment.NewLine, Color.Green);

                        foreach (var v in currentVals)
                        {
                            string txtLine = (v.Key + "=" + v.Value) + Environment.NewLine;
                            Color lineColor = Color.Black;
                            if (prevVals != null &&
                                (!prevVals.ContainsKey(v.Key) || prevVals[v.Key] != v.Value))
                                lineColor = Color.Red;

                            txtLog2.AppendText(txtLine, lineColor);
                        }
                        prevVals = currentVals;
                        txtLog2.Copy();

                        UIUtils.updateControl(txtLog1);
                        UIUtils.updateControl(txtLog2);
                    })); // invoked on GUI thread
                }
                EvaderManagerPolicy<ChromosomeType>.bestChromosome = 
                    (ChromosomeType)evadersPopulation[typeof(ChromosomeType)].BestChromosome;

                this.Invoke(new Action(() =>
                {
                    btn.Enabled = true;
                }));

            }));
        }

        private void btnWorldStateEvaders_Click(object sender, EventArgs e)
        {

            EvolutionConstants.maxSimultenousActiveBasicAlgorithms = 10;

            EvolutionConstants.actionAlgorithmsByCode = new List<IEvaderBasicAlgorithm>();
            EvolutionConstants.actionAlgorithmsByCode.Add(new GoToSinkAlg());
            EvolutionConstants.actionAlgorithmsByCode.Add(new SurviveAtArea());

            EvolutionConstants.areaStatesCount = 2;
            EvolutionConstants.assumedMaxPursuitRoundCount = 5;
            EvolutionConstants.basicAlgorithmRepairingWorthCeiling = 100;
            EvolutionConstants.graph = g;
            EvolutionConstants.initialStrategyTableSize = 2;
            EvolutionConstants.param = gp;

            //CompositeChromosome.InitialValuesMeanFactor = 0.1;
            ConcreteCompositeChromosome.InitialValuesMeanFactor = -1;

            EvolutionConstants.MinSimulationRoundCount = 30;
            EvolutionConstants.MaxSimulationRoundCount = 60;

            EvolutionConstants.initialMaxEntryProbability = 1;
            EvolutionConstants.initialMaxEntryDistance = 100;
            EvolutionConstants.initialImpactFactorWeightValue = 0;

            EvolutionConstants.strategyMutationRate = 1;
            EvolutionConstants.switchAlgorithmCodeMutationProb = 0.1f;
            EvolutionConstants.valueMutationProb = 0.1f;
            EvolutionConstants.populationCount = Int32.Parse(txtPopSize.Text);
            EvolutionConstants.EvaluationRepetitions = 5;

            evolveEvaders<WorldStateToActionEvaderManagerPolicy,WorldStateEvaderStrategyChromosome> (btnWorldState);
        }

        private void btnActionFlow_Click(object sender, EventArgs e)
        {
            EvolutionConstants.maxSimultenousActiveBasicAlgorithms = 10;

            EvolutionConstants.actionAlgorithmsByCode = new List<IEvaderBasicAlgorithm>();
            EvolutionConstants.actionAlgorithmsByCode.Add(new GoToSinkAlg());
            EvolutionConstants.actionAlgorithmsByCode.Add(new SurviveAtArea());

            EvolutionConstants.graph = g;
            EvolutionConstants.actionFlowMaxParallelActions = 2;
            EvolutionConstants.actionFlowMaxSequentialActions = 2;
            EvolutionConstants.param = gp;

            //CompositeChromosome.InitialValuesMeanFactor = 0.1;
            ConcreteCompositeChromosome.InitialValuesMeanFactor = -1;

            EvolutionConstants.MinSimulationRoundCount = 2000;
            EvolutionConstants.MaxSimulationRoundCount = 4000;

            EvolutionConstants.strategyMutationRate = 0.75f;
            EvolutionConstants.switchAlgorithmCodeMutationProb = 0.2f;
            EvolutionConstants.valueMutationProb = 0.2f;
            EvolutionConstants.populationCount = Int32.Parse(txtPopSize.Text);
            EvolutionConstants.EvaluationRepetitions = 10; // each repetiion has different amount of round limitation

            // uncomment:
            //evolveEvaders<ActionFlowEvaderManagerPolicy,ActionFlow> (btnActionFlow);

            // TODO: remove below
            ActionFlow f = new ActionFlow();

            ConcreteCompositeChromosome c1 = (ConcreteCompositeChromosome)EvolutionConstants.actionAlgorithmsByCode[0].CreateNewParam();
            c1[ConcreteCompositeChromosome.Shorts, 0] = 1;
            c1[ConcreteCompositeChromosome.Shorts, 1] = 1;
            c1[ConcreteCompositeChromosome.Shorts, 2] = 1;
            f.Actions(0, 0).algCode = 0;
            f.Actions(0, 0).algArgs = c1;
            f.Actions(0, 1).setNoAction();

            ConcreteCompositeChromosome c2 = (ConcreteCompositeChromosome)EvolutionConstants.actionAlgorithmsByCode[1].CreateNewParam();
            c2[ConcreteCompositeChromosome.Shorts, (int)SurviveAtArea.ShortParamsIdx.MinmalRing] = 9;
            c2[ConcreteCompositeChromosome.Shorts, (int)SurviveAtArea.ShortParamsIdx.MaximalRing] = 9;
            c2[ConcreteCompositeChromosome.Shorts, (int)SurviveAtArea.ShortParamsIdx.DistanceFromNonOuterRingEvaders] = 1;
            c2[ConcreteCompositeChromosome.Shorts, (int)SurviveAtArea.ShortParamsIdx.DistanceFromOuterRingEvaders] = 1;
            c2[ConcreteCompositeChromosome.Doubles, (int)SurviveAtArea.FracParamsIdx.MaxRouteLength] = 3;
            f.Actions(1, 0).algCode = 1;
            f.Actions(1, 0).algArgs = c2;
            f.Actions(1, 1).setNoAction();
            f[ActionFlow.Shorts, (int)ActionFlow.ShortIdx.InitialEvaderCount] = 1;
            f[ActionFlow.Shorts, (int)ActionFlow.ShortIdx.MinimalEvaderCount] = 0;
            f[ActionFlow.Shorts, 2] = 1; // rounds in GoToSink
            f[ActionFlow.Shorts, 3] = 60; // rounds in SurviveInArea
            f.RegroupGoToSinkAlgArg.Generate();
            ActionFlowEvaderManagerPolicy.bestChromosome = f;
        }
    }
}
