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
using GoE.Utils.Algorithms;
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
        public class WorldStateToActionEvaderManagerPolicy : EvaderManagerPolicy<WorldStateEvaderStrategyChromosome>
        {
            public class ManagedBasicAlgorithmTaggedEvader : TaggedEvader
            {
                public ManagedBasicAlgorithmTaggedEvader(TaggedEvader src, ManagedBasicAlgorithm Manager)
                        : base (src)
                {
                    manager = Manager;
                }
                public ManagedBasicAlgorithm manager;
            }

            private class EvaluatedEvaderEx : EvaluatedEvader
            {
                public EvaluatedEvaderEx(EvaluatedEvader src, double Cost, ManagedBasicAlgorithm AlgManager)
                    : base(src)
                {
                    this.cost = Cost;
                    this.value /= Cost;
                    algManager = AlgManager;
                }
                public ManagedBasicAlgorithm algManager;
                public double cost;
            }
            private struct RepairingNeedsEx
            {
                public int minEvadersCount;
                public int maxEvadersCount;
                public List<EvaluatedEvaderEx> priorityWeightPerEvader;
            }
            public struct BuyEvadersRes
            {
                /// <summary>
                /// if algorithmBroken is true, this is used to replace the destroyed alg (may be null)
                /// </summary>
                public List<ManagedBasicAlgorithm> replacementAlg;

                /// <summary>
                /// if true, the algorithm will be removed
                /// </summary>
                public bool algorithmBroken;
            }
            public class ManagedBasicAlgorithm : IComparable<ManagedBasicAlgorithm>
            {
                public IEvaderBasicAlgorithm alg;

                /// <summary>
                /// updates the worth of the algorithm, according to LostWorthPerRound, worthCeiling as given on creation
                /// NOTE: should be called by WorldStateToActionEvaderManagerPolicy only
                /// </summary>
                public void updateWorth()
                {
                    RepairBudget = (RepairBudget - LostWorthPerRound).LimitRange(0, WorthCeiling);
                }
               
                /// <summary>
                /// tells the algorithm it must give up one of its evaders (because it was either destroyed, or moved to another repetitive state)
                /// The algorithm will later have a chance to repair itself by buying evaders from other algorithms.
                /// </summary>
                /// <param name="compensationWorth">
                /// if evader was destroyed, typically there won't be a compensation.
                /// Otherwise, the compensation would typically be the exact worth of the evader.
                /// </param>
                /// <returns>
                /// true if evader was in algorithm, false otherwise
                /// </returns>
                /// <remarks>
                /// NOTE: should be called by WorldStateToActionEvaderManagerPolicy only
                /// </remarks>
                public bool loseEvader(TaggedEvader lostEvader, double compensationWorth)
                {
                    double freedWorth;
                    if (worthPerEvader.TryGetValue(lostEvader.e, out freedWorth))
                    {
                        RepairBudget += compensationWorth;
                        EvadersWorth -= freedWorth;
                        worthPerEvader.Remove(lostEvader.e);
                        alg.loseEvader(lostEvader.e);
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// serves GoToSink, when evaders are waiting to be used
                /// </summary>
                public void setZeroWorth()
                {
                    RepairBudget = 0;
                    EvadersWorth = 0;

                    Dictionary<Evader, double> newWorthPerEvader = new Dictionary<Evader, double>();
                    foreach (Evader e in worthPerEvader.Keys)
                        newWorthPerEvader[e] = 0;
                    worthPerEvader = newWorthPerEvader;
                }

                /// <summary>
                /// tells what to do with (all!) the evaders this algorithm owns, in case this algorithm can't repair itself 
                /// NOTE: may be null only if the algorithm owns no evaders
                /// </summary>
                /// <remarks>
                /// default implementation transfers all evaders to GoToSink(), and reevaluates it using evaluateIdleEvader().
                /// TODO: should this really be virtual?
                /// </remarks>
                public List<ManagedBasicAlgorithm> getDeconstructedAlg(GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink)
                {
                    if (worthPerEvader == null || worthPerEvader.Count == 0)
                        return null;

                    List<ManagedBasicAlgorithm> res = new List<ManagedBasicAlgorithm>();
                    foreach (var v in worthPerEvader)
                    {
                        ManagedBasicAlgorithm newAlg = new ManagedBasicAlgorithm();
                        newAlg.alg = new GoToSinkAlg();
                        res.Add(newAlg);
                        newAlg.worthPerEvader = new Dictionary<Evader, double>();
                        newAlg.worthPerEvader[v.Key] = newAlg.WorthCeiling = newAlg.EvadersWorth = 
                            evaluateIdleEvader(v.Key, s, unitsInSink);
                        newAlg.RepairBudget = 0;
                        newAlg.LostWorthPerRound = 0;
                        newAlg.AddedWorthPerReward = 0;

                    }

                    //for (int i = 0; i < worthPerEvader.Count; ++i)
                    //{
                    //    Evader e = worthPerEvader.Keys.ElementAt(i);
                    //    worthPerEvader[e] = evaluateIdleEvader(e, s, unitsInSink);
                    //}
                    //GoToSinkAlg res = new GoToSinkAlg();
                    //res.worthPerEvader = worthPerEvader;
                    return res;
                }



                /// <summary>
                /// This method uses the repair budget in order to buy the most satisfying team of agents.
                /// For each algorithm that gets its evaders taken away, loseEvader() is called.
                /// After the operation is done, handleNewEvaders() is called
                /// NOTE: should be called by WorldStateToActionEvaderManagerPolicy only
                /// </summary>
                public WorldStateToActionEvaderManagerPolicy.BuyEvadersRes buyEvaders(IEnumerable<ManagedBasicAlgorithm> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec dataInSink,
                    HashSet<Point> O_d,
                    PursuerStatistics ps)
                {
                    
                    //Dictionary<IEvaderBasicAlgorithm, ManagedBasicAlgorithm> managers = new Dictionary<IEvaderBasicAlgorithm, ManagedBasicAlgorithm>();
                    //foreach (var man in availableAgents)
                    //    managers.Add(man.alg, man);


                    List<ManagedBasicAlgorithmTaggedEvader> evaders = new List<ManagedBasicAlgorithmTaggedEvader>();
                    foreach (var basicAlg in availableAgents)
                    {
                        if (availableAgents == null || basicAlg.worthPerEvader == null)
                            continue;
                        foreach (var ev in basicAlg.worthPerEvader)
                            evaders.Add(new ManagedBasicAlgorithmTaggedEvader(new TaggedEvader(ev.Key), basicAlg));
                    }


                    //GoE.Utils.EnumerableTranslator<ManagedBasicAlgorithm,IEvaderBasicAlgorithm>.translate(availableAgents, (ManagedBasicAlgorithm a)=>{return a.alg;}),
                    RepairingNeeds constraintsRaw = 
                        alg.getRepairingNeeds(evaders, s, dataInSink, O_d, ps);

                    RepairingNeedsEx constraints = new RepairingNeedsEx();
                    constraints.maxEvadersCount = constraintsRaw.maxEvadersCount;
                    constraints.minEvadersCount = constraintsRaw.minEvadersCount;
                    constraints.priorityWeightPerEvader = new List<EvaluatedEvaderEx>();
                    foreach (EvaluatedEvader ev in constraintsRaw.priorityWeightPerEvader)
                        constraints.priorityWeightPerEvader.Add(new EvaluatedEvaderEx(ev, worthPerEvader[ev.e.e], ((ManagedBasicAlgorithmTaggedEvader)ev.e).manager ));

                    if (constraints.minEvadersCount == -1 ||
                       ((constraints.priorityWeightPerEvader != null && constraints.minEvadersCount > constraints.priorityWeightPerEvader.Count)))
                        return new BuyEvadersRes() { algorithmBroken = true, replacementAlg = getDeconstructedAlg(s, dataInSink) };

                    if (constraints.maxEvadersCount == 0)
                        return new BuyEvadersRes() { algorithmBroken = false };

                    // we need to solve/approximate a slight variation of knapscak problem - we have limited budget, we want to maximize utility, 
                    // but we also need a minimal amount of evaders

                    constraints.priorityWeightPerEvader.Sort(new Comparison<EvaluatedEvaderEx>((lhs, rhs) => rhs.cost.CompareTo(lhs.cost))); // cheaper evaders last

                    List<EvaluatedEvaderEx> chosenEvaders = new List<EvaluatedEvaderEx>();

                    // transfer cheapest objects from priorityWeightPerEvader to chosenEvaders-
                    double remainingBudget = RepairBudget;
                    for (int i = 0; i < constraints.maxEvadersCount; ++i)
                    {
                        if (constraints.priorityWeightPerEvader.Count == 0 ||
                           remainingBudget <= constraints.priorityWeightPerEvader.Last().cost)
                            break;
                        remainingBudget -= constraints.priorityWeightPerEvader.Last().cost;
                        chosenEvaders.Add(constraints.priorityWeightPerEvader.Last());
                        constraints.priorityWeightPerEvader.RemoveAt(constraints.priorityWeightPerEvader.Count - 1);
                    }
                    if (remainingBudget < 0 || chosenEvaders.Count < constraints.minEvadersCount)
                        return new BuyEvadersRes() { algorithmBroken = true, replacementAlg = getDeconstructedAlg(s, dataInSink) };

                    // sort chosenEvaders and re-sort priorityWeightPerEvader by item efficiency

                    // least efficient first
                    chosenEvaders.Sort(new Comparison<EvaluatedEvader>((lhs, rhs) => lhs.value.CompareTo(rhs.value)));

                    // most efficient first
                    constraints.priorityWeightPerEvader.
                        Sort(0,
                             constraints.priorityWeightPerEvader.Count,
                             Comparer<EvaluatedEvader>.Create(new Comparison<EvaluatedEvader>((lhs, rhs) => rhs.value.CompareTo(lhs.value))));

                    // we move sorted items into a list, so we can remove items easily
                    LinkedList<EvaluatedEvaderEx> remainingEvaders =
                        new LinkedList<EvaluatedEvaderEx>(constraints.priorityWeightPerEvader);


                    // replace inefficient items:
                    // TODO: inefficient nested loop. We try to replace every chosen item with all other items, and I'm sure this can be reduced to nlogn instead of n^2 (but I'm not sure that in practive this will be faster - this needs profiling)
                    for (int i = 0; i < chosenEvaders.Count; ++i)
                    {
                        double replacedBudget = chosenEvaders[i].cost;
                        double minimalEfficiency = chosenEvaders[i].value;

                        var remIt = remainingEvaders.First;
                        while (remIt != null && remIt.Value.value > minimalEfficiency)
                        {
                            var next = remIt.Next;
                            if (remIt.Value.cost < replacedBudget + remainingBudget)
                            {
                                remainingBudget = remainingBudget + replacedBudget - remIt.Value.cost;
                                chosenEvaders[i] = remIt.Value;
                                remainingEvaders.Remove(remIt);
                            }
                            remIt = next;
                        }
                    }


                    EvadersWorth += RepairBudget - remainingBudget;
                    RepairBudget = remainingBudget;


                    // if the algorithm was just created and buys evaders for the first time, 
                    // it now increases their value. We spread the repair budget to insure EvadersWorth >= remaining RepairBudget.
                    // TODO: 1)this is a heuristic that insures evaders have some worth (otherwise, all evaders may always have 0 worth). can we think of something better?
                    // TODO: 2) current heuristic implementation is a bit cumbersome and probably could be faster. consider revising
                    if (worthPerEvader == null && EvadersWorth < RepairBudget)
                    {
                        worthPerEvader = new Dictionary<Evader, double>();
                        double equalWorths = (EvadersWorth + RepairBudget) / 2;
                        double totalEvaderWorthToAdd = equalWorths - EvadersWorth;

                        List<double> normalizedUtilityPerEvader = new List<double>(chosenEvaders.Count);
                        double totalUtility = 0;

                        foreach (var e in chosenEvaders)
                            totalUtility += e.value * e.cost;

                        totalUtility = Math.Max(totalUtility, 0.001); // TODO: we need to define epsilon values
                        foreach (var e in chosenEvaders)
                            normalizedUtilityPerEvader.Add((e.value * e.cost) / totalUtility);

                        // we add some worth to each evader, in proportion to its relative utility for the algorithm, such that the total additions will be 'totalEvaderWorthToAdd'
                        for (int i = 0; i < chosenEvaders.Count; ++i)
                        {
                            EvaluatedEvaderEx e = chosenEvaders[i];
                            e.algManager.loseEvader(e.e, e.cost);
                            worthPerEvader.Add(e.e.e, e.cost + normalizedUtilityPerEvader[i] * totalEvaderWorthToAdd);
                        }
                        RepairBudget = EvadersWorth = equalWorths;
                    }
                    else
                    {
                        if (worthPerEvader == null)
                            worthPerEvader = new Dictionary<Evader, double>();
                        foreach (var e in chosenEvaders)
                        {
                            e.algManager.loseEvader(e.e, e.cost);
                            worthPerEvader.Add(e.e.e, e.cost);
                        }
                    }

                    // update the algorithm that it now has more evaders to play with:
                    List<TaggedEvader> chosenEvadersList = new List<TaggedEvader>(chosenEvaders.Count);
                    foreach (var e in chosenEvaders)
                        chosenEvadersList.Add(new TaggedEvader(e.e));
                    alg.handleNewEvaders(chosenEvadersList);

                    return new BuyEvadersRes() { algorithmBroken = false };
                }
                /// <summary> 
                /// Evaders don't lose their value when sold from algorithm to algorithm
                /// Evaders' worth may increase when they are used to construct a new algorithm. Upon construction, it spreads the repair budget on evaders' worth, so it remains with half budget
                /// Evaders' worth may also change (increase/decrease) when an algorithm is deconstructed - each evader then gets value according to how many data units it has, and its distance from a sink
                /// </summary>
                public double EvadersWorth { get; set; }

                /// <summary>
                /// the more critical and *successfull* an algorithm is, the more we want it to stay alive even if it loses some evaders.
                /// NOTE: we must separate repair budget and evaders worth, since otherwise an algorithm that loses evaders may keep the same
                /// amount of evaders but only have their worth reduced(or stay the same in total). 
                /// In other words, if we let the evaders keep their total worth, it has no actual motivaion to buy cheaper evaders.
                /// If the worth decreases, it skews the system (after an algorithm buys many evaders and remains with no budget, any other algorithm can buy ALL of them for much less - including the algorithm that just sold them!)
                /// </summary>
                public double RepairBudget { get; set; }

                /// <summary>
                /// used for sorting algorithms - only algorithms with more worth may buy evaders from algorithms with less worth
                /// (this way, if an algorithm is unable to repair itself, there's a higher chance it had less worth and the total damage to the strategy is lower)
                /// </summary>
                public double TotalAlgorithmWorth { get { return EvadersWorth + RepairBudget; } }

                /// <summary>
                /// this prevents unusefull algorithm to stay protected from the need to sell agents to other algorithms 
                /// TODO: perhaps should be derived from AddedWorthPerReward
                /// </summary>
                public double LostWorthPerRound { get; set; }

                /// <summary>
                /// this counters LostWorthPerRound, if the algorithm is useful 
                /// </summary>
                public double AddedWorthPerReward { get; set; }

                /// <summary>
                /// this prevents "worthy algorithms" from becomming TOO powerfull 
                /// TODO: not sure if this is needed. socialism almost never works anyway
                /// </summary>
                public double WorthCeiling { get; set; }

                // NOTE1: any change on worthPerEvader should also update 'EvadersWorth' . 
                // NOTE2: always check if worthPerEvader is null before using it
                // TODO: Consider writing a collection for 'worthPerEvader' that does this automatically
                public Dictionary<Evader, double> worthPerEvader { get; set; }

                /// <summary>
                /// sorts algorithms by descending total worth
                /// </summary>
                public int CompareTo(ManagedBasicAlgorithm other)
                {
                    return other.TotalAlgorithmWorth.CompareTo(this.TotalAlgorithmWorth);
                }

                public ManagedBasicAlgorithm()
                {
                    EvadersWorth = 0; // if an algorithm is created without CreateNew() call, then it's the initial alg which has no value
                    worthPerEvader = null; // only the method buy evaders should allocate worthPerEvader after IEvaderBasicAlgorithm is allocated
                }

            }

            /// <summary>
            /// generates a new algorithm of the same type, then sets budget parameters
            /// NOTE: should be called by WorldStateToActionEvaderManagerPolicy only
            /// </summary>
            public static ManagedBasicAlgorithm CreateNew(IEvaderBasicAlgorithm srcalg,
                                                   AForge.Genetic.IChromosome param,
                                                   double initialWorth,
                                                   double gainedWorthPerReward,
                                                   double lostWorthPerRound,
                                                   double worthCeiling,
                                                   NotifyBasicAlgorithmSuccess successNotifier)
            {
                ManagedBasicAlgorithm res = new ManagedBasicAlgorithm();
                res.alg = srcalg.CreateNew(param);

                res.RepairBudget = initialWorth;
                res.LostWorthPerRound = lostWorthPerRound;
                res.AddedWorthPerReward = gainedWorthPerReward;
                res.WorthCeiling = worthCeiling;
                res.EvadersWorth = 0;
                res.worthPerEvader = null;
                res.alg.setSuccessNotifier(successNotifier);
                return res;
            }

            private bool runsOutsideGA = false;
            public WorldStateToActionEvaderManagerPolicy()
            {
                WorldStateEvaderStrategyChromosome st = (WorldStateEvaderStrategyChromosome)bestChromosome;
                strategyTable = st;
                basicAlgNotifier = null;
                runsOutsideGA = true;
            }

            
            
            protected NotifyBasicAlgorithmSuccess basicAlgNotifier {get;set;}

           

            // TODO: (on lower priority) performance should be improved
            // (consider using Utils.getEvadersToRemove() )
            private HashSet<Evader> getEvadersToRemove(GameState s, HashSet<Point> O_d)
            {
                HashSet<Evader> res = new HashSet<Evader>();

                if (O_d.Count == 0)
                    return res;

                foreach (Evader e in EvolutionConstants.param.A_E)
                {
                    Tuple<DataUnit, Location, Location> evData;
                    if (!evaderTargetLocations.TryGetValue(e, out evData))
                        continue;
                    Location l = evData.Item2;
                    if (l.locationType == Location.Type.Node && O_d.Contains(l.nodeLocation))
                        res.Add(e);
                }
                return res;
            }
            
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
            override public Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep()
            {
                for(int i = 0; i < evadersCountPerAlgCode.Count; ++i)
                    evadersCountPerAlgCode[i] = 0;

                HashSet<Evader> evadersToRemove = getEvadersToRemove(s,O_d);

                if (evadersToRemove.Count > 0)
                {
                    LinkedList<Evader> evadersToRemoveFromO_d = new LinkedList<Evader>();
                    // tell each algorithm it just lost evaders
                    foreach (ManagedBasicAlgorithm st in allEvaders)
                    {
                        foreach (Evader e in evadersToRemove)
                            if (st.loseEvader(new TaggedEvader(e), 0)) // TODO: removing evaders one by one from all algorithms is no too efficient. consider managing a global dictionary
                                evadersToRemoveFromO_d.AddLast(e);
                        foreach (Evader e in evadersToRemoveFromO_d)
                            evadersToRemove.Remove(e);
                        evadersToRemoveFromO_d = new LinkedList<Evader>();
                    }

                    // we now let algorithms repair themselves, after evaders were lost
                    refreshAlgorithmEvaders(s, dataInSink, O_d,ps);
                } // if O_d.Count > 0

                updateAlgCodeCount();
                
                
                // from the second round and later, we are now ready to use the state-action table. First, check how many evaders are in each alg. type

                if (s.MostUpdatedEvadersLocationRound >= 0 && 
                    allEvaders.Count < EvolutionConstants.maxSimultenousActiveBasicAlgorithms)
                {
                    var newAlgs = strategyTable.getNewAlgorithms(currentWorldState, null);
                    List<ManagedBasicAlgorithm> managedNewAlgs = new List<ManagedBasicAlgorithm>();
                    foreach (var a in newAlgs)
                    {
                        ManagedBasicAlgorithm ma = new ManagedBasicAlgorithm();
                        ma.alg = a.alg;
                        ma.alg.setSuccessNotifier(() => { ma.RepairBudget = (ma.RepairBudget + ma.AddedWorthPerReward).LimitRange(0, ma.WorthCeiling); this.basicAlgNotifier(); });
                        allEvaders.Add(ma);
                    }
                    
                    refreshAlgorithmEvaders(s, dataInSink, O_d, ps);
                }

                foreach (LocalSink ls in localSinks)
                    ls.currentSendingAgent = null;

                Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
                foreach (ManagedBasicAlgorithm st in allEvaders)
                {
                    var algRes = st.alg.getNextStep(s,dataInSink,O_d,O_p, ps);
                    foreach (var r in algRes)
                        res.Add(r.Key,r.Value);

                    st.updateWorth();
                }


                if(ui.hasBoardGUI())
                {
                    List<string> lines  = new List<string>();

                    

                    var v = strategyTable.getValueMap(currentWorldState);
                    v.AddRange(currentWorldState.getValueMap().AddKeyPrefix("CurrentState:") );
                    
                    foreach(var rec in v)
                        lines.Add(rec.Key + "=" + rec.Value);
                    ui.addCurrentRoundLog(lines);
                }

                evaderTargetLocations = res;
                return res;
            }
            
            private void updateAlgCodeCount()
            {
                // TODO: nested for is not very efficient(though for now, if we have only 4 types, then maybe its not so bad). 
                // The alternatives are either adding type code variable to each alg(its quite dirty), or reflection
                foreach (ManagedBasicAlgorithm st in allEvaders)
                    for (int i = 0; i < EvolutionConstants.actionAlgorithmsByCode.Count; ++i)
                        if (EvolutionConstants.actionAlgorithmsByCode[i].GetType() == st.GetType())
                            evadersCountPerAlgCode[i] += st.worthPerEvader.Count;
            }
            
            /// <summary>
            /// repairs algorithms and removes broken algs
            /// </summary>
            private void refreshAlgorithmEvaders(
                GameState s,
                GoE.GameLogic.Utils.DataUnitVec dataInSink,
                HashSet<Point> O_d,
                PursuerStatistics ps)
            {
                allEvaders.Sort(); // sorts by descending (remaining) total worth 
                for (int i = 0; i < allEvaders.Count; ++i)
                {
                    // each alg may buy evaders from algorithms with less worth
                    BuyEvadersRes res =
                        allEvaders[i].buyEvaders(
                        new ListRangeEnumerable<ManagedBasicAlgorithm>(allEvaders, i+1, allEvaders.Count), s, dataInSink,O_d, ps);

                    
                    if (res.algorithmBroken)
                    {
                        allEvaders[i] = null;
                        // TODO: this doesn't necessarily keeps the array sorted by worth (evaders in deconstructed algorithms may still have some worth).
                        // for now it's probably not crucial since the algorithms with little worth rarely need any repairing anyway (I think)
                        if (res.replacementAlg != null)
                        {
                            allEvaders.AddRange(res.replacementAlg);
                        }
                    }
                }

                // clear the NULLs from array
                for (int i = 0; i < allEvaders.Count; ++i)
                    if (allEvaders[i] == null)
                    {
                        allEvaders[i] = allEvaders.Last();
                        allEvaders.RemoveAt(allEvaders.Count - 1);
                        --i;
                    }
            }

            /// <summary>
            /// algorithms that intend to create local sinks should add to this set, and remove when its no longer maintained.
            /// Evaders that intend to transmit into a local sink, should update that sink's 'currentSendingAgent', to avoid collisions
            /// </summary>
            public HashSet<LocalSink> localSinks { get; set; }

            /// <summary>
            /// tells which algorithm is currently managing which evader excluding idleEvaders (indices corespond EvolutionConstants.actionAlgorithmsByCode)
            /// </summary>
            public List<ManagedBasicAlgorithm> allEvaders { get; set; }

            /// <summary>
            /// tells how many evaders are managed by each type of algorithm excluding idleEvaders
            /// </summary>
            public List<int> evadersCountPerAlgCode { get; set; }

            /// <summary>
            /// used in getNextStep()
            /// </summary>
            private WorldStateEvaderStrategyChromosome strategyTable { get; set; }


            public WorldState currentWorldState {get; private set;}
            

            GameState s {get;set;}
            HashSet<Point> O_d, O_p;
            Algorithms.PursuerStatistics ps {get;set;}

            protected override List<ArgEntry> PolicyParamsInput
            {
                get
                {
                    return new List<ArgEntry>();
                }
            }

            struct AgentAngle
            {
                public AgentAngle(Point location, IAgent a)
                {
                    angle = Utils.getAngleOfGridPoint(location.subtruct(EvolutionConstants.targetPoint));
                    agent = a;
                }
                public IAgent agent;
                public float angle;
            }

            public int getRingIdx(Point p, int ringsPerArea)
            {
                return Math.Min(p.manDist(EvolutionConstants.targetPoint) / ringsPerArea, EvolutionConstants.areaStatesCount - 1);
            }
            
            
            public override void setGameState(int currentRound, IEnumerable<Point> currentO_d, HashSet<Point> currentO_p, GameState state)
            {
                O_d = new HashSet<Point>(currentO_d);
                O_p = currentO_p;
                ps.update(currentO_p);
                s = state;

               updateDataInSink(state);

               int rad = EvolutionConstants.radius;
                currentWorldState = new WorldState(EvolutionConstants.areaStatesCount);
                int ringsPerArea = (int)Math.Ceiling(((float)rad+1) / EvolutionConstants.areaStatesCount);

                List<int> dataUnitsInArea = 
                    AlgorithmUtils.getRepeatingValueList(0, EvolutionConstants.areaStatesCount);

                List<List<Point>> evadersPerArea =
                    AlgorithmUtils.getRepeatingValueList<List<Point>>(EvolutionConstants.areaStatesCount);

                List<List<Point>> pursuersPerArea = 
                    AlgorithmUtils.getRepeatingValueList<List<Point>>(EvolutionConstants.areaStatesCount);

                // TODO: instead of just using latest observation, find a way to incorporate location estimations
                foreach (Point pLoc in currentO_p)
                    pursuersPerArea[getRingIdx(pLoc,ringsPerArea)].Add(pLoc);

                var mem = state.M[state.MostUpdatedEvadersMemoryRound];
                foreach (Evader e in EvolutionConstants.param.A_E)
                {
                    if (state.L[state.MostUpdatedEvadersLocationRound][e].locationType != Location.Type.Node)
                        continue;
                    Point eLoc = state.L[state.MostUpdatedEvadersLocationRound][e].nodeLocation;

                    int ring = getRingIdx(eLoc, ringsPerArea);
                    evadersPerArea[ring].Add(eLoc);

                    if (mem.ContainsKey(e))
                    {
                        dataUnitsInArea[ring] = mem[e].Count - mem[e].getIntersectionSize(dataInSink);
                        //dataUnitsInArea[ring] += Utils.getUntransmittedData(dataInSink, state, e).Count;
                    }
                }

                for(int i = 0; i < EvolutionConstants.areaStatesCount; ++i)
                {
                    AreaState areaS = new AreaState();

                    // TODO: implement evaluation of all parameters below:
                    areaS[AreaState.Shorts, (int)AreaState.ShortsIndex.estimatedPursuersCount] =
                        (ushort)pursuersPerArea[i].Count;
                    
                    areaS[AreaState.Shorts, (int)AreaState.ShortsIndex.evaderCount] = 
                        (ushort)evadersPerArea[i].Count;

                    areaS[AreaState.Shorts, (int)AreaState.ShortsIndex.uniqueDataUnits] =
                        (ushort)dataUnitsInArea[i];
                    
                    areaS[AreaState.Doubles, (int)AreaState.DoublesIndex.dirtySetPointsRatio] = 0;

                    if (pursuersPerArea[i].Count > 1)
                    {
                        areaS[AreaState.Doubles, (int)AreaState.DoublesIndex.estimatedPursuersAvgDistance] =
                            Utils.getAverageDistanceVariance(Utils.sortPointsByAngles(pursuersPerArea[i], EvolutionConstants.targetPoint), EvolutionConstants.targetPoint);
                    }
                    else
                        areaS[AreaState.Doubles, (int)AreaState.DoublesIndex.estimatedPursuersAvgDistance] = 0;

                    areaS[AreaState.Doubles, (int)AreaState.DoublesIndex.evaderCountInDirtySetPercent] = 0;

                    if (evadersPerArea[i].Count > 1)
                    {
                        areaS[AreaState.Doubles, (int)AreaState.DoublesIndex.evadersAvgDistanceVariance] =
                            Utils.getAverageDistanceVariance(Utils.sortPointsByAngles(evadersPerArea[i], EvolutionConstants.targetPoint), EvolutionConstants.targetPoint);
                    }
                    else
                        areaS[AreaState.Doubles, (int)AreaState.DoublesIndex.evadersAvgDistanceVariance] = 0;

                    currentWorldState[i] = areaS;
                    
                }

            }


            UI.IPolicyGUIInputProvider ui;
            public override bool init(AGameGraph G, GoEGameParams prm, AGoEPursuersPolicy p, UI.IPolicyGUIInputProvider pgui,
                Dictionary<string, string> policyInput)
            {
                if(strategyTable == null)
                {
                    MessageBox.Show("bestChromosome must be set before using this policy!");
                    throw new Exception("bestChromosome must be set before using this policy!");   
                }
                if (runsOutsideGA)
                {
                    // now we actually need to make sure the global evolution constants corespond the current params
                    // TODO: super dirty
                    EvolutionConstants.param = prm;
                    EvolutionConstants.graph = (GridGameGraph)G;
                }

                ui = pgui;
                
                evadersCountPerAlgCode = new List<int>();

                for (int i = 0; i < EvolutionConstants.actionAlgorithmsByCode.Count; ++i)
                    evadersCountPerAlgCode.Add(0);

                allEvaders = new List<ManagedBasicAlgorithm>();
                ManagedBasicAlgorithm ma = new ManagedBasicAlgorithm();
                List<TaggedEvader> taggedEvaders = new List<TaggedEvader>();
                foreach (Evader e in EvolutionConstants.param.A_E)
                    taggedEvaders.Add(new TaggedEvader(e));
                ma.alg = new GoToSinkAlg(taggedEvaders, strategyTable.initialPositionClusterSize,0,0);
                ma.worthPerEvader = new Dictionary<Evader, double>();
                foreach (Evader e in EvolutionConstants.param.A_E)
                    ma.worthPerEvader.Add(e, 0);

                allEvaders.Add(ma);
                    
                updateAlgCodeCount();

                dataInSink = new GoE.GameLogic.Utils.DataUnitVec();
                dataInSink.Add(DataUnit.NIL);
                dataInSink.Add(DataUnit.NOISE);
                localSinks = new HashSet<LocalSink>();
                ps = new PursuerStatistics(EvolutionConstants.graph, 10); // TODO: should we make this part of the chromosome?
                return true;
            }




            /// <summary>
            /// in this method, we give a rough evaluation of how much an evader with no special managing algorithm(GoToSink) does worth
            /// TODO: the current heurisitc may be improved significantly. The probability of the evader reaching a sink can be predicted
            /// with something like the EvaderCrawl algorithm, which finds the best path. Additionally, we shouldn't give evaders utility according to 
            /// </summary>
            /// <param name="e"></param>
            /// <returns></returns>
            private static double evaluateIdleEvader(Evader e, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink)
            {
                int targetToSinkDist = EvolutionConstants.rightmostSinkPoint.manDist(EvolutionConstants.targetPoint);

                // TODO: we assume here that the target is surrounded by sinks, so we can easily compute distance to nearest sink. Fix this so we won't need the assumption
                int evaderToSinkDist = targetToSinkDist - s.L[s.MostUpdatedEvadersLocationRound][e].nodeLocation.manDist(EvolutionConstants.targetPoint);

                var EvaderMem = s.M[s.MostUpdatedEvadersMemoryRound][e];
                int transmittedData = unitsInSink.getIntersectionSize(EvaderMem);
                // TODO: 1) the intersection is very slow! consider finishing and using class OneTargetCompactMemory.
                // TODO: 2) instead of just checking how many untransmitted data units this evaders has, we should also check HOW COMMON each untransmitted data unit is (more common -> less important)

                return ((float)(EvaderMem.Count - transmittedData)) / evaderToSinkDist;

            }


           
            /// <param name="initialPositionClusterSize">
            /// the initial position of evaders is clusters of evaders (each clustered group have the same location, on a random sink)
            /// 1 means one cluster for all evaders, 0.5 means two evenly sized clusters etc.
            /// </param>
            /// <param name="ActionAlgorithmsByCode"></param>
            /// <param name="Evaders"></param>
            public override void initializeChromosome(IChromosome i, NotifyBasicAlgorithmSuccess notifier)
            {
                runsOutsideGA = false;
                WorldStateEvaderStrategyChromosome st = (WorldStateEvaderStrategyChromosome)i;
                strategyTable = st;
                basicAlgNotifier = notifier;
            }
           
            
        }  
    }
}