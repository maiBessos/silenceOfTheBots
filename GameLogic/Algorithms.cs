using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using System.Drawing;
using GoE.GameLogic.EvolutionaryStrategy;
using System.IO;
using AForge.Genetic;
using GoE.GameLogic.EvolutionaryStrategy.EvaderSide;
using GoE.Utils.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;

namespace GoE.GameLogic
{
    namespace Algorithms
    {
       
        public static class Pursuit
        {
			/// <summary>
			/// sets initial positions for pursuers, for use of the certain pursuit
			/// </summary>
			/// <param name="L">
			/// L will be updated such that it contains clusters of pursuers, distributed uniformly
			/// in the area
			/// </param>
			/// <param name="v">
			/// center of area
			/// </param>
			/// <param name="d">
			/// radius of the area
			/// </param>
			/// <returns>
			/// the first pursuer (i.e. A_P[0] in the paper - the pursuer that gets the minimal x)
			/// </returns>
			public static Pursuer InitCertainPursuit(GridGameGraph graph, int r_p, int k,
				ListRangeEnumerable<Pursuer> pursuers,
				Dictionary<Pursuer, Location> L, Location v, int d)
			{
				if (k == 0)
				{
					List<Location> grid = getPursuitGrid(graph, v, d, r_p);

					if (5 * grid.Count > pursuers.Count())
					{ // TODO What to do with more pursuers?
						throw new AlgorithmException("Not enough pursuers/pursuers too slow" +
							" for InitUniformPursuit()! (at least " + 5 * grid.Count + " pursuers are " +
							"needed, with current velocity)");
					}

					var pursEnum = pursuers.GetEnumerator();
					for (int i = 0; i < 5; i++)
					{
						foreach (Location loc in grid)
						{
							if (pursEnum.MoveNext())
								L[pursEnum.Current] = loc;
						}
					}
					while (pursEnum.MoveNext())
					{
						L[pursEnum.Current] = v;
					}
				}
				else
				{
					List<Location> grid = getPursuitGrid(graph, v, d + k, k * r_p);

					if (4 * k * grid.Count > pursuers.Count())
					{ // TODO What to do with more pursuers?
						throw new AlgorithmException("Not enough pursuers/pursuers too slow" +
							" for InitUniformPursuit()! (at least " + 4 * k * grid.Count + " pursuers are " +
							"needed, with current velocity)");
					}

					var pursEnum = pursuers.GetEnumerator();
					for (int i = 0; i < 4 * k; i++)
					{
						foreach (Location loc in grid)
						{
							if (pursEnum.MoveNext())
								L[pursEnum.Current] = loc;
						}
					}
					while (pursEnum.MoveNext())
					{
						L[pursEnum.Current] = v;
					}
				}

				//TODO d not divisable e
				//TODO non placed pursuers
				return pursuers.data[pursuers.start];
			}

			/// <summary>
			/// updates locations in L of given pursuers, assuming they were previously
			/// positioned using InitCertainPursuit() and AdvanceCertainPursuitPursuers() calls
			/// </summary>
			/// <param name="graph"></param>
			/// <param name="r_p"></param>
			/// <param name="pursuers"></param>
			/// <param name="L"></param>
			/// <param name="v"></param>
			/// <param name="d"></param>
			/// <param name="round">
			/// the last round in which pursuers were positioned
			/// </param>
			/// <param name="firstPursuer">
			/// A_P[0] (in the paper) -  the pursuer returned by InitCertainPursuit()
			/// </param>
			public static void AdvanceCertainPursuitPursuers(GridGameGraph graph,
				int r_p, int k,
				ListRangeEnumerable<Pursuer> pursuers,
				Dictionary<Pursuer, Location> L, Location v, int d, int pursuitRound,
				Pursuer firstPursuer)
			{
				List<Pursuer> squad = new List<Pursuer>();
				foreach (Pursuer p in pursuers)
				{
					if (graph.getMinDistance(L[p].nodeLocation, v.nodeLocation) <= k)
					{
						squad.Add(p);
					}
				}
				int distToV = (int)graph.getMinDistance(L[squad[0]].nodeLocation, v.nodeLocation);

				// TODO Weird workaround as getNodesInDistance seems to be broken, returns duplicates
				List<Location> pursuitArea = new List<Location>();
				graph.getNodesWithinDistance(v.nodeLocation, distToV - 1).ForEach(key => pursuitArea.Add(new Location(key)));
				//graph.getNodesInDistance (v.nodeLocation, k).ForEach(key=>pursuitArea.Add(new Location(key)));
				pursuitArea = pursuitArea.Where(key => graph.getMinDistance(key.nodeLocation, v.nodeLocation) == distToV - 1).ToList();
				List<Pursuer>.Enumerator pursEnum = squad.GetEnumerator();
				for (int t = 0; t < pursuitArea.Count; t++)
				{
					if (pursEnum.MoveNext())
						//L [pursEnum.Current ] = moveTo(L[pursEnum.Current].nodeLocation, pursuitArea[t].nodeLocation,k*r_p); // Use movement instead, overlap uses Pursuers from all surrounding areas
						L[pursEnum.Current] = pursuitArea[t]; // Use movement instead, overlap uses Pursuers from all surrounding areas
				}
			}

            /// <summary>
            /// updates locations in L of given pursuers, assuming they were previously
            /// positioned using InitCertainPursuit() and AdvanceCertainPursuitPursuers() calls
            /// </summary>
            /// <param name="graph"></param>
            /// <param name="r_p"></param>
            /// <param name="pursuers"></param>
            /// <param name="L"></param>
            /// <param name="v"></param>
            /// <param name="d"></param>
            /// <param name="round">
            /// the last round in which pursuers were positioned
            /// </param>
            /// <param name="firstPursuer">
            /// A_P[0] (in the paper) -  the pursuer returned by InitCertainPursuit()
            /// </param>
            public static Dictionary<Pursuer, Location> SurroundCertainPursuitPursuers(GridGameGraph graph,
                int r_p, int k,
                ListRangeEnumerable<Pursuer> pursuers,
                Dictionary<Pursuer, Location> L, Location v, int d, int pursuitRound,
                Pursuer firstPursuer, Dictionary<Pursuer, Location> P)
            {
                if (P.Count == 0)
                {
                    //TODO if dist target<...
                    List<Pursuer> squad = pursuers.OrderBy(key => graph.getMinDistance(L[key].nodeLocation, v.nodeLocation)).ToList().GetRange(0, 4 * k);
                    // TODO Weird workaround as getNodesInDistance seems to be broken, returns duplicates
                    List<Location> pursuitArea = new List<Location>();
                    graph.getNodesWithinDistance(v.nodeLocation, k).ForEach(key => pursuitArea.Add(new Location(key)));
                    //graph.getNodesInDistance (v.nodeLocation, k).ForEach(key=>pursuitArea.Add(new Location(key)));
                    pursuitArea = pursuitArea.Where(key => graph.getMinDistance(key.nodeLocation, v.nodeLocation) == k).ToList();
                    List<Pursuer>.Enumerator pursEnum = squad.GetEnumerator();
                    for (int t = 0; t < pursuitArea.Count; t++)
                    {
                        if (pursEnum.MoveNext())
                            P.Add(pursEnum.Current, pursuitArea[t]);
                    }
                }

                foreach (Pursuer p in P.Keys)
                {
                    L[p] = moveTo(L[p].nodeLocation, P[p].nodeLocation, r_p);
                }
                return (P);
            }
            //TODO tuple targets, pursuers

            /// <summary>
            /// updates locations in L of given pursuers, assuming they were previously
            /// positioned using InitCertainPursuit() and AdvanceCertainPursuitPursuers() calls
            /// </summary>
            /// <param name="graph"></param>
            /// <param name="r_p"></param>
            /// <param name="pursuers"></param>
            /// <param name="L"></param>
            /// <param name="v"></param>
            /// <param name="d"></param>
            /// <param name="round">
            /// the last round in which pursuers were positioned
            /// </param>
            /// <param name="firstPursuer">
            /// A_P[0] (in the paper) -  the pursuer returned by InitCertainPursuit()
            /// </param>
            public static void SpecialCertainPursuitPursuers(GridGameGraph graph,
                int r_p, int k,
                ListRangeEnumerable<Pursuer> pursuers,
                Dictionary<Pursuer, Location> L, Location v, int d, int pursuitRound,
                Pursuer firstPursuer)
            {
                //TODO if dist target<...
                List<Pursuer> squad = pursuers.OrderBy(key => graph.getMinDistance(L[key].nodeLocation, v.nodeLocation)).ToList().GetRange(0, r_p);
                // TODO Weird workaround as getNodesInDistance seems to be broken, returns duplicates
                List<Location> pursuitArea = new List<Location>();
                graph.getNodesWithinDistance(v.nodeLocation, 1).ForEach(key => pursuitArea.Add(new Location(key)));
                List<Pursuer>.Enumerator pursEnum = squad.GetEnumerator();
                for (int t = 0; t < pursuitArea.Count; t++)
                {
                    if (pursEnum.MoveNext())
                        L[pursEnum.Current] = moveTo(L[pursEnum.Current].nodeLocation, pursuitArea[t].nodeLocation, r_p);
                }
                return;
            }

            /// <summary>
            /// sets initial positions for pursuers, for use of the certain pursuit
            /// </summary>
            /// <param name="L">
            /// L will be updated such that it contains clusters of pursuers, distributed uniformly
            /// in the area
            /// </param>
            /// <param name="v">
            /// center of area
            /// </param>
            /// <param name="d">
            /// radius of the area
            /// </param>
            /// <returns>
            /// the first pursuer (i.e. A_P[0] in the paper - the pursuer that gets the minimal x)
            /// </returns>
            public static Pursuer InitImmediatePursuit(GridGameGraph graph, int r_p, int k,
                ListRangeEnumerable<Pursuer> pursuers,
                Dictionary<Pursuer, Location> L, Location v, int d)
            {
                List<Location> grid = getPursuitGrid(graph, v, d + 1, r_p);

                // TODO twice as many

                if (2 * k * grid.Count > pursuers.Count())
                { // TODO What to do with more pursuers?
                    throw new AlgorithmException("Not enough pursuers/pursuers too slow" +
                    " for InitUniformPursuit()! (at least " + 2 * k * grid.Count + " pursuers are " +
                    "needed, with current velocity)");
                }

                var pursEnum = pursuers.GetEnumerator();
                for (int i = 0; i < 2 * k; i++)
                {
                    foreach (Location loc in grid)
                    {
                        if (pursEnum.MoveNext())
                            L[pursEnum.Current] = loc;
                    }
                }
                while (pursEnum.MoveNext())
                {
                    L[pursEnum.Current] = v;
                }


                //TODO d not divisable e
                //TODO non placed pursuers
                return pursuers.data[pursuers.start];
            }

            /// <summary>
            /// sets initial positions for pursuers, for use of the uniform pursuit
            /// </summary>
            /// <param name="L">
            /// L will be updated such that it contains clusters of pursuers, distributed uniformly
            /// in the area
            /// </param>
            /// <param name="v">
            /// center of area
            /// </param>
            /// <param name="d">
            /// radius of the area
            /// </param>
            /// <returns>
            /// the first pursuer (i.e. A_P[0] in the paper - the pursuer that gets the minimal x)
            /// </returns>
            public static Pursuer InitUniformPursuit(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers, Dictionary<Pursuer, Location> L, Location v, int d)
		    {
			    List<Location> grid = getPursuitGrid (graph, v, d, r_p);

			    if (grid.Count > pursuers.Count()) // TODO What to do with more pursuers?
			    {
				    throw new AlgorithmException("Not enough pursuers/pursuers too slow for InitUniformPursuit()! (at least " + grid.Count + " pursuers are needed, with current velocity)");
			    }

			    var pursEnum = pursuers.GetEnumerator ();
			    foreach(Location loc in grid) {
				    if (pursEnum.MoveNext ())
					    L [pursEnum.Current ] = loc;
			    }
			    while (pursEnum.MoveNext ()) {
				    L [pursEnum.Current] = v;
			    }

			    //TODO d not divisable e
			    //TODO non placed pursuers
			    return pursuers.data [pursuers.start];
		    }

            /// <summary>
            /// updates locations in L of given pursuers, assuming they were previously
            /// positioned using InitImmediatePursuit() and AdvanceImmediatePursuit() calls
            /// </summary>
            /// <param name="graph"></param>
            /// <param name="r_p"></param>
            /// <param name="k"></param>
            /// <param name="pursuers"></param>
            /// <param name="L"></param>
            /// <param name="I"></param>
            /// <param name="pursuitArea"></param>
            /// <param name="d"></param>
            /// <param name="firstPursuer">
            /// A_P[0] (in the paper) -  the pursuer returned by InitImmediatePursuit()
            /// </param>
            public static void AdvanceImmediatePursuit(GridGameGraph graph,
                int r_p, int k, ListRangeEnumerable<Pursuer> pursuers,
                Dictionary<Pursuer, Location> L, Dictionary<Pursuer, Location> I,
                Point pursuitTarget, List<Point> pursuitArea)
            {
                Random rand = new ThreadSafeRandom().rand;

                //TODO if dist target<...
                List<Pursuer> squad = new List<Pursuer>();
                List<Pursuer> potential = new List<Pursuer>();
                if (pursuitArea.Count > 0)
                {
                    for (int t = 0; t < Math.Min(pursuitArea.Count, k); t++)
                    {
                        int r = rand.Next(0, pursuitArea.Count);

                       

                        potential = pursuers.Where(key => graph.getMinDistance(L[key].nodeLocation, pursuitArea[r]) <= r_p && !squad.Contains(key)).ToList();
                        if (potential.Count == 0)
                            continue;
                        Pursuer p = potential[rand.Next(0, potential.Count)];
                        L[p] = moveTo(L[p].nodeLocation, pursuitArea[r], r_p);
                        pursuitArea.RemoveAt(r);
                        squad.Add(p);
                    }
                }

                foreach (Pursuer p in pursuers.Except(squad))
                {
                    L[p] = I[p];
                }
                return;
            }

            /// <summary>
            /// updates locations in L of given pursuers, assuming they were previously 
            /// positioned using InitUniformPursuit() and AdvanceUniformPursuitPursuers() calls
            /// </summary>
            /// <param name="graph"></param>
            /// <param name="r_p"></param>
            /// <param name="pursuers"></param>
            /// <param name="L"></param>
            /// <param name="v"></param>
            /// <param name="d"></param>
            /// <param name="round">
            /// the last round in which pursuers were positioned
            /// </param>
            /// <param name="firstPursuer">
            /// A_P[0] (in the paper) -  the pursuer returned by InitUniformPursuit()
            /// </param>
            public static void AdvanceUniformPursuitPursuers(GridGameGraph graph, int r_p,
                ListRangeEnumerable<Pursuer> pursuers,
                Dictionary<Pursuer, Location> L, Location v, int d, int pursuitRound, Pursuer firstPursuer)
            {
                Dictionary<Pursuer, int> paReachable = new Dictionary<Pursuer, int>();
                List<Pursuer> paNotReachable = new List<Pursuer>();
                Point target = v.nodeLocation;
                List<Location> pursuitArea = new List<Location>();
                graph.getNodesWithinDistance(target, pursuitRound).ForEach(key => pursuitArea.Add(new Location(key))); //getPursuitArea (graph, v, pursuitRound);

                foreach (Pursuer p in pursuers)
                {
                    Point pos = L[p].nodeLocation;

                    if (graph.getMinDistance(pos, target) <= Math.Max(r_p - pursuitRound, pursuitRound))
                    { // Can reach every point or is in pursuit area
                        paReachable.Add(p, (int)graph.getMinDistance(pos, target));
                    }
                    else
                    {
                        paNotReachable.Add(p);
                    }
                }
                // TODO divide pursuit area better
                int d_pa = pursuitRound;
                int r_pa = Math.Min(2 * (int)Math.Floor((double)(d_pa / Math.Floor(Math.Sqrt((double)paReachable.Count)))), r_p);
                List<Location> pursuitGrid = new List<Location>();

                pursuitGrid = getPursuitGrid(graph, v, d_pa, r_pa);

                Random rand = new ThreadSafeRandom().rand;
                List<Location> coveredArea = new List<Location>();
                int gridPos = 0;
                foreach (Pursuer p in paReachable.Keys)
                {
                    if (gridPos < pursuitGrid.Count)
                    {
                        List<Location> cell = new List<Location>();
                        graph.getNodesWithinDistance(pursuitGrid[gridPos].nodeLocation, r_pa)
                            .Where(key => graph.getMinDistance(target, key) <= d_pa).ToList().ForEach(key => cell.Add(new Location(key)));
                        //getReachableLocations (pursuitArea, pursuitGrid [gridPos], r_pa);
                        int r = rand.Next(0, cell.Count);
                        //moveTo (L[p].nodeLocation, reachPos [r].nodeLocation, r_p); // just go to reachpos[r]
                        if (cell.Count > 0)
                            L[p] = cell[r];
                        coveredArea.AddRange(cell);
                        gridPos++;
                    }
                    else
                    {
                        //reachPos = getReachableLocations (pursuitArea, L [p], r_p);
                        List<Location> reachable = new List<Location>();
                        graph.getNodesWithinDistance(L[p].nodeLocation, r_p).ForEach(key => reachable.Add(new Location(key)));
                        List<Location> notCovered = pursuitArea.Intersect(reachable).Except(coveredArea).ToList();
                        if (notCovered.Count != 0)
                        {
                            int r = rand.Next(0, notCovered.Count);
                            L[p] = notCovered[r];
                            coveredArea.Add(notCovered[r]);
                        }
                    }

                }
                foreach (Pursuer p in paNotReachable)
                {
                    L[p] = moveTo(L[p].nodeLocation, target, r_p);
                }
            }

