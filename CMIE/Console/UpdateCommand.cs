using CMIE.Events;

namespace CMIE.Console
{
    internal class UpdateCommand : ICommand
    {
        public UpdateCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "update" };
        }

        public override Commands Type => Commands.UPDATE;

        public override bool Do(string[] arguments)
        {
            if (arguments.Length == 1)
            {
                EventManager.FireEvent(new BuildEvent());
            }
            else
            {
                for (var i = 1; i < arguments.Length; i++)
                    EventManager.FireEvent(
                        new BuildEvent(
                            arguments[i]
                            )
                        );
            }
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "update          Perform an update of DDI Instances against the repository for a given scope.";
        }
    }
}
