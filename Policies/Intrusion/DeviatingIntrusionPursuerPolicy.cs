using GoE.GameLogic;
using GoE.GameLogic.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils.Extensions;
using GoE.AppConstants;

namespace GoE.Policies
{

    /// <summary>
    /// pursuers have evenly distributed angles around 
    /// </summary>
    public class DeviatingIntrusionPursuerPolicy : AIntrusionPursuersPolicy
    {
        int currentRound;
        List<Point> O_c;
        IEnumerable<GameLogic.Utils.CapturedObservation> O_d;
        Random myrand;

        GridGameGraph G;
        IntrusionGameParams prm;
        IPolicyGUIInputProvider pgui;
        float devOutProb,devInProb;
        int maxIntrusionDist;
        float turningProb, pursuitProb;
        bool syncedPursuers;
        
        Point intrusionCenter;

        class PursuerOptions : IComparable<PursuerOptions>
        {
            public Point pursuitTargetPoint;
            public bool isPursuing;
            public bool previouslyRotated;
            public int mustContinueTo; // same as prevDirection, but forces the pursuer if not 0
            public Pursuer p;
            public float currentAngle;
            public int currentDist;
            public Point currentLocation;
            public int currentDir; // tells the previous direction of the pursuer. 1 is CW movement, -1 CCW movement, 0 is up/down deviation (pursuers can't deviant from route twice in a row)
            // deviation up/down

            public int CompareTo(PursuerOptions other)
            {
                return currentAngle.CompareTo(other.currentAngle);
            }
            public PursuerOptions clone()
            {
                PursuerOptions newP = new PursuerOptions();
                newP.mustContinueTo = mustContinueTo;
                newP.p = p;
                newP.currentAngle = currentAngle;
                newP.currentDist = currentDist;
                newP.currentLocation = currentLocation;
                newP.currentDir = currentDir;
                newP.isPursuing = isPursuing;
                return newP;
            }
        }
        List<PursuerOptions> pursuerStates = new List<PursuerOptions>();

        public override List<ArgEntry> policyInputKeys()
        {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_INWARDS_PROB);
                res.Add(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_OUTWARDS_PROB);
                res.Add(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.MAX_DISTANCE_FROM_CIRCUMFERENCE);
                res.Add(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.SYNC_PURSUERS);
                res.Add(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.TURNING_PROB);
                res.Add(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.PURSUIT_PROB);
                return res;
            
        }
        



