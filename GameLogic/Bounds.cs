using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge;
using AForge.Genetic;
using GoE.Utils;
using System.IO;
using System.Windows.Forms;
using Meta.Numerics.Functions;
using AForge.Math.Random;
using GoE.UI;
using GoE.Policies; // library for lambert W function is from: http://metanumerics.codeplex.com/SourceControl/latest
namespace GoE.GameLogic
{
    

    /// <summary>
    /// in this class we provide utilities for calculating/estimating the bound on performance
    /// of an algorithm that assumes the graph is a target, surrounded from distance k by sinks , and lets 
    /// the evader crawl in and out
    /// </summary>
    public static class EvaderAlg1Bounds
    {
        /// <summary>
        /// denoted with w(d) in the paper
        /// </summary>
        public static double expectedBlockedRounds(double ringRadius, double psi, double p_ringRadius, double n_ringRadius)
        {
            return p_ringRadius * (psi - 1) * (1 + 1 / (1 - n_ringRadius)) / (psi * 4 * ringRadius);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r_e">
        /// 
        /// </param>
        /// <param name="p_">
        /// p_[i] is p_{r_e+i} in the paper i.e.
        /// tells how many expected pursuers visit Dist_G(r_e+i, t)
        /// </param>
        /// <param name="psi">
        /// default means 2*r_e-1
        /// </param>
        /// <param name="n_">
        /// n_[i] is n_{r_e+i} in the paper i.e. 
        /// the probability of the pursuers revisiting the same point
        /// </param>
        /// <returns></returns>
        public static double getSurvivalProbability(int r_e, List<double> p_, List<double> n_, double psi = -1)
        {
            if(psi == -1)
                psi = 2 * r_e - 1;
            

            if(p_.Count == 1)
                return 1 - psi / (4 * r_e - psi*n_[0]);
            
            double survivalProb = 1;

            double k = r_e + p_.Count - 1;
            double p_k = p_.Last();
            double p_k_m1 = p_[p_.Count-2];
            double n_k_m1 = n_[p_.Count-2];
            double p_r_e = p_.First();
            double p_r_e_p1 = p_[1];
            double n_r_e_p1 = n_[1];

            survivalProb *= Math.Pow(1 - p_k / (4 * k - p_k), expectedBlockedRounds(k - 1, psi, p_k_m1, n_k_m1));
            survivalProb *= Math.Pow(1 - p_r_e / (4 * r_e - p_r_e), expectedBlockedRounds(r_e + 1, psi, p_r_e_p1, n_r_e_p1));
            for(int i = 1; i < k - r_e; ++i)
            {
                survivalProb *= Math.Pow( (1-p_[i]), 
                    expectedBlockedRounds(r_e + i -1,psi,p_[i-1],n_[i-1] ) +
                    expectedBlockedRounds(r_e + i +1,psi,p_[i+1],n_[i+1] ) );
            }

            return survivalProb;
        }
    
        //public class AlgFitness : IFitnessFunction
        //{
        //    static GameParams gp = new GameParams(@"C:\Users\Mai\Desktop\pursuit\testParam4Gen.gprm");
        //    static GridGameGraph g = new GridGameGraph(@"C:\Users\Mai\Desktop\pursuit\40x40SinksInDist17.ggrp");

        //    public AlgFitness(double r_e)
        //    {
        //        this.r_e = r_e;
        //    }
        //    public double r_e { get; set; }
        //    public double Evaluate(IChromosome chromosome)
        //    {
        //        // FIXME: choose multiple psi values, or run separate gen. algorithms for different configurations
                
        //        AlgChromosome c = (AlgChromosome)chromosome;
        //        Dictionary<string, string> inp = new Dictionary<string, string>();
        //        for(int i = 0; i < 2; ++i)
        //            inp["psi_" + i.ToString()] = c.getRelativePursuersAllocation[i].ToString();
                
        //        GoE.UI.AutomatedInitOnlyPolicyGUI policyInput = new AutomatedInitOnlyPolicyGUI(inp);
        //        GameLogic.GameProcess gproc = new GameLogic.GameProcess(gp, g);
        //        EvadersPolicyEscapeAfterConstantTime chosenEvaderPolicy = new EvadersPolicyEscapeAfterConstantTime();
        //        PursuersPolicyGenPatrol chosenPursuerPolicy = new PursuersPolicyGenPatrol();

                
        //        chosenPursuerPolicy.init(g, gp, policyInput);
        //        chosenEvaderPolicy.init(g, gp, chosenPursuerPolicy, policyInput);
        //        gproc.init(chosenPursuerPolicy, chosenEvaderPolicy);
        //        while (gproc.invokeNextPolicy()) ;
        //        return -gproc.AccumulatedEvadersReward;


        //        //return 1 - getSurvivalProbability(
        //        //    (int)r_e, 
        //        //    c.getPursuerCountPerRing(2 * r_e - 1), 
        //        //    c.getRevisitProbPerRing(), 
        //        //    2 * r_e - 1);
        //    }
        //}
        //public class AlgChromosome : IChromosome
        //{
        //    /// <summary>
        //    /// 
        //    /// </summary>
        //    /// <param name="r_e"></param>
        //    /// <param name="psi"></param>
        //    /// <param name="k">
        //    /// coresponds k_Policy i.e. minimal distance for which pursuers are allocated
        //    /// </param>
        //    public AlgChromosome(int ringCount)
        //    {
        //        p_ = new List<double>(ringCount-1);
        //        for (int i = 0; i < ringCount - 1;++i )
        //            p_.Add(0);

        //        n_ = new List<double>(ringCount);
        //        for (int i = 0; i < ringCount; ++i)
        //            n_.Add(0);
                
        //        Generate();
        //    }
        //    public AlgChromosome(AlgChromosome src)
        //    {
        //        p_ = new List<double>(src.n_.Count-1);
        //        n_ = new List<double>(src.n_.Count);
        //        p_.InsertRange(0, src.p_);
        //        n_.InsertRange(0, src.n_);
        //        this.Fitness = src.Fitness;
        //    }

        //    public double Fitness { get; protected set; }

        //    public List<double> getPursuerCountPerRing(double Psi)
        //    {
        //        List<double> res = new List<double>();
        //        double remainingPursuers = 1;
        //        for (int i = 0; i < p_.Count; ++i)
        //        {
        //            res.Add(Psi * p_[i] * remainingPursuers);
        //            remainingPursuers *= (1 - p_[i]);
        //        }
        //        res.Add(Psi * remainingPursuers);
        //        return res;
        //    }
        //    public List<double> getRevisitProbPerRing()
        //    {
        //        return n_;
        //    }
            
        //    // tells how many pursuers are allicated to ring r_e. idx 1 tells how many of the *remaining* are allocated to next level etc.
        //    public List<double> getRelativePursuersAllocation
        //    {
        //        get { return p_; }
        //    }
        //    public IChromosome Clone()
        //    {
        //        AlgChromosome c = new AlgChromosome(this);
        //        return c;
        //    }

        //    public IChromosome CreateNew()
        //    {
        //        AlgChromosome c = new AlgChromosome(n_.Count);
        //        return c;
        //    }

        //    public void Crossover(IChromosome pair)
        //    {
        //        AForge.ThreadSafeRandom rnd = new ThreadSafeRandom();
        //        AlgChromosome p = (AlgChromosome)pair; // if p is null we'll see an exception anyway, so..
        //        for (int i = 0; i < p_.Count; ++i)
        //        {
        //            double ratio1 = rnd.NextDouble();
        //            double ratio2 = rnd.NextDouble();
        //            double tmp_p_i = p_[i];
        //            double tmp_n_i = n_[i];
        //            p_[i] = p_[i] * ratio1 + p.p_[i] * (1 - ratio1);
        //            p.p_[i] = tmp_p_i * ratio2 + p.p_[i] * (1 - ratio2);
        //            n_[i] = n_[i] * ratio1 + p.n_[i] * (1 - ratio1);
        //            p.n_[i] = tmp_n_i * ratio2 + p.n_[i] * (1 - ratio2);
        //        }

        //        int l = n_.Count - 1;
        //        double ratio_n1 = rnd.NextDouble();
        //        double ratio_n2 = rnd.NextDouble();
        //        double tmp_n_last = n_[l];
        //        n_[l] = n_[l] * ratio_n1 + p.n_[l] * (1 - ratio_n1);
        //        p.n_[l] = tmp_n_last * ratio_n2 + p.n_[l] * (1 - ratio_n2);
        //    }

        //    //public void Evaluate(IFitnessFunction function)
        //    //{
        //    //    AlgFitness f = (AlgFitness)function;
        //    //    Fitness = f.Evaluate(this);
        //    //}

        //    public void Generate()
        //    {
        //        AForge.ThreadSafeRandom rnd = new ThreadSafeRandom();
        //        for (int i = 0; i < p_.Count; ++i)
        //        {
        //            p_[i] = rnd.NextDouble();
        //            n_[i] = rnd.NextDouble();
        //        }
        //        n_[n_.Count - 1] = rnd.NextDouble();
        //    }


        //    /// <summary>
        //    /// go through each p_, n_ and add (with probability GeneMutationProb) a mutation of
        //    /// max size MaxMutationSize, and ensure floats always stay in range [0,1]
        //    /// </summary>
        //    public void Mutate()
        //    {
        //        const double GeneMutationProb = 0.05;
        //        const double MaxMutationSize = 0.5;

        //        ThreadSafeRandom rnd = new ThreadSafeRandom();
        //        for (int i = 0; i < p_.Count; ++i)
        //            if (rnd.NextDouble() <= GeneMutationProb)
        //            {
        //                p_[i] += ((rnd.NextDouble() * 2) - 1) * MaxMutationSize;
        //                p_[i] = p_[i].LimitRange(0, 1);
        //                n_[i] += ((rnd.NextDouble() * 2) - 1) * MaxMutationSize;
        //                n_[i] = n_[i].LimitRange(0, 1);
        //            }
        //        if (rnd.NextDouble() <= GeneMutationProb)
        //        {
        //            n_[n_.Count - 1] += ((rnd.NextDouble() * 2) - 1) * MaxMutationSize;
        //            n_[n_.Count - 1] = n_[n_.Count - 1].LimitRange(0, 1);
        //        }
        //    }

        //    public int CompareTo(object obj)
        //    {
        //        return ((AlgChromosome)obj).Fitness.CompareTo(Fitness);
        //        //return Fitness.CompareTo(((AlgChromosome)obj).Fitness);
        //    }

            
        //    private List<double> p_; // p_[0] in [0,1] tells how many pursuers are allicated to ring r_e. p_[1] tells how many of the *remaining* are allocated to next level etc.
        //    private List<double> n_; // corresponds p_
        //}
        //public static double findMinimalLeakedData(int r_e, double psi, int k, string outputFileName = "")
        //{
            //System.IO.StreamWriter s = null;
            
            
            //if(outputFileName != "")
            //{
            //    s = new StreamWriter(
            //        new FileStream(outputFileName, FileMode.Create));
            //}

            //// FIXME: currently assumes at most 3 rings
            //Population p = new Population(20, new AlgChromosome(3 /*k - r_e  + 1*/), new AlgFitness(r_e),
            //    new EliteSelection());

            //int iterations = 100;
            //while (iterations-- > 0)
            //{
            //    p.RunEpoch();
                
            //    if(s != null && (iterations % 10) == 0 )
            //    {
            //        AlgChromosome c = (AlgChromosome)p.BestChromosome;
            //        List<double> pursuersPerRing = c.getPursuerCountPerRing(psi);
            //        List<double> revisitProb = c.getRevisitProbPerRing();
            //        s.Write("Relative Pursuers Allocation per ring:");
            //        foreach (double co in c.getRelativePursuersAllocation)
            //            s.Write(co.ToString("0.00 ,"));
            //        //foreach (double co in pursuersPerRing)
            //        //    s.Write(co.ToString("0.00 ,"));
            //        //s.WriteLine();
            //        //s.Write("n:");
            //        //foreach (double n in revisitProb)
            //        //    s.Write(n.ToString("0.00"), " ");
            //        s.WriteLine();
            //        s.WriteLine("leaked data:" + (-c.Fitness).ToString());
            //        //s.WriteLine("survival prob.:" + (1-c.Fitness).ToString() );
                    
            //    }
            //}
            //s.Flush();
            //AlgChromosome r = (AlgChromosome)p.BestChromosome;
            //return 1-r.Fitness;
            
        //}
    }


