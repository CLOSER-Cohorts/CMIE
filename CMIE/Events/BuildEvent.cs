namespace CMIE.Events
{
    internal class BuildEvent : IEvent
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
            Scope = scope;
        }
        
        public override EventType Type => EventType.BUILD;
    }
}
