using AForge;
using AForge.Genetic;
using AForge.Math.Random;
using GoE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using System.Threading;
using Meta.Numerics.Functions;
using GoE.Policies;

using GoE.Utils.Algorithms;

namespace GoE.GameLogic.EvolutionaryStrategy
{

    public class DebugRandom : Random
    {
        int val;
        public DebugRandom(int Val = 0 )
        {
            val = Val;
        }
        public override int Next()
        {
            return val ;
        }
        public override int Next(int maxValue)
        {
            return val ;
        }
        public override void NextBytes(byte[] buffer)
        {
            for (int i = 0 ; i < buffer.Count(); ++i)
                buffer[i] = (byte)val ;

        }
        public override int Next(int minValue, int maxValue)
        {
            return minValue + (maxValue / val);
        }
        public override double NextDouble()
        {
            return 1.0/(double)val ;
        }
    }
    public class DebugTSRandom : ThreadSafeRandom
    {
        DebugRandom _rand;

        public DebugTSRandom(int Val = 0)
        {
            _rand = new DebugRandom(Val);
        }
        public override Random rand
        {
            get
            {
                return _rand;
            }
        }
        //public override int Next()
        //{
        //    return 0;
        //}

        //public override int Next(int maxValue)
        //{
        //    return 0;
        //}

        //public override int Next(int minValue, int maxValue)
        //{
        //    return 0;
        //}

        //public override double NextDouble()
        //{
        //    return 0;
        //}
    }
    /// <summary>
    /// should be much faster than AForge's implementation
    /// </summary>
    public class ThreadSafeRandom
    {      
        [ThreadStatic] private static Random _local;
        
        private static Meta.Numerics.Statistics.Distributions.NormalDistribution 
            _normalRand = new Meta.Numerics.Statistics.Distributions.NormalDistribution(0.5, 0.3); // shouldn't be local static!

#if DEBUG
        int initThread = -1;
#endif
        public ThreadSafeRandom()
        {
            if (_local == null)
            {
                _local = new Random(Thread.CurrentThread.ManagedThreadId);
                //_local = new Random(0); // fixme 

#if DEBUG
                initThread = Thread.CurrentThread.ManagedThreadId;
#endif

            }
        }

        /// <summary>
        /// returns a number between -0.5 and 0.5, with random distribution
        /// </summary>
        /// <returns></returns>
        public double generateNormalDistNumber()
        {
            //const int REPETITION_COUNT = 20;

            //double res = 0;
            //for (int i = 0; i < REPETITION_COUNT; ++i)
            //    res += rand.NextDouble();
            //return (res / REPETITION_COUNT).LimitRange(0, 1) - 0.5f;
            return _normalRand.GetRandomValue(rand);
            
        }
        
        public virtual int Next()
        {
            return rand.Next();
        }

