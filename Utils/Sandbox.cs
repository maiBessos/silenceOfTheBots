using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Policies.Intrusion;
using GoE.Utils.Algorithms.FunctionTreeNode;
using RootFuncTreeNode = GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode;
using Symbolism;
using GoE.Utils.Algorithms;

namespace GoE.Utils.DynamicCompilation.SandboxCode
{
    public abstract class ASandBox : ReflectionUtils.DerivedInstancesProvider<ASandBox>
    {
        public abstract object func();
    }
    public class SB1 : ASandBox
    {
        public override object func()
        {
            int tw = 0; // time passed after the robot passed 
            double p = 0.9; // probability of going forward
            int d = 4; // amount of unoccupied segments
            int t = 2; // rounds for intrusion
            int tau = 1; // rotation time

            List<GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode> res =
                new List<GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode>(d);

            MatrixOpTree MT =
                MatrixOpTree.Power(
                PPDPatrol.constructTransitionMatrix<MatrixOpTree, RootFuncTreeNode>(
                    tau,
                    d,
                    new RootFuncTreeNode(new ParamValFuncTreeNode()),
                    new RootFuncTreeNode(new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode())),
                2);

            MatrixD probs = new MatrixD(2 * d + 1, 2 * d + 1);
            for (int i = 0; i < 2 * d + 1; ++i)
                for (int j = 0; j < 2 * d + 1; ++j)
                    probs[i, j] = MT[i, j].Evaluate(AlgorithmUtils.getRepeatingValueList<double>(0.1, 1));

            MatrixD V = MatrixD.GenerateMatrix<MatrixD>(1, 2 * d + 1);
            for (int i = 0; i < d; ++i)
            {
                V[0, 2 * i] = 1; // initial state is cw Seg_i 
                res.Add((V * probs)[0, 2 * d + 1]); // TODO: consider implementing sparse matrix multiplication
                V[0, 2 * i] = 0; // undo assignment, so we can reuse V in next iteration
            }
            return res;
            
            

            
            //List<Tuple<string, List<PointF>>> res = new List<Tuple<string, List<PointF>>>();

            //for(; tw < 10; ++tw)
            //{
            //    List<PointF> ppds = new List<PointF>();
            //    List<Algorithms.FunctionTreeNode.RootFuncTreeNode> ppdFuncs = PPDPatrol.getAllPPDFunctions(d, t, tau);
            //    for(int ppdi = 0; ppdi < d; ++ppdi)
            //    {

            //    }
            //    res.Add(Tuple.Create(tw.ToString(), ppds));
            //}
            //return res;
        }
        //public override object func()
        //{
        //    MatrixOpTree eignVec = new MatrixOpTree(1, 8);
        //    for (int i = 0; i < 3; ++i)
        //        eignVec[0, i] = new RootFuncTreeNode(new ConstantValFuncTreeNode(1.0 / 3));
        //    for (int i = 3; i < 8; ++i)
        //        eignVec[0, i] = new RootFuncTreeNode(new ConstantValFuncTreeNode(0));

        //    var mult = eignVec * PPDPatrol.constructTransitionMatrixNoAbsorbingState<MatrixOpTree, RootFuncTreeNode>(
        //        1,
        //        4,
        //        new RootFuncTreeNode(new ParamValFuncTreeNode()),
        //        new RootFuncTreeNode(new ConstantValFuncTreeNode(1) - new ParamValFuncTreeNode()));

        //    RootFuncTreeNode first = mult[0, 1];
        //    MatrixD res = new MatrixD(8, 1);
        //    string stringRes = "";
        //    for (int i = 0; i < 8; ++i)
        //    {
        //        stringRes += (mult[0, i].ToString() + Environment.NewLine);
        //    }

        //    return stringRes;
        //}
    }

    // sandbox for observing pretransmission intrusion policy markov chain
    public class SB2 : ASandBox
    {
        public override object func()
        {
            MatrixOpTree policyMovement = new MatrixOpTree();
            return null;
        }
    }
}
