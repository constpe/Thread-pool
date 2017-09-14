using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadPool
{
    class ThreadPool
    {
        private int minThreadCount;
        private int maxThreadCount;
        private Queue<Task> tasksQueue;
        private List<Thread> threads;
        private delegate void ThreadWorkerDelegate();
        private readonly object dequeueLocker = new object();
        private readonly object workendLocker = new object();
        private readonly object threadendLocker = new object();
        private int workingThreadsCount;

        private struct Task
        {
            public ParameterizedThreadStart taskProcedure;
            public object param;

            public Task(ParameterizedThreadStart taskProcedure, object param)
            {
                this.taskProcedure = taskProcedure;
                this.param = param;
            }
        }

        private void ThreadWorker()
        {
            bool isWorking = true;

            while (true)
            {
                Task task = new Task(null, null);

                lock (dequeueLocker)
                {
                    if (tasksQueue.Count != 0)
                    {
                        task = tasksQueue.Dequeue();
                        workingThreadsCount++;
                    }
                } 
              
                if (task.taskProcedure != null)
                {
                    task.taskProcedure(task.param);
                    Console.WriteLine("Threads Count: {0}; Queue Size: {1}; Working threads: {2}", threads.Count, tasksQueue.Count, workingThreadsCount);
                    lock (dequeueLocker)
                    {
                        workingThreadsCount--;                     
                    }
                }

            }
        }

        public ThreadPool(int threadCount)
        {
            minThreadCount = threadCount;
            maxThreadCount = threadCount;
            threads = new List<Thread>();
            tasksQueue = new Queue<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(ThreadWorker);
                threads.Add(thread);
                thread.Start();
            }
        }

        public ThreadPool(int minThreadCount, int maxThreadCount)
        {
            this.minThreadCount = minThreadCount;
            this.maxThreadCount = maxThreadCount;
            threads = new List<Thread>();
            tasksQueue = new Queue<Task>();

            int threadCount = (maxThreadCount + minThreadCount) / 2;

            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(ThreadWorker);
                threads.Add(thread);
                thread.Start();
            }

            new Thread(ManageThreads).Start();
        }

        private void ManageThreads()
        {
            while (true)
            {
                lock (dequeueLocker)
                {
                    if (tasksQueue.Count != 0 && threads.Count == workingThreadsCount && threads.Count < maxThreadCount)
                    {
                        Thread thread = new Thread(ThreadWorker);
                        threads.Add(thread);
                        thread.Start();
                    }

                    if (tasksQueue.Count == 0 && workingThreadsCount == 0 && threads.Count > minThreadCount)
                    {
                        threads.ElementAt(threads.Count - 1).Abort();
                        threads.RemoveAt(threads.Count - 1);
                        Console.WriteLine("Threads Count: {0}; Queue Size: {1}; Working threads: {2}", threads.Count, tasksQueue.Count, workingThreadsCount);
                    }
                }
            }
        }

        public void AddTask(ParameterizedThreadStart taskProcedure, object param)
        {
            Task task = new Task(taskProcedure, param);
            tasksQueue.Enqueue(task);
            Console.WriteLine("Init threads: {0}", threads.Count);
        }
    }
}
