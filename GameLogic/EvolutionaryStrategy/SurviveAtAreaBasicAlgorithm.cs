using AForge.Genetic;
using GoE.GameLogic.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy.EvaderSide;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using GoE.Utils.Algorithms;

namespace GoE.GameLogic.EvolutionaryStrategy
{
    /// <summary>
    /// manages a single evader that is supposed to stay in an area, always eavesdrop, and survive- until another algorithm buys it
    /// its parameters are in what rings it is allows to travel in, and how close it wants to be evaders from both more outer and inner rings
    /// </summary>
    public class SurviveAtArea : IEvaderBasicAlgorithm
    {
        public enum ShortParamsIdx : int
        {
            MinmalRing = 0, // ring closest to target in which this evader hangs around
            MaximalRing = 1,
            DistanceFromOuterRingEvaders = 2, // desired distance from other evaders that are located in rings fruther from the target than MaximalRing
            DistanceFromNonOuterRingEvaders = 3, // complements DistanceFromOuterRingEvaders (considers all rings that are closer/equal to the target than MaximalRing)
            
            Count
        }
        public enum FracParamsIdx : int
        {
            MaxRouteLength = 0,  // the evader is always moving from point a to b , and each time chooses a different b point with distance of 'MaxRouteLength' from current a point
            Count
        }



        


        private List<double> crawlParamProbabilities; 
        private ConcreteCompositeChromosome Params;
        
        
        bool isAlgBorken = false; // becomes true after the algorithm 

        // used only for generating other algorithms with same type
        public SurviveAtArea() {}

        public SurviveAtArea(ConcreteCompositeChromosome param)
        {
            Params = param;
            crawlParamProbabilities = ((EvaderCrawlChromosome)param[ConcreteCompositeChromosome.Miscs, 0]).getProbabilities();
        }

        //public override void loseEvader(Evader lostEvader)
        //{
        //    evaders.Remove(lostEvader);
        //}
        //public override void handleNewEvaders(List<TaggedEvader> gainedEvaders)
        //{
        //    foreach (var e in gainedEvaders)
        //        evaders[e.e] = e;
        //}




        //override public bool loseEvader(Evader lostEvader, double compensationWorth)
        //{
        //    bool res = base.loseEvader(lostEvader, compensationWorth);
        //    isAlgBorken |= res;
        //    return res;
        //}

        /// <summary>
        /// if point is within desired location, man dist is 0. otherwise, distance to nearest ring in desired range
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        int manDistFromDesiredLocation(Point p)
        {
            int distFromTarget = p.manDist(EvolutionConstants.targetPoint);
            if(distFromTarget < this[ShortParamsIdx.MinmalRing])
                return this[ShortParamsIdx.MinmalRing] - distFromTarget;
            if(distFromTarget > this[ShortParamsIdx.MaximalRing])
                return distFromTarget - this[ShortParamsIdx.MaximalRing];
            return 0;
        }

        public override List<EvaluatedEvader> getEvaderEvaluations(IEnumerable<TaggedEvader> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, PursuerStatistics ps)
        {
            List<EvaluatedEvader> priorityWeightPerEvader = new List<EvaluatedEvader>();
            foreach (var ev in availableAgents)
            {
                double utility =
                    EvolutionConstants.radius -
                    manDistFromDesiredLocation(s.L[s.MostUpdatedEvadersLocationRound][ev.e].nodeLocation);

                priorityWeightPerEvader.Add(new EvaluatedEvader() { e = ev, value = utility });
            }
            return priorityWeightPerEvader;
        }
        public override RepairingNeeds getRepairingNeeds(IEnumerable<TaggedEvader> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<System.Drawing.Point> O_d, Algorithms.PursuerStatistics ps)
        {
            RepairingNeeds res = new RepairingNeeds();
            if (isAlgBorken)
                return new RepairingNeeds() { minEvadersCount = -1, maxEvadersCount = 0 };

            if(Evaders.Count == 0)
            {
                // this is the first getRepairingNeeds() call
                res.minEvadersCount = res.maxEvadersCount = 1;
                res.priorityWeightPerEvader = new List<EvaluatedEvader>();

                res.priorityWeightPerEvader = getEvaderEvaluations(availableAgents, s, unitsInSink, O_d, ps);
                return res;
            }
            
            // the alg. didn't lose any evader, and continue as usual
            return new RepairingNeeds() { minEvadersCount = 0, maxEvadersCount = 0 };
        }
        public ushort this[ShortParamsIdx idx]
        {
            get
            {
                return Params[ConcreteCompositeChromosome.Shorts, (int)idx];
            }
        }
        public double this[FracParamsIdx idx]
        {
            get
            {
                return Params[ConcreteCompositeChromosome.Doubles, (int)idx];
            }
        }

