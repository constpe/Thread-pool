using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadPool
{
    interface IThreadPool
    {
        void AddTask(ParameterizedThreadStart taskProcedure, object param);
        bool HasTasks();
        void Clear();
    }
}
