namespace CMIE.Events
{
    internal class ListScopeOptionsEvent : IEvent
    {
        public bool Scopes;
        public bool Groups;

        public ListScopeOptionsEvent()
        {
            Scopes = true;
            Groups = true;
        }
        
        public override EventType Type => EventType.LIST_SCOPE_OPTIONS;
    }
}
