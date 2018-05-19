using GoE.GameLogic;
using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.GameLogic.Algorithms;
using Meta.Numerics.Functions;
using System.IO;
using System.Windows.Forms;
using GoE.Utils.Extensions;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.Utils.Algorithms;
using GoE.AppConstants;

namespace GoE.Policies
{
    
    public class PatrolAndPursuit : AGoEPursuersPolicy
    {
        private GridGameGraph g;
        private GoEGameParams gm;
        private IPolicyGUIInputProvider ginput;
        private Dictionary<Pursuer, Location> prevLocation = new Dictionary<Pursuer, Location>();
        Pursuer firstPursuer = null;
        private int currentRound;
        private int areaPatrolRad;
        Point pursuitTarget;

        private List<Point> pursuitArea;
        private Dictionary<Pursuer, Location> initLocation;
        private bool noPursuit = false;

        GoE.GameLogic.Algorithms.CircumferencePatrol circumferencePatrol;
        //GoE.GameLogic.Algorithms.CircularUniformAreaPatrol areaPatrol = new GoE.GameLogic.Algorithms.CircularUniformAreaPatrol();
        //GoE.GameLogic.Algorithms.SparseAreaPatrol areaPatrol = new GoE.GameLogic.Algorithms.SparseAreaPatrol();
        APatrol areaPatrol;
        private Utils.ListRangeEnumerable<Pursuer> circumferencePursuers, areaPatrollers, pursuitPursuers;

        int circumferenceRP;
        int patrolRP;
        //private static volatile int prevRP = -1;
        //private static volatile int prevPSI = -1;
        //private static bool isFirstInvocation = true;

        //private static bool canTransmit;
        //private static bool isSolution2;
        //private static bool canRawl;

        //private System.Object initLock = new object();

        /// <summary>
        /// may be queried after init()
        /// </summary>
        public double AreaPatrolCaptureProbability
        {
            get
            {
                if (CircumferencePatrolCaptureProbability == 0)
                    return calculatePA(patrolRP, gm.r_e, areaPatrollers.Count());
                else
                    return calculatePA(patrolRP, gm.r_e - 1, areaPatrollers.Count());
            }
        }

        /// <summary>
        /// may be queried after init()
        /// </summary>
        public double CircumferencePatrolCaptureProbability
        {
            get
            {
                return calculatePC(circumferencePursuers.Count(), circumferenceRP, gm.r_e);
            }
        }
        /// <summary>
        /// may be queried after init()
        /// </summary>
        public double PursuitCaptureProbability
        {
            get
            {
                return calculatePP(pursuitPursuers.Count(), gm.r_p, gm.r_e-1);
            }
        }

        //public override GameResult getMaxLeakedDataTheoreticalBound() 
        //{
        //    GameResult res = new GameResult();
        //    res.AddRange(theoreticalGameRes);
        //    return res;
        //}

        //public override List<string> globalPolicyInputArgs 
        //{
        //    get 
        //    {
        //        return new string[]{
        //            AppConstants.Policies.PatrolAndPursuit.CAN_EVADERS_TRANSMIT,
        //            AppConstants.Policies.PatrolAndPursuit.CAN_EVADERS_CRAWL,
        //            AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_ALLOWED,
        //            AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_ALLOWED,
        //            AppConstants.Policies.PatrolAndPursuit.PURSUIT_ALLOWED}.ToList<string>();
        //    } 
        //}

        public override List<ArgEntry> policyInputKeys()
        {
           // get
           // {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_PURSUERS_COUNT);
                res.Add(AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_PURSUERS_COUNT);
                res.Add(AppConstants.Policies.PatrolAndPursuit.PURSUIT_PURSUERS_COUNT);
                res.Add(AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_VELOCITY);
                res.Add(AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_VELOCITY);

                return res;
            //}
        }
        public override APolicyOptimizer constructTheoreticalOptimizer()
        {
            return new GoE.Policies.PatrolAndPursuitOptimizerTheory();
        }
        //public override Dictionary<string, string> preProcess(GridGameGraph G, GoEGameParams prm, Dictionary<string,string> policyInput)
        //{
        //    Dictionary<string, string> res = new Dictionary<string, string>();

        //    List<string> typeRes; 
        //    string[] inputStrings = 
        //        globalPolicyInputArgs.ToArray();

        //    //if(input.hasBoardGUI())
        //    //    typeRes = input.ShowDialog(inputStrings, "pursuers policy type", new string[]{"0","0","0"});
        //    //else
        //        typeRes = input.ShowDialog(inputStrings, "pursuers policy type", null);

        //    res.AddRange(inputStrings, typeRes);


        //    bool canEvadersTransmit = typeRes[0] == "1";
        //    bool canEvadersCrawl = typeRes[1] == "1";
        //    bool canPatrolArea = typeRes[2] == "1";
        //    bool canPatrolCircumference = typeRes[3] == "1";
        //    bool canPursue = typeRes[4] == "1";
        //    float maxCaptureProb = 1;
        //    if (typeRes[5] != "")
        //        maxCaptureProb = float.Parse(typeRes[5]);

        //    double discountFactor = 0.999999999;
        //    ConstantExponentialDecay discR = prm.R as ConstantExponentialDecay;
        //    if (discR != null)
        //        discountFactor = discR.oneRoundDiscountFactor;


        //    int overridingEtaTag = -1;
        //    int overridingLEscape = -1;

        //    // check if L_ESCAPE was overriden by user input(if not, exception is thrown):
        //    try
        //    {
        //        var assumptions =
        //            input.ShowDialog(new string[]{AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE},
        //                             "pursuer assumptions",
        //                             null);
        //        overridingLEscape = int.Parse(assumptions[0]);
        //    } catch (Exception) { }

        //    // check if EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS was overriden by user input (if not, exception is thrown):
        //    try
        //    {
        //        var assumptions =
        //            input.ShowDialog(new string[]{AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS},
        //                             "pursuer assumptions",
        //                             null);
        //        overridingEtaTag= int.Parse(assumptions[0]);
        //    }
        //    catch (Exception) { }


        //    if (overridingEtaTag != -1 && overridingLEscape != -1)
        //    {
        //        input.addLogValue("PatrolAndPursuitError", "can't have both overridingEtaTag and overridingLEscape set");
        //        throw new Exception("can't have both overridingEtaTag and overridingLEscape set");
        //    }

        //    res.AddRange(
        //        PursuersBounds.findSolution2Alt1MaximalLeakedData((int)prm.r_e, prm.A_P.Count, (int)prm.r_p,prm.p_d, prm.A_E.Count,
        //                            canEvadersTransmit,canPatrolCircumference,canPursue,canEvadersCrawl,canPatrolArea,
        //                            discountFactor,
        //                            overridingEtaTag,
        //                            overridingLEscape,
        //                            maxCaptureProb));

        //    return res;


        //    //int areaPatrollersCount, circumferencePursuersCount, pursuitPursuersCount;
        //    //if (!Int32.TryParse(pursuerCounts[0], out areaPatrollersCount) ||
        //    //    !Int32.TryParse(pursuerCounts[1], out circumferencePursuersCount) ||
        //    //    !Int32.TryParse(pursuerCounts[2], out pursuitPursuersCount))
        //    //    throw new Exception("can't init PursuersPolicySolution2 - invalid pursuer counts input format");

        //        //try // fixme remove/make better
        //        //{
        //        //    string mod = "re" + gm.r_e.ToString();

        //        //    if (canRawl)
        //        //        mod += "_Crawl_";
        //        //    else
        //        //        mod += "_noCrawl_";

        //        //    if (!isSolution2)
        //        //        mod += "_solution1_";
        //        //    else
        //        //        mod += "_solution2_";
        //        //    if (canTransmit)
        //        //        mod += "_Transmit_";
        //        //    else
        //        //        mod += "_NoTransmit_";
        //        //    if (prevRP != prm.r_p || prevPSI != prm.A_P.Count)
        //        //    {
        //        //        double p_a = double.Parse(defaults["p_a"]);
        //        //        double p_c = double.Parse(defaults["p_c"]);
        //        //        double p_p = double.Parse(defaults["p_p"]);
        //        //        int l_escape = calculateIntegerL_Escape(p_a, p_c);
        //        //        int simTransmissions;
        //        //        calculateE_Stay(prm.A_E.Count, p_a, p_p, out simTransmissions);

        //        //        string folder;


