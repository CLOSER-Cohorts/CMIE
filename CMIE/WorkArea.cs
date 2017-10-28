using System.Collections.Generic;

using Algenta.Colectica.Model;

using CMIE.ControllerSystem.Actions;
using CMIE.ControllerSystem.Resources;

namespace CMIE
{
    public abstract class WorkArea
    {
        public enum Counters {Total, Compared, Updated, Added, Removed};
        public Dictionary<Counters, int> Counter { get; protected set; }
        public List<IVersionable> WorkingSet;
        public List<IVersionable> ToBeAdded { get; protected set; }
        public List<IAction> Actions { get; protected set; }
        public List<IResource> Resources { get; protected set; }
        protected ConsoleQueue Console;

        protected void Init()
        {
            WorkingSet = new List<IVersionable>();
            Actions = new List<IAction>();
            Resources = new List<IResource>();
            Console = new ConsoleQueue();
            Counter = new Dictionary<Counters, int>();
            Counter[Counters.Total] =
                Counter[Counters.Compared] =
                Counter[Counters.Updated] =
                Counter[Counters.Added] =
                Counter[Counters.Removed] = 0;
        }

        public void PublishConsole()
        {
            Console.Publish();
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