        public virtual int Next(int maxValue)
        {
            return rand.Next(maxValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minValue">inclusive</param>
        /// <param name="maxValue">exclusive</param>
        /// <returns></returns>
        public virtual int Next(int minValue, int maxValue)
        {
            return rand.Next(minValue, maxValue);
        }

        public virtual double NextDouble()
        {
            return rand.NextDouble();
        }

        public virtual Random rand
        {
            get
            {
                return _local;
            }
        }
    }
    static class EvolutionUtils
    {
        /// <summary>
        /// iunsures val isn't higher than maxHigher, by using % for much larger values (assuming it was random, so we keep it random) 
        /// or using ceil, for slightly higher values (assuming it was an out of bounds mutation)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="maxVal"></param>
        /// <returns></returns>
        public static ushort getLegalVal(ushort val, ushort maxVal)
        {
            if (val > 2 * (int)maxVal)
                return (ushort)(val % maxVal);
            return Math.Min(maxVal,val);
        }

        /// <summary>
        /// unlike the ushort version, this is just insures a value between 0 and maxVal
        /// </summary>
        /// <typeparam name="?"></typeparam>
        /// <param name="val"></param>
        /// <param name="maxVal"></param>
        /// <returns></returns>
        public static double getLegalVal(double val, double maxVal)
        {
            return val.LimitRange(0, maxVal);
        }

        /// <summary>
        /// Must be initialized once for each separate thread, before use.
        /// </summary>
        public static ThreadSafeRandom threadSafeRand = new ThreadSafeRandom();

        /// <summary>
        /// gets two values v1, v2, and replaces each of them with a value between v1 and v2
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public static void CrossValues(ref ushort v1, ref ushort v2)
        {
            
            ushort tmpV1 = v1;
            GoE.Utils.MathEx.MinMax(ref v1, ref v2);
            v1 = (ushort)threadSafeRand.rand.Next(v1, v2+1);
            v2 = (ushort)threadSafeRand.rand.Next(tmpV1, v2 );
        }

        public static bool getRandomDecision(double decisionTrueProbability)
        {
            return (threadSafeRand.rand.Next() % 1001) < decisionTrueProbability * 1000;
        }
    }


    public class MemberBinaryChromosome : BinaryChromosome
    {
        public MemberBinaryChromosome(int length) : base(length)
        {
        }

        protected MemberBinaryChromosome(BinaryChromosome source) : base(source)
        {
        }
        public override IChromosome Clone()
        {
            return new MemberBinaryChromosome(this);
        }
        public override IChromosome CreateNew()
        {
            return new MemberBinaryChromosome(length);
        }
        
        public new ulong Value
        {
            get
            {
                return val;
            }
            set
            {
                this.val = value;
            }
        }
    }
    /// <summary>
    /// allows editing a DoubleArrayChromosome (useful when the chromosome is a member, and not inherited)
    /// </summary>
    public class MemberDoubleArrayChromosome : AForge.Genetic.DoubleArrayChromosome
    {

        private double valueMutationProb;
        public MemberDoubleArrayChromosome(int length, float maxVal, double ValueMutationProb)
            : base(new AForge.Math.Random.UniformGenerator(new Range(0,maxVal)),
                   new AForge.Math.Random.GaussianGenerator(1.0f, 0.33f),
                   new AForge.Math.Random.GaussianGenerator(0.5f, 0.33f),
                   length)
        {
            this.valueMutationProb = ValueMutationProb;
        }
        public override void Mutate()
        {
            // do an average of (valueMutationProb * length) mutations 

            double mutationsCount = valueMutationProb * this.Length;
            while (mutationsCount > 0)
            {
                base.Mutate();
                --mutationsCount;
            }
            if (rand.NextDouble() < mutationsCount)
                base.Mutate();
        }
        public override string ToString()
        {
            string res = "";
            for (int i = 0; i < Length; ++i)
                res += this[i].ToString("000.000");
            return res;
        }
        public override IChromosome Clone()
        {
            return new MemberDoubleArrayChromosome(this);
        }

        public override IChromosome CreateNew()
        {
            MemberDoubleArrayChromosome res = new MemberDoubleArrayChromosome(this);
            res.Generate();
            return res;
        }

        public MemberDoubleArrayChromosome(IRandomNumberGenerator chromosomeGenerator,
                                           IRandomNumberGenerator mutationMultiplierGenerator,
                                           IRandomNumberGenerator mutationAdditionGenerator, int length)
            : base(chromosomeGenerator, mutationMultiplierGenerator, mutationAdditionGenerator, length)
        { }

        public MemberDoubleArrayChromosome(MemberDoubleArrayChromosome src)
            : base(src)
        { }

        /// <summary>
        /// assumes all values are in [0,1], and returns a vector of values with a sum of 1.
        /// Example: [0.5,0.5] -> [0.5,0.25,0.25] 
        /// </summary>
        /// <returns></returns>
        public List<Double> getNormalizedValues()
        {
            //List<Double> res = new List<double>();
            //double remaining = 1;
            //for(int i = 0 ; i < count(); ++i)
            //{
            //    res.Add(remaining * this[i]);
            //    remaining -= res.Last();
            //}
            //res.Add(remaining);
            //return res;
            return AlgorithmUtils.getNormalizedValues(val);
        }

        public double this[int idx]
        {
            get
            {
                return val[idx];
            }
            set
            {
                val[idx] = value;
            }
        }
        public int count()
        {
            return val.Count();
        }
    }
    /// <summary>
    /// allows editing a ShortArrayChromosome (useful when the chromosome is a member, and not inherited)
    /// </summary>
    public class MemberShortArrayChromosome : AForge.Genetic.ShortArrayChromosome
    {
        private double valueMutationProb;
        /// <summary>
        /// assumes uniform distribution, with maxVal as maxVal
        /// </summary>
        public MemberShortArrayChromosome(int length, ushort maxValue, double ValueMutationProb)
            : base(length, maxValue)
        {
            this.valueMutationProb = ValueMutationProb;
        }
        public MemberShortArrayChromosome(MemberShortArrayChromosome src)
            : base(src)
        {

        }
        
        public override void Mutate()
        {
            // do an average of (valueMutationProb * length) mutations 

            double mutationsCount = valueMutationProb * length;
            while (mutationsCount > 0)
            {
                base.Mutate();
                --mutationsCount;
            }
            if(rand.NextDouble() < mutationsCount)
                base.Mutate();
        }
        public override string ToString()
        {
            string res = "";
            for (int i = 0; i < Length; ++i)
            {
                res += this[i].ToString();
                if (i + 1 < Length)
                    res += ",";
            }
            return res;
        }
        public override IChromosome Clone()
        {
            return new MemberShortArrayChromosome(this);
        }
        
        public override IChromosome CreateNew()
        {
            MemberShortArrayChromosome res = new MemberShortArrayChromosome(this);
            res.Generate();
            return res;
        }

        public ushort this[int idx]
        {
            get
            {
                return val[idx];
            }
            set
            {
                val[idx] = value;
            }
        }
        public int count()
        {
            return val.Count();
        }
    }
    
    /// <summary>
    /// similar to short array, but instead gives values in [0,1]
    /// </summary>
    public class MemberFractionArrayChsromosome : MemberShortArrayChromosome
    {

        public ushort PossibleValueCount
        {
            get
            {
                return (ushort)(this.MaxValue + 1);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <param name="PossibleValueCount">
        /// tells how many values in [0,1] may be produced (2 is minimal value)
        /// </param>
        public MemberFractionArrayChsromosome(int length, int PossibleValueCount, double ValueMutationProb)
            : base(length, (ushort)(PossibleValueCount - 1),ValueMutationProb)
        { 
        }

        public MemberFractionArrayChsromosome(MemberFractionArrayChsromosome src)
            : base(src)
        {

        }
        public override string ToString()
        {
            string res = "";
            for (int i = 0; i < Length; ++i)
            {
                res += this[i].ToString();
                if (i + 1 < Length)
                    res += ",";
            }
            return res;
        }
        public override IChromosome Clone()
        {
            return new MemberFractionArrayChsromosome(this);
        }
        /// <summary>
        /// returns a list of normalized values for the doubles in [startIndex, doubles.count]
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public List<double> getNormalizedValues(int startIndex = 0)
        {
            double[] vals = new double[length- startIndex];
            for (int i = startIndex; i < length; ++i)
                vals[i-startIndex] = this[i];
            return AlgorithmUtils.getNormalizedValues(vals);
        }
        public override IChromosome CreateNew()
        {
            MemberFractionArrayChsromosome res = new MemberFractionArrayChsromosome(this);
            res.Generate();
            return res;
        }

        public new double this[int idx]
        {
            get
            {
                return ((double)val[idx]) / (MaxValue);
            }
            set
            {
                val[idx] = (ushort)Math.Round(value * MaxValue);
            }
        }
        public int count()
        {
            return val.Count();
        }
    }

    public abstract class ACompositeChromosome : IChromosome
    {

        public struct _ShortIndices { }
        public struct _DoubleIndices { }
        public struct _MiscIndices { }
        public static _ShortIndices Shorts;
        public static _DoubleIndices Doubles;
        public static _MiscIndices Miscs;

        /// <summary>
        /// allows setting a mean value, relative for max value (e.g. if the max value is 10 and Mean Factor is 0.3, the random values have a mean of 3)
        /// </summary>
        public static double InitialValuesMeanFactor = 0.5; // ignored if -1

        protected double valueMutationProb;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shortsCount"></param>
        /// <param name="MaxShortVals"></param>
        /// <param name="doublesCount"></param>
        /// <param name="MaxDoubleVals"></param>
        /// <param name="doublesValueCount">
        /// tells how many different double values may be generated (e.g. 2 means only 0 and max value are possible)
        /// </param>
        /// <param name="misc"></param>
        public ACompositeChromosome(int shortsCount, ushort[] MaxShortVals,
                                   int doublesCount, double[] MaxDoubleVals, int doublesValueCount,
                                   double ValueMutationProb,
                                   List<IChromosome> misc = null)
        {
            this.ShortsCount = shortsCount;
            this.DoublesCount = doublesCount;
            this.valueMutationProb = ValueMutationProb;

            if (shortsCount > 0)
                shorts = new MemberShortArrayChromosome(shortsCount, ushort.MaxValue, valueMutationProb);
            else
                shorts = null;

            this.maxShortVals = MaxShortVals;//new ushort[shortsCount];
            //MaxShortVals.CopyTo(this.maxShortVals,0);

            if (doublesCount > 0)
                doubles = new MemberFractionArrayChsromosome(doublesCount, doublesValueCount, valueMutationProb);
            else
                doubles = null;
            this.maxDoubleVals = MaxDoubleVals;//new double[doublesCount];
            //MaxDoubleVals.CopyTo(this.maxDoubleVals,0);

            ensureLegalVals();

            if (InitialValuesMeanFactor != -1)
            {
                double factor = 2 * InitialValuesMeanFactor; // after normal value generation, the mean is already 0.5
                if (shorts != null)
                    for (int i = 0; i < shorts.count(); ++i)
                        shorts[i] = (ushort)(shorts[i] * factor);
                if (doubles != null)
                    for (int i = 0; i < doubles.count(); ++i)
                        doubles[i] = doubles[i] * factor;
            }

            miscChromosomes = misc;

            NormalDistShortsMutation = true;
            NormalDistDoublesMutation = true;
        }

        public bool NormalDistShortsMutation { get; set; }
        public bool NormalDistDoublesMutation { get; set; }

        /// <summary>
        /// utility method for inheritors, to randomize all values, when CreateNew() is called
        /// </summary>
        protected void RandomizeCompositeVals()
        {
            if (shorts != null)
                shorts.Generate();

            if (doubles != null)
                doubles.Generate();

            ensureLegalVals();

            if (miscChromosomes != null)
            {
                miscChromosomes = new List<IChromosome>();
                foreach (IChromosome c in miscChromosomes)
                    miscChromosomes.Add(c.CreateNew());

            }
            if (InitialValuesMeanFactor != -1)
            {
                double factor = 2 * InitialValuesMeanFactor; // after normal value generation, the mean is already 0.5
                if (shorts != null)
                    for (int i = 0; i < shorts.count(); ++i)
                        shorts[i] = (ushort)(shorts[i] * factor);
                if (doubles != null)
                    for (int i = 0; i < doubles.count(); ++i)
                        doubles[i] = doubles[i] * factor;
            }
        }
        public override string ToString()
        {
            string res = "(";
            if (shorts != null)
                res += shorts.ToString();
            res += "),(";
            if (doubles != null)
                res += doubles.ToString();
            res += ")";

            if (miscChromosomes != null)
                foreach (IChromosome m in miscChromosomes)
                    res += m.ToString();

            return res;
        }

        /// <summary>
        /// clones src chromosome (and clone()s inner misc chromosomes)
        /// </summary>
        /// <param name="src"></param>
        public ACompositeChromosome(ACompositeChromosome src)
        {
            if (src.shorts != null)
                shorts = new MemberShortArrayChromosome(src.shorts);
            else
                shorts = null;

            if (src.doubles != null)
                doubles = new MemberFractionArrayChsromosome(src.doubles);
            else
                doubles = null;

            maxDoubleVals = src.maxDoubleVals;
            maxShortVals = src.maxShortVals;
            ShortsCount = src.ShortsCount;
            DoublesCount = src.DoublesCount;
            Fitness = src.Fitness;

            if (src.miscChromosomes != null)
            {
                miscChromosomes = new List<IChromosome>(src.miscChromosomes.Count);
                foreach (IChromosome c in src.miscChromosomes)
                    miscChromosomes.Add(c.Clone());
            }
            else
                miscChromosomes = null;
        }


        private ACompositeChromosome() { }



        protected void CrossoverShorts(ACompositeChromosome pair)
        {
            if (shorts != null)
            {
                shorts.Crossover(pair.shorts);
                insureShortVals();
            }
        }
        protected void CrossoverDoubles(ACompositeChromosome pair)
        {
            if (doubles != null)
            {
                doubles.Crossover(pair.doubles);
                insureDoubleVals();
            }
        }
        protected void CrossoverMiscs(ACompositeChromosome pair)
        {
            if (miscChromosomes != null)
                for (int i = 0; i < MiscCount; ++i)
                    miscChromosomes[i].Crossover(pair.miscChromosomes[i]);
        }
        virtual public void Crossover(IChromosome pair)
        {
            ACompositeChromosome p = (ACompositeChromosome)pair;

            CrossoverShorts(p);
            CrossoverDoubles(p);
            CrossoverMiscs(p);
        }

        virtual public void Generate()
        {
            GenerateShorts();
            GenerateDoubles();
            GenerateMiscs();
        }
        virtual public void GenerateShorts()
        {
            if (shorts == null)
                return;

            shorts.Generate();
            insureShortVals();
            if (InitialValuesMeanFactor != -1)
            {
                double factor = 2 * InitialValuesMeanFactor; // after normal value generation, the mean is already 0.5
                for (int i = 0; i < shorts.count(); ++i)
                    shorts[i] = (ushort)(Math.Min(shorts[i] * factor, maxShortVals[i]));
            }
        }

        /// <summary>
        /// returns a randomized short (a value that could have been generated in GenerateShorts() ), with max value of 
        /// maxShortVals[i]
        /// </summary>
        virtual public ushort getGeneratedShort(int idx)
        {
            double factor = 2 * InitialValuesMeanFactor;
            ushort res = (ushort)EvolutionUtils.threadSafeRand.rand.Next();
            res = EvolutionUtils.getLegalVal(res, maxShortVals[idx]);
            return (ushort)(Math.Min(res * factor, maxShortVals[idx]));
        }
        virtual public void GenerateDoubles()
        {
            if (doubles == null)
                return;

            doubles.Generate();
            insureDoubleVals();
            if (InitialValuesMeanFactor != -1)
            {
                double factor = 2 * InitialValuesMeanFactor; // after normal value generation, the mean is already 0.5
                for (int i = 0; i < doubles.count(); ++i)
                    doubles[i] = Math.Min(doubles[i] * factor, maxDoubleVals[i]);
            }
        }
        /// <summary>
        /// returns a randomized double (a value that could have been generated in GenerateDoubles() ), with max value of 
        /// maxDoubleVals[i]
        /// </summary>
        virtual public ushort getGeneratedDouble(int idx)
        {
            double factor = 2 * InitialValuesMeanFactor;
            double res = EvolutionUtils.threadSafeRand.rand.NextDouble() * maxDoubleVals[idx];
            return (ushort)(Math.Min(res * factor, maxDoubleVals[idx]));
        }

        virtual public void GenerateMiscs()
        {
            if (miscChromosomes != null)
                foreach (IChromosome c in miscChromosomes)
                    c.Generate();
        }

        virtual public void Mutate()
        {
            if (this.NormalDistShortsMutation)
                MutateShortsUniformDist();

            if (this.NormalDistDoublesMutation)
                MutateDoublesUniformDist();

            MutateMiscs();

        }

        virtual public void MutateShortNormalDistAddition(int shortIdx)
        {
            shorts.Value[shortIdx] = ((ushort)
                (shorts.Value[shortIdx] +
                 (EvolutionUtils.threadSafeRand.generateNormalDistNumber() * maxShortVals[shortIdx]))).
                 LimitRange<ushort>((ushort)0, maxShortVals[shortIdx]);
        }
        virtual public void MutateShortsUniformDist()
        {
            if (shorts != null)
            {
                shorts.Mutate();
                insureShortVals();
            }
        }
        virtual public void MutateShortsNormalDistAddition()
        {
            if (shorts != null)
            {
                for (int i = 0; i < ShortsCount; ++i)
                    MutateShortNormalDistAddition(i);
            }
        }

        virtual public void MutateDoubleNormalDistAddition(int doubleIdx)
        {
            this[Doubles, doubleIdx] =
                (doubles.Value[doubleIdx] +
                 (EvolutionUtils.threadSafeRand.generateNormalDistNumber() * maxDoubleVals[doubleIdx])).
                 LimitRange(0.0, maxDoubleVals[doubleIdx]);
        }
        virtual public void MutateDoublesUniformDist()
        {
            if (doubles != null)
            {
                doubles.Mutate();
                insureDoubleVals();
            }
        }
        virtual public void MutateDoublesNormalDistAddition()
        {
            if (doubles != null)
            {
                for (int i = 0; i < DoublesCount; ++i)
                    MutateDoubleNormalDistAddition(i);
            }
        }
        virtual public void MutateMiscs()
        {
            if (miscChromosomes != null)
                miscChromosomes[EvolutionUtils.threadSafeRand.rand.Next() % MiscCount].Mutate();
        }

        public virtual void Evaluate(IFitnessFunction function)
        {
            Fitness = function.Evaluate(this);
        }

        public double Fitness
        {
            get;
            protected set;
        }
        public int CompareTo(object obj)
        {
            return ((ACompositeChromosome)obj).Fitness.CompareTo(Fitness); // opposite of standard comparison
        }

        /// <summary>
        /// reorders misc algorithms using 'comp', in ascending order
        /// </summary>
        /// <param name="comp"></param>
        public void sortMiscs(IComparer<IChromosome> comp)
        {
            if (miscChromosomes == null)
                return;
            miscChromosomes.Sort(comp);
        }
        public ushort this[_ShortIndices t, int idx]
        {
            get
            {
                return shorts[idx];
            }
            set
            {
                shorts[idx] = value;
            }
        }
        public double this[_DoubleIndices t, int idx]
        {
            get
            {
                return doubles[idx] * maxDoubleVals[idx];
            }
            set
            {
                doubles[idx] = value / maxDoubleVals[idx];
            }
        }
        public IChromosome this[_MiscIndices t, int idx]
        {
            get
            {
                return miscChromosomes[idx];
            }
            set
            {
                miscChromosomes[idx] = value;
            }
        }
        public int ShortsCount
        {
            get; protected set;
        }
        public int DoublesCount
        {
            get; protected set;
        }
        public int MiscCount
        {
            get { return miscChromosomes.Count; }
        }

        protected MemberShortArrayChromosome shorts;
        protected readonly ushort[] maxShortVals;
        protected MemberFractionArrayChsromosome doubles;
        protected readonly double[] maxDoubleVals;
        protected List<IChromosome> miscChromosomes = null;

        /// <summary>
        /// called after each genetic operation, to insure all values are plausible
        /// </summary>
        virtual protected void ensureLegalVals()
        {
            insureShortVals();
            insureDoubleVals();
        }
        protected void insureShortVals()
        {
            if (shorts != null)
                for (int i = 0; i < ShortsCount; ++i)
                    shorts[i] = EvolutionUtils.getLegalVal(shorts[i], maxShortVals[i]);
        }
        protected void insureDoubleVals()
        {
            if (doubles != null)
                for (int i = 0; i < DoublesCount; ++i)
                    doubles[i] = EvolutionUtils.getLegalVal(doubles[i], 1.0);
        }

        public abstract IChromosome CreateNew();
        public abstract IChromosome Clone();
    }

    public class ConcreteCompositeChromosome : ACompositeChromosome
    {
        public ConcreteCompositeChromosome(ConcreteCompositeChromosome src) : base(src) {}

        public ConcreteCompositeChromosome(int shortsCount, ushort[] MaxShortVals, int doublesCount, double[] MaxDoubleVals, int doublesValueCount, double ValueMutationProb, List<IChromosome> misc = null) : base(shortsCount, MaxShortVals, doublesCount, MaxDoubleVals, doublesValueCount, ValueMutationProb, misc)
        {}

        public override IChromosome Clone()
        {
            return new ConcreteCompositeChromosome(this);
        }

        public override IChromosome CreateNew()
        {
            
            var res =
                new ConcreteCompositeChromosome(ShortsCount, maxShortVals,
                                        DoublesCount, maxDoubleVals, (maxDoubleVals == null) ? (0) : (this.doubles.PossibleValueCount),
                                        valueMutationProb,
                                        null);

            res.RandomizeCompositeVals();
            return res;
        }

    }

   
}