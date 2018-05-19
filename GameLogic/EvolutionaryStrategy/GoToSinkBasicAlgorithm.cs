using AForge.Genetic;
using AForge.Math.Random;
using GoE.GameLogic.Algorithms;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic.EvolutionaryStrategy
{
    namespace EvaderSide
    {
        /// <summary>
        /// spawns all evaders on a random sink point , and lets them stay there. If an evader is not in a sink, it will go to the nearest, transmit everything, 
        /// then stays there
        /// </summary>
        public class GoToSinkAlg : IEvaderBasicAlgorithm
        {
            //enum ShortChromosomeMeaning : int
            //{
            //    EvadersCountIdx = 0,
            //    DesiredUntrasmittedDataUnitsIdx = 1
            //}
            public enum ShortIdx
            {
                DesiredUntransmittedDataUnits = 0,
                DistanceFromSinkForTransmission = 1,
                InitialClusterSize = 2,

                Count
            }
            

            public override Dictionary<string, string> getValueMap(AForge.Genetic.IChromosome param)
            {
                ConcreteCompositeChromosome p = (ConcreteCompositeChromosome)param;
                Dictionary<string, string> res = new Dictionary<string, string>();

                res["DesiredUntrasmittedDataUnits"] = p[ConcreteCompositeChromosome.Shorts, (int)ShortIdx.DesiredUntransmittedDataUnits].ToString();
                res["DistanceFromSinkForTransmission"] = p[ConcreteCompositeChromosome.Shorts, (int)ShortIdx.DistanceFromSinkForTransmission].ToString();

                return res;
            }

            private ushort DistanceFromSinkForTransmission { get; set; }
            private ushort DesiredUntrasmittedDataUnits { get; set; }
            
            private int InitialPositionClusterSize; // used for the first round in the game, only

            /// <summary>
            /// used to take responsibility on existing evaders who's previous algorithm was deconstructed - makes
            /// the evaders go (each) to the nearest sink, transmit everything, then wait for another task
            /// </summary>
            /// <param name="Evaders"></param>
            public GoToSinkAlg()
            {
                setParams(0, 0, -1);
            }


            public GoToSinkAlg(List<TaggedEvader> Evaders, ConcreteCompositeChromosome param)
            {
                setParams(param);
                handleNewEvaders(Evaders);
            }
            /// <summary>
            /// used for spawning the first evaders in the game, and for evaders with no better task 
            /// </summary>
            public GoToSinkAlg(List<TaggedEvader> Evaders, 
                               ushort initialPositionClusterSize,
                               ushort desiredUntransmittedDataUnits, 
                               ushort distanceFromSinkForTransmission)
                
            {
                setParams(desiredUntransmittedDataUnits, distanceFromSinkForTransmission, initialPositionClusterSize);
                handleNewEvaders(Evaders);
            }

            public override void handleNewEvader(TaggedEvader gainedEvader)
            {
                evadersInitialized = true;
                base.handleNewEvader(gainedEvader);
            }
            
            protected static ushort[] getMaxShorts()
            {
                ushort[] res = new ushort[(int)ShortIdx.Count];
                res[(int)ShortIdx.DesiredUntransmittedDataUnits] = (ushort)EvolutionConstants.MinSimulationRoundCount;
                res[(int)ShortIdx.DistanceFromSinkForTransmission] = (ushort)EvolutionConstants.radius;
                res[(int)ShortIdx.InitialClusterSize] = (ushort)EvolutionConstants.param.A_E.Count;
                return res;
            }
            override public IChromosome CreateNewParam()
            {
                //return new ShortArrayChromosome(Enum.GetNames(typeof(ShortChromosomeMeaning)).Length);
                return new ConcreteCompositeChromosome((int)ShortIdx.Count,getMaxShorts(),0,null,0, EvolutionConstants.valueMutationProb, null);
            }

            protected void setParams(ushort desiredUntransmittedDataUnits, 
                                     ushort distanceFromSinkForTransmission,
                                     int initialPositionClusterSize )
            {
                this.DesiredUntrasmittedDataUnits = desiredUntransmittedDataUnits;
                this.DistanceFromSinkForTransmission = distanceFromSinkForTransmission;
                this.InitialPositionClusterSize = initialPositionClusterSize;
            }
            protected void setParams(ConcreteCompositeChromosome param)
            {
                setParams(param[ConcreteCompositeChromosome.Shorts, (int)ShortIdx.DesiredUntransmittedDataUnits],
                           param[ConcreteCompositeChromosome.Shorts, (int)ShortIdx.DistanceFromSinkForTransmission],
                           param[ConcreteCompositeChromosome.Shorts, (int)ShortIdx.InitialClusterSize]);
            }

            override public IEvaderBasicAlgorithm CreateNew(IChromosome param)
            {
                ConcreteCompositeChromosome c = (ConcreteCompositeChromosome)param;
                GoToSinkAlg res = new GoToSinkAlg();
                res.setParams(c);
                return res;
            }

            override public Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep(GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, HashSet<System.Drawing.Point> O_p, PursuerStatistics ps)
            {
                Dictionary<Evader, Tuple<DataUnit, Location, Location>> res = new Dictionary<Evader, Tuple<DataUnit, Location, Location>>();
                
                if (InitialPositionClusterSize != -1)
                {
                    InitialPositionClusterSize = Math.Max(InitialPositionClusterSize, 1); // the value might be 0
                    int clusterCount = (int)Math.Ceiling(((float)Evaders.Count) / InitialPositionClusterSize);

                    var eIter = Evaders.Keys.GetEnumerator();
                    float angle = (float)EvolutionUtils.threadSafeRand.NextDouble() * 4;
                    for (int i = 0; i < clusterCount; ++i)
                    {
                        angle += 4.0f / clusterCount;
                        if (angle >= 4.0)
                            angle -= 4.0f;

                        for (int k = 0; k < InitialPositionClusterSize; ++k)
                        {
                            if (!eIter.MoveNext())
                            {
                                i = clusterCount;
                                break;
                            }

                            Location eveLoc = s.L[s.MostUpdatedEvadersLocationRound][eIter.Current];
                            
                           

                            if (eveLoc.locationType == Location.Type.Unset)
                            {
                                res[eIter.Current] = new Tuple<DataUnit, Location, Location>(
                                       DataUnit.NIL,
                                       new Location(EvolutionConstants.targetPoint.add(Utils.getGridPointByAngle(EvolutionConstants.radius, angle))),
                                       new Location(Location.Type.Undefined));
                                continue;
                            }
                            else
                            {
                                res[eIter.Current] = MoveEvader(unitsInSink,s,eIter.Current,eveLoc.nodeLocation,O_p);
                            }
                        }
                    }
                    InitialPositionClusterSize = -1;


                    // TODO: remove below
                    //foreach (var e in res)
                    //    if (e.Value.Item2.locationType != Location.Type.Node ||
                    //        e.Value.Item2.nodeLocation.manDist(EvolutionConstants.targetPoint) > EvolutionConstants.radius + 1)
                    //    {
                    //        while (true) ;
                    //    }

                    return res;
                }
                
                bool remainingData = false;
                foreach (Evader e in Evaders.Keys)
                {
                    Location eveLoc = s.L[s.MostUpdatedEvadersLocationRound][e];

                    if(eveLoc.locationType != Location.Type.Node)
                    {
                        res[e] = new Tuple<DataUnit, Location, Location>(
                            DataUnit.NIL,
                            new Location(EvolutionConstants.targetPoint.add(EvolutionConstants.radius,0)),
                            new Location(Location.Type.Undefined));
                        continue;
                    }
                    Point currentEvaderLocation = eveLoc.nodeLocation;
                    
                    //Point destPoint;
                    //DataUnit toTransmit = DataUnit.NIL;
                    //if (currentEvaderLocation.manDist(EvolutionConstants.targetPoint) == EvolutionConstants.radius)
                    //{
                    //    destPoint = currentEvaderLocation;

                    //    var untransmittedData = Utils.getUntransmittedData(unitsInSink, s, e);
                    //    if (untransmittedData.Count > 0)
                    //    {
                    //        remainingData = true;
                    //        toTransmit = untransmittedData.First();
                    //        reportSuccess();
                    //    }
                    //}
                    //else
                    //{
                    //    float currentAngle = Utils.getAngleOfGridPoint(currentEvaderLocation.subtruct(EvolutionConstants.targetPoint));
                    //    Point nearestSinkLocation = EvolutionConstants.targetPoint.add(Utils.getGridPointByAngle(EvolutionConstants.radius, currentAngle));
                        
                    //    destPoint = EvaderCrawl.getSomeAvailablePoint(currentEvaderLocation, nearestSinkLocation, O_p);
                    //}
                        
                    //res[e] = new Tuple<DataUnit, Location, Location>(
                    //    toTransmit,
                    //    new Location(destPoint),
                    //    new Location(EvolutionConstants.targetPoint));

                    var eveRes = MoveEvader(unitsInSink,s,e,currentEvaderLocation, O_p);
                    res[e] = eveRes;
                    remainingData |= (eveRes.Item1 != DataUnit.NIL);
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

            Tuple<DataUnit, Location, Location> MoveEvader(
                GoE.GameLogic.Utils.DataUnitVec unitsInSink,
                GameState s,
                Evader e,
                Point currentEvaderLocation,
                HashSet<System.Drawing.Point> O_p)
            {
                Point destPoint;
                DataUnit toTransmit = DataUnit.NIL;

                if (currentEvaderLocation.manDist(EvolutionConstants.targetPoint) >=
                    EvolutionConstants.radius - DistanceFromSinkForTransmission)
                {
                    if (Utils.getUntransmittedDataUnit(unitsInSink, s, e, out toTransmit))
                    {
                        reportSuccess();
                    }
                    else
                        toTransmit = DataUnit.NIL;
                }
                
                //if (currentEvaderLocation.manDist(EvolutionConstants.targetPoint) == DistanceFromSinkForTransmission)
                //{
                //    destPoint = currentEvaderLocation;
                //}
                //else
                //{
                    float currentAngle = Utils.getAngleOfGridPoint(currentEvaderLocation.subtruct(EvolutionConstants.targetPoint));
                    Point nearestSinkLocation = EvolutionConstants.targetPoint.add(Utils.getGridPointByAngle(EvolutionConstants.radius, currentAngle));
                    if (nearestSinkLocation == currentEvaderLocation)
                        destPoint = currentEvaderLocation;
                    else
                        destPoint = EvaderCrawl.getSomeAvailablePoint(currentEvaderLocation, nearestSinkLocation, O_p);
                //}

                return new Tuple<DataUnit, Location, Location>(
                        toTransmit,
                        new Location(destPoint),
                        new Location(EvolutionConstants.targetPoint));
            }

            public override List<EvaluatedEvader> getEvaderEvaluations(IEnumerable<TaggedEvader> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, PursuerStatistics ps)
            {
                List<EvaluatedEvader> priorityWeightPerEvader = new List<EvaluatedEvader>();

                var mem = s.M[s.MostUpdatedEvadersMemoryRound];
                // filter out irrelevant evaders, then give priority according to their untransmitted data
                foreach (var ev in availableAgents)
                {
                    var eveMem = mem[ev.e];
                    float untransmittedCount = eveMem.Count - eveMem.getIntersectionSize(unitsInSink); // (float)Utils.getUntransmittedData(unitsInSink, s, ev.e).Count;
                    double utility;
                    
                    if (untransmittedCount >= DesiredUntrasmittedDataUnits)
                        utility = 1; // we want an evader that has as close as possible to the specified amount of untransmitted data
                    else
                        utility = untransmittedCount / DesiredUntrasmittedDataUnits;
                    

                    priorityWeightPerEvader.Add(new EvaluatedEvader() { e = ev, value = utility });
                }
                
                return priorityWeightPerEvader;
            }
            bool evadersInitialized = false;
            public override RepairingNeeds getRepairingNeeds(IEnumerable<TaggedEvader> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, PursuerStatistics ps)
            {
                RepairingNeeds res = new RepairingNeeds();
                res.minEvadersCount = 1;

                if(this.Evaders.Count > 0)
                    res.minEvadersCount = 0;

                res.maxEvadersCount = int.MaxValue;
                
                res.priorityWeightPerEvader = getEvaderEvaluations(availableAgents,s,unitsInSink,O_d,ps);

                if (res.priorityWeightPerEvader.Count == 0)
                {
                    if (!evadersInitialized || Evaders.Count == 0)
                        res.minEvadersCount = -1; // we don't want/can't afford any new evader + we now have no managed evader anyway -> time to die
                    else
                        res.maxEvadersCount = res.minEvadersCount = 0; // we don't want any additional evaders, so we stay as we were
                }
                
                return res;
            }

        }   
    }
}