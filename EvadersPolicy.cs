using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using GoE.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Policies
{
    public interface IEvadersPolicy
    {
        bool init(AGameGraph G, 
                  IGameParams prm, 
                  APursuersPolicy initializedPursuers, 
                  IPolicyGUIInputProvider pgui, 
                  Dictionary<string, string> policyParams);

        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        void gameFinished();

        bool GaveUp { get; }

        /// <summary>
        /// tells which keys are needed for 'policyParams' in init()
        /// </summary>
        List<ArgEntry> policyInputKeys { get; }
    }
    public abstract class AGoEEvadersPolicy : GoE.Utils.ReflectionUtils.DerivedTypesProvider<AGoEEvadersPolicy>, IEvadersPolicy
    {
        /// <summary>
        /// it turned 'true' by the policy, 
        /// some performance estimators will stop (useful when we want to calculate leaked data for only the captured evaders)
        /// </summary>
        public bool GaveUp { get { return _gaveUp; } protected set { _gaveUp = value; } }


        /// <summary>
        /// // since we can't implement abstract method to satisfy the interface, we concatenate the call
        /// </summary>
        public List<ArgEntry> policyInputKeys
        {
            get
            {
                return PolicyParamsInput;
            }
        }

        /// <summary>
        /// called by the public policyParamsInput getter
        /// </summary>
        protected abstract List<ArgEntry> PolicyParamsInput { get; }

        private bool _gaveUp = false;
        
        /// invoked before each getNextStep() calls
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="O_p">
        /// Locations of pursuers in the current round, as detected by all evaders/sinks
        /// </param>
        /// <param name="O_d">
        /// Locations of evaders captured since the previous setGameState() call 
        /// (due to the previous movement of the pursuers, and due to evaders "suiciding" and going 
        /// into where a pursuer was)
        /// </param>
        public abstract void setGameState(int currentRound, IEnumerable<Point> O_d, HashSet<Point> O_p, GameState s);

        /// <summary>
        /// invoked after setGameState()
        /// </summary>
        /// <returns>
        /// for each evader:
        /// item1->data to transmit
        /// item2->next location of the evader
        /// item3->target to listen to
        /// </returns>
        public abstract Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="G"></param>
        /// <param name="prm"></param>
        /// <param name="pgui"></param>
        /// <param name="initializedPursuers">
        /// this allows evaders to "cheat" and quickly learn the pursuers' policy
        /// TODO: consider adding a coresponding param to Pursuers's policy init(). Right now we have nothing that uses it
        /// </param>
        /// <returns>
        /// false if policy can't be used with given parameters
        /// </returns>
        public bool init(AGameGraph G, 
                        IGameParams prm, 
                        APursuersPolicy initializedPursuers, 
                        IPolicyGUIInputProvider pgui, 
                        Dictionary<string, string> policyParams)
        {
            return init(G, (GoEGameParams)prm, (AGoEPursuersPolicy)initializedPursuers, pgui, policyParams);
        }

        public abstract bool init(AGameGraph G, GoEGameParams prm, AGoEPursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams);
        

        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        public virtual void gameFinished() { }

        /// <summary>
        /// may be called after init()
        /// </summary>
        /// <returns></returns>
        public virtual GameResult getMinLeakedDataTheoreticalBound() { return null; }
    }
    class EvadersPolicyUI : AGoEEvadersPolicy
    {
        private GridGameGraph g;
        private GoEGameParams gm;
        private IPolicyGUIInputProvider pgui;
        private GameState prevS;
        private IEnumerable<Point> prevO_d;
        private HashSet<Point> prevO_p;
        private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();
        
        /// <summary>
        /// tells which units have reached the sink successfully
        /// </summary>
        private List<DataUnit> dataUnitsInSink = new List<DataUnit>();

        public override bool init(AGameGraph G, 
                                  GoEGameParams prm, 
                                  AGoEPursuersPolicy p, 
                                  IPolicyGUIInputProvider gui,
                                  Dictionary<string, string> PolicyParams = null)
        {
            this.g = (GridGameGraph)G;
            this.gm = prm;
            this.pgui = gui;
            
            foreach (Evader e in gm.A_E)
                currentEvadersLocations[e] = new Location(Location.Type.Unset);

            targets = g.getNodesByType(NodeType.Target);
            return true;
        }
        public EvadersPolicyUI() { }

        private List<Point> targets;

        protected override List<ArgEntry> PolicyParamsInput
        {
            get
            {
                return new List<ArgEntry>();
            }
        }

        public override void setGameState(int currentRound, IEnumerable<Point> O_d, HashSet<Point> O_p, GameState s)
        {
            prevS = s;
            prevO_p = O_p;
            prevO_d = O_d;
            
            foreach(Evader e in gm.A_E)
                if(currentEvadersLocations[e].locationType == Location.Type.Node &&
                   O_d.Contains(currentEvadersLocations[e].nodeLocation))
                {
                    currentEvadersLocations[e] = new Location(Location.Type.Captured);
                }

            updateSink(currentRound);

            Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();
            markedLocations.Add("Detected Pursuers(O_p)", O_p.ToList());
            markedLocations.Add("Destroyed Evaders(O_d)", O_d.ToList());

            List<Point> receptionArea = new List<Point>();
            List<Point> detectionArea = new List<Point>();
            foreach (Evader e in gm.A_E)
            {
                if (currentEvadersLocations[e].locationType == Location.Type.Node)
                {
                    receptionArea.AddRange(
                        g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_e));
                    detectionArea.AddRange(
                        g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_s));
                }
            }
            markedLocations.Add("Area within r_e(reception)", receptionArea);
            markedLocations.Add("Area within r_s(pursuer detection)", detectionArea);

            
            pgui.markLocations(markedLocations.toPointFMarkings());
             
            InputRequest req = new InputRequest();
            foreach (Evader e in gm.A_E)
                if (currentEvadersLocations[e].locationType != Location.Type.Captured)
                {
                    req.addMovementOption(e, currentEvadersLocations[e], getPossibleNextLocations(currentEvadersLocations[e]));
                    req.addComboChoice(e, "Noise/Data unit to transmit",
                        s.M[s.MostUpdatedEvadersMemoryRound][e].ToList().Except(dataUnitsInSink).Union(new DataUnit[]{DataUnit.NIL, DataUnit.Flush}), 
                        DataUnit.NIL);

                    List<Point> relevantTargets = new List<Point>();
                    var relevantTargetsDummy = targets.Intersect(g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_e));
                    foreach (Point p in relevantTargetsDummy)
                        relevantTargets.Add(p);

                    req.addComboChoice(e, "Target to eavesdrop (" + relevantTargets.Count.ToString() + " available)",
                        GameLogic.Utils.pointsToLocations(relevantTargets),
                        new Location(Location.Type.Undefined));
                }

            pgui.setInputRequest(req);
        }

        public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep()
        {
            Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader,Tuple<DataUnit,Location,Location>>();
             foreach (Evader e in gm.A_E)
                 if (currentEvadersLocations[e].locationType != Location.Type.Captured)
                 {
                     res[e] = Tuple.Create((DataUnit)pgui.getChoice(e, 0), pgui.getMovement(e).First(), (Location)pgui.getChoice(e, 1));
                     currentEvadersLocations[e] = pgui.getMovement(e).First();
                 }
             return res;
        }
        private void updateSink(int currentRound)
        {
            foreach (Evader e in gm.A_E)
                if (currentEvadersLocations[e].locationType == Location.Type.Node &&
                    prevS.B_O[currentRound-1][e] != DataUnit.NIL &&
                    prevS.B_O[currentRound-1][e] != DataUnit.NOISE)
                {
                    foreach (var s in g.getNodesByType(NodeType.Sink))
                        if (g.getMinDistance(s, currentEvadersLocations[e].nodeLocation) <= gm.r_e)
                            dataUnitsInSink.Add(prevS.B_O[currentRound-1][e]);
                }

        }
        private List<Location> getPossibleNextLocations(Location currentAgentLocation)
        {
            List<Location> res = new List<Location>();
            res.Add(currentAgentLocation);

            if (currentAgentLocation.locationType == Location.Type.Unset)
                foreach (var n in g.getNodesByType(NodeType.Sink))
                    res.AddRange(GameLogic.Utils.pointsToLocations(g.getNodesWithinDistance(n, gm.r_e)));
            else
                foreach (var n in g.getNodesWithinDistance(currentAgentLocation.nodeLocation, 1))
                    res.Add(new Location(n));

            return res;
        }

    }
}
