using GoE.GameLogic;
using GoE.GameLogic.Algorithms;
using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.AppConstants;
using GoE.Utils.Algorithms;
using GoE.AppConstants.Policies.Routing;

namespace GoE.Policies
{

    public class AttractionRepulsionPursuersPolicy : AFrontsGridRoutingPursuersPolicy
    {
        public const double MINIMAL_EFFECT_VAL = 0.01; // fixme a bit dirty. works for both bonus and penalty

        protected struct PointState
        {
            public void reset()
            {
                penalty = detectedBonus = transmissionDetectionBonus = 0;
                isOccupied = false;
            }
            public bool isOccupied;

            public double getTotalEnergy()
            {
                return detectedBonus + transmissionDetectionBonus - penalty;
            }
            public void setPenalty(double val)
            {
                penalty = val;
            }
            public double getPenalty() { return penalty; }
            public void setDetectedBonus(double val)
            {
                detectedBonus = val;
            }
            public double getDetectedBonus() { return detectedBonus; }
            public void setTransmissionDetectionBonus(double val)
            {
                transmissionDetectionBonus = val;
            }
            public double getTransmissionDetectionBonus() { return transmissionDetectionBonus; }

            double penalty; // visiting a point
            double detectedBonus; //hitting an ebot
            double transmissionDetectionBonus; // temporary bonus, between transmission detection and until point is visited
        }
        
        protected PointState[,] statePerPoint; // state per [x,y]
        protected GridGameGraph graph;
        protected FrontsGridRoutingGameParams param;
        protected ThreadSafeRandom myRand;

        
        double currentTotalSum = 1.0; // helps dealing with accumulated inaccuracies

        public override List<ArgEntry> policyInputKeys()
        {
            //get
           // {
                return ReflectionUtils.getStaticInstancesInClass<ArgEntry>(typeof(AttractionRepulsionRoutingPursuers));
           // }
        }
        
        int unoccupiedPoints;

        public double setEnergy(out double[,] valuePerPoint)
        {
            double totalEnergy = 0, minEnergy = 0;
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
                            // why do we need visitPenaltySignificance? both events already have arbitrary initial value...
                            //double penaltyDist = (Math.Abs(xs - x) + Math.Abs(ys - y)) / visitPenaltySignificance;
                            //double penaltyDist = (Math.Abs(xs - x) + Math.Abs(ys - y));

                            double bonusDist = Math.Abs(xs - x) +
                                               Math.Abs(ys - y) * this.xAxisDistBias;

                            //valuePerPoint[x, y] += (statePerPoint[xs, ys].detectedBonus / (1 + bonusDist)) -
                            //                       (statePerPoint[xs, ys].penalty / (1 + penaltyDist));
                            valuePerPoint[x, y] += (statePerPoint[xs, ys].getTotalEnergy() / (1 + bonusDist));
                        }


