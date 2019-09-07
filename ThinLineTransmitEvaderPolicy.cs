//using GoE.GameLogic;
//using GoE.UI;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//
//using GoE.GameLogic.EvolutionaryStrategy;

//namespace GoE.Policies
//{

//	class ThinLineTransmitEvaderPolicy : AEvadersPolicy {
//		private GridGameGraph g;
//		private GoEGameParams gm;
//		private IPolicyGUIInputProvider pgui;

//		private GameState prevS;
//		private IEnumerable<Point> prevO_d;
//		private HashSet<Point> prevO_p;
//		private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();
//		private Dictionary<Evader, Location> lastTransmissionLocations = new Dictionary<Evader, Location>();

//		private int prevRound;
//		private Point sink;
//		private Point target;
//		private Point dest;
//		private Evader lead;
//		private Dictionary<Evader,Location> prevEvadersLocations = new Dictionary<Evader,Location>();
//		private Dictionary<Evader, Location> destEvadersLocations = new Dictionary<Evader, Location>();

//		/// <summary>
//		/// tells which units have reached the sink successfully
//		/// </summary>
//		private List<DataUnit> dataUnitsInSink = new List<DataUnit>();

//		public override void setGameState(int currentRound, IEnumerable<Point> O_d, HashSet<Point> O_p, GameState s)
//		{
//			// TODO investigate burst increasing data units
//			Console.WriteLine("---------");
//			prevS = s;
//			prevO_p = O_p;
//			prevO_d = O_d;
//			prevRound = currentRound;

//			foreach(Evader e in gm.A_E)
//				if(currentEvadersLocations[e].locationType == Location.Type.Node &&
//					O_d.Contains(currentEvadersLocations[e].nodeLocation))
//				{
//					currentEvadersLocations[e] = new Location(Location.Type.Captured);
//					if(destEvadersLocations.ContainsKey(e)) {
//						destEvadersLocations.Remove(e);
//					}
//				}

//			updateSink(currentRound);

//			Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();
//			markedLocations.Add("Detected Pursuers(O_p)", O_p.ToList());
//			markedLocations.Add("Destroyed Evaders(O_d)", O_d.ToList());

//			List<Point> receptionArea = new List<Point>();
//			List<Point> detectionArea = new List<Point>();
//			foreach (Evader e in gm.A_E)
//			{
//				if (currentEvadersLocations[e].locationType == Location.Type.Node)
//				{
//					receptionArea.AddRange(
//						g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_e));
//					detectionArea.AddRange(
//						g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation, gm.r_s));
//				}
//			}
//			markedLocations.Add("Area within r_e(reception)", receptionArea);
//			markedLocations.Add("Area within r_s(pursuer detection)", detectionArea);


//			pgui.markLocations(markedLocations);

//		}

//        public override bool init(GridGameGraph G, GoEGameParams prm, AGoEPursuersPolicy p, IPolicyGUIInputProvider gui)
//		{
//			this.g = G;
//			this.gm = prm;
//			this.pgui = gui;

//			// Choose sink (randomise?)
//			target = g.getNodesByType(NodeType.Target)[0];
//			List<Point> sinks = g.getNodesByType(NodeType.Sink).OrderBy(key => g.getMinDistance(key,target)).ToList();
//			Random rand = new Random ((int)DateTime.UtcNow.Ticks);
//			sink = sinks [rand.Next (0, sinks.Count)];
//			foreach (Evader e in gm.A_E) {
//				currentEvadersLocations [e] = new Location (Location.Type.Unset);
//				prevEvadersLocations [e] = new Location (Location.Type.Unset);
//			}

//            return true;
//		}

//		public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep ()
//		{
//			// TODO Initialise res to prevent exceptions in GameProcess
//			// TODO What to do if not enough evaders?
//			Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader,Tuple<DataUnit,Location,Location>> ();
//			// Captured? Reset

//			// Init?

//			// Advance?

//			// Transmit and restart?

//			if (prevO_d.Count () > 0) {
//				// TODO Choose new sink?
//				foreach (Evader e in gm.A_E) {
//					if (currentEvadersLocations [e].locationType == Location.Type.Node) {
//						// Reset, back to sink
//						destEvadersLocations [e] = new Location (sink);
//						// Move TODO check for pursuer
//						currentEvadersLocations [e] = Pursuit.moveTo (currentEvadersLocations [e].nodeLocation, destEvadersLocations [e].nodeLocation, 1);
//						res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//						// Also reset dest, be careful about condition v
//					}
//				}
//			} else if (destEvadersLocations.Count > 0) {
//				if (g.getMinDistance(currentEvadersLocations[lead].nodeLocation,dest)==1&&prevO_p.Contains(dest)) {
//					reset ();
//				}
//				res = advance (res);
//				if (currentEvadersLocations [lead].nodeLocation == dest)
//					reset ();
//				/*

