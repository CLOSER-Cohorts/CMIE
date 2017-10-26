using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Events;

namespace CMIE.Console
{
    class StatusCommand : ICommand
    {
        public StatusCommand(EventManager em)
            : base(em)
        {
            aliases = new string[] { "status" };
        }

        public override Commands Type
        {
            get
            {
                return Commands.STATUS;
            }
        }

        public override bool Do(string[] arguments)
        {
            EventManager.FireEvent(new StatusEvent());
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "status          Displays current status information of CMIE.";
        }
    }
}
