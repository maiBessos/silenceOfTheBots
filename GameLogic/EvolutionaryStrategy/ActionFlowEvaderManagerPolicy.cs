using AForge.Genetic;
using AForge.Math.Random;
using GoE.GameLogic.Algorithms;
using GoE.Policies;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using GoE.Utils.Extensions;
using GoE.AppConstants;

namespace GoE.GameLogic.EvolutionaryStrategy
{
    namespace EvaderSide
    {

        
        /// <summary>
        /// serves evaders policy  as it runs, by telling which IEvaderBasicAlgorithm object manages which agent. Each agent
        /// is either inactive, dead, or managed by exatcly one object
        /// </summary>
        public class ActionFlowEvaderManagerPolicy : EvaderManagerPolicy<ActionFlow>
        {
            private bool runsOutsideGA = false;

            public ActionFlowEvaderManagerPolicy()
            {
                // note: EvolutionConstants.param may not be set yet! wait for init()

                activeBasicAlgs = new List<IEvaderBasicAlgorithm>();
                ActionFlow st = (ActionFlow)bestChromosome;
                actionsGenerator = st;
                basicAlgNotifier = null;

                runsOutsideGA = true;
            }

            
            protected NotifyBasicAlgorithmSuccess basicAlgNotifier {get;set;}

           
 
            /// <summary>
            /// must be invoked at the begining of each round, before manager can be used.
            /// 
            /// This removes dead evaders, resets local sinks' currentSendingAgent to null, and updates evadersCountPerAlgCode
            /// 
            /// TODO: consider rewriting the current evader exchange method (repair budgets) into weighted bipartite matching, where each algorithm
            /// tells how much it wants each agent, and we allocate the evaders in a way that maximizes the total benefit (we also have the constraint that each alg needs 
            /// mininmal and maximal range of evaders)
            /// 
            /// </summary>
            Dictionary<Evader, Tuple<DataUnit, Location, Location>> evaderTargetLocations = null;

            bool currentlyRegrouping = false;
            int currentLayer = -1;// -1 means all evaders are managed by GoToSink() to regroup, and we wait until they all flushed their data
            int roundsUntilNextAlgorithmLayer = -1; 
            HashSet<Evader> currentlyActiveEvaders;

            /// <summary>
            /// for some X, this method returns param.A_E.Count / X value that is nearest to desiredEvesCount, 
            /// where X is integer
            /// </summary>
            /// <param name="desiredEvesCount"></param>
            /// <returns></returns>
            private int dividableEvadersCount(int desiredEvesCount)
            {
                return (int)(EvolutionConstants.param.A_E.Count / 
                             Math.Round(((float)EvolutionConstants.param.A_E.Count) / desiredEvesCount));
            }


            List<TaggedEvader> capturedEvaders = new List<TaggedEvader>();
            List<TaggedEvader> unsetEvaders = new List<TaggedEvader>();
            List<TaggedEvader> activeEvaders = new List<TaggedEvader>();

            bool evadersScrace = false; // if we reach a point where we don't have enough evaders to 
            // activate a flow in a perfect way, we activate the flow without letting it break - untill all evaders are destroyed

            // tells all evaders to go to sink, and makes sure that we have enough evaders for when we start 
            // the next flow
            protected void startRegroupEvaders()
            {
                activeBasicAlgs.Clear();
                
                int desiredEvesCount = dividableEvadersCount(actionsGenerator[ActionFlow.ShortIdx.InitialEvaderCount]);
                while (activeEvaders.Count < desiredEvesCount && 
                       unsetEvaders.Count > 0)
                {
                    activeEvaders.Add(unsetEvaders.Last());
                    unsetEvaders.RemoveAt(unsetEvaders.Count-1);
                }

                if (activeEvaders.Count < desiredEvesCount)
                    evadersScrace = true;

                activeBasicAlgs.Add(
                    new GoToSinkAlg(activeEvaders, actionsGenerator.RegroupGoToSinkAlgArg ));
                activeBasicAlgs.First().setSuccessNotifier(this.basicAlgNotifier);
                currentlyRegrouping = true;
                roundsUntilNextAlgorithmLayer = -1;
            }

