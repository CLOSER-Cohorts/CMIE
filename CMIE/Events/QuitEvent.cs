using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class QuitEvent : IEvent
    {
        public QuitEvent() { }

        public EventType GetEventType()
        {
            return EventType.QUIT;
        }
    }
}
