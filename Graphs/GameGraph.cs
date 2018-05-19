using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;

namespace GoE
{
    public struct MovementDisplay
    {
        public Color lineColor { get; set; }
        public Point From { get; set; }
        public Point To { get; set; }
    }
    public class NodeDisplay
    {
        public NodeDisplay() { }
        public NodeDisplay(Color Col, string Text)
        {
            this.c = Col;
            this.text = Text;
        }

        public Color c = new Color();
        public string text = "";
    }

    public enum NodeType : int
    {
        Normal = 0,
        Target = 1,
        Sink = 2,
        Blocked = 3
    }

    public class Node<NodeID>
    {
        public Node(NodeID ID)
        {
            id = ID;
        }
        public Node(NodeID ID, NodeType T)
        {
            t = T;
            id = ID;
        }

        public NodeType t = NodeType.Normal;
        public NodeID id;
    }
    
    public abstract class AGameGraph : Utils.ReflectionUtils.DerivedTypesProvider<AGameGraph>
    {
        public abstract string[] serialize();
        public abstract void deserialize(string[] serialization);

        public static AGameGraph loadGraph(string[] serialization)
        {
            string[] filteredSerialization = ParsingUtils.clearComments(serialization);
            string graphType = filteredSerialization[0];
            AGameGraph graph = (AGameGraph)Activator.CreateInstance(null, graphType).Unwrap();
            graph.deserialize(filteredSerialization);
            return graph;
        }

        /// <summary>
        /// for graphs that need no deserialization
        /// </summary>
        /// <param name="graphTypeName"></param>
        /// <returns></returns>
        public static AGameGraph loadGraph(string graphTypeName)
        {
            AGameGraph graph  = ReflectionUtils.constructEmptyCtorType<AGameGraph>(graphTypeName);
            //AGameGraph graph = (AGameGraph)Activator.CreateInstance(null, graphTypeName).Unwrap();
            return graph;
        }
    }
    public class EmptyEnvironment : AGameGraph
    {
        public EmptyEnvironment() { }

        public override void deserialize(string[] serialization)
        {
            
        }

        public override string[] serialize()
        {
            return null;
        }
    }
    /// <summary>
    /// represents a general graph to be used for a game
    /// </summary>
    /// <typeparam name="NodeID"></typeparam>
    public abstract class GameGraph<NodeID> : AGameGraph
    {
        
        /// <summary>
        /// represents an undirected, weighted edge
        /// </summary>
        public class Edge
        {
            public double weight = 1;
            public Node<NodeID> destination;

            public Edge(Node<NodeID> Destination)
            {
                this.destination = Destination;
            }
            public Edge(Node<NodeID> Destination, double Weight)
            {
                this.destination = Destination;
                this.weight = Weight;
            }
        }
        
        /// <summary>
        /// allows saving the graph.
        /// 
        /// if an inheriting graph needs to extend serialize()/desiralize(), it needs to implement serializeEx()/deserializeEx() 
        /// (these methods are called at the end of serialize()/deserialize() automatically)
        /// </summary>
        /// <returns></returns>
        /// 
        public override string[] serialize()
        {
            List<string> res = new List<string>();
            res.Add("#Concrete graph class type:");
            res.Add(this.GetType().Name);
            res.Add("#Node count:");
            res.Add(Nodes.Count.ToString());
            res.Add("#Nodes (node id + type, where 0->Normal, 1->target, 2->sink, 3->blocked) : ");
            foreach(var n in Nodes)
            {
                res.Add(serializeID(n.Value.id));
                res.Add(((int)n.Value.t).ToString());
            }
            res.Add("#Edges (from node id 1 + to node id 2 + edge weight):");
            foreach(var n1 in Edges)
            {
                foreach (var n2 in n1.Value)
                {
                    res.Add(serializeID(n1.Key)); // from 
                    res.Add(serializeID(n2.Key)); // to
                    res.Add(n2.Value.ToString());
                }
            }

            res.AddRange(serializeEx()); // concatenate lines from serialization extension
            return res.ToArray();
        }

        // default implementation of serialize Ex (empty)
        protected virtual string[] serializeEx() { return new string[0]; }
        
        /// <summary>
        /// allows loading the graph, from lines as returned from GameGraph.serialize()
        /// 
        /// if an inheriting graph needs to extend serialize()/desiralize(), it needs to implement serializeEx()/deserializeEx() 
        /// (these methods are called at the end of serialize()/deserialize() automatically)
        /// </summary>
        /// <returns></returns>
        /// 
        public override void deserialize(string[] serialization)
        {
            string[] lines = ParsingUtils.clearComments(serialization);
            // line 1 specifies graph concrete type, so we skip it (assuming this object was already constructed)
            int nodeCount = Int32.Parse(lines[1]);
            uint nextLine = 2;
            while (nodeCount > 0)
            {
                NodeID id = deserializeID(lines[nextLine++]);
                Nodes[id] = new Node<NodeID>(id, (NodeType)Int32.Parse(lines[nextLine++]));
                Edges[id] = new Dictionary<NodeID, double>();
                --nodeCount;
            }
            while (nextLine < lines.Count())
            {
                NodeID id1 = deserializeID(lines[nextLine++]);
                NodeID id2 = deserializeID(lines[nextLine++]);
                double weight = Double.Parse(lines[nextLine++]);

                if (Edges[id1] == null)
                    Edges[id1] = new Dictionary<NodeID, Double>();
                if (Edges[id2] == null)
                    Edges[id2] = new Dictionary<NodeID, Double>();

                Edges[id1][id2] = weight;
                Edges[id2][id1] = weight;
            }

            // call desiralization extension with remaining lines:
            deserializeEx(lines.Skip((int)nextLine).ToArray());
            
        }
       
