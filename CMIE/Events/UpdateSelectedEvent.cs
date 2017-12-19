namespace CMIE.Events
{
    internal class UpdateSelectedEvent : IEvent
    {
        public enum Actions
        {
            ADD,
            REMOVE
        }

        public string Scope;
        public Actions Action;

        public UpdateSelectedEvent(string scope, Actions action)
        {
            Scope = scope;
            Action = action;
        }

        public override EventType Type { get { return EventType.UPDATE_SELECTED; } }
    }
}
