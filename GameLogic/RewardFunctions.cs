using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.GameLogic
{
    public abstract class ARewardFunction
    {
        /// <summary>
        /// allows more control on getReward().
        /// setArgs() may be invoked several times, each time reseting the behaviour of the function
        /// note: throws exception if not correct number of arguments/can't be parsed correctly
        /// </summary>
        /// <param name="CSVArgs"></param>
        public abstract void setArgs(string CSVArgs);

        public abstract float getReward(int packetAge);
        public abstract List<string> argumentsDescription();

        public abstract string ArgsCSV { get; }

        public abstract Dictionary<string, string> ArgsData();

        public string fileNameDescription()
        {
            return this.GetType().Name + "~" + ArgsCSV.Replace(',', '~');
        }

        public abstract List<ARewardFunction> getRewardFunctions(string CSVArgsStart, string CSVArgsEnd, string jump);
    }

    /// <summary>
    /// a non increasing function telling the reward of a packet
    /// </summary>
    /// <param name="packetAge">
    /// value in [0, inf], telling how many rounds had passed since the data was first eavesdropped from a target)
    /// (value of 0 means the evader finished eavesdropping at the begining of the previous round, and finished 
    /// transmitting it in the begining of the current round - i.e. minimal possible delay)
    /// </param>
    /// <returns>
    /// reward on a given packet (value in [0,1])
    /// </returns>
    public abstract class RewardFunction<T> : ARewardFunction where T:new()
    {
        
    }

    public class NoDecrease : RewardFunction<NoDecrease>
    {

        public override Dictionary<string, string> ArgsData()
        {
            return new Dictionary<string, string>();
        }
        public override void setArgs(string CSVArgs){}

        public override float getReward(int packetAge)
        {
 	        return 1.0f;
        }
        public override List<string> argumentsDescription() { return new List<string>(); }

        public override string ArgsCSV { get { return ""; } }

        public override List<ARewardFunction>  getRewardFunctions(string CSVArgsStart, string CSVArgsEnd, string jump)
        {
            List<ARewardFunction> res = new List<ARewardFunction>();
            res.Add( new NoDecrease());
            return res;
        }
    }

    /// <summary>
    /// Reward decreases by being multiplied (by a constant factor) each round i.e. discount factor.
    /// if the package has the age of k - first CSV arg (typically eavesdropping radius), the remaining reward is second CSV arg
    /// </summary>
    public class ConstantExponentialDecay : RewardFunction<ConstantExponentialDecay>
    {
        private double eavesdroppingRadius;
        private double remainingReward;

        public override Dictionary<string, string> ArgsData()
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            res[AppConstants.GameLogic.RewardFunctionArgNames.DISCOUNT_FACTOR.key] = oneRoundDiscountFactor.ToString();
            return res;
        }

        public ConstantExponentialDecay() { }

        public ConstantExponentialDecay(double EavesdroppingRadius, double RemainingReward)
        {
            this.eavesdroppingRadius = EavesdroppingRadius;
            this.remainingReward = RemainingReward;
        }

        public override string ArgsCSV 
        { 
            get 
            {
                return eavesdroppingRadius.ToString() + "," + remainingReward.ToString(); 
            }
        }

        public override void setArgs(string CSVArgs)
        {
            var vals = ParsingUtils.separateCSV(CSVArgs);
            eavesdroppingRadius = double.Parse(vals[0]);
            remainingReward = double.Parse(vals[1]);
        }
        
        public double oneRoundDiscountFactor // discount factor "lambda"
        {
            get
            {
                return Math.Pow(remainingReward, 1.0 / eavesdroppingRadius);
            }
        }
        public override float getReward(int packetAge)
        {
            return (float)Math.Pow(remainingReward, ((double)packetAge) / (eavesdroppingRadius));
        }

        public override  List<string> argumentsDescription() 
        {
            return new string[] { "k (typically Eavesdropping Radius)", "Remaining Reward after k rounds(can jump)" }.ToList();
        }

        public override List<ARewardFunction> getRewardFunctions(string CSVArgsStart, string CSVArgsEnd, string jump)
        {
            List<ARewardFunction> res = new List<ARewardFunction>();

            if(CSVArgsEnd.Length == 0)
            {
                ConstantExponentialDecay r = new ConstantExponentialDecay();
                r.setArgs(CSVArgsStart);
                res.Add(r);
                return res;
            }

            try
            {
            
                var vals1 = ParsingUtils.separateCSV(CSVArgsStart);
                var vals2 = ParsingUtils.separateCSV(CSVArgsEnd);
                double eavesdroppingRadius = double.Parse(vals1[0]);
                double remainingRewardMin = double.Parse(vals1[1]);
                double remainingRewardMax = double.Parse(vals2[1]);
                double remainingRewardJump = double.Parse(jump);
                for(;remainingRewardMin < remainingRewardMax + remainingRewardJump; remainingRewardMin += remainingRewardJump )
                {
                    remainingRewardMin = Math.Min(remainingRewardMin, remainingRewardMax);
                    res.Add(new ConstantExponentialDecay(eavesdroppingRadius,remainingRewardMin));
                }

                return res;
            }
            catch(Exception)
            {
                throw new Exception("jump param should only contain jump value for remaining reward");
            }
        }
    }
        
    
    
}
