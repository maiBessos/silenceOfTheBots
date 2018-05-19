using AForge.Genetic;
using GoE.AppConstants;
using GoE.AppConstants.Policies.Routing;
using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using GoE.Utils;
using GoE.Utils.Algorithms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Policies
{
   
    public class AttractionRepulsionEvadersPolicy : AFrontsGridRoutingEvadersPolicy
    {
        public const double MINIMAL_PENALTY = 0.01; // fixme a bit dirty

        protected struct PointState
        {
            public bool isOccupied;
            public double penalty;
        }

        protected int remainingEvadersToPlace;
        protected PointState[,] statePorPoint; // state per [x,y]
        protected GridGameGraph graph;
        protected FrontsGridRoutingGameParams param;
        protected ThreadSafeRandom myRand;
        
        double currentTotalSum = 1.0; // helps dealing with accumulated inaccuracies
        
        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AttractionRepulsionRoutingEvaders));
            }
        }
        public override CommunicationGraph communicate()
        {
            return new CommunicationGraph();
        }
        
    
        int unoccupiedPoints;

        //private List<Point> getNextStepOpt2()
        //{
        //    List<Point> res = new List<Point>();

        //    // update penalties
        //    for (int xs = 0; xs < graph.WidthCellCount; ++xs)
        //        for (int ys = 0; ys < graph.HeightCellCount; ++ys)
        //        {
        //            statePorPoint[xs, ys].penalty = (float)(statePorPoint[xs, ys].penalty * penaltyDiscount);
        //            if (statePorPoint[xs, ys].penalty < MINIMAL_PENALTY)
        //                statePorPoint[xs, ys].penalty = 0;
        //        }

        //    while (remainingEvadersToPlace > 0 && unoccupiedPoints > 0)
        //    {
        //        --remainingEvadersToPlace;
        //        --unoccupiedPoints;

        //        double totalEnergy = 0;
        //        double[,] valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
        //        for (int x = 0; x < graph.WidthCellCount; ++x)
        //            for (int y = 0; y < graph.HeightCellCount; ++y)
        //            {
        //                valuePerPoint[x, y] = 0;
        //                if (statePorPoint[x, y].isOccupied)
        //                    continue;


        //                for (int xs = 0; xs < graph.WidthCellCount; ++xs)
        //                    for (int ys = 0; ys < graph.HeightCellCount; ++ys)
        //                    {
        //                        double dist = Math.Abs(xs - x) +
        //                                      Math.Abs(ys - y) * this.xAxisDistBias;
        //                        float occupiedBonus = (statePorPoint[xs, ys].isOccupied) ? (1) : (0);

        //                        valuePerPoint[x, y] += (occupiedBonus - statePorPoint[xs, ys].penalty) / (1 + dist);
        //                    }

        //                var xvals = new int[2] { -1, (int)graph.WidthCellCount };
        //                // we also pretend the first and last column are "occupied"
        //                foreach (var xval in xvals)
        //                    for (int ys = 0; ys < graph.HeightCellCount; ++ys)
        //                    {
        //                        double dist = Math.Abs(xval - x) + 
        //                                      Math.Abs(ys - y) * this.xAxisDistBias;
        //                        valuePerPoint[x, y] += phantomColumnsWeight / (1 + dist);
        //                    }

        //                valuePerPoint[x, y] = Math.Max(0, valuePerPoint[x, y]);
        //                valuePerPoint[x, y] = (double)Math.Pow(valuePerPoint[x, y], energyPowerFactor);
        //                totalEnergy += valuePerPoint[x, y];
        //            }

        //        double choice = myRand.NextDouble() * totalEnergy;

        //        int cx, cy = 0;
        //        bool breakOuter = false;
        //        for (cx = 0; cx < graph.WidthCellCount && !breakOuter; ++cx)
        //            for (cy = 0; cy < graph.HeightCellCount; ++cy)
        //            {
        //                choice -= valuePerPoint[cx, cy];
        //                if (choice < 0)
        //                {
        //                    --cx; // before outer loop exits, it advances cx
        //                    breakOuter = true;
        //                    break;
        //                }
        //            }
        //        if (cx == graph.WidthCellCount)
        //        {
        //            cx = (int)graph.WidthCellCount - 1;
        //            cy = (int)graph.HeightCellCount - 1;
        //        }
        //        statePorPoint[cx, cy].isOccupied = true;
        //        res.Add(new Point(cx, cy));
        //    }

        //    return res;
        //}
        
        public double setEnergy(out double[,] valuePerPoint)
        {
            double totalEnergy = 0;
            valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    valuePerPoint[x, y] = 0;
                    if (statePorPoint[x, y].isOccupied)
                        continue;


                    for (int xs = 0; xs < graph.WidthCellCount; ++xs)
                        for (int ys = 0; ys < graph.HeightCellCount; ++ys)
                        {
                            double dist = Math.Abs(xs - x) +
                                          Math.Abs(ys - y) * this.xAxisDistBias;
                            float occupiedBonus = (statePorPoint[xs, ys].isOccupied) ? (1) : (0);

                            valuePerPoint[x, y] += (occupiedBonus - statePorPoint[xs, ys].penalty) / (1 + dist);
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

            //double totalSum = 0;
            List<double> valSumPerColumn = new List<double>(); // for each target column - sum all source rows
            //List <double> accumSumFromColumn = new List<double>(); //accumSumFromColumn[ci] is sum of all columns from ci (including) to last
            //List<double> accumSumToColumn = new List<double>(); // accumSumToColumn[ci] is sum of all columns from 0 to ci (including)

            List<double> valSumPerRow = new List<double>();
            //List<double> accumSumFromRow = new List<double>(); // including "from" row
            //List<double> accumSumToRow = new List<double>(); // including "to" row


            #region populate totalSum, valSumPerColumn, accumSumFromColumn, accumSumToColumn
            for (int sourceColIdx = 0; sourceColIdx < graph.WidthCellCount; ++sourceColIdx)
            {
                double sum = 0;
                for (int rowIdx = 0; rowIdx < graph.HeightCellCount; ++rowIdx)
                {
                    float occupiedBonus = (statePorPoint[sourceColIdx, rowIdx].isOccupied) ? (1) : (0);
                    double penalty = statePorPoint[sourceColIdx, rowIdx].penalty;
                    sum += occupiedBonus - penalty;
                }
                valSumPerColumn.Add(sum);
                //totalSum += sum;
            }
            //double partialSum = 0;
            //for (int colIdx = 0; colIdx < graph.WidthCellCount; ++colIdx)
            //{
                //partialSum += valSumPerColumn[colIdx];
                //accumSumToColumn[colIdx] = partialSum;
                //accumSumFromColumn[colIdx] = totalSum - partialSum;
            //}
            #endregion
            #region populate valSumPerRow,accumSumFromRow,accumSumToRow
            for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
            {
                double sum = 0;

                for (int colIdx = 0; colIdx < graph.WidthCellCount; ++colIdx)
                {
                    float occupiedBonus = (statePorPoint[colIdx, sourceRowIdx].isOccupied) ? (1) : (0);
                    double penalty = statePorPoint[colIdx, sourceRowIdx].penalty;
                    sum += occupiedBonus - penalty;
                }
                valSumPerRow.Add(sum);
            }
            //partialSum = 0;
            //for (int rowIdx = 0; rowIdx < graph.HeightCellCount; ++rowIdx)
            //{
            //    partialSum += valSumPerRow[rowIdx];
            //    accumSumToRow[rowIdx] = partialSum;
            //    accumSumFromRow[rowIdx] = totalSum - partialSum;
            //}
            #endregion

            #region calculate total energy for different column/row gradients
            // FIXME:  AForge.Math.FFT allows us to do this using nlogn instead of n^2. It may be very significant!
            // (though we might need to ranslate the weights (1/1,1/2,1/3,1/4,1/5...1/x) into a common divisor number, first?)
            for (int targetColidx = 0; targetColidx < graph.WidthCellCount; ++targetColidx)
            {
                // if we calculate energy for points in column targetColidx, then the further we go to 
                // more distant columns, the energy diminishes in proportion to xDist
                for (int sourceColidx = 0; sourceColidx < graph.WidthCellCount; ++sourceColidx)
                {
                    int xDist = Math.Abs(sourceColidx - targetColidx);
                    totalEnergyForTargetColumn[targetColidx] += valSumPerColumn[sourceColidx] / (1+xDist);
                }
            }
            for (int targetRowIdx = 0; targetRowIdx < graph.HeightCellCount; ++targetRowIdx)
            {
                for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
                {
                    int yDist = Math.Abs(sourceRowIdx - targetRowIdx);
                    totalEnergyForTargetRow[targetRowIdx] += valSumPerRow[sourceRowIdx] / (1+yDist);
                }
            }
            #endregion

            double totalEnergy = 0;
            valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
            double minEnergy = double.MaxValue;
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    if (statePorPoint[x, y].isOccupied)
                    {
                        valuePerPoint[x, y] = 0; 
                        continue;
                    }
                    
                    valuePerPoint[x, y] = totalEnergyForTargetRow[y]  +
                                          totalEnergyForTargetColumn[x]/ this.xAxisDistBias;
                    
                    // we pretend to have 2 phantom columns (in each extreme, graph.HeightCellCount points with weight 'phantomColumnsWeight'):
                    double dist = Math.Abs(-1 - x);
                    valuePerPoint[x, y] += graph.HeightCellCount * phantomColumnsWeight / ( this.xAxisDistBias * (1 + dist));

                    dist = Math.Abs(graph.WidthCellCount - x);
                    valuePerPoint[x, y] += graph.HeightCellCount * phantomColumnsWeight / ( this.xAxisDistBias * (1 + dist));
                    
                    
                    double energySign = (valuePerPoint[x, y] > 0) ? (1) : (-1);
                    valuePerPoint[x, y] = energySign * MathEx.PowInt(Math.Abs(valuePerPoint[x, y]), (int)energyPowerFactor);
                    minEnergy = Math.Min(minEnergy, valuePerPoint[x, y]);
                }

            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    if (statePorPoint[x, y].isOccupied)
                        continue;
                    
                    valuePerPoint[x, y] -= minEnergy;
                    totalEnergy += valuePerPoint[x, y];
                    
                }

            return totalEnergy;
        }

        public override List<Point> getNextStep()
        {
            if (remainingEvadersToPlace == 0 || unoccupiedPoints == 0)
                return new List<Point>();

            List<Point> res = new List<Point>();

            // update penalties:
            for (int xs = 0; xs < graph.WidthCellCount; ++xs)
                for (int ys = 0; ys < graph.HeightCellCount; ++ys)
                {
                    statePorPoint[xs, ys].penalty = (float)(statePorPoint[xs, ys].penalty * penaltyDiscount);
                    if (statePorPoint[xs, ys].penalty < MINIMAL_PENALTY)
                        statePorPoint[xs, ys].penalty = 0;
                    if (statePorPoint[xs, ys].isOccupied)
                        res.Add(new Point(xs, ys));
                }
            
            
            double totalEnergy;
            double[,] valuePerPoint;

            if(useOptEnergyAlg)
                totalEnergy = setEnergyOpt(out valuePerPoint);
            else
                totalEnergy = setEnergy(out valuePerPoint);
           
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
                        choice -= valuePerPoint[cx, cy];
                        if (choice < 0)
                        {
                            --cx; // before outer loop exits, it advances cx
                            breakOuter = true;
                            break;
                        }
                    }
                if(cx == graph.WidthCellCount)
                {
                    cx = myRand.Next() % ((int)graph.WidthCellCount - 1);
                    cy = myRand.Next() % ((int)graph.HeightCellCount - 1);
                }
                statePorPoint[cx, cy].isOccupied = true;
                res.Add(new Point(cx, cy));
            }
      

            return res;
        }

        /// <summary>
        /// serves getNextStepOpt()
        /// </summary>
        private class SamplesPerSourceColIdx
        {
            //public List<List<double>> samplesPerSourceCol =new List<List<double>>(); // for each source col index, we have several samples, from source_row_idx = 0, to source_row_idx = Graph.Height
            public List<double> samplesPerSourceColSum = new List<double>(); // sums all samples from the same sourceCol, so calculation of target point is faster

        }
        /// <summary>
        /// optimized version of  getNextStep()
        /// we keep logn copies of the entire matrix, and each cell uses a different combinatiion (of logn)
        /// </summary>
        /// <returns></returns>
        //private List<Point> getNextStepOpt()
        //{
        //    // update penalties
        //    for (int xs = 0; xs < graph.WidthCellCount; ++xs)
        //        for (int ys = 0; ys < graph.HeightCellCount; ++ys)
        //        {
        //            statePorPoint[xs, ys].penalty = (float)(statePorPoint[xs, ys].penalty * penaltyDiscount);
        //            if (statePorPoint[xs, ys].penalty < MINIMAL_PENALTY)
        //                statePorPoint[xs, ys].penalty = 0;
        //        }

        //    // approximate the value of each point, using a data structures that holds for each xDist and yDist combination, a value list per xDist (of several yDist samples, from which we interpolate)


        //    List<Point> res = new List<Point>();
        //    // target point - the point we want to evaluate by accumulating all other points
        //    // source point - a point which's value affects the target point
        //    int SAMPLE_COUNT = (int)graph.HeightCellCount;//2 *  (int)Math.Ceiling(Math.Log(graph.HeightCellCount));

        //    List<double> pointValSumPerCol = new List<double>();
        //    List<SamplesPerSourceColIdx> samplesPerTargetColIdx = new List<SamplesPerSourceColIdx>();

        //    // for each target column - sum all source columns
        //    for (int sourceColIdx = 0; sourceColIdx < graph.WidthCellCount; ++sourceColIdx)
        //    {
        //        double sum = 0;
        //        for (int rowIdx = 0; rowIdx < graph.HeightCellCount; ++rowIdx)
        //        {
        //            float occupiedBonus = (statePorPoint[sourceColIdx, rowIdx].isOccupied) ? (1) : (0);
        //            double penalty = statePorPoint[sourceColIdx, rowIdx].penalty;

        //            sum += occupiedBonus - penalty;
        //        }
        //        pointValSumPerCol.Add(sum);
        //    }

        //    for (int targetColIdx = 0; targetColIdx < graph.WidthCellCount; ++targetColIdx)
        //    {
        //        // for each source column, hold log(n) values to allow constructing different source rows
        //        samplesPerTargetColIdx.Add(new SamplesPerSourceColIdx());
        //        samplesPerTargetColIdx.Last().samplesPerSourceColSum =
        //            AlgorithmUtils.getRepeatingValueList<double>(0.0, SAMPLE_COUNT);

        //        // we want to construct for every possible y value: pointValSumPerCol[sourceColIdx]/(1+xDist + y)
        //        // using log(graph.HeightCellCount) values.this way, as a sum of logn sums, we can calculate the value of each point
        //        // in the matrix. unfortunately, this is an harmonic sum, which is difficult to construct using building
        //        // blocks. however, we use approximation by sampling a constant amount of points, and using linear interpolation
        //        // TODO: check if 2D convolution can make this process faster and more accurate
        //        for (int sourceColIdx = 0; sourceColIdx < graph.WidthCellCount; ++sourceColIdx)
        //        {
        //            int xDist = Math.Abs(targetColIdx - sourceColIdx);
        //            for (float i = 0; i < SAMPLE_COUNT; ++i)
        //            {
        //                // for a specific xDist and yDist = (i/(SAMPLE_COUNT-1)) * this.xAxisDistBias , we take a sample of that column's sum
        //                double sample =
        //                    pointValSumPerCol[sourceColIdx] / (1 + xDist + (graph.HeightCellCount-1) * (i / (SAMPLE_COUNT - 1)) * this.xAxisDistBias);
        //                //samplesPerTargetColIdx.Last().samplesPerSourceCol.Last().Add(sample);
        //                samplesPerTargetColIdx.Last().samplesPerSourceColSum[(int)i] += sample;
        //            }
        //        }

        //        var xvals = new int[2] { -1, (int)graph.WidthCellCount };
        //        // we also pretend the first and last column are "occupied"
        //        foreach (var xval in xvals)
        //        { 
        //            int xDist = Math.Abs(targetColIdx - xval);
        //            for (float i = 0; i < SAMPLE_COUNT; ++i)
        //            {
        //                // 'phantomColumnsWeight * graph.HeightCellCount' is the "sum" of a fully occupied column
        //                double sample =
        //                   (phantomColumnsWeight * graph.HeightCellCount) / (1 + xDist + (graph.HeightCellCount-1) * (i / (SAMPLE_COUNT - 1)) * this.xAxisDistBias);
        //                samplesPerTargetColIdx.Last().samplesPerSourceColSum[(int)i] += sample;
        //            }
        //        }

        //    }


        //    while (remainingEvadersToPlace > 0 && unoccupiedPoints > 0)
        //    {
        //        --remainingEvadersToPlace;
        //        --unoccupiedPoints;

        //        double totalEnergy = 0;
        //        double[,] valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
        //        for (int x = 0; x < graph.WidthCellCount; ++x)
        //            for (int y = 0; y < graph.HeightCellCount; ++y)
        //            {
        //                valuePerPoint[x, y] = 0;
        //                if (statePorPoint[x, y].isOccupied)
        //                    continue;

        //                // interpolate nearest values:
        //                double minIdx = Math.Floor((SAMPLE_COUNT-1) * ((double)y)/(graph.HeightCellCount-1));
        //                double maxIdx = Math.Ceiling((SAMPLE_COUNT-1) * ((double)y)/ (graph.HeightCellCount - 1));
        //                double maxIdxWeight = (SAMPLE_COUNT-1) * ((double)y) / (graph.HeightCellCount - 1) - minIdx;

        //                valuePerPoint[x, y] = 
        //                    samplesPerTargetColIdx[x].samplesPerSourceColSum[(int)minIdx] * (1 - maxIdxWeight) +
        //                    samplesPerTargetColIdx[x].samplesPerSourceColSum[(int)maxIdx] * (maxIdxWeight);

        //                valuePerPoint[x, y] = Math.Max(0, valuePerPoint[x, y]);
        //                valuePerPoint[x, y] = (double)Math.Pow(valuePerPoint[x, y], energyPowerFactor);
        //                totalEnergy += valuePerPoint[x, y];
        //            }

        //        double choice = myRand.NextDouble() * totalEnergy;

        //        int cx, cy = 0;
        //        bool breakOuter = false;
        //        for (cx = 0; cx < graph.WidthCellCount && !breakOuter; ++cx)
        //            for (cy = 0; cy < graph.HeightCellCount; ++cy)
        //            {
        //                choice -= valuePerPoint[cx, cy];
        //                if (choice < 0)
        //                {
        //                    --cx; // before outer loop exits, it advances cx
        //                    breakOuter = true;
        //                    break;
        //                }
        //            }
        //        if (cx == graph.WidthCellCount)
        //        {
        //            cx = (int)graph.WidthCellCount - 1;
        //            cy = (int)graph.HeightCellCount - 1;
        //        }
        //        statePorPoint[cx, cy].isOccupied = true;
        //        res.Add(new Point(cx, cy));
        //    }

        //    return res;
        //}
        bool useOptEnergyAlg;
        double energyPowerFactor, penaltyDiscount, capturedPenalty;
        double xAxisDistBias, phantomColumnsWeight;
        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            this.myRand = new ThreadSafeRandom();
            this.graph = (GridGameGraph)G;
            this.param = prm;

            phantomColumnsWeight =
                double.Parse(AttractionRepulsionRoutingEvaders.PHANTOM_COLUMN_WEIGHT.tryRead(policyParams));

            xAxisDistBias = double.Parse(
                AttractionRepulsionRoutingEvaders.X_AXIS_DIST_BIAS.tryRead(policyParams));

            this.useOptEnergyAlg =
                AttractionRepulsionRoutingEvaders.OPTIMIZE_ENERGY_CALCULATION.tryRead(policyParams) == "1";
            this.energyPowerFactor = double.Parse(AttractionRepulsionRoutingEvaders.ENERGY_POWER_FACTOR.tryRead(policyParams));

            this.penaltyDiscount =
                double.Parse(AttractionRepulsionRoutingEvaders.PURSUERS_HIT_PENALTY_DISCOUNT_FACTOR.tryRead(policyParams));

            this.capturedPenalty =
                graph.WidthCellCount *
                double.Parse(AttractionRepulsionRoutingEvaders.PURSUERS_HIT_INITIAL_PENALTY_FACTOR.tryRead(policyParams));

           

            unoccupiedPoints = (int)(graph.WidthCellCount * graph.HeightCellCount);
            statePorPoint = new PointState[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    statePorPoint[x, y].isOccupied = false;
                    statePorPoint[x, y].penalty = 0;
                }


            return true;
        }

        public override void setGameState(int currentRound,
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d,
                                          AgentGrid<Evader> currentEvaders, float MaxEvadersToPlace,
                                          List<Point> CurrentPatrollerLocations)
        {
            
            this.remainingEvadersToPlace = (int)MaxEvadersToPlace;
            foreach(var de in O_d)
            {
                statePorPoint[de.where.X, de.where.Y].penalty = this.capturedPenalty;
                statePorPoint[de.where.X, de.where.Y].isOccupied = false;
                ++unoccupiedPoints;
            }
        }
    }

    ///// <summary>
    ///// similar to AttractionRepulsionEvadersPolicy, but also allows evaders to transmit to distance r_e
    ///// </summary>
    //public class AttractionRepulsionTransmittingEvadersPolicy : ARoutingEvadersPolicy
    //{
    //    public const double MINIMAL_PENALTY = 0.01; // fixme a bit dirty

    //    protected struct PointState
    //    {
    //        public bool isOccupied;
    //        public double capturedPenalty; // if an e-bot was previously captured here
    //        public double transmittedPenalty; // if an e-bot transmitted from this point
    //        public int forwardConnectivity; // if isOccupied=true, this tells how many other nodes with >=x are connected
    //        public int backwardConnectivity; // if isOccupied=true, this tells how many other nodes with <x are connected
            
    //        // TODO: note: if both nodes are connected and have similar x, this is a bit weird: it's not really a forward connection,
    //        // but if one is uncovered it still hurts the other (i.e. there shouldn't be a bonus, but ehre should be a penalty).
    //        // however, this may make the route slightly more robust, so maybe the bonus is justified.
    //    }

    //    protected int remainingEvadersToPlace;
    //    protected PointState[,] statePorPoint; // state per [x,y]
    //    protected GridGameGraph graph;
    //    protected RoutingGameParams param;
    //    protected ThreadSafeRandom myRand;

    //    double currentTotalSum = 1.0; // helps dealing with accumulated inaccuracies

    //    public override List<ArgEntry> policyInputKeys
    //    {
    //        get
    //        {
    //            List<ArgEntry> res = new List<ArgEntry>();
    //            res.AddRange(ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AttractionRepulsionRoutingEvaders)));
    //            res.AddRange(ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AttractionRepulsionRoutingTransmittingEvaders)));
    //            return res;
    //        }
    //    }
    //    public override CommunicationGraph communicate()
    //    {
    //        return new CommunicationGraph();
    //    }


    //    int unoccupiedPoints;


    //    /// <summary>
    //    /// tells the enrgy that the three points (s.x,s.y+opEventDistance),(s.x,s.y-opEventDistance) and (s.x+opEventDistance,s.y)  feel 
    //    /// </summary>
    //    /// <param name="s"></param>
    //    /// <returns></returns>
    //    private double getPointEnergyForward(PointState s)
    //    {
    //        double occupiedBonusPenalty = (s.isOccupied) ? (optForwardConnectivity - s.forwardConnectivity) : (0.0); // either bonus or a penalty
    //        double penalty = s.capturedPenalty + s.transmittedPenalty;
    //        return occupiedBonusPenalty - penalty;
    //    }
    //    /// <summary>
    //    /// tells the enrgy that the point (s.x-opEventDistance,s.y) feels
    //    /// </summary>
    //    /// <param name="s"></param>
    //    /// <returns></returns>
    //    private double getPointEnergyBackward(PointState s)
    //    {
    //        double occupiedBonusPenalty = (s.isOccupied) ? (optBackConnectivity - s.backwardConnectivity) : (0.0); // either bonus or a penalty
    //        double penalty = s.capturedPenalty + s.transmittedPenalty;
    //        return occupiedBonusPenalty - penalty;
    //    }

    //    // tells the direct energy a source point feels
    //    private double getAffectingEnergy(int sourceX, int sourceY)
    //    {
    //        double sum = 0;
    //        if (sourceX + optEventDistance < graph.WidthCellCount)
    //            sum += getPointEnergyBackward(statePorPoint[sourceX + optEventDistance, sourceY]);

    //        if (sourceX - optEventDistance >= 0)
    //            sum += getPointEnergyForward(statePorPoint[sourceX - optEventDistance, sourceY]);

    //        if (sourceY + optEventDistance < graph.HeightCellCount)
    //            sum += getPointEnergyForward(statePorPoint[sourceX, sourceY + optEventDistance]);

    //        if (sourceY - optEventDistance >= 0)
    //            sum += getPointEnergyForward(statePorPoint[sourceX, sourceY - optEventDistance]);

    //        return sum;
    //    }

    //    public double setEnergy(out double[,] valuePerPoint)
    //    {
    //        double totalEnergy = 0;
    //        valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
    //        for (int x = 0; x < graph.WidthCellCount; ++x)
    //            for (int y = 0; y < graph.HeightCellCount; ++y)
    //            {
    //                valuePerPoint[x, y] = 0;
    //                if (statePorPoint[x, y].isOccupied)
    //                    continue;


    //                for (int xs = 0; xs < graph.WidthCellCount; ++xs)
    //                    for (int ys = 0; ys < graph.HeightCellCount; ++ys)
    //                    {
    //                        double dist = Math.Abs(xs - x) +
    //                                      Math.Abs(ys - y) * this.xAxisDistBias;
    //                        float occupiedBonus = (statePorPoint[xs, ys].isOccupied) ? (1) : (0);

    //                        valuePerPoint[x, y] += (occupiedBonus - statePorPoint[xs, ys].capturedPenalty) / (1 + dist);
    //                    }

    //                var xvals = new int[2] { -1, (int)graph.WidthCellCount };
    //                // we also pretend the first and last column are "occupied"
    //                foreach (var xval in xvals)
    //                    for (int ys = 0; ys < graph.HeightCellCount; ++ys)
    //                    {
    //                        double dist = Math.Abs(xval - x) + Math.Abs(ys - y) * this.xAxisDistBias;
    //                        valuePerPoint[x, y] += phantomColumnsWeight / (1 + dist);
    //                    }

    //                valuePerPoint[x, y] = Math.Max(0, valuePerPoint[x, y]);
    //                valuePerPoint[x, y] = (double)Math.Pow(valuePerPoint[x, y], energyPowerFactor);
    //                totalEnergy += valuePerPoint[x, y];
    //            }

    //        return totalEnergy;
    //    }
    //    public double setEnergyOpt(out double[,] valuePerPoint)
    //    {

    //        double[] totalEnergyForTargetColumn = new double[graph.WidthCellCount];
    //        double[] totalEnergyForTargetRow = new double[graph.HeightCellCount];
            
    //        List<double> valSumPerColumn = new List<double>(); // for each target column - sum all source rows that affect it directly
    //        List<double> valSumPerRow = new List<double>();
          
    //        #region populate totalSum, valSumPerColumn, accumSumFromColumn, accumSumToColumn
    //        for (int sourceColIdx = 0; sourceColIdx < graph.WidthCellCount; ++sourceColIdx)
    //        {
    //            double sum = 0;
    //            for (int rowIdx = 0; rowIdx < graph.HeightCellCount; ++rowIdx)
    //                sum += getAffectingEnergy(sourceColIdx, rowIdx);
    //            valSumPerColumn.Add(sum);
    //        }
            
    //        #endregion
    //        #region populate valSumPerRow,accumSumFromRow,accumSumToRow
    //        for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
    //        {
    //            double sum = 0;
    //            for (int colIdx = 0; colIdx < graph.WidthCellCount; ++colIdx)
    //                sum += getAffectingEnergy(colIdx, sourceRowIdx);
    //            valSumPerRow.Add(sum);
    //        }
     
    //        #endregion

    //        #region calculate total energy for different column/row gradients
    //        for (int targetColidx = 0; targetColidx < graph.WidthCellCount; ++targetColidx)
    //        {
    //            // if we calculate energy for points in column targetColidx, then the further we go to 
    //            // more distant columns, the energy diminishes in proportion to xDist
    //            for (int sourceColidx = 0; sourceColidx < graph.WidthCellCount; ++sourceColidx)
    //            {
    //                int xDist = Math.Abs(sourceColidx - targetColidx);
    //                totalEnergyForTargetColumn[targetColidx] += valSumPerColumn[sourceColidx] / (1 + xDist);
    //            }
    //        }
    //        for (int targetRowIdx = 0; targetRowIdx < graph.HeightCellCount; ++targetRowIdx)
    //        {
    //            for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
    //            {
    //                int yDist = Math.Abs(sourceRowIdx - targetRowIdx);
    //                totalEnergyForTargetRow[targetRowIdx] += valSumPerRow[sourceRowIdx] / (1 + yDist);
    //            }
    //        }
    //        #endregion

    //        double totalEnergy = 0;
    //        valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
    //        double minEnergy = double.MaxValue;
    //        for (int x = 0; x < graph.WidthCellCount; ++x)
    //            for (int y = 0; y < graph.HeightCellCount; ++y)
    //            {
    //                if (statePorPoint[x, y].isOccupied)
    //                {
    //                    valuePerPoint[x, y] = 0;
    //                    continue;
    //                }

    //                valuePerPoint[x, y] = totalEnergyForTargetRow[y] +
    //                                      totalEnergyForTargetColumn[x] / this.xAxisDistBias;

    //                // we pretend to have 2 phantom columns (in each extreme, graph.HeightCellCount points with weight 'phantomColumnsWeight'):
    //                double dist = Math.Abs(-1 - x);
    //                valuePerPoint[x, y] += graph.HeightCellCount * phantomColumnsWeight / (this.xAxisDistBias * (1 + dist));

    //                dist = Math.Abs(graph.WidthCellCount - x);
    //                valuePerPoint[x, y] += graph.HeightCellCount * phantomColumnsWeight / (this.xAxisDistBias * (1 + dist));


    //                double energySign = (valuePerPoint[x, y] > 0) ? (1) : (-1);
    //                valuePerPoint[x, y] = energySign * MathEx.PowInt(Math.Abs(valuePerPoint[x, y]), (int)energyPowerFactor);
    //                minEnergy = Math.Min(minEnergy, valuePerPoint[x, y]);
    //            }

    //        for (int x = 0; x < graph.WidthCellCount; ++x)
    //            for (int y = 0; y < graph.HeightCellCount; ++y)
    //            {
    //                if (statePorPoint[x, y].isOccupied)
    //                    continue;

    //                valuePerPoint[x, y] -= minEnergy;
    //                totalEnergy += valuePerPoint[x, y];

    //            }

    //        return totalEnergy;
    //    }

    //    public override List<Point> getNextStep()
    //    {
    //        if (remainingEvadersToPlace == 0 || unoccupiedPoints == 0)
    //            return new List<Point>();

    //        List<Point> res = new List<Point>();

    //        // update penalties:
    //        for (int xs = 0; xs < graph.WidthCellCount; ++xs)
    //            for (int ys = 0; ys < graph.HeightCellCount; ++ys)
    //            {
    //                statePorPoint[xs, ys].transmittedPenalty = (float)(statePorPoint[xs, ys].transmittedPenalty * transmissionPenaltyDiscount);
    //                statePorPoint[xs, ys].capturedPenalty = (float)(statePorPoint[xs, ys].capturedPenalty * capturedEbotPenaltyDiscount);

    //                if (statePorPoint[xs, ys].capturedPenalty < MINIMAL_PENALTY)
    //                    statePorPoint[xs, ys].capturedPenalty = 0;

    //                if (statePorPoint[xs, ys].transmittedPenalty < MINIMAL_PENALTY)
    //                    statePorPoint[xs, ys].transmittedPenalty = 0;

    //                if (statePorPoint[xs, ys].isOccupied)
    //                    res.Add(new Point(xs, ys)); // since res sould contain all ebots in this round
    //            }


    //        double totalEnergy;
    //        double[,] valuePerPoint;

    //        if (useOptEnergyAlg)
    //            totalEnergy = setEnergyOpt(out valuePerPoint);
    //        else
    //            totalEnergy = setEnergy(out valuePerPoint);

    //        // choose where to add each ebot
    //        while (remainingEvadersToPlace > 0 && unoccupiedPoints > 0)
    //        {
    //            --remainingEvadersToPlace;
    //            --unoccupiedPoints;

    //            double choice = myRand.NextDouble() * totalEnergy;

    //            int cx, cy = 0;
    //            bool breakOuter = false;
    //            for (cx = 0; cx < graph.WidthCellCount && !breakOuter; ++cx)
    //                for (cy = 0; cy < graph.HeightCellCount; ++cy)
    //                {
    //                    choice -= valuePerPoint[cx, cy];
    //                    if (choice < 0)
    //                    {
    //                        --cx; // before outer loop exits, it advances cx
    //                        breakOuter = true;
    //                        break;
    //                    }
    //                }
    //            if (cx == graph.WidthCellCount)
    //            {
    //                cx = myRand.Next() % ((int)graph.WidthCellCount - 1);
    //                cy = myRand.Next() % ((int)graph.HeightCellCount - 1);
    //            }
    //            statePorPoint[cx, cy].isOccupied = true;
    //            res.Add(new Point(cx, cy));
    //            activeEbots.addPoint(res.Last());
    //        }


    //        return res;
    //    }

    //    /// <summary>
    //    /// serves getNextStepOpt()
    //    /// </summary>
    //    private class SamplesPerSourceColIdx
    //    {
    //        //public List<List<double>> samplesPerSourceCol =new List<List<double>>(); // for each source col index, we have several samples, from source_row_idx = 0, to source_row_idx = Graph.Height
    //        public List<double> samplesPerSourceColSum = new List<double>(); // sums all samples from the same sourceCol, so calculation of target point is faster

    //    }

    //    CoarsePointGrid activeEbots; // speeds up connectivity checks
    //    bool useOptEnergyAlg;
    //    double energyPowerFactor, capturedEbotPenaltyDiscount, capturedPenalty;
    //    double xAxisDistBias, phantomColumnsWeight;

    //    int optEventDistance; // if opEventDistance = 1, then it's like previous policy. if larger, then each PointState affects the energy of points with opEventDistance distance to all directions
    //    double optBackConnectivity, optForwardConnectivity;
    //    double transmissionPenalty, transmissionPenaltyDiscount;

    //    public override bool init(GridGameGraph G, RoutingGameParams prm, IPursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
    //    {
    //        this.myRand = new ThreadSafeRandom();
    //        this.graph = G;
    //        this.param = prm;
    //        activeEbots = new CoarsePointGrid(new Rectangle(0, 0, (int)graph.WidthCellCount, (int)graph.HeightCellCount), 
    //                                          (int)Math.Round(param.r_e * 1.5), 
    //                                          (int)Math.Round(param.r_e * 1.5));
    //        optBackConnectivity = double.Parse(
    //            AttractionRepulsionRoutingTransmittingEvaders.OPTIMAL_BACKWARD_CONNECTIVITY.tryRead(policyParams));
    //        optForwardConnectivity = double.Parse(
    //            AttractionRepulsionRoutingTransmittingEvaders.OPTIMAL_FORWARD_CONNECTIVITY.tryRead(policyParams));
    //        optEventDistance = (int)Math.Round(
    //            param.r_e * double.Parse(AttractionRepulsionRoutingTransmittingEvaders.OPTIMAL_EVENT_DISTANCE_FACTOR.tryRead(policyParams)));
    //        transmissionPenalty =
    //            graph.WidthCellCount *
    //            double.Parse(AttractionRepulsionRoutingTransmittingEvaders.TRANSMISSION_INITIAL_PENALTY_FACTOR.tryRead(policyParams));
    //        transmissionPenaltyDiscount = double.Parse(AttractionRepulsionRoutingTransmittingEvaders.TRANSMISSION_PENALTY_DISCOUNT_FACTOR.tryRead(policyParams));
            
            
    //        phantomColumnsWeight =
    //            double.Parse(AttractionRepulsionRoutingEvaders.PHANTOM_COLUMN_WEIGHT.tryRead(policyParams));

    //        xAxisDistBias = double.Parse(
    //            AttractionRepulsionRoutingEvaders.X_AXIS_DIST_BIAS.tryRead(policyParams));

    //        this.useOptEnergyAlg =
    //            AttractionRepulsionRoutingEvaders.OPTIMIZE_ENERGY_CALCULATION.tryRead(policyParams) == "1";
    //        this.energyPowerFactor = double.Parse(AttractionRepulsionRoutingEvaders.ENERGY_POWER_FACTOR.tryRead(policyParams));

    //        this.capturedEbotPenaltyDiscount =
    //            double.Parse(AttractionRepulsionRoutingEvaders.PURSUERS_HIT_PENALTY_DISCOUNT_FACTOR.tryRead(policyParams));

    //        this.capturedPenalty =
    //            graph.WidthCellCount *
    //            double.Parse(AttractionRepulsionRoutingEvaders.PURSUERS_HIT_INITIAL_PENALTY_FACTOR.tryRead(policyParams));



    //        unoccupiedPoints = (int)(graph.WidthCellCount * graph.HeightCellCount);
    //        statePorPoint = new PointState[graph.WidthCellCount, graph.HeightCellCount];
    //        for (int x = 0; x < graph.WidthCellCount; ++x)
    //            for (int y = 0; y < graph.HeightCellCount; ++y)
    //            {
    //                statePorPoint[x, y].isOccupied = false;
    //                statePorPoint[x, y].capturedPenalty = 0;
    //            }


    //        return true;
    //    }

    //    public override void setGameState(int currentRound,
    //                                      IEnumerable<GameLogic.Utils.CapturedObservation> O_d,
    //                                      AgentGrid<Evader> currentEvaders, float MaxEvadersToPlace,
    //                                      List<Point> CurrentPatrollerLocations)
    //    {

    //        this.remainingEvadersToPlace = (int)MaxEvadersToPlace;
    //        foreach (var de in O_d)
    //        {
    //            statePorPoint[de.where.X, de.where.Y].capturedPenalty = this.capturedPenalty;
    //            statePorPoint[de.where.X, de.where.Y].isOccupied = false;
    //            activeEbots.removePoint(de.where);
    //            ++unoccupiedPoints;
    //        }
            
    //    }
        
    //}
}
