using System.Collections.Generic;
using System;
using SysCon = System.Console;

namespace CMIE
{
    public class ConsoleQueue
    {
        private readonly Queue<string> _queue;

        public ConsoleQueue()
        {
            _queue = new Queue<string>();
        }

        public void Write(string str, params object[] args)
        {
            _queue.Enqueue(string.Format(str, args));
        }

        public void WriteLine(string str, params object[] args)
        {
            Write(str + Environment.NewLine, args);
        }

        public void Publish()
        {
            while (_queue.Count > 0)
            {
                SysCon.Write(_queue.Dequeue());
            }
        }
    }
}