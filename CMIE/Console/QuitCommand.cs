using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Events;

namespace CMIE.Console
{
    class QuitCommand : ICommand
    {
        public QuitCommand(EventManager em) : base(em)
        {
            aliases = new string[] { "quit" };
        }

        public override Commands Type
        {
            get
            {
                return Commands.QUIT;
            }
        }

        public override bool Do(string[] arguments)
        {
            EventManager.FireEvent(new QuitEvent());
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "quit            Closes program.";
        }
    }
}
