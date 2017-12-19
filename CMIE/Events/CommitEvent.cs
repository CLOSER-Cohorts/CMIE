namespace CMIE.Events
{
    internal class CommitEvent : IEvent
    {
        public string Rationale;

        public CommitEvent(string rationale = "")
        {
            Rationale = rationale;
        }
        
        public override EventType Type { get { return EventType.COMMIT; } }
    }
}
