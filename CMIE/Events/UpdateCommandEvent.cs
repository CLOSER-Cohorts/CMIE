using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Console;
using CMIE.Events;

namespace CMIE.Events
{
    class UpdateCommandEvent : IEvent
    {
        public enum Actions
        {
            ADD,
            REMOVE
        }

        public Actions action;
        public Commands command;

        public UpdateCommandEvent(Actions action, Commands command)
        {
            this.action = action;
            this.command = command;
        }

        public EventType GetEventType()
        {
            return EventType.UPDATE_COMMAND;
        }
    }
}