        //        //        if (IPursuersPolicy.defaults.ContainsKey("results_path"))
        //        //        {
        //        //            folder = IPursuersPolicy.defaults["results_path"];
        //        //        }
        //        //        else
        //        //        {
        //        //            folder = @"C:\Users\Mai\Desktop\pursuit\analysis\";
        //        //            if (!Directory.Exists(folder))
        //        //                folder = "";
        //        //        }



        //        //        string fileName = folder + "_theory_" + mod + ".txt";

        //        //        if (!File.Exists(fileName))
        //        //            File.AppendAllText(fileName, "prsrs rsrcs,\tLeakedPerEve,\t\tp_a,\t\tp_c,\t\tp_p,\t\tl_escape,\tSim.Transmissions" + Environment.NewLine);

        //        //        File.AppendAllText(fileName, prm.A_P.Count.ToString("00.0")+"("  + prm.r_p.ToString("0.000") + ")" + ",\t" + 
        //        //                                     (float.Parse(defaults["totalLeaked"])/prm.A_E.Count).ToString("0.000") + ",\t\t" + 
        //        //                                     p_a.ToString("0.000") + ",\t\t" + 
        //        //                                     p_c.ToString("0.000") + ",\t\t" + 
        //        //                                     p_p.ToString("0.000") + ",\t\t" + 
        //        //                                     l_escape.ToString("0.000") + ",\t\t" + 
        //        //                                     simTransmissions.ToString("0.000") + Environment.NewLine);
        //        //    }
        //        //    prevRP = prm.r_p;
        //        //    prevPSI = prm.A_P.Count;
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    MessageBox.Show(ex.Message);
        //        //}


        //        //patrolRP = Int32.Parse(pursuerCounts[3]);
        //        //if (patrolRP == 0 || double.Parse(defaults["p_a"]) == 0)
        //        //    throw new Exception();

        //        //circumferenceRP = Int32.Parse(pursuerCounts[4]);

        //        //double pc = calculatePC(circumferencePursuersCount, circumferenceRP, prm.r_e);
        //        //defaults["p_c"] = pc.ToString();
        //        //if (pc == 0)
        //        //    defaults["p_a"] = calculatePA(patrolRP, prm.r_e, areaPatrollersCount).ToString();
        //        //else
        //        //    defaults["p_a"] = calculatePA(patrolRP, prm.r_e - 1, areaPatrollersCount).ToString();

        //        //defaults["p_p"] = calculatePP(pursuitPursuersCount, prm.r_p, prm.r_e).ToString();

        //        //if (pc != 0)
        //        //{
        //        //    int groupSize = CircumferencePatrol.getGroupSize(circumferenceRP, gm.r_e, circumferencePursuersCount);
        //        //    IPursuersPolicy.defaults["MaximalPursueresDiff"] = (circumferenceRP + 1 - groupSize).ToString();
        //        //}
        //        //else
        //        //    IPursuersPolicy.defaults["MaximalPursueresDiff"] = "-1";
        //}


        Dictionary<string, string> theoreticalGameRes;

        public override bool init(AGameGraph G, IGameParams iprm, 
                                  IPolicyGUIInputProvider input, 
                                  Dictionary<string,string> policyParams)
        {
            this.g = (GridGameGraph)G;
            this.gm = (GoEGameParams)iprm;
            this.ginput = input;

            //if (preProcessInput == null)
            //    preProcessInput = preProcess(G, gm, input);
            //theoreticalGameRes = new Dictionary<string, string>(preProcessInput);
            //if (policyParams != null)
            //    preProcessInput.AddRange(policyParams);

            //string[] inputStrings = new string[5]{
            //   AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_PURSUERS_COUNT,
            //   AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_PURSUERS_COUNT,
            //   AppConstants.Policies.PatrolAndPursuit.PURSUIT_PURSUERS_COUNT,
            //   AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_VELOCITY,
            //   AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_VELOCITY};

            //List<string> pursuerCounts =
            //    input.ShowDialog(inputStrings,
            //                     "Solution2 parameters",
            //                     preProcessInput.getValuesOf(inputStrings).ToArray());

            int areaPatrollersCount, circumferencePursuersCount, pursuitPursuersCount;
            if (!Int32.TryParse(AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_PURSUERS_COUNT.tryRead(policyParams, ""), out areaPatrollersCount) ||
                !Int32.TryParse(AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_PURSUERS_COUNT.tryRead(policyParams, ""), out circumferencePursuersCount) ||
                !Int32.TryParse(AppConstants.Policies.PatrolAndPursuit.PURSUIT_PURSUERS_COUNT.tryRead(policyParams, ""), out pursuitPursuersCount) ||
                !Int32.TryParse(AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_VELOCITY.tryRead(policyParams, ""), out patrolRP) ||
                !Int32.TryParse(AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_VELOCITY.tryRead(policyParams, ""), out circumferenceRP))
            {
                throw new Exception("can't init PursuersPolicySolution2 - invalid pursuer counts input format");
            }

            //for (int i = 0; i < pursuerCounts.Count; ++i)
            //    input.addLogValue(inputStrings[i], pursuerCounts[i]);

                //lock (initLock)
                //{
                //if (isFirstInvocation)
                //{
                //    isFirstInvocation = false;
                //    List<string> typeRes; 
                //    if(gui.hasBoardGUI())
                //        typeRes = gui.ShowDialog(new string[] { "canTransmit (0/1)", "isSolution2 (0/1)", "canRawl (0/1)" }, "pursuers policy type", new string[]{"0","1","0"});
                //    else
                //        typeRes = gui.ShowDialog(new string[] { "canTransmit (0/1)", "isSolution2 (0/1)", "canRawl (0/1)" }, "pursuers policy type", null);

                //    canTransmit = typeRes[0] == "1";
                //    isSolution2 = typeRes[1] == "1";
                //    canRawl = typeRes[2] == "1";
                //}

                //this.g = G;
                //this.gm = prm;
                //this.pgui = gui;

                ////// fixme remove
                ////if (prm.r_p < 33)
                ////    throw new Exception();


                //if (!IPursuersPolicy.defaults.ContainsKey("R_P") || 
                //     IPursuersPolicy.defaults["R_P"] != prm.r_p.ToString() || 
                //     IPursuersPolicy.defaults["PSI"] != prm.A_P.Count.ToString())
                //{
                //    IPursuersPolicy.defaults["R_P"] = prm.r_p.ToString();
                //    IPursuersPolicy.defaults["PSI"] = prm.A_P.Count.ToString();
                //    PursuersBounds.findSolution2Alt1MaximalLeakedData((int)prm.r_e, prm.A_P.Count, (int)prm.r_p, prm.A_E.Count,
                //                canTransmit,
                //                isSolution2,
                //                isSolution2,
                //                canRawl,
                //                null, defaults);
                //}

                //string[] inputStrings = new string[5]
                //    {"uniform area patrol pursuers #", 
                //     "circumference patrol pursuers #(0 extends area patrol)",
                //     "pursuit pursuers #",
                //     "areaPatrol_r_p",
                //    "circumferencePatrol_r_p"};



                //// fixme remove
                ////defaults["uniform area patrol pursuers #"] = (prm.A_P.Count() / 2).ToString();
                ////defaults["circumference patrol pursuers #(0 extends area patrol)"] = (prm.A_P.Count() / 2).ToString();
                ////defaults["areaPatrol_r_p"] = "18";
                ////defaults["circumferencePatrol_r_p"] = "18";

                //List<string> pursuerCounts =
                //    gui.ShowDialog(inputStrings, "Solution2 parameters",
                //        new string[] 
                //    { 
                //        defaults[inputStrings[0]],
                //        defaults[inputStrings[1]],
                //        defaults[inputStrings[2]],
                //        defaults[inputStrings[3]],
                //        defaults[inputStrings[4]]
                //    });

                //int areaPatrollersCount, circumferencePursuersCount, pursuitPursuersCount;
                //if (!Int32.TryParse(pursuerCounts[0], out areaPatrollersCount) ||
                //    !Int32.TryParse(pursuerCounts[1], out circumferencePursuersCount) ||
                //    !Int32.TryParse(pursuerCounts[2], out pursuitPursuersCount))
                //    throw new Exception("can't init PursuersPolicySolution2 - invalid pursuer counts input format");


                //try // fixme remove/make better
                //{
                //    string mod = "re" + gm.r_e.ToString();

                //    if (canRawl)
                //        mod += "_Crawl_";
                //    else
                //        mod += "_noCrawl_";

                //    if (!isSolution2)
                //        mod += "_solution1_";
                //    else
                //        mod += "_solution2_";
                //    if (canTransmit)
                //        mod += "_Transmit_";
                //    else
                //        mod += "_NoTransmit_";
                //    if (prevRP != prm.r_p || prevPSI != prm.A_P.Count)
                //    {
                //        double p_a = double.Parse(defaults["p_a"]);
                //        double p_c = double.Parse(defaults["p_c"]);
                //        double p_p = double.Parse(defaults["p_p"]);
                //        int l_escape = calculateIntegerL_Escape(p_a, p_c);
                //        int simTransmissions;
                //        calculateE_Stay(prm.A_E.Count, p_a, p_p, out simTransmissions);

                //        string folder;


                //        if (IPursuersPolicy.defaults.ContainsKey("results_path"))
                //        {
                //            folder = IPursuersPolicy.defaults["results_path"];
                //        }
                //        else
                //        {
                //            folder = @"C:\Users\Mai\Desktop\pursuit\analysis\";
                //            if (!Directory.Exists(folder))
                //                folder = "";
                //        }



                //        string fileName = folder + "_theory_" + mod + ".txt";

                //        if (!File.Exists(fileName))
                //            File.AppendAllText(fileName, "prsrs rsrcs,\tLeakedPerEve,\t\tp_a,\t\tp_c,\t\tp_p,\t\tl_escape,\tSim.Transmissions" + Environment.NewLine);

                //        File.AppendAllText(fileName, prm.A_P.Count.ToString("00.0")+"("  + prm.r_p.ToString("0.000") + ")" + ",\t" + 
                //                                     (float.Parse(defaults["totalLeaked"])/prm.A_E.Count).ToString("0.000") + ",\t\t" + 
                //                                     p_a.ToString("0.000") + ",\t\t" + 
                //                                     p_c.ToString("0.000") + ",\t\t" + 
                //                                     p_p.ToString("0.000") + ",\t\t" + 
                //                                     l_escape.ToString("0.000") + ",\t\t" + 
                //                                     simTransmissions.ToString("0.000") + Environment.NewLine);
                //    }
                //    prevRP = prm.r_p;
                //    prevPSI = prm.A_P.Count;
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show(ex.Message);
                //}


                //patrolRP = Int32.Parse(pursuerCounts[3]);
                //if (patrolRP == 0 || double.Parse(defaults["p_a"]) == 0)
                //    throw new Exception();

                //circumferenceRP = Int32.Parse(pursuerCounts[4]);

                //double pc = calculatePC(circumferencePursuersCount, circumferenceRP, prm.r_e);
                //defaults["p_c"] = pc.ToString();
                //if (pc == 0)
                //    defaults["p_a"] = calculatePA(patrolRP, prm.r_e, areaPatrollersCount).ToString();
                //else
                //    defaults["p_a"] = calculatePA(patrolRP, prm.r_e - 1, areaPatrollersCount).ToString();

                //defaults["p_p"] = calculatePP(pursuitPursuersCount, prm.r_p, prm.r_e).ToString();

                //if (pc != 0)
                //{
                //    int groupSize = CircumferencePatrol.getGroupSize(circumferenceRP, gm.r_e, circumferencePursuersCount);
                //    IPursuersPolicy.defaults["MaximalPursueresDiff"] = (circumferenceRP + 1 - groupSize).ToString();
                //}
                //else
                //    IPursuersPolicy.defaults["MaximalPursueresDiff"] = "-1";


                areaPatrollers = new Utils.ListRangeEnumerable<Pursuer>(gm.A_P, 0, areaPatrollersCount);
                circumferencePursuers = new Utils.ListRangeEnumerable<Pursuer>(gm.A_P, areaPatrollersCount, areaPatrollersCount + circumferencePursuersCount);
                pursuitPursuers = new Utils.ListRangeEnumerable<Pursuer>(gm.A_P,
                                                                        areaPatrollersCount + circumferencePursuersCount,
                                                                        gm.A_P.Count);
                
            if (pursuitPursuersCount == 0)
                noPursuit = true;

            targets = g.getNodesByType(NodeType.Target);

            return true;
        }

