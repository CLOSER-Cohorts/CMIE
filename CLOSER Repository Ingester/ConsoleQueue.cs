using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices.ComTypes;
using Algenta.Colectica.Model.Utility;

namespace CLOSER_Repository_Ingester
{
    public class ConsoleQueue
    {
        private Queue<string> queue;

        public ConsoleQueue()
        {
            queue = new Queue<string>();
        }

        public void Write(string str, params object[] args)
        {
            queue.Enqueue(String.Format(str, args));
        }

        public void WriteLine(string str, params object[] args)
        {
            Write(str + Environment.NewLine, args);
        }

        public void Publish()
        {
            while (queue.Count > 0)
            {
                Console.Write(queue.Dequeue());
            }
        }
    }
}