            public static List<Location> getPursuitGrid(GridGameGraph graph, Location center, int d, int r_p)
            {
                d = (int)Math.Ceiling((double)d / r_p) * r_p;
                List<Location> grid = new List<Location>();

                for (int x = -d + r_p; x <= d - r_p; x += r_p)
                {
                    for (int y = -d + r_p + Math.Abs(x); y <= d - r_p - Math.Abs(x); y += 2 * r_p)
                    {
                        grid.Add(new Location(new Point(center.nodeLocation.X + x, center.nodeLocation.Y + y)));
                    }
                }
                return grid;
            }

            public static Location moveTo(Point start, Point target, int r_p)
            {
                // Pursuer function?
                int xdist = Math.Abs(target.X - start.X);
                int ydist = Math.Abs(target.Y - start.Y);
                if (target != start)
                {
                    //if (xdist >= ydist) {
                    int xstep = Math.Min(xdist, r_p);
                    int xsign = (xdist > 0 ? (target.X - start.X) / xdist : 0);
                    int ystep = 0;
                    int ysign = (ydist > 0 ? (target.Y - start.Y) / ydist : 0);
                    if (xdist < r_p)
                    {
                        ystep = Math.Min(ydist, r_p - xdist);
                    }
                    return (new Location(new Point(start.X + xsign * xstep, start.Y + ysign * ystep)));
                    /*	} else if (Math.Abs (target.X - start.X) < Math.Abs (target.Y - start.Y)) {
                        int step = Math.Min (Math.Abs (target.Y - start.Y), r_p);
                        int sign = (target.Y - start.Y) / Math.Abs (target.Y - start.Y);
                        return (new Location (new Point (start.X, start.Y + sign * step)));
                    }*/
                }
                return (new Location(start));
            }

            public static void ResetUniformPursuitPursuers(GridGameGraph graph, int r_p,
                ListRangeEnumerable<Pursuer> pursuers,
                Dictionary<Pursuer, Location> L, Dictionary<Pursuer, Location> I, int d, Pursuer firstPursuer)
            {
                foreach (Pursuer p in pursuers) // FIXME More pursuers results in crash
                {
                    L[p] = moveTo(L[p].nodeLocation, I[p].nodeLocation, r_p);
                }
            }
        
            public static int getMinimalPursuersCount(double r_e, double r_p, int k)
            {
                //return (int)(2.0 * 5 * Math.Pow(Math.Ceiling((r_e + 1) / r_p), 2) / 5);
                //return (2 * k * (int)Math.Ceiling((double)(r_e + 1) / r_p) ^ 2);

                return (2 * k * (int)Math.Pow((int)Math.Ceiling(  (double)(r_e + 1) / r_p),2) );
                
            }
            public static int getUsedPursuers(double r_e, double r_p, double maxPursuers)
            {
                int k= getK(r_e, r_p, maxPursuers);
                double minPursuers = getMinimalPursuersCount(r_e,r_p,k);
                if (minPursuers == 0)
                    return 0;
                return (int)(minPursuers * Math.Min(5,((int)maxPursuers / (int)minPursuers)));
            }
            public static double getCaptureProbability(double r_e, double r_p, double maxPursuers)
            {
                //return ((double)getUsedPursuers(r_e, r_p, maxPursuers) / getMinimalPursuersCount(r_e, r_p))/5.0;
                int k = 5;
                while (getMinimalPursuersCount(r_e, r_p, k) > maxPursuers)
                {
                    k--;
                }
                return (k * 0.2);
            }
            public static int getK(double r_e, double r_p, double maxPursuers)
            {
                int k = 5;
                while (getMinimalPursuersCount(r_e, r_p, k) > maxPursuers)
                {
                    k--;
                }
                return k;
            }
        }

        public abstract class APatrol : GoE.Utils.ReflectionUtils.DerivedInstancesProvider<APatrol>
        {
            /// <summary>
            /// returns a new uninitialized object of the same type
            /// </summary>
            /// <returns></returns>
            public abstract APatrol generateNew();

            public abstract double getCaptureProbability(int r_p, int d, int maxPursuersCount);
            public abstract int minimalPursuersCount(int r_p, int d);
            public abstract int getUsedPursuersCount(int r_p, int d, int pursuersCount);
            
            public abstract void Init(GridGameGraph graph, int r_p,
                                  ListRangeEnumerable<Pursuer> pursuers,
                                  Dictionary<Pursuer, Location> L, Location v, int d);

            public abstract void advancePursuers(GridGameGraph graph, int r_p, 
                                    ListRangeEnumerable<Pursuer> pursuers,
                                    Dictionary<Pursuer, Location> L, Location v, int d);
        
        }
       
        /// <summary>
        /// NOTE: the original circular algorithm won't work - the designated area for each pursuer is problematic.
        /// if the agents reachs the edge of the vertical area and now have to go back, this is the same problem
        /// as the sparse grid patrol.
        /// if the cycles are not moving in coordination and instead each cycle moves vertically on it's own, then the pursuers 
        /// not always can go from one vertical extreme to the other vertical extreme and still reach the horizontal extremes, i.e. not all of it's area is reachable
        /// </summary>
        //class CircularUniformAreaPatrol
        //{
        //    public static double getMinimalCaptureProbability(int r_p, int areaRad, int maxPursuersCount)
        //    {
        //        int areaDominatedByPursuer = (r_p) * (r_p / 2);
        //        int pursuerGroupSize = maxPursuersCount / minimalPursuersCount(r_p, areaRad);

        //        return (double)(pursuerGroupSize) / (areaDominatedByPursuer - pursuerGroupSize);
        //    }
        //    public static int getUsedPursuersCount(int r_p, int areaRad, int maxPursuersCount)
        //    {
        //        int minPCount = minimalPursuersCount(r_p, areaRad);
        //        if (minPCount == 0)
        //            return 0;
        //        return maxPursuersCount - (maxPursuersCount % minPCount);
        //    }
        //    public static int minimalPursuersCount(int r_p, int areaRad)
        //    {
        //        int pursuersCount = 0;

        //        r_p -= r_p % 4;

        //        if (r_p == 0)
        //            return 0;

        //        int x;
                
        //        // FIXME : I'm not sure points are set exactly as needed
        //        for (x = -areaRad; x <= 0; x += r_p / 2)
        //            pursuersCount += (-areaRad - x + 2 * (x + areaRad) + r_p + 1 - (-areaRad - x)) / (r_p + 1);

        //        for (; x < areaRad + r_p / 4; x += r_p / 2)
        //            pursuersCount += (-areaRad + x + 2 * (areaRad - x) + r_p + 1 - (-areaRad + x)) / (r_p + 1);

        //        return pursuersCount;
        //    }
 

        //    public void Init(GridGameGraph graph, int r_p, 
        //                     ListRangeEnumerable<Pursuer> pursuers,
        //                     Dictionary<Pursuer, Location> L, Location areaCenter, int areaRad)
        //    {
                

        //        r_p -= r_p % 4;
        //        int xAxisMovement = r_p / 3; // to each direction (not both)
        //        int circularAxisMovement = 2 * xAxisMovement; // to each direction  (not both)

        //        m_xDiffBetweenPursuers = r_p / 2;
        //        m_currentMinPursuerX = 0;

        //        ListRangeEnumerator<Pursuer> pIter = (ListRangeEnumerator<Pursuer>)pursuers.GetEnumerator();
        //        pIter.MoveNext();
        //        int x;

        //        // each Y loop sets a group of pursuers that should do a circular path
        //        // FIXME1: I'm not sure points are set exactly as needed. 
        //        // FIXME2: add support for pursuer groups

        //        int maxX = (2 * xAxisMovement + 1) * (int)Math.Ceiling((float)(areaRad - xAxisMovement) / (2 * xAxisMovement + 1));
        //        int minX = -maxX;
        //        for (x = minX; x <= maxX; x += 2 * xAxisMovement + 1)
        //        {
        //            if(Math.Abs(x) >= areaRad )
        //            {
        //                // optionally occurs on first and last iterations

        //                int tmpX = x.LimitRange(-areaRad,areaRad);
        //                PursuerState newPursuer = new PursuerState()
        //                {
        //                    dir = PursuerDirection.Right,
        //                    minY = -xAxisMovement,
        //                    maxY = xAxisMovement,
        //                    minX =  tmpX,
        //                    maxX =  tmpX
        //                }; // the lone pursuer only keeps a "line", and may move on x only with coordination with the rest of the pursuers

        //                newPursuer.location = areaCenter.nodeLocation.add(tmpX, 0);
        //                m_pursuerCircularPath[pIter.Current] = newPursuer;
        //                L[pIter.Current] = new Location(newPursuer.location);
        //                pIter.MoveNext();
        //            }
        //            else
        //            {
        //                int minY =  - areaRad + Math.Abs(x) - circularAxisMovement;
        //                int maxY =  + areaRad - Math.Abs(x) + circularAxisMovement;
        //                if(x == 0)
        //                {
        //                    minY = -areaRad;
        //                    maxY = areaRad;
        //                }

        //                int cycleLen = 4 * xAxisMovement + // sum of top and bottom edges
        //                               2 * (maxY - minY) // right and left edges
        //                               ; // overlapping points
        //                //int neededPursuers = cycleLen / (2 * circularAxisMovement + 1);//FIXME uncomment
        //                int neededPursuers = cycleLen -2;
        //                for(int i = 0; i < neededPursuers; ++i)
        //                {
                            
        //                    PursuerState newPursuer = new PursuerState()
        //                    {
        //                        dir = PursuerDirection.Right,
        //                        minY = minY,
        //                        maxY = maxY,
        //                        minX = x - xAxisMovement,
        //                        maxX = x + xAxisMovement
        //                    };
        //                    newPursuer.location =  new Point(newPursuer.minX,newPursuer.minY);
        //                    //newPursuer.advanceLocationInCycle(i * r_p); FIXME uncomment
        //                    newPursuer.advanceLocationInCycle(i, true);
        //                    m_pursuerCircularPath[pIter.Current] = newPursuer;
        //                    L[pIter.Current] = new Location(areaCenter.nodeLocation.add(newPursuer.location));
        //                    pIter.MoveNext();
        //                }
        //            }
        //        }
                

        //        //for (x = -r_p / 4; x >= -areaRad; x -= r_p / 2)
        //        //    for (int y = -areaRad - x; y <= x + areaRad; y += r_p + 1)
        //        //        setTwoSidesPursuers(x, y, r_p, areaCenter, areaRad, pIter, L, (x + r_p / 4 ) + areaRad);
                        
        //        //for(x = r_p/4; x < areaRad + r_p/4; x += r_p / 2)
        //        //    for (int y = -areaRad + x; y <= -areaRad + x + 2*(areaRad-x) + r_p + 1; y += r_p + 1)
        //        //        setTwoSidesPursuers(x, y, r_p, areaCenter, areaRad, pIter, L, -areaRad + x + 2 * (areaRad - x) + r_p + 1);
            
        //        while (pIter.CurrentIdx < pIter.End)
        //        {
        //            L[pIter.Current] = new Location(areaCenter.nodeLocation);
        //            m_pursuerCircularPath[pIter.Current] = new PursuerState() { minY = -1 }; // marks a pursuer that is unsed in the patrol
        //            pIter.MoveNext();
        //        }
        //    }
            
            

        //    public void advancePursuers(GridGameGraph graph, int r_p, 
        //                                ListRangeEnumerable<Pursuer> pursuers,
        //                                Dictionary<Pursuer, Location> L, Location v, int d)
        //    {
                
        //        Random rnd = new Random();

        //        int newMinPursuerX = rnd.Next(-m_xDiffBetweenPursuers, m_xDiffBetweenPursuers);
        //        int xJump = newMinPursuerX - m_currentMinPursuerX;
        //        int circJump = (r_p - Math.Abs(xJump));
        //        bool reverseDir = rnd.Next(0, 2) == 0;

        //        // FIXME: make sure we indeed cover everything
        //        foreach(Pursuer p in pursuers)
        //        {
        //            PursuerState currentPursuerState = m_pursuerCircularPath[p];
        //            if (currentPursuerState.minY == -1)
        //                continue;

        //            currentPursuerState.location.X += xJump; // fixme uncomment

        //            currentPursuerState.advanceLocationInCycle(circJump, reverseDir);
        //            #region comment
        //            /* while(remainingCircJump > 0)
        //            {
        //                switch(currentPursuerState.dir)
        //                {
        //                    case PursuerDirection.Down:
        //                    {
        //                        int diff = currentPursuerState.maxY - currentPursuerState.location.Y;
        //                        if(remainingCircJump <= diff)
        //                        {
        //                            currentPursuerState.location.Y += remainingCircJump ;
        //                            remainingCircJump = 0;
        //                        }
        //                        else
        //                        {   
        //                            currentPursuerState.location.Y += diff;
        //                            remainingCircJump -= diff;
        //                            currentPursuerState.dir = PursuerDirection.Left;
        //                        }
        //                        break;
        //                    }
        //                    case PursuerDirection.Left:
        //                    {
        //                        int diff = currentPursuerState.location.X - currentPursuerState.minX;
        //                        if (remainingCircJump <= diff)
        //                        {
        //                            currentPursuerState.location.X -= remainingCircJump;
        //                            remainingCircJump = 0;
        //                        }
        //                        else
        //                        {
        //                            currentPursuerState.location.X += diff;
        //                            remainingCircJump -= diff;
        //                            currentPursuerState.dir = PursuerDirection.Up;
        //                        }
        //                        break;
        //                    }
        //                    case PursuerDirection.Up:
        //                    {
        //                        int diff = currentPursuerState.location.Y - currentPursuerState.minY;
        //                        if(remainingCircJump <= diff)
        //                        {
        //                            currentPursuerState.location.Y -= remainingCircJump ;
        //                            remainingCircJump = 0;
        //                        }
        //                        else
        //                        {   
        //                            currentPursuerState.location.Y -= diff;
        //                            remainingCircJump -= diff;
        //                            currentPursuerState.dir = PursuerDirection.Right;
        //                        }
        //                        break;
        //                    }
        //                    case PursuerDirection.Right:
        //                    {
        //                        int diff = currentPursuerState.maxX - currentPursuerState.location.X;
        //                        if(remainingCircJump <= diff)
        //                        {
        //                            currentPursuerState.location.X += remainingCircJump ;
        //                            remainingCircJump = 0;
        //                        }
        //                        else
        //                        {   
        //                            currentPursuerState.location.X += diff;
        //                            remainingCircJump -= diff;
        //                            currentPursuerState.dir = PursuerDirection.Down;
        //                        }
        //                        break;
        //                    }
        //                } // switch dir
        //            } // while(remainingCircJump > 0) */
        //            #endregion

        //            L[p] = new Location(currentPursuerState.location);
        //            m_pursuerCircularPath[p] = currentPursuerState;
                
        //        } // foreach pursuer

        //        m_currentMinPursuerX = newMinPursuerX;
        //    }

        //    private void setTwoSidesPursuers(int x, int y, int r_p, Location v, int areaRad,
        //       ListRangeEnumerator<Pursuer> pIter,
        //       Dictionary<Pursuer, Location> L,
        //       int maxY) // serves Init
        //    {
        //        L[pIter.Current] = new Location(v.nodeLocation.add(x, y));

        //        m_pursuerCircularPath[pIter.Current] =
        //            new PursuerState()
        //            {
        //                dir = PursuerDirection.Up,
        //                minY = v.nodeLocation.Y - areaRad - x, // the minimal value used by the loop that calls setTwoSidesPursuers
        //                maxY = v.nodeLocation.Y + maxY,
        //                location = L[pIter.Current].nodeLocation,
        //                minX = v.nodeLocation.X + x,
        //                maxX = v.nodeLocation.X + x + r_p / 2
        //            };
        //        pIter.MoveNext();
                

        //        Point newPos = new Point(x + r_p / 2, y + r_p / 4 + 1);
        //        PursuerDirection dir = PursuerDirection.Down;
        //        if (newPos.Y > maxY)
        //            if (maxY - newPos.Y < r_p / 4)
        //            {
        //                // newPos is the "stiching point" between the two sides, and the pursuer on the other side is too far
        //                newPos.X -= newPos.Y - maxY;
        //                newPos.Y = maxY;
        //                dir = PursuerDirection.Left;
        //            }
        //            else
        //                return;

