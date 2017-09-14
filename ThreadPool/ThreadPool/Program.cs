using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreadPool;

namespace ThreadPool
{
    class Program
    {
        private static void DoSmth(object o)
        {
            int a = 0;
            for (int i = 0; i < 1000000; i++)
                for (int j = 0; j < 100; j++)
                    a = i + j;
                    Console.WriteLine(a);                 
        }

        static void Main(string[] args)
        {
            {
                ThreadPool tp = new ThreadPool(5, 10);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
                tp.AddTask(DoSmth, null);
            }

            Console.ReadKey();

        }
    }
}