    public static class PursuersBounds
    {
        // lazy fix, since I couldn't change DoubleArrayChromosome's inner vals from Solution2Alt1Chromosome
        public class CustomDoubleArrayChromosome : AForge.Genetic.DoubleArrayChromosome
        {
            public CustomDoubleArrayChromosome(IRandomNumberGenerator chromosomeGenerator, IRandomNumberGenerator mutationMultiplierGenerator, IRandomNumberGenerator mutationAdditionGenerator, int length)
                : base(chromosomeGenerator, mutationMultiplierGenerator, mutationAdditionGenerator, length)
            {
                
            }
            public void setVal(int idx, double value)
            {
                val[idx] = value;
            }
        }
        public class Solution2Alt1Chromosome : IChromosome
        {
            private CustomDoubleArrayChromosome rawData;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r_e"></param>
            /// <param name="psi"></param>
            /// <param name="k">
            /// coresponds k_Policy i.e. minimal distance for which pursuers are allocated
            /// </param>
            public Solution2Alt1Chromosome(int psi)
            {
                Fitness = double.NegativeInfinity;

                rawData = new CustomDoubleArrayChromosome(new AForge.Math.Random.UniformGenerator(new Range(0, 1)),
                    new AForge.Math.Random.UniformGenerator(new Range(0, 1)),
                    new AForge.Math.Random.UniformGenerator(new Range(0, 1)),
                    2);
                this.Psi = psi;
                Generate();
            }