            /// <summary>
            /// either starts or continues regrouping.
            /// If regrouping ended (all evaders are on sinks, and no more data to flush) - the flow
            /// will restart. 
            /// NOTE : Make sure that if the flow restarted (i.e. after invocation currentlyRegrouping == false) 
            /// then the new algorithm will have no evaders, so also refreshAlgorithmEvaders() should be invoked.
            /// </summary>
            public void regroupEvaders()
            {
                if (!currentlyRegrouping)
                    startRegroupEvaders();
                else
                {
                    bool untransmittedData = false;
                    var mem = s.M[s.MostUpdatedEvadersMemoryRound];
                    DataUnit tmp;
                    foreach (Evader e in s.ActiveEvaders)
                    {
                        if(mem.ContainsKey(e) && mem[e].getMinimalUnitNotInOtherSet(dataInSink, out tmp))
                        {
                            untransmittedData = true;
                            break;
                        }
                        
                    }

                    if (!untransmittedData)
                    {
                        // all data is flushed - we can invoke the flow, again
                        currentLayer = 0;
                        currentlyRegrouping = false;
                        activeBasicAlgs = actionsGenerator.generateAlgorithms(currentLayer, this.basicAlgNotifier);
                        roundsUntilNextAlgorithmLayer = (ushort)Math.Max(((ushort)1),actionsGenerator.RoundsPerLayer(currentLayer));

                        // TODO: remove below
                        //foreach (var e in activeEvaders)
                        //    if (s.L[s.MostUpdatedEvadersLocationRound][e.e].locationType != Location.Type.Node)
                        //    {
                        //        while (true) ;
                        //    }
                    }
                }
            }

            override public Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep()
            {
                HashSet<Evader> evadersToRemove = Utils.getEvadersToRemove(activeEvaders, s,O_d,EvolutionConstants.param);

                // update the lists capturedEvaders and activeEvaders
                for(int i = 0 ;i < activeEvaders.Count; ++i)
                    if(evadersToRemove.Contains(activeEvaders[i].e))
                    {
                        if (currentlyRegrouping)
                            activeBasicAlgs.First().loseEvader(activeEvaders[i].e);
                        capturedEvaders.Add(activeEvaders[i]);
                        
                        activeEvaders[i] = activeEvaders.Last();
                        activeEvaders.RemoveAt(activeEvaders.Count - 1);
                        --i;
                    }
                
                // TODO :remove below
                //foreach(var e in activeEvaders)
                //    if(s.L[s.MostUpdatedEvadersLocationRound][e.e].locationType != Location.Type.Node)
                //    {
                //        while (true) ;
                //    }

                if (activeEvaders.Count < actionsGenerator[ActionFlow.ShortIdx.MinimalEvaderCount] && !evadersScrace)
                    regroupEvaders(); // algorithm is broken
                else
                {
                    // try continuing the current flow:
                    // (this is skipped on the first round, since roundsUntilNextAlgorithmLayer = -1)
                    if (roundsUntilNextAlgorithmLayer == 0)
                    {
                        ++currentLayer;
                        if (currentLayer == EvolutionConstants.actionFlowMaxSequentialActions)
                            currentLayer = -1; // flow is finished - we now regroup
                        else
                        {
                            activeBasicAlgs = actionsGenerator.generateAlgorithms(currentLayer, this.basicAlgNotifier);
                            roundsUntilNextAlgorithmLayer = (ushort)Math.Max(((ushort)1),actionsGenerator.RoundsPerLayer(currentLayer));
                            
                            // TODO: remove below
                            //foreach (var e in activeEvaders)
                            //    if (s.L[s.MostUpdatedEvadersLocationRound][e.e].locationType != Location.Type.Node)
                            //    {
                            //        while (true) ;
                            //    }
                        }
                    }
                    else
                        --roundsUntilNextAlgorithmLayer;


                    if (currentLayer == -1)
                        regroupEvaders(); // flow ended (or didn't start yet)
                }


                if (!currentlyRegrouping) 
                {
                    foreach (IEvaderBasicAlgorithm st in activeBasicAlgs)
                        st.loseAllEvaders();

                    if (!refreshAlgorithmEvaders(O_d, ps)) // makes sure each algorithm manages the most suitable agents
                        regroupEvaders(); // algorithm is broken
                }
                
                foreach (LocalSink ls in localSinks)
                    ls.currentSendingAgent = null;

                Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
                foreach (IEvaderBasicAlgorithm st in activeBasicAlgs)
                {
                    var algRes = st.getNextStep(s,dataInSink,O_d,O_p, ps);

                    // TODO: remove below
                    //foreach (var e in algRes)
                    //    if (e.Value.Item2.locationType != Location.Type.Node)
                    //    {
                    //        while (true) ;
                    //    }


                    foreach (var r in algRes)
                        res.Add(r.Key,r.Value);
                }

                if (currentlyRegrouping)
                {
                    regroupEvaders();
                    if (!currentlyRegrouping)
                        refreshAlgorithmEvaders(O_d, ps);
                }


                if(ui.hasBoardGUI())
                {
                    List<string> lines  = new List<string>();

                    var v = actionsGenerator.getValueMap();
                    
                    foreach(var rec in v)
                        lines.Add(rec.Key + "=" + rec.Value);


                    for (int i = 0; i < activeBasicAlgs.Count;++i )
                    {
                        IEvaderBasicAlgorithm alg = activeBasicAlgs[i];
                        lines.Add("CurrentlyActive (" + alg.Evaders.Count.ToString() + " eves) :" + alg.GetType().Name);
                    }
                        
                    ui.addCurrentRoundLog(lines);
                }

                foreach (var e in unsetEvaders)
                    res[e.e] = 
                        new Tuple<DataUnit, Location, Location>(DataUnit.NIL, 
                            new Location(Location.Type.Unset),
                            new Location(Location.Type.Unset));
                
                ////TODO: remove below
                //if(res.Count < EvolutionConstants.param.A_E.Count - capturedEvaders.Count)
                //{
                //    while (true) ;
                //}

                return res;
            }
            
