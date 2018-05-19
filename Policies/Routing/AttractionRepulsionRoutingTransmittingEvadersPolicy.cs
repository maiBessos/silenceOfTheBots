using AForge.Genetic;
using GoE.AppConstants;
using GoE.AppConstants.Policies.Routing;
using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using GoE.Utils;
using GoE.Utils.Algorithms;
using GoE.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Policies
{
    /// <summary>
    /// similar to AttractionRepulsionEvadersPolicy, but also allows evaders to transmit to distance r_e
    /// </summary>
    public class AttractionRepulsionTransmittingEvadersPolicy : AFrontsGridRoutingEvadersPolicy
    {
        public const double MINIMAL_PENALTY = 0.01; // fixme a bit dirty

        protected struct PointState
        {
            public bool isOccupied;
            public double capturedPenalty; // if an e-bot was previously captured here
            public double transmittedPenalty; // if an e-bot transmitted from this point
            public int forwardConnectivity; // if isOccupied=true, this tells how many other nodes with >=x are connected
            public int backwardConnectivity; // if isOccupied=true, this tells how many other nodes with <x are connected
            
            // TODO: note: if both nodes are connected and have similar x, this is a bit weird: it's not really a forward connection,
            // but if one is uncovered it still hurts the other (i.e. there shouldn't be a bonus, but ehre should be a penalty).
            // however, this may make the route slightly more robust, so maybe the bonus is justified.
        }

        protected int remainingEvadersToPlace;
        protected PointState[,] statePerPoint; // state per [x,y]
        protected GridGameGraph graph;
        protected FrontsGridRoutingGameParams param;
        protected ThreadSafeRandom myRand;

        double currentTotalSum = 1.0; // helps dealing with accumulated inaccuracies

        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.AddRange(ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AttractionRepulsionRoutingEvaders)));
                res.AddRange(ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AttractionRepulsionRoutingTransmittingEvaders)));
                res.Remove(AttractionRepulsionRoutingEvaders.OPTIMIZE_ENERGY_CALCULATION);
                return res;
            }
        }

        public enum RoutingHeuristic : int 
        {
            Random = 0,
            MinimizeRoutersEnergy = 1
        }
        CommunicationGraph connectedPoints;
        public override CommunicationGraph communicate()
        {
            if (energyPerPoint == null)
                return new CommunicationGraph();


            Point deadEndMarker = new Point(-4, -4); // all nodes are connected to dead end. if the minimal path uses this edge, then there is no real way
            Point destMarker = new Point(-2, -2); // a node that marks "destination"
            Point currentSourcePoint = CommunicationGraph.SourcePoint;
            GraphAlgorithms.FindShortestPath.ReachableNodesGetter expander =
                (Point from) =>
                {

                    if (from == destMarker)
                        return new List<Point>();

                    List<Point> res;
                    if (from == deadEndMarker)
                    {
                        res = new List<Point>();
                        res.Add(destMarker);
                        res.Add(CommunicationGraph.SourcePoint);
                        return res; // lets you reach the sink, so the path will always be feasible
                    }
                    if(from == currentSourcePoint)
                    {
                        res = new List<Point>(connectedPoints.getForwardEdgesOrDest(from, deadEndMarker));
                        res.Add(deadEndMarker);
                        return res;
                    }

                    return connectedPoints.getForwardEdgesOrDest(from, destMarker);
                    //var back = connectedPoints.getBackwardEdges(from);
                    //var forw = connectedPoints.getForwardEdgesOrDest(from, destMarker);
                    //// can't use the real edge list. instead, duplicate the list, and optinally add deadEndMarker, if needed,
                    //res = new List<Point>(back);
                    //res.AddRange(forw);
                    //if (forw.Count == 0 || back.Count == 0)
                    //{
                    //    res.Add(deadEndMarker);
                    //    res.Add(CommunicationGraph.SourcePoint);
                    //}
                    //return res;
                };

            GraphAlgorithms.FindShortestPath.PathWeightGetter evalFunc = null;
            switch(comHeuristic)
            {
                case RoutingHeuristic.MinimizeRoutersEnergy:
                    evalFunc = (Point from, Point to) => 
                    {
                        if (to == deadEndMarker)
                            return double.MaxValue;
                        if (to == destMarker || to == CommunicationGraph.SourcePoint)
                            return 0;
                        return energyPerPoint[to.X, to.Y];
                    };
                    break;
                case RoutingHeuristic.Random:
                    evalFunc = (Point from, Point to) => 
                    {
                        if (to == deadEndMarker)
                            return double.MaxValue;
                        if (to == destMarker)
                            return 0;
                        return myRand.NextDouble();
                    };
                    break;
            }
            
            var allActiveEbots = connectedPoints.getAllTransmittingPoints();
            allActiveEbots.Add(deadEndMarker);
            allActiveEbots.Add(destMarker);
            // note: after setEnergyOpt() runs, all energies in energyPerPoint are non-negative. Since we want to minimize the energy, we
            // refer to energy as path weight, and find minimal energy/weight path
            var transmittingPoints = GraphAlgorithms.FindShortestPath.findShortestPath(allActiveEbots, CommunicationGraph.SourcePoint, destMarker,
                expander,
                evalFunc,
                new Point(-3,-3));

            if(transmittingPoints.Contains(deadEndMarker))
                return new CommunicationGraph(); // can't generate a real route
            
            allActiveEbots.RemoveAt(allActiveEbots.Count - 1); // remove the 'deadEndMarker'
            
            // we keep destMarker in transmittingPoints the later part with backup nodes can target this again as the last destination

            // before adding redundant nodes to main route (to strenghen it), we filter out nodes that are too distant
            var possibleRedundantNodes = new List<Point>(); // find points that are not in transmittingPoints, but are in distance  <= maxTransmissionRedundancyMainRouteDistanceFactor from one of them
            foreach (var e in allActiveEbots)
            {
                if (transmittingPoints.Contains(e))
                    continue;
                foreach (var p in transmittingPoints)
                    if (p.manDist(e) <= maxTransmissionRedundancyMainRouteDistanceFactor)
                    {
                        possibleRedundantNodes.Add(e);
                        break;
                    }
            }

            
            possibleRedundantNodes.Add(deadEndMarker);

            int mainRouteLen = transmittingPoints.Count;
            int rndSrc = 1 + myRand.Next() % Math.Max(1,(mainRouteLen - 2));
            int rndDest = rndSrc + 1 + myRand.Next() % Math.Max((mainRouteLen - (rndSrc+1)),1);
            for (int r = 0; r < maxRedundantRoutes; ++r)
            {
                List<Point> tmpNodes = new List<Point>(possibleRedundantNodes); 
                tmpNodes.Add(transmittingPoints[rndSrc]);
                //tmpNodes.Add(transmittingPoints[rndDest]);
                // searches a route between two random points on the main route. If found, we add them 
                currentSourcePoint = transmittingPoints[rndSrc]; // affects expander
                destMarker = transmittingPoints[rndDest]; // affects expander

                var redundantPath = GraphAlgorithms.FindShortestPath.findShortestPath(tmpNodes,
                    transmittingPoints[rndSrc],
                    transmittingPoints[rndDest],
                    expander,
                    evalFunc,
                    new Point(-3, -3));

                if (redundantPath.Count > 0)
                {
                    redundantPath.RemoveAt(redundantPath.Count - 1); // remove the lastly added 'destMarker'
                    if (!redundantPath.Contains(deadEndMarker))
                        transmittingPoints.AddRange(redundantPath);
                }

                if (rndDest >= mainRouteLen-2) // since next rndSrc is > than current rndDest , reaching mainRouteLen-2 or mainRouteLen-1 means we stop
                    break; // in this case, we make less than maxRedundantRoutes routes (instead, the routes length is greater)
                rndSrc = rndDest + 1;
                rndDest = rndSrc + 1 + myRand.Next() % Math.Max((mainRouteLen - (rndSrc + 1)), 1);
            }

            transmittingPoints[mainRouteLen-1] = transmittingPoints.Last(); // remove the first added'destMarker' (we wanted to keep it for the prev loop)
            transmittingPoints.RemoveAt(transmittingPoints.Count - 1);

            foreach (var p in transmittingPoints)
                if (p.X > 0 && p.Y > 0) // make sure it's not sink/dest/deadend node
                    statePerPoint[p.X, p.Y].transmittedPenalty = transmissionPenalty;

            return new CommunicationGraph(ref transmittingPoints, param.r_e, param.r_e-1, (int)(graph.WidthCellCount - param.r_e));
            
            //return connectedPoints.getReduandantComGraph(
            //    transmittingPoints,
            //    (int)Math.Round(transmittingPoints.Count * maxTransmissionRedundancyFactor),
            //    (Point n) => { energyPerPoint[n.X, n.Y]; });
            
        }


        int unoccupiedPoints;
        
        /// <summary>
        /// tells the enrgy that the three points (s.x,s.y+opEventDistance),(s.x,s.y-opEventDistance) and (s.x+opEventDistance,s.y)  feel 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private double getPointEnergyForward(PointState s)
        {
            double occupiedBonusPenalty = (s.isOccupied) ? (optForwardConnectivity - s.forwardConnectivity) : (0.0); // either bonus or a penalty
            double penalty = s.capturedPenalty + s.transmittedPenalty;
            return occupiedBonusPenalty - penalty;
        }
        /// <summary>
        /// tells the enrgy that the point (s.x-opEventDistance,s.y) feels
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private double getPointEnergyBackward(PointState s)
        {
            double occupiedBonusPenalty = (s.isOccupied) ? (optBackConnectivity - s.backwardConnectivity) : (0.0); // either bonus or a penalty
            double penalty = s.capturedPenalty + s.transmittedPenalty;
            return occupiedBonusPenalty - penalty;
        }

        // tells the direct energy a source point feels
        // FIXME: right now, each event creates energy in 4 points around it (up,left,right down)
        // and it's a fair approximation, but in practice we want energy in the entire square/diamond that
        // surrounds the event point. this can be done efficiently by registering the added energy added
        // in start/end points over diagonal lines in the grid, then with one pass over the grid we can add
        // the energy to the correct points
        private double getAffectingEnergy(int sourceX, int sourceY)
        {
            double sum = 0;
            if (sourceX + optEventDistance < graph.WidthCellCount)
                sum += getPointEnergyBackward(statePerPoint[sourceX + optEventDistance, sourceY]);

            if (sourceX - optEventDistance >= 0)
                sum += getPointEnergyForward(statePerPoint[sourceX - optEventDistance, sourceY]);

            if (sourceY + optEventDistance < graph.HeightCellCount)
                sum += getPointEnergyForward(statePerPoint[sourceX, sourceY + optEventDistance]);

            if (sourceY - optEventDistance >= 0)
                sum += getPointEnergyForward(statePerPoint[sourceX, sourceY - optEventDistance]);

            return sum;
        }

        public double setEnergy(out double[,] valuePerPoint)
        {
            double totalEnergy = 0;
            valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    valuePerPoint[x, y] = 0;
                    if (statePerPoint[x, y].isOccupied)
                        continue;


                    for (int xs = 0; xs < graph.WidthCellCount; ++xs)
                        for (int ys = 0; ys < graph.HeightCellCount; ++ys)
                        {
                            double dist = Math.Abs(xs - x) +
                                          Math.Abs(ys - y) * this.xAxisDistBias;
                            float occupiedBonus = (statePerPoint[xs, ys].isOccupied) ? (1) : (0);

                            valuePerPoint[x, y] += (occupiedBonus - statePerPoint[xs, ys].capturedPenalty) / (1 + dist);
                        }

                    var xvals = new int[2] { -1, (int)graph.WidthCellCount };
                    // we also pretend the first and last column are "occupied"
                    foreach (var xval in xvals)
                        for (int ys = 0; ys < graph.HeightCellCount; ++ys)
                        {
                            double dist = Math.Abs(xval - x) + Math.Abs(ys - y) * this.xAxisDistBias;
                            valuePerPoint[x, y] += phantomColumnsWeight / (1 + dist);
                        }

                    valuePerPoint[x, y] = Math.Max(0, valuePerPoint[x, y]);
                    valuePerPoint[x, y] = (double)Math.Pow(valuePerPoint[x, y], energyPowerFactor);
                    totalEnergy += valuePerPoint[x, y];
                }

            return totalEnergy;
        }
        
        public double setEnergyOpt(out double[,] valuePerPoint)
        {

            double[] totalEnergyForTargetColumn = new double[graph.WidthCellCount];
            double[] totalEnergyForTargetRow = new double[graph.HeightCellCount];
            
            List<double> valSumPerColumn = new List<double>(); // for each target column - sum all source rows that affect it directly
            List<double> valSumPerRow = new List<double>();
          
            #region populate totalSum, valSumPerColumn, accumSumFromColumn, accumSumToColumn
            for (int sourceColIdx = 0; sourceColIdx < graph.WidthCellCount; ++sourceColIdx)
            {
                double sum = 0;
                for (int rowIdx = 0; rowIdx < graph.HeightCellCount; ++rowIdx)
                    sum += getAffectingEnergy(sourceColIdx, rowIdx);
                valSumPerColumn.Add(sum);
            }

            //// we pretend to have 2 phantom columns (in each extreme, graph.HeightCellCount points with weight 'phantomColumnsWeight').
            //// Every point in distance r_e from these columns gets bonus energy, since it's the best place to add an ebot:
            valSumPerColumn[(param.r_e - 1)] += phantomColumnsWeight;
            valSumPerColumn[(int)graph.WidthCellCount - param.r_e] += phantomColumnsWeight;


            #endregion
            #region populate valSumPerRow,accumSumFromRow,accumSumToRow
            for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
            {
                double sum = 0;
                for (int colIdx = 0; colIdx < graph.WidthCellCount; ++colIdx)
                    sum += getAffectingEnergy(colIdx, sourceRowIdx);
                valSumPerRow.Add(sum);
            }
     
            #endregion

            #region calculate total energy for different column/row gradients
            for (int targetColidx = 0; targetColidx < graph.WidthCellCount; ++targetColidx)
            {
                // if we calculate energy for points in column targetColidx, then the further we go to 
                // more distant columns, the energy diminishes in proportion to xDist
                for (int sourceColidx = 0; sourceColidx < graph.WidthCellCount; ++sourceColidx)
                {
                    int xDist = Math.Abs(sourceColidx - targetColidx);
                    totalEnergyForTargetColumn[targetColidx] += valSumPerColumn[sourceColidx] / (1 + xDist);
                }
            }
            
            for (int targetRowIdx = 0; targetRowIdx < graph.HeightCellCount; ++targetRowIdx)
            {
                for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
                {
                    int yDist = Math.Abs(sourceRowIdx - targetRowIdx);
                    totalEnergyForTargetRow[targetRowIdx] += valSumPerRow[sourceRowIdx] / (1 + yDist);
                }
            }
            #endregion

            double totalEnergy = 0;
            valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
            double minEnergy = double.MaxValue;
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    if (statePerPoint[x, y].isOccupied)
                    {
                        valuePerPoint[x, y] = 0;
                        continue;
                    }

                    valuePerPoint[x, y] = totalEnergyForTargetRow[y] * this.xAxisDistBias +
                                          totalEnergyForTargetColumn[x] ;

                    //// we pretend to have 2 phantom columns (in each extreme, graph.HeightCellCount points with weight 'phantomColumnsWeight').
                    //// Every point in distance less than r_e from these columns gets bonus energy:
                    //double dist = Math.Abs((param.r_e-1) - x);
                    //valuePerPoint[x, y] += graph.HeightCellCount * phantomColumnsWeight / (this.xAxisDistBias * (1 + dist));

                    //dist = Math.Abs((graph.WidthCellCount - param.r_e) - x);
                    //valuePerPoint[x, y] += graph.HeightCellCount * phantomColumnsWeight / (this.xAxisDistBias * (1 + dist));


                    double energySign = (valuePerPoint[x, y] > 0) ? (1) : (-1);
                    valuePerPoint[x, y] = energySign * MathEx.PowInt(Math.Abs(valuePerPoint[x, y]), (int)energyPowerFactor);
                    minEnergy = Math.Min(minEnergy, valuePerPoint[x, y]);
                }

            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    if (statePerPoint[x, y].isOccupied)
                        continue;

                    valuePerPoint[x, y] -= minEnergy;
                    totalEnergy += valuePerPoint[x, y];

                }

            return totalEnergy;
        }

        double[,] energyPerPoint;
        public override List<Point> getNextStep()
        {
            if (remainingEvadersToPlace == 0 || unoccupiedPoints == 0)
                return new List<Point>();

            List<Point> res = new List<Point>();

            // update penalties:
            for (int xs = 0; xs < graph.WidthCellCount; ++xs)
                for (int ys = 0; ys < graph.HeightCellCount; ++ys)
                {
                    statePerPoint[xs, ys].transmittedPenalty = (float)(statePerPoint[xs, ys].transmittedPenalty * transmissionPenaltyDiscount);
                    statePerPoint[xs, ys].capturedPenalty = (float)(statePerPoint[xs, ys].capturedPenalty * capturedEbotPenaltyDiscount);

                    if (statePerPoint[xs, ys].capturedPenalty < MINIMAL_PENALTY)
                        statePerPoint[xs, ys].capturedPenalty = 0;

                    if (statePerPoint[xs, ys].transmittedPenalty < MINIMAL_PENALTY)
                        statePerPoint[xs, ys].transmittedPenalty = 0;

                    if (statePerPoint[xs, ys].isOccupied)
                        res.Add(new Point(xs, ys)); // since res sould contain all ebots in this round
                }


            double totalEnergy;
            

            if (useOptEnergyAlg)
                totalEnergy = setEnergyOpt(out energyPerPoint);
            else
                totalEnergy = setEnergy(out energyPerPoint);

            // choose where to add each ebot
            while (remainingEvadersToPlace > 0 && unoccupiedPoints > 0)
            {
                --remainingEvadersToPlace;
                --unoccupiedPoints;

                double choice = myRand.NextDouble() * totalEnergy;

                int cx, cy = 0;
                bool breakOuter = false;
                for (cx = 0; cx < graph.WidthCellCount && !breakOuter; ++cx)
                    for (cy = 0; cy < graph.HeightCellCount; ++cy)
                    {
                        choice -= energyPerPoint[cx, cy];
                        if (choice < 0)
                        {
                            --cx; // before outer loop exits, it advances cx
                            breakOuter = true;
                            break;
                        }
                    }
                if (cx == graph.WidthCellCount)
                {
                    cx = myRand.Next() % ((int)graph.WidthCellCount - 1);
                    cy = myRand.Next() % ((int)graph.HeightCellCount - 1);
                }
                statePerPoint[cx, cy].isOccupied = true;

                #region add point to connectivity graphs
                Point newPoint = new Point(cx, cy);
                res.Add(newPoint);
                var newConnections = activeEbots.findPointsWithinManDistance(newPoint, param.r_e);
                activeEbots.addPoint(newPoint);
                connectedPoints.addPoint(newPoint, param.r_e-1, (int)(graph.WidthCellCount - param.r_e));

                foreach (var c in newConnections)
                {
                    if(c.X > newPoint.X)
                    {
                        ++statePerPoint[c.X, c.Y].backwardConnectivity;
                        ++statePerPoint[newPoint.X, newPoint.Y].forwardConnectivity;
                        connectedPoints.addEdge(newPoint, c);
                    }
                    else if (c.X < newPoint.X)
                    {
                        ++statePerPoint[c.X, c.Y].forwardConnectivity;
                        ++statePerPoint[newPoint.X, newPoint.Y].backwardConnectivity;
                        connectedPoints.addEdge(c,newPoint);
                    }
                    else // if x is equal:
                    {
                        ++statePerPoint[c.X, c.Y].forwardConnectivity;
                        ++statePerPoint[newPoint.X, newPoint.Y].forwardConnectivity;
                        connectedPoints.addEdge(newPoint, c);
                        connectedPoints.addEdge(c, newPoint);
                    }
                }
                #endregion
            }

            #if DEBUG
            if(pgui.hasBoardGUI())
                addEnergyGUI();
            #endif

            return res;
        }

        void addEnergyGUI()
        {
            double maxEnergy = -double.MaxValue;
            double minEnergy = double.MaxValue;
            

            int markCount = 10;
            List<Point> overConnectionPenaltyForward = new List<Point>();
            List<Point> overConnectionPenaltyBackward = new List<Point>();
            List<Point> transmittedPenalty = new List<Point>();
            List<Point> capturedPenalty = new List<Point>();
            List<List<Point>> energyByLevel = AlgorithmUtils.getRepeatingValueList(markCount, ()=> { return new List<Point>(); } );
            
            var avg = new List<Point>();
            for (int cx = 0; cx < graph.WidthCellCount; ++cx)
                for (int cy = 0; cy < graph.HeightCellCount; ++cy)
                {
                    minEnergy = Math.Min(minEnergy, energyPerPoint[cx, cy]);
                    maxEnergy = Math.Max(maxEnergy, energyPerPoint[cx, cy]);

                    if (statePerPoint[cx, cy].capturedPenalty > 0)
                        capturedPenalty.Add(new Point(cx, cy));
                    if (statePerPoint[cx, cy].transmittedPenalty > 0)
                        transmittedPenalty.Add(new Point(cx, cy));


                    double occupiedBonusPenaltyBack = (statePerPoint[cx, cy].isOccupied) ? (optBackConnectivity - statePerPoint[cx, cy].backwardConnectivity) : (0.0); // either bonus or a penalty
                    double occupiedBonusPenaltyForward = (statePerPoint[cx, cy].isOccupied) ? (optForwardConnectivity - statePerPoint[cx, cy].forwardConnectivity) : (0.0); // either bonus or a penalty
                    if (occupiedBonusPenaltyBack < 0)
                        overConnectionPenaltyBackward.Add(new Point(cx, cy));
                    if (occupiedBonusPenaltyForward < 0)
                        overConnectionPenaltyForward.Add(new Point(cx, cy));
                }
            
            for (int cx = 0; cx < graph.WidthCellCount; ++cx)
                for (int cy = 0; cy < graph.HeightCellCount; ++cy)
                {
                    for (int i = 0; i < markCount; ++i)
                        if (energyPerPoint[cx, cy] - minEnergy <= (maxEnergy - minEnergy) * (((float)i)/(markCount-1)) )
                        {
                            energyByLevel[i].Add(new Point(cx, cy));
                            break;
                        }

                    //if (energyPerPoint[cx, cy] - minEnergy < (maxEnergy - minEnergy) / 3)
                    //    low.Add(new Point(cx, cy));
                    //else if (energyPerPoint[cx, cy] - minEnergy < 2 * ((maxEnergy - minEnergy) / 3))
                    //    avg.Add(new Point(cx, cy));
                    //else
                    //    top.Add(new Point(cx, cy));

                }

            Dictionary<string, List<Point>> markers = new Dictionary<string, List<Point>>();
            for (int i = 0; i < markCount; ++i)
                markers[((100.0 * (1+i)) / markCount).ToString() + "% energy"] = energyByLevel[i];
            markers["CapturedPenalty"] = capturedPenalty;
            markers["TransmittedPenalty"] = transmittedPenalty;
            markers["ForwardConnectionPenalty"] = overConnectionPenaltyForward;
            markers["BackwardConnectionPenalty"] = overConnectionPenaltyBackward;
            pgui.markLocations(markers.toPointFMarkings());

        }

        /// <summary>
        /// serves getNextStepOpt()
        /// </summary>
        private class SamplesPerSourceColIdx
        {
            //public List<List<double>> samplesPerSourceCol =new List<List<double>>(); // for each source col index, we have several samples, from source_row_idx = 0, to source_row_idx = Graph.Height
            public List<double> samplesPerSourceColSum = new List<double>(); // sums all samples from the same sourceCol, so calculation of target point is faster

        }

        CoarsePointGrid activeEbots; // speeds up connectivity checks
        bool useOptEnergyAlg;
        double energyPowerFactor, capturedEbotPenaltyDiscount, capturedPenalty;
        double xAxisDistBias, phantomColumnsWeight;

        int optEventDistance; // if opEventDistance = 1, then it's like previous policy. if larger, then each PointState affects the energy of points with opEventDistance distance to all directions
        double optBackConnectivity, optForwardConnectivity;
        double transmissionPenalty, transmissionPenaltyDiscount;
        double maxTransmissionRedundancyMainRouteDistanceFactor;
        int maxRedundantRoutes;
        RoutingHeuristic comHeuristic;

        IPolicyGUIInputProvider pgui;
        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            this.energyPerPoint = null;
            this.connectedPoints = new CommunicationGraph();
            this.myRand = new ThreadSafeRandom();
            this.graph = (GridGameGraph)G;
            this.param = prm;
            this.pgui = pgui;
            activeEbots = new CoarsePointGrid(new Rectangle(0, 0, (int)graph.WidthCellCount, (int)graph.HeightCellCount), 
                                              (int)Math.Round(param.r_e * 1.5), 
                                              (int)Math.Round(param.r_e * 1.5));

            maxRedundantRoutes = int.Parse(AttractionRepulsionRoutingTransmittingEvaders.MAX_REDUNDANT_ROUTES.tryRead(policyParams));

            comHeuristic = (RoutingHeuristic)int.Parse(
                AttractionRepulsionRoutingTransmittingEvaders.COMMUNICATION_HEURISTIC_CODE.tryRead(policyParams));

            maxTransmissionRedundancyMainRouteDistanceFactor = param.r_e * double.Parse(
                AttractionRepulsionRoutingTransmittingEvaders.MAX_TRANSMISSION_REDUDANCY_MAIN_ROUTE_DISTANCE_FACTOR.tryRead(policyParams));
            optBackConnectivity = double.Parse(
                AttractionRepulsionRoutingTransmittingEvaders.OPTIMAL_BACKWARD_CONNECTIVITY.tryRead(policyParams));

            // we arbitrarily decided that forward is always <= than backwards, since it's symmetric and no need for duplicate options.
            // additionally, when evolving pbot vs ebot strategies, this may cause a loop where both sides swap bias
            optForwardConnectivity = optBackConnectivity * double.Parse(
                AttractionRepulsionRoutingTransmittingEvaders.OPTIMAL_FORWARD_CONNECTIVITY.tryRead(policyParams));
            optEventDistance = (int)Math.Round(
                param.r_e * double.Parse(AttractionRepulsionRoutingTransmittingEvaders.OPTIMAL_EVENT_DISTANCE_FACTOR.tryRead(policyParams)));
            transmissionPenalty =
                graph.WidthCellCount *
                double.Parse(AttractionRepulsionRoutingTransmittingEvaders.TRANSMISSION_INITIAL_PENALTY_FACTOR.tryRead(policyParams));
            transmissionPenaltyDiscount = double.Parse(AttractionRepulsionRoutingTransmittingEvaders.TRANSMISSION_PENALTY_DISCOUNT_FACTOR.tryRead(policyParams));
            
            
            phantomColumnsWeight =
                double.Parse(AttractionRepulsionRoutingEvaders.PHANTOM_COLUMN_WEIGHT.tryRead(policyParams));

            xAxisDistBias = double.Parse(
                AttractionRepulsionRoutingEvaders.X_AXIS_DIST_BIAS.tryRead(policyParams));

            this.useOptEnergyAlg =
                AttractionRepulsionRoutingEvaders.OPTIMIZE_ENERGY_CALCULATION.tryRead(policyParams) == "1";
            this.energyPowerFactor = double.Parse(AttractionRepulsionRoutingEvaders.ENERGY_POWER_FACTOR.tryRead(policyParams));

            this.capturedEbotPenaltyDiscount =
                double.Parse(AttractionRepulsionRoutingEvaders.PURSUERS_HIT_PENALTY_DISCOUNT_FACTOR.tryRead(policyParams));

            this.capturedPenalty =
                graph.WidthCellCount *
                double.Parse(AttractionRepulsionRoutingEvaders.PURSUERS_HIT_INITIAL_PENALTY_FACTOR.tryRead(policyParams));



            unoccupiedPoints = (int)(graph.WidthCellCount * graph.HeightCellCount);
            statePerPoint = new PointState[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    statePerPoint[x, y].isOccupied = false;
                    statePerPoint[x, y].capturedPenalty = 0;
                }


            return true;
        }

        public override void setGameState(int currentRound,
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d,
                                          AgentGrid<Evader> currentEvaders, float MaxEvadersToPlace,
                                          List<Point> CurrentPatrollerLocations)
        {

            this.remainingEvadersToPlace = (int)MaxEvadersToPlace;
            foreach (var de in O_d)
            {
                statePerPoint[de.where.X, de.where.Y].capturedPenalty = this.capturedPenalty;
                statePerPoint[de.where.X, de.where.Y].isOccupied = false;

                // remove point from activeEbots, connectedPoints and reduce connectivity with connected points:
                activeEbots.removePoint(de.where);
                List<Point> backConnections, forwardConnections;
                connectedPoints.removePoint(de.where, out backConnections, out forwardConnections);
                foreach (var bc in backConnections)
                    if (bc != CommunicationGraph.SourcePoint)
                        --statePerPoint[bc.X,bc.Y].forwardConnectivity;
                if(forwardConnections != CommunicationGraph.Sink)
                    foreach (var bc in forwardConnections)
                        --statePerPoint[bc.X,bc.Y].backwardConnectivity;


                ++unoccupiedPoints;
            }
            
        }
        
    }
}