            public int CompareTo(object obj)
            {
                return ((Solution2Alt1Chromosome)obj).Fitness.CompareTo(Fitness); // opposite of standard comparison
            }

            private void normalize()
            {
                for (int i = 0; i < rawData.Value.Length; ++i)
                    rawData.setVal(i, rawData.Value[i].LimitRange(0, 1));

                double sum = rawData.Value.Sum();

                for (int i = 0; i < rawData.Value.Length; ++i)
                    rawData.setVal(i, rawData.Value[i] /= sum);
                
            }

            public CustomDoubleArrayChromosome RawData { get { return rawData; } }
            public void setValues(double pursuitPortion, double areaPortion)
            {
                pursuitPortion = Math.Min(pursuitPortion, 1);
                areaPortion = Math.Min(areaPortion, 1);
                rawData.setVal(0, pursuitPortion);
                rawData.setVal(1,areaPortion);
            }

            public int UsedAreaPursuers
            {
                get;
                set;
            }
            public double UsedEta { get; set; }
            public int UsedPursuitPursuers
            {
                get;
                set;
            }
            public int UsedCircumferencePursuers
            {
                get;
                set;
            }
            public double usedP_P { get; set; }
            public double usedP_A { get; set; }
            public double usedP_C { get; set; }

            public double usedCircumferenceRP{ get; set; }
            public double usedPatrolRP { get; set; }