            /// <summary>
            /// assumes all active algorithms currently lost all evaders, and then 
            /// rearranges all active evaders between them
            /// </summary>
            /// <returns>
            /// if false, flow is broken 
            /// </returns>
            private bool refreshAlgorithmEvaders(
                HashSet<Point> O_d,
                PursuerStatistics ps)
            {

                if (activeEvaders.Count < activeBasicAlgs.Count)
                {
                    if(evadersScrace == false)
                        return false;

                    for (int i = 0; i < activeEvaders.Count; ++i)
                        activeBasicAlgs[i].handleNewEvader(activeEvaders[i]);
                        return true;
                }

                if(activeBasicAlgs.Count == 1)
                {

                    // TODO: remove below
                    //foreach (var e in activeEvaders)
                    //    if (s.L[s.MostUpdatedEvadersLocationRound][e.e].locationType != Location.Type.Node)
                    //    {
                    //        while (true) ;
                    //    }

                    activeBasicAlgs.First().handleNewEvaders(activeEvaders);
                    return true;
                }

                List<List<EvaluatedEvader>> evaluationPerAlgorithm = new List<List<EvaluatedEvader>>();
                    // will server as the costs matrix

                int remainingEvaders = activeEvaders.Count;

                int algIdx = 0;
                List<int> evadersPerAlg = new List<int>();
                for (; algIdx < activeBasicAlgs.Count; ++ algIdx )
                {
                    var evaluations = activeBasicAlgs[algIdx].getEvaderEvaluations(
                        activeEvaders,s,dataInSink,O_d,ps);

                    // TODO: consider adding a minimal evader count condition for each alg separately

                    int algEvaders;
                    
                    if(algIdx == (activeBasicAlgs.Count - 1))
                        algEvaders = remainingEvaders;
                    else
                    {
                        algEvaders = (int)Math.Round(remainingEvaders * actionsGenerator.EvaderCountPortionPerAction(currentLayer,algIdx));
                        remainingEvaders -= algEvaders;
                    }

                    // TODO: CRITICAL IMPROVEMENT: note that breaking the alg when algEvaders == 0 is problematic. Consider
                    // a SurviveAtAreaAlgorithm who's all purpose is serving as "fat cell" for other algorithms - 
                    // the alg. should continue by moving it's evaders to other algs.
                    // solution is defining priorities for algorithms (even 2 might be enough).
                    // First, we give evaders to high priority algorithms. Then, if they have more evaders then
                    // the maximal amount they start with, we give the rest of the evaders for the low priority
                    // algorithms
                    if (algEvaders == 0 && evadersScrace == false)
                        return false;
                    
                    evadersPerAlg.Add(algEvaders);

                    for(int i = 0; i < algEvaders; ++i)
                        evaluationPerAlgorithm.Add(evaluations); // each algorithm gets several (identical) rows in the matrix
                }


                // TODO: remove debug:
                //int sum1 = 0;
                //for(int k = 0; k < assignments.Count; ++k)
                //    for(int k2 = k+1; k2 < assignments.Count; ++k2)
                //        if(assignments[k] == assignments[k2])
                //        {

                //        }
                //for(int k = 0; k < evaluationPerAlgorithm.Count; ++k)
                //    if(evaluationPerAlgorithm[k].Count != evaluationPerAlgorithm.Count)
                //    {
                //        while (true) ;
                //    }

                // we translate double typed values to ints by multiplying by 2048 (i.e. there are 1000 different possible valus)
                var assignments = 
                    GraphAlgorithms.LinearAssignment.auction(
                        (int algIndex, int agentIdx) => { return (int)(2048 * evaluationPerAlgorithm[algIndex][agentIdx].value); }, 
                        evaluationPerAlgorithm.Count);

                
                
                

                int currentAlgEves = 0;
                algIdx = 0;
                for (int asIdx = 0; asIdx < assignments.Count; ++asIdx )
                {
                    activeBasicAlgs[algIdx].handleNewEvader(activeEvaders[assignments[asIdx]]);


                    // TODO: remove below
                    //if (s.L[s.MostUpdatedEvadersLocationRound][activeEvaders[assignments[asIdx]].e].locationType != Location.Type.Node)
                    //{
                    //    while (true) ;
                    //}

                    if (++currentAlgEves >= evadersPerAlg[algIdx])
                    {
                        ++algIdx;
                        currentAlgEves = 0;
                    }
                }

                return true;
            }

