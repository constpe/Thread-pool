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
        private readonly object locker = new object();
        private int workingThreadsCount;

        private struct Task
        {
            public ParameterizedThreadStart taskProcedure;
            public object param;

            public Task(ParameterizedThreadStart paramTaskProcedure, object param)
            {
                this.taskProcedure = paramTaskProcedure;
                this.param = param;
            }
        }

        private void ThreadWorker(object id)
        {
            while (true)
            {
                Task task = new Task(null, null);

                lock (locker)
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
                   
                    lock (locker)
                    {
                        //Console.WriteLine("Method {0} executed by thread with id {1} | Total threads count: {2}; Tasks in queue: {3}; Working threads count: {4}", task.taskProcedure.Method.ToString(), (int)id, threads.Count, tasksQueue.Count, workingThreadsCount);
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
                thread.Start(i);
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
                thread.Start(i);
            }

            new Thread(ManageThreads).Start();
        }

        private void ManageThreads()
        {
            while (true)
            {
                lock (locker)
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

        public void Clear()
        {
            foreach (Thread thread in threads)
            {
                thread.Abort();
            }

            threads.Clear();
        }
    }
}