        //        L[pIter.Current] = new Location(v.nodeLocation.add(newPos));
        //        m_pursuerCircularPath[pIter.Current] =
        //            new PursuerState()
        //            {
        //                dir = dir,
        //                minY = v.nodeLocation.Y - areaRad - x, // the minimal value used by the loop that calls setTwoSidesPursuers
        //                maxY = v.nodeLocation.Y + maxY,
        //                location = L[pIter.Current].nodeLocation,
        //                minX = v.nodeLocation.X + x,
        //                maxX = v.nodeLocation.X + x + r_p / 2
        //            };
        //        pIter.MoveNext();
        //    }


            
        //    private int m_xDiffBetweenPursuers;
        //    private int m_currentMinPursuerX; // should always be between -m_xDiffBetweenPursuers and m_xDiffBetweenPursuers
            
        //    private enum PursuerDirection : byte
        //    {
        //        Down = 0,
        //        Left = 1,
        //        Up = 2,
        //        Right = 3
        //    }
        //    private struct PursuerState
        //    {
        //        public int minY;
        //        public int maxY;
        //        public int minX;
        //        public int maxX;
        //        public Point location;
        //        public PursuerDirection dir;

        //        // tells how many points comprise the cycle
        //        public int cycleLen()
        //        {
        //            return 2 * (maxY - minY) + 2 * (maxX - minX);
        //        }
        //        /// <summary>
        //        /// 
        //        /// </summary>
        //        /// <param name="remainingCircJump"></param>
        //        /// <param name="cw">
        //        /// tells if movement is clockwise or counterclockwise
        //        /// </param>
        //        public void advanceLocationInCycle(int remainingCircJump, bool cw)
        //        {
        //            // I just copy-pasted all cases. yes, it's horrible, but probably a more elegant code
        //            // would be slightly slower and will take a little longer to write anyway...
        //            if (!cw)
        //                remainingCircJump = cycleLen() - remainingCircJump;

        //            while (remainingCircJump > 0)
        //            {
        //                switch (dir)
        //                {
        //                    case PursuerDirection.Down:
        //                        {
        //                            int diff = maxY - location.Y;
        //                            if (remainingCircJump <= diff)
        //                            {
        //                                location.Y += remainingCircJump;
        //                                remainingCircJump = 0;
        //                            }
        //                            else
        //                            {
        //                                location.Y += diff;
        //                                remainingCircJump -= diff;
        //                                dir = PursuerDirection.Left;
        //                            }
        //                            break;
        //                        }
        //                    case PursuerDirection.Left:
        //                        {
        //                            int diff = location.X - minX;
        //                            if (remainingCircJump <= diff)
        //                            {
        //                                location.X -= remainingCircJump;
        //                                remainingCircJump = 0;
        //                            }
        //                            else
        //                            {
        //                                location.X -= diff;
        //                                remainingCircJump -= diff;
        //                                dir = PursuerDirection.Up;
        //                            }
        //                            break;
        //                        }
        //                    case PursuerDirection.Up:
        //                        {
        //                            int diff = location.Y - minY;
        //                            if (remainingCircJump <= diff)
        //                            {
        //                                location.Y -= remainingCircJump;
        //                                remainingCircJump = 0;
        //                            }
        //                            else
        //                            {
        //                                location.Y -= diff;
        //                                remainingCircJump -= diff;
        //                                dir = PursuerDirection.Right;
        //                            }
        //                            break;
        //                        }
        //                    case PursuerDirection.Right:
        //                        {
        //                            int diff = maxX - location.X;
        //                            if (remainingCircJump <= diff)
        //                            {
        //                                location.X += remainingCircJump;
        //                                remainingCircJump = 0;
        //                            }
        //                            else
        //                            {
        //                                location.X += diff;
        //                                remainingCircJump -= diff;
        //                                dir = PursuerDirection.Down;
        //                            }
        //                            break;
        //                        }
        //                } // switch dir
        //            } // while(remainingCircJump > 0)
                    
        //        }
        //    }
        //    private Dictionary<Pursuer, PursuerState> m_pursuerCircularPath = new Dictionary<Pursuer,PursuerState>();
        //}
        
        /// <summary>
        /// this is actually a bad alg, and should be removed.
        /// a better alternative would be having a pursuer in each r_p/4 X r_p/4 mini-area move independenly, and randomly.
        /// If we have extra pursuers, each mini-area will simply have more pursuers - that still move independently from each other (
        /// but move to another place each time)
        /// </summary>
        //static class UniformAreaPatrol
        //{
        //    /// <summary>
        //    /// sets a cluster of pursuers (Pursuer) around 'center', in L
        //    /// (details in paper)
        //    /// </summary>
        //    public static void SetPursuerGroup(GameGraph<Point> graph, Point lineStart, IEnumerable<Pursuer> pursuers, Dictionary<Pursuer, Location> L)
        //    {
        //        var j = pursuers.GetEnumerator();
        //        while (j.MoveNext())
        //        {
        //            L[j.Current] = new Location(lineStart);
        //            ++lineStart.Y;
        //            //++lineStart.X;
        //        }

        //    }


        //    /// <summary>
        //    /// sets initial positions for pursuers, for use of the uniform area patrol, using SetPursuerCluster()
        //    /// (details in paper, on function init() )
        //    /// </summary>
        //    /// <param name="L">
        //    /// L will be updated such that it contains clusters of pursuers, distributed uniformly
        //    /// in the area
        //    /// </param>
        //    /// <param name="v">
        //    /// center of area
        //    /// </param>
        //    /// <param name="d">
        //    /// radius of the area
        //    /// </param>
        //    /// <returns>
        //    /// the first pursuer (i.e. A_P[0] in the paper - the pursuer that gets the minimal x)
        //    /// </returns>
        //    public static Pursuer InitUniformAreaPatrol(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers,
        //                                      Dictionary<Pursuer, Location> L, Location v, int d)
        //    {
        //        r_p = r_p - (r_p % 2); // if r_p is not even, we round down

               
        //        int clustersCountPerRad = (int)Math.Ceiling(((float)d) / (1 + r_p / 2));


        //        int p_i = 0;
        //        /*
        //        int exD = (int)(r_p * Math.Ceiling((float)(d + r_p/2) / r_p));
        //        exD /= r_p;
        //        int p_c = pursuers.count() / (exD * exD);
        //        */
        //        int p_c = calculateP_C(r_p, d, pursuers.Count());

        //        if (p_c == 0)
        //        {
        //            int minPursuersCount = (2 * clustersCountPerRad * (2 * clustersCountPerRad + 1) + 1);
        //            throw new AlgorithmException("Not enough pursuers/pursuers too slow for InitUniformAreaPatrol()! (at least " + minPursuersCount.ToString() + " pursuers# are needed, with current velocity)");
        //        }
        //        /*
        //                    for (int x = 0; x < exD; ++x)
        //                        for (int y = 0; y < exD; ++y)
        //                        {
        //                            SetPursuerGroup(graph, new Point(v.nodeLocation.X + r_p * (x - y), v.nodeLocation.Y -d + (y+x) * r_p), new ListRangeEnumerable<Pursuer>(pursuers, p_i, p_i + p_c), L);
        //                            p_i += p_c;
        //                        }
        //        */

        //        for (int x = -d; x <= d + r_p / 2; x += (r_p / 2) + 1)
        //            for (int y = Math.Abs(x) - d; y <= d + r_p / 2 - Math.Abs(x); y += (r_p / 2) + 1)
        //            {
        //                // TODO:
        //                // this is actually not good - the pattern should set a straight line down\diagonally
        //                // from each (x,y). Instead, on the edges we want the line to go inwards, then as
        //                // (x,y) is further from the edges, the (x,y) is "more the center of the line".
        //                // our goals are:
        //                // 1) after advancePursuers() call, if (x,y) is within the sensitive area => so should the entire group be
        //                // 2) there should be no overlap between groups
        //                SetPursuerGroup(graph, new Point(v.nodeLocation.X + x, v.nodeLocation.Y + y), new ListRangeEnumerable<Pursuer>(pursuers, p_i, p_i + p_c), L);
        //                p_i += p_c;
        //            }
        //        // set remaining pursuers :
        //        //SetPursuerGroup(graph, new Point(v.nodeLocation.X, v.nodeLocation.Y), new ListRangeEnumerable<Pursuer>(pursuers, p_i, pursuers.count()), L);
        //        for (; p_i < pursuers.data.Count; ++p_i)
        //            L[pursuers.data[p_i]] = new Location(v.nodeLocation);



        //        firstPursuerInitialLocation = L[pursuers.data[0]].nodeLocation;
        //        randLocations = new List<Point>();
        //        int pointIndex = 0;
        //        for (int x = -r_p / 2; x <= r_p / 2; ++x) // r_p is even
        //            for (int y = -r_p / 2; y <= r_p / 2; ++y)
        //            {
        //                //if (x != y || Math.Abs(y) >= p_c) 


        //                // no point for a pursuer to move where another pursuer, in it's cluster/group, already stands.
        //                // clusteres are now a straight line "down", but in fact each line should be different, to insure that
        //                // 1) after advancePursuers() call, if (x,y) is within the sensitive area => so should the entire group be
        //                // 2) there should be no overlap between groups
        //                if (x != 0 || Math.Abs(y) >= p_c)
        //                    randLocations.Add(new Point(x, y));

        //            }


        //        return pursuers.data[pursuers.start];
        //    }



        //    //private static Point[] randLocations; // serves AdvanceUniformAreaPatrolPursuers
        //    private static List<Point> randLocations; // serves InitUniformAreaPatrol
        //    private static Point firstPursuerInitialLocation;// serves AdvanceUniformAreaPatrolPursuers

        //    /// <summary>
        //    /// updates locations in L of given pursuers, assuming they were previously 
        //    /// positioned using InitUniformAreaPatrol() and AdvanceUniformAreaPatrolPursuers() calls
        //    /// </summary>
        //    /// <param name="graph"></param>
        //    /// <param name="r_p"></param>
        //    /// <param name="pursuers"></param>
        //    /// <param name="L"></param>
        //    /// <param name="v">
        //    /// target
        //    /// </param>
        //    /// <param name="d"></param>
        //    /// <param name="round">
        //    /// the last round in which pursuers were positioned
        //    /// </param>
        //    /// <param name="firstPursuer">
        //    /// A_P[0] (in the paper) -  the pursuer returned by InitUniformAreaPatrol()
        //    /// </param>
        //    public static void AdvanceUniformAreaPatrolPursuers(GridGameGraph graph, int r_p,
        //        ListRangeEnumerable<Pursuer> pursuers,
        //        Dictionary<Pursuer, Location> L, Location v, int d, Pursuer firstPursuer)
        //    {

        //        Dictionary<Pursuer, Location> nextL = new Dictionary<Pursuer, Location>();

        //        r_p = r_p - (r_p % 2); // if r_p is not even, we round down

        //        //int exD = (int)(r_p * Math.Ceiling((float)(d + r_p / 2) / r_p));
        //        // exD /= r_p;

        //        //int p_c = pursuers.count() / (exD * exD);

        //        //if (randLocations == null || randLocations.Count() != 4 * r_p * r_p - p_c)
        //        //{
        //        //    // we assume this method will be used consequtively with the same exD, p_c, and
        //        //    // then this is probably the fastest solution
        //        //    randLocations = new Point[exD * exD - p_c];
        //        //    int pointIndex = 0;
        //        //    for (int x = -r_p ; x <= r_p; ++x) // r_p is even
        //        //        for (int y = Math.Abs(x) - r_p ; y <= r_p  - Math.Abs(x); ++y)
        //        //            if(x != y || Math.Abs(y) >= p_c)
        //        //                randLocations[pointIndex++] = new Point(x,y); 
        //        //}

        //        //  TODO: point/vector arithmetic is very cumbersome. find some existing library for vector math
        //        Point p0 = L[firstPursuer].nodeLocation;
        //        Random rand = new Random();
        //        Point diff =
        //            Utils.addPoints(
        //            Utils.subtructPoints(firstPursuerInitialLocation, p0),
        //            randLocations[rand.Next() % randLocations.Count()]);

        //        foreach (Pursuer p in pursuers)
        //            L[p] = new Location(new Point(L[p].nodeLocation.X + diff.X, L[p].nodeLocation.Y + diff.Y));

        //    }

        //    public static int calculateP_C(int r_p, int d, int pursuerCount)
        //    {


        //        int singleJump = (r_p / 2) + 1;
        //        int x1 = d / singleJump;
        //        int whenX2IsTooLarge = (2 * d + r_p / 4) / singleJump; //if   2 * (x2 *singleJump- d) > 2 * d + r_p / 2 holds, no Y iterations are done
        //        int x3 = Math.Min((2 * d + r_p / 2) / singleJump, whenX2IsTooLarge);
        //        int totalIterationsFormula =
        //            (x1 + 1) * (1 + (2 * d + r_p / 2 - 2 * d) / singleJump) + x1 * (x1 + 1);
        //        int part2IterCount = x3 - (x1 + 1) + 1;
        //        totalIterationsFormula +=
        //            part2IterCount * (1 + (2 * d + r_p / 2 + 2 * d) / singleJump) -
        //            (part2IterCount * ((x1 + 1) * 2 + (part2IterCount - 1)));

        //        return pursuerCount / totalIterationsFormula;
        //    }

        //}
        
        // two layers of patrol
        
        /// <summary>
        /// it's impossible to spread pursuers sparsely, since when they reach an edge and then *coordinately*
        /// have to go to some direction, then half of the *entire* area is unreachable (not only the edges)
        /// </summary>
        //class SparseAreaPatrol : IPatrol<SparseAreaPatrol>
        //{
        //    static int rollingRandomizer = 0;
        //    Random rand = new Random((int)DateTime.Now.Ticks + (rollingRandomizer++));

        //    List<Pursuer> densePursuers = new List<Pursuer>();
        //    Point currentDenseOffset = new Point(0, 0);

        //    List<Pursuer> sparsePursuers = new List<Pursuer>();
        //    Point currentSparseOffset = new Point(0, 0);
                              
        //    static int captureProbability(int maxAvilableAgents, int r_p, int d)
        //    {
        //        throw new Exception("not implemented");
        //    }

        //    static int getUsedAgents(int maxAvilableAgents, int r_p, int d)
        //    {
        //        throw new Exception("not implemented");
        //    }

        //    List<Point> getGridPoints(bool skipFirst, bool skipLast, int r_p, int d, Point startPoint, int xDir, int yDir)
        //    {
        //        List<Point> res = new List<Point>();

        //        int iterCount = d / (r_p ) + 1;
        //        Point processedPoint = startPoint;

        //        if (!skipFirst)
        //            res.Add(processedPoint);
        //        if (skipLast)
        //            iterCount--;
        //        for (int i = 1; i < iterCount; ++i)
        //        {
        //            processedPoint = processedPoint.add(xDir * (r_p ), yDir * (r_p ));
        //            res.Add(processedPoint);
        //        }
        //        return res;
        //    }
        //    public void Init(GridGameGraph graph, int r_p,
        //              ListRangeEnumerable<Pursuer> pursuers,
        //              Dictionary<Pursuer, Location> L, Location v, int d)
        //    {
        //        r_p -= r_p % 2;
        //        int y = v.nodeLocation.Y - d + r_p / 2 - 1;
        //        int x = v.nodeLocation.X;

        //        var pIter = pursuers.GetEnumerator();
        //        pIter.MoveNext();
                
        //        //int iterCount = d / (r_p / 2) + 1;
        //        //if (iterCount * (r_p / 2) < (d - (r_p / 4 - 1) )) // check whether the reachable area (r_p/4 + 1) covers the larger area
        //          //  ++iterCount;

        //        // run until the reachable area of the last added point already covers the large area
        //        //for(int i = 0; i < iterCount; ++i) 
        //        //{
        //        //    L[pIter.Current] = new Location(new Point(x, y));
        //        //    pIter.MoveNext();
        //        //    x -= r_p / 2;
        //        //    y += r_p / 2;
        //        //}
        //        List<Point> sparsePursuerPoints =
        //            getGridPoints(false, false, r_p / 2, d, new Point(v.nodeLocation.X, v.nodeLocation.Y - d + r_p / 2 - 1), -1, 1);
        //        sparsePursuerPoints.AddRange(
        //            getGridPoints(true, false, r_p / 2, d, new Point(v.nodeLocation.X, v.nodeLocation.Y - d + r_p / 2 - 1), 1, 1));
        //        sparsePursuerPoints.AddRange(
        //            getGridPoints(false, true, r_p / 2, d, new Point(v.nodeLocation.X, v.nodeLocation.Y + d - r_p / 2 + 1), -1, -1));
        //        sparsePursuerPoints.AddRange(
        //            getGridPoints(true, true, r_p / 2, d, new Point(v.nodeLocation.X, v.nodeLocation.Y + d - r_p / 2 + 1), 1, -1));

        //        foreach(Point p in sparsePursuerPoints)
        //        {
        //            L[pIter.Current] = new Location(p);
        //            densePursuers.Add(pIter.Current);
        //            pIter.MoveNext();
        //        }

        //        List<Point> densePursuerPoints = new List<Point>();


        //    }

        //    public void advancePursuers(GridGameGraph graph, int r_p,
        //                            ListRangeEnumerable<Pursuer> pursuers,
        //                            Dictionary<Pursuer, Location> L, Location v, int d)
        //    {

        //    }


