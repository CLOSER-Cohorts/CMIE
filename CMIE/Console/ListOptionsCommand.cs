using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Events;

namespace CMIE.Console
{
    class ListOptionsCommand : ICommand
    {
        public ListOptionsCommand(EventManager em) : base(em)
        {
            aliases = new string[] { "ls", "list" };
        }

        public override Commands Type
        {
            get
            {
                return Commands.LIST;
            }
        }

        public override bool Do(string[] arguments)
        {
            if (arguments.Length > 1)
            {
                if (arguments[1].ToLower() == "scopes")
                {
                    EventManager.FireEvent(new ListScopeOptionsEvent() { Groups = false });
                }
                else if (arguments[1].ToLower() == "groups")
                {
                    EventManager.FireEvent(new ListScopeOptionsEvent() { Scopes = false });
                }
                else
                {
                    System.Console.WriteLine("ls only accepts 'scopes', 'groups' or neither");
                    return false;
                }
            }
            else
            {
                EventManager.FireEvent(new ListScopeOptionsEvent());
            }
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "list            List possible scopes.";
        }
    }
}