                    valuePerPoint[x, y] = (double)Math.Pow(valuePerPoint[x, y], energyPowerFactor);
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
        public double setEnergyOpt(out double[,] valuePerPoint)
        {
            double[] totalBonusForTargetColumn = new double[graph.WidthCellCount];
            double[] totalBonusForTargetRow = new double[graph.HeightCellCount];
            //double[] totalPenaltyForTargetColumn = new double[graph.WidthCellCount];
            //double[] totalPenaltyForTargetRow = new double[graph.HeightCellCount];
            List<double> bonusSumPerColumn = new List<double>();
            //List<double>    penaltySumPerColumn = new List<double>(); 
            List<double> bonusSumPerRow = new List<double>();
            //List<double> penaltySumPerRow = new List<double>();


            #region populate valSumPerColumn
            for (int sourceColIdx = 0; sourceColIdx < graph.WidthCellCount; ++sourceColIdx)
            {
                double bonusSum = 0;
                //double penaltySum = 0;
                for (int rowIdx = 0; rowIdx < graph.HeightCellCount; ++rowIdx)
                {
                    //penaltySum += statePerPoint[sourceColIdx, rowIdx].penalty;
                    //bonusSum += statePerPoint[sourceColIdx, rowIdx].detectedBonus;
                    
                    bonusSum += statePerPoint[sourceColIdx, rowIdx].getTotalEnergy();
                }
                bonusSumPerColumn.Add(bonusSum);
                //penaltySumPerColumn.Add(penaltySum);
            }

            #endregion
            #region populate valSumPerRow
            for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
            {
                double bonusSum = 0;
                //double penaltySum = 0;
                for (int colIdx = 0; colIdx < graph.WidthCellCount; ++colIdx)
                {
                    //bonusSum += statePerPoint[colIdx, sourceRowIdx].detectedBonus;
                    //penaltySum += statePerPoint[colIdx, sourceRowIdx].penalty;
                    bonusSum += statePerPoint[colIdx, sourceRowIdx].getTotalEnergy();
                }
                bonusSumPerRow.Add(bonusSum);
                //penaltySumPerRow.Add(penaltySum);
            }
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
                    totalBonusForTargetColumn[targetColidx] += bonusSumPerColumn[sourceColidx] / (1 + xDist);
                    //totalPenaltyForTargetColumn[targetColidx] += penaltySumPerColumn[sourceColidx] / (1 + xDist);
                }
            }
            for (int targetRowIdx = 0; targetRowIdx < graph.HeightCellCount; ++targetRowIdx)
            {
                for (int sourceRowIdx = 0; sourceRowIdx < graph.HeightCellCount; ++sourceRowIdx)
                {
                    int yDist = Math.Abs(sourceRowIdx - targetRowIdx);
                    totalBonusForTargetRow[targetRowIdx] += bonusSumPerRow[sourceRowIdx] / (1 + yDist);
                    //totalPenaltyForTargetRow[targetRowIdx] += penaltySumPerRow[sourceRowIdx] / (1 + yDist);
                }
            }
            #endregion
            double totalEnergy = 0, minEnergy = 0;
            valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    if (statePerPoint[x, y].isOccupied)
                    {
                        valuePerPoint[x, y] = 0;
                        continue;
                    }

                    // why do we need visitPenaltySignificance? both events already have arbitrary initial value...
                    //valuePerPoint[x, y] =                         
                    //    (totalBonusForTargetRow[y] + totalBonusForTargetColumn[x] / this.xAxisDistBias) -
                    //    (totalPenaltyForTargetRow[y] + totalPenaltyForTargetRow[x]) / visitPenaltySignificance;

                    //valuePerPoint[x, y] =
                    //    (totalBonusForTargetRow[y] + totalBonusForTargetColumn[x] / this.xAxisDistBias) -
                    //    (totalPenaltyForTargetRow[y] + totalPenaltyForTargetRow[x]);

                    valuePerPoint[x, y] =
                        (totalBonusForTargetRow[y] + totalBonusForTargetColumn[x] / this.xAxisDistBias);
                        //(totalPenaltyForTargetRow[y] + totalPenaltyForTargetRow[x] / this.xAxisDistBias);


                    //for (int xs = 0; xs < graph.WidthCellCount; ++xs)
                    //    for (int ys = 0; ys < graph.HeightCellCount; ++ys)
                    //    {
                    //        double penaltyDist = (Math.Abs(xs - x) + Math.Abs(ys - y)) / visitPenaltySignificance;

                    //        double bonusDist = Math.Abs(xs - x) +
                    //                            Math.Abs(ys - y) * this.xAxisDistBias;

                    //        valuePerPoint[x, y] += (statePorPoint[xs, ys].bonus / (1 + bonusDist)) -
                    //                                (statePorPoint[xs, ys].penalty / (1 + penaltyDist));
                    //    }