        //}


        
        /// <summary>
        /// Note about clusters:
        /// Clustering is dicouraged by default because it causes a problem - suppose we normally don't a stiching point,
        /// but since the groups are large, their movement must be smaller (so they don't jump one over the other) - and then 
        /// since velocity is lower, we actually DO need a stiching point! (which again, turns the size of the groups - it's a cycle in dependencies)
        /// 
        /// The only advantage in clustering is when multiples patrols work in sync to maximize undirty area
        /// </summary>
        public class CircumferencePatrol
        {
            
            Random rand = new ThreadSafeRandom().rand;

            List<Pursuer> unusedPursuers = new List<Pursuer>();
            List<List<Pursuer>> pursuersPerJump = new List<List<Pursuer>>();
            List<int> availableTargetIndices = new List<int>(); // regards the first group within the first r_p+_1 points (all other pursuers follow)
            List<int> currentlyUsedTargetIndices = new List<int>();

            int minP;
            int groupSize;
            int distBetweenPursuers;
            int realD;

            // in some cases, we don't want all pursuers to utilize r_p, and isntead have each group keep a distance
            // of (r_p - 1) + 1 from each other, instead of (r_p) + 1
            private static int getGroupDistance(int r_p, int d, int maxPursuersCount)
            {
                r_p = r_p - (r_p % 2);

                if ((4 * d) % (1 + r_p) == 0 && 
                    (4 * d) % (r_p) != 0)
                    return 1 + r_p; // if one option doesn't divide well, then it's inferior (at least until we define the improved algorithm)

                if ((4 * d) % (r_p) == 0)
                    return r_p;

                // if both options divide well, we want to minimize the amount of unused pursuers
                int evenGroupCount = (4 * d) / (1 + r_p);
                int unevenGroupCount = (4 * d) / (r_p);

                if (unevenGroupCount > maxPursuersCount)
                    return 1 + r_p; // this doesn't mean that also evenGroupCount>maxPursuersCount, but in case that it is, we want larger distance

                return ((maxPursuersCount % evenGroupCount) > (maxPursuersCount % unevenGroupCount))?
                    (r_p) : (1 + r_p);
            }

            static int getBestFitRadius(int r_p, int d)
            {
                return d;
                // find the minimal d for groups match perfectly
                // TODO: ridiculously inefficient
                while (true)
                {
                    if ((4 * d % (1 + r_p)) == 0)
                        return d;
                    ++d;
                }
            }
   
            public void resetPursuers(GridGameGraph graph, int r_p,
                                    ListRangeEnumerable<Pursuer> pursuers,
                                    Dictionary<Pursuer, Location> L, Location v, int d)
            {

            }
            /// <summary>
            /// calculates p_c in the paper
            /// </summary>
            /// <returns></returns>
            private static bool extraStichingPoint(int r_p, int d)
            {
                r_p -= r_p % 2;
                
                int firstPursuerDiffToAreaEdge = (2 * d ) % r_p;
                return (firstPursuerDiffToAreaEdge == 0) ||
                       (firstPursuerDiffToAreaEdge > (r_p / 2));
            }


            public virtual int getGroupSize(int r_p, int d, int maxPursuersCount)
            {
                r_p = r_p - (r_p % 2);

                int groupDistance = getGroupDistance(r_p, d, maxPursuersCount);
                    

                int perfectMatchCircumference = 4 * d - (4 * d % (groupDistance));

                float circumferenceRatio = ((float)perfectMatchCircumference) / (4 * d);
                int perfectCircumferencePursuersCount = (int)(maxPursuersCount * circumferenceRatio);

                int perfectCircumferenceChunkCount = perfectMatchCircumference / (groupDistance);

                int usedPursuersInPerfectCircumference =
                    perfectCircumferencePursuersCount - (perfectCircumferencePursuersCount % perfectCircumferenceChunkCount);

                return usedPursuersInPerfectCircumference / perfectCircumferenceChunkCount; 


                //d = getBestFitRadius(r_p, d);
               // return getUsedPursuersCount(r_p, d, maxPursuersCount) / minimalPursuersCount(r_p, d);
            }
            public virtual double getCaptureProbability(int r_p, int d, int maxPursuersCount)
            {
                d = getBestFitRadius(r_p, d);
                int minimal = minimalPursuersCount(r_p, d);

                if (minimal > maxPursuersCount || maxPursuersCount == 0)
                    return 0;

                int groupDistance = getGroupDistance(r_p, d, maxPursuersCount);

                double used = getUsedPursuersCount(r_p, d, maxPursuersCount);
                
                r_p -= r_p % 2;
                int groupSize = getGroupSize(r_p, d, maxPursuersCount);

                //if ((4 * d) % (1 + r_p) != 0)
                //{
                //    return 0; // TODO: solving overlaps is VERY difficult. using clustered pursuer groups causes uneven visiting frequencies distributions,
                //    // and using separate pursuer group (each group of size 'minimalPursuersCount') will eventually be out of sync because the sticthing
                //    // group isn't really part of the group - and can't satisfy the cycle constraint in both direction, unless that group is enlarged
                //}

                if ((4 * d) % (groupDistance) == 0)
                    return ((float)groupSize) / (groupDistance - groupSize);
                
                // if there is a stiching group, then the pursuers might get unsynchronized and sometimes visit points that were already visited
                //return (used - groupSize) / (4 * d - used + groupSize);
                //return (used ) / (4 * d - used + groupSize);
                return 0;


                int groupSizeOverlap = 0;

                if ((4 * d) % (groupDistance) != 0)
                {

                    int lastGroupStartPoint = 4 * d - (4 * d % (groupDistance));

                    if (lastGroupStartPoint + groupSize > 4 * d)
                        lastGroupStartPoint = 4 * d - groupSize;


                    int optimalStartPoint = (4 * d - groupDistance);


                    //int groupSizeOverlap = Math.Min(groupSize, 2 * Math.Abs(optimalStartPoint - lastGroupStartPoint));
                    groupSizeOverlap = Math.Min(groupSize, 2 * Math.Abs(optimalStartPoint - lastGroupStartPoint));
                }

                
                // sometimes,  the points in the stiching group almost never visit a point unvisited in the previous round , and therfore shouldn't
                // be considered as one of the "used" pursuers
                return (used-groupSizeOverlap) / (4.0 * d - used + groupSizeOverlap);
            }
            public virtual int minimalPursuersCount(int r_p, int d)
            {
                r_p = r_p - (r_p % 2);
                //d = getBestFitRadius(r_p, d);
                if (r_p == 0)
                    return 0;
                
                return Math.Max(1, (int)Math.Ceiling((4.0 * d) / (r_p+1)));
            }
            public virtual int getUsedPursuersCount(int r_p, int d, int pursuersCount)
            {
                r_p = r_p - (r_p % 2);

                int minimalPursuers = minimalPursuersCount(r_p, d);

                if (minimalPursuers == 0 || pursuersCount < minimalPursuers)
                    return 0;

                int groupDistance = getGroupDistance(r_p, d, pursuersCount);

                int perfectMatchCircumference = 4 * d - (4 * d % (groupDistance));

                float circumferenceRatio = ((float)perfectMatchCircumference) / (4 * d);
                int perfectCircumferencePursuersCount = (int)(pursuersCount * circumferenceRatio);

                int perfectCircumferenceChunkCount = perfectMatchCircumference / (groupDistance);

                int usedPursuersInPerfectCircumference =
                    perfectCircumferencePursuersCount - (perfectCircumferencePursuersCount % perfectCircumferenceChunkCount);

                int groupSize = usedPursuersInPerfectCircumference / perfectCircumferenceChunkCount; 

                int pursuersInCircumferenceRemainder = Math.Min(4 * d - perfectMatchCircumference, groupSize);

                return pursuersInCircumferenceRemainder + usedPursuersInPerfectCircumference;

                //return pursuersCount - (pursuersCount % minimalPursuers);
                
            }
            private static int getLastGroupPointCount(int r_p, int d, int pursuersCount)
            {
                r_p = r_p - (r_p % 2);

                int groupDistance = getGroupDistance(r_p, d, pursuersCount);

                return (4 * d % (groupDistance));
            }
            private static int getLastGroupPursuersCount(int r_p, int d, int pursuersCount)
            {
                r_p = r_p - (r_p % 2);

                int groupDistance = getGroupDistance(r_p, d, pursuersCount);

                int perfectMatchCircumference = 4 * d - (4 * d % (groupDistance));

                float circumferenceRatio = ((float)perfectMatchCircumference) / 4 * d;
                int perfectCircumferencePursuersCount = (int)(pursuersCount * circumferenceRatio);

                int usedPursuersInPerfectCircumference =
                    perfectCircumferencePursuersCount - (perfectCircumferencePursuersCount % (groupDistance));

                int perfectCircumferenceChunkCount = perfectMatchCircumference / (groupDistance);

                //float density = usedPursuersInPerfectCircumference / perfectMatchCircumference;

                int groupSize = usedPursuersInPerfectCircumference / perfectCircumferenceChunkCount; //(int)(density * (groupDistance));

                int pursuersInCircumferenceRemainder = Math.Min(4 * d - perfectMatchCircumference, groupSize);

                return pursuersInCircumferenceRemainder;
            }
            
