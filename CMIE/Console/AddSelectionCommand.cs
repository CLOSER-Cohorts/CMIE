using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Events;

namespace CMIE.Console
{
    class AddSelectionCommand : ICommand
    {
        public AddSelectionCommand(EventManager em) : base(em)
        {
            aliases = new string[] { "select", "sel" };
        }

        public override Commands Type
        {
            get
            {
                return Commands.ADD_SELECTION;
            }
        }

        public override bool Do(string[] arguments)
        {
            for (var i = 1; i < arguments.Length; i++)
                EventManager.FireEvent(
                    new UpdateSelectedEvent(
                        arguments[i],
                        UpdateSelectedEvent.Actions.ADD
                        )
                    );
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "select          Add scope or group to working set.";
        }
    }
}
