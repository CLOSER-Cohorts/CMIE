using CMIE.Console;

namespace CMIE.Events
{
    internal class UpdateCommandEvent : IEvent
    {
        public enum Actions
        {
            ADD,
            REMOVE
        }

        public Actions Action;
        public Commands Command;

        public UpdateCommandEvent(Actions action, Commands command)
        {
            Action = action;
            Command = command;
        }

        public override EventType Type { get { return EventType.UPDATE_COMMAND; } }
    }
}
