namespace CMIE.Events
{
    internal class MapEvent : IEvent
    {
        public enum MappingType
        {
            QV,
            DV,
            RV,
            ALL
        };
        public bool AllScopes;
        public string Scope;
        private readonly MappingType _mappingType;

        public MapEvent(MappingType type = MappingType.ALL)
        {
            AllScopes = true;
            _mappingType = type;
        }

        public MapEvent(string scope, MappingType type = MappingType.ALL)
        {
            AllScopes = false;
            Scope = scope;
            _mappingType = type;
        }
        
        public override EventType Type => EventType.MAP;

        public bool DVMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.DV;
        }

        public bool QVMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.QV;
        }

        public bool RVMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.RV;
        }
    }
}
