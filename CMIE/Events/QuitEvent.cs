namespace CMIE.Events
{
    internal class QuitEvent : IEvent
    {
        public override EventType Type { get { return EventType.QUIT; } }
    }
}
