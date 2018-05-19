using GoE.GameLogic.EvolutionaryStrategy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoE.Utils
{
    /// <summary>
    /// threads will be created only after tasks 
    /// NOTE: method of this class are NOT thread safe!
    /// </summary>
    public class CustomThreadPool
    {
        private CancellationTokenSource wakeUpStuckWorkers;
        private BlockingCollection<Action> m_WorkItems = new BlockingCollection<Action>();
        private bool areThreadsActive = false;
        private int threadCount;
        private volatile bool isWaitingForCompletion;
        private List<Thread> activeThreads;
        private Action threadInitializationAction;

        private void startThreads()
        {
            wakeUpStuckWorkers = new CancellationTokenSource();
            isWaitingForCompletion = false;
            activeThreads = new List<Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                activeThreads.Add(new Thread(
                  () =>
                  {
                      if (threadInitializationAction != null)
                          threadInitializationAction();

                      while (true)
                      {
                          Action action = null;
                          bool success = false;

                          try
                          {
                              //success = m_WorkItems.TryTake(out action, -1, wakeUpStuckWorkers.Token);
                              // FIXME: for some reason, cancellation doesn't work??
                              success = m_WorkItems.TryTake(out action, 10, wakeUpStuckWorkers.Token);
                          }
                          catch(Exception)
                          {
                              try
                              {
                                  // if cancellation token was invoked it doesn't mean the thread shouldn't take more jobs,
                                  // it only means we are trying to finish all jobs and kill this thread asap. therefore, 
                                  // the thread is not allowed to get stuck in TryTake() anymore
                                  success = m_WorkItems.TryTake(out action, 1); 
                              }
                              catch (Exception) { }
                          }
                          if (success)
                          {
                              action();
                              continue;
                          }

                          // if we have no tasks, check if this thread is supposed to die
                          if (isWaitingForCompletion && success == false)
                              return;
                      }
                  }));
                activeThreads.Last().IsBackground = true;
                activeThreads.Last().Start();
            }
            areThreadsActive = true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="numberOfThreads">
        /// if -1 is used, Environment.ProcessorCount threads will be used
        /// </param>
        /// <param name="ThreadInitializationAction">
        /// for each newly opened thread (i.e. excluding current thread, which called this c'tor),
        /// this function will be called. This is usefull for initializing [ThreadStatic] variables
        /// </param>
        public CustomThreadPool(int numberOfThreads = -1, Action ThreadInitializationAction = null)
        {
            this.threadInitializationAction = ThreadInitializationAction;
            threadCount = numberOfThreads;
            if (threadCount == -1)
                threadCount = Environment.ProcessorCount;

            startThreads();
        }


        public void QueueUserWorkItem(Action action)
        {
            if(!areThreadsActive)
                startThreads();
            m_WorkItems.Add(action);
        }

        // makes the calling thread wait until all queued tasks are done
        public void waitAllTasks()
        {
            isWaitingForCompletion = true;
            wakeUpStuckWorkers.Cancel(); // makes sure that threads with no more work will now kill themselves if no more work is available
            foreach (var t in activeThreads)
                t.Join();
           areThreadsActive = false;
        }
    }

    /// <summary>
    /// when the pool is created, threads will remain in the background and will wait for new tasks
    /// </summary>
    public class LingeringThreadsPool
    {
        private BlockingCollection<Action> m_WorkItems = new BlockingCollection<Action>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numberOfThreads">
        /// if -1 is used, Environment.ProcessorCount threads will be used
        /// </param>
        public LingeringThreadsPool(int numberOfThreads = -1)
        {
            if (numberOfThreads == -1)
                numberOfThreads = Environment.ProcessorCount;

            for (int i = 0; i < numberOfThreads; i++)
            {
                var thread = new Thread(
                  () =>
                  {
                      while (true)
                      {
                          Action action = m_WorkItems.Take();
                          action();
                      }
                  });
                thread.IsBackground = true;
                thread.Start();
            }

            
        }

        public void QueueUserWorkItem(Action action)
        {
            m_WorkItems.Add(action);
        }
        
    }
}
