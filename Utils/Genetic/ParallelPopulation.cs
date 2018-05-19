using AForge.Genetic;
using GoE.AdvRouting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Utils.Genetic
{
    /// <summary>
    /// wraps the chromosome with a utility chromsome, so the fitness evaluation task is sent to a thread pool
    /// NOTE: The first attempt to wrap the chromosome with a wrapper/generic inheritor that does lazy evaluation
    /// caused a few problems, since the code contains explicit down casts, chromosome cloning etc.
    /// </summary>
    public class MultiThreadEvaluationPopulation<T> : GoE.Utils.Genetic.Population where T : IChromosome
    {
        public delegate IChromosome ChromosomeGenerator(string seralization);

        //private class LazyFitnessEvaluator<K> : K where K : IChromosome
        //{
        //    public CustomThreadPool TasksPool {get; set;}

        //    public LazyFitnessEvaluator(K originalChromosome, CustomThreadPool tasksPool)
        //        : base(originalChromosome)
        //    {
        //        this.TasksPool = tasksPool;
        //    }
        //    public static implicit operator K(LazyFitnessEvaluator<K> c)
        //    {
        //        return c.OriginalChromosome;
        //    }

        //    public void Evaluate(IFitnessFunction f)
        //    {
        //        Action action = () => { OriginalChromosome.Evaluate(f); };
        //        TasksPool.QueueUserWorkItem(action);
        //    }

        //    public IChromosome Clone()
        //    {
        //        return new LazyFitnessEvaluator<K>((K)OriginalChromosome.Clone(),TasksPool);
        //    }

        //    public IChromosome CreateNew()
        //    {
        //        return new LazyFitnessEvaluator<K>((K)OriginalChromosome.CreateNew(),TasksPool);
        //    }

        //    public void Crossover(IChromosome pair)
        //    {
        //        OriginalChromosome.Crossover(pair);
        //    }

        //    public double Fitness
        //    {
        //        get { return OriginalChromosome.Fitness; }
        //    }

        //    public void Generate()
        //    {
        //        OriginalChromosome.Generate();
        //    }

        //    public void Mutate()
        //    {
        //        OriginalChromosome.Mutate();
        //    }

        //    public int CompareTo(object obj)
        //    {
        //        return OriginalChromosome.CompareTo(obj);
        //    }
        //}
        private CustomThreadPool TasksPool;
        
        /// <summary>
        /// allows saving the population (using each chromosome's ToString() method
        /// </summary>
        /// <returns></returns>
        public List<string> serializePopulation()
        {
            List<string> res = new List<string>();
            for (int cc = 0; cc < this.Size; ++cc)
                res.Add(this[cc].ToString());
            return res;
        }

        
        /// <summary>
        /// creates a new population from seralization (returned from serializePopulation())
        /// ancesstor is set to the first of the deserialized chromosomes
        /// </summary>
        /// <param name="popSerialization"></param>
        public static MultiThreadEvaluationPopulation<T> desrializePopulation(
            List<string> popSerialization,
            ChromosomeGenerator deserializer,
            IFitnessFunction fitnessFunction, 
            ISelectionMethod selectionMethod, 
            CustomThreadPool pool,
            bool allowDuplicateChromosomes)
        {
            IChromosome ancestor = deserializer(popSerialization[0]);
            MultiThreadEvaluationPopulation <T> newpop = 
                new MultiThreadEvaluationPopulation<T>(0, (T)ancestor, fitnessFunction, selectionMethod, pool, allowDuplicateChromosomes);

            newpop.size = popSerialization.Count;
            for (int cc = 0; cc < popSerialization.Count; ++cc)
                newpop.AddChromosome(deserializer(popSerialization[cc]));
            return newpop;
            
        }

        bool allowDupes;
        public MultiThreadEvaluationPopulation(int size, T ancestor, IFitnessFunction fitnessFunction, ISelectionMethod selectionMethod, CustomThreadPool pool, bool allowDuplicateChromosomes)
            : base(size,fitnessFunction, selectionMethod)
        {
            TasksPool = pool;
            if(ancestor != null && size > 0)
                AddChromosome(ancestor);
            TasksPool.waitAllTasks(); // the first evaluation can't be parallel, due to gui input problems
            for (int s = 1; s < size; ++s)
                AddChromosome(this[0].CreateNew());
            allowDupes = allowDuplicateChromosomes;
        }

        
        public void runEpochParallel()
        {
            
            Crossover();
            Mutate();
            if (!allowDupes)
                replaceDuplicates();
            TasksPool.waitAllTasks(); // selection needs the fitness already evaluated
            Selection();
            
            if (AutoShuffling)
                Shuffle();
        }

        const int MAX_UNDUPE_ATTEMPTS = 100;

        // replaces duplicate chromosomes (relying on chromosome's ToString() )
        // with chromsome's CreateNew() chromosomes
        private void replaceDuplicates()
        {
            HashSet<string> currentChromosomeStrings = new HashSet<string>();
            List<IChromosome> oldpop = population;
            population = new List<IChromosome>();
            foreach (var p in oldpop)
            {
                IChromosome newp = p;
                string cs = newp.ToString();
                int fsCounter = MAX_UNDUPE_ATTEMPTS;
                while (currentChromosomeStrings.Contains(cs) && fsCounter-- > 0)
                {
                    newp = newp.CreateNew();
                    cs = newp.ToString();
                }
                currentChromosomeStrings.Add(cs);
                population.Add(newp);
            }

        }

        /// <summary>
        /// Do crossover in the population.
        /// </summary>
        /// 
        /// <remarks>The method walks through the population and performs crossover operator
        /// taking each two chromosomes in the order of their presence in the population.
        /// The total amount of paired chromosomes is determined by
        /// <see cref="CrossoverRate">crossover rate</see>.</remarks>
        /// 
        public override void Crossover()
        {
            // crossover
            for (int i = 1; i < Size; i += 2)
            {
                // generate next random number and check if we need to do crossover
                if (rand.NextDouble() <= CrossoverRate)
                {
                    // clone both ancestors
                    IChromosome c1 = this[i - 1].Clone();
                    IChromosome c2 = this[i].Clone();

                    // do crossover
                    c1.Crossover(c2);

                    // calculate fitness of these two offsprings
                    AddChromosome(c1);
                    AddChromosome(c2);
                }
            }
        }

        /// <summary>
        /// Do mutation in the population.
        /// </summary>
        /// 
        /// <remarks>The method walks through the population and performs mutation operator
        /// taking each chromosome one by one. The total amount of mutated chromosomes is
        /// determined by <see cref="MutationRate">mutation rate</see>.</remarks>
        /// 
        public override void Mutate()
        {
            // mutate
            for (int i = 0; i < Size; i++)
            {
                // generate next random number and check if we need to do mutation
                if (rand.NextDouble() <= MutationRate)
                {
                    // clone the chromosome
                    IChromosome c = this[i].Clone();
                    // mutate it
                    c.Mutate();
                    // calculate fitness of the mutant and add mutant to the population
                    AddChromosome(c);
                }
            }
        }
        public override void AddChromosome(IChromosome chromosome)
        {
            Action action = () => { chromosome.Evaluate(FitnessFunction); };
            TasksPool.QueueUserWorkItem(action);
            AddChromosomeNoEvaluation(chromosome);
        }
       
        /// <summary>
        /// Do selection.
        /// </summary>
        /// 
        /// <remarks>The method applies selection operator to the current population. Using
        /// specified selection algorithm it selects members to the new generation from current
        /// generates and adds certain amount of random members, if is required
        /// (see <see cref="RandomSelectionPortion"/>).</remarks>
        /// 
        public override void Selection()
        {
            // amount of random chromosomes in the new population
            int randomAmount = (int)(RandomSelectionPortion * Size);

            // do selection
            applySelection(Size - randomAmount);

            // add random chromosomes
            if (randomAmount > 0)
            {
                IChromosome ancestor = this[0];

                for (int i = 0; i < randomAmount; i++)
                {
                    // create new chromosome
                    IChromosome c = ancestor.CreateNew();
                    // calculate it's fitness and add it to population
                    AddChromosome(c);
                }
            }

            TasksPool.waitAllTasks();

            FindBestChromosome();
        }
    }
}
