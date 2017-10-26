using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class BuildEvent : IEvent
    {
        public bool All;
        public string Scope;

        public BuildEvent()
        {
            All = true;
        }

        public BuildEvent(string scope)
        {
            All = false;
            this.Scope = scope;
        }

        public EventType GetEventType()
        {
            return EventType.BUILD;
        }
    }
}
