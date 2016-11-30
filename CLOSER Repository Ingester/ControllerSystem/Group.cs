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
    class Group
    {
        public string name { get; private set; }
        private ConcurrentDictionary<string, Scope> scopes;
        private List<IAction> unscopedActions;
        private List<IVersionable> workingSet;
        private List<IVersionable> toBeAdded;
        
        public Group(string name)
        {
            this.name = name;
            scopes = new ConcurrentDictionary<string, Scope>();
            unscopedActions = new List<IAction>();
            workingSet = new List<IVersionable>();
            toBeAdded = new List<IVersionable>();
        }

        public void AddAction(IAction action)
        {
            unscopedActions.Add(action);
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
            Parallel.ForEach<IAction>(unscopedActions, action =>
            {
                action.Validate();
                workingSet.AddRange(action.Build(workingSet));
            });
            Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            {
                scope.Value.Build();
            });
        }

        public void CompareWithRepository()
        {
            var client = Utility.GetClient();

            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.StudyUnit);
            SearchResponse response = client.Search(facet);
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
                if (wsRps.Count() > 0)
                {
                    scope.Value.rp = wsRps.First();
                    var bubbleOut = false;
                    foreach (var g in workingSet.OfType<Algenta.Colectica.Model.Ddi.Group>())
                    {
                        foreach (var su in g.StudyUnits)
                        {
                            if (su.DataCollections.Where(x => x.ItemName.Best == scope.Key).Count() > 0)
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
            Console.WriteLine(" {0} items...", toCommit.Count);
            client.RegisterItems(toCommit, new CommitOptions());
        }

        public ICollection<string> GetScopes()
        {
            return scopes.Keys;
        }
    }
}