            public bool didEscape { get; set; }
            public int Psi { get; set; }
            public int Area 
            {
                get
                {
                    return (int)Math.Floor(Psi * (1 - rawData.Value[0]) * rawData.Value[1]);
                }
            }
            public int Pursuit
            {
                get
                {
                    return (int)Math.Floor(Psi * rawData.Value[0]);
                    
                }
            }
            public int Circumference
            {
                get
                {
                    return Psi - Pursuit - Area;
                }
            }

            public double Fitness
            {
                get;
                protected set;
            }

           
            public IChromosome Clone()
            {
                Solution2Alt1Chromosome c = new Solution2Alt1Chromosome(Psi);
                for (int i = 0; i < rawData.Value.Count(); ++i)
                    c.rawData.setVal(i, rawData.Value[i]);
                c.UsedPursuitPursuers = UsedPursuitPursuers;
                c.UsedCircumferencePursuers = UsedCircumferencePursuers;
                c.UsedAreaPursuers = UsedAreaPursuers;
                
                c.Fitness = Fitness;
                c.usedP_A = usedP_A;
                c.usedP_C = usedP_C;
                c.usedP_P = usedP_P;

                return c;
            }

            public IChromosome CreateNew()
            {
                Solution2Alt1Chromosome c = new Solution2Alt1Chromosome(Psi);
                return c;
            }

            public void Crossover(IChromosome pair)
            {
                rawData.Crossover(((Solution2Alt1Chromosome)pair).rawData);
                normalize();
            }

            public void Evaluate(IFitnessFunction function)
            {
                Solution2Alt1Fitness f = (Solution2Alt1Fitness)function;
                Fitness = f.Evaluate(this);
            }

            public void Generate()
            {
                rawData.Generate();
                normalize();
            }


            /// <summary>
            /// go through each p_, n_ and add (with probability GeneMutationProb) a mutation of
            /// max size MaxMutationSize, and ensure floats always stay in range [0,1]
            /// </summary>
            public void Mutate()
            {
                rawData.Mutate();
                normalize();
            }
        }
        public class Solution2Alt1Fitness : IFitnessFunction
        {
            private float MaxCaptureProb;
            public Solution2Alt1Fitness(int r_e, int r_p, int eta, double P_d,
                                        bool canEvadersTransmit, bool canPatrolOnCircumference, bool canDoPursuit, bool canEvadersCrawl,
                                        double discountFactor,
                                        int overridingEtaTag, int overridingLEscape,
                                        float maxCaptureProb)
            {
                MaxCaptureProb = maxCaptureProb;
                this.p_d = P_d;
                this.r_e = r_e;
                this.r_p = r_p;
                this.eta = eta;
                this.CanDoPursuit = canDoPursuit;
                this.CanEvadersTransmit = canEvadersTransmit;
                this.CanPatrolOnCircumference = canPatrolOnCircumference;
                CanEvadersCrawl = canEvadersCrawl;

                this.DiscountFactor = discountFactor;

                this.OverridingEtaTag = overridingEtaTag;
                this.OverridingLEscape = overridingLEscape;
            }
            