        public PatrolAndPursuit()
        {
        }

        //double p_a = 0, p_c = 0, p_p = 0;
        //double e_stay, e_escape;


        


        //public double getExpectedLeakedDataPerEvaderBound()
        //{
        //    if (c.Pursuit < 0 || c.Circumference < 0 || c.Area < 0)
        //    {
        //        return double.NegativeInfinity;
        //    }

        //    int remainingPursuers = c.Psi;


        //    double p_p = 0;
        //    c.UsedPursuitPursuers = Algorithms.Pursuit.getUsedPursuers((double)r_e, (double)r_p, (double)c.Pursuit);
        //    c.usedP_P = p_p = Algorithms.Pursuit.getCaptureProbability(r_e, r_p, c.UsedPursuitPursuers);
        //    remainingPursuers -= c.UsedPursuitPursuers;


        //    double patrolGroupSize = 1;
        //    double p_a1 = 0;
        //    int best_rp_1 = (int)r_p;
        //    int areaPursuers;
        //    double p_a2 = 0;
        //    int best_rp_2 = (int)r_p;

        //    // note we don't use c.Area, and instead use remainingPursuers
        //    for (int tested_r_p = 4; tested_r_p <= r_p; ++tested_r_p)
        //    {
        //        double tmppatrolGroupSize = Algorithms.SwitchPatrol.getCaptureProbability((int)tested_r_p, (int)r_e, c.Area);

        //        double tmp = tmppatrolGroupSize / (2 * (tested_r_p / 2) * (tested_r_p / 2 + 1) + 1 - tmppatrolGroupSize); // group size divided by area guarded by each pursuer
        //        if (tmp > p_a1)
        //        {
        //            patrolGroupSize = tmppatrolGroupSize;
        //            p_a1 = tmp;
        //            best_rp_1 = tested_r_p;
        //        }
        //    }

        //    for (int tested_r_p = 4; tested_r_p <= r_p; ++tested_r_p)
        //    {
        //        areaPursuers = Algorithms.SwitchPatrol.getUsedPursuersCount((int)tested_r_p, (int)r_e, c.Area);
        //        double tmp = Algorithms.SwitchPatrol.getCaptureProbability((int)tested_r_p, (int)r_e, areaPursuers);
        //        if (tmp > p_a2)
        //        {
        //            p_a2 = tmp;
        //            best_rp_2 = tested_r_p;
        //        }
        //    }
        //    double p_a;
        //    if (p_a1 > p_a2)
        //    {
        //        p_a = p_a1;
        //        c.UsedAreaPursuers = (int)(
        //            patrolGroupSize * (int)(
        //            (2 * best_rp_1 * (best_rp_1 + 1) + 1) / (2 * (best_rp_1 / 2) * (best_rp_1 / 2 + 1) + 1)));
        //    }
        //    else
        //    {
        //        if (c.Area > 0)
        //            c.UsedAreaPursuers = Algorithms.SwitchPatrol.getUsedPursuersCount((int)best_rp_2, (int)r_e, c.Area);
        //        p_a = p_a2;
        //    }
        //    c.usedP_A = p_a;
        //    remainingPursuers -= c.UsedAreaPursuers;

        //    if (p_a == 0)
        //        return double.NegativeInfinity; // pursuers can stay inside for as long as they want

        //    double p_c = 0;
        //    int circumferencePursuers = 0;

        //    for (int tested_r_p = 4; tested_r_p <= r_p; ++tested_r_p)
        //    {
        //        int tmpcircumferencePursuers = Algorithms.CircumferencePatrol.getUsedPursuersCount((int)tested_r_p, (int)r_e, remainingPursuers);
        //        double tmp = Algorithms.CircumferencePatrol.getCaptureProbability((int)tested_r_p, (int)r_e, tmpcircumferencePursuers);
        //        if (tmp > p_c)
        //        {
        //            circumferencePursuers = tmpcircumferencePursuers;
        //            p_c = tmp;
        //        }
        //    }
        //    c.UsedCircumferencePursuers = circumferencePursuers;
        //    c.usedP_C = p_c;

        //    if (p_c < p_a)
        //        return double.NegativeInfinity; // this contradicts the solution's analysis

        //    double lmbrtArg = -(p_c * p_c - 2 * p_c + 1) / (Math.E * Math.Pow(-1 + p_a, 2));
        //    double extremePoint = 3;
        //    double l_escape = 1;


        //    if (lmbrtArg > -0.99999 / Math.E) // lambert isn't defined outside this range
        //    {
        //        extremePoint = -(AdvancedMath.LambertW(lmbrtArg) + 1) / (Math.Log(1 - p_a, Math.E));
        //        extremePoint = Math.Max(3, extremePoint);

