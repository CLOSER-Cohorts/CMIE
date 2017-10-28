namespace CMIE.Events
{
    internal class ListAvailableCommandsEvent : IEvent
    {
        public override EventType Type => EventType.LIST_AVAILABLE_COMMANDS;
    }
}
