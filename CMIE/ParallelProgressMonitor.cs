using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysCon = System.Console;

namespace CMIE
{
    class ParallelProgressMonitor
    {
        public int Total;
        private List<int> threads;
        private object _lock;
        
        public ParallelProgressMonitor(int total)
        {
            Total = total;
            threads = new List<int>();
            _lock = new Object();
        }

        public void StartThread(int threadID, string text)
        {
            lock (_lock) {
                threads.Insert(0, threadID);
                SysCon.WriteLine(text);
            }
        }

        public void FinishThread(int threadID, string text)
        {
            lock (_lock)
            {
                int cursorTop = SysCon.CursorTop;
                SysCon.SetCursorPosition(0, cursorTop - threads.IndexOf(threadID) - 1);
                SysCon.Write(text);
                SysCon.SetCursorPosition(0, cursorTop);
            }
        }
    }
}