        //        //if (optTime > 2 &&
        //        //    (1.0 / p_c - 1) < optTime * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, optTime - 2)) - 1))
        //        //{
        //        //    l_escape = optTime;
        //        //}
        //    }
        //    //else if(lmbrtArg > )

        //    // fixme tested code: not in the paper yet:
        //    double optTimeUtil = (1.0 / p_c) - 1;
        //    if (optTimeUtil < extremePoint * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, extremePoint - 2)) - 1))
        //    {
        //        l_escape = extremePoint;
        //        optTimeUtil = extremePoint * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, extremePoint - 2)) - 1);
        //    }
        //    //if(optTimeUtil < 3 * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, 3 - 2)) - 1))
        //    //{
        //    //    optTimeUtil = 3 * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, 3 - 2)) - 1);
        //    //    l_escape = 3;
        //    //}

        //    // fixme remove below:
        //    //int jump = 100;
        //    //for (double checkedTime = 3; checkedTime <= 4000; checkedTime += jump)
        //    //{

        //    //    if (optTimeUtil <
        //    //        checkedTime * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, checkedTime - 2)) - 1))
        //    //    {
        //    //        jump = 100;
        //    //        MessageBox.Show("optTimeUtil:" + optTimeUtil.ToString() + "," + (checkedTime * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, checkedTime - 2)) - 1)).ToString());
        //    //    }
        //    //}


        //    double e_escape;

        //    if (l_escape == 1)
        //        e_escape = p_c;
        //    else
        //        e_escape = (1 - (1 - p_c) * (1 - p_c) * Math.Pow((1 - p_a), l_escape - 2)) * (1.0 / l_escape);

        //    double eta_min = eta;
        //    lmbrtArg = -(p_a * p_p + p_a - 1) / (Math.E * (p_a - 1));

        //    if (lmbrtArg > -0.99999 / Math.E) // lambert isn't defined outside this range
        //        eta_min = (1 + AdvancedMath.LambertW(lmbrtArg)) / (Math.Log(1 - p_a, Math.E));

        //    double e_stay;
        //    double e_stay_1 = (1 - (-1 + p_a) * (Math.Pow(1 - p_a, 1) - 1) / p_a + p_p) * (1.0 / 1);
        //    double e_stay_eta = (eta - (-1 + p_a) * (Math.Pow(1 - p_a, eta) - 1) / p_a + p_p) * (1.0 / eta);

        //    eta_min = Math.Max(1, eta_min); // can't be less than 1
        //    double e_stay_eta_min = (eta_min - (-1 + p_a) * (Math.Pow(1 - p_a, eta_min) - 1) / p_a + p_p) * (1.0 / eta_min);
        //    e_stay = Math.Min(Math.Min(e_stay_1, e_stay_eta), e_stay_eta_min);

        //    if (e_escape == 0 || e_stay == 0)
        //        return double.NegativeInfinity;

        //    if (CanEvadersTransmit)
        //    {
        //        if (e_stay < e_escape)
        //            c.didEscape = false;
        //        else
        //            c.didEscape = true;

        //        return -(eta / Math.Min(e_stay, e_escape));
        //    }

        //    c.didEscape = true;
        //    return -eta / e_escape;
        //}


        // fixme remove below debug:
        //float roundsCount = 0;
        //float detectedTransmission = 0;
        //float startedPursuit = 0;
        //float capturedInPursuit = 0;
        //List<Point> prevPursuitArea;
        //Dictionary<Point, int> visitedPursuits = new Dictionary<Point, int>();
        //List<Point> prevOC = new List<Point>();
        //Dictionary<Point, int> visitsPerArea = new Dictionary<Point, int>(); // FIXME remove
        //Dictionary<Point, int> visitsPerCircumference = new Dictionary<Point, int>(); // fixme remove
        //Point prevPursuitTarget;
        // fime remove above debug

        public override void setGameState(int CurrentRound, List<Point> O_c, IEnumerable<Point> O_d)
        {
            this.currentRound = CurrentRound;

            if (pursuitPursuers.Count() > 0)
            {
                pursuitArea = new List<Point>();
                // TODO multiple pursuits in different cells?
                if (O_c.Count > 0)
                {
                    List<Point> possTargets = O_c.Where(key => g.getMinDistance(key, g.getNodesByType(NodeType.Target)[0]) <= gm.r_e).ToList();
                    if (possTargets.Count > 0)
                    {
                        int r = EvolutionUtils.threadSafeRand.Next(0, possTargets.Count);
                        pursuitTarget = possTargets[r];
                        g.getNodesWithinDistance(pursuitTarget, 1).ForEach((key) =>
                        {
                            // @FIXED PURSUIT
                            if (key.manDist(targets.First()) <= gm.r_e)
                                pursuitArea.Add(key);
                        });
                    }
                }

                // fixme remove below
                //if (O_c.Count > 0)
                //{
                //    ++detectedTransmission;
                //    ++startedPursuit;
                //}
                //++roundsCount;
                //if (prevPursuitArea!= null && O_d.Count() > 0 && prevPursuitArea.Count > 0)
                //{
                //    foreach (Point p in O_d)
                //        //if (prevPursuitArea.Contains(p))
                //        if(prevPursuitTarget == p)
                //        {
                //            ++capturedInPursuit;
                //            break;
                //        }
                //        else
                //        {
                //            capturedInPursuit = capturedInPursuit;
                //        }
                    
                //}
                //prevOC = O_c;
                //prevPursuitTarget = pursuitTarget;
                //prevPursuitArea = new List<Point>(pursuitArea);
                
                // fixme rmove above


                if (pursuitArea.Count > 0)
                {
                    Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();
                    markedLocations.Add("Evader spread", pursuitArea);
                    ginput.markLocations(markedLocations.toPointFMarkings());
                }
            }
        }

        public struct PatrolAlg
        {
            public APatrol alg;
            public double p_a;
        }
        public static PatrolAlg getBestPatrolType(int r_p, int d, int areaPursuers)
        {
            if(r_p == 0)
                return new PatrolAlg(){alg = null, p_a = 0};

            double maxPA = 0;
            APatrol bestType = null;

            foreach(var p in APatrol.ChildrenByTypename)
            {
                if(p.Value.minimalPursuersCount(r_p,d) <= areaPursuers)
                {
                    double tmpPA = p.Value.getCaptureProbability(r_p, d, areaPursuers);
                    if(tmpPA > maxPA)
                    {
                        maxPA = tmpPA;
                        bestType = p.Value;
                    }
                }
            }

            return new PatrolAlg() { alg = bestType, p_a = maxPA };
        }

        public static double calculatePA(int r_p, int d, int areaPursuers)
        {
            return getBestPatrolType(r_p, d, areaPursuers).p_a;
            //if (r_p == 0)
            //    return 0;
            
            //if (areaPursuers >= new DenseGridPatrol().minimalPursuersCount(r_p, d))
            //    return new DenseGridPatrol().getCaptureProbability(r_p, d, areaPursuers);
            //else
            //    return new SwitchPatrol().getCaptureProbability(r_p, d, areaPursuers);
        }
        public static double calculatePC(int circumferencePursuers, int r_p, int d)
        {
            if (r_p == 0)
                return 0;
            return new CircumferencePatrol().getCaptureProbability(r_p, d, circumferencePursuers);
        }
        public static double calculatePP(int pursuitPursuers, int r_p, int d)
        {
            return Pursuit.getCaptureProbability(d, r_p, pursuitPursuers);
        }

