using GoE.Utils.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE.Utils
{
    /// <summary>
    /// stuff like dijakstra
    /// </summary>
    public static class GraphAlgorithms
    {
        // FIXME: replace point with a general type TNode
        public static class FindShortestPath //where TNode : struct
        {
            public class PointDictionary<T>
            {
                public int Width { get; protected set; }
                public int Height { get; protected set; }
                public PointDictionary(int width, int height, T initValue)
                {
                    Width = width;
                    Height = height;
                    values = AlgorithmUtils.getRepeatingValueList(initValue, width * height);   
                }
                public T this[Point p]
                {
                    get
                    {
                        return values[p.X * Height + p.Y];
                    }
                    set
                    {
                        values[p.X * Height + p.Y] = value;
                    }
                }
                private List<T> values;
            }
            public delegate List<Point> ReachableNodesGetter(Point from);
            public delegate double PathWeightGetter(Point from, Point to);
            private class NodeComparer<T> : IComparer<Tuple<Point, T>> where T : IComparable
            {
                public int Compare(Tuple<Point, T> x, Tuple<Point, T> y)
                {
                    return 4 * x.Item2.CompareTo(y.Item2) +
                           2 * Math.Sign(x.Item1.X.CompareTo(y.Item1.X)) + 
                               Math.Sign(x.Item1.Y.CompareTo(y.Item1.Y));
                    //int cmp =  x.Item2.CompareTo(y.Item2);
                    //return cmp != 0? cmp : (x.Item1.X * 10000 + x.Item1.Y) - (y.Item1.X * 10000 + y.Item1.Y);
                }
            }

            /// <summary>
            ///  populates 'dist' distances of each point in the graph from 'src'
            ///  (using dijakstra)
            /// </summary>
            /// <typeparam name="Point"></typeparam>
            /// <param name="src"></param>
            /// <param name="dest">
            /// initialized with an empty dictionary i.e.
            /// all values are -1
            /// will be populated with distances of each point from 'src'
            /// </param>
            /// <returns>
            /// 
            /// </returns>
            public static void getAllDistances(
                IEnumerable<Point> allNodes,
                Point src,
                ReachableNodesGetter expander,
                PointDictionary<int> dist)
            {
                const int UNINITIALIZED_VALUE = -1;

                HashSet<Point> Q = new HashSet<Point>();
                //Dictionary<Point, double> dist = new Dictionary<Point, double>();
                SortedSet<Tuple<Point, int>> distQueued = new SortedSet<Tuple<Point, int>>(new NodeComparer<int>());

                foreach (Point c in allNodes)
                {
                    distQueued.Add(Tuple.Create(c, int.MaxValue));
                    dist[c] = int.MaxValue;
                    Q.Add(c);
                }

                distQueued.Remove(Tuple.Create(src, dist[src]));
                distQueued.Add(Tuple.Create(src, 0));
                dist[src] = 0;

                Point u = src;
                while (Q.Count > 0)
                {
                    Q.Remove(u);
                    distQueued.Remove(Tuple.Create(u, dist[u]));
                    var neighbors = expander(u);
                    foreach (var v in neighbors)
                    {
                        var alt = dist[u] + 1;

                        //if (dist[v] != UNINITIALIZED_VALUE)
                          //  continue;

                        if (alt < dist[v])
                        {
                            distQueued.Remove(Tuple.Create(v, dist[v]));
                            distQueued.Add(Tuple.Create(v, alt));
                            dist[v] = alt;
                        }
                    }

                    if (distQueued.Count > 0)
                        u = distQueued.Min.Item1;
                }

                
            }
            /// <summary>
            /// finds a path with maximal utility
            /// </summary>
            /// <typeparam name="Point"></typeparam>
            /// <param name="src"></param>
            /// <param name="dest"></param>
            /// <param name="expander"></param>
            /// <param name=""></param>
            /// <param name="illegalNode">
            /// value that no other node has (used as "null")
            /// </param>
            /// <returns></returns>
            public static List<Point> findShortestPath(IEnumerable<Point> allNodes, 
                Point src, 
                Point dest, 
                ReachableNodesGetter expander, 
                PathWeightGetter evaluator, 
                Point illegalNode)
            {
                HashSet<Point> Q = new HashSet<Point>();
                Dictionary<Point, double> dist = new Dictionary<Point, double>();
                SortedSet<Tuple<Point, double>> distQueued = new SortedSet<Tuple<Point, double>>(new NodeComparer<double>());
                Dictionary<Point, Point> prev = new Dictionary<Point, Point>();

                foreach(Point c in allNodes)
                {
                    distQueued.Add(Tuple.Create(c, double.PositiveInfinity));
                    dist[c] = double.PositiveInfinity;
                    prev[c] = illegalNode;
                    Q.Add(c);
                }

                distQueued.Remove(Tuple.Create(src, dist[src]));
                distQueued.Add(Tuple.Create(src, 0.0));
                dist[src] = 0;

                Point u = src;
                while (Q.Count > 0)
                {
                    Q.Remove(u);
                    distQueued.Remove(Tuple.Create(u, dist[u]));
                    var neighbors = expander(u);
                    foreach (var v in neighbors)
                    {
                        var alt = dist[u] + evaluator(u, v);
                        
                        if (!dist.Keys.Contains(v))
                            continue;

                        if (alt < dist[v])
                        {
                            distQueued.Remove(Tuple.Create(v, dist[v]));
                            distQueued.Add(Tuple.Create(v, alt));
                            dist[v] = alt;
                            prev[v] = u;
                        }
                    }

                    if (distQueued.Count > 0)
                        u = distQueued.Min.Item1;
                }
                
                List<Point> S = new List<Point>();
                u = dest;
                while (!u.Equals(illegalNode) && prev.ContainsKey(u))
                {
                    S.Add(u);
                    u = prev[u];
                }
                S.Reverse();
                return S;

            }
        }
        public static class LinearAssignment
        {

            public delegate int CostGetter(int i, int j);

            /// <summary>
            /// finds the assignment with highest utility sum
            /// </summary>
            /// <param name="C">
            /// an N X N utility matrix (i.e. amount of tasks is equal to agents), 
            /// where C[i,j] tells the utility of assigning agent j to task i
            /// </param>
            /// <returns>
            /// returns an assignment array, where if arr[i] = j, then the best agent for task i is j
            /// </returns>
            public static List<int> auction(CostGetter C, int N)
            {

                List<int> assignment = Utils.Algorithms.AlgorithmUtils.getRepeatingValueList(int.MaxValue, N);
                List<double> prices = Utils.Algorithms.AlgorithmUtils.getRepeatingValueList(1.0, N);
                double epsilon = 1.0;
                int iter = 1;

                while (epsilon > 1.0 / N)
                {
                    for (int i = 0; i < assignment.Count; i++)
                        assignment[i] = int.MaxValue;

                    while (assignment.Contains(int.MaxValue))
                        auctionRound(assignment, prices, C, epsilon);

                    epsilon = epsilon * .25;
                }


                //clock_t end = clock();


                /* End Time */
                //var t2 = std.chrono.high_resolution_clock.now();
                //double timing = std.chrono.duration_cast<std.chrono.milliseconds>(t2 - t1).count();
                //double time = (double)(end - start) / CLOCKS_PER_SEC * 1000.0;

                //Console.Write("Num Iterations:\t");
                //Console.Write(iter);
                //Console.Write("\n");
                //Console.Write("Total time:\t");
                //Console.Write(timing / 1000.0);
                //Console.Write("\n");
                //Console.Write("Total CPU time:\t");
                //Console.Write(time);
                //Console.Write("\n");

                //if (false)
                //{
                //    Console.Write("\n");
                //    Console.Write("\n");
                //    Console.Write("Solution: ");
                //    Console.Write("\n");
                //    for (int i = 0; i < assignment.Count; i++)
                //    {
                //        Console.Write("Person ");
                //        Console.Write(i);
                //        Console.Write(" gets object ");
                //        Console.Write(assignment[i]);
                //        Console.Write("\n");
                //    }
                //}

                return assignment;
            }

            /// <summary>
            /// serves auction
            /// </summary>
            private static void auctionRound(List<int> assignment, List<double> prices, CostGetter C, double epsilon)
            {

                ///* Prints the assignment and price vectors */
                //if (false)
                //{
                //    Console.Write("\n");
                //    Console.Write("Assignment: \t\t");
                //    printVec(assignment);
                //    Console.Write("prices: \t\t");
                //    printVec(prices);
                //    Console.Write("\n");
                //}

                int N = prices.Count;

                /*
                These are meant to be kept in correspondance such that bidded[i]
                and bids[i] correspond to person i bidding for bidded[i] with bid bids[i]
                */
                List<int> tmpBidded = new List<int>();
                List<double> tmpBids = new List<double>();
                List<int> unAssig = new List<int>();

                /* Compute the bids of each unassigned individual and store them in temp */
                for (int i = 0; i < assignment.Count; i++)
                {
                    if (assignment[i] == int.MaxValue)
                    {
                        unAssig.Add(i);

                        /*
                        Need the best and second best value of each object to this person
                        where value is calculated row_{j} - prices{j}
                        */
                        double optValForI = -int.MaxValue;
                        double secOptValForI = -int.MaxValue;
                        int optObjForI = -1;
                        int secOptObjForI;
                        for (int j = 0; j < N; j++)
                        {
                            double curVal = C(i, j) - prices[j];
                            if (curVal > optValForI)
                            {
                                secOptValForI = optValForI;
                                secOptObjForI = optObjForI;
                                optValForI = curVal;
                                optObjForI = j;
                            }
                            else if (curVal > secOptValForI)
                            {
                                secOptValForI = curVal;
                                secOptObjForI = j;
                            }
                        }

                        /* Computes the highest reasonable bid for the best object for this person */
                        double bidForI = optValForI - secOptValForI + epsilon;

                        /* Stores the bidding info for future use */
                        tmpBidded.Add(optObjForI);
                        tmpBids.Add(bidForI);
                    }
                }

                /*
                Each object which has received a bid determines the highest bidder and
                updates its price accordingly
                */
                for (int j = 0; j < N; j++)
                {
                    List<int> indices = getIndicesWithVal(tmpBidded, j);
                    if (indices.Count != 0)
                    {
                        /* Need the highest bid for object j */
                        double highestBidForJ = -int.MaxValue;
                        int i_j = -1;
                        for (int i = 0; i < indices.Count; i++)
                        {
                            double curVal = tmpBids[indices[i]];
                            if (curVal > highestBidForJ)
                            {
                                highestBidForJ = curVal;
                                i_j = indices[i];
                            }
                        }

                        /* Find the other person who has object j and make them unassigned */
                        for (int i = 0; i < assignment.Count; i++)
                        {
                            if (assignment[i] == j)
                            {
                                //if (false)
                                //{
                                //    Console.Write("Person ");
                                //    Console.Write(unAssig[i_j]);
                                //    Console.Write(" was assigned object ");
                                //    Console.Write(i);
                                //    Console.Write(" but it will now be unassigned");
                                //    Console.Write("\n");
                                //}
                                assignment[i] = int.MaxValue;
                                break;
                            }
                        }
                        //if (false)
                        //{
                        //    Console.Write("Assigning object ");
                        //    Console.Write(j);
                        //    Console.Write(" to person ");
                        //    Console.Write(unAssig[i_j]);
                        //    Console.Write("\n");
                        //}

                        /* Assign oobject j to i_j and update the price vector */
                        assignment[unAssig[i_j]] = j;
                        prices[j] = prices[j] + highestBidForJ;
                    }
                }
            }

            /* Returns a vector of indices from v which have the specified value val */
            private static List<int> getIndicesWithVal(List<int> v, int val)
            {
                List<int> res = new List<int>();
                for (int i = 0; i < v.Count; i++)
                {
                    if (v[i] == val)
                    {
                        res.Add(i);
                    }
                }
                return res;
            }
        }
    }
}
