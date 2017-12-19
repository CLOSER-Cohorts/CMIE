namespace CMIE.Events
{
    internal class JobCompletedEvent : IEvent
    {
        public enum JobType
        {
            EVALUATION,
            COMPARISON,
            MAPPING
        }

        public JobType JobTypeCompleted;

        public JobCompletedEvent(JobType type)
        {
            JobTypeCompleted = type;
        }

        public override EventType Type { get { return EventType.JOB_COMPLETED; } }
    }
}