        /// <summary>
        /// evaders try to minimize this value
        /// </summary>
        public static double e_stay_etaTag(double p_a, double p_p, double p_d, double saToSinksRoundCount, double discountFactor, double etaTag)
        {
            
            double discountX = Math.Pow(discountFactor, etaTag);
            double discountSink = Math.Pow(discountFactor, saToSinksRoundCount);
            //double totalreward = discountSink * ((discountX - 1) / (discountFactor - 1));
            
            //double averageRewardPerUnit = totalreward / etaTag;
            //double totalRisk = (etaTag - ((-1 + p_a) * (Math.Pow(1 - p_a, etaTag) - 1)) / p_a + p_p);
            //double averageRisk = totalRisk / etaTag;
            //return averageRisk * averageRewardPerUnit; 

            //double term1 = (etaTag - ((-1 + p_a) * (Math.Pow(1 - p_a, etaTag) - 1)) / p_a + p_p);
            //double term2 = 1.0 / (etaTag * discountSink * (discountX - 1) / (discountFactor - 1));
            //return term1 * term2;

            double term1;

            if (p_a == 0)
                term1 = p_p * (1 - Math.Pow(1 - p_d, etaTag));
            else
                term1 = (etaTag - ((-1 + p_a) * (Math.Pow(1 - p_a, etaTag) - 1)) / p_a + (p_p * (1 - Math.Pow(1 - p_d, etaTag))) );
            
            double term2 = 1.0 / (discountSink * (discountX - 1) / (discountFactor - 1));
            return term1 * term2;

        }
        public static double optimalEtaTagDiff(double p_a, double p_p, double p_d, double saToSinksRoundCount, double discountFactor, double x)
        {
            // without p_d :
            if (p_d > 0.99999)
            {
                double nopdlnDiscount = Math.Log(discountFactor, Math.E);
                double nopddiscountX = Math.Pow(discountFactor, x);
                double nopddiscountSink = Math.Pow(discountFactor, saToSinksRoundCount);
                double nopddiscountDiv = discountFactor - 1;
                double nopdtermdiv = nopddiscountSink * (nopddiscountX - 1);
                double nopdterm1 = 1.0 - ((p_a - 1) * Math.Pow((1 - p_a), x) * Math.Log(1 - p_a, Math.E)) / p_a;
                double nopdterm2 = (x + p_p - ((p_a - 1) * (Math.Pow((1 - p_a), x) - 1)) / p_a) * nopddiscountDiv * nopddiscountX * nopdlnDiscount;
                return nopdterm1 * nopddiscountDiv / nopdtermdiv - nopdterm2 / (nopdtermdiv * (nopddiscountX - 1));
            }

            // note - the equation below has Math.Log(1 - p_d, Math.E), so p_d = 1 is undefined
            double lnDiscount = Math.Log(discountFactor, Math.E);
            double discountX = Math.Pow(discountFactor, x);
            double discountSink = Math.Pow(discountFactor, saToSinksRoundCount);
            double discountDiv = discountFactor - 1;
            double termdiv = discountSink * (discountX - 1);
            double term1 = 1.0 - ((p_a - 1) * Math.Pow((1 - p_a), x) * Math.Log(1 - p_a, Math.E)) / p_a + p_p * Math.Pow(1 - p_d, x) * Math.Log(1 - p_d, Math.E);
            double term2 = (x + p_p * (1 - Math.Pow(1 - p_d, x)) - ((p_a - 1) * (Math.Pow((1 - p_a), x) - 1)) / p_a) * discountDiv * discountX * lnDiscount;
            return term1 * discountDiv / termdiv - term2 / (termdiv * (discountX - 1));
        }


        /// <summary>
        /// similarly to calculatel_escape() , if saToSinksRoundCount is 0 and we decide optSimultennousTransmissionCount=1 , then there's actually no discount
        /// </summary>
        /// <param name="eta"></param>
        /// <param name="p_a"></param>
        /// <param name="p_p"></param>
        /// <param name="saToSinksRoundCount"></param>
        /// <param name="discountFactor"></param>
        /// <param name="optSimultennousTransmissionCount"></param>
        /// <returns></returns>
        public static double calculateE_Stay(int eta, double p_a, double p_p, double p_d, double saToSinksRoundCount, double discountFactor, out int optSimultennousTransmissionCount)
        {
            if(p_a == 0 && p_d > 0.99999)
            {
                optSimultennousTransmissionCount = eta;
                return e_stay_etaTag(p_a, p_p,p_d, saToSinksRoundCount, discountFactor, eta);
            }

            GoE.Utils.RootFinding.FunctionOfOneVariable optimalEtaTagDiffCaller  =
                (double x) =>
                {
                    return optimalEtaTagDiff(p_a, p_p,p_d, saToSinksRoundCount, discountFactor, x);
                };

            OptimizedObj<double> best_eta = new OptimizedObj<double>() { value = double.MaxValue};

            OptimizedObj<double> eta_stay_1 =
                new OptimizedObj<double>() { value = e_stay_etaTag(p_a,p_p,p_d,saToSinksRoundCount,discountFactor,1), data = 1 };

            OptimizedObj<double> eta_stay_eta =
                new OptimizedObj<double>() { value = e_stay_etaTag(p_a, p_p,p_d, saToSinksRoundCount, discountFactor, eta), data = eta };

            if (optimalEtaTagDiffCaller(eta_stay_1.data) * optimalEtaTagDiffCaller(eta_stay_eta.data) < 0) // optimal value is in range
            {
                double extremePoint = GoE.Utils.RootFinding.NumericMethods.Brent(optimalEtaTagDiffCaller, 1, eta, 1e-7);
                double extremePointCeil = Math.Ceiling(extremePoint);
                double extremePointFloor = Math.Floor(extremePoint);
                best_eta.setIfValueDecreases(extremePointCeil, e_stay_etaTag(p_a, p_p,p_d, saToSinksRoundCount, discountFactor, extremePointCeil));
                best_eta.setIfValueDecreases(extremePointFloor, e_stay_etaTag(p_a, p_p,p_d, saToSinksRoundCount, discountFactor, extremePointFloor));


                //double test0 = e_stay_etaTag(p_a, p_p, saToSinksRoundCount, discountFactor, extremePointFloor);
                //double test1 = e_stay_etaTag(p_a, p_p, saToSinksRoundCount, discountFactor, extremePointFloor + 1);
                //double test2 = e_stay_etaTag(p_a, p_p, saToSinksRoundCount, discountFactor, extremePointFloor - 1);

                //double test02 = e_stay_etaTag(p_a, p_p, (int)extremePointFloor);
                //double test12 = e_stay_etaTag(p_a, p_p, (int)extremePointFloor + 1);
                //double test22 = e_stay_etaTag(p_a, p_p, (int)extremePointFloor - 1);
                //int a=0;
                //a++;
            }
            best_eta.setIfValueDecreases(eta_stay_1);
            best_eta.setIfValueDecreases(eta_stay_eta);

            optSimultennousTransmissionCount = (int)best_eta.data;
            return best_eta.value;
        }

