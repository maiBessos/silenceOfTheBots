/*
    Matrix class in C#
    Written by Ivan Kuckir (ivan.kuckir@gmail.com, http://blog.ivank.net)
    Faculty of Mathematics and Physics
    Charles University in Prague
    (C) 2010
    - updated on 01.06.2014 - Trimming the string before parsing
    - updated on 14.06.2012 - parsing improved. Thanks to Andy!
    - updated on 03.10.2012 - there was a terrible bug in LU, SoLE and Inversion. Thanks to Danilo Neves Cruz for reporting that!
    - updated on 21.01.2014 - multiple changes based on comments -> see git for further info
	
    This code is distributed under MIT licence.
	
		Permission is hereby granted, free of charge, to any person
		obtaining a copy of this software and associated documentation
		files (the "Software"), to deal in the Software without
		restriction, including without limitation the rights to use,
		copy, modify, merge, publish, distribute, sublicense, and/or sell
		copies of the Software, and to permit persons to whom the
		Software is furnished to do so, subject to the following
		conditions:
		The above copyright notice and this permission notice shall be
		included in all copies or substantial portions of the Software.
		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
		EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
		OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
		NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
		HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
		WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
		FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
		OTHER DEALINGS IN THE SOFTWARE.
*/

using GoE.Utils.Algorithms;
using System.Text;
using System.Text.RegularExpressions;
using GoE.Utils.Algorithms.FunctionTreeNode;

namespace GoE.Utils
{
    using Random = System.Random;
    using Boolean = System.Boolean;

    /// <summary>
    /// NOTE: many methods were commented, since treating RootFuncTreeNode like
    /// normal numbers is not entirely trivial. in some places, there are operations done to the values
    /// depending on the result of an if(). Even though this can be achieved by extending FuncTreeNode further,
    /// this was not necessary for the specific use in this matrix when this class was written
    /// </summary>
    public class MatrixOpTree : AMatrix<RootFuncTreeNode> 
    {
        //public int rows;
        //public int cols;
        //public RootFuncTreeNode[] mat;

        public MatrixOpTree L;
        public MatrixOpTree U;
        private int[] pi;
        private RootFuncTreeNode detOfP = 1;

        public MatrixOpTree()
        {
            rows = 0;
            cols = 0;
            mat = null;
        }
        public MatrixOpTree(int iRows, int iCols)         // Matrix Class constructor
        {
            rows = iRows;
            cols = iCols;
            mat = new RootFuncTreeNode[rows * cols];
        }
        public System.Boolean IsSquare()
        {
            return (rows == cols);
        }

        public override RootFuncTreeNode fromDouble(double v)
        {
            return new RootFuncTreeNode(new ConstantValFuncTreeNode(v));
        }

        public MatrixOpTree GetCol(int k)
        {
            MatrixOpTree m = new MatrixOpTree(rows, 1);
            for (int i = 0; i < rows; i++) m[i, 0] = this[i, k];
            return m;
        }

        public void SetCol(MatrixOpTree v, int k)
        {
            for (int i = 0; i < rows; i++) this[i, k] = v[i, 0];
        }

        public void MakeLU()                        // Function for LU decomposition
        {
            if (!IsSquare()) throw new MException("The matrix is not square!");
            L = IdentityMatrix(rows, cols);
            U = Duplicate();

            pi = new int[rows];
            for (int i = 0; i < rows; i++) pi[i] = i;

            RootFuncTreeNode p = 0;
            RootFuncTreeNode pom2;
            int k0 = 0;
            int pom1 = 0;

            for (int k = 0; k < cols - 1; k++)
            {
                p = 0;
                int i;
                for (i = k; i < rows; i++)      // find the row with the biggest pivot
                {
                    //if (Math.Abs(U[i, k]) > p)
                    if(U[i,k] != 0)
                    {
                        k0 = i;
                        break;
                    }
                }
                if (i == rows) // samé nuly ve sloupci
                    throw new MException("The matrix is singular!");

                pom1 = pi[k]; pi[k] = pi[k0]; pi[k0] = pom1;    // switch two rows in permutation matrix

                for (i = 0; i < k; i++)
                {
                    pom2 = L[k, i]; L[k, i] = L[k0, i]; L[k0, i] = pom2;
                }

                if (k != k0) detOfP *= -1;

                for (i = 0; i < cols; i++)                  // Switch rows in U
                {
                    pom2 = U[k, i]; U[k, i] = U[k0, i]; U[k0, i] = pom2;
                }

                for (i = k + 1; i < rows; i++)
                {
                    L[i, k] = U[i, k] / U[k, k];
                    for (int j = k; j < cols; j++)
                        U[i, j] = U[i, j] - L[i, k] * U[k, j];
                }
            }
        }

