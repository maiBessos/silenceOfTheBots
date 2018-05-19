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
    public abstract class AIntrusionEvadersPolicy : GoE.Utils.ReflectionUtils.DerivedTypesProvider<AIntrusionEvadersPolicy>, IEvadersPolicy
    {
        /// <summary>
        /// it turned 'true' by the policy, 
        /// some performance estimators will stop (useful when we want to calculate leaked data for only the captured evaders)
        /// </summary>
        public bool GaveUp { get { return _gaveUp; } protected set { _gaveUp = value; } }

        public virtual List<ArgEntry> policyInputKeys
        {
            get
            {
                return new List<ArgEntry>();
            }
        }

        private bool _gaveUp = false;
        protected void GiveUp() { _gaveUp = true; }
        /// <summary>
        /// first method called at the begining of the round
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="O_d">
        /// all dead evaders communicate to other evaders that they just got captured + all of their held
        /// observations until that point
        /// </param>
        /// <param name="O_p">
        /// new data observed by evaders (local observations)
        /// </param>
        /// <param name="s"></param>
        public abstract void setGameState(int currentRound, 
            IEnumerable<GoE.GameLogic.Utils.CapturedObservation> O_d,
            List<GameLogic.Utils.PursuerPathObservation> O_p, 
            IntrusionGameState s);

        /// <summary>
        /// internally, the policy may move from agent to agent data in the form of List<GameLogic.Utils.Observation>
        /// NOTE1: Each evader that transmits data must be added to the List<Evader> in the return value
        /// NOTE2: dead evaders (that were in O_d in the latest setGameState() call, and before this communicate() call) communicate freely
        /// </summary>
        /// <param name="communicatedObservations"></param>
        public abstract List<Evader> communicate();

        /// <summary>
        /// invoked after setGameState(), and tells where each evader is destined.
        /// Locations that are on the intrusion area (circumference) means that the evader is 
        /// starting/continuing the intrusion
        /// </summary>
        /// <returns>
        /// tells where each evader goes to, next round 
        /// NOTE1: evaders that began intruding must stay in the same point until captured or until game ends
        /// NOTE2: captured evaders should be excluded
        /// </returns>
        public abstract Dictionary<Evader, Point> getNextStep();

        public bool init(AGameGraph G, IGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            return init(G, (IntrusionGameParams)prm, initializedPursuers, pgui, policyParams);
        }
        public abstract bool init(AGameGraph G, IntrusionGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null);
        


        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        public virtual void gameFinished() { }

        /// <summary>
        /// may be called after init()
        /// </summary>
        /// <returns></returns>
        public virtual GameResult getMinLeakedDataTheoreticalBound() { return null; }




        public void addGUIMarks(List<GameLogic.Utils.PursuerPathObservation> O_p, 
                         IPolicyGUIInputProvider ui, 
                         IntrusionGameState s, 
                         GridGameGraph gr,
                         IntrusionGameParams prm,
                        Dictionary<Evader, Point> currentEvaderLocations)
        {
            if (ui.hasBoardGUI())
            {
                Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();

                List<Point> tmpPointList = new List<Point>();
                foreach (var p in O_p)
                    tmpPointList.AddRange(p.observedPursuerPath);
                markedLocations.Add("Pursuer trail(O_p)", tmpPointList);

                tmpPointList = new List<Point>();
                foreach (var p in O_p)
                    tmpPointList.Add(p.observedPursuerPath.Last());
                markedLocations.Add("Pursuer last pos", tmpPointList);

                List<Point> detectionArea = new List<Point>();
                foreach (Evader e in s.ActiveEvaders)
                {
                    if (currentEvaderLocations.ContainsKey(e))
                        detectionArea.AddRange(
                            gr.getNodesWithinDistance(currentEvaderLocations[e], prm.r_es));
                }
                markedLocations.Add("Area within r_s(evaders detecting pursuers)", detectionArea);

                ui.markLocations(markedLocations.toPointFMarkings());
            }
        }
    }
    class IntrusionEvadersPolicyEvadersPolicyUI : AIntrusionEvadersPolicy
    {
        private GridGameGraph g;
        private IntrusionGameParams gm;
        private IPolicyGUIInputProvider pgui;

        private IntrusionGameState prevS;
        private IEnumerable<GoE.GameLogic.Utils.CapturedObservation> prevO_d;
        private List<GameLogic.Utils.PursuerPathObservation> prevO_p;
        private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();
        private Point intrusionAreaCenter;

        private Dictionary<Evader, List<GameLogic.Utils.PursuerPathObservation>> accumObservationsPerEve;
        private List<Evader> nextEvadersToCommunicate;


        public override bool init(AGameGraph G, IntrusionGameParams prmi, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pGui, Dictionary<string, string> policyParams = null)
        {
            IntrusionGameParams prm = prmi;

            this.accumObservationsPerEve = new Dictionary<Evader, List<GameLogic.Utils.PursuerPathObservation>>();
            foreach (var e in prm.A_E)
                this.accumObservationsPerEve[e] = new List<GameLogic.Utils.PursuerPathObservation>();

            this.nextEvadersToCommunicate = new List<Evader>();
            this.g = (GridGameGraph)G;
            this.gm = prm;
            this.pgui = pGui;
            foreach (Evader e in gm.A_E)
                currentEvadersLocations[e] = new Location(Location.Type.Unset);

            intrusionAreaCenter = g.getNodesByType(NodeType.Target).First();
            return true;
        }
        public IntrusionEvadersPolicyEvadersPolicyUI() { }


        public override void setGameState(int currentRound, 
            IEnumerable<GoE.GameLogic.Utils.CapturedObservation> O_d,
            List<GameLogic.Utils.PursuerPathObservation> O_p, 
            IntrusionGameState s)
        {
            prevS = s;
            prevO_p = O_p;
            prevO_d = O_d;

            foreach (var o in O_p)
                accumObservationsPerEve[o.observer].Add(o);

            foreach(Evader e in s.ActiveEvaders)
                foreach(var obs in O_d)
                    if(obs.who == e)
                        currentEvadersLocations[e] = new Location(Location.Type.Captured);

            Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();

            List<Point> tmpPointList = new List<Point>();
            foreach (var p in O_p)
                tmpPointList.AddRange(p.observedPursuerPath);
            markedLocations.Add("Pursuer trail(O_p)", tmpPointList);

            tmpPointList = new List<Point>();
            foreach (var p in O_p)
                tmpPointList.Add(p.observedPursuerPath.Last());
            markedLocations.Add("Pursuer last pos", tmpPointList);

            tmpPointList = new List<Point>();
            foreach (var p in O_d)
                tmpPointList.Add(p.where);
            markedLocations.Add("Dead Evaders(O_d)", tmpPointList);

            
            List<Point> detectionArea = new List<Point>();
            foreach (Evader e in s.ActiveEvaders)
            {
                detectionArea.AddRange(
                    g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_es));
            }
            markedLocations.Add("Area within r_s(evaders detecting pursuers)", detectionArea);

            
            pgui.markLocations(markedLocations.toPointFMarkings());

            InputRequest req = new InputRequest();
            IEnumerable<Evader> relevantEvaders;
            if (currentRound == 0)
                relevantEvaders = gm.A_E;
            else
                relevantEvaders = s.ActiveEvaders;

            foreach (Evader e in relevantEvaders)
            {
                if (currentEvadersLocations[e].locationType != Location.Type.Node ||
                    (currentEvadersLocations[e].nodeLocation.manDist(intrusionAreaCenter) > gm.r_e))
                    req.addMovementOption(e, currentEvadersLocations[e], getPossibleNextLocations(currentEvadersLocations[e]));

                prevChoiceKey = req.addComboChoice(e, "communicate", new string[] { "yes", "no" }, "no");
                //req.addComboChoice(e, "Noise/Data unit to transmit",
                //    s.M[s.MostUpdatedEvadersMemoryRound][e].ToList().Except(dataUnitsInSink).Union(new DataUnit[]{DataUnit.NIL, DataUnit.Flush}), 
                //    DataUnit.NIL);

                //List<Point> relevantTargets = new List<Point>();
                //var relevantTargetsDummy = targets.Intersect(g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_e));
                //foreach (Point p in relevantTargetsDummy)
                //    relevantTargets.Add(p);

                //req.addComboChoice(e, "Target to eavesdrop (" + relevantTargets.Count.ToString() + " available)",
                //    GameLogic.Utils.pointsToLocations(relevantTargets),
                //    new Location(Location.Type.Undefined));
            }

            pgui.setInputRequest(req);
            nextEvadersToCommunicate.Clear();
        }
        int prevChoiceKey;
        public override List<Evader> communicate()
        {
            List<Evader> res = new List<Evader>();
            foreach (Evader e in prevS.ActiveEvaders)
                if (((string)pgui.getChoice(e, prevChoiceKey)) != "no")
                    res.Add(e);
            return res;
        }
        public override Dictionary<Evader, Point> getNextStep()
        {
            Dictionary<Evader, Point> res = new Dictionary<Evader, Point>();
            IEnumerable<Evader> relevantEvaders;

            if (prevS.ActiveEvaders.Count == 0)
                relevantEvaders = gm.A_E; //first round
            else
                relevantEvaders = prevS.ActiveEvaders;

            foreach (Evader e in relevantEvaders)
            {
                if (currentEvadersLocations[e].nodeLocation.manDist(intrusionAreaCenter) > gm.r_e)
                {
                    res[e] = pgui.getMovement(e).First().nodeLocation;
                    currentEvadersLocations[e] = pgui.getMovement(e).First();
                }
                else
                {
                    // evader is on intrusion circumference - it is now stuck!
                    res[e] = currentEvadersLocations[e].nodeLocation;
                }
            }
            return res;
        }

        private List<Location> getPossibleNextLocations(Location currentAgentLocation)
        {
            List<Location> res = new List<Location>();
            res.Add(currentAgentLocation);

            if (currentAgentLocation.locationType == Location.Type.Unset)
                res.AddRange(GameLogic.Utils.pointsToLocations(g.getNodesWithinDistance(intrusionAreaCenter, gm.r_e+5)));
            else
                foreach (var n in g.getNodesWithinDistance(currentAgentLocation.nodeLocation, 1))
                    res.Add(new Location(n));

            return res;
        }

    }
}
