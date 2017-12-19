namespace CMIE.Events
{
    internal class LoadControlFileEvent : IEvent
    {
        public bool Reset;
        public string Filepath;

        public LoadControlFileEvent() 
        {
            Reset = true;
            Filepath = null;
        }

        public override EventType Type { get { return EventType.LOAD_CONTROL_FILE; } }

        public bool HasNewFile()
        {
            return Filepath != null;
        }
    }
}
