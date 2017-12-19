using CMIE.Events;

namespace CMIE.Console
{
    internal class HelpCommand : ICommand
    {
        public HelpCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "help" };
        }

        public override Commands Type { get { return Commands.HELP; } }

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
