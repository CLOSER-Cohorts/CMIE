using CMIE.Events;

namespace CMIE.Console
{
    internal class LinkCommand : ICommand
    {
        public LinkCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "link" };
        }

        public override Commands Type { get { return Commands.LINK; } }

        public override bool Do(string[] arguments)
        {
            EventManager.FireEvent(new RunLinkerEvent());
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "link            Evaluates all the hard links and provides an cli to update links.";
        }
    }
}
