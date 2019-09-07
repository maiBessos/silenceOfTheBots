using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE;
using GoE.GameLogic;
using GoE.Policies;
using Utils;
using GoE.Utils.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using System.Drawing;
using GoE.Utils;
using RootFuncTreeNode = GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode;
using ParamValFuncTreeNode = GoE.Utils.Algorithms.FunctionTreeNode.ParamValFuncTreeNode;
using ConstantValFuncTreeNode = GoE.Utils.Algorithms.FunctionTreeNode.ConstantValFuncTreeNode;
using GoE.AppConstants;

namespace GoE.Policies.Intrusion
{
    /// <summary>
    /// assumpes pursuers only patrol the circumference, 
    /// that the sensitive area is a square (1 edge between every two points on circumference)
    /// </summary>
    public class PPDPatrol : AIntrusionPursuersPolicy
    {
        public enum StrategyType
        {
            EvenDistances = 0,
            MiddlePursuerFlactuates = 1
        }

        public StrategyType ActiveStrategy { get { return activeStrategy; } }

        

        public override List<ArgEntry> policyInputKeys()
        {
            //get
            //{
                return new List<ArgEntry>();
            //}
        }

        public override bool init(AGameGraph G, IntrusionGameParams Prm, UI.IPolicyGUIInputProvider Pgui, Dictionary<string, string> policyParams)
        {
            this.g = (GridGameGraph)G;
            this.prm = (IntrusionGameParams)Prm;
            this.pgui = Pgui;

            //if (pgui.hasBoardGUI())
            //    policyParams.AddRange(argNames, pgui.ShowDialog(argNames.ToArray(), "StraightForwardIntruderPolicy init", null));

            //delayBetweenIntrusions = int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS,
            //        AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS_DEFAULT));


            Point target = g.getNodesByType(NodeType.Target).First();
            //Point topRight = target.subtruct(prm.r_e / 2, prm.r_e / 2);
            sensitiveArea = prm.SensitiveAreaSquare(target);//new GameLogic.Utils.Grid4Square(topRight, prm.r_e - 1);
            myRand = new ThreadSafeRandom().rand;
            return prm.IsAreaSquare;
        }

        public override void setGameState(int CurrentRound, 
                                          List<System.Drawing.Point> O_c, 
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            this.currentRound = CurrentRound;
            this.o_c = O_c;
            this.o_d = O_d;
        }

        public override Dictionary<GameLogic.Pursuer, List<System.Drawing.Point>> getNextStep()
        {
            if(currentRound == 0)
            {
                latestLocations = new Dictionary<Pursuer, List<System.Drawing.Point>>();
                float ang = 0;
                float angDiff = 4.0f/((float)prm.A_P.Count);


                foreach (Pursuer p in prm.A_P)
                {
                    latestLocations[p] = AlgorithmUtils.getRepeatingValueList(sensitiveArea.getPointFromAngle(ang),1);
                    ang += angDiff;                    
                }

                // populate unoccupiedPointCountToNextPursuer:
                for(int pi = 0; pi < prm.A_P.Count; ++pi)
                {
                    int prevPI = (prm.A_P.Count + pi - 1) % prm.A_P.Count;
                    unoccupiedPointCountToNextPursuer[prm.A_P[pi]] =
                        sensitiveArea.CWDistanceOnSquare(
                        latestLocations[prm.A_P[pi]].First(),
                        latestLocations[prm.A_P[prevPI]].First());
                }
            }
            else
            {
                double roundRand = myRand.NextDouble();
                int dir = 1;
                if (roundRand > continueForwardProb)
                    dir = -1;

                foreach (Pursuer p in prm.A_P)
                {
                    latestLocations[p] = 
                        AlgorithmUtils.getRepeatingValueList( sensitiveArea.advancePointOnCircumference(latestLocations[p].First(),dir), 1);
                }
            }

            return latestLocations;
        }

        delegate ValType DoubleToValType<ValType>(double dVal);

