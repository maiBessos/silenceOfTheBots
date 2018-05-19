using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.GameLogic;
using GoE.UI;
using GoE.Policies.Intrusion.SingleTransmissionPerimOnly.Utils;
using static GoE.GameLogic.Utils;
using GoE.Utils.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;

namespace GoE.Policies.Intrusion.SingleTransmissionPerimOnly
{
    /// <summary>
    /// describes an intruder with 1 observer, 1 intruder, may transmit only once,
    /// and the observer may observe only a single segment.
    /// 
    /// If the agent designated for intruding fails, the intruder gives up
    /// </summary>
    public class SingleTransmissionPerimOnlyHeuristicIntruderPolicy : AIntrusionEvadersPolicy
    {
        int observerLocation, intruderLocation;
        int transmittingCountdown = -1, roundsBeforeIntruding = -1, remainingRoundsToIntrusion = -1;
        Grid4Square gameCircumference;
        GridGameGraph G;
        Dictionary<Evader, Point> evaderLocations = new Dictionary<Evader, Point>();
        Evader observingEvader, intrudingEvader;

#if DEBUG
        int roundOfTransmission = -1;
        int round = 0;
        Point prevIntruderLocation;
        Point finalIntruderLocation;
#endif
        public override List<Evader> communicate()
        {


            if (transmittingCountdown-- == 0)
            {

#if DEBUG
                roundOfTransmission = round;
#endif

                remainingRoundsToIntrusion = roundsBeforeIntruding;
                return AlgorithmUtils.getContainingCollection<List<Evader>, Evader>(observingEvader);
            }

#if DEBUG
            round++;
#endif
            return new List<Evader>();
        }

        public override Dictionary<Evader, Point> getNextStep()
        {
            if(remainingRoundsToIntrusion == 0)
            {
#if DEBUG
                prevIntruderLocation = evaderLocations[intrudingEvader];
#endif

                evaderLocations[intrudingEvader] =
                    gameCircumference.advancePointOnCircumference(gameCircumference.TopLeft, intruderLocation);

#if DEBUG
                finalIntruderLocation = evaderLocations[intrudingEvader];
#endif
            }
            --remainingRoundsToIntrusion; // since it begins with -1, this will not hit 0 before transmission occurs

            return evaderLocations;
        }

        public override bool init(AGameGraph graph, 
                                  IntrusionGameParams prm, 
                                  APursuersPolicy initializedPursuers, 
                                  IPolicyGUIInputProvider pgui, 
                                  Dictionary<string, string> policyParams = null)
        {
            G = (GridGameGraph)graph;
            observingEvader = prm.A_E[0];
            intrudingEvader = prm.A_E[1];
            gameCircumference = prm.SensitiveAreaSquare(G.getNodesByType(NodeType.Target)[0]);

#if DEBUG // in case initOpt isn't called
            this.observerLocation = new ThreadSafeRandom().Next() % gameCircumference.PointCount;
            this.intruderLocation = new ThreadSafeRandom().Next() % gameCircumference.PointCount;
            this.transmittingCountdown = 10 * (new ThreadSafeRandom().Next() % gameCircumference.PointCount);
            this.roundsBeforeIntruding = new ThreadSafeRandom().Next() % gameCircumference.PointCount;
            this.remainingRoundsToIntrusion = -1; 
            evaderLocations[observingEvader] = getPointNearCircumference(observerLocation);
            evaderLocations[intrudingEvader] = getPointNearCircumference(intruderLocation);
#endif

            return true;
        }

        /// <summary>
        /// the intruder will transmit in 'TransmittingCountdown' rounds,
        /// and will begin intrusion in 'IntrudingCountdown' rounds after transmitting
        /// </summary>
        /// <param name="ObserverLocation"></param>
        /// <param name="IntruderLocation"></param>
        /// <param name="TransmittingCountdown">
        /// 0 means for immediate transmission
        /// </param>
        /// <param name="intrudingCountdown"></param>
        /// <returns></returns>
        public bool initOpt(//Grid4Square GameCircumference,
                            int ObserverLocation, int IntruderLocation,
                            int TransmittingCountdown, int IntrudingCountdown)
        {
            //this.gameCircumference = GameCircumference;
            this.observerLocation = ObserverLocation;
            this.intruderLocation = IntruderLocation;
            this.transmittingCountdown = TransmittingCountdown;
            this.roundsBeforeIntruding = IntrudingCountdown;
            this.remainingRoundsToIntrusion = -1; // will become 'IntrudingCountdown' after transmission
            evaderLocations[observingEvader] = getPointNearCircumference(ObserverLocation);
            evaderLocations[intrudingEvader] = getPointNearCircumference(intruderLocation);

            return true;
        }

        /// <summary>
        /// returns a point that is one step before enetering the point 'location'
        /// </summary>
        /// <param name="location">
        /// indicates a point on the sensitive area
        /// </param>
        /// <returns></returns>
        private Point getPointNearCircumference(int location)
        {
            return
                GameLogic.Utils.increaseSquareRadius(G,
                                            gameCircumference.advancePointOnCircumference(gameCircumference.TopLeft, location),
                                            gameCircumference.Center,
                                            (Point p) => { return true; }).Value;
        }
        public bool initOpt(//Grid4Square GameCircumference,
                            int ObserverLocation, int IntruderLocation, 
                            IntruderObservationHistory2 TransmittingObservation, int IntrudingCountdown)
        {
            //this.gameCircumference = GameCircumference;
            this.observerLocation = ObserverLocation;
            this.intruderLocation = IntruderLocation;
            this.transmittingCountdown = -1; // we transmit immediately after receiving the observation
            this.roundsBeforeIntruding = IntrudingCountdown;

            throw new NotImplementedException();
        }

        public override void setGameState(int currentRound, IEnumerable<GameLogic.Utils.CapturedObservation> O_d, List<GameLogic.Utils.PursuerPathObservation> O_p, IntrusionGameState s)
        {
            if (O_d.Count() > 0)
                GaveUp = true; // only the intruder may be captured. if it was, game ends
        }
    }
}
