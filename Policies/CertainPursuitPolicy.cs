//using GoE.GameLogic;
//using GoE.GameLogic.Algorithms;
//using GoE.UI;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//namespace GoE.Policies
//{
//	public class CertainPursuitPolicy : IPursuersPolicy
//	{
//		private GridGameGraph g;
//		private GameParams gm;
//		//private IPolicyGUI pgui;
//		private Dictionary<Pursuer, Location> prevLocation = new
//			Dictionary<Pursuer, Location>();
//		private Dictionary<Pursuer, Location> initLocation = new
//			Dictionary<Pursuer, Location>();
//		Pursuer firstPursuer;
//		private Point pursuitTarget;
//		private int currentRound;
//		private int pursuitRound=-1;
//		static private int k=0;
//		private int maxRound=Math.Max(2*k,1);
//		private Dictionary<Pursuer, Location> pursuitLocations = new
//			Dictionary<Pursuer, Location>();
//		public void init(GridGameGraph G, GameParams prm, IPolicyInputProvider gui)
//		{
//			this.g = G;
//			this.gm = prm;
//			//this.pgui = gui;
//		}

//		public CertainPursuitPolicy()
//		{
//		}

//		public void setGameState(int CurrentRound, List<Point> O_c,
//			IEnumerable<Point> O_d)
//		{
//			this.currentRound = CurrentRound;
//			if (O_d.Contains (pursuitTarget)||pursuitRound>=maxRound) {
//				//TODO Pursuit for every target
//				pursuitRound = -1;
//				pursuitLocations = new Dictionary<Pursuer, Location> ();
//			} else if (pursuitRound == -1 && O_c.Count > 0) {
//				Random rand = new Random ((int)DateTime.UtcNow.Ticks);
//				int r = rand.Next (0, O_c.Count);
//				pursuitTarget = O_c [r];
//				//List<Point> targets = g.getNodesByType(NodeType.Target);
//				pursuitRound = 1;
//			} else if (O_c.Contains (pursuitTarget)) { //TODO recalculate maxround
//				pursuitRound = 1;
//			} else if (pursuitRound > 0) {
//				pursuitRound++;
//			}
//			Dictionary<string, List<Point>> markedLocations = new
//				Dictionary<string, List<Point>>();
//			//List<Location> pa = Algorithms.getPursuitArea (g, new	Location (pursuitTarget), pursuitRound);
//			if (pursuitRound > 0) {
//				List<Point> spread = g.getNodesWithinDistance
//					(pursuitTarget, pursuitRound).ToList ();
//				markedLocations.Add ("Evader spread", spread);
//				/*List<Point> reach = new List<Point> ();
//				foreach (Pursuer pu in gm.A_P) { // FIXME More pursuers result in crash
//					//foreach (Location l in Algorithms.getReachableLocations (pa, prevLocation [p], gm.r_p)) {
//						foreach(Point po in g.getNodesWithinDistance(prevLocation[pu].nodeLocation,
//								k*gm.r_p).Intersect(spread).ToList()) {
//							reach.Add (po);
//						}
//					}
//					markedLocations.Add ("Reachable in pursuit area", reach);
//					pgui.markLocations (markedLocations);*/
//			}

//		}

//		public Dictionary<Pursuer, Location> getNextStep()
//		{
//			if(currentRound == 0)
//			{
//				List<Point> targets = g.getNodesByType(NodeType.Target);
//				int pursuerRange = 0;
//				int pursuersPerTarget = gm.A_P.Count / targets.Count;
//				foreach(Point t in targets)
//				{
//					firstPursuer =
//						Pursuit.InitCertainPursuit(g, gm.r_p, k,
//							new
//							Utils.ListRangeEnumerable<Pursuer>(gm.A_P, pursuerRange, pursuerRange +
//								pursuersPerTarget),
//							prevLocation,
//							new Location(t), gm.r_e);
//					pursuerRange += pursuersPerTarget;
//				}
//				initLocation = new Dictionary<Pursuer,
//				Location>(prevLocation);
//			}
//			else
//			{	
//				if (pursuitRound > 0 && k == 0) { // TODO Split pursuit by targets
//					List<Point> targets = g.getNodesByType
//						(NodeType.Target);
//					int pursuerRange = 0;
//					int pursuersPerTarget = gm.A_P.Count / targets.Count;

//					foreach (Point t in targets) {
//						Pursuit.SpecialCertainPursuitPursuers (g, gm.r_p, k,
//							new Utils.ListRangeEnumerable<Pursuer> (gm.A_P, pursuerRange, pursuerRange + pursuersPerTarget),
//							prevLocation,
//							new Location (pursuitTarget),
//							gm.r_e,
//							pursuitRound,
//							firstPursuer);

//						pursuerRange += pursuersPerTarget;
//					}

//				} else if (pursuitRound > 0 && pursuitRound <= k) { // TODO Split pursuit by targets
//					List<Point> targets = g.getNodesByType
//						(NodeType.Target);
//					int pursuerRange = 0;
//					int pursuersPerTarget = gm.A_P.Count / targets.Count;

//					foreach (Point t in targets) {
//						pursuitLocations = Pursuit.SurroundCertainPursuitPursuers (g, gm.r_p, k,
//							new Utils.ListRangeEnumerable<Pursuer> (gm.A_P, pursuerRange, pursuerRange + pursuersPerTarget),
//							prevLocation,
//							new Location (pursuitTarget),
//							gm.r_e,
//							pursuitRound,
//							firstPursuer,
//							pursuitLocations);

//						pursuerRange += pursuersPerTarget;
//					}
//				} else if (pursuitRound > k) { // TODO Split pursuit by targets
//					List<Point> targets = g.getNodesByType
//						(NodeType.Target);
//					int pursuerRange = 0;
//					int pursuersPerTarget = gm.A_P.Count / targets.Count;

//					foreach (Point t in targets) {
//						Pursuit.AdvanceCertainPursuitPursuers (g, gm.r_p, k,
//							new Utils.ListRangeEnumerable<Pursuer>
//							(gm.A_P, pursuerRange, pursuerRange + pursuersPerTarget),
//							prevLocation,
//							new Location (pursuitTarget),
//							gm.r_e,
//							pursuitRound,
//							firstPursuer);

//						pursuerRange += pursuersPerTarget;
//					}
//				} else {
//					List<Point> targets = g.getNodesByType
//						(NodeType.Target);
//					int pursuerRange = 0;
//					int pursuersPerTarget = gm.A_P.Count / targets.Count;

//					foreach (Point t in targets) {
//						Pursuit.ResetUniformPursuitPursuers (g, gm.r_p,
//							new Utils.ListRangeEnumerable<Pursuer>
//							(gm.A_P, pursuerRange, pursuerRange + pursuersPerTarget),
//							prevLocation,
//							initLocation,
//							gm.r_e,
//							firstPursuer);

//						pursuerRange += pursuersPerTarget;
//					}
//				}
//			}

//			return prevLocation;
//		}

//	}
//}

