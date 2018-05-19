using System;
using System.Collections.Generic;
namespace Utils.Minimax
{
    // TODO: implement Monte carlo tree search:  https://en.wikipedia.org/wiki/Monte_Carlo_tree_search

    public interface Node
    {
        /// <summary>
        /// Create your subtree here and return the results
        /// </summary>
        /// <param name="Player"></param>
        /// <returns></returns>
        List<Node> Children(bool Player);
        
        // tells if Game over
        bool IsTerminal(bool Player);

        /// <summary>
        /// a heuristic evaluation function to evaluate
        /// the current situation of the player
        /// for player 1, return positive score
        /// for player 0, return negative scoer
        /// </summary>
        /// <param name="Player"></param>
        /// <returns></returns>
        int GetTotalScore(bool Player);
    }

    public class AlphaBeta
    {
        private const bool MaxPlayer = true;

        public AlphaBeta()
        {
        }

        public Node getNextNode(Node startNode, int depth = 10, bool startWithMaxPlayer = true)
        {
            Node resNode;
            Iterate(startNode, depth, int.MinValue + 1, int.MaxValue - 1, startWithMaxPlayer, out resNode);
            return resNode;
        }

        protected int Iterate(Node node, int depth, int alpha, int beta, bool Player, out Node resNode)
        {
            if (depth == 0 || node.IsTerminal(Player))
            {
                resNode = node;
                return node.GetTotalScore(Player);
            }

            if (Player == MaxPlayer)
            {
                resNode = null;
                foreach (Node child in node.Children(Player))
                {
                    alpha = Math.Max(alpha, Iterate(child, depth - 1, alpha, beta, !Player, out resNode));
                    if (beta < alpha)
                    {
                        break;
                    }

                }

                return alpha;
            }
            else
            {
                resNode = null;
                foreach (Node child in node.Children(Player))
                {
                    beta = Math.Min(beta, Iterate(child, depth - 1, alpha, beta, !Player, out resNode));

                    if (beta < alpha)
                    {
                        break;
                    }
                }

                return beta;
            }
        }
    }
}