            double DiscountFactor {get;set;}
            public int OverridingEtaTag;
            public int OverridingLEscape;
            public double p_d { get; set; }
            public double r_e { get; set; }
            public double r_p { get; set; }
            public double eta { get; set;}
            public bool CanDoPursuit { get; set; }
            
            public bool CanEvadersTransmit { get; set; }
            public bool CanPatrolOnCircumference { get; set; }
            public bool CanEvadersCrawl { get; set; }
            //private int pursuerCountForPursuit()
            //{
            //    if(r_e != 100)
            //    {
            //        MessageBox.Show("works for r_e = 100 only!");
            //        throw new Exception("r_e!=100");
            //    }

            //    #region BIG ARRAY
            //    int[] pursuer_conut_per_rp = {
            //        180,
            //        180,
            //        180,
            //        125,
            //        125,
            //        125,
            //        125,
            //        125,
            //        80,
            //        80,
            //        80,
            //        80,
            //        80,
            //        80,
            //        80,
            //        80,
            //        80,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        45,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        20,
            //        5};
            //        #endregion

            //    if (r_p - 17 > pursuer_conut_per_rp.Count())
            //        return 5;
            //    return pursuer_conut_per_rp[(int)r_p-17];
            
            //}

            bool increaseAreaRadius = false; // if we discover that p_c = 0 after setting p_a and p_p, we need to recalculate p_a since area covers more area (i.e. bigger radius)
            private Algorithms.CircumferencePatrol dummyPatrol = new Algorithms.CircumferencePatrol(); // we allocate this once, instead of reallocating in each Evaluate() call
            