        public override bool init(AGameGraph G, IntrusionGameParams iprm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            this.G = (GridGameGraph)G;
            this.prm = iprm;
            this.pgui = pgui;


            myrand = new ThreadSafeRandom().rand;

            //if (policyParams == null)
            //    policyParams = new Dictionary<string, string>();
            //if (pgui.hasBoardGUI())
            //    policyParams.AddRange(argNames, pgui.ShowDialog(argNames.ToArray(), "DeviatingIntrusionPursuerPolicy init", null));
           
            //devInProb = float.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_INWARDS_PROB,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_INWARDS_PROB_DEFAULT));
            devInProb = float.Parse(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_INWARDS_PROB.tryRead(policyParams));

            //devOutProb = float.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_OUTWARDS_PROB,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_OUTWARDS_PROB_DEFAULT));
            devOutProb = float.Parse(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.DEVIATE_OUTWARDS_PROB.tryRead(policyParams));

            //maxIntrusionDist = int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.MAX_DISTANCE_FROM_CIRCUMFERENCE,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.MAX_DISTANCE_FROM_CIRCUMFERENCE_DEFAULT));
            maxIntrusionDist = int.Parse(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.MAX_DISTANCE_FROM_CIRCUMFERENCE.tryRead(policyParams));


            //syncedPursuers = 1 == int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.SYNC_PURSUERS,
            //        AppConstants.Policies.DeviatingIntrusionPursuerPolicy.SYNC_PURSUERS_DEFAULT));
            syncedPursuers = 1 == int.Parse(
                AppConstants.Policies.DeviatingIntrusionPursuerPolicy.SYNC_PURSUERS.tryRead(policyParams));


            //turningProb = float.Parse(
            //   Utils.ParsingUtils.readValueOrDefault(
            //       policyParams,
            //       AppConstants.Policies.DeviatingIntrusionPursuerPolicy.TURNING_PROB,
            //       AppConstants.Policies.DeviatingIntrusionPursuerPolicy.TURNING_PROB_DEFAULT));
            turningProb = float.Parse(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.TURNING_PROB.tryRead(policyParams));

            //pursuitProb = float.Parse(
            //   Utils.ParsingUtils.readValueOrDefault(
            //       policyParams,
            //       AppConstants.Policies.DeviatingIntrusionPursuerPolicy.PURSUIT_PROB,
            //       AppConstants.Policies.DeviatingIntrusionPursuerPolicy.PURSUIT_PROB_DEFAULT));
            pursuitProb = float.Parse(AppConstants.Policies.DeviatingIntrusionPursuerPolicy.PURSUIT_PROB.tryRead(policyParams));


            intrusionCenter = this.G.getNodesByType(NodeType.Target).First();


            float ang = 0.3f;// (float)(myrand.NextDouble() * 4.0f); FIXME NOW
            foreach (Pursuer pa in prm.A_P)
            {
                pursuerStates.Add( 
                    new PursuerOptions(){currentLocation = intrusionCenter.add(GameLogic.Utils.getGridPointByAngle(prm.r_e, ang)),
                                         currentDir = 1,
                                         currentDist = prm.r_e,
                                         p = pa,
                                         currentAngle = ang,
                                         mustContinueTo = 1, 
                                         isPursuing = false});
                ang += 4.0f / prm.A_P.Count;
                ang = Utils.MathEx.modf(ang, 4);
            }
            
            minAngleDist = ((float)prm.r_p / 2) / (float)prm.r_e; // pursuers will keep this distance at the end of each step, to insure the same points won't be ever visited in a row
            return true;
        }

        float minAngleDist;
        public override void setGameState(int CurrentRound, List<Point> o_c, 
                                          IEnumerable<GameLogic.Utils.CapturedObservation> o_d)
        {
            this.currentRound = CurrentRound;
            this.O_c = o_c;
            O_d = o_d;

            if(O_c.Count > 0)
                pgui.debugStopSkippingRound(); // FIXME remove

            if(O_c.Count > 0 && myrand.NextDouble() <= pursuitProb)
            {
                int dist = int.MaxValue;
                Point nearestPoint = new Point();
                bool pursue = false;
                foreach(Point p in O_c)
                {
                    if(p.manDist(intrusionCenter) < dist)
                    {
                        nearestPoint = p;
                        dist = p.manDist(intrusionCenter);
                        if (dist <= prm.r_es + 2 * prm.r_e)
                            pursue = true;
                    }
                }
                if(pursue) // if communication was from a relatively near point, choose the nearest pursuer, and chase that pursuer
                {
                    PursuerOptions nearestPursuer = null;
                    dist = int.MaxValue;
                    foreach(PursuerOptions o in pursuerStates)
                    {
                        if(o.currentLocation.manDist(nearestPoint) < dist)
                        {
                            dist = o.currentLocation.manDist(nearestPoint);
                            nearestPursuer = o;
                        }
                    }
                    nearestPursuer.isPursuing = true;

                    do
                    {
                        nearestPursuer.pursuitTargetPoint =
                            nearestPoint.add(GoE.GameLogic.Utils.getUniformRandomPointInManDistance((int)(Math.Ceiling(((float)(dist) / prm.r_p)) + 1), myrand, true));
                    } while (nearestPursuer.pursuitTargetPoint.manDist(intrusionCenter) < prm.r_e);
                }
            }
        }