//				foreach (Evader e in gm.A_E) {
//					if (currentEvadersLocations [e].locationType != Location.Type.Captured) {
//						if (destEvadersLocations.ContainsKey (e)) {
//							// Not yet at destination
//							if (currentEvadersLocations [e].nodeLocation == destEvadersLocations [e].nodeLocation) {
//								// Already at destination
//								if (currentEvadersLocations [e].nodeLocation == sink) {
//									List<DataUnit> newData = prevS.M [prevRound] [e].Except (dataUnitsInSink).Except(prevS.M [0] [e]).ToList ();
//									if(newData.Count>0) {
//										res.Add (e, new Tuple<DataUnit, Location, Location> (newData[0], currentEvadersLocations [e], new Location (Location.Type.Unset)));
//									} else {
//										destEvadersLocations.Remove (e);
//										res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//									}
//								} else {
//									// Something to send?
//									List<DataUnit> lastData = prevS.M [prevRound] [e].Except (dataUnitsInSink).Except (prevS.M [prevRound - 1] [e]).ToList ();
//									// Is it possible to eavesdrop and transmit? It is, but not relevant for us
//									// Eavesdrop or transmit
//									// TODO Eavesdrop every round, for variation with escape
//									if (lastData.Count > 0) {
//										Console.WriteLine (currentEvadersLocations [e] + ": Data received. Passing on");
//										if (currentEvadersLocations [e].nodeLocation != dest)
//											currentEvadersLocations [e] = Algorithms.moveTo (currentEvadersLocations [e].nodeLocation, sink, 1);
//										//TODO update destLoc to match new dest
//										else {
//											//TODO move after transmission
//											if (destEvadersLocations.Count > 1) {*/
//												/*List<Point> possPoints = g.getNodesWithinDistance (first,gm.r_e).Intersect(g.getNodesWithinDistance(target,gm.r_e)).ToList();
//												Random rand = new Random ((int)DateTime.UtcNow.Ticks);
//												dest = possPoints [rand.Next (0, possPoints.Count)];
//												destEvadersLocations [e] = new Location (dest);
//												currentEvadersLocations[e] = Algorithms.moveTo (currentEvadersLocations [e].nodeLocation, dest, 1);*//*
//											}
//										}
//										res.Add (e, new Tuple<DataUnit, Location, Location> (lastData [0], currentEvadersLocations [e], new Location (Location.Type.Unset)));
//									} else if (g.getMinDistance (currentEvadersLocations [e].nodeLocation, target) <= gm.r_e) {
//										Console.WriteLine ("Eavesdropping");
//										res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (target)));
//									} else {
//										res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//									}
//								}
//							} else {
//								// Move TODO check for pursuer
//								// FIXME Stops when blocked
//								Location loc = Algorithms.moveTo (currentEvadersLocations [e].nodeLocation, destEvadersLocations [e].nodeLocation, 1);
//								if (prevO_p.Contains (loc.nodeLocation)) {
//									currentEvadersLocations [e] = loc;
//									foreach(Point p in g.getNodesWithinDistance(currentEvadersLocations[e].nodeLocation,1)) {
//										if (!prevO_p.Contains (p) &&
//											g.getMinDistance (p, destEvadersLocations [e].nodeLocation) < g.getMinDistance (currentEvadersLocations [e].nodeLocation, destEvadersLocations [e].nodeLocation)) {
//											loc = new Location (p);
//											break;
//										}
//									}
//								}
//								if (currentEvadersLocations [e] == loc) {
//									destEvadersLocations [e] = currentEvadersLocations [e];
//									// Something to send?
//									List<DataUnit> lastData = prevS.M [prevRound] [e].Except (dataUnitsInSink).Except (prevS.M [prevRound - 1] [e]).ToList ();
//									// TODO Is it possible to eavesdrop and transmit?
//									// Eavesdrop or transmit
//									if (lastData.Count > 0) {
//										Console.WriteLine (currentEvadersLocations [e] + ": Data received. Passing on");
//										if (currentEvadersLocations [e].nodeLocation != dest)
//											currentEvadersLocations [e] = Algorithms.moveTo (currentEvadersLocations [e].nodeLocation, sink, 1);
//										res.Add (e, new Tuple<DataUnit, Location, Location> (lastData [0], currentEvadersLocations [e], new Location (Location.Type.Unset)));
//									} else if (g.getMinDistance (currentEvadersLocations [e].nodeLocation, target) <= gm.r_e) {
//										Console.WriteLine ("Eavesdropping");
//										res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (target)));
//									} else {
//										res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//									}
//								} else {
//									currentEvadersLocations [e] = loc;
//									res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//								}
//							}
//						} else {
//							res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//						}
//					}
//				}
//*/
//			} else {
//				reset ();
//				res = advance (res);
//				/*
//				// At sink, build path
//				Random rand = new Random ((int)DateTime.UtcNow.Ticks);
//				List<Point> possLoc = g.getNodesWithinDistance (sink, g.getMinDistance (sink, target)).Intersect (g.getNodesWithinDistance (target, gm.r_e)).ToList ();
//				Location loc = new Location (possLoc [rand.Next (0, possLoc.Count)]);
//				dest = loc.nodeLocation;
//				//first = Algorithms.moveTo (loc.nodeLocation, sink, gm.r_e).nodeLocation;
//				foreach (Evader e in gm.A_E) {
//					if (currentEvadersLocations [e].locationType != Location.Type.Captured) {
//						if (loc.nodeLocation != sink) {
//							destEvadersLocations [e] = loc;
//							currentEvadersLocations [e] = Algorithms.moveTo (currentEvadersLocations [e].nodeLocation, destEvadersLocations [e].nodeLocation, 1);
//							res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//							loc = Algorithms.moveTo (loc.nodeLocation, sink, gm.r_e);
//						} else {
//							res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//						}
//					}
//				}
//			*/}
//			foreach (Evader e in gm.A_E.Except(res.Keys)) {
//				res.Add (e, new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [e], new Location (Location.Type.Unset)));
//}
//			return res;
//		}