        // the higher, the better
        private float evaluateTargetLocation(Point target, GameState s)
        {
            //if (target.X < 0 || target.Y < 0 || target.X >= EvolutionConstants.graph.WidthCellCount || target.Y >= EvolutionConstants.graph.HeightCellCount)
            if(target.manDist(EvolutionConstants.targetPoint) > EvolutionConstants.radius)
                return float.NegativeInfinity;

            Point nearestEvaderInOutsideArea;
            Point nearestEvaderInArea;
            // TODO: obviously this is very slow, and considers only the nearest. consider using quad/kd-tree..

            int nearestEvaderInOutsideAreaMinDist = int.MaxValue;
            int nearestEvaderInAreaMinDist = int.MaxValue;
             
            int rad;
            float res = 0;

            foreach (Evader managedEve in Evaders.Keys)
            {
                // find the two nearest evaders - one within the area and one outside the area
                foreach (Evader otherEve in EvolutionConstants.param.A_E)
                {
                    Point evaderPoint;

                    if (otherEve == managedEve)
                        continue;
                    if (!s.L[s.MostUpdatedEvadersLocationRound][otherEve].getLocationIfNode(out evaderPoint))
                        continue;

                    rad = evaderPoint.manDist(target);

                    if (evaderPoint.manDist(EvolutionConstants.targetPoint) <= this[ShortParamsIdx.DistanceFromNonOuterRingEvaders])
                    {
                        if (rad < nearestEvaderInAreaMinDist)
                        {
                            nearestEvaderInAreaMinDist = rad;
                            nearestEvaderInArea = evaderPoint;
                        }
                    }
                    else
                    {
                        if (rad < nearestEvaderInOutsideAreaMinDist)
                        {
                            nearestEvaderInOutsideAreaMinDist = rad;
                            nearestEvaderInOutsideArea = evaderPoint;
                        }
                    }
                }
            }
            res -= Math.Abs(nearestEvaderInOutsideAreaMinDist - this[ShortParamsIdx.DistanceFromOuterRingEvaders]);
            res -= Math.Abs(nearestEvaderInAreaMinDist - this[ShortParamsIdx.DistanceFromNonOuterRingEvaders]);
                
            
            rad = target.manDist(EvolutionConstants.targetPoint);
            if(rad > this[ShortParamsIdx.MaximalRing])
                res += (this[ShortParamsIdx.MaximalRing] - rad);
            else if(rad < this[ShortParamsIdx.MinmalRing])
                res += (rad - this[ShortParamsIdx.MinmalRing]);

            return res;
        }


        Point getPointInArea(Point p)
        {
            int rad = p.manDist(EvolutionConstants.targetPoint);

            

            if (rad == 0)
                return EvolutionConstants.targetPoint.add(this[ShortParamsIdx.MinmalRing], 0);

            float angle = Utils.getAngleOfGridPoint(p.subtruct(EvolutionConstants.targetPoint));
            
            if (rad < this[ShortParamsIdx.MinmalRing])
                rad = this[ShortParamsIdx.MinmalRing];
            else if (rad > this[ShortParamsIdx.MaximalRing])
                rad = this[ShortParamsIdx.MaximalRing];

            Point res = EvolutionConstants.targetPoint.add(Utils.getGridPointByAngle(rad, angle));
            return res;
        }

        Dictionary<Evader, EvaderCrawl> crawl = new Dictionary<Evader, EvaderCrawl>();
        
        override public void  loseEvader(Evader lostEvader)
        {
 	         crawl.Remove(lostEvader);
             base.loseEvader(lostEvader);
        }
        override public void  loseAllEvaders()
        {
 	         crawl.Clear();
             base.loseAllEvaders();
        }