            public double Evaluate(IChromosome chromosome)
            {
                
                Solution2Alt1Chromosome c = (Solution2Alt1Chromosome)chromosome;

                if (c.Psi >= 2 * r_e)
                {
                    c.usedP_A = 0.0001;
                    c.usedP_C = 1;
                    c.usedP_P = 0;
                    c.UsedAreaPursuers = 0;
                    c.UsedPursuitPursuers = 0;
                    c.UsedCircumferencePursuers = (int)(2 * r_e);
                    c.usedPatrolRP = 0;
                    c.usedCircumferenceRP = 2;
                    c.UsedEta = 0;
                    return 0;
                }

                if (c.Pursuit < 0 || c.Circumference < 0 || c.Area < 0 || (!CanDoPursuit && c.Pursuit > 0) || (!CanPatrolOnCircumference && c.Circumference > 0))
                {
                    return double.MinValue;
                }

                int remainingPursuers = c.Psi;
                c.UsedEta = 0;

                double p_p = 0;
                if (c.Pursuit > 0)
                {
                    c.UsedPursuitPursuers = Algorithms.Pursuit.getUsedPursuers((double)r_e-1,// @FIXED PURSUIT
                        (double)r_p, (double)c.Pursuit);
                    c.usedP_P = p_p = Algorithms.Pursuit.getCaptureProbability(r_e-1,// @FIXED PURSUIT
                        r_p, c.UsedPursuitPursuers);
                }
                else
                {
                    c.usedP_P = p_p = 0;
                    c.UsedPursuitPursuers = 0;
                }

                if (c.usedP_P > MaxCaptureProb)
                    return double.MinValue;

                remainingPursuers -= c.UsedPursuitPursuers;
                

                int patrolGroupSize1 = 1;
                int patrolGroupSize2 = 1; 
                double p_a1 = 0;
                int best_rp_1 = (int)r_p;
                int areaPursuers; 
                double p_a2 = 0;
                int best_rp_2 = (int)r_p;

                int areaPatrolRadius = (int)r_e - 1;

                if (c.Circumference == 0 || CanPatrolOnCircumference == false || increaseAreaRadius == true)
                    areaPatrolRadius = (int)r_e;

                //for (int tested_r_p = 4; tested_r_p <= r_p; ++tested_r_p)
                //{
                //    double tmppatrolGroupSize = new Algorithms.SwitchPatrol().getCaptureProbability((int)tested_r_p, (int)patrolRadius, c.Area);

                //    //double tmp = tmppatrolGroupSize / (2 * (tested_r_p / 2) * (tested_r_p / 2 + 1) + 1 - tmppatrolGroupSize); // group size divided by area guarded by each pursuer
                //    double tmp = tmppatrolGroupSize;
                //    if (tmp > p_a1)
                //    {
                //        patrolGroupSize1 = (int)new Algorithms.SwitchPatrol().getUsedPursuersCount(tested_r_p, patrolRadius, c.Area);
                //        p_a1 = tmp;
                //        best_rp_1 = tested_r_p;
                //    }
                //}

                //for (int tested_r_p = 4; tested_r_p <= r_p; ++tested_r_p)
                //{
                //    areaPursuers = new Algorithms.DenseGridPatrol().getUsedPursuersCount((int)tested_r_p, (int)patrolRadius, c.Area);
                //    if (areaPursuers == 0 || areaPursuers > c.Area)
                //        continue;

                //    double tmp = new Algorithms.DenseGridPatrol().getCaptureProbability((int)tested_r_p, (int)patrolRadius, areaPursuers);
                //    if (tmp > p_a2)
                //    {
                //        p_a2 = tmp;
                //        best_rp_2 = tested_r_p;
                //        patrolGroupSize2 = areaPursuers;
                //    }
                //}
                double p_a = 0;
                
                //if (p_a1 > p_a2)
                //{
                //    p_a = p_a1;
                //    //c.UsedAreaPursuers = (int)(
                //    //    patrolGroupSize1 * (int)(
                //    //    (2 * best_rp_1 * (best_rp_1 + 1) + 1) / (2 * (best_rp_1 / 2) * (best_rp_1 / 2 + 1) + 1)));
                //    c.UsedAreaPursuers = patrolGroupSize1;
                //    c.usedPatrolRP = best_rp_1;
                //}
                //else
                //{
                //    //if (c.Area > 0)
                //    //c.UsedAreaPursuers = Algorithms.SwitchPatrol.getUsedPursuersCount((int)best_rp_2, (int)r_e, c.Area);
                //    c.UsedAreaPursuers = patrolGroupSize2;
                //    p_a = p_a2;
                //    c.usedPatrolRP = best_rp_2;
                //}
                
                //c.usedP_A = p_a;


                int best_rp = 4;
                PatrolAndPursuit.PatrolAlg bestAlg = 
                    PatrolAndPursuit.getBestPatrolType(best_rp, (int)areaPatrolRadius, c.Area);
                for (int tested_r_p = 5; tested_r_p <= r_p; ++tested_r_p)
                {
                    PatrolAndPursuit.PatrolAlg  tmpAlg =
                        PatrolAndPursuit.getBestPatrolType(tested_r_p, (int)areaPatrolRadius, c.Area);
                    if (tmpAlg.p_a > bestAlg.p_a)
                    {
                        bestAlg = tmpAlg;
                        best_rp = tested_r_p;
                    }
                }
                
                if(bestAlg.alg == null && DiscountFactor > 0.999)
                    return double.MinValue;

                if (bestAlg.alg == null)
                {
                    p_a = c.usedP_A = 0;
                    c.UsedAreaPursuers = 0;
                    c.usedPatrolRP = 0;
                }
                else
                {
                    c.UsedAreaPursuers = bestAlg.alg.getUsedPursuersCount(best_rp, (int)areaPatrolRadius, c.Area);
                    c.usedPatrolRP = best_rp;
                    p_a = c.usedP_A = bestAlg.p_a;
                }
                remainingPursuers -= c.UsedAreaPursuers;

                if (p_a == 0 && DiscountFactor > 0.9999 && !CanPatrolOnCircumference)
                    return double.MinValue; // pursuers can stay inside for as long as they want

                double p_c = 0;

                 if(areaPatrolRadius < (int)r_e)
                 {
                     // solution 2 is used (not only area patrol)

                     int circumferencePursuers = 0;

                     for (int tested_r_p = 4; tested_r_p <= r_p; tested_r_p += 2)
                     {
                         int tmpcircumferencePursuers = dummyPatrol.getUsedPursuersCount((int)tested_r_p, (int)r_e, remainingPursuers);
                         if (tmpcircumferencePursuers == 0 || tmpcircumferencePursuers > remainingPursuers)
                             continue;
                         double tmp = dummyPatrol.getCaptureProbability((int)tested_r_p, (int)r_e, tmpcircumferencePursuers);
                         if (tmp > p_c)
                         {
                             circumferencePursuers = tmpcircumferencePursuers;
                             p_c = tmp;
                             c.usedCircumferenceRP = tested_r_p;
                         }
                     }
                     c.UsedCircumferencePursuers = circumferencePursuers;
                     c.usedP_C = p_c;

                     if (p_c < p_a)
                     {
                         //increaseAreaRadius = true; // either p_c > p_a, or no circumference patrol at all
                         //return Evaluate(chromosome);
                         return double.MinValue;
                     }
                 } // if (CanPatrolOnCircumference && c.Circumference > 0)
                 else
                 {
                     c.UsedCircumferencePursuers = 0;
                     c.usedP_C = 0;
                 }

                 if (p_a == 0 && p_c == 0)
                     return double.MinValue;

                 if (CanEvadersTransmit && p_p == 0 && p_a == 0)
                     return double.MinValue;

                 increaseAreaRadius = false;

                
                double e_escape;
                if (!CanEvadersCrawl)
                    e_escape = double.MaxValue; // escape is just not an option
                else
                {
                    if (OverridingLEscape == -1)
                    {
                        double usedP_C = p_c;
                        if (!CanPatrolOnCircumference || c.Circumference == 0)
                            usedP_C = 0;

                        int l_escape = (int)Policies.PatrolAndPursuit.calculatel_escape(c.usedP_A, usedP_C, DiscountFactor, 0);
                        e_escape = PatrolAndPursuit.calculateE_Escape(p_a, usedP_C, l_escape, this.DiscountFactor, 0);
                    }
                    else
                    {
                        
                        if (!CanPatrolOnCircumference || c.Circumference == 0)
                            e_escape = PatrolAndPursuit.calculateE_Escape(p_a, 0, OverridingLEscape, this.DiscountFactor, 0);
                        else
                        {
                            e_escape = PatrolAndPursuit.calculateE_Escape(p_a, p_c, OverridingLEscape, this.DiscountFactor, 0);
                        }
                        
                        
                       
                    }
                }
               
                
                int tmpEta;
                double e_stay; 
                
                if(OverridingEtaTag != -1)
                {
                    // if user gave us a specific eta tag value, use it:
                    e_stay = PatrolAndPursuit.e_stay_etaTag(p_a, p_p, p_d, 0, this.DiscountFactor, OverridingEtaTag);
                    tmpEta = OverridingEtaTag;
                }
                else
                {
                    if (!CanEvadersTransmit)
                    {
                        e_stay = double.MaxValue;
                        tmpEta = 0;
                    }
                    else
                        e_stay = PatrolAndPursuit.calculateE_Stay((int)eta, p_a, p_p,p_d,0,this.DiscountFactor, out tmpEta);
                }
                
                c.UsedEta = tmpEta;

                if (e_escape == 0 || e_stay == 0)
                    return double.NegativeInfinity;

                if (CanEvadersTransmit)
                {
                    if (e_stay < e_escape)
                    {
                        c.didEscape = false;
                    }
                    else
                    {
                        c.didEscape = true;
                        c.UsedEta = 0;
                    }

                    return -(eta / Math.Min(e_stay, e_escape));
                }
                else if (!CanPatrolOnCircumference)
                    return -eta / e_stay;

                c.UsedEta = 0;
                c.didEscape = true;
                return -eta / e_escape;
            }
        }
 
        
        // tells how many pursuers are expected to be captured for each leaked data unit, 
        // if the evader crawls in and out, each time accumulating l_escape data units
        //public static double calcSolution2_e_escape(double p_a, double p_c, int l_escape)
        //{
        //    double e_escape;