        public MatrixOpTree SolveWith(MatrixOpTree v)                        // Function solves Ax = v in confirmity with solution vector "v"
        {
            if (rows != cols) throw new MException("The matrix is not square!");
            if (rows != v.rows) throw new MException("Wrong number of results in solution vector!");
            if (L == null) MakeLU();

            MatrixOpTree b = new MatrixOpTree(rows, 1);
            for (int i = 0; i < rows; i++) b[i, 0] = v[pi[i], 0];   // switch two items in "v" due to permutation matrix

            MatrixOpTree z = SubsForth(L, b);
            MatrixOpTree x = SubsBack(U, z);

            return x;
        }

        // TODO check for redundancy with MakeLU() and SolveWith()
        public void MakeRref()                                    // Function makes reduced echolon form
        {
            int lead = 0;
            for (int r = 0; r < rows; r++)
            {
                if (cols <= lead) break;
                int i = r;
                while (this[i, lead] == 0)
                {
                    i++;
                    if (i == rows)
                    {
                        i = r;
                        lead++;
                        if (cols == lead)
                        {
                            lead--;
                            break;
                        }
                    }
                }
                for (int j = 0; j < cols; j++)
                {
                    RootFuncTreeNode temp = this[r, j];
                    this[r, j] = this[i, j];
                    this[i, j] = temp;
                }
                RootFuncTreeNode div = this[r, lead];
                for (int j = 0; j < cols; j++) this[r, j] /= div;
                for (int j = 0; j < rows; j++)
                {
                    if (j != r)
                    {
                        RootFuncTreeNode sub = this[j, lead];
                        for (int k = 0; k < cols; k++) this[j, k] -= (sub * this[r, k]);
                    }
                }
                lead++;
            }
        }

        public MatrixOpTree Invert()                                   // Function returns the inverted matrix
        {

            if (L == null) MakeLU();

            MatrixOpTree inv = new MatrixOpTree(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                MatrixOpTree Ei = MatrixOpTree.GenerateMatrix(rows, 1);
                Ei[i, 0] = 1;
                MatrixOpTree col = SolveWith(Ei);
                inv.SetCol(col, i);
            }
            return inv;
        }


        //public RootFuncTreeNode Det()                         // Function for determinant
        //{
        //    if (L == null) MakeLU();
        //    RootFuncTreeNode det = detOfP;
        //    for (int i = 0; i < rows; i++) det *= U[i, i];
        //    return det;
        //}

        //public MatrixOpTree GetP()                        // Function returns permutation matrix "P" due to permutation vector "pi"
        //{
        //    if (L == null) MakeLU();

        //    MatrixOpTree matrix = GenerateMatrix(rows, cols);
        //    for (int i = 0; i < rows; i++) matrix[pi[i], i] = 1;
        //    return matrix;
        //}

        public MatrixOpTree Duplicate()                   // Function returns the copy of this matrix
        {
            MatrixOpTree matrix = new MatrixOpTree(rows, cols);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    matrix[i, j] = this[i, j];
            return matrix;
        }

        public static MatrixOpTree SubsForth(MatrixOpTree A, MatrixOpTree b)          // Function solves Ax = b for A as a lower triangular matrix
        {
            if (A.L == null) A.MakeLU();
            int n = A.rows;
            MatrixOpTree x = new MatrixOpTree(n, 1);

            for (int i = 0; i < n; i++)
            {
                x[i, 0] = b[i, 0];
                for (int j = 0; j < i; j++) x[i, 0] -= A[i, j] * x[j, 0];
                x[i, 0] = x[i, 0] / A[i, i];
            }
            return x;
        }