            /// <summary>
            /// algorithms that intend to create local sinks should add to this set, and remove when its no longer maintained.
            /// Evaders that intend to transmit into a local sink, should update that sink's 'currentSendingAgent', to avoid collisions
            /// </summary>
            public HashSet<LocalSink> localSinks { get; set; }

            /// <summary>
            /// tells which algorithm is currently managing which evader excluding idleEvaders (indices corespond EvolutionConstants.actionAlgorithmsByCode)
            /// </summary>
            public List<IEvaderBasicAlgorithm> activeBasicAlgs { get; set; }

            
            /// <summary>
            /// used in getNextStep()
            /// </summary>
            private ActionFlow actionsGenerator { get; set; }


            GameState s {get;set;}
            HashSet<Point> O_d, O_p;
            Algorithms.PursuerStatistics ps {get;set;}



            public override void setGameState(int currentRound, IEnumerable<Point> currentO_d, HashSet<Point> currentO_p, GameState state)
            {
                O_d = new HashSet<Point>(currentO_d);
                O_p = currentO_p;
                ps.update(currentO_p);
                s = state;

                // update data that reached to sink
                updateDataInSink(state);
            }

            Dictionary<string, string> policyInput;
            UI.IPolicyGUIInputProvider ui;
            public override bool init(AGameGraph G, GoEGameParams prm, AGoEPursuersPolicy p, UI.IPolicyGUIInputProvider pgui,
                Dictionary<string, string> PolicyInput)
            {
                policyInput = PolicyInput;

                if (actionsGenerator == null)
                {
                    MessageBox.Show("bestChromosome must be set before using ActionFlowEvaderManagerPolicy!");
                    throw new Exception("bestChromosome must be set before using ActionFlowEvaderManagerPolicy!");
                }

                if (runsOutsideGA)
                {
                    // now we actually need to make sure the global evolution constants corespond the current params
                    // TODO: super dirty
                    EvolutionConstants.param = prm;
                    EvolutionConstants.graph = (GridGameGraph)G;
                }

                foreach (Evader e in EvolutionConstants.param.A_E)
                    unsetEvaders.Add(new TaggedEvader(e));

                ui = pgui;

                dataInSink = new Utils.DataUnitVec();
                dataInSink.Add(DataUnit.NIL);
                dataInSink.Add(DataUnit.NOISE);
                localSinks = new HashSet<LocalSink>();
                ps = new PursuerStatistics(EvolutionConstants.graph, 10); // TODO: should we make this part of the chromosome?
                return true;
            }

            public override void initializeChromosome(IChromosome i, NotifyBasicAlgorithmSuccess notifier)
            {
                runsOutsideGA = false;
                ActionFlow st = (ActionFlow)i;
                actionsGenerator = st;
                basicAlgNotifier = notifier;
            }

            protected override List<ArgEntry> PolicyParamsInput
            {
                get
                {
                    return new List<ArgEntry>();
                }
            }
        }  
    }
}