using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class JobCompletedEvent : IEvent
    {
        public enum JobType
        {
            EVALUATION,
            COMPARISON
        }

        public JobType JobTypeCompleted;

        public JobCompletedEvent(JobType type)
        {
            JobTypeCompleted = type;
        }

        public EventType GetEventType()
        {
            return EventType.JOB_COMPLETED;
        }
    }
}
