using CMIE.Events;

namespace CMIE.Console
{
    internal class RemoveSelectionCommand : ICommand
    {
        public RemoveSelectionCommand(EventManager em) : base(em)
        {
            Aliases = new[] { "deselect", "dsel" };
        }

        public override Commands Type { get { return Commands.REMOVE_SELECTION; } }

        public override bool Do(string[] arguments)
        {
            for (var i = 1; i < arguments.Length; i++)
                EventManager.FireEvent(
                    new UpdateSelectedEvent(
                        arguments[i],
                        UpdateSelectedEvent.Actions.REMOVE
                        )
                    );
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "deselect        Remove scope or group to working set.";
        }
    }
}
