



//using GoE.Utils.Algorithms;
//using System;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace GoE.Utils
//{


//	public static class MatrixUtils
    //{
		
        /// <summary>
        /// expects a right stochastic matrix (aka row stochastic matrix), and returns a column vector that tells 
        /// the what the probabilities of the matrix converge to (probability to reach each state if the matrix is raised by power of infinity)
		/// <param name=initialStateRow> </param name=initialStateRow>
		/// <return> a column vector s.t. result[i] tells the probability of being in state i</return>
        /// </summary>
        //public static MatrixD  
		//getConvergedProbabilities<ValType>(MatrixD rowStochasticMatrix, 
		//								   MatrixD initialStateRow) where MatrixD : AMatrix<ValType>
        //{
          //  MatrixD res = AMatrix<ValType>.GenerateMatrix< MatrixD >(1,rowStochasticMatrix.cols,0);
//			initialStateRow * ( MatrixD.IdentityMatrix() - rowStochasticMatrix ).Invert();
	//		return res; 
      //  }
	  //
		//
        /// <summary>
        /// expects a right stochastic matrix (aka row stochastic matrix), and returns a column vector that tells 
        /// the what the probabilities of the matrix converge to (probability to reach each state if the matrix is raised by power of infinity)
		/// <param name=initialStateRow> </param name=initialStateRow>
		/// <return> a column vector s.t. result[i] tells the probability of being in state i</return>
        /// </summary>
        //public static MatrixOpTree  
		//getConvergedProbabilities<ValType>(MatrixOpTree rowStochasticMatrix, 
		//								   MatrixOpTree initialStateRow) where MatrixOpTree : AMatrix<ValType>
        //{
          //  MatrixOpTree res = AMatrix<ValType>.GenerateMatrix< MatrixOpTree >(1,rowStochasticMatrix.cols,0);
//			initialStateRow * ( MatrixOpTree.IdentityMatrix() - rowStochasticMatrix ).Invert();
	//		return res; 
      //  }
	  //
		// // foreach MatrixType in types
    //}
	


//}