            public virtual void Init(GridGameGraph graph, int r_p, 
                                    ListRangeEnumerable<Pursuer> pursuers,
                                    Dictionary<Pursuer, Location> L, Location v, int d)
            {
                int groupDistance = getGroupDistance(r_p, d, pursuers.Count());

                // TODO:
                // commented code below (until the commented "return;") is the still incomplete algorithm for letting pursuers 
                // move randomly, then "steal" pursuers from either directions. last stiching group either keeps
                // a compressed version of the pattern that repeats in all other groups, or just populated with all pursuers, 
                // in case the points count of that group is smaller then the amount of pursuers in each group.
                // Additionally, another thing unimplemented - using distance of r_p instead of r_p+1 between each two groups may sometimes
                // eliminate the stiching group entirely. There is a coresponding commented code in advancePursuers()

                //r_p = r_p - (r_p % 2);
                //groupSize = getGroupSize(r_p, d, pursuers.Count());
                //minP = minimalPursuersCount(r_p, d);
                //if (minP > pursuers.Count())
                //    throw new AlgorithmException("not enough pursuers (or too slow) for circumference alg.!");

                //ListRangeEnumerator<Pursuer> pIter = (ListRangeEnumerator<Pursuer>)pursuers.GetEnumerator();

                //int groupDistance = getGroupDistance(r_p, d, pursuersCount);
                //float pursuerJumpAngle = 4.0f * 1 / (4 * d);
                //float r_pAngleJump = 4.0f * (groupDistance) / (4 * d); // there is a gap of r_p BETWEEN each teo pursuers
                //float groupSizeAngleJump = 4.0f * groupSize / (4 * d);
                //float currentAngle = 0;
                //int pursuerIdx = 0;

                //while (currentAngle <= 4)
                //{
                //    for (int i = 0; i < groupSize; ++i)
                //    {
                //        if (pIter.MoveNext())
                //        {
                //            L[pIter.Current] = new Location(v.nodeLocation.add(Utils.getGridPointByAngle(d, currentAngle)));
                //            pursuersPerJump[i].Add(pIter.Current);
                //            ++pursuerIdx;
                //        }
                //        else
                //            break;
                //        currentAngle += pursuerJumpAngle;
                //    }
                //    currentAngle += (r_pAngleJump - groupSizeAngleJump);
                //}

                //for (int i = groupSize; i <= r_p; ++i)
                //    availableTargetIndices.Add(i);

                //for (int i = 0; i < groupSize; ++i)
                //    currentlyUsedTargetIndices.Add(i);



                //while (pIter.MoveNext())
                //{
                //    L[pIter.Current] = new Location(v.nodeLocation);
                //    unusedPursuers.Add(pIter.Current);
                //}

                //return;
                d = realD = getBestFitRadius(r_p, d);

                r_p = r_p - (r_p % 2);
                minP = minimalPursuersCount(r_p, d);
                if (minP > pursuers.Count())
                    throw new AlgorithmException("not enough pursuers (or too slow) for circumference alg.!");



                groupSize = getGroupSize(r_p,d,pursuers.Count());

                //int maxJump = distBetweenPursuers - groupSize; // groups will jump between 'groupSize' to 'maxJump' each round

                // if( 
                //     (!extraStichingPoint(r_p,d) && maxJump < (2 * d) % r_p) )
                // {
                //     if(pursuers.Count() - getUsedPursuersCount(r_p, )
                // }
                //distBetweenPursuers = (int)Math.Ceiling(4.0 * d / minP); // tells dist between first pursuer in each group


                int x = -d;

                ListRangeEnumerator<Pursuer> pIter = (ListRangeEnumerator<Pursuer>)pursuers.GetEnumerator();

                float pursuerJumpAngle = 4.0f * 1 / (4 * d);

                float r_pAngleJump = 4.0f * (groupDistance) / (4 * d); // there is a gap of r_p BETWEEN each teo pursuers
                float groupSizeAngleJump = 4.0f * groupSize / (4 * d);

                float currentAngle = 0;
                pursuersPerJump = new List<List<Pursuer>>(groupSize);
                for (int i = 0; i < groupSize; ++i)
                    pursuersPerJump.Add(new List<Pursuer>(minP));

                int pursuerIdx = 0;
                while (currentAngle <= 4)
                {
                    if (currentAngle > 4 - groupSizeAngleJump)
                        currentAngle = 4 - groupSizeAngleJump;

                    for (int i = 0; i < groupSize; ++i)
                    {
                        if (pIter.MoveNext())
                        {
                            L[pIter.Current] = new Location(v.nodeLocation.add(GameLogic.Utils.getGridPointByAngle(d, currentAngle)));
                            pursuersPerJump[i].Add(pIter.Current);
                            ++pursuerIdx;
                        }
                        currentAngle += pursuerJumpAngle;
                    }
                    currentAngle += (r_pAngleJump - groupSizeAngleJump);
                }

                //int maxJump = groupDistance - 2 * groupSize;
                for (int i = groupSize; i <= groupDistance - 1; ++i)
                    availableTargetIndices.Add(i);
                for (int i = 0; i < groupSize; ++i)
                    currentlyUsedTargetIndices.Add(i);

                while (pIter.MoveNext())
                {
                    L[pIter.Current] = new Location(v.nodeLocation);
                    unusedPursuers.Add(pIter.Current);
                }

                return;

                //int k = 0;

                //for (k = x; k < x + groupSize; ++k)
                //{
                //    pIter.MoveNext();
                //    L[pIter.Current] = new Location(new Point(v.nodeLocation.X + k, v.nodeLocation.Y - (d + k)));
                //}

                //if(minP != 2) // for minp==2, we use first point + stiching point
                //for (x = -d + r_p; x < d; x += r_p)
                //{
                //    for (k = x; k < x + groupSize; ++k)
                //    {
                //        int yDir = 1;
                //        if (k > 0)
                //            yDir = -1;

                //        pIter.MoveNext();

                //        int xFix = k;
                //        if (k > d)
                //            xFix = d - (k - d);
                //        L[pIter.Current] = new Location(new Point(v.nodeLocation.X + xFix, v.nodeLocation.Y - (d + k * yDir)));


                //        int xDiff = x - (k - x);
                //        pIter.MoveNext();
                //        L[pIter.Current] = new Location(new Point(v.nodeLocation.X + xDiff, v.nodeLocation.Y + d - Math.Abs(xDiff)));

                //    }
                //}

                //// x = d is the "stiching point", so somtimes we need to add another point, and sometimes the existing pursuers are already close enough
                //if (extraStichingPoint(r_p, d) && minP > 1 || minP == 2) // if minP == 1, then stiching point is irrelevant, if minP == 2, then only first and stiching points are used
                //    for (k = d; k < d + groupSize; ++k)
                //    {
                //        pIter.MoveNext();
                //        L[pIter.Current] = new Location(new Point(v.nodeLocation.X + 2 * d - k, v.nodeLocation.Y + k - d));
                //    }

                //while (pIter.MoveNext())
                //{
                //    L[pIter.Current] = new Location(v.nodeLocation);
                //    unusedPursuers.Add(pIter.Current);
                //}
                    
            }

            
            private static Point movePoint(Point pToMove, int jump, Point center, int d)
            {
               
                // we let all pursuers move "clockwise" or "counter clockwise"

                int xRight = -1; // tells if going clockwise makes x increase or decrease
                int yDown = 1; // ditto, but for y
                if ((pToMove.Y < center.Y) || (pToMove.Y == center.Y && center.X - d == pToMove.X))
                    xRight = 1;
                if ((pToMove.X < center.X) || (pToMove.X == center.X && center.Y - d == pToMove.Y))
                    yDown = -1;

                Point dest = pToMove.add(xRight * jump, yDown * jump);

                // now, we make sure the pursuer didn't get outside the circumference (and fix as needed)
                while ((dest.X < center.X - d || dest.X > center.X + d) ||
                       (dest.Y < center.Y - d || dest.Y > center.Y + d))
                {
                    // we use while, since for very big jumps we need more than one fix
                    if (dest.X < center.X - d)
                        dest.X += 2 * ((center.X - d) - dest.X);
                    else if (dest.X > center.X + d)
                        dest.X -= 2 * (dest.X - (center.X + d));

                    if (dest.Y < center.Y - d)
                        dest.Y += 2 * ((center.Y - d) - dest.Y);
                    else if (dest.Y > center.Y + d)
                        dest.Y -= 2 * (dest.Y - (center.Y + d));
                }
                return dest;
            }
            public virtual void AdvancePursuers(GridGameGraph graph, int r_p, 
                                    ListRangeEnumerable<Pursuer> pursuers,
                                    Dictionary<Pursuer, Location> L, Location v, int d)
            {
                int groupDistance = getGroupDistance(r_p, d, pursuers.Count());

                // TODO:
                // commented code below (until the commented "return;") is the still incomplete algorithm for letting pursuers 
                // move randomly, then "steal" pursuers from either directions. last stiching group either keeps
                // a compressed version of the pattern that repeats in all other groups, or just populated with all pursuers, 
                // in case the points count of that group is smaller then the amount of pursuers in each group.
                // Additionally, another thing unimplemented - using distance of r_p instead of r_p+1 between each two groups may sometimes
                // eliminate the stiching group entirely

                //List<int> nextCurrentlyUsedTargetIndices = new List<int>();
                //List<int> nextAvailableTargetIndices = new List<int>();
                //for (int i = 0; i <= r_p; ++i)
                //    nextAvailableTargetIndices.Add(i);

                //List<int> indices = new List<int>(groupSize);
                //for (int j = 0; j < groupSize; ++j)
                //{
                //    int targetIndex = availableTargetIndices.popRandomItem(rand);
                //    indices.Add(targetIndex);
                //    nextAvailableTargetIndices[targetIndex] = -1;
                //    //nextCurrentlyUsedTargetIndices.Add(targetIndex);
                //}
                //indices.Sort();

                //for (int i = nextAvailableTargetIndices.Count() - 1; i >= 0; --i)
                //    if (nextAvailableTargetIndices[i] == -1)
                //    {
                //        // we don't mind messing the order of the list, since it is used in random anyway
                //        nextAvailableTargetIndices[i] = nextAvailableTargetIndices.Last();
                //        nextAvailableTargetIndices.RemoveAt(nextAvailableTargetIndices.Count - 1);
                //    }
                //availableTargetIndices = nextAvailableTargetIndices;
                //currentlyUsedTargetIndices = indices;// nextCurrentlyUsedTargetIndices;

                //List<Point> pursuerPoints = new List<Point>();
                //foreach(Pursuer p in pursuers)
                //{
                //    if(L[p].nodeLocation == v.nodeLocation)
                //        continue; // we ignore pursuers in the center
                //    pursuerPoints.Add(L[p].nodeLocation);
                //}
                //List<GoE.GameLogic.Utils.SortedPointIdx> sortedPursuers = Utils.sortPointIndicesByAngles(pursuerPoints, v.nodeLocation);

                //bool drawFromFollowingPursuers = true;
                //int drawnPursuersCount = 0;
                //for (int i = 0; i < groupSize; ++i ) // try to match the new target indices with the first groupSize pursuers
                //{
                //    int jump = indices[i] - currentlyUsedTargetIndices[i];
                //    if(jump > r_p / 2)
                //    {
                //        drawFromFollowingPursuers = true;
                //        ++drawnPursuersCount;
                //    }
                //    if (jump < (-r_p / 2))
                //    {
                //        drawFromFollowingPursuers = false;
                //        ++drawnPursuersCount;
                //    }
                //}

                //// TODO: remove sanity checks:
                //if(drawFromFollowingPursuers)
                //{
                //    for (int i = 0; i < groupSize; ++i )
                //    {
                //        int jump = indices[i] - currentlyUsedTargetIndices[i];
                //        if(jump < -r_p/2)
                //        {
                //            int a = 0;
                //        }
                //    }
                //}
                //if(!drawFromFollowingPursuers)
                //{
                //    for (int i = 0; i < groupSize; ++i )
                //    {
                //        int jump = indices[i] - currentlyUsedTargetIndices[i];
                //        if (jump > r_p / 2)
                //        {
                //            int a = 0;
                //        }
                //    }
                //}

                //int minp = minimalPursuersCount(r_p,d);
                //int used = getUsedPursuersCount(r_p, d, pursuerPoints.Count);
                //int completeGroupsCount = minp-1;
                //if (used == minp * groupSize)
                //    ++completeGroupsCount;
                //if (drawFromFollowingPursuers)
                //{

                //    for (int i = 0; i < completeGroupsCount; ++i)
                //    {
                //        for(int j = i * groupSize; j < (1+i) * groupSize; ++j)
                //        {
                //            int idxInGroup = j - i * groupSize;
                //            int jump = indices[idxInGroup] - currentlyUsedTargetIndices[idxInGroup];
                //            int movingPursuerIndex = sortedPursuers[(j - drawnPursuersCount + used) % used].pointIndex;
                //            L[pursuers[movingPursuerIndex]] = 
                //                new Location(movePoint(L[pursuers[movingPursuerIndex]].nodeLocation, jump, v.nodeLocation, d));
                //        }
                //    }
                //    if(completeGroupsCount == minp-1)
                //    {
                //        // there was another group of pursuers, that has an area smaller than r_p+1 point count,
                //        // but has min(groupSize, point count) pursuers
                //        int lastGroupPointCount = getLastGroupPointCount(r_p, d, used); // make sure used and pursuers.count give same result
                //        int lastGroupPursuersCount = getLastGroupPursuersCount(r_p, d, used);
                //        if (lastGroupPursuersCount == groupSize)
                //        {
                //            float compressRatio = ((float)lastGroupPointCount) / groupSize;
                //            for (int j = used - lastGroupPointCount; j < used; ++j)
                //            {
                //                int idxInGroup = j - completeGroupsCount * groupSize;
                //                int jump = (int)Math.Round(compressRatio * (indices[idxInGroup] - currentlyUsedTargetIndices[idxInGroup]));
                //                int movingPursuerIndex = sortedPursuers[(j - drawnPursuersCount + used) % used].pointIndex;
                //                L[pursuers[movingPursuerIndex]] =
                //                    new Location(movePoint(L[pursuers[movingPursuerIndex]].nodeLocation, jump, v.nodeLocation, d));
                //            }
                //        }
                //        else
                //        {
                //            // if points count is so small, then the area is so compressed - that all points must have a pursuer
                //        }
                        
                //    }
                //}

                //    foreach (Pursuer p in unusedPursuers)
                //        L[p] = new Location(v.nodeLocation);

                //return;
                int maxJump = groupDistance - 2 * groupSize;
                d = realD;

                r_p = r_p - (r_p % 2);

                List<int> nextCurrentlyUsedTargetIndices = new List<int>();
                List<int> nextAvailableTargetIndices = new List<int>();
                for (int i = 0; i <= groupDistance-1; ++i)
                    nextAvailableTargetIndices.Add(i);

                List<int> indices = new List<int>(groupSize);
                for (int j = 0; j < groupSize; ++j)
                    indices.Add(availableTargetIndices.popRandomItem(rand));
                //indices.Sort(); // we must insure jump groups remain sorted the same way, since if they get mixed, the pursuers in the stuching group
                // may overlap other pursuers

                // we prevent pursuers from stiching group from falling on the same point by letting them get "dragged" by their own jump group.
                // therefore, they won't collide with other agents in the group

                float angleJump = 1.0f / d;
                for (int j = 0; j < groupSize; ++j)
                {
                    int targetIndex = indices[j];
                    int jump = targetIndex - currentlyUsedTargetIndices[j];
                    if (jump > r_p / 2)
                        jump -= groupDistance;
                    else if (jump < -r_p / 2)
                        jump += groupDistance;

                    nextCurrentlyUsedTargetIndices.Add(targetIndex);
                    nextAvailableTargetIndices[targetIndex] = -1;
                    foreach (Pursuer p in pursuersPerJump[j])
                    {
                        Point pLoc = L[p].nodeLocation;
                        // we let all pursuers move "clockwise" or "counter clockwise"

                        if (pLoc == v.nodeLocation)
                            break; // this and the following pursuers are at the center, and are unused pursuers

                        //float currentAngle = GameLogic.Utils.getAngleOfGridPoint(pLoc.subtruct(v.nodeLocation));
                        //currentAngle += angleJump * jump;
                        //Point dest = v.nodeLocation.add(GameLogic.Utils.getGridPointByAngle(d, currentAngle));

                        int xRight = -1; // tells if going clockwise makes x increase or decrease
                        int yDown = 1; // ditto, but for y
                        if ((pLoc.Y < v.nodeLocation.Y) || (pLoc.Y == v.nodeLocation.Y && v.nodeLocation.X - d == pLoc.X))
                            xRight = 1;
                        if ((pLoc.X < v.nodeLocation.X) || (pLoc.X == v.nodeLocation.X && v.nodeLocation.Y - d == pLoc.Y))
                            yDown = -1;

                        Point dest = pLoc.add(xRight * jump, yDown * jump);

                        // now, we make sure the pursuer didn't get outside the circumference (and fix as needed)
                        while ((dest.X < v.nodeLocation.X - d || dest.X > v.nodeLocation.X + d) ||
                               (dest.Y < v.nodeLocation.Y - d || dest.Y > v.nodeLocation.Y + d))
                        {
                            // we use while, since for very big jumps we need more than one fix
                            if (dest.X < v.nodeLocation.X - d)
                                dest.X += 2 * ((v.nodeLocation.X - d) - dest.X);
                            else if (dest.X > v.nodeLocation.X + d)
                                dest.X -= 2 * (dest.X - (v.nodeLocation.X + d));

                            if (dest.Y < v.nodeLocation.Y - d)
                                dest.Y += 2 * ((v.nodeLocation.Y - d) - dest.Y);
                            else if (dest.Y > v.nodeLocation.Y + d)
                                dest.Y -= 2 * (dest.Y - (v.nodeLocation.Y + d));
                        }
                        L[p] = new Location(dest);
                    }
                }
                currentlyUsedTargetIndices = nextCurrentlyUsedTargetIndices;

                for (int i = nextAvailableTargetIndices.Count() - 1; i >= 0; --i)
                    if (nextAvailableTargetIndices[i] == -1)
                    {
                        // we don't mind messing the order of the list, since it is used in random anyway
                        nextAvailableTargetIndices[i] = nextAvailableTargetIndices.Last();
                        nextAvailableTargetIndices.RemoveAt(nextAvailableTargetIndices.Count - 1);
                    }
                availableTargetIndices = nextAvailableTargetIndices;


                foreach (Pursuer p in unusedPursuers)
                    L[p] = new Location(v.nodeLocation);

                return;


                //r_p = r_p - (r_p % 2);

                //int jump, maxJump;


                //maxJump = r_p - 2 * groupSize + 1;
                //if (maxJump > 0)
                //    jump = groupSize + rand.Next(0, maxJump + 1); // imagine groupSize = 1. jumping 0 or r_p/2 is equivalant to no jump at all
                //else
                //    jump = groupSize;

                //if (jump > r_p / 2)
                //    jump = -(2 * groupSize + maxJump - jump);
                ////jump *= (rand.Next(0, 2) * 2 - 1); // mult by -1 or 1

                //foreach (Pursuer p in pursuers)
                //{

                //    Point pLoc = L[p].nodeLocation;
                //    // we let all pursuers move "clockwise" or "counter clockwise"

                //    if (pLoc == v.nodeLocation)
                //        break; // this and the following pursuers are at the center, and are unused pursuers

                //    int xRight = -1; // tells if going clockwise makes x increase or decrease
                //    int yDown = 1; // ditto, but for y
                //    if ((pLoc.Y < v.nodeLocation.Y) || (pLoc.Y == v.nodeLocation.Y && v.nodeLocation.X - d == pLoc.X))
                //        xRight = 1;
                //    if ((pLoc.X < v.nodeLocation.X) || (pLoc.X == v.nodeLocation.X && v.nodeLocation.Y - d == pLoc.Y))
                //        yDown = -1;

                //    Point dest = pLoc.add(xRight * jump, yDown * jump);

                //    // now, we make sure the pursuer didn't get outside the circumference (and fix as needed)
                //    while ((dest.X < v.nodeLocation.X - d || dest.X > v.nodeLocation.X + d) ||
                //           (dest.Y < v.nodeLocation.Y - d || dest.Y > v.nodeLocation.Y + d))
                //    {
                //        // we use while, since for very big jumps we need more than one fix
                //        if (dest.X < v.nodeLocation.X - d)
                //            dest.X += 2 * ((v.nodeLocation.X - d) - dest.X);
                //        else if (dest.X > v.nodeLocation.X + d)
                //            dest.X -= 2 * (dest.X - (v.nodeLocation.X + d));

                //        if (dest.Y < v.nodeLocation.Y - d)
                //            dest.Y += 2 * ((v.nodeLocation.Y - d) - dest.Y);
                //        else if (dest.Y > v.nodeLocation.Y + d)
                //            dest.Y -= 2 * (dest.Y - (v.nodeLocation.Y + d));
                //    }
                //    L[p] = new Location(dest);

                //}
            }
        }
        /// <summary>
        /// divides the area as a grid, gives each 2 pursuer an area of r_p to each direction,
        /// and lets them switch roles - one stays in the center, and one jumps somewhere in the area
        /// </summary>
        public class SwitchPatrol : APatrol
        {
            static int rollingRandomizer = 0;
            Random rand = new ThreadSafeRandom().rand;

            public List<Point> centers = new List<Point>();


            public override double getCaptureProbability(int r_p, int d, int maxPursuersCount)
            {
                r_p -= r_p % 2;
                if (maxPursuersCount >= minimalPursuersCount(r_p,d))
                    return 1.0 / (2 * r_p * (r_p + 1) + 1 - 2);// for each area of  r_pXr_p exclusing 2 points, there is at least 1 pursuer jumping
                return 0;
            }
            public override int minimalPursuersCount(int r_p, int d)
            {
                r_p -= r_p % 2;
                //return 2 * (int)Math.Pow(d / (r_p) + 1, 2);
                return 2 * (int)Math.Pow(Math.Ceiling((float)d / (r_p)), 2);
            }
            public override int getUsedPursuersCount(int r_p, int d, int pursuersCount)
            {
                r_p -= r_p % 2;
                int m = minimalPursuersCount(r_p, d);
                if (pursuersCount >= m)
                    return m;
                return 0;
            }

            public override void Init(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers, Dictionary<Pursuer, Location> L, Location v, int d)
            {
                r_p -= r_p % 2;
                int y = v.nodeLocation.Y - d + r_p - 1;
                int x = v.nodeLocation.X;

                var pIter = pursuers.GetEnumerator();
                pIter.MoveNext();

                List<Point> initialCenters = GameLogic.Utils.getGridPoints(false, false, r_p, d, new Point(x, y), -1, 1);
                foreach(Point p in initialCenters)
                {
                    List<Point> line = GameLogic.Utils.getGridPoints(false, false, r_p, d, p, 1, 1);
                    foreach(Point lp in line)
                    {
                        L[pIter.Current] = new Location(lp);
                        centers.Add(lp);
                        pIter.MoveNext();

                        L[pIter.Current] = new Location(lp);
                        pIter.MoveNext();
                    }
                }

            }

            bool evenPursuersInCenter = true;
            public override void advancePursuers(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers, Dictionary<Pursuer,Location> L, Location v, int d)
            {
                r_p -= r_p % 2;
                //int offsetX = rand.Next(-r_p,r_p +1);
                //int offsetY = (r_p) - Math.Abs(offsetX);
                //offsetY = rand.Next(-offsetY,offsetY+1);
                //Point offset = new Point(offsetX, offsetY);
                Point offset = Utils.getUniformRandomPointInManDistance(r_p, rand);

                 var pIter = pursuers.GetEnumerator();

                if(evenPursuersInCenter)
                {
                    foreach(Point center in centers)
                    {
                        Point newPos = center.add(offset);
                        pIter.MoveNext();
                        L[pIter.Current] = new Location(newPos);
                        pIter.MoveNext();
                        L[pIter.Current] = new Location(center);
                        
                    }
                }
                else
                {
                    foreach (Point center in centers)
                    {
                        Point newPos = center.add(offset);
                        pIter.MoveNext();
                        L[pIter.Current] = new Location(center);
                        pIter.MoveNext();
                        L[pIter.Current] = new Location(newPos);
                        
                    }
                }
                evenPursuersInCenter = !evenPursuersInCenter;
                while (pIter.MoveNext())
                {
                    L[pIter.Current] = v;
                }
                
            }

            public override APatrol generateNew()
            {
                return new SwitchPatrol();
            }
        }
        /// <summary>
        /// divides area into a grid, and lets each pursuer jump randomly within an area of (r_p/2) X (r_p/2)
        /// </summary>
        public class DenseGridPatrol : APatrol
        {
            static int rollingRandomizer = 0;
            Random rand = new ThreadSafeRandom().rand;
            int clusterSize = 1;
            public Dictionary<Pursuer, Point> centers = new Dictionary<Pursuer, Point>();

