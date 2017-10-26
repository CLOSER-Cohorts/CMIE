using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class ListAvailableCommandsEvent : IEvent
    {
        public ListAvailableCommandsEvent() { }

        public EventType GetEventType()
        {
            return EventType.LIST_AVAILABLE_COMMANDS;
        }
    }
}