                    double sign = (valuePerPoint[x, y] > 0) ? (1) : (-1);
                    valuePerPoint[x, y] = sign * (double)MathEx.PowInt(Math.Abs(valuePerPoint[x, y]), 
                        (int)energyPowerFactor);

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
        public override List<Point> getNextStep()
        {
            

            List<Point> res = new List<Point>();


            //for (int i = 0; i < param.A_P.Count; ++i)
            //    res.Add(new Point(0, 0));
            //return res;


            // update penalties
            foreach (Point prev in prevPbotLocations)
            {
                statePerPoint[prev.X, prev.Y].setPenalty(visitInitialPenalty);
                statePerPoint[prev.X, prev.Y].setTransmissionDetectionBonus(0); // after we visited a point where we had a transmission detection, we remove that one-time bonus. There was also added energy in the surrounding area, that stays
            }

            for (int xs = 0; xs < graph.WidthCellCount; ++xs)
                for (int ys = 0; ys < graph.HeightCellCount; ++ys)
                {
                    
                    statePerPoint[xs, ys].setDetectedBonus(statePerPoint[xs, ys].getDetectedBonus() * detectedDiscountFactor);
                    if (statePerPoint[xs, ys].getDetectedBonus() < MINIMAL_EFFECT_VAL)
                        statePerPoint[xs, ys].setDetectedBonus(0);

                    statePerPoint[xs, ys].setPenalty( statePerPoint[xs, ys].getPenalty() * visitPenaltyDiscount);
                    if (statePerPoint[xs, ys].getPenalty() < MINIMAL_EFFECT_VAL)
                        statePerPoint[xs, ys].setPenalty(0);
                }

            double[,] valuePerPoint = new double[graph.WidthCellCount, graph.HeightCellCount];
            double totalEnergy;

            if (fastCalculation)
                totalEnergy = setEnergyOpt(out valuePerPoint);
            else
                totalEnergy = setEnergy(out valuePerPoint);

            for (int i = 0; i < param.A_P.Count; ++i)
            {
               
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
                if (cx == graph.WidthCellCount)
                {
                    cx = (int)Math.Round(myRand.NextDouble() * (graph.WidthCellCount - 1));
                    cy = (int)Math.Round(myRand.NextDouble() * (graph.HeightCellCount - 1));
                }
                statePerPoint[cx, cy].isOccupied = true;
                res.Add(new Point(cx, cy));
            }

            foreach (Point prev in prevPbotLocations)
            {
                statePerPoint[prev.X, prev.Y].isOccupied = false;
            }
            prevPbotLocations = new List<Point>(res);

            return res;
        }
        List<Point> prevPbotLocations;

        bool fastCalculation;
        double energyPowerFactor, visitPenaltyDiscount, visitInitialPenalty, transmissionDetectionBonus;
        //double hitBonusDiscount;
        //double visitPenaltySignificance;
        double xAxisDistBias;
        int optEventDistance;
        double detectedInitForewardBonus,
               detectedInitBackwardBonus,
               detectedDiscountFactor;
        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            this.myRand = new ThreadSafeRandom();
            this.graph = (GridGameGraph)G;
            this.param = prm;
            prevPbotLocations = new List<Point>();
            //visitPenaltySignificance = double.Parse(AttractionRepulsionRoutingPursuers.PURSUER_VISIT_PENALTY_SIGNIFICANCE.tryRead(policyParams));
            xAxisDistBias = double.Parse(AttractionRepulsionRoutingPursuers.X_AXIS_DIST_BIAS.tryRead(policyParams));
            energyPowerFactor = double.Parse(AttractionRepulsionRoutingPursuers.ENERGY_POWER_FACTOR.tryRead(policyParams));

            transmissionDetectionBonus = graph.WidthCellCount * double.Parse(AttractionRepulsionRoutingPursuers.TRANSMISSION_DETECTION_INITIAL_BONUS_FACTOR.tryRead(policyParams));
            //hitBonusDiscount = double.Parse(AttractionRepulsionRoutingPursuers.PURSUERS_HIT_BONUS_DISCOUNT_FACTOR.tryRead(policyParams));
            visitPenaltyDiscount = double.Parse(AttractionRepulsionRoutingPursuers.PURSUERS_VISIT_PENALTY_DISCOUNT_FACTOR.tryRead(policyParams));
            visitInitialPenalty = graph.WidthCellCount * double.Parse(AttractionRepulsionRoutingPursuers.PURSUERS_VISIT_INITIAL_PENALTY_FACTOR.tryRead(policyParams));
            fastCalculation = (AttractionRepulsionRoutingPursuers.OPTIMIZE_ENERGY_CALCULATION.tryRead(policyParams) == "1");