            public override double getCaptureProbability(int r_p, int d, int maxPursuersCount)
            {
                r_p -= r_p % 2;
                int m = minimalPursuersCount(r_p, d);
                
                maxPursuersCount = getUsedPursuersCount(r_p, d, maxPursuersCount);

                // we don't reduce the size of the designated area because
                // sometimes pursuers go outside
                if (maxPursuersCount >= m)
                    return ((float)(maxPursuersCount/m)) / (r_p * (r_p/2 + 1) + 1); // cluster size divided by designated area
                return 0;
            }
            public override int minimalPursuersCount(int r_p, int d)
            {
                r_p -= r_p % 2;
                int iterCount = (int)Math.Ceiling(((float)d) / (r_p / 2));
                return (int)Math.Pow(iterCount + 1, 2);
            }
            public override int getUsedPursuersCount(int r_p, int d, int pursuersCount)
            {
                int m = minimalPursuersCount(r_p, d);
                return pursuersCount - (pursuersCount % m);
            }

            public override void Init(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers, Dictionary<Pursuer, Location> L, Location v, int d)
            {
                r_p -= r_p % 2;
                
                //int fixD = -1;
                int fixD = 0;
                if(d % 2 == 1)
                    fixD = 0;

                int y = v.nodeLocation.Y - d + r_p / 2 + fixD;
                int x = v.nodeLocation.X;

                var pIter = pursuers.GetEnumerator();

                clusterSize = getUsedPursuersCount(r_p, d, pursuers.Count()) / minimalPursuersCount(r_p, d);

                List<Point> initialCenters = Utils.getGridPoints(false, false, r_p/2, d, new Point(x, y), -1, 1);
                foreach (Point p in initialCenters)
                {
                    List<Point> line = Utils.getGridPoints(false, false, r_p/2, d, p, 1, 1);
                    foreach (Point lp in line)
                    {
                        for (int i = 0; i < clusterSize; ++i)
                        {
                            pIter.MoveNext();
                            L[pIter.Current] = new Location(lp);
                            centers[pIter.Current] = lp;
                        }

                    }
                }


                while (pIter.MoveNext())
                {
                    L[pIter.Current] = v;
                }
                
                prevOffsets = new Point[clusterSize];
                for (int i = 0; i < clusterSize; ++i )
                    prevOffsets[i] = new Point(0, 0);
            }

            Point[] prevOffsets;


            public override void advancePursuers(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers, Dictionary<Pursuer, Location> L, Location v, int d)
            {

                r_p -= r_p % 2;
                
                Point[] offset = new Point[clusterSize];
                
                for(int i = 0; i < clusterSize; ++i)
                {
                    while (true)
                    {
                        //int offsetX;
                        //int offsetY;

                        //do
                        //{

                            //offsetX = rand.Next(-r_p / 2, r_p / 2 + 1);
                            //offsetY = (r_p / 2) - Math.Abs(offsetX);
                            //offsetY = rand.Next(-offsetY, offsetY + 1);
                        //}
                        //while (offsetX == 0 && offsetY == 0);

                        //Point newP = new Point(offsetX, offsetY);
                        Point newP = Utils.getUniformRandomPointInManDistance(r_p / 2, rand);
                        for (int j = 0; j < i; ++j)
                            if (offset[j] == newP)
                                continue; // points must not be equal, to increase capture prob.
                        for (int j = 0; j < clusterSize; ++j)
                            if (prevOffsets[j] == newP)
                                continue;
                        offset[i] = newP;
                        break;
                    }
                }
                prevOffsets = offset;

                int c = 0;
                foreach(Pursuer p in pursuers)
                {
                    if (!centers.Keys.Contains(p))
                        L[p] = v;
                    else
                    {
                        Point newPos = centers[p].add(offset[c]);
                        L[p] = new Location(newPos);
                    }
                    c = (c + 1) % clusterSize;
                }

            }

            public override APatrol generateNew()
            {
                return new DenseGridPatrol();
            }
        }

        public class SwitchPatrol2 : APatrol
        {
            protected struct DualPursuersArea
            {
                public Pursuer p1, p2;
            }
            protected class DualPursuersLocations
            {
                public Point p1Loc, p2Loc;
            }

            // given two pursuers and their two previous locations, and two new locations,
            // this method sets the pursuers to the new location, such that both can reach the new targets
            protected void updatePursuerLocations(int r_p,
                                                  Point center,
                                                  Dictionary<Pursuer,Location> locations,
                                                  DualPursuersLocations patternLocations,
                                                  DualPursuersArea pursuersToUpdate)
            {
                Point nextNonCenterLoc = patternLocations.p1Loc.add(center);
                Point nextLoc = patternLocations.p2Loc.add(center);
                
                if (nextNonCenterLoc.X == 0 && nextNonCenterLoc.Y == 0)
                    // to make sure nextNonCenterLoc really isn't center location
                    AlgorithmUtils.Swap(ref nextNonCenterLoc, ref nextLoc);

                Point prevLoc1 = locations[pursuersToUpdate.p1].nodeLocation;
                Point prevLoc2 = locations[pursuersToUpdate.p2].nodeLocation;
                
                if ( ((prevLoc1.X != 0 || prevLoc1.Y != 0) && nextNonCenterLoc.manDist(prevLoc1) <= r_p) ||
                    nextLoc.manDist(prevLoc2) <= r_p)
                {
                    // ether prevLoc1 is not center point - and may only reach 'nextNonCenterLoc',
                    // or that it is the center point, but only prevLoc2 can reach nextNonCenterLoc
                    locations[pursuersToUpdate.p1] = new Location(nextNonCenterLoc);
                    locations[pursuersToUpdate.p2] = new Location(nextLoc);
                }
                else
                {
                    locations[pursuersToUpdate.p1] = new Location(nextLoc);
                    locations[pursuersToUpdate.p2] = new Location(nextNonCenterLoc);
                }
            }

            protected class SymmetricAreaPattern
            {
                public ThreadSafeRandom rand;

                public PatternRandomizer pRand { get; protected set; }

                public List<Point> patternIdxToAreaPoint { get; protected set; }

                public bool isCenterOccupied { get; protected set; }


                /// <summary>
                /// probability of any 1 point for being occupied, each round
                /// </summary>
                public double singlePointOccupationProb { get; protected set; }

                public List<DualPursuersLocations> Pursuers { get; protected set; }

                /// <summary>
                /// </summary>
                /// <param name="rad"></param>
                /// <param name="usedPursuers">
                /// an even number
                /// </param>
                public SymmetricAreaPattern(int rad, int pursuersCount)
                {
                    rand = new ThreadSafeRandom();
                    double usedPursuers = (pursuersCount - (pursuersCount % 2));
                    singlePointOccupationProb =
                        usedPursuers / (2 * rad * (rad + 1) + 1 - usedPursuers);
                    
                    patternIdxToAreaPoint = Utils.getAllPointsInArea(rad,false,true,true,false,false); // idx 0 is point (0,0)
                    pRand = new PatternRandomizer(patternIdxToAreaPoint.Count, (int)usedPursuers / 2); // we randomize over half the area, and the other half becomes a mirror (center point is excluded)
                    isCenterOccupied = false; // only points in the half area are now occupied
                    
                    Pursuers = new List<DualPursuersLocations>((int)usedPursuers / 2);
                    for (int i = (int)usedPursuers / 2; i > 0; --i)
                        Pursuers.Add(new DualPursuersLocations() { p1Loc = new Point(0, 0), p2Loc = new Point(0, 0) });
                }
                public void randomize()
                {
                    pRand.Randomize(rand, false);

                    int pairIdx = 0;
                    foreach (var p in Pursuers)
                    {
                        p.p1Loc = patternIdxToAreaPoint[pRand.CurrentlyUsedPoints[pairIdx++]];
                        p.p2Loc = Utils.getRotated(p.p1Loc, new Point(0, 0), 2);
                    }

                    isCenterOccupied =
                        (!isCenterOccupied && rand.NextDouble() < singlePointOccupationProb);
                    if(isCenterOccupied)
                    {
                        // choose a random pursuer, and instead make it go to the center.
                        // every pursuer can get to the center. The pursuer remains "dual" since no matter
                        // where its match goes the next round, the pursuer in the center will necessarily be able
                        // to walk to the opposite point
                        Pursuers[rand.Next() % Pursuers.Count].p1Loc = new Point(0, 0);
                    }
                }
            }

            public override double getCaptureProbability(int r_p, int d, int maxPursuersCount)
            {
                //r_p -= r_p % 2;
                if (maxPursuersCount >= minimalPursuersCount(r_p, d))
                {
                    int areasCount = (int)Math.Pow(Math.Ceiling((float)d / (r_p)), 2);
                    double pursuersPerArea = (int)(maxPursuersCount / areasCount);
                    pursuersPerArea = pursuersPerArea - pursuersPerArea % 2;

                    return pursuersPerArea / (2 * r_p * (r_p + 1) + 1 - pursuersPerArea);// for each area of  r_pXr_p exclusing 2 points, there is at least 1 pursuer jumping
                }
                return 0;
            }
            public override int minimalPursuersCount(int r_p, int d)
            {
                //r_p -= r_p % 2;
                //return 2 * (int)Math.Pow(d / (r_p) + 1, 2);
                return 2 * (int)Math.Pow(Math.Ceiling((float)d / (r_p)), 2);
            }
            public override int getUsedPursuersCount(int r_p, int d, int pursuersCount)
            {
                //r_p -= r_p % 2;
                if (pursuersCount >= minimalPursuersCount(r_p, d))
                {
                    int areasCount = (int)Math.Pow(Math.Ceiling((float)d / (r_p)), 2);
                    double pursuersPerArea = pursuersCount / areasCount;
                    pursuersPerArea = pursuersPerArea - pursuersPerArea % 2;
                    return (int)(pursuersPerArea * areasCount);
                }
                return 0;
            }

            SymmetricAreaPattern pattern;
            Dictionary<Point, List<DualPursuersArea>> pursuerPairs; // a list of pursuers associated with each area center point
            private List<Point> initialCenters;
            private int pursuersPerArea; // a multiple of 2

            public override void Init(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers, Dictionary<Pursuer, Location> L, Location v, int d)
            {
                //r_p -= r_p % 2;
                int y = v.nodeLocation.Y - d + r_p;// -(1 - r_p % 2);
                int x = v.nodeLocation.X;

                var pIter = pursuers.GetEnumerator();
                pIter.MoveNext();

                int areasCount = (int)Math.Pow(Math.Ceiling((float)d / (r_p)), 2);
                pursuersPerArea = pursuers.Count() / areasCount;
                pursuersPerArea = pursuersPerArea - pursuersPerArea % 2;
                pursuerPairs = new Dictionary<Point, List<DualPursuersArea>>();

                //for (int areaI = pursuersPerArea / 2; areaI > 0; --areaI)
                //    pursuerPairs[new DualPursuersLocations()] = new List<DualPursuersArea>(areasCount);
                
                //for (int i = 0; i < pursuersPerArea; ++i)
                //{
                    //List<DualPursuersArea> assignedPursuers = new List<DualPursuersArea>();
                    //for(int areaI = pursuersPerArea/2; areaI > 0; --areaI)
                    //    assignedPursuers.Add(new DualPursuersArea(){center)
                    //pursuers[pI++], pursuers[pI++]
                    //pursuerPairs.Add(new DualPursuersLocations(), assignedPursuers);
                //}

                pattern = new SymmetricAreaPattern(r_p, pursuersPerArea);
                initialCenters = GameLogic.Utils.getGridPoints(false, false, r_p, d, new Point(x, y), -1, 1);

                foreach (Point p in initialCenters)
                {
                    List<Point> line = GameLogic.Utils.getGridPoints(false, false, r_p, d, p, 1, 1);
                    foreach (Point lp in line)
                    {
                        var pList = new List<DualPursuersArea>(pursuersPerArea);
                        for (int pI = pursuersPerArea/2; pI > 0; --pI)
                        {
                            var dualP = new DualPursuersArea();
                            dualP.p1 = pIter.Current;
                            pIter.MoveNext();
                            dualP.p2 = pIter.Current;
                            pIter.MoveNext();
                            pList.Add(dualP);
                            L[dualP.p1] = new Location(lp);
                            L[dualP.p2] = new Location(lp);
                        }
                        pursuerPairs[lp] = pList;
                        //L[pIter.Current] = new Location(lp);
                        //centers.Add(lp);
                        //pIter.MoveNext();

                        //L[pIter.Current] = new Location(lp);
                        //pIter.MoveNext();
                        
                    }
                }
                while (pIter.MoveNext())
                {
                    L[pIter.Current] = v;
                }
            }

            public override void advancePursuers(GridGameGraph graph, int r_p, ListRangeEnumerable<Pursuer> pursuers, Dictionary<Pursuer, Location> L, Location v, int d)
            {
              
                //r_p -= r_p % 2;
                //int offsetX = rand.Next(-r_p,r_p +1);
                //int offsetY = (r_p) - Math.Abs(offsetX);
                //offsetY = rand.Next(-offsetY,offsetY+1);
                //Point offset = new Point(offsetX, offsetY);
                //Point offset = Utils.getUniformRandomPointInManDistance(r_p, rand);

                pattern.randomize();
                var pIter = pursuers.GetEnumerator();
                
                // go over each area
                foreach(var areaPursuers in pursuerPairs)
                {
                    int i = 0;
                    // go over all pursuers in that area, and make sure they now match the pattern
                    foreach (var p in areaPursuers.Value)
                    {
                        updatePursuerLocations(r_p, areaPursuers.Key, L, pattern.Pursuers[i++], p);
                        pIter.MoveNext();
                        pIter.MoveNext();
                    }
                }

                //if (evenPursuersInCenter)
                //{
                //    foreach (Point center in centers)
                //    {
                //        Point newPos = center.add(offset);
                //        pIter.MoveNext();
                //        L[pIter.Current] = new Location(newPos);
                //        pIter.MoveNext();
                //        L[pIter.Current] = new Location(center);

                //    }
                //}
                //else
                //{
                //    foreach (Point center in centers)
                //    {
                //        Point newPos = center.add(offset);
                //        pIter.MoveNext();
                //        L[pIter.Current] = new Location(center);
                //        pIter.MoveNext();
                //        L[pIter.Current] = new Location(newPos);

                //    }
                //}
                //evenPursuersInCenter = !evenPursuersInCenter;
                while (pIter.MoveNext())
                {
                    L[pIter.Current] = v;
                }

            }

            public override APatrol generateNew()
            {
                return new SwitchPatrol2();
            }
        }

        //public class RingPatrol 
        //    // can't inherit from IPatrol, since Init() method differs
        //{

        //    ThreadSafeRandom rand = new ThreadSafeRandom();

        //    List<Pursuer> sortedPursuers = new List<Pursuer>();
        //    GoE.Utils.AlgorithmUtils.MultiPatternRandomizer pRand;
        //    int patternPoints, patternOccupiedPoints;

        //    /// <summary>
        //    /// 
        //    /// </summary>
        //    /// <param name="r_p"></param>
        //    /// <param name="minD">
        //    /// maxD - r_p/2 <= minD <= maxD
        //    /// </param>
        //    /// <param name="maxD"></param>
        //    /// <param name="maxPursuersCount"></param>
        //    /// <returns></returns>
        //    public double getCaptureProbability(int r_p, int minD, int maxD, int maxPursuersCount)
        //    {
        //        int patternPointCount, patternOccupiedPointCount;
        //        getPattern(r_p, minD, maxD, maxPursuersCount, out  patternPointCount, out patternOccupiedPointCount);
        //        return (((double)patternOccupiedPointCount) / (patternPointCount-patternOccupiedPointCount)) / (maxD - minD + 1);
        //    }

        //    public int minimalPursuersCount(int r_p, int minD, int maxD)
        //    {
        //        int d = (int)Math.Ceiling((float)(minD + maxD) / 2); // reaachable area is +-r_p/4 from ring center
        //        int patternPointCount = r_p + 1;
        //        return (int)Math.Ceiling((4.0f * d) / patternPointCount);
        //    }

        //    public int getUsedPursuersCount(int r_p, int minD, int maxD, int pursuersCount)
        //    {
        //        if (minimalPursuersCount(r_p, minD, maxD) > pursuersCount)
        //            return 0;
        //        return pursuersCount;
        //    }

        //    private static void getPattern(int r_p, int minD, int maxD, int maxPursuersCount, out int patternPointCount, out int patternOccupiedPointCount)
        //    {
        //        patternPointCount = r_p + 1;
        //        int d = (int)Math.Min(maxD,
        //                               Math.Floor(((float)patternPointCount * maxPursuersCount) / 4));
        //            // if maxPursuersCount == minimalPursuersCount and patternPointCount == 1, then
        //            // we assume pursuers are spread accorss the ring 'Math.Ceiling((float)(minD + maxD) / 2)',
        //            // and from there may reach all the desired area. It would be preferable to use
        //            // a larger ring, since then there are more pattern options (assuming all r_p values are tested),
        //            // but it's possible only if there are more avialable purusers

        //        patternOccupiedPointCount = (int) (patternPointCount * (maxPursuersCount/(4.0f * d)));

                
        //        // if the last group is so crowded with pursuers compared to it's size, that it would be better
        //        // to just put 'lastGroupSize' pursuers in the last pattern, and spread the others between the other groups:
        //        int lastGroupSize = 4 * d % patternPointCount;
        //        int excessPursuers = patternOccupiedPointCount - lastGroupSize;
        //        int groupsCount = getGroupsCount(r_p, d);
        //        patternOccupiedPointCount += (excessPursuers / groupsCount); // in most cases the addition would be 0
        //    }
        //    // last group might have less points, but same amount of points occupy it
        //    private static int getGroupsCount(int r_p, int d)
        //    {
        //        return (int)Math.Ceiling((4.0f * d) / (r_p+1)); 
        //    }

