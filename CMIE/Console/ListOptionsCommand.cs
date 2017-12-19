using CMIE.Events;

namespace CMIE.Console
{
    internal class ListOptionsCommand : ICommand
    {
        public ListOptionsCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "ls", "list" };
        }

        public override Commands Type { get { return Commands.LIST; } }

        public override bool Do(string[] arguments)
        {
            if (arguments.Length > 1)
            {
                switch (arguments[1].ToLower())
                {
                    case "scopes":
                        EventManager.FireEvent(new ListScopeOptionsEvent { Groups = false });
                        break;
                    case "groups":
                        EventManager.FireEvent(new ListScopeOptionsEvent { Scopes = false });
                        break;
                    default:
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
