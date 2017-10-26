using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Events;

namespace CMIE.Console
{
    abstract class ICommand
    {
        protected string[] aliases;
        protected EventManager EventManager;
        protected string [] lastArguments;

        protected ICommand(EventManager em)
        {
            EventManager = em;
        }

        public abstract Commands Type
        {
            get;
        }

        public bool Do()
        {
            return Do(lastArguments);
        }

        public abstract bool Do(string[] arguments);

        public abstract string GetFormattedGuidance();

        public bool IsMatch(string command)
        {
            var chunks = command.Split(' ');
            if (Array.Exists(aliases, a => a == chunks.First()))
            {
                lastArguments = chunks;
                return true;
            }
            else
                return false;
        }
    }
}