        private void updatePursuerOptions(List<Point> pathOut, PursuerOptions p, int nextDir, int nextDist)
        {
            float remainingMovement = prm.r_p;
            
            Point dest;

            //// fixme remove
            //PursuerOptions dupePursuer = p.clone();

            // if needed, update pursuer's dist (orthogonal movement takes priority over horizontal movement)
            if (nextDist != p.currentDist)
            {
                int orthDiff = nextDist - p.currentDist;
                if (orthDiff > (remainingMovement)/2)
                    orthDiff = (int)(remainingMovement)/2;
                else if (orthDiff < -(remainingMovement/2))
                    orthDiff = (int)-(remainingMovement/2);

                orthDiff -= orthDiff % 2; // nextDist must be even
                nextDist = p.currentDist + orthDiff;

                dest = p.currentLocation.add(
                    GameLogic.Utils.getOrthogonalPointDiff(p.currentLocation, intrusionCenter, p.currentDist, nextDist));
                GameLogic.Utils.addOrhotonalPath(pathOut, p.currentLocation, dest, intrusionCenter, nextDir == 1);

                //// fixme remove below
                //for (int pi = 1; pi < pathOut.Count; ++pi)
                //{
                //    if (pathOut[pi - 1].manDist(pathOut[pi]) > 1)
                //    {
                //        int a = 0;
                //        ++a;
                //    }
                //}

                remainingMovement -= p.currentLocation.manDist(dest);
                p.currentLocation = dest;
                p.currentAngle = GameLogic.Utils.getAngleOfGridPoint(dest.subtruct(intrusionCenter)); // when dist changes, angle also changes slightly
                p.currentDist = nextDist;
                

                //foreach(Point pi in pathOut)
                //{
                //    if(pi.manDist(new Point(0,0)) < 10)
                //    {
                //        // FIXME remove
                //        p = dupePursuer;
                //    }
                //}
            }

            // if needed and remainingMovement > 0, move towards the pursuer's new direction
            
            float angleDiff = remainingMovement / (2*nextDist);

            float newAngle = MathEx.modf(p.currentAngle + angleDiff * nextDir + 4,4);
            dest = intrusionCenter.add(GameLogic.Utils.getGridPointByAngle(nextDist, newAngle));
            GameLogic.Utils.addHorizontalPath(pathOut, p.currentLocation, dest, intrusionCenter, nextDir == 1);
            
            p.currentAngle = GameLogic.Utils.getAngleOfGridPoint(dest.subtruct(intrusionCenter));
            p.currentLocation = dest;
            
            p.mustContinueTo = 0; // may be turned on only by other pursuers, that collided with this one

            p.previouslyRotated = (nextDir != p.currentDir);
            p.currentDir = nextDir;
            
        }


        float syncJumpRand;
        int syncDirRand;

