using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class StatusEvent : IEvent
    {
        public StatusEvent() { }

        public EventType GetEventType()
        {
            return EventType.STATUS;
        }
    }
}
