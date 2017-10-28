using CMIE.Events;

namespace CMIE.Console
{
    internal class QuitCommand : ICommand
    {
        public QuitCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "quit" };
        }

        public override Commands Type => Commands.QUIT;

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
