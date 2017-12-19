using CMIE.Events;

namespace CMIE.Console
{
    internal class EvaluateCommand : ICommand
    {
        public EvaluateCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "evaluate", "eval", "ev" };
        }

        public override Commands Type { get { return Commands.EVALUATE; } }

        public override bool Do(string[] arguments)
        {
            EventManager.FireEvent(new EvaluateEvent());
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "evaluate        Evaluates the selected scopes to determine whether they are new or require an update.";
        }
    }
}
