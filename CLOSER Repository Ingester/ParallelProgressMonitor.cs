using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLOSER_Repository_Ingester
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
                Console.WriteLine(text);
            }
        }

        public void FinishThread(int threadID, string text)
        {
            lock (_lock)
            {
                int cursorTop  = Console.CursorTop;
                Console.SetCursorPosition(0, cursorTop - threads.IndexOf(threadID) - 1);
                Console.Write(text);
                Console.SetCursorPosition(0, cursorTop);
            }
        }
    }
}
