using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class LoadControlFileEvent : IEvent
    {
        public bool Reset;
        public string Filepath;

        public LoadControlFileEvent() 
        {
            Reset = true;
            Filepath = null;
        }

        public EventType GetEventType()
        {
            return EventType.LOAD_CONTROL_FILE;
        }

        public bool HasNewFile()
        {
            return this.Filepath != null;
        }
    }
}