        //    /// <summary>
        //    /// 
        //    /// </summary>
        //    /// <param name="graph"></param>
        //    /// <param name="r_p"></param>
        //    /// <param name="pursuers"></param>
        //    /// <param name="L"></param>
        //    /// <param name="v"></param>
        //    /// <param name="maxD"></param>
        //    /// <param name="minD">
        //    ///   maxD - r_p/2 <= minD <= maxD
        //    /// </param>
        //    public void Init(GridGameGraph graph, int r_p,
        //                     ListRangeEnumerable<Pursuer> pursuers,
        //                     Dictionary<Pursuer, Location> L, Location v, int maxD, int minD)
        //    {
        //        // pursuers  move randomly, then "steal" pursuers from either directions. last stiching group either keeps
        //        // a compressed version of the pattern that repeats in all other groups, or just populated with all pursuers, 
        //        // in case the points count of that group is smaller then the amount of pursuers in each group.
        //        // Additionally, another thing unimplemented - using distance of r_p instead of r_p+1 between each two groups may sometimes
        //        // eliminate the stiching group entirely. There is a coresponding commented code in advancePursuers()

        //        r_p = r_p - (r_p % 2);
                
        //        getPattern(r_p,minD,maxD,pursuers.Count(),out patternPoints,out patternOccupiedPoints);

        //        if (minimalPursuersCount(r_p,minD, maxD) < pursuers.Count())
        //            throw new AlgorithmException("not enough pursuers (or too slow) for circumference alg.!");

        //        int groupDistance = r_p + 1;
        //        int layersCount = maxD - minD + 1;
        //        double layersPointsSum = MathEx.sum(4 * minD, 4, minD, maxD);
        //        //double density = ((double)(patternOccupiedPoints)) / patternPoints;
        //        List<float> avgPointsPerLayer = new List<float>(layersCount);
                
        //        for(int lI = maxD; lI >= minD; --lI)
        //            avgPointsPerLayer.Add((float)(patternOccupiedPoints * ((4.0 * maxD) / layersPointsSum)));

        //        pRand =
        //            new AlgorithmUtils.MultiPatternRandomizer(avgPointsPerLayer, patternPoints, patternOccupiedPoints);
                    
        //        ListRangeEnumerator<Pursuer> pIter = (ListRangeEnumerator<Pursuer>)pursuers.GetEnumerator();

                
        //        float pursuerJumpAngle = 4.0f * 1 / (4 * maxD);
        //        float r_pAngleJump = 4.0f * (groupDistance) / (4 * maxD); // there is a gap of r_p BETWEEN each teo pursuers
        //        float groupSizeAngleJump = 4.0f * patternOccupiedPoints / (4 * maxD);
        //        float currentAngle = 0;
        //        int pursuerIdx = 0;

        //        while (currentAngle <= 4)
        //        {
        //            for (int i = 0; i < patternOccupiedPoints; ++i)
        //            {
        //                if (pIter.MoveNext())
        //                {
        //                    L[pIter.Current] = new Location(v.nodeLocation.add(Utils.getGridPointByAngle(maxD, currentAngle)));
        //                    sortedPursuers.Add(pIter.Current);
        //                    ++pursuerIdx;
        //                }
        //                else
        //                    break;
        //                currentAngle += pursuerJumpAngle;
        //            }
        //            currentAngle += (r_pAngleJump - groupSizeAngleJump);
        //        }

                

        //        //while (pIter.MoveNext())
        //        //{
        //        //    L[pIter.Current] = new Location(v.nodeLocation);
        //        //    unusedPursuers.Add(pIter.Current);
        //        //}

        //        return;
        //    }

        //    public void advancePursuers(GridGameGraph graph, int r_p,
        //                            ListRangeEnumerable<Pursuer> pursuers,
        //                            Dictionary<Pursuer, Location> L, Location v, int maxD, int minD)
        //    {
        //        int groupDistance = r_p + 1;

        //        // pursuers move randomly, then "steal" pursuers from either directions. last stiching group either keeps
        //        // a compressed version of the pattern that repeats in all other groups, or just populated with all pursuers, 
        //        // in case the points count of that group is smaller then the amount of pursuers in each group.
        //        // Additionally, another thing unimplemented - using distance of r_p instead of r_p+1 between each two groups may sometimes
        //        // eliminate the stiching group entirely

        //        pRand.Randomize(rand);


        //        int layersCount = maxD - minD + 1;

                
        //        // pursuerPointsByLayer[0] are the pursuers in the ourter most layer
        //        //List<List<Point>> pursuerPointsByLayer =
        //        //    new List<List<Point>>(layersCount);
        //        //for (int lI = 0; lI < layersCount; ++lI)
        //        //    pursuerPointsByLayer.Add(new List<Point>());

        //        //foreach (Pursuer p in pursuers)
        //        //{
        //        //    if (L[p].nodeLocation == v.nodeLocation)
        //        //        continue; // we ignore pursuers in the center

        //        //    Point pLoc = L[p].nodeLocation;
        //        //    int pursuerLayer = pLoc.manDist(v) - maxD;
        //        //    pursuerPointsByLayer[pursuerLayer].Add()
        //        //}

        //        List<Point> pursuerPoints = new List<Point>();
        //        foreach (Pursuer p in pursuers)
        //        {
        //            if (L[p].nodeLocation == v.nodeLocation)
        //                continue; // we ignore pursuers in the center
        //            pursuerPoints.Add(L[p].nodeLocation);
        //        }

        //        // TODO: maybe we don't even need this sorting, since pursuers remain sorted aftert 
        //        // each advancepursuers() call
        //        List<GoE.GameLogic.Utils.SortedPointIdx> sortedPursuers =
        //            Utils.sortPointIndicesByAngles(pursuerPoints, v.nodeLocation);

        //        bool drawFromFollowingPursuers = true;
        //        int drawnPursuersCount = 0;
        //        for (int i = 0; i < patternOccupiedPoints; ++i) // try to match the new target indices with the first groupSize pursuers
        //        {
        //            int jump = pRand.CurrentlyUsedPoints[i] - pRand.PreviouslyUsedPoints[i];
        //            // note: in this loop, only of the two ifs will always be used
        //            if (jump > r_p / 2)
        //            {

        //                #if DEBUG
        //                // TODO: remove
        //                if(drawnPursuersCount < 0);
        //                    while(true);
        //                #endif

        //                //drawFromFollowingPursuers = true;
        //                ++drawnPursuersCount;
        //            }
        //            if (jump < (-r_p / 2))
        //            {
        //                #if DEBUG
        //                // TODO: remove
        //                if(drawnPursuersCount > 0);
        //                    while(true);
        //                #endif
        //                //drawFromFollowingPursuers = false;
        //                --drawnPursuersCount;
        //            }
        //        }

        //        // the pattern describes how the first points should look like
        //        float pursuerJumpAngle = 4.0f * 1 / (4 * maxD);

        //        int pursuerPatternJumpAngle = 0;
        //        int nextGroupCounter = patternOccupiedPoints;

        //        // if all is well and pursuers remain in the same area, then drawnPursuersCount=0 and the first 'patternOccupiedPoints' pursuers would corespond the pattern)
        //        // otherwise, we have to put in the area of the first pattern some pursuers from a different area:
        //        int pursuerIdx = drawnPursuersCount; 
        //        if(pursuerIdx < 0)
        //            pursuerIdx += pursuers.Count();
                
        //        int lastGroupOccupyingPoints = pursuers.Count() % patternOccupiedPoints; // last few pursuers might get compressed
        //        int lastGroupPoints = (4 * maxD) % patternPoints;
        //        int uncompressedPursuers = pursuers.Count() - lastGroupOccupyingPoints ;
                
        //        int loopIt = 0;
                
        //        for(; loopIt < uncompressedPursuers; ++loopIt)
        //        {
        //            float destAngle =  
        //                pursuerJumpAngle * 
        //                (pursuerPatternJumpAngle + pRand.CurrentlyUsedPoints[loopIt % patternOccupiedPoints] );

        //            L[pursuers[pursuerIdx]].setPoint(Utils.getGridPointByAngle(maxD, destAngle));
        //            pursuerIdx = (pursuerIdx + 1) % pursuers.Count();


        //            // check if we now move to the next repetition of the pattern
        //            --nextGroupCounter;
        //            if(nextGroupCounter == 0)
        //            {
        //                nextGroupCounter = patternOccupiedPoints;
        //                pursuerPatternJumpAngle += patternOccupiedPoints;
        //            }
        //        }

        //        // the last few pursuers (from "stiching group") may not go outside their region, since otherwise they 
        //        // may overlap other puruers
        //        bool[] assignedPoint = GoE.Utils.AlgorithmUtils.getRepeatingValueArr(false, lastGroupPoints);

        //        float maxAngleIdx = lastGroupPoints;
        //        for (; loopIt < pursuers.Count(); ++loopIt)
        //        {
        //            int angIdx = pRand.CurrentlyUsedPoints[loopIt % patternOccupiedPoints];
                    
        //            // we now put pursuers in the range of pursuerPatternJumpAngle + [0, maxAngleIdx)
        //            angIdx = (int)Math.Min(angIdx, maxAngleIdx - 1);
        //            while (assignedPoint[angIdx])
        //                --angIdx;

        //            assignedPoint[angIdx] = true;

        //            float destAngle =
        //                pursuerJumpAngle *
        //                (pursuerPatternJumpAngle + angIdx);

        //            L[pursuers[pursuerIdx]].setPoint(Utils.getGridPointByAngle(maxD, destAngle));
        //            pursuerIdx = (pursuerIdx + 1) % pursuers.Count();
        //        }


        //        // TODO: remove sanity checks:
        //        //if (drawFromFollowingPursuers)
        //        //{
        //        //    for (int i = 0; i < groupSize; ++i)
        //        //    {
        //        //        int jump = indices[i] - currentlyUsedTargetIndices[i];
        //        //        if (jump < -r_p / 2)
        //        //        {
        //        //            int a = 0;
        //        //        }
        //        //    }
        //        //}
        //        //if (!drawFromFollowingPursuers)
        //        //{
        //        //    for (int i = 0; i < groupSize; ++i)
        //        //    {
        //        //        int jump = indices[i] - currentlyUsedTargetIndices[i];
        //        //        if (jump > r_p / 2)
        //        //        {
        //        //            int a = 0;
        //        //        }
        //        //    }
        //        //}

        //        //int minp = minimalPursuersCount(r_p, d);
        //        //int used = getUsedPursuersCount(r_p, d, pursuerPoints.Count);
        //        //int completeGroupsCount = minp - 1;
        //        //if (used == minp * groupSize)
        //        //    ++completeGroupsCount;
        //        //if (drawFromFollowingPursuers)
        //        //{

        //        //    for (int i = 0; i < completeGroupsCount; ++i)
        //        //    {
        //        //        for (int j = i * groupSize; j < (1 + i) * groupSize; ++j)
        //        //        {
        //        //            int idxInGroup = j - i * groupSize;
        //        //            int jump = indices[idxInGroup] - currentlyUsedTargetIndices[idxInGroup];
        //        //            int movingPursuerIndex = sortedPursuers[(j - drawnPursuersCount + used) % used].pointIndex;
        //        //            L[pursuers[movingPursuerIndex]] =
        //        //                new Location(movePoint(L[pursuers[movingPursuerIndex]].nodeLocation, jump, v.nodeLocation, d));
        //        //        }
        //        //    }
        //        //    if (completeGroupsCount == minp - 1)
        //        //    {
        //        //        // there was another group of pursuers, that has an area smaller than r_p+1 point count,
        //        //        // but has min(groupSize, point count) pursuers
        //        //        int lastGroupPointCount = getLastGroupPointCount(r_p, d, used); // make sure used and pursuers.count give same result
        //        //        int lastGroupPursuersCount = getLastGroupPursuersCount(r_p, d, used);
        //        //        if (lastGroupPursuersCount == groupSize)
        //        //        {
        //        //            float compressRatio = ((float)lastGroupPointCount) / groupSize;
        //        //            for (int j = used - lastGroupPointCount; j < used; ++j)
        //        //            {
        //        //                int idxInGroup = j - completeGroupsCount * groupSize;
        //        //                int jump = (int)Math.Round(compressRatio * (indices[idxInGroup] - currentlyUsedTargetIndices[idxInGroup]));
        //        //                int movingPursuerIndex = sortedPursuers[(j - drawnPursuersCount + used) % used].pointIndex;
        //        //                L[pursuers[movingPursuerIndex]] =
        //        //                    new Location(movePoint(L[pursuers[movingPursuerIndex]].nodeLocation, jump, v.nodeLocation, d));
        //        //            }
        //        //        }
        //        //        else
        //        //        {
        //        //            // if points count is so small, then the area is so compressed - that all points must have a pursuer
        //        //        }

        //        //    }
        //        //}

        //        //foreach (Pursuer p in unusedPursuers)
        //        //    L[p] = new Location(v.nodeLocation);

        //        //return;
                
        //    }
  
        //}
        
        /// <summary>
        /// utility to be used by evaders, to check 
        /// </summary>
        public class PursuerStatistics
        {
            public PursuerStatistics(GridGameGraph gg, int HistoryLookback) 
            {
                p = gg;
                visitTimesBegining = new float[p.WidthCellCount, p.HeightCellCount];
                visitTimesHistoryLookback = new float[p.WidthCellCount, p.HeightCellCount];
                for (int i = 0; i < p.WidthCellCount; ++i)
                    for (int j = 0; j < p.HeightCellCount; ++j)
                    {
                        visitTimesBegining[i, j] = 0;
                        visitTimesHistoryLookback[i, j] = 0;
                    }
                
                this.historyLookback = HistoryLookback;
            }
            
            public void update(HashSet<Point> O_p)// GameState s, GameParams p, GridGameGraph g)
            {
                int reducedVists = 0;
                if(updatesCount > historyLookback)
                    reducedVists = 1;
                
                for (int i = 0; i < p.WidthCellCount; ++i)
                    for (int j = 0; j < p.HeightCellCount; ++j)
                    {
                        if(O_p.Contains(new Point(i,j)))
                        {
                            ++visitTimesBegining[i, j];
                            visitTimesHistoryLookback[i, j] = Math.Min(visitTimesHistoryLookback[i, j]+1,historyLookback);
                        }
                        else
                        {
                            // assuming visitTimesHistoryLookback[i, j] times in the last historyLookback
                            // we detected a pursuer, then if we had an accurate table of size historyLookback
                            // then assuming we replaced the oldest entry, there was a probability of visitTimesHistoryLookback[i, j]/historyLookback
                            // that it was "visit" entry
                            // TODO: consider using this method only for large history lookbacks
                            visitTimesHistoryLookback[i, j] -=
                                reducedVists * Math.Min(1.0f,visitTimesHistoryLookback[i, j]) / historyLookback; 
                        }
                    }
                ++updatesCount;
            }

            public GridGameGraph p;
            public int historyLookback { get; protected set; }
            public ushort updatesCount = 0;
            public float[,] visitTimesBegining;
            public float[,] visitTimesHistoryLookback; // not entirely accurate, but good enough
        }
        /// <summary>
        /// a basic function to manage an evader that walks from one point to another, while avoiding pursuers
        /// </summary>

        /// <summary>
        /// may be used for class EvaderCrawl
        /// </summary>
        
        
        
        public class EvaderCrawlChromosome : MemberFractionArrayChsromosome
        {
            public enum ProbabilityMeaningIdx : int
            {
                 RandProb = 0, 
                 DijakstraBeginingProb, 
                 DijakstraHistoryProb, 
                 FurthestFromPursuersProb,

                Count
            }
            public List<double> getProbabilities()
            {
                return getNormalizedValues();
            }


            public EvaderCrawlChromosome(EvaderCrawlChromosome src)
                : base(src)
            {

            }
            public EvaderCrawlChromosome()
                : base((int)(ProbabilityMeaningIdx.Count - 1), 11, EvolutionConstants.valueMutationProb) { }

            public override IChromosome CreateNew()
            {
                return new EvaderCrawlChromosome();
            }
            public override IChromosome Clone()
            {
                return new EvaderCrawlChromosome(this);
            }
        }
        
        /// <summary>
        /// allows deciding how to safely walk from point a to b
        /// </summary>
        public class EvaderCrawl
        {
            /// <summary>
            /// all probabilites must be summed to 1, and tell the probability
            /// of the algorithm choosing to walk by a certain method
            /// </summary>
            /// <param name="CrawlingEvader"></param>
            /// <param name="s"></param>
            /// <param name="TargetLocation"></param>
            /// <param name="RandProb">random walk</param>
            /// <param name="DijakstraBeginingProb">
            /// dijakstra, where weights a statistics from history begining
            /// </param>
            /// <param name="DijakstraHistoryProb">
            /// dijakstra, where weights a statistics from short lookback history only 
            /// </param>
            /// <param name="FurthestFromPursuersProb">
            /// chooses the point that is furthest from sum of sqr distances from known pursuer locations
            /// </param>
            public EvaderCrawl(Evader CrawlingEvader, 
                               GameState s, 
                               Point TargetLocation,
                               float RandProb, 
                               float DijakstraBeginingProb, 
                               float DijakstraHistoryProb, 
                               float FurthestFromPursuersProb)
            {
                this.crawlingEvader = CrawlingEvader;
                this.state = s;
                this.targetLocation = TargetLocation;
                this.randProb = RandProb;
                this.dijakstraBeginingProb = DijakstraBeginingProb;
                this.dijakstraHistoryProb = DijakstraHistoryProb;
                this.furthestFromPursuersProb = FurthestFromPursuersProb;
            }
            public Point targetLocation { get; protected set; }

