using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;

namespace CLOSER_Repository_Ingester.ControllerSystem
{
    class Group : WorkArea
    {
        public string name { get; private set; }
        public int numberItemsToCommit
        {
            get
            {
                var count = 0;
                count += toBeAdded.Count;
                foreach (var scope in scopes)
                    count += scope.Value.toBeAdded.Count;
                return count;
            }
        }
        private ConcurrentDictionary<string, Scope> scopes;

        public Group(string name)
        {
            this.name = name;
            scopes = new ConcurrentDictionary<string, Scope>();
            Init();
        }

        public void AddAction(IAction action)
        {
            actions.Add(action);
        }

        public void AddAction(string scope, IAction action)
        {
            if (!scopes.ContainsKey(scope))
            {
                scopes[scope] = new Scope(scope);
            }
            scopes[scope].AddAction(action);
        }

        public void Build(bool include_globals = false)
        {
            Parallel.ForEach<IAction>(actions, action =>
            {
                Console.WriteLine("{0}: Validating {1}", name, action.scope);
                action.Validate();
                workingSet.AddRange(action.Build(workingSet));
            });
            PublishConsole();
            var progress = new ParallelProgressMonitor(scopes.Count);
            Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            {
                string text = String.Format("{0}: Building {1}", name, scope.Value.name);
                progress.StartThread(
                    Thread.CurrentThread.ManagedThreadId, 
                    text
                    );
                scope.Value.Build();
                progress.FinishThread(
                    Thread.CurrentThread.ManagedThreadId,
                    text.PadRight(40,'-') + "> done. (" + String.Format("{0} items)", scope.Value.counter[Counters.Total]).PadLeft(12)
                    );
            });
            if (include_globals)
            {
                workingSet.AddRange(ControllerSystem.Actions.LoadTVLinking.FinishedAllBuilds());
            }
            
            foreach (var scope in scopes)
            {
                scope.Value.PublishConsole();
            }
        }

        public void CompareWithRepository()
        {
            var client = Utility.GetClient();

            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.StudyUnit);
            var response = client.Search(facet);
            foreach (var result in response.Results)
            {
                var su = client.GetItem(
                        result.CompositeId,
                        ChildReferenceProcessing.PopulateLatest) as StudyUnit;
                foreach (var rp in su.ResourcePackages)
                {
                    try
                    {
                        var scope = scopes[rp.ItemName.Best];
                        scope.su = su;
                        scope.rp = rp;
                    } catch(KeyNotFoundException)
                    {
                    }
                }
            }

            foreach (var scope in scopes)
            {
                if (scope.Value.rp != default(ResourcePackage)) continue;

                var wsRps = workingSet.OfType<ResourcePackage>().Where( x => string.Compare(
                    x.DublinCoreMetadata.Title.Best, scope.Value.name
                    ) == 0
                );
                if (wsRps.Any())
                {
                    scope.Value.rp = wsRps.First();
                    var bubbleOut = false;
                    foreach (var g in workingSet.OfType<Algenta.Colectica.Model.Ddi.Group>())
                    {
                        foreach (var su in g.StudyUnits)
                        {
                            if (su.DataCollections.Count(x => x.ItemName.Best == scope.Key) > 0)
                            {
                                scope.Value.su = su;
                                var gatherer = new ItemGathererVisitor();
                                g.Accept(gatherer);
                                toBeAdded.AddRange(gatherer.FoundItems);
                                bubbleOut = true;
                            }
                            if (bubbleOut) break;
                        }
                        if (bubbleOut) break;
                    }
                }
            }

            var progress = new ParallelProgressMonitor(scopes.Count);
            Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            {
                string text = String.Format("{0}: Comparing {1}", name, scope.Value.name);
                progress.StartThread(
                    Thread.CurrentThread.ManagedThreadId,
                    text
                    );
                scope.Value.Compare();
                progress.FinishThread(
                    Thread.CurrentThread.ManagedThreadId,
                    text.PadRight(40, '-') + 
                    "> done." + 
                    String.Format("{0} compared.", scope.Value.counter[Counters.Compared]).PadLeft(16) +
                    String.Format("{0} updated.", scope.Value.counter[Counters.Updated]).PadLeft(16) +
                    String.Format("{0} added.", scope.Value.counter[Counters.Added]).PadLeft(16) +
                    String.Format("{0} removed.", scope.Value.counter[Counters.Removed]).PadLeft(16)
                    );
            });
        }

        public void Commit()
        {
            var client = Utility.GetClient();
            var facet = new SetSearchFacet();
            facet.ItemTypes.Add(DdiItemType.Group);
            facet.ReverseTraversal = true;
            var toCommit = new List<IVersionable>();
            toCommit.AddRange(toBeAdded);
            foreach (var scope in scopes)
            {
                toCommit.AddRange(scope.Value.toBeAdded);
            }
            var versioner = new Versioner();

            var acceptedTypes = new List<Guid>() {
                DdiItemType.ResourcePackage,
                DdiItemType.DataCollection,
                DdiItemType.InstrumentScheme,
                DdiItemType.Instrument
            };
            var joints = toCommit.Where(x => acceptedTypes.Contains(x.ItemType));
            var tops = new HashSet<Algenta.Colectica.Model.Ddi.Group>();
            foreach (var joint in joints)
            {
                var set = client.SearchTypedSet(joint.CompositeId, facet);
                foreach (var parent in set)
                {
                    var top = client.GetItem(
                            parent.CompositeId,
                            ChildReferenceProcessing.PopulateLatest
                            ) as Algenta.Colectica.Model.Ddi.Group;
                    tops.Add(top);
                }
            }

            foreach (var top in tops)
            {
                toCommit.Add(top);
                for (var i = 0; i < top.StudyUnits.Count; i++)
                {
                    toCommit.Add(top.StudyUnits[i]);
                    foreach (var child in top.StudyUnits[i].GetChildren().ToList())
                    {
                        var bottom_joint = toCommit.FirstOrDefault(x => x.CompositeId == child.CompositeId);
                        if (bottom_joint != default(IVersionable))
                        {
                            top.StudyUnits[i].ReplaceChild(child.CompositeId, bottom_joint);
                        }
                    }
                }
                versioner.IncrementDityItemAndParents(top);
            }
            client.RegisterItems(toCommit, new CommitOptions());
        }

        public ICollection<string> GetScopes()
        {
            return scopes.Keys;
        }
    }
}
