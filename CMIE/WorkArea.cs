using System.Collections.Generic;

using Algenta.Colectica.Model;

using CMIE.ControllerSystem;
using CMIE.ControllerSystem.Actions;
using CMIE.ControllerSystem.Resources;

namespace CMIE
{
    public abstract class WorkArea
    {
        public enum Counters {Total, Compared, Updated, Added, Removed};
        public Dictionary<Counters, int> counter { get; protected set; }
        public List<IVersionable> workingSet;
        public List<IVersionable> toBeAdded { get; protected set; }
        public List<IAction> actions { get; protected set; }
        public List<IResource> resources { get; protected set; }
        protected ConsoleQueue console;

        protected void Init()
        {
            workingSet = new List<IVersionable>();
            actions = new List<IAction>();
            resources = new List<IResource>();
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