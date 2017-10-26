using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class UpdateSelectedEvent : IEvent
    {
        public enum Actions
        {
            ADD,
            REMOVE
        }

        public string Scope;
        public Actions Action;

        public UpdateSelectedEvent(string scope, Actions action)
        {
            this.Scope = scope;
            Action = action;
        }

        public EventType GetEventType()
        {
            return EventType.UPDATE_SELECTED;
        }
    }
}
