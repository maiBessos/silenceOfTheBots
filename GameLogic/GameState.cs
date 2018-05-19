using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic
{
    /// <summary>
    /// represents the state of an agent in the graph (which is either located somewhere in the
    /// graph, still unset, or captured)
    /// </summary>
    public struct Location 
    {
        public enum Type
        {
            Node, // for specific location, see the value of nodeLocation
            Unset, // before we can associate the location a specific place
            Captured, // after the location is no longer relevant (evader was captured)
            Undefined // NIL
        }
        public Type locationType;
        public Point nodeLocation;

        public void setPoint(Point p)
        {
            nodeLocation = p;
        }
        public bool getLocationIfNode(out Point p)
        {
            if (locationType == Type.Node)
            {
                p = nodeLocation;
                return true;
            }
            p = new Point();
            return false;
        }

        public Location(Point NodeLocation)
        {
            locationType = Type.Node;
            nodeLocation = NodeLocation;
        }
        public Location(Type LocationType)
        {
            locationType = LocationType;
            nodeLocation = new Point();
        }
        public static bool operator==(Location lhs, Location rhs)
        {
            return lhs.locationType == rhs.locationType && lhs.nodeLocation == rhs.nodeLocation;
        }
        public static bool operator!=(Location lhs, Location rhs)
        {
            return lhs.locationType != rhs.locationType || lhs.nodeLocation != rhs.nodeLocation;
        }

        public override string ToString()
        {
            if (locationType != Type.Node)
                return "Undefined location";
            return nodeLocation.X.ToString() + "," + nodeLocation.Y.ToString();
        }
    }

    //public enum Channel
    //{
    //    c1, 
    //    c2
    //}
    //public struct DataUnit
    //{
    //    /// <summary>
    //    /// represents a transmission with no meaningful information
    //    /// </summary>
    //    public static DataUnit NOISE 
    //    {
    //        get 
    //        {
    //            return new DataUnit(){round = 0, sourceTarget = new Location(GridGameGraph.ILLEGAL_NODE_ID)};
    //        }
    //    }
        
    //    /// <summary>
    //    /// represents no transmission at all
    //    /// </summary>
    //    public static DataUnit NIL
    //    {
    //        get
    //        {
    //            return new DataUnit() { round = -1, sourceTarget = new Location(Location.Type.Undefined) };
    //        }
    //    }
    //    public static bool operator==(DataUnit lhs, DataUnit rhs)
    //    {
    //        return lhs.round == rhs.round && lhs.sourceTarget == rhs.sourceTarget;
    //    }
    //    public static bool operator !=(DataUnit lhs, DataUnit rhs)
    //    {
    //        return lhs.round != rhs.round || lhs.sourceTarget != rhs.sourceTarget;
    //    }
    //    public override string ToString()
    //    {
    //        if (round == -1)
    //            return "NIL";
    //        if (round == 0 && sourceTarget.nodeLocation == GridGameGraph.ILLEGAL_NODE_ID)
    //            return "NOISE";
    //        return "R:" + round.ToString().PadRight(4) + " T:" + sourceTarget.nodeLocation.X.ToString().PadRight(4) + "," + sourceTarget.nodeLocation.Y.ToString();
    //    }
    //    public Location sourceTarget { get; set; }
    //    public int round { get; set; } // the round in which the data was first avilable in the memory of the eavesdropper (1 round after it started eavesdropping)
    //}

    // TODO FIXME :  emergency fix to reduce memory, we assume there is only one target, and replaced the DataUnit struct temporarily with a degenerated version, 
    // so we can replace it with RangeSet
    public struct DataUnit
    {
        /// <summary>
        /// represents a transmission with no meaningful information
        /// </summary>
        public static DataUnit NOISE
        {
            get
            {
                // NOTE: be careful with changing the value from -2, since DataUnitVec relies on it
                return new DataUnit() { round = -2, sourceTarget = new Location(GridGameGraph.ILLEGAL_NODE_ID) };
            }
        }

        /// <summary>
        /// represents no transmission at all
        /// </summary>
        public static DataUnit NIL
        {
            get
            {
                // NOTE: be careful with changing the value from -2, since DataUnitVec relies on it
                return new DataUnit() { round = -1, sourceTarget = new Location(Location.Type.Undefined) };
            }
        }

        /// <summary>
        /// if an evader stands on a sink, it may immediately flush all of it's data, by "transmitting" flush
        /// </summary>
        public static DataUnit Flush
        {
            get
            {
                return new DataUnit() {round = -3, sourceTarget = new Location(Location.Type.Undefined)};
            }
        }
        public static bool operator ==(DataUnit lhs, DataUnit rhs)
        {
            if (lhs.round < 0)
                return lhs.round == rhs.round; // if NOISE or NIL, we don't compare the target
            return lhs.round == rhs.round && lhs.sourceTarget == rhs.sourceTarget;
        }
        public static bool operator !=(DataUnit lhs, DataUnit rhs)
        {
            if (lhs.round < 0)
                return lhs.round != rhs.round; // if NOISE or NIL, we don't compare the target
            return lhs.round != rhs.round || lhs.sourceTarget != rhs.sourceTarget;
        }
        public override string ToString()
        {
            if (round == NIL.round)
                return "NIL";
            if (round == NOISE.round)
                return "NOISE";
            if (round == Flush.round)
                return "FLUSH";
            return "R:" + round.ToString().PadRight(4) + " T:" + sourceTarget.nodeLocation.X.ToString().PadRight(4) + "," + sourceTarget.nodeLocation.Y.ToString();
        }
        public Location sourceTarget { get; set; }
        public int round { get; set; } // the round in which the data was first avilable in the memory of the eavesdropper (1 round after it started eavesdropping)
    }
    
    /// <summary>
    /// used to describe which Data Units are stored in evaders memory/sink
    /// TODO: 1) OneTargetCompactMemory works under the assumption there is only one target. may be fixed in most cases by generating separate memories for each target
    /// TODO: 2) OneTargetCompactMemory should be used to represent memory in Game State and in each evader's memory, and in global sink
    /// TODO: 3) maybe can be further optimized with hashset(though I doubt it)
    /// </summary>
    class OneTargetCompactMemory
    {
        /// <summary>
        /// sorted list of ranges {min,max} of rounds of when the data was eavesdropped
        /// </summary>
        public List<Tuple<ushort, ushort>> Mem = new List<Tuple<ushort, ushort>>(); // maybe use SortedList<key,value> instead?
        protected int blankSlotsCount = 0;

        // TODO: implement below - not much work to do, but this whole thing is premature optimization and maybe not needed
        //void addData(DataUnit u)
        //{
        //    if(u.sourceTarget.locationType == Location.Type.Node)
        //    {
        //        if(Mem.Count == 0)
        //            Mem.Add(Tuple.Create(u.round,u.round));
        //        else if(Mem.Last().Item2 < u.round)
        //        {
        //            if(Mem.Last().Item2 + 1 == u.round)
        //                ++Mem.Last().Item2;
        //            else
        //                Mem.Add(Tuple.Create(u.round,u.round));
        //        }
        //        else if (Mem.First().Item1 > u.round)
        //        {
        //
        //        }
        //        else
        //        {
        //            int res = Mem.BinarySearch(
        //                Tuple.Create((ushort)0,(ushort)u.round),
        //                Comparer<Tuple<ushort,ushort>>.Create(new Comparison<Tuple<ushort,ushort>>((lhs,rhs)=>lhs.Item2.CompareTo(rhs.Item2))));
        //            if (res > 0)
        //                return true;
        //            res = ~res;
        //            // merge unit with next/previous units. If the new unit made them sequential, consider removing and compacting the array (slow process, should not be done often)
        //        }
        //    }
        //}

        /// <summary>
        /// tells how many items this and 'lhs' have in common
        /// TODO: implement
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        int getIntersectionSize(OneTargetCompactMemory rhs)
        {
            return 0;
        }
        bool Contains(DataUnit u)
        {
            int res = Mem.BinarySearch(
                Tuple.Create((ushort)0, (ushort)u.round),
                Comparer<Tuple<ushort, ushort>>.Create(new Comparison<Tuple<ushort, ushort>>((lhs, rhs) => lhs.Item2.CompareTo(rhs.Item2))));

            if (res > 0)
                return true;

            res = ~res;
            return (res < Mem.Count && Mem[res].Item1 >= u.round);
        }
    }
    public class GameState
    {
        
        public GameState()
        {
            L = new Vec<Dictionary<IAgent, Location>>();
            M = new Vec<Dictionary<Evader, GoE.GameLogic.Utils.DataUnitVec>>();
            B_O = new Vec<Dictionary<Evader, DataUnit>>();
            B_I = new Vec<Dictionary<Evader, Location>>();

            ActiveEvaders = new HashSet<Evader>();
        }

        public int MostUpdatedPursuersRound
        {
            get;
            set;
        }
        public int MostUpdatedEvadersLocationRound
        {
            get;
            set;
        }
        public int MostUpdatedEvadersMemoryRound
        {
            get;
            set;
        }
        /// <summary>
        /// utility property, that returns all evaders that has a location (were set + not yet captured)
        /// </summary>
        /// <returns></returns>
        public HashSet<Evader> ActiveEvaders
        {
            get;
            protected set;
        }
        /// <summary>
        /// given IAgent (Evader or Pursuer) a, L[r][a] indicates the location at round r of the agent
        /// </summary>
        public Vec<Dictionary<IAgent, Location>> L { get; set; }

        /// <summary>
        /// given Evader e, M[r][e] tells the data units accumulated in e, until round r
        /// </summary>
        public Vec<Dictionary<Evader, GoE.GameLogic.Utils.DataUnitVec>> M { get; set; }

        /// <summary>
        /// given Evader e, B_O[r][e] tells the data unit that e started transmitting at round r
        /// </summary>
        public Vec<Dictionary<Evader, DataUnit>> B_O { get; set; }

        /// <summary>
        /// given Evader e, B_I[r][e] tells the ID of the target that e is attempting to eavesdrop at round r (and finishes eavesdropping it in round r+1, if it doesn't get captured)
        /// (GameGraph's ILLEGAL_NODE_ID means no target is being listened to)
        /// </summary>
        public Vec<Dictionary<Evader, Location>> B_I { get; set; }
    }
}
