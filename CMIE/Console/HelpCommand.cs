using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Events;

namespace CMIE.Console
{
    class HelpCommand : ICommand
    {
        public HelpCommand(EventManager em) : base(em)
        {
            aliases = new string[] { "help" };
        }

        public override Commands Type
        {
            get
            {
                return Commands.HELP;
            }
        }

        public override bool Do(string[] arguments)
        {
            EventManager.FireEvent(new ListAvailableCommandsEvent());
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "help            Show this help screen.";
        }
    }
}