        private static double e_stay_etaTag(double p_a, double p_p, double p_d, int etaTag)
        {
            return ((double)etaTag - (-1 + p_a) * (Math.Pow(1 - p_a, (double)etaTag) - 1) / p_a + p_p *(1-Math.Pow(1-p_d,etaTag)) ) * (1.0 / (double)etaTag);
        }
        public static double calculateE_Stay(int eta, double p_a,  double p_p, double p_d, out int optSimultennousTransmissionCount)
        {

            

            if (p_a == 0)
            {
                optSimultennousTransmissionCount = eta;
                return p_p/eta;
            }
            if(p_p == 0)
            {
                optSimultennousTransmissionCount = 1;
                return p_a;
            }

            if (p_d < 0.9999)
            {
                throw new Exception("calculateE_Stay() doesn't support p_d<1");
            }
            double eta_min = eta;
            double lmbrtArg = -(p_a * p_p + p_a - 1) / (Math.E * (p_a - 1));
            if (lmbrtArg > -0.99999 / Math.E) // lambert isn't defined outside this range
                eta_min = (1 + AdvancedMath.LambertW(lmbrtArg)) / (Math.Log(1 - p_a, Math.E));

            if (eta_min < 0) // even though maple's output didn't mention we need absolute value, I found out that this is in fact the case
                eta_min *= -1;

            OptimizedObj<double> bestEtaTag = new OptimizedObj<double>() { value = double.MaxValue };
            OptimizedObj<double> etaStayMinCeil =
                new OptimizedObj<double>() { data = Math.Ceiling(eta_min), value = (float)e_stay_etaTag(p_a, p_p,p_d, (int)Math.Ceiling(eta_min)) };
            OptimizedObj<double> etaStayMinFloor =
                new OptimizedObj<double>() { data = Math.Floor(eta_min), value = (float)e_stay_etaTag(p_a, p_p,p_d, (int)Math.Floor(eta_min)) };

            OptimizedObj<double> etaStay1 =
                new OptimizedObj<double>() { data = 1, value = (float)e_stay_etaTag(p_a, p_p,p_d, 1) };
            OptimizedObj<double> etaStayeta =
                new OptimizedObj<double>() { data = eta, value = (float)e_stay_etaTag(p_a, p_p,p_d, eta) };

            bestEtaTag.setIfValueDecreases(etaStayMinCeil);
            bestEtaTag.setIfValueDecreases(etaStayMinFloor);
            bestEtaTag.setIfValueDecreases(etaStay1);
            bestEtaTag.setIfValueDecreases(etaStayeta);


            //double e_stay;
            //double e_stay_1 = (1 - (-1 + p_a) * (Math.Pow(1 - p_a, 1) - 1) / p_a + p_p) * (1.0 / 1);
            //double e_stay_eta = (eta - (-1 + p_a) * (Math.Pow(1 - p_a, eta) - 1) / p_a + p_p) * (1.0 / eta);

            //eta_min = Math.Max(1, eta_min); // can't be less than 1
            //double e_stay_eta_min = (eta_min - (-1 + p_a) * (Math.Pow(1 - p_a, eta_min) - 1) / p_a + p_p) * (1.0 / eta_min);
            //e_stay = Math.Min(Math.Min(e_stay_1, e_stay_eta), e_stay_eta_min);
            
            //if (e_stay == e_stay_1)
            //    optSimultennousTransmissionCount = 1;
            //else if (e_stay == e_stay_eta)
            //    optSimultennousTransmissionCount = eta;
            //else
            //    optSimultennousTransmissionCount = (int)eta_min;

            optSimultennousTransmissionCount = (int)bestEtaTag.data;
            return bestEtaTag.value;
        }

        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static int calculateIntegerL_Escape(double p_a, double p_c)
        {
            double l_escape = calculatel_escape(p_a, p_c);
            int l_escapeCeil = (int)Math.Ceiling(l_escape);
            int l_escapeFloor = (int)Math.Floor(l_escape);

            if (PatrolAndPursuit.calculateE_Escape(p_a, p_c, l_escapeCeil) <
                PatrolAndPursuit.calculateE_Escape(p_a, p_c, l_escapeFloor))
            {
                return l_escapeCeil;
            }
            
            return l_escapeFloor;
        }

        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double calculatel_escape(double p_a, double p_c)
        {
            double lmbrtArg = -(p_c * p_c - 2 * p_c + 1) / (Math.E * Math.Pow(-1 + p_a, 2));
            double extremePoint = 3;
            
            if (lmbrtArg > -0.99999 / Math.E) // lambert isn't defined outside this range
            {
                extremePoint = -(AdvancedMath.LambertW(lmbrtArg) + 1) / (Math.Log(1 - p_a, Math.E));
                extremePoint = Math.Max(3, extremePoint);
            }

            double l_escape = 1;
            double optTimeUtil = (1.0 / p_c) - 1;

            if (optTimeUtil < util_l(p_a, p_c, extremePoint))
            {
                l_escape = extremePoint;
                optTimeUtil = util_l(p_a, p_c, extremePoint); //extremePoint * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, extremePoint - 2)) - 1);
            }
            if (optTimeUtil < util_l(p_a, p_c, 3) )//3 * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, 3 - 2)) - 1))
            {
                optTimeUtil = util_l(p_a, p_c, 3);//3 * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, 3 - 2)) - 1);
                l_escape = 3;
            }

            return l_escape;
        }


        // serves calculatel_escape()
        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double optimalLEscapeDiff(double p_a, double p_c, double discountFactor, double saToSinksRoundCount, double x)
        {
            double discountX = Math.Pow(discountFactor, x);
            double discountSink = Math.Pow(discountFactor, saToSinksRoundCount - 1);

            double captureProb = Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, x - 2);

            double term1 = captureProb * Math.Log(1.0 - p_a, Math.E) * discountSink * (discountX - 1);
            double term2 = Math.Pow((1 - captureProb), 2) * (discountFactor - 1);
            double term3 = 1.0 / (1 - captureProb) - 1;
            double term4 = discountSink * discountX * Math.Log(discountFactor, Math.E);

            return (term1 / term2) + (term3 * term4) / (discountFactor - 1);
        }

