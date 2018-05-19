//using GoE.GameLogic;
//using GoE.UI;
//using GoE.Utils;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using GoE.Utils.Extensions;
//using GoE.AppConstants.Algorithms;
//using GoE.GameLogic.Algorithms;
//namespace GoE.Policies
//{
    // NOTE: the code is fine, but stopped mid-development, since we started the security game

//    /// <summary>
//    /// finds the value of AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE_MULTIPLIER that
//    /// maximizes evaders' performance
//    /// </summary>
//    public class PatrolRingsOptimizer : APolicyOptimizer
//    {

//        public int MaxRing { get; set; }
//        public int MinRing { get; set; }

//        protected override void initEx()
//        {
//            MaxRing = int.Parse(
//                Utils.ParsingUtils.readValueOrDefault(policyInput,
//                Optimizers.MAX_RING,
//                Optimizers.MAX_RING_DEFAULT));

//            MinRing= int.Parse(
//                Utils.ParsingUtils.readValueOrDefault(policyInput,
//                Optimizers.MIN_RING,
//                Optimizers.MIN_RING_DEFAULT));

//            pursuerCountFactorJump = float.Parse(
//                Utils.ParsingUtils.readValueOrDefault(policyInput,
//                Optimizers.PURSUERS_COUNT_FACTOR_JUMP_KEY,
//                Optimizers.PURSUERS_COUNT_FACTOR_JUMP_DEFAULT));
//        }



//        private ParallelOptions currentParalleOpt;
        
//        /// <summary>
//        /// min. avg leaked per evader for each:
//        /// each pursuit patrol X
//        /// each circumference patrol (best rp) X
//        /// each (Derived) area patrol (best rp) 
//        /// max. avg leaked per evader:
//        /// either escape after constant time ( smaller or larger than optimal, no-discount l_ex)
//        /// either transmit eta' simulteneous transmissions
//        /// </summary>
//        /// <param name="opt"></param>
//        public override void process(ParallelOptions opt = null)
//        {
//            currentParalleOpt = opt;

//            // pursuers choose config that minimizes leakage:
//            AlgorithmUtils.OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome>
//                bestPursuersConfig = new AlgorithmUtils.OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome>()
//                {value = 0, data = new PursuersBounds.Solution2Alt1Chromosome(this.gameParams.A_P.Count)}; // value is e_stay/e_escape for each data unit

//            // evaders respond by choosing l_escape/eta_tag that maximize leakage:
            
//            AlgorithmUtils.OptimizedObj<float> maxLeakEtaTagFactor  = new AlgorithmUtils.OptimizedObj<float>(){value = 0};

//            discountFactor = 0.999999999;
//            ConstantExponentialDecay discR = gameParams.R as ConstantExponentialDecay;
//            if (discR != null)
//                discountFactor = discR.oneRoundDiscountFactor;

//            ringCount = MaxRing - MinRing;
//            areaPatrolRadius = gameParams.r_e - MaxRing;

//            int parallelTaskCount = this.gameParams.A_P.Count * this.gameParams.A_P.Count;
//            List<List<int>> pursuersPerRing = new List<List<int>>();
//            for (int i = 0; i < parallelTaskCount; ++i)
//                pursuersPerRing.Add(AlgorithmUtils.getRepeatingValueList(0, ringCount));
                    
//            ParallelOptions parOpt = new ParallelOptions();


//            Parallel.For(0, 2 * parallelTaskCount, parOpt, ring12PursuerCount =>
//            {
//                int firstRingPursuersCount = ring12PursuerCount % this.gameParams.A_P.Count;
//                int secondRingPursuersCount = ring12PursuerCount / this.gameParams.A_P.Count;
//                if (secondRingPursuersCount + firstRingPursuersCount > ringCount)
//                    return;

//                pursuersPerRing[ring12PursuerCount][0] = firstRingPursuersCount;
//                pursuersPerRing[ring12PursuerCount][1] = secondRingPursuersCount;

//                float minLeakedData = float.MaxValue;
//                // we set the first and second value, and let the recursive function fill the remaining values
//                pursuersPerRing[ring12PursuerCount] =
//                    minLeakedDataPerRingConfigRecursive(pursuersPerRing[ring12PursuerCount], this.gameParams.A_P.Count - firstRingPursuersCount - secondRingPursuersCount, 2, out minLeakedData);

//            }); 

            
//        }
//        double discountFactor;

//        List<int> minLeakedDataPerRingConfigRecursive(List<int> pursuersPerRing, int remainingPursuersToSpread, int idxToSet, out float minLeakedData)
//        {
//            if(remainingPursuersToSpread == 1 || idxToSet == pursuersPerRing.Count) 
//            {
//                minLeakedData = getMaxLeakedData(pursuersPerRing, remainingPursuersToSpread);
//                return pursuersPerRing;
//            }

//            AlgorithmUtils.OptimizedObj<List<int>> minLeakedDataConf = new AlgorithmUtils.OptimizedObj<List<int>>();
//            minLeakedDataConf.value = float.MaxValue;

//            for(int i = 1; i <= remainingPursuersToSpread; ++i)
//            {
//                pursuersPerRing[idxToSet] = i;
//                float minLeakedDataTmp;
//                var tmpConf = minLeakedDataPerRingConfigRecursive(pursuersPerRing, remainingPursuersToSpread - i, idxToSet + 1, out minLeakedDataTmp);
//                minLeakedDataConf.setIfValueDecreases(tmpConf, minLeakedDataTmp);
//            }

//            minLeakedData = (float)minLeakedDataConf.value;
//            return minLeakedDataConf.data;
//        }

//        int areaPatrolRadius;
//        private float getMaxLeakedData(List<int> pursuersPerRing, int remainingPursuers)
//        {
//            double p_a;
//            double p_c;
//            PatrolAndPursuit.PatrolAlg bestAlg =
//                PatrolAndPursuit.getBestPatrolType(areaPatrolRadius, areaPatrolRadius, remainingPursuers);
//            p_a = bestAlg.p_a;
            
//            AlgorithmUtils.OptimizedObj<int> leakedPerLEscape = new AlgorithmUtils.OptimizedObj<int>();


//            int l_escape = (int)Policies.PatrolAndPursuit.calculatel_escape(tmpPursuersConfig.data.usedP_A, tmpPursuersConfig.data.usedP_C, discountFactor, 0);
//            tmpMinEEscapeLEscapeFactor.setIfValueDecreases(l_escape, Policies.PatrolAndPursuit.calculateE_Escape(tmpPursuersConfig.data.usedP_A, tmpPursuersConfig.data.usedP_C, l_escape, discountFactor, 0));


//            leakedPerLEscape.data = 1;
//            leakedPerLEscape.value = 
//                Policies.PatrolAndPursuit.calculateE_Escape(p_a, pursuersPerRing[-1-MinRing], 1, discountFactor, 0);
            
//            for(int l_escape = 1; l_escape ) 
//        }
//        double calculatePC(List<int> pursuersPerRing)
//        {

//        }
//        private int ringCount;

//        private static string PursuersPolicy = typeof(PatrolAndPursuit).Name;
//        private static string EvadersCrawlPolicy = typeof(EvadersPolicyEscapeAfterConstantTime).Name;
//        private static string EvadersTransmitPolicy = typeof(EvadersPolicyTransmitFromWithinArea).Name;

//        protected float l_escapeMinFactor, l_escapeMaxFactor, l_escapeFactorJump,
//                        etaTagMinFactor, etaTagMaxFactor, etaTagFactorJump;
//        protected float pursuerCountFactorJump;
//    }
//}