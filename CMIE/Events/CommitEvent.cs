using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class CommitEvent : IEvent
    {
        public string Rationale;

        public CommitEvent(string rationale = "")
        {
            Rationale = rationale;
        }

        public EventType GetEventType()
        {
            return EventType.COMMIT;
        }
    }
}