        /// <summary>
        /// assumes p_c is the risk of entering the circumference i.e. gameParams.EvaderCircumferenceEntranceKillProb + probability of being captued by a pursuer
        /// </summary>
        /// <param name="p_a"></param>
        /// <param name="p_c"></param>
        /// <param name="discountFactor"></param>
        /// <param name="saToSinksRoundCount">
        /// tells how more rounds will pass from the point the evader leaves the sensitive area, and when it can finally transmit
        /// (we assume the evader transmits as it enters the sink, and if saToSinksDistance = 0, this means that the sink is actually adjacent to the SA. 
        /// Note that if l_escape=1, in this case there is no discount)
        /// </param>
        /// <returns></returns>
        public static double calculatel_escape(double p_a, double p_c, double discountFactor, double saToSinksRoundCount)
        {

            p_c = Math.Max(p_a, p_c);

            if (p_a == 0 && discountFactor < 1)
            {
                int infiniteLEscape = 256; // technichally the optimal value is infinite, but we don't want to kill the simulation
                // either 1 or infinite
                if (util_l(p_a, p_c, 1, discountFactor, saToSinksRoundCount) >=
                    util_l(p_a, p_c, infiniteLEscape, discountFactor, saToSinksRoundCount))
                {
                    return 1;
                }
                else
                    return infiniteLEscape; 
            }
            
            
            GoE.Utils.RootFinding.FunctionOfOneVariable optimalLEscapeDiffCaller = 
                (double x)=>
                {
                    return optimalLEscapeDiff(p_a, p_c, discountFactor,saToSinksRoundCount, x);
                };

            OptimizedObj<double> best_l_escape = new OptimizedObj<double>() { value = 0 };

            OptimizedObj<double> l_escape_1 =
                new OptimizedObj<double>() { data = 1, value = util_l(p_a, p_c, 1, discountFactor, saToSinksRoundCount) };

            OptimizedObj<double> l_escape_3 =
                new OptimizedObj<double>() { data = 3, value = util_l(p_a, p_c, 3, discountFactor, saToSinksRoundCount) };

            double MAX_L_ESCAPE = 2000; // we never reached a higher value than that, and in most cases the improvement gained by going beyond that value is negligable anyway

            OptimizedObj<double> l_escape_max =
                new OptimizedObj<double>() { data = MAX_L_ESCAPE, value = util_l(p_a, p_c, MAX_L_ESCAPE, discountFactor, saToSinksRoundCount) };

            if (optimalLEscapeDiffCaller(l_escape_3.data) * optimalLEscapeDiffCaller(l_escape_max.data) < 0) // if optimal value is in range
            {
                double extremePoint = GoE.Utils.RootFinding.NumericMethods.Brent(optimalLEscapeDiffCaller, 3, MAX_L_ESCAPE);
                double extremePointCeil = Math.Ceiling(extremePoint);
                double extremePointFloor = Math.Floor(extremePoint);
                best_l_escape.setIfValueIncreases(extremePointCeil, (float)util_l(p_a, p_c, extremePointCeil, discountFactor, saToSinksRoundCount));
                best_l_escape.setIfValueIncreases(extremePointFloor, (float)util_l(p_a, p_c, extremePointFloor, discountFactor, saToSinksRoundCount));
            }
            best_l_escape.setIfValueIncreases(l_escape_1);
            best_l_escape.setIfValueIncreases(l_escape_3);
            best_l_escape.setIfValueIncreases(l_escape_max);

            return best_l_escape.data;
        }

        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double util_l(double p_a, double p_c, double l)
        {
            if (l == 1)
                return (1.0 / p_c) - 1;
            return l * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, l - 2)) - 1);
        }

        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double util_l(double p_a, double p_c, double l, double discountFactor, double saToSinksRoundCount)
        {
            if (l == 1)
                return ((1.0 / Math.Max(p_a, p_c)) - 1) * Math.Pow(discountFactor, l + saToSinksRoundCount - 1);
            //return (l * (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, l - 2)) - 1)) * Math.Pow(discountFactor, l + saToSinksRoundCount - 1);

            double discountX = Math.Pow(discountFactor, l);
            double discountSink = Math.Pow(discountFactor, saToSinksRoundCount);
            return (1.0 / (1 - Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, l - 2)) - 1) * discountSink * (discountX - 1) / (discountFactor - 1);
        }
        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double calculateE_Escape(double p_a, double p_c, double l)
        {
            return 1.0 / util_l(p_a, p_c, l);
        }
        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double calculateE_Escape(double p_a, double p_c, double l, double discountFactor, double saToSinksRoundCount)
        {
            return 1.0 / util_l(p_a, Math.Max(p_a,p_c), l, discountFactor, saToSinksRoundCount);
        }
        // assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        public static double calculateE_Escape(double p_a, double p_c)
        {
            if (p_c == 0)
                return 1.0/(1.0/p_a-1); // we assume area patrol works also on circumference

            double l_escape = calculatel_escape(p_a,p_c);
            //double e_escape;

            //double repetitions = (util_l(p_a, p_c, l_escape) / l_escape);
            //return repetitions * Math.Pow(1 - p_c, 2) * Math.Pow(1 - p_a, l_escape - 2);
            // FIXME what to do here?

            return calculateE_Escape(p_a, p_c, l_escape);
            //return 1.0/util_l(p_a, p_c, l_escape);
            //if (l_escape == 1)
            //    e_escape = p_c;
            //else
            //    e_escape = (1 - (1 - p_c) * (1 - p_c) * Math.Pow((1 - p_a), l_escape - 2)) * (1.0 / l_escape);

            //return e_escape;
        }

        /// <summary>
        /// if sinks and SA are adjacent, set roundsBeforeReachingSink to 0
        /// assumes p_c includes + gameParams.EvaderCircumferenceEntranceKillProb 
        /// </summary>
        /// <param name="p_a"></param>
        /// <param name="p_c"></param>
        /// <param name="discountFactor"></param>
        /// <param name="roundsBeforeReachingSink"></param>
        /// <returns></returns>
        public static double calculateE_Escape(double p_a, double p_c, double discountFactor, int roundsBeforeReachingSink)
        {
            double l_escape;
            if (p_a == 0 && discountFactor < 1)
            {
                // evader should stay for "infinite" amount of time inside, and get reward according to geometrical sum
                double reward = 1.0 / (1.0 - discountFactor);
                return reward * (1.0 / (1 - Math.Pow(1 - p_c, 2)) - 1);
            }

            if (p_c == 0)
            {
                l_escape = 1; // we assume area patrol works also on circumference
                p_c = p_a;
            }
            else
                l_escape = calculatel_escape(p_a, p_c, discountFactor, roundsBeforeReachingSink);
            
            return calculateE_Escape(p_a, p_c, l_escape,discountFactor,roundsBeforeReachingSink);
        }




        private int pursuitK;
        private List<Point> targets;
        private List<Pursuer> unusedPursuers = new List<Pursuer>();
        private Location unusedPursuersLocation = new Location();

        public override Dictionary<Pursuer, Location> getNextStep()
        {
            if (currentRound == 0)
            {
                
                int areaPursuerRange = 0, circPursuerRange = 0, pursuitpursuerRange = 0;
                int areapursuersPerTarget = areaPatrollers.Count() / targets.Count;
                int cicpursuersPerTarget = circumferencePursuers.Count() / targets.Count;
                int pursuitpursuersPerTarget = pursuitPursuers.Count() / targets.Count;
                pursuitK = Pursuit.getK(gm.r_e-1,// @FIXED PURSUIT
                    gm.r_p, pursuitpursuersPerTarget);
                    //pursuitpursuersPerTarget / GoE.GameLogic.Algorithms.Pursuit.getMinimalPursuersCount(gm.r_e + 1, gm.r_p);
                foreach (Point t in targets)
                {
                    try
                    {
                        //firstPursuer =
                        //    GoE.GameLogic.Algorithms.UniformAreaPatrol.InitUniformAreaPatrol(g, gm.r_p,
                        //        new Utils.ListRangeEnumerable<Pursuer>(areaPatrollers, areaPursuerRange, areaPursuerRange + areapursuersPerTarget),
                        //        prevLocation,
                        //        new Location(t), gm.r_e - 1);

                        areaPatrolRad = gm.r_e - 1;
                        //areaPatrolRad = gm.r_e - 6; // FIXME remove NOW
                        
                        if (cicpursuersPerTarget == 0)
                            areaPatrolRad = gm.r_e; // we can never leave the circumference empty

                        if (areapursuersPerTarget > 0)
                        {
                            areaPatrol = getBestPatrolType(patrolRP, areaPatrolRad, areapursuersPerTarget).alg.generateNew();
                            //if (areapursuersPerTarget < new DenseGridPatrol().minimalPursuersCount(patrolRP, areaPatrolRad))
                            //{
                            //    areaPatrol = new SwitchPatrol();

                            //}
                            //else
                            //{
                            //    areaPatrol = new DenseGridPatrol(); // preferrable, if it's possible
                                
                            //}
                            //p_a = calculatePA(patrolRP, areaPatrolRad, areapursuersPerTarget);

                            areaPatrol.Init(g,
                                    patrolRP,
                                    new Utils.ListRangeEnumerable<Pursuer>(areaPatrollers, areaPursuerRange, areaPursuerRange + areapursuersPerTarget),
                                    prevLocation,
                                    new Location(t),
                                    areaPatrolRad);

                            areaPursuerRange += areapursuersPerTarget;
                        }
                    }
                    catch (Exception ex)
                    {
                        // if area patrol can't init (probably because not enough pursuers), solution 2 still 
                        // works with p_a = 0
                    }

                    if (cicpursuersPerTarget > 0) // actually for the solution p_c>0 is necessary, but for debugging we sometimes want to disable circumference pursuers
                    {
                        circumferencePatrol = new GameLogic.Algorithms.CircumferencePatrol();
                        circumferencePatrol.Init(g, circumferenceRP,
                            new Utils.ListRangeEnumerable<Pursuer>(circumferencePursuers, circPursuerRange, circPursuerRange + cicpursuersPerTarget),
                            prevLocation,
                            new Location(t), gm.r_e);
                        circPursuerRange += cicpursuersPerTarget;

                        //p_c = CircumferencePatrol.getCaptureProbability(circumferenceRP, gm.r_e, cicpursuersPerTarget);

                    }
                    if (pursuitpursuersPerTarget > 0 && !noPursuit)
                    {
                        
                        GoE.GameLogic.Algorithms.Pursuit.InitImmediatePursuit(g, 
                            gm.r_p,
                            pursuitK,
                            new Utils.ListRangeEnumerable<Pursuer>(pursuitPursuers, 
                                                                    pursuitpursuerRange,
                                                                    pursuitpursuerRange + pursuitpursuersPerTarget),
                            prevLocation,
                            new Location(t), gm.r_e-1); // @FIXED PURSUIT
                        
                        //pursuitArea = g.getNodesWithinDistance(t, gm.r_e + 1);
                        initLocation = new Dictionary<Pursuer, Location>();
                        initLocation = new Dictionary<Pursuer,Location>(prevLocation);

                        pursuitpursuerRange += pursuitpursuersPerTarget;
                        //p_p = Pursuit.getCaptureProbability(gm.r_e, gm.r_p, pursuitpursuersPerTarget);
                    }
                    

                    //double e_escape = 0;
                    //double e_stay = 0;
                    //int optSimultennousTransmissionCount;
                    //e_escape = calculateE_Escape(p_a, p_c);
                    //e_stay = calculateE_Stay(gm.A_E.Count, p_a, p_p, out optSimultennousTransmissionCount);
                    //pgui.ShowDialog(new string[6]{"pa","pc","pp","e_escape","e_stay","opt Simultennous Transmission#"},"theoretical bounds",
                    //new string[6] { p_a.ToString(), p_c.ToString(), p_p.ToString(), e_escape.ToString(), e_stay.ToString(), optSimultennousTransmissionCount.ToString()});
                
                } // for each target
                // if pursuers count doesn't divide target count exactly, remainging pursuers remain unused:
                var remainingCircumferencePursuers =
                    new Utils.ListRangeEnumerable<Pursuer>(circumferencePursuers, circPursuerRange, circumferencePursuers.Count() - 1);
                var remainingAreaPursuers =
                    new Utils.ListRangeEnumerable<Pursuer>(areaPatrollers, areaPursuerRange, areaPatrollers.Count() - 1);
                var remainingPursuitPursuers =
                    new Utils.ListRangeEnumerable<Pursuer>(pursuitPursuers, pursuitpursuerRange, pursuitPursuers.Count() - 1);

               

                if (noPursuit)
                {
                    // even if we don't use pursuit, by default we designate extra pursuers for pursuit, 
                    // so these pursuers should be initialized to SOME point
                    foreach (Pursuer p in pursuitPursuers)
                        unusedPursuers.Add(p); 
                }

                foreach (Pursuer p in remainingAreaPursuers)
                    unusedPursuers.Add(p);
                foreach (Pursuer p in remainingCircumferencePursuers)
                    unusedPursuers.Add(p);
                foreach (Pursuer p in remainingPursuitPursuers)
                    unusedPursuers.Add(p);

                this.unusedPursuersLocation = new Location(targets.FirstOrDefault());
            }
            else // of if (currentround==0)
            {

                
                int areaPursuerRange;
                int circPursuerRange;
                int pursuitpursuerRange;
                int areapursuersPerTarget;
                int cicpursuersPerTarget;
                int pursuitpursuersPerTarget;

                areaPursuerRange = 0;
                circPursuerRange = 0;
                pursuitpursuerRange = 0;
                areapursuersPerTarget = areaPatrollers.Count() / targets.Count;
                cicpursuersPerTarget = circumferencePursuers.Count() / targets.Count;
                pursuitpursuersPerTarget = pursuitPursuers.Count() / targets.Count;
                //int pursuitK =
                //  pursuitpursuersPerTarget / GoE.GameLogic.Algorithms.Pursuit.getMinimalPursuersCount(gm.r_e + 1, gm.r_p);

                foreach (Point t in targets)
                {

                    //if (firstPursuer != null)
                    //{
                    //    GameLogic.Algorithms.UniformAreaPatrol.AdvanceUniformAreaPatrolPursuers(g, gm.r_p,
                    //        new Utils.ListRangeEnumerable<Pursuer>(areaPatrollers, pursuerRange, pursuerRange + areapursuersPerTarget),
                    //        prevLocation,
                    //        new Location(t),
                    //        gm.r_e - 1,
                    //        firstPursuer);
                    //    pursuerRange += areapursuersPerTarget;
                    //}

                    if (areapursuersPerTarget > 0)
                    {
                        areaPatrol.advancePursuers(g,
                            patrolRP,
                            new Utils.ListRangeEnumerable<Pursuer>(areaPatrollers, areaPursuerRange, areaPursuerRange + areapursuersPerTarget),
                            prevLocation,
                            new Location(t),
                            areaPatrolRad);
                        areaPursuerRange += areapursuersPerTarget;
                    }

                    if (cicpursuersPerTarget > 0)
                    {
                        circumferencePatrol.AdvancePursuers(g, circumferenceRP,
                            new Utils.ListRangeEnumerable<Pursuer>(circumferencePursuers, circPursuerRange, circPursuerRange + cicpursuersPerTarget),
                            prevLocation,
                            new Location(t), gm.r_e);
                        circPursuerRange += cicpursuersPerTarget;
                    }
                    if (pursuitpursuersPerTarget > 0 && !noPursuit)
                    {



                        GoE.GameLogic.Algorithms.Pursuit.AdvanceImmediatePursuit(g, gm.r_p,
                            pursuitK,
                            new Utils.ListRangeEnumerable<Pursuer>(pursuitPursuers,
                                                                    pursuitpursuerRange,
                                                                    pursuitpursuerRange + pursuitpursuersPerTarget),
                            prevLocation,
                            initLocation,
                            pursuitTarget, pursuitArea);
                        pursuitpursuerRange += pursuitpursuersPerTarget;


                        // fixme remove below debug
                        //if (pursuitArea.Count < prevPursuitArea.Count)
                        //    foreach (Point p in prevPursuitArea)
                        //        if (!pursuitArea.Contains(p))
                        //        {
                        //            if (!visitedPursuits.ContainsKey(p.subtruct(pursuitTarget)))
                        //                visitedPursuits[p.subtruct(pursuitTarget)] = 1;
                        //            else
                        //                ++visitedPursuits[p.subtruct(pursuitTarget)];
                        //        }
                        //if(pursuitArea.Count > 0)
                        //{
                        //    bool found = false;
                        //    foreach(var l in prevLocation)
                        //    {
                        //        if(l.Value.nodeLocation == pursuitTarget)
                        //        {
                        //            found = true;
                        //            break;
                        //        }
                        //    }
                        //    found = found;
                        //}
                        // fixme remove above debug

                    }
                }

                foreach (Pursuer p in unusedPursuers)
                    prevLocation[p] = unusedPursuersLocation;
            }

            //if(ginput.hasBoardGUI()) // fixme remove
            //{
            //    try
            //    {
            //        if (CircumferencePatrolCaptureProbability == 0 && PursuitCaptureProbability == 0)
            //        {
            //            foreach (Pursuer p in areaPatrollers)
            //            {
            //                //if(g.getMinDistance(prevLocation[p].nodeLocation,targets.First()) < gm.r_e)
            //                {
            //                    if (visitsPerArea.ContainsKey(prevLocation[p].nodeLocation))
            //                        ++visitsPerArea[prevLocation[p].nodeLocation];
            //                    else
            //                        visitsPerArea[prevLocation[p].nodeLocation] = 1;
            //                }
            //            }
            //            foreach (Pursuer p in circumferencePursuers)
            //            {
            //                if (g.getMinDistance(prevLocation[p].nodeLocation, targets.First()) == gm.r_e)
            //                {
            //                    if (visitsPerCircumference.ContainsKey(prevLocation[p].nodeLocation))
            //                        ++visitsPerCircumference[prevLocation[p].nodeLocation];
            //                    else
            //                        visitsPerCircumference[prevLocation[p].nodeLocation] = 1;
            //                }
            //            }
            //        }
            //        else
            //        {
            //            foreach (Pursuer p in areaPatrollers)
            //            {
            //                if (g.getMinDistance(prevLocation[p].nodeLocation, targets.First()) <= gm.r_e)
            //                {
            //                    if (visitsPerArea.ContainsKey(prevLocation[p].nodeLocation))
            //                        ++visitsPerArea[prevLocation[p].nodeLocation];
            //                    else
            //                        visitsPerArea[prevLocation[p].nodeLocation] = 1;
            //                }
            //            }
            //        }

            //        if (currentRound % 100 == 0)
            //        {

            //            List<Point> allArea = new List<Point>(); // fixme remove
            //            List<Point> areabelowHalf = new List<Point>(); // fixme remove
            //            List<Point> areaaboveoneAndHalf = new List<Point>(); // fixme remove
            //            List<Point> circbelowHalf = new List<Point>(); // fixme remove
            //            List<Point> circboveoneAndHalf = new List<Point>(); // fixme remove
            //            double areaTotal = 0;

            //            foreach (var v in visitsPerArea)
            //            {
            //                areaTotal += v.Value;
            //            }
            //            areaTotal /= visitsPerArea.Count();
            //            foreach (var v in visitsPerArea)
            //            {

            //                if (v.Value > areaTotal * 1.1)
            //                    areaaboveoneAndHalf.Add(v.Key);
            //                else if (v.Value < areaTotal * 0.9)
            //                    areabelowHalf.Add(v.Key);

            //                allArea.Add(v.Key);
            //            }


            //            double circumTotal = 0;

            //            foreach (var v in visitsPerCircumference)
            //            {
            //                circumTotal += v.Value;
            //            }
            //            circumTotal /= visitsPerCircumference.Count();


            //            foreach (var v in visitsPerCircumference)
            //            {
            //                //double freq = ((float)(v.Value) / currentRound);
            //                //if (v.Value > circumTotal * 1.1)
            //                //    circboveoneAndHalf.Add(v.Key);
            //                //else if (v.Value < circumTotal * 0.9)
            //                //    circbelowHalf.Add(v.Key);
            //                if (v.Value > currentRound * CircumferencePatrolCaptureProbability * 1.1)
            //                    circboveoneAndHalf.Add(v.Key);
            //                else if (v.Value < currentRound * CircumferencePatrolCaptureProbability * 0.9)
            //                    circbelowHalf.Add(v.Key);
            //            }

            //            performanceMarks["area all"] = allArea;
            //            performanceMarks["area below 0.9"] = areabelowHalf;
            //            performanceMarks["area above 1.1"] = areaaboveoneAndHalf;
            //            performanceMarks["circ below 0.9"] = circbelowHalf;
            //            performanceMarks["circ above 1.1"] = circboveoneAndHalf;

            //        }
            //        ginput.markLocations(performanceMarks); // fixme remove
            //    }
            //    catch (Exception) { }
            //}

            return prevLocation;
        }

        public override void gameFinished()
        {
            //this.ginput.addLogValue(AppConstants.Policies.PatrolAndPursuit.RESULTED_P_P, (capturedInPursuit / startedPursuit).ToString());
            //this.ginput.addLogValue(AppConstants.Policies.PatrolAndPursuit.RESULTED_P_D, (detectedTransmission / roundsCount).ToString());

            //// fixme remove below
            //foreach (var v in visitedPursuits)
            //{
            //    this.ginput.addLogValue(v.Key.ToString(), v.Value.ToString());
            //}
        }

 

        Dictionary<string, List<Point>> performanceMarks = new Dictionary<string, List<Point>>(); // fixme remove
    }
}
