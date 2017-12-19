namespace CMIE.Events
{
    internal class ListAvailableCommandsEvent : IEvent
    {
        public override EventType Type { get { return EventType.LIST_AVAILABLE_COMMANDS; } }
    }
}