        // if prevPursuers[pi].mustContinueTo is 0 , then it go choose direction freely - 
        // but then also might force the following pursuer to move 
        private void updatePursuer(int pi, int piDir, bool isfirst)
        {
            PursuerOptions p = pursuerStates[pi];
            PursuerOptions nextP = pursuerStates[(pi + piDir + pursuerStates.Count) % pursuerStates.Count];
            PursuerOptions prevP = pursuerStates[(pi - piDir + pursuerStates.Count) % pursuerStates.Count];

            List<Point> res = new List<Point>();
            int dirRand;


            if(p.isPursuing)
            {
                // instead of advancing normally, just go towards destination point
                Point currentLoc = p.currentLocation;
                for (int steps = 0; steps < prm.r_p; ++steps )
                {
                    int xDiff = p.pursuitTargetPoint.X - currentLoc.X;
                    int yDiff = p.pursuitTargetPoint.Y - currentLoc.Y;
                    if (xDiff == 0 && yDiff == 0)
                        break;
                    
                    if(Math.Abs(xDiff) > Math.Abs(yDiff))
                    {
                        int dir = (xDiff > 0)?(1):(-1);
                        currentLoc = currentLoc.add(dir, 0);
                    }
                    else
                    {
                        int dir = (yDiff > 0) ? (1) : (-1);
                        currentLoc = currentLoc.add(0, dir);
                    }
                    res.Add(currentLoc);
                }

                p.previouslyRotated = false;
                p.currentLocation = res.Last();
                p.currentAngle = GameLogic.Utils.getAngleOfGridPoint(p.currentLocation.subtruct(intrusionCenter)); // when dist changes, angle also changes slightly
                p.currentDist = p.currentLocation.manDist(intrusionCenter);
                nextStepRes[p.p] = res;
                return;
            }
            if(p.currentLocation.manDist(intrusionCenter) > prm.r_e + maxIntrusionDist)
            {
                // this occurs after pursuer finished pursuit. FIXME: if pursuers are sync, right
                // not pursuers won't return to be in sync!!
                int dir = p.mustContinueTo;
                if(dir == 0)
                    dir = p.currentDir;

                updatePursuerOptions(res, p, dir, Math.Max(prm.r_e,p.currentDist - (prm.r_p/2)));
                nextStepRes[p.p] = res;
                return;
            }


            if (p.previouslyRotated)
                dirRand = p.currentDir;
            else
            {
                if (syncedPursuers)
                    dirRand = syncDirRand;
                else
                {
                    dirRand = Convert.ToInt32(myrand.NextDouble() > turningProb);

                    if (dirRand == 0)
                        dirRand = -1; // dirRand = either 1 or -1
                }
            }

            float jumpRand;

            if (syncedPursuers)
                jumpRand = syncJumpRand;
            else
                jumpRand = (float)myrand.NextDouble();
            Point newLocation = p.currentLocation;

            int nextDist = p.currentDist; 
            int nextDir = p.currentDir;


            // note that dist always increases/decreases in jumps of 2, since the path a pursuer takes is always along 2 distances
            // and if we deviate by uneven distance then continue CW or CCW (remaining r_p) - we end up either using the entire steps,
            // or changing distance at the end of the path
            //Console.WriteLine("\nManaging pursuer:    " + p.p.ID.ToString() + "*************");
            //Console.WriteLine("current dist:    " + p.currentDist.ToString());
            //Console.WriteLine("current dir:    " + p.currentDir.ToString());
            //Console.WriteLine("current angle:    " + p.currentAngle.ToString());

            if(p.mustContinueTo != 0)
            {
                //Console.WriteLine("must continue to:    " + p.mustContinueTo.ToString());

                if (p.mustContinueTo != nextDir)
                {
                    //Console.WriteLine("rotation needed");
                    // the pursuer was forced to turn around, because the previous pursuer caught up.
                    if (p.currentDist == prm.r_e)
                    {
                        nextDist = Math.Min(prm.r_e + maxIntrusionDist, prm.r_e + 2);
                        //Console.WriteLine("must rotate + current dist too low. dist increased to: " + nextDist.ToString());
                    }
                    else
                    {
                        // unless impossible, make this pursuer "faster" by decreasing it's distance
                        nextDist = Math.Max(prm.r_e, p.currentDist - 2);
                        //Console.WriteLine("dist decreaed to:    " + nextDist.ToString());
                    }
                }
                
                // note that if this pursuer was forced, it won't change dist to something higher

                nextDir = p.mustContinueTo;
            }
            else if (jumpRand < devOutProb)
            {
                // increase dist
                nextDist = Math.Min(prm.r_e + maxIntrusionDist, p.currentDist + 2);
                //Console.WriteLine("increasing dist to :    " + nextDist.ToString());
                if (nextDist != p.currentDist)
                {
                    nextDir *= dirRand; // we can change dir only if we also change dist
                    //Console.WriteLine("next dir :    " + nextDir.ToString());
                }
            }
            else if (jumpRand < devOutProb + devInProb)
            {
                // decrease dist

                nextDist = Math.Max(prm.r_e, p.currentDist - 2);
                //Console.WriteLine("decreasing dist to :    " + nextDist.ToString());
                if (nextDist != p.currentDist)
                {
                    nextDir *= dirRand; // we can change dir only if we also change dist
                    //Console.WriteLine("next dir :    " + nextDir.ToString());
                }
            }

            //bool tmp;
            //float minNextAngle = 4 * minAngleDist;
            //float nextAngle = MathEx.modf(p.currentAngle + ((float)prm.r_p * nextDir) / (2*nextDist),4);
            //if (GameLogic.Utils.getGridPointAngleDiff(nextAngle, nextP.currentAngle, out tmp) < minNextAngle)
            //{
            //    nextP.mustContinueTo = nextDir;
            //    nextDist = prm.r_e + maxIntrusionDist;
            //    //Console.WriteLine("forcing pursuer" + nextP.p.ID.ToString() + " to go in dir" + nextDir.ToString());
            //}

            // we can force the direction of next pursuer, but can't for the previous one. 
            // the previous one is a problem only if we changed direction. if it is a problem, don't change dir
            // the next one might always be a problem. if it is a problem, try to move slower + force it to more with your direction
            
            //float nextAngle = MathEx.modf(p.currentAngle + ((float)prm.r_p * nextDir) / (2*nextDist),4);
            //float diffToPrev;
            //float diffToNext;

            ////Console.WriteLine("furthest possible next angle:    " + nextAngle.ToString());

            //if(nextDir == 1)
            //{
            //    diffToNext = GameLogic.Utils.getGridPointAngleDiffCW(p.currentAngle, nextP.currentAngle);
            //    diffToPrev = GameLogic.Utils.getGridPointAngleDiffCW(p.currentAngle, prevP.currentAngle);

            //    //Console.WriteLine("next angle diff to pursuer" + nextP.p.ID.ToString() + ":    " + diffToNext.ToString());
            //    //Console.WriteLine("next angle diff to pursuer" + prevP.p.ID.ToString() + ":    " + diffToPrev.ToString());
            //}
            //else
            //{
            //    diffToNext = GameLogic.Utils.getGridPointAngleDiffCCW(p.currentAngle, nextP.currentAngle);
            //    diffToPrev = GameLogic.Utils.getGridPointAngleDiffCCW(p.currentAngle, prevP.currentAngle);

            //    //Console.WriteLine("next angle diff to pursuer " + nextP.p.ID.ToString() + ":    " + diffToNext.ToString());
            //    //Console.WriteLine("next angle diff to pursuer" + prevP.p.ID.ToString() + ":    " + diffToPrev.ToString());
            //}

            //if (p.currentDir != nextDir && p.mustContinueTo == 0)
            //{
            //    //Console.WriteLine("checking collision with prev");
            //    // prev might be a problem
            //    if (diffToPrev <= 2 * minAngleDist) // if we rotate, then when prev in it's next round will continue advancing, we must make sure it will stay have at least dist minAngleDist
            //    {
            //        //Console.WriteLine("couldn't rotate due to collision with pursuer " + prevP.p.ID.ToString());
            //        nextDir = p.currentDir;

            //        if (nextDir == 1) // recalculate diffToNext
            //            diffToNext = GameLogic.Utils.getGridPointAngleDiffCW(nextAngle, nextP.currentAngle);
            //        else
            //            diffToNext = GameLogic.Utils.getGridPointAngleDiffCCW(nextAngle, nextP.currentAngle);
            //    }
            //}

            //if(isfirst) // if this is the first processed pursuer, then we shouldn't assume that prevP was already processed and turned our "must continue to" as needed..?
            //{

            //}

            //// next might be a problem
            //// TODO: why 3* and not 2*? for some reason 2* causes problems (maybe because sometimes after movement, the angle was very small and the forced also had to rotate...)
            //float minNextAngle = 3 * minAngleDist + 2.0f / prm.r_e; // we want to make sure there is a distance of minAngleDist between nextP's location and it's "trail". assume in worst case, after this and next pursuer move in each other's direction, and on the next round the other pursuer will need to rotate
            ////if (nextP.currentDir == nextDir) 
            ////{
            ////    minNextAngle *= 2;
            ////    //Console.WriteLine("pursuer " + nextP.p.ID.ToString() + " is going in our direction!");
            ////}

            ////Console.WriteLine("test if " + nextP.p.ID.ToString() + " will have diff of at least" + minNextAngle.ToString());
            //if (diffToNext <= minNextAngle) // 2 * minAngleDist is used, since we assume the next agent is going in the opposite direction to us
            //{
            //    //Console.WriteLine("collision with" + nextP.p.ID.ToString() + " is expected");

            //    //if (p.mustContinueTo == 0)
            //    //{
            //    //    // unless you are already avoiding collision with some other pursuer, 
            //    //    // make sure you are not faster then next pursuer- even after it will turn around for us
            //    //    int preferredNextDist = Math.Max(p.currentDist + 2, Math.Max(prm.r_e + 2, nextP.currentDist));
            //    //    nextDist = Math.Min(preferredNextDist, prm.r_e + maxIntrusionDist); 
            //    //}
            //    //Console.WriteLine("bext dist: " + nextDist.ToString());
            //    nextP.mustContinueTo = nextDir;
            //    //Console.WriteLine("forcing pursuer" + nextP.p.ID.ToString() + " to go in dir" + nextDir.ToString());
            //}
            //else
            //{
            //    //Console.WriteLine("no collisions!");
            //}
            //Console.WriteLine("prev location:" + p.currentLocation.X.ToString() + "," + p.currentLocation.Y.ToString());

            
            updatePursuerOptions(res, p, nextDir, nextDist);
            nextStepRes[p.p] = res;
            //Console.WriteLine("new location:" + p.currentLocation.X.ToString() + "," + p.currentLocation.Y.ToString());
            //Console.WriteLine("new angle:" + p.currentAngle.ToString());

            
        }