        public static MatrixOpTree SubsBack(MatrixOpTree A, MatrixOpTree b)           // Function solves Ax = b for A as an upper triangular matrix
        {
            if (A.L == null) A.MakeLU();
            int n = A.rows;
            MatrixOpTree x = new MatrixOpTree(n, 1);

            for (int i = n - 1; i > -1; i--)
            {
                x[i, 0] = b[i, 0];
                for (int j = n - 1; j > i; j--) x[i, 0] -= A[i, j] * x[j, 0];
                x[i, 0] = x[i, 0] / A[i, i];
            }
            return x;
        }

        public static MatrixOpTree GenerateMatrix(int iRows, int iCols)       // Function generates the zero matrix
        {
            MatrixOpTree matrix = new MatrixOpTree(iRows, iCols);
            for (int i = 0; i < iRows; i++)
                for (int j = 0; j < iCols; j++)
                    matrix[i, j] = 0;
            return matrix;
        }

        public static MatrixOpTree IdentityMatrix(int iRows, int iCols)   // Function generates the identity matrix
        {
            MatrixOpTree matrix = GenerateMatrix(iRows, iCols);
            for (int i = 0; i < System.Math.Min(iRows, iCols); i++)
                matrix[i, i] = 1;
            return matrix;
        }

        public static MatrixOpTree RandomMatrix(int iRows, int iCols, int dispersion)       // Function generates the random matrix
        {
            Random random = new Random();
            MatrixOpTree matrix = new MatrixOpTree(iRows, iCols);
            for (int i = 0; i < iRows; i++)
                for (int j = 0; j < iCols; j++)
                    matrix[i, j] = random.Next(-dispersion, dispersion);
            return matrix;
        }

        //public static MatrixOpTree Parse(string ps)                        // Function parses the matrix from string
        //{
        //    string s = NormalizeMatrixString(ps);
        //    string[] rows = Regex.Split(s, "\r\n");
        //    string[] nums = rows[0].Split(' ');
        //    MatrixOpTree matrix = new MatrixOpTree(rows.Length, nums.Length);
        //    try
        //    {
        //        for (int i = 0; i < rows.Length; i++)
        //        {
        //            nums = rows[i].Split(' ');
        //            for (int j = 0; j < nums.Length; j++) matrix[i, j] = RootFuncTreeNode.Parse(nums[j]);
        //        }
        //    }
        //    catch (FormatException) { throw new MException("Wrong input format!"); }
        //    return matrix;
        //}

        //public override string ToString()                           // Function returns matrix as a string
        //{
        //    StringBuilder s = new StringBuilder();
        //    for (int i = 0; i < rows; i++)
        //    {
        //        for (int j = 0; j < cols; j++)
        //            s.Append(String.Format("{0,5:E2}", this[i, j]) + " ");
        //        s.AppendLine();
        //    }
        //    return s.ToString();
        //}

        public static MatrixOpTree Transpose(MatrixOpTree m)              // Matrix transpose, for any rectangular matrix
        {
            MatrixOpTree t = new MatrixOpTree(m.cols, m.rows);
            for (int i = 0; i < m.rows; i++)
                for (int j = 0; j < m.cols; j++)
                    t[j, i] = m[i, j];
            return t;
        }

        public static MatrixOpTree Power(MatrixOpTree m, int pow)           // Power matrix to exponent
        {
            if (pow == 0) return IdentityMatrix(m.rows, m.cols);
            if (pow == 1) return m.Duplicate();
            if (pow == -1) return m.Invert();

            MatrixOpTree x;
            if (pow < 0) { x = m.Invert(); pow *= -1; }
            else x = m.Duplicate();

            MatrixOpTree ret = IdentityMatrix(m.rows, m.cols);
            while (pow != 0)
            {
                if ((pow & 1) == 1) ret *= x;
                x *= x;
                pow >>= 1;
            }
            return ret;
        }

