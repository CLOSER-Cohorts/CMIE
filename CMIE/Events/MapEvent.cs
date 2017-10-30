using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    class MapEvent : IEvent
    {
        public enum MappingType
        {
            QV,
            DV,
            RV,
            QB,
            ALL
        };
        public bool AllScopes;
        public string Scope;
        private MappingType Type;

        public MapEvent(MappingType type = MappingType.ALL)
        {
            AllScopes = true;
            Type = type;
        }

        public MapEvent(string scope, MappingType type = MappingType.ALL)
        {
            AllScopes = false;
            Scope = scope;
            Type = type;
        }

        public EventType GetEventType()
        {
            return EventType.MAP;
        }

        public bool DVMap()
        {
            return Type == MappingType.ALL || Type == MappingType.DV;
        }

        public bool QVMap()
        {
            return Type == MappingType.ALL || Type == MappingType.QV;
        }

        public bool RVMap()
        {
            return Type == MappingType.ALL || Type == MappingType.RV;
        }

        public bool QBMap()
        {
            return Type == MappingType.ALL || Type == MappingType.QB;
        }
    }
}