        /// <summary>
        /// default implementation of deserialize Ex (empty).
        /// 
        /// called at the end of deserialize() call (i.e. GameGraph base mambers are already initialized)
        /// </summary>
        /// <param name="serialiationEx">
        /// will always be only the lines previously returned by serializeEx()
        /// </param>
        protected virtual void deserializeEx(string[] serialiationEx) {}

        /// <summary>
        /// tells if the graph has changed since last isGraphDirty() call
        /// </summary>
        protected abstract bool isGraphDirty();

        // note: [ThreadStatic] works only for static members, hence the double dictionary
        [ThreadStatic] private static Dictionary<GameGraph<NodeID>, Dictionary<NodeType, List<NodeID>>> nodesByType = null;
        
        public virtual List<NodeID> getNodesByType(NodeType t)
        {
            if(nodesByType == null) // thread static members are always initialized to null
                nodesByType = new Dictionary<GameGraph<NodeID>, Dictionary<NodeType, List<NodeID>>>();
            
            if (isGraphDirty() || !nodesByType.ContainsKey(this))
                nodesByType[this] = new Dictionary<NodeType, List<NodeID>>();

            if (!isGraphDirty() && nodesByType[this].Keys.Contains(t))
                return nodesByType[this][t];

            List<NodeID> result = new List<NodeID>();
            foreach (var n in Nodes)
                if (n.Value.t == t)
                    result.Add(n.Key);
            
            nodesByType[this][t] = result;
            return result;
        }

        public virtual double getMinDistance(NodeID n1, NodeID n2)
        {
            throw new Exception("Not implemented!");
        }
        
        /// <summary>
        /// returns nodes in exactly distance 'dist' from 'origin
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        public virtual List<NodeID> getNodesInDistance(NodeID origin, double dist)
        {
            HashSet<NodeID> processedNodes = new HashSet<NodeID>();
            HashSet<NodeID> unprocessedNodes = new HashSet<NodeID>();
            Dictionary<NodeID, double> remainingDistance = new Dictionary<NodeID, double>();
            List<NodeID> nodesInExactDistance = new List<NodeID>();
            remainingDistance[origin] = dist;
            unprocessedNodes.Add(origin);
            HashSet<NodeID> nextunprocessedNodes = new HashSet<NodeID>();

            if (dist <= 0.001)
                nodesInExactDistance.Add(origin);

            while (unprocessedNodes.Count > 0)
            {
                foreach (var v in unprocessedNodes)
                {
                    processedNodes.Add(v);
                    double prevRemainingDist = remainingDistance[v];

                    foreach (var adj in Edges[v])
                    {
                        if (processedNodes.Contains(adj.Key))
                            continue;

                        double remainingDist = prevRemainingDist - adj.Value; // decrease the edge's value from distance remaining for "spreading"
                        if (remainingDist >= -0.001)
                        {
                            
                            nextunprocessedNodes.Add(adj.Key);
                            remainingDistance[adj.Key] = remainingDist;
                            if (remainingDist <= 0.001)
                                nodesInExactDistance.Add(adj.Key);
                        }
                    }
                }
                var tmp = unprocessedNodes;
                unprocessedNodes = nextunprocessedNodes;
                nextunprocessedNodes = tmp;
                nextunprocessedNodes.Clear();
            }

            return nodesInExactDistance;
        }
        public virtual List<NodeID> getNodesWithinDistance(NodeID origin, double maxDist)
        {
            HashSet<NodeID> processedNodes = new HashSet<NodeID>();
            HashSet<NodeID> unprocessedNodes = new HashSet<NodeID>();
            Dictionary<NodeID, double> remainingDistance = new Dictionary<NodeID, double>();

            remainingDistance[origin] = maxDist;
            unprocessedNodes.Add(origin);

            HashSet<NodeID> nextunprocessedNodes = new HashSet<NodeID>();
            while (unprocessedNodes.Count > 0)
            {
                foreach (var v in unprocessedNodes)
                {
                    processedNodes.Add(v);
                    double prevRemainingDist = remainingDistance[v];

                    foreach (var adj in Edges[v])
                    {
                        if (processedNodes.Contains(adj.Key))
                            continue;

                        double remainingDist = prevRemainingDist - adj.Value; // decrease the edge's value from distance remaining for "spreading"
                        if (remainingDist >= -0.001)
                        {
                            remainingDistance[adj.Key] = remainingDist;
                            nextunprocessedNodes.Add(adj.Key);
                        }
                    }
                }
                var tmp = unprocessedNodes;
                unprocessedNodes = nextunprocessedNodes;
                nextunprocessedNodes = tmp;
                nextunprocessedNodes.Clear();
            }

            return remainingDistance.Keys.ToList();
        }

        public abstract Dictionary<NodeID, Node<NodeID>> Nodes { get; }
        
        /// <summary>
        /// useage: Edges[NodeID1][NodeID2] = edge weight from Node1 to Node2 (or null if doesn't exist)
        /// currently edges are undirected i.e. Edges[NodeID1][NodeID2] = Edges[NodeID2][NodeID1]
        /// </summary>
        public abstract Dictionary<NodeID, Dictionary<NodeID, Double>> Edges { get; }

        protected abstract string serializeID(NodeID nid);
        protected abstract NodeID deserializeID(string seralizedNode);
    }
}
