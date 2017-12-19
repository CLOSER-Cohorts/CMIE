namespace CMIE.Events
{
    internal class RunLinkerEvent : IEvent
    {
        public override EventType Type { get { return EventType.RUN_LINKER; } }
    }
}
