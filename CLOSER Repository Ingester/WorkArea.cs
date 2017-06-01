using System.Collections.Generic;

using Algenta.Colectica.Model;

using CLOSER_Repository_Ingester.ControllerSystem;

namespace CLOSER_Repository_Ingester
{
    public abstract class WorkArea
    {
        public enum Counters {Total, Compared, Updated, Added, Removed};
        public Dictionary<Counters, int> counter { get; protected set; }
        public List<IVersionable> workingSet;
        public List<IVersionable> toBeAdded { get; protected set; }
        protected List<IAction> actions;
        protected ConsoleQueue console;

        protected void Init()
        {
            workingSet = new List<IVersionable>();
            toBeAdded = new List<IVersionable>();
            actions = new List<IAction>();
            console = new ConsoleQueue();
            counter = new Dictionary<Counters, int>();
            counter[Counters.Total] =
                counter[Counters.Compared] =
                counter[Counters.Updated] =
                counter[Counters.Added] =
                counter[Counters.Removed] = 0;
        }

        public void PublishConsole()
        {
            console.Publish();
        }

        public void PublishConsole<T>(T was) where T : IDictionary<string, object>
        {
            foreach (var wa in was)
            {
                var x = (WorkArea)wa.Value;
                x.PublishConsole();
            }
            PublishConsole();
        }
    }
}