//		private void updateSink(int currentRound)
//		{
//			foreach (Evader e in gm.A_E)
//				if (currentEvadersLocations[e].locationType == Location.Type.Node &&
//					prevS.B_O[currentRound-1][e] != DataUnit.NIL &&
//					prevS.B_O[currentRound-1][e] != DataUnit.NOISE)
//				{
//					foreach (var s in g.getNodesByType(NodeType.Sink))
//						// TODO Better fix for duplicates?
//						if (g.getMinDistance(s, currentEvadersLocations[e].nodeLocation) <= gm.r_e && !dataUnitsInSink.Contains(prevS.B_O[currentRound-1][e]))
//							dataUnitsInSink.Add(prevS.B_O[currentRound-1][e]);
//				}

//		}
		
//        private List<Location> getPossibleNextLocations(Location currentAgentLocation)
//		{
//			List<Location> res = new List<Location>();
//			res.Add(currentAgentLocation);

//			if (currentAgentLocation.locationType == Location.Type.Unset)
//				foreach (var n in g.getNodesByType(NodeType.Sink))
//					res.AddRange(GameLogic.Utils.pointsToLocations(g.getNodesWithinDistance(n, gm.r_e)));
//			else
//				foreach (var n in g.getNodesWithinDistance(currentAgentLocation.nodeLocation, 1))
//					res.Add(new Location(n));

//			return res;
//		}

//		private void reset() {
//			// TODO first check for transmission?
//			dest = chooseDest (sink, target);
//			List<Point> path = buildPath (sink, dest);
//			List<Evader> squad = gm.A_E.Where (key => currentEvadersLocations [key].locationType == Location.Type.Node)
//				.OrderByDescending (key => g.getMinDistance (sink, currentEvadersLocations [key].nodeLocation)).ToList ();
//			List<Evader> newSquad = new List<Evader> ();
//			List<Evader>.Enumerator squadEnum = squad.GetEnumerator ();
//			foreach (Point p in path) {
//				if (squadEnum.MoveNext ()) {
//					destEvadersLocations [squadEnum.Current] = new Location (p);
//				} else {
//					List<Evader> unset = gm.A_E.Where (key => (currentEvadersLocations [key].locationType == Location.Type.Unset)).Except (newSquad).ToList ();
//					if (unset.Count > 0) {
//						destEvadersLocations [unset [0]] = new Location (p);
//						prevEvadersLocations [unset [0]] = currentEvadersLocations [unset [0]];

//						currentEvadersLocations [unset [0]] = Pursuit.moveTo (sink, p, gm.r_e - 1);
//						//res.Add (unset [0], new Tuple<DataUnit, Location, Location> (DataUnit.NIL, currentEvadersLocations [unset [0]], new Location (Location.Type.Unset)));

