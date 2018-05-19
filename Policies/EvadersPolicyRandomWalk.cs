//using GoE.GameLogic;
//using GoE.GameLogic.EvolutionaryStrategy;
//using GoE.UI;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GoE.Policies
//{
//    class EvadersPolicyRandomWalk : AEvadersPolicy
//    {
//        private GridGameGraph g;
//        private GoEGameParams gm;
//        private IPolicyGUIInputProvider pgui;

//        private GameState prevS;
//        private IEnumerable<Point> prevO_d;
//        private HashSet<Point> prevO_p;
//        private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();

//        /// <summary>
//        /// tells which units have reached the sink successfully
//        /// </summary>
//        private List<DataUnit> dataUnitsInSink = new List<DataUnit>();

//        public override bool init(GridGameGraph G, GoEGameParams prm, AGoEPursuersPolicy p, IPolicyGUIInputProvider gui)
//        {
//            this.g = G;
//            this.gm = prm;
//            this.pgui = gui;
//            foreach (Evader e in gm.A_E)
//                currentEvadersLocations[e] = new Location(Location.Type.Unset);
//            return true;
//        }
//        public EvadersPolicyRandomWalk() { }

//        public override void setGameState(int currentRound, IEnumerable<Point> O_d, HashSet<Point> O_p, GameState s)
//        {
//            prevS = s;
//            prevO_p = O_p;
//            prevO_d = O_d;

//            foreach (Evader e in gm.A_E)
//                if (currentEvadersLocations[e].locationType == Location.Type.Node &&
//                    O_d.Contains(currentEvadersLocations[e].nodeLocation))
//                {
//                    currentEvadersLocations[e] = new Location(Location.Type.Captured);
//                }

//            updateSink(currentRound);

//            Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();
//            markedLocations.Add("Detected Pursuers(O_p)", O_p.ToList());
//            markedLocations.Add("Destroyed Evaders(O_d)", O_d.ToList());

//            List<Point> receptionArea = new List<Point>();
//            List<Point> detectionArea = new List<Point>();
//            foreach (Evader e in gm.A_E)
//            {
//                if (currentEvadersLocations[e].locationType == Location.Type.Node)
//                {
//                    receptionArea.AddRange(
//                        g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_e));
//                    detectionArea.AddRange(
//                        g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_s));
//                }
//            }
//            markedLocations.Add("Area within r_e(reception)", receptionArea);
//            markedLocations.Add("Area within r_s(pursuer detection)", detectionArea);


//            pgui.markLocations(markedLocations);

//            InputRequest req = new InputRequest();
//            foreach (Evader e in gm.A_E)
//                if (currentEvadersLocations[e].locationType == Location.Type.Unset)
//                {
//                    List<DataUnit> dataNotInSink =  s.M[currentRound][e].ToList();

//                    req.addMovementOption(e, currentEvadersLocations[e], getPossibleNextLocations(currentEvadersLocations[e]));
//                    req.addComboChoice(e, "Noise/Data unit to transmit",
//                        dataNotInSink.Except(dataUnitsInSink).Union(new DataUnit[] { DataUnit.NIL }),
//                        DataUnit.NIL);

//                    List<Point> relevantTargets = new List<Point>();
//                    var relevantTargetsDummy = g.getNodesByType(NodeType.Target).Intersect(g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_e));
//                    foreach (Point p in relevantTargetsDummy)
//                        relevantTargets.Add(p);

//                    req.addComboChoice(e, "Target to eavesdrop (" + relevantTargets.Count.ToString() + " available)",
//                        GameLogic.Utils.pointsToLocations(relevantTargets),
//                        new Location(Location.Type.Undefined));
//                }

//            pgui.setInputRequest(req);
//        }

//        public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep()
//        {
//            Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
//            Random rand = new ThreadSafeRandom().rand;
//            foreach (Evader e in gm.A_E)
//            {
//                if (currentEvadersLocations[e].locationType == Location.Type.Node)
//                {
//                    List<Point> possPos = g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, 1).Except(prevO_p).ToList();
//                    possPos.Remove(currentEvadersLocations[e].nodeLocation);
//                    if (possPos.Count > 0)
//                    {
//                        int r = rand.Next(0, possPos.Count);
//                        res[e] = Tuple.Create(DataUnit.NIL, new Location(possPos[r]), new Location(Location.Type.Undefined));
//                        currentEvadersLocations[e] = new Location(possPos[r]);
//                    }
//                }
//                else if (currentEvadersLocations[e].locationType != Location.Type.Captured)
//                {
//                    res[e] = Tuple.Create((DataUnit)pgui.getChoice(e, 0), pgui.getMovement(e).First(), (Location)pgui.getChoice(e, 1));
//                    currentEvadersLocations[e] = pgui.getMovement(e).First();
//                }
//            }
//            return res;
//        }
//        private void updateSink(int currentRound)
//        {
//            foreach (Evader e in gm.A_E)
//                if (currentEvadersLocations[e].locationType == Location.Type.Node &&
//                    prevS.B_O[currentRound - 1][e] != DataUnit.NIL &&
//                    prevS.B_O[currentRound - 1][e] != DataUnit.NOISE)
//                {
//                    foreach (var s in g.getNodesByType(NodeType.Sink))
//                        if (g.getMinDistance(s, currentEvadersLocations[e].nodeLocation) <= gm.r_e)
//                            dataUnitsInSink.Add(prevS.B_O[currentRound - 1][e]);
//                }

//        }
//        private List<Location> getPossibleNextLocations(Location currentAgentLocation)
//        {
//            List<Location> res = new List<Location>();
//            res.Add(currentAgentLocation);

//            if (currentAgentLocation.locationType == Location.Type.Unset)
//                foreach (var n in g.getNodesByType(NodeType.Sink))
//                    res.AddRange(GameLogic.Utils.pointsToLocations(g.getNodesWithinDistance(n, gm.r_e)));
//            else
//                foreach (var n in g.getNodesWithinDistance(currentAgentLocation.nodeLocation, 1))
//                    res.Add(new Location(n));

//            return res;
//        }

//    }
//}
