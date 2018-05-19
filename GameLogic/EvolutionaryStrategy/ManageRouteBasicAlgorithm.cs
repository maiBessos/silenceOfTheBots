using AForge.Genetic;
using GoE.GameLogic.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy.EvaderSide;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic.EvolutionaryStrategy
{
    /// <summary>
    /// manages a single evader that is supposed to stay in an area, always eavesdrop, and survive- until another algorithm buys it
    /// its parameters are in what rings it is allows to travel in, and how close it wants to be evaders from both more outer and inner rings
    /// </summary>
    public class ManageRouteBasicAlgorithm : IEvaderBasicAlgorithm
    {
        enum ParamIdx : int
        {
            MinRing = 0, // minimal distance from target
            MaxRing = 1,
            MaxEvaderDistance = 2, // maximal distance between each two evaders
            RoundsBeforeEvasion = 3, // how many rounds pass from the round an evader first transmits, and the point it necessarily stops transmitting - and starts evading
            EvasionMaxDistance = 4, // after evasion, the local sink evader moves somewhere else, according to this distance, then all other evaders follow and recreate the route

            Count
        }

        private int neededEvaders;

        public override Dictionary<string, string> getValueMap(AForge.Genetic.IChromosome param)
        {
            throw new NotImplementedException();
        }
        public override List<EvaluatedEvader> getEvaderEvaluations(IEnumerable<TaggedEvader> availableAgents, GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, PursuerStatistics ps)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep(GameState s, GoE.GameLogic.Utils.DataUnitVec unitsInSink, HashSet<Point> O_d, HashSet<Point> O_p, PursuerStatistics ps)
        {
            throw new NotImplementedException();
        }

        public override void handleNewEvader(TaggedEvader gainedEvaders)
        {
            throw new NotImplementedException();
        }

        public override void loseEvader(Evader lostEvader)
        {
            throw new NotImplementedException();
        }

        public override IChromosome CreateNewParam()
        {
            throw new NotImplementedException();
        }

        public override IEvaderBasicAlgorithm CreateNew(IChromosome param)
        {
            throw new NotImplementedException();
        }
    }
}
