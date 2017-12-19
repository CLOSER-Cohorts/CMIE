using CMIE.Events;

namespace CMIE.Console
{
    internal class StatusCommand : ICommand
    {
        public StatusCommand(EventManager em)
            : base(em)
        {
            Aliases = new[] { "status" };
        }

        public override Commands Type { get { return Commands.STATUS; } }

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
