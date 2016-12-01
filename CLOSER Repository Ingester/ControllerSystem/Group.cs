using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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

        public void Build()
        {
            Parallel.ForEach<IAction>(actions, action =>
            {
                action.Validate();
                workingSet.AddRange(action.Build(workingSet));
            });
            PublishConsole();
            Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            {
                scope.Value.Build();
            });
            PublishConsole(scopes);
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

            Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            {
                scope.Value.Compare();
            });
        }

        public void Commit()
        {
            var client = Utility.GetClient();
            var toCommit = new List<IVersionable>();
            toCommit.AddRange(toBeAdded);
            foreach (var scope in scopes)
            {
                toCommit.AddRange(scope.Value.toBeAdded);
            }
            console.WriteLine(" {0} items...", toCommit.Count);
            client.RegisterItems(toCommit, new CommitOptions());
        }

        public ICollection<string> GetScopes()
        {
            return scopes.Keys;
        }
    }
}