            /// <summary>
            /// similar to getAvailableDestPoints(), but returns 'from' if no options are avilable,
            /// otherwise returns either a point closer on x or on y (y by default)
            /// </summary>
            public static Point getSomeAvailablePoint(Point from, Point to, HashSet<Point> O_p)
            {
                Point p1, p2;
                switch(getAvailableDestPoints(from, to, O_p, out p1, out p2))
                {
                    case 0: return from;
                    case 1: return p1;
                    case 2:
                    case 3: return p2;
                }
                return new Point();
            }

            /// <param name="O_p">
            /// unavailable points
            /// </param>
            /// <returns>
            /// 0 - no points available
            /// 1 - dest1 is available
            /// 2 - dest2 is available
            /// 3 - both points are available
            /// </returns>
            public static int getAvailableDestPoints(Point from, Point to, HashSet<Point> O_p, out Point destPoint1, out Point destPoint2)
            {
                destPoint1 = new Point(-1, -1);
                destPoint2 = new Point(-1, -1);
                
                if(from.X < to.X)
                    destPoint1 = from.add(1, 0);
                else if(from.X > to.X)
                    destPoint1 = from.add(-1, 0);
                
                if (from.Y < to.Y)
                    destPoint2 = from.add(0, 1);
                else if (from.Y > to.Y)
                    destPoint2 = from.add(0, -1);

                int p1Available = (destPoint1.X != -1 && !O_p.Contains(destPoint1)) ? (1) : (0);
                int p2Available = (destPoint2.X != -1 && !O_p.Contains(destPoint2)) ? (2) : (0);
                return p1Available + p2Available;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="s"></param>
            /// <returns></returns>
            public Point getNextEvaderPoint(PursuerStatistics s, HashSet<Point> O_p, Random r)
            {
                Point currentLocation = state.L[state.MostUpdatedEvadersLocationRound][crawlingEvader].nodeLocation;
                Point destPoint1 = new Point(-1, -1), 
                    destPoint2 = new Point(-1, -1);
                
                if(currentLocation.X < targetLocation.X)
                    destPoint1 = currentLocation.add(1, 0);
                else if(currentLocation.X > targetLocation.X)
                    destPoint1 = currentLocation.add(-1, 0);
                
                if (currentLocation.Y < targetLocation.Y)
                    destPoint2 = currentLocation.add(0, 1);
                else if (currentLocation.Y > targetLocation.Y)
                    destPoint2 = currentLocation.add(0, -1);

                bool p1Available = (destPoint1.X != -1 && !O_p.Contains(destPoint1));
                bool p2Available = (destPoint2.X != -1 && !O_p.Contains(destPoint2));

                if (p2Available && !p1Available)
                    return destPoint2;
                if (p1Available && !p2Available)
                    return destPoint1;
                if (!p1Available && !p2Available)
                    return currentLocation;

                float alg = (float)r.NextDouble();
                if (alg < randProb)
                {
                    if (r.Next() % 2 == 0)
                        return destPoint1;
                    return destPoint2;
                }
                alg -= randProb;
                if(alg < dijakstraBeginingProb)
                {
                    return Utils.getShortestPathDest(currentLocation, targetLocation, s.visitTimesBegining);
                }
                alg -= dijakstraBeginingProb;
                if(alg < dijakstraHistoryProb)
                {
                    return Utils.getShortestPathDest(currentLocation, targetLocation, s.visitTimesHistoryLookback);
                }
                
                // maximize sqr distances sum from all known pursuers
                double dist1 =0 , dist2 = 0;
                foreach(Point p in O_p)
                {
                    dist1 += MathEx.Sqr(p.manDist(destPoint1));
                    dist2 += MathEx.Sqr(p.manDist(destPoint2));
                }
                if (dist1 < dist2)
                    return destPoint2;
                return destPoint1;
            }

            private float randProb, dijakstraBeginingProb, dijakstraHistoryProb, furthestFromPursuersProb;
            private Evader crawlingEvader;
            private GameState state;
            

        }
        
        /// <summary>
        /// allows deciding where an evader should go, if it wants to make sure his location distributes uniformly
        /// between all the locations he may go to
        /// </summary>
        public class Evasion
        {
            /// <summary>
            /// used by uniformEvade().  evasionProb[k] tells the movement probabilities, given a specific k
            /// </summary>
            private static List<List<double>> evasionProb = new List<List<double>>(); 
            private static int probIndex(int type, int x, int y)
            {
                int i = x + y;
                int j = 0;

                switch (type)
                {
                    case 1: // stay
                    case 2: // right
                    case 3: // up
                        j = type; break;
                }

                return (3 * ((i + 1) * i / 2 - 1 + y) + j);
            }
            private static int distRandom(List<double> probs, Random rand)
            {
                double r = rand.NextDouble();
                double sum = 0;
                int k = 0;
                foreach (double p in probs)
                {
                    if (p == 0)
                        break;
                    sum += p;
                    k++;
                    if (sum > r)
                        return (k);
                }
                return (0);
            }
            static Evasion()
            {
                List<string> filenames = null;
                string path = GoE.AppConstants.PathLocations.EVASION_PROB_FOLDER; //@"C:\Users\Mai\Documents\Bar Ilan\phd\evesSVNwc\GoE\software\r_plots";

                while (filenames == null)
                {
                    try
                    {
                        filenames = new List<string>(Directory.GetFiles(path, GoE.AppConstants.Algorithms.Evasion.PROBABILITY_FILENAME_FORMAT));
                    }
                    catch (Exception) { }

                    if (filenames == null)
                    {
                        // TODO: we need to load application arguments independently of GUI + save/load settings e.g. previously
                        // used path
                        path = InputBox.ShowDialog("EvasionProbTable csv file path:", "Error: couldn't find files in default folder", "").First();
                        if (path == "")
                            return;
                    }
                }

                filenames.Sort();
                // each file is associated with evasion that lasts for k rounds. all files are kept in the static evasionProb list
                for (int k = 1; k <= filenames.Count; ++k)
                {
                    evasionProb.Add(new List<double>());
                    string filename = FileUtils.TryFindingFile(filenames[k]);
                    string[] fileProbs = File.ReadAllLines(filename);
                    for (int l = 1; l < fileProbs.Count(); ++l)
                    {
                        evasionProb.Last().Add(double.Parse(fileProbs[l].Split(new char[] { ',' }).Last()));
                    }
                }

            }

            Point evasionStartPoint;
            GameState state;
            Evader evadingEvader;
            
            public Evasion(Point EvasionStartPoint, int k, GameState gs, Evader evadingEvader)
            {
                this.state = gs;
                this.evasionStartPoint = EvasionStartPoint;
                this.evadingEvader = evadingEvader;
            }
            
            /// <summary>
            /// Uniform evasion in round k after transmission
            /// Assumes that Point of transmission is (0,0)
            /// </summary>
            /// <param name="p">
            /// Current position relative to point of transmission
            /// </param>
            /// <param name="k">
            /// rounds since transmission
            /// </param>
            public Point getNextEvaderPoint(int k, HashSet<Point> O_p, Random rand)
            {
                Point currentPoint =
                    state.L[state.MostUpdatedEvadersLocationRound][evadingEvader].nodeLocation;
                Point p = currentPoint.subtruct(evasionStartPoint);
                Point q = new Point(Math.Abs(p.X), Math.Abs(p.Y));
                if (q.X + q.Y >= k)
                {
                    throw new AlgorithmException("Point not reachable");
                }

                double s00 = GameLogic.Utils.getGridGraphPointCount(k - 1) / GameLogic.Utils.getGridGraphPointCount(k);
                double m00 = (1 - s00) / 4;

                List<double> allProbs = evasionProb[k];

                
                if (q.X == 0 && q.Y == 0)
                {
                    // Center
                    List<double> probs = new List<double>();
                    probs.Add(s00);
                    probs.AddRange(Enumerable.Repeat(m00, 4));
                    int r = distRandom(probs, rand);
                    switch (r)
                    {
                        case 2:
                            q = new Point(1, 0);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                                goto case 3;
                        case 3:
                            q = new Point(-1, 0);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                                goto case 4;
                        case 4:
                            q = new Point(0, 1);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                                goto case 5;
                        case 5:
                            q = new Point(0, -1);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                            {
                                q = new Point(0,0);
                                break;
                            }
                        default:
                            break;
                    }
                }
                else if (q.Y == 0)
                {
                    // Cross
                    double s = allProbs[probIndex(1, q.X, q.Y)];
                    double mr = allProbs[probIndex(2, q.X, q.Y)];
                    double mu = allProbs[probIndex(3, q.X, q.Y)];
                    List<double> probs = new List<double>();
                    probs.Add(s);
                    probs.Add(mr);
                    probs.AddRange(Enumerable.Repeat(mu, 2));
                    int r = distRandom(probs, rand);
                    switch (r)
                    {
                        case 2:
                            // Right
                            q = new Point(q.X + 1, q.Y);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                                goto case 3;
                        case 3:
                            // Up
                            q = new Point(q.X, q.Y + 1);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                                goto case 4;
                        case 4:
                            // Down
                            q = new Point(q.X, q.Y - 1);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                            {
                                q = q.add(0,1);
                                break;
                            }

                        default:
                            break;
                    }
                }
                else if (q.X == 0)
                {
                    // Cross
                    double s = allProbs[probIndex(1, q.X, q.Y)];
                    double mr = allProbs[probIndex(2, q.X, q.Y)];
                    double mu = allProbs[probIndex(3, q.X, q.Y)];
                    List<double> probs = new List<double>();
                    probs.Add(s);
                    probs.Add(mu);
                    probs.AddRange(Enumerable.Repeat(mr, 2));
                    int r = distRandom(probs, rand);
                    switch (r)
                    {
                        case 2:
                            // Up
                            q = new Point(q.X, q.Y + 1);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                                goto case 3;
                        case 3:
                            // Left
                            q = new Point(q.X - 1, q.Y);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                                goto case 4;
                        case 4:
                            // Right
                            q = new Point(q.X + 1, q.Y);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                            {
                                q = q.add(-1, 0);
                                break;
                            }
                        default:
                            break;
                    }
                }
                else
                {
                    double s = allProbs[probIndex(1, q.X, q.Y)];
                    double mr = allProbs[probIndex(2, q.X, q.Y)];
                    double mu = allProbs[probIndex(3, q.X, q.Y)];
                    List<double> probs = new List<double>();
                    probs.Add(s);
                    probs.Add(mu);
                    probs.Add(mr);
                    int r = distRandom(probs,rand);
                    switch (r)
                    {
                        case 2:
                            // Up
                            q = new Point(q.X, q.Y + 1);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            goto case 3;
                        case 3:
                            // Right
                            q = new Point(q.X + 1, q.Y);
                            if (!O_p.Contains(evasionStartPoint.add(q)))
                                break;
                            else
                            {
                                q.add(-1, 0);
                                break;
                            }
                        default:
                            break;
                    }
                }
                int xsign, ysign = 0;
                if (p.X < 0)
                {
                    xsign = -1;
                }
                else
                {
                    xsign = 1;
                }
                if (p.Y < 0)
                {
                    ysign = -1;
                }
                else
                {
                    ysign = 1;
                }
                q = new Point(xsign * q.X, ysign * q.Y);
                return q.add(evasionStartPoint);
            }

        }
     
        /// <summary>
        /// makes a group of evaders form a route between two points, with some maximal distance between each two evaders
        /// </summary>
        public class TightRoute
        {
            //float randProb;
            //float dijakstraBeginingProb;
            //float dijakstraHistoryProb;
            //float furthestFromPursuersProb;
            //TightRoute( float RandProb, 
            //            float DijakstraBeginingProb, 
            //            float DijakstraHistoryProb, 
            //            float FurthestFromPursuersProb)
            public TightRoute()
            {
                //this.randProb = RandProb;
                //this.dijakstraBeginingProb = DijakstraBeginingProb;
                //this.dijakstraHistoryProb = DijakstraHistoryProb;
                //this.furthestFromPursuersProb = FurthestFromPursuersProb;
            }
            /// <summary>
            /// NOTE: Assumes route.manLen() isn't larger than maxEvaderManDistance * evaderLocationsInOut.count.
            /// The method tells each evader how to advance, such that finally all evaders will be on the route.
            /// Additinally, it is guarenteed that (finally) it will be possible to travel on the route with
            /// jumps of 'maxEvaderManDistance' from one evader to another (if there 
            /// are too many evaders, they are just guarenteed to be somewhere in the route). If indeed this constraint
            /// is satisfied, method returns 'true'
            /// </summary>
            /// <param name="routeDestination">
            /// points from which transmissions can reach the sink/destination
            /// </param>
            /// <param name="sortedRouteEvaders">
            /// Each tuple is 1-Point index 2-its man distance to route 3-its factor on route on the closest point.
            /// the first sortedRouteEvaders Ceil (route.manLen() / maxEvaderManDistance) are sorted by their factor.
            /// If the method returns true, then these evaders form a legal route (excluding maybe first and last point)
            /// </param>
            /// <returns>
            /// true if there are evaders within distance maxEvaderManDistance that cover the route (after the movement),
            /// false otherwise (note: the first and last points on the route are not necessarily on the start and end of 'route'!)
            /// </returns>
            public bool advanceToRoute(Segment route, 
                                       int maxEvaderManDistance,
                                       HashSet<Point> O_p,
                                       List<Point> evaderLocationsInOut,
                                       out List<Tuple<int, int, float>> sortedRouteEvaders)
            {
                // TODO: 1)move evaders similarly to "SilentCrawl" movement (right now only rand movement is implemented)
                // TODO: 2) consider using bipartite matching instead of current heuristic. The problem with current heuristic is when
                // there are more pursuers than needed, and they are more or less with same distance from the route, but there are two clusters
                // at the begining and end. In this case, we only give good directions to some of the evaders (the closest ones), and the 
                // others, which may get faster, aren't used properly

                sortedRouteEvaders = new List<Tuple<int,int,float>>(); // First is point index, second is man dist, third is closest Point Factor
                for(int i = 0 ; i < evaderLocationsInOut.Count; ++i)
                {
                    Point p = evaderLocationsInOut[i];
                    Point closest;
                    float factor = route.GetClosestPointOnLineSegmentFactor(p,out closest);

                    sortedRouteEvaders.Add(
                        Tuple.Create(i, closest.manDist(p), factor));
                }
                Comparison<Tuple<int, int, float>> distComp = (x, y) => x.Item2.CompareTo(y.Item2);
                Comparison<Tuple<int, int, float>> factorComp = (x, y) => x.Item3.CompareTo(y.Item3);
                sortedRouteEvaders.Sort(distComp);
                
                // as a heuristic, we assume that the more distant an evader is from a route, then it can get to more places with the same amount of rounds. Therefore, we don't care much
                // what destination point they are given, since they are the bottleneck anyway(of when the route is completed)

                bool isLegalRoute = true;
                int neededPoints = (int)Math.Ceiling(((float)route.manLen())) / maxEvaderManDistance;
                float pointDistFactor = ((float)maxEvaderManDistance) / route.manLen();
                sortedRouteEvaders.Sort(0,neededPoints,  Comparer<Tuple<int, int, float>>.Create(factorComp));
                // TODO:  make sure sorts have the right order
                Point opt1, opt2, prevP = evaderLocationsInOut[sortedRouteEvaders[0].Item1];
                for(int i = 0; i < neededPoints; ++i)
                {
                    int pIdx = sortedRouteEvaders[i].Item1;
                    Point chosenP = EvaderCrawl.getSomeAvailablePoint(evaderLocationsInOut[pIdx],route.getPoint(i * pointDistFactor),O_p);
                    
                    evaderLocationsInOut[pIdx] = chosenP;
                    if (isLegalRoute && prevP.manDist(chosenP) <= maxEvaderManDistance)
                        prevP = chosenP;
                    else
                        isLegalRoute = false;
                }
                
                // below we handle the more distant points (We don't care much about them, as the route would probably be completed before they 
                // reach the route anyway)
                for (int i = neededPoints; i < sortedRouteEvaders.Count; ++i)
                {
                    int pIdx = sortedRouteEvaders[i].Item1;
                    evaderLocationsInOut[pIdx] = EvaderCrawl.getSomeAvailablePoint(evaderLocationsInOut[pIdx], route.getPoint(sortedRouteEvaders[i].Item3), O_p);
                }

                return isLegalRoute;
            }
        
        
            // TODO: we also need a method that takes points on route, and insures they have some distribution on it(e.g. uniform)
            // so the route is more robust when losing an evader
        }
    
    }

}
