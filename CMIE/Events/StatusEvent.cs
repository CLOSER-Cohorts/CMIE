namespace CMIE.Events
{
    internal class StatusEvent : IEvent
    {
        public override EventType Type { get { return EventType.STATUS; } }
    }
}