        //    if (l_escape == 1)
        //        e_escape = p_c;
        //    else
        //        e_escape = (1 - (1 - p_c) * (1 - p_c) * Math.Pow((1 - p_a), l_escape - 2)) * (1.0 / l_escape);
        //    return e_escape;
        //}

        public static GameResult findSolution2Alt1MaximalLeakedData(int r_e, int psi, int rp, double p_d, int eta, bool canEvadersTransmit, bool canDoCircumferencePatrol, bool canDoPursuit, bool canEvadersCrawl, bool canDoAreaPatrol, double discountFactor, int overridingEtaTag = -1, int overridingLEscape = -1, float maxCaptureProb = 1)
        {
            // fixme remove below
            //for(double i = -1.0/Math.E; i < 1; i += 0.00001)
            //{
            //    double res;
            //    try
            //    {
            //        res = AdvancedMath.LambertW(i);
            //    }
            //    catch(Exception ex)
            //    {
            //    }
            //}



            //Population p = new Population(500, 
            //    new Solution2Alt1Chromosome(psi),
            //    new Solution2Alt1Fitness(r_e, rp,eta),
            //    new EliteSelection());

            //int iterations = 2000;
            //while (iterations-- > 0)
            //    p.RunEpoch();
            //Solution2Alt1Chromosome r = (Solution2Alt1Chromosome)p.BestChromosome;

            Solution2Alt1Chromosome tmp = new Solution2Alt1Chromosome(psi); 
            Solution2Alt1Chromosome r = (Solution2Alt1Chromosome)tmp.Clone();
            Solution2Alt1Fitness f = 
                new Solution2Alt1Fitness(r_e, rp, eta,p_d,
                    canEvadersTransmit, canDoCircumferencePatrol, canDoPursuit, canEvadersCrawl, discountFactor,
                    overridingEtaTag, overridingLEscape, maxCaptureProb);

            double v1 = 0,v2 = 0;

            double maxAreaPortion = 1.004;
            if (!canDoAreaPatrol)
                maxAreaPortion = 0;

            //v1 = 0.579122327814781;
            //v2 = 0.420877672185219;
            //tmp.setValues(v1, v2);
            //tmp.Evaluate(f);
            if (psi >= 2 * r_e)
            {
                tmp.setValues(0, 0);
                tmp.Evaluate(f);
                r = (Solution2Alt1Chromosome)tmp.Clone();
            }
            else
            {
                for (v1 = 0; v1 < 1.005; v1 += 0.005) // fixme: allow choosing via gui whether to use gen. alg. to exhaustive search
                {
                    //v1 = Math.Min(0.1, v1); //fixme remove
                    for (v2 = 0; v2 <= maxAreaPortion; v2 += 0.005)
                    {

                        tmp.setValues(v1, v2);
                        tmp.Evaluate(f);
                        if (tmp.Fitness > r.Fitness)
                            r = (Solution2Alt1Chromosome)tmp.Clone();
                    }


                    if (!canEvadersTransmit)
                        break; // we only check v1 = 0
                }
            }

            //Solution2Alt1Chromosome toogoodc = new Solution2Alt1Chromosome(36);
            //Solution2Alt1Fitness toogoodf = new Solution2Alt1Fitness(20, 5, 50, true);
            //toogoodc.setValues(0.579122327814781, 0.420877672185219); 
            //toogoodc.Evaluate(toogoodf);

            //tmp = (Solution2Alt1Chromosome)r.Clone();
            //tmp.setValues(0.5, 0.5);
            //tmp.Evaluate(f);

            r.Evaluate(new Solution2Alt1Fitness(r_e, rp, eta,p_d, canEvadersTransmit,
                canDoCircumferencePatrol, canDoPursuit, canEvadersCrawl, discountFactor, overridingEtaTag, overridingLEscape, maxCaptureProb));
            
            GameResult res = new GameResult();
            res.utilityPerEvader = (float)(-r.Fitness/eta);
            res[AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_CAPTURE_PROB.key] = r.usedP_A.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_CAPTURE_PROB.key] = r.usedP_C.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.PURSUIT_CAPTURE_PROB.key] = r.usedP_P.ToString();
            res[AppConstants.Policies.EvadersPolicyTransmitFromWithinArea.EVADERS_OPTIMAL_SIMULTENEOUS_TRANSMISSIONS.key] = r.UsedEta.ToString();