        private static void SafeAplusBintoC(MatrixOpTree A, int xa, int ya, MatrixOpTree B, int xb, int yb, MatrixOpTree C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++)     // cols
                {
                    C[i, j] = 0;
                    if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
                    if (xb + j < B.cols && yb + i < B.rows) C[i, j] += B[yb + i, xb + j];
                }
        }

        private static void SafeAminusBintoC(MatrixOpTree A, int xa, int ya, MatrixOpTree B, int xb, int yb, MatrixOpTree C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++)     // cols
                {
                    C[i, j] = 0;
                    if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
                    if (xb + j < B.cols && yb + i < B.rows) C[i, j] -= B[yb + i, xb + j];
                }
        }

        private static void SafeACopytoC(MatrixOpTree A, int xa, int ya, MatrixOpTree C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++)     // cols
                {
                    C[i, j] = 0;
                    if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
                }
        }

        private static void AplusBintoC(MatrixOpTree A, int xa, int ya, MatrixOpTree B, int xb, int yb, MatrixOpTree C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++) C[i, j] = A[ya + i, xa + j] + B[yb + i, xb + j];
        }

        private static void AminusBintoC(MatrixOpTree A, int xa, int ya, MatrixOpTree B, int xb, int yb, MatrixOpTree C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++) C[i, j] = A[ya + i, xa + j] - B[yb + i, xb + j];
        }

        private static void ACopytoC(MatrixOpTree A, int xa, int ya, MatrixOpTree C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++) C[i, j] = A[ya + i, xa + j];
        }

        // TODO assume matrix 2^N x 2^N and then directly call StrassenMultiplyRun(A,B,?,1,?)
        private static MatrixOpTree StrassenMultiply(MatrixOpTree A, MatrixOpTree B)                // Smart matrix multiplication
        {
            if (A.cols != B.rows) throw new MException("Wrong dimension of matrix!");

            MatrixOpTree R;

            int msize = System.Math.Max(System.Math.Max(A.rows, A.cols), System.Math.Max(B.rows, B.cols));

            int size = 1; int n = 0;
            while (msize > size) { size *= 2; n++; };
            int h = size / 2;


            MatrixOpTree[,] mField = new MatrixOpTree[n, 9];

            /*
             *  8x8, 8x8, 8x8, ...
             *  4x4, 4x4, 4x4, ...
             *  2x2, 2x2, 2x2, ...
             *  . . .
             */

            int z;
            for (int i = 0; i < n - 4; i++)          // rows
            {
                z = (int)System.Math.Pow(2, n - i - 1);
                for (int j = 0; j < 9; j++) mField[i, j] = new MatrixOpTree(z, z);
            }

            SafeAplusBintoC(A, 0, 0, A, h, h, mField[0, 0], h);
            SafeAplusBintoC(B, 0, 0, B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 1], 1, mField); // (A11 + A22) * (B11 + B22);

            SafeAplusBintoC(A, 0, h, A, h, h, mField[0, 0], h);
            SafeACopytoC(B, 0, 0, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 2], 1, mField); // (A21 + A22) * B11;

            SafeACopytoC(A, 0, 0, mField[0, 0], h);
            SafeAminusBintoC(B, h, 0, B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 3], 1, mField); //A11 * (B12 - B22);

            SafeACopytoC(A, h, h, mField[0, 0], h);
            SafeAminusBintoC(B, 0, h, B, 0, 0, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 4], 1, mField); //A22 * (B21 - B11);

            SafeAplusBintoC(A, 0, 0, A, h, 0, mField[0, 0], h);
            SafeACopytoC(B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 5], 1, mField); //(A11 + A12) * B22;

            SafeAminusBintoC(A, 0, h, A, 0, 0, mField[0, 0], h);
            SafeAplusBintoC(B, 0, 0, B, h, 0, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 6], 1, mField); //(A21 - A11) * (B11 + B12);

            SafeAminusBintoC(A, h, 0, A, h, h, mField[0, 0], h);
            SafeAplusBintoC(B, 0, h, B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 7], 1, mField); // (A12 - A22) * (B21 + B22);

            R = new MatrixOpTree(A.rows, B.cols);                  // result

            /// C11
            for (int i = 0; i < System.Math.Min(h, R.rows); i++)          // rows
                for (int j = 0; j < System.Math.Min(h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 1][i, j] + mField[0, 1 + 4][i, j] - mField[0, 1 + 5][i, j] + mField[0, 1 + 7][i, j];

            /// C12
            for (int i = 0; i < System.Math.Min(h, R.rows); i++)          // rows
                for (int j = h; j < System.Math.Min(2 * h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 3][i, j - h] + mField[0, 1 + 5][i, j - h];

            /// C21
            for (int i = h; i < System.Math.Min(2 * h, R.rows); i++)          // rows
                for (int j = 0; j < System.Math.Min(h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 2][i - h, j] + mField[0, 1 + 4][i - h, j];

            /// C22
            for (int i = h; i < System.Math.Min(2 * h, R.rows); i++)          // rows
                for (int j = h; j < System.Math.Min(2 * h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 1][i - h, j - h] - mField[0, 1 + 2][i - h, j - h] + mField[0, 1 + 3][i - h, j - h] + mField[0, 1 + 6][i - h, j - h];

            return R;
        }
        private static void StrassenMultiplyRun(MatrixOpTree A, MatrixOpTree B, MatrixOpTree C, int l, MatrixOpTree[,] f)    // A * B into C, level of recursion, matrix field
        {
            int size = A.rows;
            int h = size / 2;

            AplusBintoC(A, 0, 0, A, h, h, f[l, 0], h);
            AplusBintoC(B, 0, 0, B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 1], l + 1, f); // (A11 + A22) * (B11 + B22);

            AplusBintoC(A, 0, h, A, h, h, f[l, 0], h);
            ACopytoC(B, 0, 0, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 2], l + 1, f); // (A21 + A22) * B11;

            ACopytoC(A, 0, 0, f[l, 0], h);
            AminusBintoC(B, h, 0, B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 3], l + 1, f); //A11 * (B12 - B22);

            ACopytoC(A, h, h, f[l, 0], h);
            AminusBintoC(B, 0, h, B, 0, 0, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 4], l + 1, f); //A22 * (B21 - B11);

            AplusBintoC(A, 0, 0, A, h, 0, f[l, 0], h);
            ACopytoC(B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 5], l + 1, f); //(A11 + A12) * B22;

            AminusBintoC(A, 0, h, A, 0, 0, f[l, 0], h);
            AplusBintoC(B, 0, 0, B, h, 0, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 6], l + 1, f); //(A21 - A11) * (B11 + B12);

            AminusBintoC(A, h, 0, A, h, h, f[l, 0], h);
            AplusBintoC(B, 0, h, B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 7], l + 1, f); // (A12 - A22) * (B21 + B22);

            /// C11
            for (int i = 0; i < h; i++)          // rows
                for (int j = 0; j < h; j++)     // cols
                    C[i, j] = f[l, 1 + 1][i, j] + f[l, 1 + 4][i, j] - f[l, 1 + 5][i, j] + f[l, 1 + 7][i, j];

            /// C12
            for (int i = 0; i < h; i++)          // rows
                for (int j = h; j < size; j++)     // cols
                    C[i, j] = f[l, 1 + 3][i, j - h] + f[l, 1 + 5][i, j - h];

            /// C21
            for (int i = h; i < size; i++)          // rows
                for (int j = 0; j < h; j++)     // cols
                    C[i, j] = f[l, 1 + 2][i - h, j] + f[l, 1 + 4][i - h, j];

            /// C22
            for (int i = h; i < size; i++)          // rows
                for (int j = h; j < size; j++)     // cols
                    C[i, j] = f[l, 1 + 1][i - h, j - h] - f[l, 1 + 2][i - h, j - h] + f[l, 1 + 3][i - h, j - h] + f[l, 1 + 6][i - h, j - h];
        }
        private static MatrixOpTree StupidMultiply(MatrixOpTree m1, MatrixOpTree m2)                  // Stupid matrix multiplication
        {
            if (m1.cols != m2.rows) throw new MException("Wrong dimensions of matrix!");

            MatrixOpTree result = GenerateMatrix(m1.rows, m2.cols);
            for (int i = 0; i < result.rows; i++)
                for (int j = 0; j < result.cols; j++)
                    for (int k = 0; k < m1.cols; k++)
                        result[i, j] += m1[i, k] * m2[k, j];
            return result;
        }

        private static MatrixOpTree Multiply(MatrixOpTree m1, MatrixOpTree m2)                         // Matrix multiplication
        {
            if (m1.cols != m2.rows) throw new MException("Wrong dimension of matrix!");
            int msize = System.Math.Max(System.Math.Max(m1.rows, m1.cols), System.Math.Max(m2.rows, m2.cols));
            // stupid multiplication faster for small matrices
            if (msize < 32)
            {
                return StupidMultiply(m1, m2);
            }
            // stupid multiplication faster for non square matrices
            if (!m1.IsSquare() || !m2.IsSquare())
            {
                return StupidMultiply(m1, m2);
            }
            // Strassen multiplication is faster for large square matrix 2^N x 2^N
            // NOTE because of previous checks msize == m1.cols == m1.rows == m2.cols == m2.cols
            double exponent = System.Math.Log(msize) / System.Math.Log(2);
            if (System.Math.Pow(2, exponent) == msize)
            {
                return StrassenMultiply(m1, m2);
            }
            else
            {
                return StupidMultiply(m1, m2);
            }
        }
        private static MatrixOpTree Multiply(RootFuncTreeNode n, MatrixOpTree m)                          // Multiplication by constant n
        {
            MatrixOpTree r = new MatrixOpTree(m.rows, m.cols);
            for (int i = 0; i < m.rows; i++)
                for (int j = 0; j < m.cols; j++)
                    r[i, j] = m[i, j] * n;
            return r;
        }
        private static MatrixOpTree Add(MatrixOpTree m1, MatrixOpTree m2)         // Sčítání matic
        {
            if (m1.rows != m2.rows || m1.cols != m2.cols) throw new MException("Matrices must have the same dimensions!");
            MatrixOpTree r = new MatrixOpTree(m1.rows, m1.cols);
            for (int i = 0; i < r.rows; i++)
                for (int j = 0; j < r.cols; j++)
                    r[i, j] = m1[i, j] + m2[i, j];
            return r;
        }

        public static string NormalizeMatrixString(string matStr)	// From Andy - thank you! :)
        {
            // Remove any multiple spaces
            while (matStr.IndexOf("  ") != -1)
                matStr = matStr.Replace("  ", " ");

            // Remove any spaces before or after newlines
            matStr = matStr.Replace(" \r\n", "\r\n");
            matStr = matStr.Replace("\r\n ", "\r\n");

            // If the data ends in a newline, remove the trailing newline.
            // Make it easier by first replacing \r\n’s with |’s then
            // restore the |’s with \r\n’s
            matStr = matStr.Replace("\r\n", "|");
            while (matStr.LastIndexOf("|") == (matStr.Length - 1))
                matStr = matStr.Substring(0, matStr.Length - 1);

            matStr = matStr.Replace("|", "\r\n");
            return matStr.Trim();
        }

        //   O P E R A T O R S

        public static MatrixOpTree operator -(MatrixOpTree m)
        { return MatrixOpTree.Multiply(-1, m); }

        public static MatrixOpTree operator +(MatrixOpTree m1, MatrixOpTree m2)
        { return MatrixOpTree.Add(m1, m2); }

        public static MatrixOpTree operator -(MatrixOpTree m1, MatrixOpTree m2)
        { return MatrixOpTree.Add(m1, -m2); }

        public static MatrixOpTree operator *(MatrixOpTree m1, MatrixOpTree m2)
        { return MatrixOpTree.Multiply(m1, m2); }

        public static MatrixOpTree operator *(RootFuncTreeNode n, MatrixOpTree m)
        { return MatrixOpTree.Multiply(n, m); }
    }
    
}