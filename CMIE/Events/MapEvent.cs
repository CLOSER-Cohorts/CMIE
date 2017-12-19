namespace CMIE.Events
{
    internal class MapEvent : IEvent
    {
        public enum MappingType
        {
            QV,
            DV,
            TQ,
            TV,
            RV,
            QB,
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

        public override EventType Type { get { return EventType.MAP; } }

        public bool DVMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.DV;
        }

        public bool QVMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.QV;
        }

        public bool TQMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.TQ;
        }

        public bool TVMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.TV;
        }

        public bool RVMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.RV;
        }

        public bool QBMap()
        {
            return _mappingType == MappingType.ALL || _mappingType == MappingType.QB;
        }
    }
}