            int usedLEscape;
            if (overridingLEscape != -1)
                usedLEscape = overridingLEscape;
            else
                usedLEscape = (int)PatrolAndPursuit.calculatel_escape(r.usedP_A, r.usedP_C, discountFactor, 0);
            res[AppConstants.Policies.EvadersPolicyEscapeAfterConstantTime.L_ESCAPE.key] = usedLEscape.ToString();

            res[AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_PURSUERS_COUNT.key] = r.UsedAreaPursuers.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_PURSUERS_COUNT.key] = r.UsedCircumferencePursuers.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.PURSUIT_PURSUERS_COUNT.key] = r.UsedPursuitPursuers.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.AREA_PATROL_VELOCITY.key] = r.usedPatrolRP.ToString();
            res[AppConstants.Policies.PatrolAndPursuit.CIRCUMFERENCE_PATROL_VELOCITY.key] = r.usedCircumferenceRP.ToString();
            return res;

            //results["p_a"] = r.usedP_A.ToString();
            //results["p_c"] = r.usedP_C.ToString();
            //results["p_p"] = r.usedP_P.ToString();
            //results["totalLeaked"] = (-r.Fitness).ToString();
            //results["simultenous_transmissions"] = r.UsedEta.ToString();
            //results["uniform area patrol pursuers #"] = r.UsedAreaPursuers.ToString();
            //results["circumference patrol pursuers #(0 extends area patrol)"] = r.UsedCircumferencePursuers.ToString();
            //results["pursuit pursuers #"] = r.UsedPursuitPursuers.ToString();
            //results["areaPatrol_r_p"] = r.usedPatrolRP.ToString();
            //results["circumferencePatrol_r_p"] = r.usedCircumferenceRP.ToString();
            
        }
    }
}
