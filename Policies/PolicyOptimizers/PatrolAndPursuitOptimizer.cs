using GoE.GameLogic;
using GoE.UI;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoE.Utils.Extensions;
using GoE.AppConstants.Algorithms;
using GoE.GameLogic.Algorithms;
using GoE.Utils.Algorithms;
using GoE.AppConstants.GameProcess;
using System.Drawing;
using GoE.AppConstants;

namespace GoE.Policies
{
   

    /// <summary>
    /// finds the value of AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE_MULTIPLIER that
    /// maximizes evaders' performance
    /// </summary>
    public abstract class PatrolAndPursuitOptimizerBase : APolicyOptimizer
    {
        
        protected abstract double EvaluateEtaTag(double p_a, double p_c, double p_p, double p_d, double discountFactor, float eta_tag,
                                                 GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome pursuersConfig);

        /// <summary>
        /// assumes p_c doesn't include the added gameParams.EvaderCircumferenceEntranceKillProb 
        /// </summary>
        /// <param name="p_a"></param>
        /// <param name="p_c"></param>
        /// <param name="p_p"></param>
        /// <param name="p_d"></param>
        /// <param name="discountFactor"></param>
        /// <param name="pursuersConfig"></param>
        /// <returns></returns>
        private OptimizedObj<float> findMinimalEStayEtaTag(double p_a, double p_c, double p_p, double p_d, double discountFactor,
                                                           GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome pursuersConfig)
        {
            OptimizedObj<float> bestEta = new OptimizedObj<float>();
            bestEta.data = -1;
            bestEta.value = float.MaxValue;

            int etaTagJump =
                (int)(float.Parse(Optimizers.ETA_TAG_ESTIMATION_FACTOR_JUMP.tryRead(policyInput)) * gameParams.A_E.Count);

            float eta_tag = 1;
            for (; eta_tag < gameParams.A_E.Count; eta_tag += etaTagJump)
                bestEta.setIfValueDecreases(eta_tag, EvaluateEtaTag(p_a, p_c, p_p, p_d, discountFactor, eta_tag, pursuersConfig));

            if(eta_tag < gameParams.A_E.Count) // just in case, also try the maximal value
            {
                eta_tag = gameParams.A_E.Count;
                bestEta.setIfValueDecreases(eta_tag, EvaluateEtaTag(p_a, p_c, p_p, p_d, discountFactor, eta_tag, pursuersConfig));
            }
            
            return bestEta;
        }

        public int MinimalCircumferencePatrollers { get; protected set; }
        public bool CanPursuersPursue { get; protected set; } // // if set to false, pursuers will not allocate pursuit pursuers
        public bool CanEvadersTransmit {get; protected set;} // if set to false, pursuers will not allocate pursuit pursuers, and evaders will necessarily use EscapeAfterConstantTime policy
        public bool CanEvadersCrawl { get; protected set; } // if set to false,  evaders will necessarily use TransmitFromWithinArea policy
        public bool CanPursuersPatrolCircumference { get; protected set; } // if set to false, pursuers will not allocate circumference pursuers
        public bool CanPursuersPatrolArea { get; protected set; }
        private int ForcedLEscape = 0; // if 0, no value is forced
        private int ForcedSimulteneousTransmissions = 0; // if 0, no value is forced
        public override List<string> optimizationOutputKeys
        {
            get
            {
                List<string> res = new List<string>();
                
                // pursuers output:
                res.Add(AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_VELOCITY.key);
                res.Add(AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_VELOCITY.key);
                res.Add(AppConstants.Policies.PatrolAndPursuit.PURSUIT_PURSUERS_COUNT.key);
                res.Add(AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_PURSUERS_COUNT.key);
                res.Add(AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_PURSUERS_COUNT.key);
                res.Add(AppConstants.Policies.PatrolAndPursuit.PURSUIT_CAPTURE_PROB.key);
                res.Add(AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_CAPTURE_PROB.key);
                res.Add(AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_CAPTURE_PROB.key);

                // evaders output:
                res.Add(AppConstants.AppArgumentKeys.EVADER_POLICY.key);
                res.Add(AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE.key);
                res.Add(AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.ESCAPE_REWARD.key);
                res.Add(AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS.key);
                res.Add(AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.TRANSMIT_REWARD.key);

                res.AddRange(new GameResult(0, 0).Keys); // TODO: a bit dirty, since we can't really rely on game result to add all keys on initialization

                return res;
            }
        }

