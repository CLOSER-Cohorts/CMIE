using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class EventManager
    {
        Dictionary<EventType, List<IEventListener>> listeners;

        public EventManager()
        {
            listeners = new Dictionary<EventType, List<IEventListener>>();
        }

        public bool AddListener(EventType eventType, IEventListener listener)
        {
            if (!listeners.ContainsKey(eventType))
            {
                listeners[eventType] = new List<IEventListener>();
            }
            listeners[eventType].Add(listener);
            return true;
        }

        public bool FireEvent(IEvent _event) 
        {
            if (!listeners.ContainsKey(_event.GetEventType()))
            {
                return false;
            }
            foreach (var listener in listeners[_event.GetEventType()])
            {
                listener.OnEvent(_event);
            }
            return true;
        }
    }
}
