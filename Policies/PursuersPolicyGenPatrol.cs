//using GoE.GameLogic;
//using GoE.UI;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using GoE.GameLogic.Algorithms;

//namespace GoE.Policies
//{
//    public class PursuersPolicyGenPatrol : APursuersPolicy
//    {
//        protected GridGameGraph g;
//        protected GameParams gm;
//        protected Dictionary<Pursuer, Location> prevLocation = new Dictionary<Pursuer, Location>();
//        protected int currentRound;

//        /// <summary>
//        /// idx 0 means on circumference, idx 1 means more outer ring, idx 2 means even more outer ring etc
//        /// </summary>
//        protected List<Utils.ListRangeEnumerable<Pursuer>> circumferencePursuersPerRadius = new List<Utils.ListRangeEnumerable<Pursuer>>();
//        protected List<GoE.GameLogic.Algorithms.CircumferencePatrol> circumferencePatrols = new List<GameLogic.Algorithms.CircumferencePatrol>();
//        protected int outwardRingsCount; // tells how many of the rings are allocated for circumference and
//                                         // outwards (the rest of the rings go inwards)

//        Point t; // single considered target
//        protected void resetPursuers()
//        {
//            // reset outward rings:
//            for (int i = 0; i < outwardRingsCount; ++i)
//            {
//                if (circumferencePatrols[i] != null)
//                    continue;

//                circumferencePatrols[i].resetPursuers(g, gm.r_p,
//                    circumferencePursuersPerRadius[i],
//                    prevLocation,
//                    new Location(t), gm.r_e + i);
//            }

//            // reset inward rings:
//            int lastIdx = circumferencePursuersPerRadius.Count();
//            for (int i = outwardRingsCount; i < lastIdx; ++i)
//            {
//                if (circumferencePatrols[i] != null)
//                    continue;

//                circumferencePatrols[i].resetPursuers(g, gm.r_p,
//                    circumferencePursuersPerRadius[i],
//                    prevLocation,
//                    new Location(t), gm.r_e - (i - outwardRingsCount) - 1);
//            }
//        }
//        public override bool init(GridGameGraph G, GameParams prm, IPolicyInputProvider gui, Dictionary<string, string> PreprocessResult = null)
//        {
//            this.g = G;
//            this.gm = prm;
//            t = g.getNodesByType(NodeType.Target).First();

//            if (prm.r_p < prm.r_e * 4)
//                return false;//throw new Exception("cannot initialize PursuersGenPolicy: r_p must at least 4 * r_e (virtually unlimited, since policy's purpose is calculating a lower bound)");
//            if (g.getNodesByType(NodeType.Target).Count > 1)
//                return false;//throw new Exception("cannot initialize PursuersGenPolicy: only 1 target may be used in graph");

//            // k>= is the amount of rings in circumference or outside, and 
//            // k_< is the amount of rings inwards
//            var res = 
//                gui.ShowDialog(new string[2] { "k_>=", "k_<" }, "PursuersGenPolicy parameters - (ring # in circumference and outwards, ring # inwards)", new string[2] { "3", "0" });
//            string ringCountStr1 = res.First();
//            string ringCountStr2 = res.Last();

//            outwardRingsCount = Int32.Parse(ringCountStr1);
//            int ringCount2 = Int32.Parse(ringCountStr2);
//            string[] psiInputStrings = new string[outwardRingsCount + ringCount2 - 1];
//            string[] defaultVals = new string[outwardRingsCount + ringCount2 - 1];
//            for (int i = 0; i < outwardRingsCount + ringCount2 - 1; ++i)
//            {
//                psiInputStrings[i] = "psi_" + i.ToString();
//                defaultVals[i] = "0";
//            }
//            defaultVals[0] = "1";

//            List<string> pursuerCounts =
//                gui.ShowDialog(
//                    psiInputStrings,
//                    "PursuersGenPolicy parameters",
//                    defaultVals);

//            //double[] pursuersAtRingOut = new double[ringCount1]; //
//            //int pursuersAtCircumference;
//            //int pursuersAtOuterRing1;
//            //int pursuersAtOuterRing2;

//            //if (!Double.TryParse(pursuerCounts[0], out pursuersAtRing[0]) ||
//            //    !Double.TryParse(pursuerCounts[1], out pursuersAtRing[1]))
//            //{
//            //    throw new Exception("can't init PursuersGenPolicy - invalid psi_0/psi_1 counts input format (needed double in [0,1])");
//            //}

//            int []pursuerCountAtRing = new int[outwardRingsCount + ringCount2];
//            int remainingPursuers = prm.A_P.Count;
            