            optEventDistance = (int)Math.Round(prm.r_e *
                double.Parse(AttractionRepulsionRoutingPursuers.OPTIMAL_EVENT_DISTANCE_FACTOR.tryRead(policyParams)));
            detectedInitBackwardBonus =
                double.Parse(AttractionRepulsionRoutingPursuers.DETECTED_BACKWARDS_BONUS_FACTOR.tryRead(policyParams));
            detectedInitForewardBonus =
                detectedInitBackwardBonus * double.Parse(AttractionRepulsionRoutingPursuers.DETECTED_FORWARDS_BONUS_FACTOR.tryRead(policyParams));
            detectedDiscountFactor =
                double.Parse(AttractionRepulsionRoutingPursuers.DETECTED_BONUS_DISCOUNT_FACTOR.tryRead(policyParams));


            unoccupiedPoints = (int)(graph.WidthCellCount * graph.HeightCellCount);
            statePerPoint = new PointState[graph.WidthCellCount, graph.HeightCellCount];
            for (int x = 0; x < graph.WidthCellCount; ++x)
                for (int y = 0; y < graph.HeightCellCount; ++y)
                {
                    statePerPoint[x, y].reset();
                }


            return true;
        }

        /// <summary>
        /// if a pbot was detected in certain point, we add energy to the point around it (in distance  'optEventDistance')
        /// FIXME: we should add energy to all points in the saquare/diamond surrrounding the point, but instead 
        /// we only add to the up,left,right,down points. this may be fixed efficiently (and evesdroppers need
        /// a similar fix)
        /// </summary>
        /// <param name="detectionPoint"></param>
        private void addDetectionEventEnergy(Point detectionPoint)
        {

            if (detectionPoint.X + optEventDistance < graph.WidthCellCount)
            {
                double currentBonus = statePerPoint[detectionPoint.X + optEventDistance, detectionPoint.Y].getDetectedBonus();
                statePerPoint[detectionPoint.X + optEventDistance, detectionPoint.Y].setDetectedBonus(currentBonus + detectedInitBackwardBonus);
            }

            if (detectionPoint.X - optEventDistance >= 0)
            {
                double currentBonus = statePerPoint[detectionPoint.X - optEventDistance, detectionPoint.Y].getDetectedBonus();
                statePerPoint[detectionPoint.X - optEventDistance, detectionPoint.Y].setDetectedBonus(currentBonus + detectedInitForewardBonus);
            }

            if (detectionPoint.Y + optEventDistance < graph.HeightCellCount)
            {
                double currentBonus = statePerPoint[detectionPoint.X, detectionPoint.Y + optEventDistance].getDetectedBonus();
                statePerPoint[detectionPoint.X, detectionPoint.Y + optEventDistance].setDetectedBonus(currentBonus + detectedInitForewardBonus);
            }

            if (detectionPoint.Y - optEventDistance >= 0)
            {
                double currentBonus = statePerPoint[detectionPoint.X, detectionPoint.Y - optEventDistance].getDetectedBonus();
                statePerPoint[detectionPoint.X, detectionPoint.Y - optEventDistance].setDetectedBonus(currentBonus + detectedInitForewardBonus);
            }
            
        }
        public override void setGameState(int currentRound, IEnumerable<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {

            foreach (var de in O_d)
            {
                if (statePerPoint[de.where.X, de.where.Y].getTransmissionDetectionBonus() == 0)
                    addDetectionEventEnergy(de.where); // we captured an ebot by chance. add energy to surrounding area
                ++unoccupiedPoints;
            }
            foreach(var de in O_c)
            {
                if (statePerPoint[de.X, de.Y].getTransmissionDetectionBonus() == 0)
                {
                    // we detected a new transmitting ebot. add energy to surrounding area
                    statePerPoint[de.X, de.Y].setTransmissionDetectionBonus(transmissionDetectionBonus);
                    addDetectionEventEnergy(de);
                }
            }
        }
    }
}
