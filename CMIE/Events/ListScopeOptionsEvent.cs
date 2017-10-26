using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class ListScopeOptionsEvent : IEvent
    {
        public bool Scopes;
        public bool Groups;

        public ListScopeOptionsEvent()
        {
            Scopes = true;
            Groups = true;
        }

        public EventType GetEventType()
        {
            return EventType.LIST_SCOPE_OPTIONS;
        }
    }
}