        /// <summary>
        /// generates stochastic matrix of the markov chain that represents the probability of paths between any segment - s0 to s(d+1), assuming
        /// s1 is adjacent to left robot and sd adjacent to right robot and robots are moving clockwise
        /// </summary>
        /// <param name="turningTime">
        /// "tau"
        /// </param>
        /// <param name="unoccupiedPointsBetweenBots">
        /// "d"
        /// </param>
        /// <param name="continueForwardProb">
        /// "p"
        /// </param>
        /// <returns></returns>
        public static MatType constructTransitionMatrix<MatType, ValType>(
            int turningTime, 
            int unoccupiedPointsBetweenBots, 
            ValType continueForwardProb,
            ValType turnAroundProb) where MatType : AMatrix<ValType>, new()
        {

            int segStatesCount = 2 * unoccupiedPointsBetweenBots;
            MatType res = AMatrix<ValType>.GenerateMatrix <MatType>(segStatesCount+1, segStatesCount+1);

            res[segStatesCount,segStatesCount] = res.fromDouble(1); // absorbing state where the robot path begins
            switch(turningTime)
            {
                case 0:
                    // note: all edges may end up in the absorbing state
                    for (int i = 1; i < segStatesCount; i += 2) // from CW states to CW/CC states
                    {
                        res[i, Math.Min(i + 2, segStatesCount)] =  turnAroundProb; // reverse direction to CC, go to next segment (towards to sd and sd+1)
                        res[i, ((i - 2) + (segStatesCount + 1)) % (segStatesCount + 1)] =  continueForwardProb; // remain CW, go to prev segment (towards to s1 and s0)
                    }
                    // the exact opposite from above loop:
                    for (int i = 0; i < segStatesCount; i += 2) // from CC states to CW/CC states
                    {
                        res[i, ((i - 2) + (segStatesCount + 1)) % (segStatesCount + 1)] =  turnAroundProb; 
                        res[i, Math.Min(i + 2, segStatesCount)] = continueForwardProb; 
                    }
                    break;
                case 1:
                    for (int i = 1; i < segStatesCount; i += 2) // from CW states to CW/CC states
                    {
                        res[i, i - 1] = turnAroundProb; // reverse direction to CC, stay on same segment
                        res[i, ((i - 2) + (segStatesCount + 1)) % (segStatesCount + 1)] =  continueForwardProb; // remain CW, go to prev segment (towards to s1 and s0) - or go to absorbing state
                    }
                    for (int i = 0; i < segStatesCount; i += 2) // from CC states to CW/CC states
                    {
                        res[i, i + 1] = turnAroundProb; // reverse direction to CW, stay on same segment
                        res[i, Math.Min(i + 2, segStatesCount)] =  continueForwardProb; // remain CC, go to next segment (towards to sd and sd+1) - or go to absorbing state
                    }
                    break;
            }

            return res;
        }

        public static MatType constructTransitionMatrixNoAbsorbingState<MatType, ValType>(
            int turningTime, 
            int unoccupiedPointsBetweenBots, 
            ValType continueForwardProb,
            ValType turnAroundProb) where MatType : AMatrix<ValType>, new()
        {
            // very similar to constructTransitionMatrix, but doesn't create an absorbing state (and states that were supposed
            // to continue to the absorbing state, go backwards instead with prob. 1)
            int segStatesCount = 2 * unoccupiedPointsBetweenBots;
            MatType res = AMatrix<ValType>.GenerateMatrix <MatType>(segStatesCount, segStatesCount);

            int i;
            switch(turningTime)
            {
                    
                case 0:
                    for (i = 3; i < segStatesCount; i += 2) // from CW states to CW/CC states
                    {
                        res[i, Math.Min(i + 2, segStatesCount)] =  turnAroundProb; // reverse direction to CC, go to next segment (towards to sd and sd+1)
                        res[i, ((i - 2) + (segStatesCount + 1)) % (segStatesCount + 1)] =  continueForwardProb; // remain CW, go to prev segment (towards to s1 and s0)
                    }
                    res[1, 2] = res.fromDouble(1);


                    // the exact opposite from above loop:
                    for (i = 0; i < segStatesCount-2; i += 2) // from CC states to CW/CC states
                    {
                        res[i, ((i - 2) + (segStatesCount + 1)) % (segStatesCount + 1)] =  turnAroundProb; 
                        res[i, Math.Min(i + 2, segStatesCount)] = continueForwardProb; 
                    }
                    res[i, i-1] = res.fromDouble(1);

                    break;
                case 1:
                    for (i = 3; i < segStatesCount; i += 2) // from CW states to CW/CC states
                    {
                        res[i, i - 1] = turnAroundProb; // reverse direction to CC, stay on same segment
                        res[i, ((i - 2) + (segStatesCount + 1)) % (segStatesCount + 1)] =  continueForwardProb; // remain CW, go to prev segment (towards to s1 and s0) - or go to absorbing state
                    }
                    res[1, 0] = res.fromDouble(1); // in the last CW state, the robot necessarily reverses direction to CC, and stays on same segment

                    for (i = 0; i < segStatesCount-2; i += 2) // from CC states to CW/CC states
                    {
                        res[i, i + 1] = turnAroundProb; // reverse direction to CW, stay on same segment
                        res[i, Math.Min(i + 2, segStatesCount)] =  continueForwardProb; // remain CC, go to next segment (towards to sd and sd+1) - or go to absorbing state
                    }
                    res[i, i+1] = res.fromDouble(1); // reverse direction to CW, stay on same segment
                    
                    break;
            }

            return res;
        }

