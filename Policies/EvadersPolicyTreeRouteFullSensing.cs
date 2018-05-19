//using GoE.GameLogic;
//using GoE.UI;
//using System;
//using System.Collections.Generic;
//using System.Drawing;

//namespace GoE.Policies
//{
//    /// <summary>
//    /// a policy that randomly creates a route from target to sink 
//    /// 1) the evaders assume that in general, they have a constant probability for being captured each round
//    /// 2) 1 data unit is accumulated from the sensitive area. when the evader that accumulated escapes the area,
//    /// there are 1-2 evaders that may listen to the packet, and advance the data by at least x * r_s towards the target (x in [0,1])
//    /// 3) each evader either:
//    /// -walks towards the closest sink, and each of the evaders in it's range also do this (and this propogates forward),
//    /// -transmits the data the the 1-2 evaders near it. 
//    /// the decision depends on the assumption that if all the pursuers in the world will go towards the evader, 
//    /// then when the first of them reaches the dirty set, the dirty set becomes so big that the ratio between pursuers count and dirty set size means that the evader has a better chance to survive than the expected amount of total captured evaders in the last x * r_s  rounds
//    /// 
//    /// - should we add diversions? setting up more than 1 route, so if all pursuers attack 1 route, they necessarily
//    /// don't guard the other route
//    /// - how about false transmissions? evaders that didn't necessarily get data may send noise in order to draw
//    /// pursuers towards their direction
//    /// </summary>
//    class EvadersPolicyTreeRouteFullSensing : AEvadersPolicy
//    {
//        // we need to calculate the probability of a data unit to reach a sink, as a function of it's distance from a sink, and what is the current dirty set (i.e. the probability of the pursuers to intercept it)
//        // this way, when we move the packet forward, if we increase by X the probability of getting a packet, we can calculate how many expected captured evaders are worth the transmission

//        // suggestion - should evaders by "safe" if they are inside a sink point?

//        // calculate min. expected utility of one evader that crawls alone (and derive how many evaders die per data unit)

//        // problem - why should we use multiple evaders and a route? having the evaders in the danger zone is bad, and the routing - even though it spares time, it rarely makes up for it

//        //option: add scattered evaders for distraction - may help with bound, because it increases the possible location for the next node
//        //option: make more than one route

//        // when making a route from sink to target, consider the question - 
//        // if one(any) node in the route transmits, what is the dirty set in which the next evaders may be

//        // when an evader transmits, we assume all pursuers chase it.
//        // 1. find distance between evader and closest pursuer
//        // 2. assume when the first pursuer gets to the dirty set, ALL pursuers get there
//        // 3. assume the dirty set spreads slowly (+4D-pursuer_count) each round. calculate 
//        // (should be a geom. series) the probability of capturing the evader before the dirty set becomes so large, that the probability of capturing the evader is the same as catching ANY evader by random walking

//        private GameState prevS;
//        private IEnumerable<Point> prevO_d;
//        private HashSet<Point> prevO_p;

//        private GridGameGraph g;
//        private GoEGameParams gm;
//        private IPolicyGUIInputProvider pgui;

//        double evaderCaptureProbPerRound; // bounds from above the probability of an evader for being captured, each round, 
//        // if it doesn't tramsit

//        private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();
//        List<Point> closestSinks = new List<Point>();
//        int targetToClosestSinksDist;

//        public override void setGameState(int currentRound, IEnumerable<Point> O_d, HashSet<Point> O_p, GameState s)
//        {
//            prevS = s;
//            prevO_p = O_p;
//            prevO_d = O_d;

//        }

//        public override Dictionary<Evader, Tuple<DataUnit, Location, Location>> getNextStep()
//        {
//            throw new NotImplementedException();
//        }

//        public override bool init(GridGameGraph G, GoEGameParams prm, AGoEPursuersPolicy p, IPolicyGUIInputProvider pgui)
//        {
//            this.g = G;
//            this.gm = prm;
//            this.pgui = pgui;


//            foreach (Evader e in gm.A_E)
//                currentEvadersLocations[e] = new Location(Location.Type.Unset);

//            return true;
//        }
//    }
//}
