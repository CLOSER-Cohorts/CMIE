namespace CMIE.Events
{
    internal class StatusEvent : IEvent
    {
        public override EventType Type => EventType.STATUS;
    }
}