        private List<double> calculatePPD()
        {
            List<double> res = new List<double>();
            //int d = unoccupiedPointCountToNextPursuer.First().Value;
            
            //List<float> res = new List<float>(d);

            //Matrix MT = 
            //    Matrix.Power(constructTransitionMatrix(1, d, continueForwardProb), prm.t_i);
            
            //Matrix V = Matrix.GenerateMatrix(1, 2 * d + 1);
            //for(int i = 0; i < d; ++i)
            //{
            //    V[0, 2 * i] = 1; // initial state is cw Seg_i 
            //    res.Add((float)(V * MT)[0, 2 * d + 1]); // TODO: consider implementing sparse matrix multiplication
            //    V[0, 2 * i] = 0; // undo assignment, so we can reuse V in next iteration
            //}
            
            //return res;
            switch(activeStrategy)
            {
                case StrategyType.EvenDistances:
                {
                    int d = unoccupiedPointCountToNextPursuer.First().Value;
                    List<GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode> ppds = 
                        getAllPPDFunctions(d, prm.t_i,1);
                    break;
                }
            }

            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="d">
        /// distance between every two near patrollers
        /// </param>
        /// <param name="t">
        /// time for penetration
        /// </param>
        /// <param name="continueForwardProb"></param>
        /// <param name="tau">
        /// time to turn (0 means that turning backward will jump directly to another segment)
        /// </param>
        /// <param name="MatrixType">
        /// Either MatrixOpTree or MatrixD
        /// </param>
        /// <returns></returns>
        public static List<double> FindFuncVal(int d, int t, float continueForwardProb, int tau)
        {
            List<double> res = new List<double>(d);

            MatrixD MT =
                MatrixD.Power(constructTransitionMatrix<MatrixD,double>(tau, d, continueForwardProb, 1-continueForwardProb), t);
            MatrixD V = AMatrix<double>.GenerateMatrix<MatrixD>(1, 2 * d + 1);
            for(int i = 0; i < d; ++i)
            {
                V[0, 2 * i] = 1; // initial state is cw Seg_i 
                res.Add((V * MT)[0, 2 * d + 1]); // TODO: consider implementing sparse matrix multiplication
                V[0, 2 * i] = 0; // undo assignment, so we can reuse V in next iteration
            }
            return res;
        }


        /// <summary>
        /// returns ppd for each of the d segments i.e.
        /// result[i] tells the probabilituy of capturing the evader in segment i, given:
        /// d segments, 
        /// t rounds are given for the robots to patrol, 
        /// tau rounds are needed for turning around
        /// </summary>
        /// <param name="d"></param>
        /// <param name="t"></param>
        /// <param name="tau"></param>
        /// <returns></returns>
        public static List<GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode> getAllPPDFunctions(int d, int t, int tau)
        {
            List<GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode> res =
                new List<GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode>(d);

            MatrixOpTree MT =
                MatrixOpTree.Power(
                constructTransitionMatrix<MatrixOpTree, RootFuncTreeNode>(
                    tau, 
                    d,
                    new RootFuncTreeNode(new ParamValFuncTreeNode()),
                    new RootFuncTreeNode(new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode())),
                t);


            MatrixOpTree V = MatrixOpTree.GenerateMatrix(1, 2 * d + 1);
            for (int i = 0; i < d; ++i)
            {
                V[0, 2 * i] = 1; // initial state is cw Seg_i 
                res.Add((V * MT)[0, 2 * d + 1]); // TODO: consider implementing sparse matrix multiplication
                V[0, 2 * i] = 0; // undo assignment, so we can reuse V in next iteration
            }
            return res;
        }


        //private double findP()
        //{
        //    double pOpt = 0;
            
        //}
        private Dictionary<GameLogic.Pursuer, List<System.Drawing.Point>> latestLocations;

        private GridGameGraph g;
        private IntrusionGameParams prm;
        private UI.IPolicyGUIInputProvider pgui;
        
        private int currentRound;
        private List<System.Drawing.Point> o_c;
        private IEnumerable<GameLogic.Utils.CapturedObservation> o_d;

        private Dictionary<Pursuer, int> unoccupiedPointCountToNextPursuer = new Dictionary<Pursuer,int>(); // order of pursuers is prm.A_P
        private StrategyType activeStrategy;
        private float continueForwardProb; // a.k.a. 'p' from intrusion paper
        private float changeSegSizeProb; // used for 'MiddlePursuerFlactuates' strategy. tells the prob of a pursuer changing the size of it's adjacent segments, if there just was a rotation
        private GameLogic.Utils.Grid4Square sensitiveArea;
        Random myRand;
    }
}
