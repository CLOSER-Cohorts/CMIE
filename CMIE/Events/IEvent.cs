namespace CMIE.Events
{
    abstract class IEvent
    {
        public abstract EventType Type
        {
            get;
        }
    }
}
