using System;
using System.Linq;

using CMIE.Events;

namespace CMIE.Console
{
    abstract class ICommand
    {
        protected string[] Aliases;
        protected EventManager EventManager;
        protected string [] LastArguments;

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
            return Do(LastArguments);
        }

        public abstract bool Do(string[] arguments);

        public abstract string GetFormattedGuidance();

        public bool IsMatch(string command)
        {
            var chunks = command.Split(' ');
            if (!Array.Exists(Aliases, a => a == chunks.First())) return false;
            
            LastArguments = chunks;
            return true;
        }
    }
}
