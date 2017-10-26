using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class EvaluateEvent : IEvent
    {
        public EvaluateEvent() {}

        public EventType GetEventType()
        {
            return EventType.EVALUATE;
        }
    }
}