        public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep(
            GameState s, 
            GoE.GameLogic.Utils.DataUnitVec unitsInSink, 
            HashSet<System.Drawing.Point> O_d,
            HashSet<System.Drawing.Point> O_p, 
            PursuerStatistics ps)
        {
            if (Evaders.Count == 0)
                return new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();

            Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();

            // TODO: heuristic question: since multiple evaders may want to find points distant from each other, this operation should be done in collaboration, somehow
            // current method is not accurate at all
            foreach (Evader eve in Evaders.Keys)
            {
                Point currentLocation = s.L[s.MostUpdatedEvadersLocationRound][eve].nodeLocation;

                // even though there is no need to get additional reward (the alg won't repair itself after it gets destroyed)
                // but if notifies that it was successfull
                //if (currentLocation.manDist(EvolutionConstants.targetPoint) <= EvolutionConstants.param.r_e)
                if (manDistFromDesiredLocation(currentLocation) == 0)
                    reportSuccess();

                if (!crawl.ContainsKey(eve) || crawl[eve].targetLocation == currentLocation)
                {
                    // we choose a point in distance RouteLength, and within MinmalRing and MaximalRing, that
                    // also satisfies (as much as possible) DistanceFromOuterRingEvaders and DistanceFromNonOuterRingEvaders

                    int diff = ((int)this[ShortParamsIdx.MaximalRing]) - ((int)this[ShortParamsIdx.MinmalRing]);
                    int len = (int)(this[FracParamsIdx.MaxRouteLength] * Math.Max(0, diff));

                    len = EvolutionUtils.threadSafeRand.rand.Next(0, len + 1) / 2;

                    Point[] options = new Point[]{
                    getPointInArea(currentLocation.add(-len,-len)),
                    getPointInArea(currentLocation.add(-len,len)),
                    getPointInArea(currentLocation.add(len,-len)), 
                    getPointInArea(currentLocation.add(len,len))};
                    //Utils.getUniformRandomPointInManDistance(len, 0, EvolutionUtils.threadSafeRand.rand),
                    //Utils.getUniformRandomPointInManDistance(len, 1, EvolutionUtils.threadSafeRand.rand),
                    //Utils.getUniformRandomPointInManDistance(len, 2, EvolutionUtils.threadSafeRand.rand),
                    //Utils.getUniformRandomPointInManDistance(len, 3, EvolutionUtils.threadSafeRand.rand)};

                    Point bestTargetPoint = options[0];
                    float bestTargetPointVal = evaluateTargetLocation(options[0], s);
                    for (int i = 1; i < 4; ++i)
                    {
                        float val = evaluateTargetLocation(options[i], s);
                        if (val > bestTargetPointVal)
                        {
                            bestTargetPoint = options[i];
                            bestTargetPointVal = val;
                        }
                    }
                    // TODO: remove below
                    //if(bestTargetPoint.manDist(EvolutionConstants.targetPoint) > EvolutionConstants.radius + 1 )
                    //{
                    //    while (true) ;
                    //}
                    float randProb = (float)crawlParamProbabilities[(int)EvaderCrawlChromosome.ProbabilityMeaningIdx.RandProb];
                    float FurthestFromPursuersProb = (float)crawlParamProbabilities[(int)EvaderCrawlChromosome.ProbabilityMeaningIdx.FurthestFromPursuersProb];
                    float DijakstraBeginingProb = (float)crawlParamProbabilities[(int)EvaderCrawlChromosome.ProbabilityMeaningIdx.DijakstraBeginingProb];
                    float DijakstraHistoryProb = (float)crawlParamProbabilities[(int)EvaderCrawlChromosome.ProbabilityMeaningIdx.DijakstraHistoryProb];

                    crawl[eve] = new EvaderCrawl(eve, s, bestTargetPoint, randProb, DijakstraBeginingProb, DijakstraHistoryProb, FurthestFromPursuersProb);
                }

                Point nextP = crawl[eve].getNextEvaderPoint(ps, O_p, EvolutionUtils.threadSafeRand.rand);

                res.Add(eve,
                    Tuple.Create(DataUnit.NIL,
                    new Location(nextP),
                    new Location(EvolutionConstants.targetPoint)));
            }

            // TODO: remove below
            //foreach (var e in res)
            //    if (e.Value.Item2.locationType != Location.Type.Node ||
            //        e.Value.Item2.nodeLocation.manDist(EvolutionConstants.targetPoint) > EvolutionConstants.radius + 1)
            //    {
            //        while (true) ;
            //    }

            return res;
        }

        public override AForge.Genetic.IChromosome CreateNewParam()
        {
            ushort[] usArr = AlgorithmUtils.getRepeatingValueArr<ushort>((ushort)EvolutionConstants.radius,(int)ShortParamsIdx.Count);
            List<IChromosome> icArr =
                AlgorithmUtils.getRepeatingValueList<IChromosome>((IChromosome)(new EvaderCrawlChromosome()), 1);
            double[] fArr = new double[]{1.0};
            return new ConcreteCompositeChromosome(
                (int)ShortParamsIdx.Count, usArr,
                (int)FracParamsIdx.Count, fArr , 10,
                EvolutionConstants.valueMutationProb,
                icArr);
        }
        public override Dictionary<string, string> getValueMap(AForge.Genetic.IChromosome param)
        {
            ConcreteCompositeChromosome p = (ConcreteCompositeChromosome)param;

            Dictionary<string, string> res = new Dictionary<string, string>();
            res["DistanceFromNonOuterRingEvaders"] = p[ConcreteCompositeChromosome.Shorts,(int)ShortParamsIdx.DistanceFromNonOuterRingEvaders].ToString();
            res["DistanceFromOuterRingEvaders"] = p[ConcreteCompositeChromosome.Shorts, (int)ShortParamsIdx.DistanceFromOuterRingEvaders].ToString();
            res["MaximalRing"] = p[ConcreteCompositeChromosome.Shorts, (int)ShortParamsIdx.MaximalRing].ToString();
            res["MinmalRing"] = p[ConcreteCompositeChromosome.Shorts, (int)ShortParamsIdx.MinmalRing].ToString();

            int diff = ((int)p[ConcreteCompositeChromosome.Shorts, (int)ShortParamsIdx.MaximalRing]) - 
                       ((int)p[ConcreteCompositeChromosome.Shorts,(int)ShortParamsIdx.MinmalRing]);
            int len = (int)(p[ConcreteCompositeChromosome.Shorts, (int)FracParamsIdx.MaxRouteLength] * Math.Max(0, diff)) / 2;
            res["MaxRouteLength"] = len.ToString();

            return res;
        }

        public override IEvaderBasicAlgorithm CreateNew(AForge.Genetic.IChromosome param)
        {
            return new SurviveAtArea((ConcreteCompositeChromosome)param);
        }
    }
}