        Dictionary<Pursuer, List<Point>> nextStepRes = new Dictionary<Pursuer, List<Point>>();

       



        public override Dictionary<Pursuer, List<Point>> getNextStep()
        {
            nextStepRes.Clear();
            int dirRand = myrand.Next(0, 2);

            // make sure pursuers will not collide in the next round
            int li = 0;
            int le = pursuerStates.Count;
            int ld = 1;
            if (dirRand == 0)
            {
                li = le - 1;
                le = -1;
                ld = -1;
            }

            bool prevStuck = false;
            bool collideNext = true, collidePrev = true;
            int nextI, prevI, tmpi;
            for (; li != le; li += ld)
            {
                if (pursuerStates[li].isPursuing)
                {
                    if (pursuerStates[li].currentLocation == pursuerStates[li].pursuitTargetPoint)
                        pursuerStates[li].isPursuing = false;
                }

                collideNext = collidesWithNext(li, out nextI);
                collidePrev = collidesWithPrev(li, out prevI);

                if (collideNext && collidePrev)
                {
                    //prevStuck = true;
                    continue;
                }
                //prevStuck = false;
                if (collideNext)
                {
                    if (dirRand == 1 || collidesWithNext(nextI, out tmpi))
                        pursuerStates[li].mustContinueTo = -1;
                }
                else if (collidePrev) // if previous pursuer was stuck, then if didn't previously set mustContinueTo=-1 for itself
                {
                    if (dirRand == 0 || collidesWithPrev(prevI, out tmpi))
                        pursuerStates[li].mustContinueTo = 1;
                }
                    
            }


           

            //if (pursuerStates[0].mustContinueTo != 0)
            //    dirRand = pursuerStates[0].mustContinueTo;

            //if (dirRand <= 0) //must go to -1 or random was 0
            //    for (int i = 0; i > -pursuerStates.Count; --i)
            //        updatePursuer((pursuerStates.Count + i) % pursuerStates.Count, -1, i == 0);
            //else // must go to 1 or random was 1
            //    for (int i = 0; i < pursuerStates.Count; ++i)
            //        updatePursuer(i, 1, i ==0);

            if (syncedPursuers)
            {
                // make sure all decisions done by pursuers will be done in sync
                syncJumpRand = (float)myrand.NextDouble();
                
                syncDirRand= Convert.ToInt32(myrand.NextDouble() > turningProb);
                if (syncDirRand == 0)
                    syncDirRand = -1; // dirRand = either 1 or -1
            }


            for (int i = 0; i < pursuerStates.Count; ++i)
                updatePursuer(i, 1, i ==0);

           

            // the loop finds the first li (pursuer index) which is not trapped
            //for (; collidePrev && collideNext;  li += ld)
            //{
            //    collideNext = collidesWithNext(li, out nextI);
            //    collidePrev = collidesWithPrev(li, out prevI);
            //}
            
           

            return nextStepRes;
        }