        public override List<ArgEntry> optimizationInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(Optimizers.PURSUERS_COUNT_FACTOR_JUMP);
                res.Add(Optimizers.CAN_PURSUERS_PATROL_AREA);
                res.Add(Optimizers.CAN_PURSUERS_PATROL_CIRCUMFERENCE);
                res.Add(Optimizers.CAN_PURSUERS_PURSUE);
                res.Add(Optimizers.ETA_TAG_ESTIMATION_FACTOR_JUMP);
                res.Add(Optimizers.MINIMAL_CIRCUMFERENCE_PATROLLERS);

                // the following can be cancelled if specified in OPTIMIZER_UNAWARE :
                //res.Add(AppConstants.Policies.PatrolAndPursuit.CAN_EVADERS_TRANSMIT);
                //res.Add(AppConstants.Policies.PatrolAndPursuit.CAN_EVADERS_CRAWL);
                res.Add(Optimizers.OPTIMIZER_UNAWARE);
                res.Add(AppArgumentKeys.EVADER_POLICY);
                res.Add(AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS);
                res.Add(AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE);
                return res;
            }
        }

        protected GoEGameParams gameParams;
        //private float maxPursuitProb;

        /// <summary>
        /// if optimizer is not allowed to be aware of the parameter, default value is used. otherwise, it is read normally
        /// </summary>
        protected string parseIfAware(Dictionary<string,string> policyInput, ArgEntry arg, List<string> unawareParams)
        {
            if (unawareParams.Contains(arg.key))
                return arg.val;
            return arg.tryRead(policyInput);
        }
        
        protected override void initEx()
        {
            gameParams = (GoEGameParams)igameParams;

            List<string> unawareParams = ParsingUtils.separateCSV(Optimizers.OPTIMIZER_UNAWARE.tryRead(policyInput));

            // ForcedSimulteneousTransmissions and ForcedLEscape will get 0 if no forced value is given (0 means the value will be optimized)
            int.TryParse(parseIfAware(policyInput, AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS, unawareParams),
                         out ForcedSimulteneousTransmissions);

            int.TryParse(parseIfAware(policyInput, AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE, unawareParams),
                         out ForcedLEscape);

            CanEvadersTransmit =
                (typeof(EvadersPolicyEscapeAfterConstantTime).Name != 
                parseIfAware(policyInput, AppArgumentKeys.EVADER_POLICY, unawareParams));
            
            
            CanEvadersCrawl = (typeof(EvadersPolicyTransmitFromWithinArea).Name !=
                parseIfAware(policyInput, AppArgumentKeys.EVADER_POLICY, unawareParams));

            CanPursuersPursue = (1 == int.Parse(
                Optimizers.CAN_PURSUERS_PURSUE.tryRead(policyInput)));

            CanPursuersPatrolCircumference = (1 == int.Parse(
                Optimizers.CAN_PURSUERS_PATROL_CIRCUMFERENCE.tryRead(policyInput)));

            CanPursuersPatrolArea = (1 == int.Parse(
                Optimizers.CAN_PURSUERS_PATROL_AREA.tryRead(policyInput)));

            pursuerCountFactorJump = float.Parse(
                Optimizers.PURSUERS_COUNT_FACTOR_JUMP.tryRead(policyInput));

            MinimalCircumferencePatrollers = int.Parse(
                Optimizers.MINIMAL_CIRCUMFERENCE_PATROLLERS.tryRead(policyInput));
        }

        
        private void findEquilibriumPursuerSearchParams(
            out float minAreaPatrolPursuersFactor, 
            out float maxAreaPatrolPursuersFactor,
            out float maxPursuitPursuersFactor,
            out double discountFactor)
        {
            maxPursuitPursuersFactor = 1;
            if (!CanEvadersTransmit || !CanPursuersPursue)
                maxPursuitPursuersFactor = 0;

            minAreaPatrolPursuersFactor = 0;
            maxAreaPatrolPursuersFactor = 1;
            if (!CanPursuersPatrolArea)
                maxAreaPatrolPursuersFactor = 0;
            if (!CanPursuersPatrolCircumference)
                minAreaPatrolPursuersFactor = 1;

            discountFactor = 0.999999999;
            ConstantExponentialDecay discR = gameParams.R as ConstantExponentialDecay;
            if (discR != null)
                discountFactor = discR.oneRoundDiscountFactor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bestPursuersConfig"></param>
        /// <param name="maxLeakEtaTag">value is expected captured evaders per 1 reward</param>
        /// <param name="maxLeakLEscape"> value is expected captured evaders per 1 reward </param>
        private void findEquilibrium(
            out OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome> bestPursuersConfig,
            out OptimizedObj<float> maxLeakEtaTag,
            out OptimizedObj<float> maxLeakLEscape)
        {
            bestPursuersConfig = new OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome>()
                { value = 0, data = new PursuersBounds.Solution2Alt1Chromosome(this.gameParams.A_P.Count) }; // value is e_stay/e_escape for each data unit
            maxLeakEtaTag = new OptimizedObj<float>() { value = 0 };
            maxLeakLEscape = new OptimizedObj<float>() { value = 0 };

            float minAreaPatrolPursuersFactor, maxAreaPatrolPursuersFactor, maxPursuitPursuersFactor;
            double discountFactor;

            findEquilibriumPursuerSearchParams(out minAreaPatrolPursuersFactor, out maxAreaPatrolPursuersFactor, out maxPursuitPursuersFactor, out discountFactor);

            HashSet<Tuple<int, int, int>> prevPursuersConfigs = new HashSet<Tuple<int, int, int>>();
            for (float currentPursuitPursuersFactor = 0; currentPursuitPursuersFactor <= maxPursuitPursuersFactor; currentPursuitPursuersFactor += pursuerCountFactorJump)
                for (float currentAreaPursuersFactor = minAreaPatrolPursuersFactor; currentAreaPursuersFactor <= maxAreaPatrolPursuersFactor; currentAreaPursuersFactor += pursuerCountFactorJump)
                {
                    //make sure final value is maximal
                    if (currentPursuitPursuersFactor + pursuerCountFactorJump > maxPursuitPursuersFactor)
                        currentPursuitPursuersFactor = maxPursuitPursuersFactor;
                    if (currentAreaPursuersFactor + pursuerCountFactorJump > 1.0f)
                        currentAreaPursuersFactor = 1.0f;

                    OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome> tmpPursuersConfig;
                    if (!preparePursuersPolicy(currentPursuitPursuersFactor, currentAreaPursuersFactor, discountFactor, out tmpPursuersConfig))
                        continue;


                    var currentTuple = Tuple.Create(tmpPursuersConfig.data.UsedAreaPursuers, tmpPursuersConfig.data.UsedCircumferencePursuers, tmpPursuersConfig.data.UsedPursuitPursuers);
                    if(prevPursuersConfigs.Contains(currentTuple))
                        continue; // no point in estimating the same thing again
                    prevPursuersConfigs.Add(currentTuple);

                    if (tmpPursuersConfig.data.UsedCircumferencePursuers < MinimalCircumferencePatrollers)
                        continue;
                    
                    OptimizedObj<float> minEstayEtaTag, minEEscapeLEscape;
                    findBestEvadersResponse(tmpPursuersConfig.data, discountFactor, out minEstayEtaTag, out minEEscapeLEscape);

                    // we now have the best evader's parameters (l_escape and eta_tag) that yield maximal leaked data. 
                    // pursuers will try to minimize :
                    tmpPursuersConfig.value =
                        Math.Min(minEEscapeLEscape.value, minEstayEtaTag.value);

                    if (bestPursuersConfig.setIfValueIncreases(tmpPursuersConfig))
                    {
                        maxLeakEtaTag = minEstayEtaTag;
                        maxLeakLEscape = minEEscapeLEscape;
                    }

                } // each pursuers configuration
            
        }


        /// <summary>
        /// returns true if resulting config is legal, false otherwise
        /// </summary>
        /// <param name="currentPursuitPursuersFactor"></param>
        /// <param name="currentAreaPursuersFactor"></param>
        /// <param name="discountFactor"></param>
        /// <param name="pursuersConfig"></param>
        /// <returns></returns>
        private bool preparePursuersPolicy(
            double currentPursuitPursuersFactor, 
            double currentAreaPursuersFactor,
            double discountFactor,
            out OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome> pursuersConfig)
        {
            
            pursuersConfig = new OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome>()
                { value = double.PositiveInfinity, data = new PursuersBounds.Solution2Alt1Chromosome(this.gameParams.A_P.Count) };

            Dictionary<string, string> pursuersPreprocessResult = new Dictionary<string, string>();
            pursuersConfig.data.setValues(currentPursuitPursuersFactor, currentAreaPursuersFactor);

            int remainingPursuers = pursuersConfig.data.Psi;
            pursuersConfig.data.UsedEta = 0;
            pursuersConfig.data.usedP_P = 0;
            pursuersConfig.data.UsedPursuitPursuers = 0;
            if (pursuersConfig.data.Pursuit > 0)
            {
                pursuersConfig.data.UsedPursuitPursuers = Pursuit.getUsedPursuers((double)this.gameParams.r_e-1, // @FIXED PURSUIT
                    (double)this.gameParams.r_p, (double)pursuersConfig.data.Pursuit);
                pursuersConfig.data.usedP_P = Pursuit.getCaptureProbability(this.gameParams.r_e-1, // @FIXED PURSUIT
                    this.gameParams.r_p, pursuersConfig.data.UsedPursuitPursuers);
            }
            remainingPursuers -= pursuersConfig.data.UsedPursuitPursuers;

            //if (pursuersConfig.data.usedP_P > maxPursuitProb)
              //  return false;

            int areaPatrolRadius = (!CanPursuersPatrolCircumference || pursuersConfig.data.Circumference == 0) ? (this.gameParams.r_e) : (this.gameParams.r_e - 1);
            int best_rp = 4;
            PatrolAndPursuit.PatrolAlg bestAlg =
                PatrolAndPursuit.getBestPatrolType(best_rp, (int)areaPatrolRadius, pursuersConfig.data.Area);
            for (int tested_r_p = 5; tested_r_p <= this.gameParams.r_p; ++tested_r_p)
            {
                PatrolAndPursuit.PatrolAlg tmpAlg =
                    PatrolAndPursuit.getBestPatrolType(tested_r_p, (int)areaPatrolRadius, pursuersConfig.data.Area);
                if (tmpAlg.p_a > bestAlg.p_a)
                {
                    bestAlg = tmpAlg;
                    best_rp = tested_r_p;
                }
            }
            if (bestAlg.alg == null && discountFactor > 0.999 && !CanPursuersPatrolCircumference)
                return false;

            if (bestAlg.alg == null)
            {
                pursuersConfig.data.UsedAreaPursuers = 0;
                pursuersConfig.data.usedPatrolRP = 0;
                pursuersConfig.data.usedP_A = 0;
            }
            else
            {
                pursuersConfig.data.UsedAreaPursuers = bestAlg.alg.getUsedPursuersCount(best_rp, (int)areaPatrolRadius, pursuersConfig.data.Area);
                pursuersConfig.data.usedPatrolRP = best_rp;
                pursuersConfig.data.usedP_A = bestAlg.p_a;
            }
            remainingPursuers -= pursuersConfig.data.UsedAreaPursuers;

            pursuersConfig.data.usedP_C = 0;
            pursuersConfig.data.UsedCircumferencePursuers = 0;
            if (areaPatrolRadius < (int)this.gameParams.r_e)
            {
                // solution 2 is used (not only area patrol)
                pursuersConfig.data.UsedCircumferencePursuers = 0;

                for (int tested_r_p = 4; tested_r_p <= this.gameParams.r_p; tested_r_p += 2)
                {
                    int tmpcircumferencePursuers = new CircumferencePatrol().getUsedPursuersCount((int)tested_r_p, (int)this.gameParams.r_e, remainingPursuers);
                    if (tmpcircumferencePursuers == 0 || tmpcircumferencePursuers > remainingPursuers)
                        continue;
                    double tmp = new CircumferencePatrol().getCaptureProbability((int)tested_r_p, (int)this.gameParams.r_e, tmpcircumferencePursuers);
                    if (tmp > pursuersConfig.data.usedP_C)
                    {
                        pursuersConfig.data.UsedCircumferencePursuers = tmpcircumferencePursuers;
                        pursuersConfig.data.usedP_C = tmp;
                        pursuersConfig.data.usedCircumferenceRP = tested_r_p;
                    }
                }
                if (gameParams.EvaderCircumferenceEntranceKillProbWithPC (pursuersConfig.data.usedP_C) < pursuersConfig.data.usedP_A)
                    return false; // either p_c > p_a, or no circumference patrol at all
            }
            remainingPursuers -= pursuersConfig.data.UsedCircumferencePursuers;

            if (CanEvadersTransmit && pursuersConfig.data.usedP_P == 0 && pursuersConfig.data.usedP_A == 0)
                return false;
            if (pursuersConfig.data.usedP_A == 0 && (gameParams.EvaderCircumferenceEntranceKillProbWithPC(pursuersConfig.data.usedP_C)) == 0)
                return false;


            // FIXME remove the dirty fix below, and instead invoke this function recursively to make sure all pursuers are used
            int toAdd = Pursuit.getUsedPursuers((double)this.gameParams.r_e - 1, (double)this.gameParams.r_p, remainingPursuers);
            if (toAdd > 0)
            {
                pursuersConfig.data.UsedPursuitPursuers += toAdd;
                pursuersConfig.data.usedP_P = Pursuit.getCaptureProbability(this.gameParams.r_e - 1,
                    this.gameParams.r_p, pursuersConfig.data.UsedPursuitPursuers);
            }


            return true;
        }
        
        /// <summary>
        /// returns the highest reward of the two option
        /// </summary>
        /// <param name="maxLeakEtaTagFactor"></param>
        /// <param name="maxLeakLEscapeFactor"></param>
        /// <param name="chosenPolicyName"></param>
        /// <returns></returns>
        private double getRewardPerEvader(OptimizedObj<float> maxLeakEtaTagFactor,
                                          OptimizedObj<float> maxLeakLEscapeFactor,
                                          out string chosenPolicyName)
        {
            if ((maxLeakEtaTagFactor.value < maxLeakLEscapeFactor.value && CanEvadersTransmit) || !CanEvadersCrawl)
            {
                chosenPolicyName = EvadersTransmitPolicy;
                return 1.0 / maxLeakEtaTagFactor.value;
            }
            chosenPolicyName = EvadersCrawlPolicy;
            return 1.0 / maxLeakLEscapeFactor.value;
        }
        private double getRewardPerEvader(double riskPerReward)
        {
            return 1.0 / riskPerReward;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pursuersConfig"></param>
        /// <param name="discountFactor"></param>
        /// <param name="minEstayEtaTag">
        /// minEstayEtaTag.data is optimalEtaTag, and minEstayEtaTag.value is resulting EStay
        /// (if evaders can't transmit, value is  double.PositiveInfinity)
        /// </param>
        /// <param name="minEEscapeLEscape">
        /// minEstayEtaTag.data is LEscape, and minEstayEtaTag.value is resulting EEscape
        /// (if evaders can't crawl, value is  double.PositiveInfinity)
        /// </param>
        private void findBestEvadersResponse(GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome pursuersConfig,
                                               double discountFactor,
                                               out OptimizedObj<float> minEstayEtaTag,
                                               out OptimizedObj<float> minEEscapeLEscape)
        {
            minEstayEtaTag = new OptimizedObj<float>() { value = double.PositiveInfinity };
            minEEscapeLEscape = new OptimizedObj<float>() { value = double.PositiveInfinity };

            // calculate and set the optimal l_escape and eta tag (the final values will be calculate relatively to them)

            if (CanEvadersCrawl)
            {
                int l_escape = ForcedLEscape;
                if (ForcedLEscape == 0) 
                {
                    // optimize l_escape:
                    l_escape = (int)Policies.PatrolAndPursuit.calculatel_escape(pursuersConfig.usedP_A, 
                        gameParams.EvaderCircumferenceEntranceKillProbWithPC(pursuersConfig.usedP_C), discountFactor, 0);
                }

                minEEscapeLEscape.setIfValueDecreases(l_escape, 
                    Policies.PatrolAndPursuit.calculateE_Escape(pursuersConfig.usedP_A, gameParams.EvaderCircumferenceEntranceKillProbWithPC(pursuersConfig.usedP_C), l_escape, discountFactor, 0));
            }
            if (CanEvadersTransmit)
            {
                int eta_tag = ForcedSimulteneousTransmissions;
                if (ForcedSimulteneousTransmissions == 0)
                {
                    // optimize minEstayEtaTag:
                    minEstayEtaTag = findMinimalEStayEtaTag(pursuersConfig.usedP_A, pursuersConfig.usedP_C, pursuersConfig.usedP_P, gameParams.p_d, discountFactor, pursuersConfig);
                }
                else
                {
                    minEstayEtaTag.data = ForcedSimulteneousTransmissions;
                    minEstayEtaTag.value = 
                        EvaluateEtaTag(pursuersConfig.usedP_A, pursuersConfig.usedP_C, pursuersConfig.usedP_P, gameParams.p_d, discountFactor, ForcedSimulteneousTransmissions, pursuersConfig);
                    //minEstayEtaTag.setIfValueDecreases(eta_tag, Policies.EvadersPolicyTransmitFromWithinArea.practicalEStay(pursuersConfig.usedP_A, gameParams.EvaderCircumferenceEntranceKillProbWithPC(pursuersConfig.usedP_C), pursuersConfig.usedP_P, gameParams.p_d, discountFactor, eta_tag));
                }
                
            }
            
        }


        protected Dictionary<string,string> generatePolicyInput(GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome bestPursuersConfig)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            
            res[AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_CAPTURE_PROB.key] = bestPursuersConfig.usedP_A.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_CAPTURE_PROB.key] = bestPursuersConfig.usedP_C.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.PURSUIT_CAPTURE_PROB.key] = bestPursuersConfig.usedP_P.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_PURSUERS_COUNT.key] = bestPursuersConfig.UsedAreaPursuers.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_PURSUERS_COUNT.key] = bestPursuersConfig.UsedCircumferencePursuers.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.PURSUIT_PURSUERS_COUNT.key] = bestPursuersConfig.UsedPursuitPursuers.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_VELOCITY.key] = bestPursuersConfig.usedPatrolRP.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_VELOCITY.key] = bestPursuersConfig.usedCircumferenceRP.ToString();

            return res;
        }
        protected GameResult generatePolicyInput(
            OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome> bestPursuersConfig,
            OptimizedObj<float> maxLeakEtaTag,
            OptimizedObj<float> maxLeakLEscape)
        {
            GameResult res = new GameResult();
            
            res.capturedEvaders = gameParams.A_E.Count;
            res.AddRange(generatePolicyInput(bestPursuersConfig.data));
            res.AddRange(gameParams.toValueMap(),false);

            string chosenPolicyName;
            res.utilityPerEvader =
                (float)getRewardPerEvader(maxLeakEtaTag, maxLeakLEscape, out chosenPolicyName);
            
            if ((maxLeakEtaTag.value < maxLeakLEscape.value && CanEvadersTransmit) || !CanEvadersCrawl)
                res[AppConstants.AppArgumentKeys.EVADER_POLICY.key] = EvadersTransmitPolicy;
            else
                res[AppConstants.AppArgumentKeys.EVADER_POLICY.key] = EvadersCrawlPolicy;
            
            res[AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.ESCAPE_REWARD.key] = getRewardPerEvader(maxLeakLEscape.value).ToString();
            res[AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.TRANSMIT_REWARD.key] = getRewardPerEvader(maxLeakEtaTag.value).ToString();
            res[AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS.key] = maxLeakEtaTag.data.ToString();
            res[AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE.key] = maxLeakLEscape.data.ToString();

            return res;
        }

        /// <summary>
        /// min. avg leaked per evader for each:
        /// each pursuit patrol X
        /// each circumference patrol (best rp) X
        /// each (Derived) area patrol (best rp) 
        /// max. avg leaked per evader:
        /// either escape after constant time ( smaller or larger than optimal, no-discount l_ex)
        /// either transmit eta' simulteneous transmissions
        /// </summary>
        /// <param name="opt"></param>
        public override void process(ParallelOptions opt = null)
        {
            OptimizedObj<GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome> bestPursuersConfig;
            OptimizedObj<float> maxLeakEtaTag;
            OptimizedObj<float> maxLeakLEscape;
            findEquilibrium(out bestPursuersConfig, out maxLeakEtaTag, out maxLeakLEscape);


            // fixme remove below
            //string chosenEvesPolicyName;
            //double simAvgReward, thryAvgReward, simP_P, simP_A, simP_C, simP_D;
            //Dictionary<Point, double> visits = new Dictionary<Point, double>();
            //PerformanceEstimation es;
            //Dictionary<string, string> inp, preprocess;

            //List<ProcessGameResult> simRes;
            //var oneThread = new ParallelOptions();
            //var manyThread = new ParallelOptions();
            //oneThread.MaxDegreeOfParallelism = 1;
            //manyThread.MaxDegreeOfParallelism = 8;
            
            //findEquilibrium(out bestPursuersConfig, out maxLeakEtaTag, out maxLeakLEscape);
            //findBestEvadersResponse(bestPursuersConfig.data, 0.8, out maxLeakEtaTag, out maxLeakLEscape);

            //inp = generatePolicyInput(bestPursuersConfig, maxLeakEtaTag, maxLeakLEscape);
            //thryAvgReward = getRewardPerEvader(maxLeakEtaTag, maxLeakLEscape, out chosenEvesPolicyName);
            //es = new PerformanceEstimation(chosenEvesPolicyName, PursuersPolicy, gameParams, this.gameGraph, this.policyInput, inp);
            //preprocess = es.calculateMaximalLeakedDataTheoreticalBound();
            //preprocess.AddRange(inp);
            //simRes = es.estimatePerformance(new GoEGameProcess(), 10, int.MaxValue, false, preprocess, manyThread);
            //simAvgReward = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[OutputFields.PRESENTABLE_UTILITY]);

            //try
            //{
            //    simP_P = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[AppConstants.Policies.PatrolAndPursuit.RESULTED_P_P]);
            //    simP_C = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[AppConstants.Policies.PatrolAndPursuit.RESULTED_P_C]);
            //    simP_A = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[AppConstants.Policies.PatrolAndPursuit.RESULTED_P_A]);
            //    simP_D = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[AppConstants.Policies.PatrolAndPursuit.RESULTED_P_D]);

            //    visits.Clear();
            //    visits[new Point(-1, 0)] = 0;
            //    visits[new Point(0, 0)] = 0;
            //    visits[new Point(1, 0)] = 0;
            //    visits[new Point(0, 1)] = 0;
            //    visits[new Point(0, -1)] = 0;
            //    var tmpDic = new Dictionary<Point, double>(visits);
            //    foreach (Point p in tmpDic.Keys)
            //    {
            //        var dic = Utils.Algorithms.AlgorithmUtils.Average(simRes);
            //        string val = dic[p.ToString()];
            //        visits[p] = double.Parse(val);
            //    }

            //}
            //catch (Exception) { }

            //gameParams.p_d = 0.9;
            //findEquilibrium(out bestPursuersConfig, out maxLeakEtaTag, out maxLeakLEscape);
            //inp = generatePolicyInput(bestPursuersConfig, maxLeakEtaTag, maxLeakLEscape);
            //thryAvgReward = getRewardPerEvader(maxLeakEtaTag, maxLeakLEscape, out chosenEvesPolicyName);
            //es = new PerformanceEstimation(chosenEvesPolicyName, PursuersPolicy, gameParams, this.gameGraph, this.policyInput, inp);
            //preprocess = es.calculateMaximalLeakedDataTheoreticalBound();
            //preprocess.AddRange(inp);
            //simRes = es.estimatePerformance(new GoEGameProcess(), 50, int.MaxValue, false, preprocess, oneThread);
            //simAvgReward = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[OutputFields.PRESENTABLE_UTILITY]);
            //try
            //{
            //    simP_P = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[AppConstants.Policies.PatrolAndPursuit.RESULTED_P_P]);
            //}
            //catch (Exception) { }

            //gameParams.p_d = 0.7;
            //findEquilibrium(out bestPursuersConfig, out maxLeakEtaTag, out maxLeakLEscape);
            //inp = generatePolicyInput(bestPursuersConfig, maxLeakEtaTag, maxLeakLEscape);
            //thryAvgReward = getRewardPerEvader(maxLeakEtaTag, maxLeakLEscape, out chosenEvesPolicyName);
            //es = new PerformanceEstimation(chosenEvesPolicyName, PursuersPolicy, gameParams, this.gameGraph, this.policyInput, inp);
            //preprocess = es.calculateMaximalLeakedDataTheoreticalBound();
            //preprocess.AddRange(inp);
            //simRes = es.estimatePerformance(new GoEGameProcess(), 50, int.MaxValue, false, preprocess, oneThread);
            //simAvgReward = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[OutputFields.PRESENTABLE_UTILITY]);

            //try
            //{
            //    simP_P = double.Parse(Utils.Algorithms.AlgorithmUtils.Average(simRes)[AppConstants.Policies.PatrolAndPursuit.RESULTED_P_P]);
            //}
            //catch (Exception) { }


            // fixme remove above


            optimizationOutput = generatePolicyInput(bestPursuersConfig, maxLeakEtaTag, maxLeakLEscape);

        }

        public override GameResult optimizationOutput { get; protected set; }

        ///// <summary>
        ///// updates tmpMaxLeakLEscapeFactorLeakedData and tmpMaxLeakLEscape, if the current estimation shows a larger leakage
        ///// </summary>
        //private void estimateParam(float currentFactor, Dictionary<string, string> policyInput, Dictionary<string, string> pursuersPreprocessResult,
        //                             string paramFactorEntry, string evaderStrategyName,
        //                             ref AlgorithmUtils.OptimizedObj<float> tmpMaxLeakParamFactor)
        //{
        //    policyInput[paramFactorEntry] = currentFactor.ToString();

        //    PerformanceEstimation estimator =
        //        new PerformanceEstimation(evaderStrategyName, PatrolAndPursuitOptimizer.PursuersPolicy, gameParams, gameGraph, policyInput,null);

        //    var results = estimator.estimatePerformance( repetitionCount, int.MaxValue, false, pursuersPreprocessResult, currentParalleOpt);
        //    float currentFactorLeakedData = Utils.AlgorithmUtils.Average<ProcessGameResult>(results).utilityPerEvader;

        //    tmpMaxLeakParamFactor.setIfValueIncreases(currentFactor, currentFactorLeakedData);

        //}

        private static string PursuersPolicy = typeof(PatrolAndPursuit).Name;
        private static string EvadersCrawlPolicy = typeof(EvadersPolicyEscapeAfterConstantTime).Name;
        private static string EvadersTransmitPolicy = typeof(EvadersPolicyTransmitFromWithinArea).Name;

        protected float l_escapeMinFactor, l_escapeMaxFactor, l_escapeFactorJump,
                        etaTagMinFactor, etaTagMaxFactor, etaTagFactorJump;
        protected float pursuerCountFactorJump;
    }
    public class PatrolAndPursuitOptimizerPractical : PatrolAndPursuitOptimizerBase
    {
        protected override double EvaluateEtaTag(double p_a, double p_c, double p_p, double p_d, double discountFactor, float eta_tag,
            GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome pursuersConfig)
        {

            return Policies.EvadersPolicyTransmitFromWithinArea.practicalEStay(p_a, gameParams.EvaderCircumferenceEntranceKillProbWithPC(p_c), p_p, p_d, discountFactor, eta_tag);
        }
    }
    public class PatrolAndPursuitOptimizerTheory : PatrolAndPursuitOptimizerBase
    {
        protected override double EvaluateEtaTag(double p_a, double p_c, double p_p, double p_d, double discountFactor, float eta_tag,
            GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome pursuersConfig)
        {
            return Policies.PatrolAndPursuit.e_stay_etaTag(p_a, p_p, p_d, 0, discountFactor, eta_tag);
        }
    }
    public class PatrolAndPursuitOptimizerPracticalPessimistic : PatrolAndPursuitOptimizerBase
    {
        protected override double EvaluateEtaTag(double p_a, double p_c, double p_p, double p_d, double discountFactor, float eta_tag,
            GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome pursuersConfig)
        {
            return Policies.EvadersPolicyTransmitFromWithinArea.practicalEStay_pessimistic(p_a, gameParams.EvaderCircumferenceEntranceKillProbWithPC(p_c), p_p, p_d, discountFactor, eta_tag);
        }
    }

    public class PatrolAndPursuitOptimizerEtaTagEstimation : PatrolAndPursuitOptimizerBase
    {
        protected override double EvaluateEtaTag(double p_a, double p_c, double p_p, double p_d, double discountFactor, float eta_tag,
            GoE.GameLogic.PursuersBounds.Solution2Alt1Chromosome pursuersConfig)
        {
            ParallelOptions opt = new ParallelOptions();
            opt.MaxDegreeOfParallelism = 8;
            Dictionary<string, string> tmpVals = new Dictionary<string, string>(this.policyInput);
            tmpVals[AppConstants.AppArgumentKeys.SIMULATION_REPETETION_COUNT.key] = Optimizers.ESTIMATION_REPETITION_COUNT.tryRead(tmpVals);


            tmpVals.AddRange(generatePolicyInput(pursuersConfig));
            tmpVals[AppConstants.AppArgumentKeys.EVADER_POLICY.key] = typeof(EvadersPolicyTransmitFromWithinArea).Name;
            tmpVals[AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS.key] = eta_tag.ToString();

            return 1.0/double.Parse(SimProcess.processParams(opt, tmpVals, gameGraph,false,false).processOutput[AppConstants.GameProcess.OutputFields.PRESENTABLE_UTILITY]);
        }
        public override List<ArgEntry> optimizationInputKeys
        {
            get
            {
                var res = base.optimizationInputKeys;
                res.Add(Optimizers.ESTIMATION_REPETITION_COUNT);
                return res;
            }
        }
    }


}