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
        private int workingThreadsCount;

        private readonly object locker = new object();

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

        private void ThreadWorker()
        {
            while (true)
            {
                Task task = new Task(null, null);

                lock (locker)
                {
                    if (tasksQueue.Count != 0)
                    {
                        task = tasksQueue.Dequeue();
                        workingThreadsCount += 1;
                    }
                } 
              
                if (task.taskProcedure != null)
                {
                    task.taskProcedure(task.param);
                    lock (locker)
                    {
                        workingThreadsCount -= 1;
                    }
                }

            }
        }

        public ThreadPool(int threadCount)
        {
            threads = new List<Thread>();
            tasksQueue = new Queue<Task>();
            workingThreadsCount = 0;

            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(ThreadWorker);
                threads.Add(thread);
                thread.Start(i);
            }
        }

        public ThreadPool(int minThreadCount, int maxThreadCount)
        {
            if (minThreadCount > maxThreadCount)
            {
                int temp = minThreadCount;
                minThreadCount = maxThreadCount;
                maxThreadCount = temp;
            }

            this.minThreadCount = minThreadCount;
            this.maxThreadCount = maxThreadCount;
            threads = new List<Thread>();
            tasksQueue = new Queue<Task>();
            workingThreadsCount = 0;

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
                }
            }
        }

        public void AddTask(ParameterizedThreadStart taskProcedure, object param)
        {
            Task task = new Task(taskProcedure, param);
            tasksQueue.Enqueue(task);
        }

        public bool HasTasks()
        {
            if (workingThreadsCount > 0 || tasksQueue.Count != 0)
                return true;
            else
                return false;

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
