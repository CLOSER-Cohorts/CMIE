using SysCon = System.Console;

using CMIE.Events;

namespace CMIE.Console
{
    internal class CommitCommand : ICommand
    {
        public CommitCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "commit" };
        }

        public override Commands Type => Commands.COMMIT;

        public override bool Do(string[] arguments)
        {
            SysCon.Write("Commit comment: ");
            var rationale = SysCon.ReadLine();
            EventManager.FireEvent(new CommitEvent(rationale));
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "commit          Submits all staged work to the repository.";
        }
    }
}