//						newSquad.Add (unset [0]);
//						squadEnum.MoveNext ();
//					}
//				}
//			}
//			lead = destEvadersLocations.Where (p => p.Value.nodeLocation == dest).Select (p => p.Key).ToList () [0]; // FIXME can cause exception

//			foreach (Evader e in gm.A_E) {
//				prevEvadersLocations [e] = new Location (Location.Type.Unset);
//			}
//		}

//		private Point chooseSink() {
//			List<Point> sinks = g.getNodesByType(NodeType.Sink);
//            Random rand = new ThreadSafeRandom().rand;
//			return(sinks [rand.Next (0, sinks.Count)]);
//		}

//		private Point chooseDest(Point s,Point t) {
//			List<Point> dests = g.getNodesWithinDistance(sink, g.getMinDistance (s, t)-2).Intersect (g.getNodesWithinDistance (t, gm.r_e)).ToList ();
//            Random rand = new ThreadSafeRandom().rand;
//			return(dests [rand.Next (0, dests.Count)]);
//		}

//		private List<Point> buildPath(Point s, Point d) {
//			List<Point> pl = new List<Point> (){d};
//			Point p = Pursuit.moveTo (d, sink, gm.r_e).nodeLocation;
//			for (int i = 0; i<Math.Floor(g.getMinDistance(s,d)/gm.r_e); i++) {
//					pl.Add (p);
//					p = Pursuit.moveTo (p, sink, gm.r_e).nodeLocation;
//			}
//			pl.Remove (sink);
//			return pl;
//		}

//		//Advance
//		private Dictionary<Evader, Tuple<DataUnit, Location, Location>> advance(Dictionary<Evader, Tuple<DataUnit, Location, Location>> res) {

//			foreach (Evader e in gm.A_E) {
//				Location snoop = new Location (Location.Type.Unset);
//				DataUnit data = DataUnit.NIL;
//				if (currentEvadersLocations [e].locationType == Location.Type.Node) {
					
//                    List<DataUnit> lastData = prevS.M [prevRound][e].ToList().Except (prevS.M [Math.Max (prevRound - 1, 0)][e].ToList()).ToList ();
					
//                    if (destEvadersLocations.ContainsKey (e)) {
//						if (currentEvadersLocations [e] != destEvadersLocations [e]) {
//							//TODO check if surrounded, prevent back and forth
//					// Use random <= curPos instead? if only curPos, reset instead
//							Point nextPos = Pursuit.moveTo (currentEvadersLocations [e].nodeLocation, destEvadersLocations [e].nodeLocation, 1).nodeLocation;
//					Point backfix = new Point (currentEvadersLocations [e].nodeLocation.X + (currentEvadersLocations [e].nodeLocation.X-nextPos.X),
//						currentEvadersLocations [e].nodeLocation.Y + (currentEvadersLocations [e].nodeLocation.Y-nextPos.Y));
//							if (prevO_p.Contains (dest)) {
//								reset (); // Should be covered in control
//							} else {
//						//Ugly fix for blocking
//								List<Location> possLoc = getPossibleNextLocations (currentEvadersLocations [e]).Except (new List<Location> {
//							currentEvadersLocations [e],prevEvadersLocations [e],new Location(backfix)
//								}).Except (GameLogic.Utils.pointsToLocations (prevO_p.ToList ())).ToList ();
//								prevEvadersLocations [e] = currentEvadersLocations [e];
//								currentEvadersLocations [e] = possLoc.OrderBy (key => g.getMinDistance (key.nodeLocation, destEvadersLocations [e].nodeLocation)).ToList () [0];
//							}
//						} else if (currentEvadersLocations [e].nodeLocation == sink) {
//							lastData = prevS.M [prevRound] [e].ToList().Except (dataUnitsInSink).Except (prevS.M [0] [e].ToList()).ToList ();
//							Console.WriteLine ("Data to send");
//							if (lastData.Count == 0) {
//								Console.WriteLine ("Arrived at sink");
//								destEvadersLocations.Remove (e);
//							}
//						}
//				if ( e == lead && g.getMinDistance (currentEvadersLocations [e].nodeLocation, target) <= gm.r_e) {
//							snoop = new Location (target);
//						}

					
//						if (lastData.Count > 0 && (currentEvadersLocations [e].nodeLocation == dest || destEvadersLocations [e].nodeLocation != dest)) {
//							// TODO move away after sending?
//							data = lastData [0];
//							Console.WriteLine ("Data sent");

//						}
//					}
//				}
//				res.Add (e, new Tuple<DataUnit, Location, Location> (data, currentEvadersLocations [e], snoop));
//			}
//			return res;
//		}

//		//TransmitAndInit
//	}
//}