        private bool collidesWithPrev(int pi, out int prev)
        {
            bool tmpOut;
            prev = (pi - 1 + pursuerStates.Count) % pursuerStates.Count;
            return 4 * minAngleDist >= //2 * minAngleDist + (2.0f / prm.r_e) >=
                    GameLogic.Utils.getGridPointAngleDiff(pursuerStates[pi].currentAngle,
                                                          pursuerStates[prev].currentAngle, out tmpOut);
        }
        private bool collidesWithNext(int pi, out int next)
        {
            bool tmpOut;
            next = (pi + 1) % pursuerStates.Count;
            return 4 * minAngleDist >= //2 * minAngleDist + (2.0f / prm.r_e) >=
                    GameLogic.Utils.getGridPointAngleDiff(pursuerStates[pi].currentAngle,
                                                          pursuerStates[next].currentAngle, out tmpOut);
        }

        ///// <summary>
        ///// assuming 'from' and 'to' are two adjacent diagonal points,
        ///// this returns one of the two points (that is adjacent to both 'from' and 'to')
        ///// that is further from 'firtherFrom'
        ///// </summary>
        ///// <param name="from"></param>
        ///// <param name="to"></param>
        ///// <param name="furtherFrom"></param>
        ///// <returns></returns>
        //public static Point getMidPoint(Point from, Point to, Point furtherFrom)
        //{
        //    int dirx = (from.X <= to.X) ? (1) : (-1);
        //    int diry = (from.Y <= to.Y) ? (1) : (-1);