//            for (int i = 0; i < outwardRingsCount + ringCount2 - 1; ++i)
//            {
//                pursuerCountAtRing[i] = (int)(remainingPursuers * Double.Parse(pursuerCounts[i]));
//                remainingPursuers -= pursuerCountAtRing[i];
//            }
//            pursuerCountAtRing[outwardRingsCount + ringCount2 - 1] = remainingPursuers;

//            //    pursuersAtCircumference = (int)(prm.A_P.Count * pursuersAtRing[0]);
//            //pursuersAtOuterRing1 = (int)((prm.A_P.Count - pursuersAtCircumference) * pursuersAtRing[1]);
//            //pursuersAtOuterRing2 = prm.A_P.Count - pursuersAtOuterRing1 - pursuersAtCircumference;
//            int prevIndex = 0;
//            foreach(int pc in pursuerCountAtRing)
//            {
//                circumferencePursuersPerRadius.Add(
//                    new Utils.ListRangeEnumerable<Pursuer>(prm.A_P, prevIndex, prevIndex + pc));
//                prevIndex += pc;
//                circumferencePatrols.Add(new GameLogic.Algorithms.CircumferencePatrol());
//            }
//            return true;
            
//                //// add patrol for circumference ring:
//                //circumferencePursuersPerRadius.Add(
//                //    new Utils.ListRangeEnumerable<Pursuer>(prm.A_P,
//                //        0, pursuersAtCircumference));
//                //circumferencePatrols.Add(new GameLogic.Algorithms.CircumferencePatrol());

//                //// add patrol for next ring:
//                //circumferencePursuersPerRadius.Add(
//                //    new Utils.ListRangeEnumerable<Pursuer>(prm.A_P,
//                //        pursuersAtCircumference, pursuersAtCircumference + pursuersAtOuterRing1));
//                //circumferencePatrols.Add(new GameLogic.Algorithms.CircumferencePatrol());

//                //// add patrol for outmost ring:
//                //circumferencePursuersPerRadius.Add(
//                //    new Utils.ListRangeEnumerable<Pursuer>(prm.A_P,
//                //        pursuersAtCircumference + pursuersAtOuterRing1, prm.A_P.Count));
//                //circumferencePatrols.Add(new GameLogic.Algorithms.CircumferencePatrol());
//        }

//        public override void setGameState(int CurrentRound, List<Point> O_c, IEnumerable<Point> O_d)
//        {
//            this.currentRound = CurrentRound;
//        }

//        public override Dictionary<Pursuer, Location> getNextStep()
//        {
//            if (currentRound == 0)
//            {
//                //Point t = g.getNodesByType(NodeType.Target).First();

//                // init outward rings
//                for (int i = 0; i < outwardRingsCount; ++i)
//                {
//                    if (circumferencePursuersPerRadius[i].Count() < new CircumferencePatrol().minimalPursuersCount(gm.r_p, gm.r_e + i))
//                    {
//                        circumferencePatrols[i] = null;
//                        continue;
//                    }
//                    circumferencePatrols[i].Init(g, gm.r_p,
//                        circumferencePursuersPerRadius[i],
//                        prevLocation,
//                        new Location(t), gm.r_e + i);
//                }
//                // init inward rings
//                for (int i = 0; i < circumferencePursuersPerRadius.Count() - outwardRingsCount; ++i)
//                {
//                    if (circumferencePursuersPerRadius[outwardRingsCount + i].Count() < new CircumferencePatrol().minimalPursuersCount(gm.r_p, gm.r_e + i))
//                    {
//                        circumferencePatrols[outwardRingsCount + i] = null;
//                        continue;
//                    }
//                    circumferencePatrols[outwardRingsCount + i].Init(g, gm.r_p,
//                        circumferencePursuersPerRadius[outwardRingsCount + i],
//                        prevLocation,
//                        new Location(t), gm.r_e - i - 1);
//                }
//            }
//            else
//            {

//                //Point t = g.getNodesByType(NodeType.Target).First();

//                // advance outward rings:
//                for (int i = 0; i < outwardRingsCount; ++i)
//                {
//                    if (circumferencePatrols[i] != null)
//                        continue;

//                    circumferencePatrols[i].AdvancePursuers(g, gm.r_p,
//                        circumferencePursuersPerRadius[i],
//                        prevLocation,
//                        new Location(t), gm.r_e + i);
//                }

//                // advance inward rings:
//                int lastIdx = circumferencePursuersPerRadius.Count();
//                for (int i = outwardRingsCount; i < lastIdx; ++i)
//                {
//                    if (circumferencePatrols[i] != null)
//                        continue;

//                    circumferencePatrols[i].AdvancePursuers(g, gm.r_p,
//                        circumferencePursuersPerRadius[i],
//                        prevLocation,
//                        new Location(t), gm.r_e - (i-outwardRingsCount) - 1);
//                }

//            }


//            return prevLocation;
//        }
//    }
    
    
//}
