namespace CMIE.Events
{
    internal class EvaluateEvent : IEvent
    {
        public override EventType Type { get { return EventType.EVALUATE; } }
    }
}