        //    Point opt1 = from.add(dirx, 0);
        //    Point opt2 = from.add(0, diry);
        //    if (opt1.manDist(furtherFrom) >= opt2.manDist(furtherFrom))
        //        return opt1;
        //    return opt2;
        //}
        

        /// <summary>
        /// creates a path that is as diagonal as possible, and never goes into intrusion area (i.e. "too close to target")
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private List<Point> createPath(Point from, Point to)
        {
            List<Point> res = new List<Point>();

            return res;

            //int dirx = (from.X <= to.X) ? (1) : (-1);
            //int diry = (from.Y <= to.Y) ? (1) : (-1);

            //Point opt1 = from.add(dirx, 0);
            //Point opt2 = from.add(0, diry);
            //if (opt1.manDist(intrusionCenter) >= opt2.manDist(intrusionCenter))
            //{

            //    for (int x = from.X; x != to.X; x += dirx)
            //        res.Add(new Point(x, from.Y));

            //    for (int y = from.Y; y != to.Y; y += diry)
            //        res.Add(new Point(to.X, y));
            //}
            //else
            //{
            //    for (int x = from.X; x != to.X; x += dirx)
            //        res.Add(new Point(x, from.Y));

            //    for (int y = from.Y; y != to.Y; y += diry)
            //        res.Add(new Point(to.X, y));
            //}
            //res.Add(to);

            
        }

    }
}
