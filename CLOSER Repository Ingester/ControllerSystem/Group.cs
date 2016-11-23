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
                workingSet.AddRange(action.Build());
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
            facet.ItemTypes.Add(DdiItemType.ResourcePackage);
            facet.SearchTargets.Add(DdiStringType.Name);

            foreach (var scope in scopes)
            {
                facet.SearchTerms.Clear();
                facet.SearchTerms.Add(scope.Value.name);
                SearchResponse response = client.Search(facet);
                Console.WriteLine("{0} has {1} results", scope.Key, response.Results.Count);
                if (response.Results.Count > 0)
                {
                    var rp = client.GetItem(
                        response.Results.First().CompositeId,
                        ChildReferenceProcessing.Populate) as ResourcePackage;
                    scope.Value.rp = rp;
                }
                else
                {
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
            }

            Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            {
                scope.Value.Compare();
            });

            
            
            //foreach (var ws in workingSets) total += ws.Value.Count;
            //Parallel.ForEach<KeyValuePair<string, List<IVersionable>>>(workingSets, workingSet =>
            //{
            //    var result = response.Results.FirstOrDefault(x => x.ItemName.Best == workingSet.Key);
            //    if (result != null)
            //    {
            //        Console.WriteLine("\r\t{0}'s DC found.", workingSet.Key);
            //        var dc = client.GetItem(
            //            result.CompositeId,
            //            ChildReferenceProcessing.Populate) as DataCollection;

            //        Instrument instrument = null;
            //        foreach (var child in dc.GetChildren())
            //        {
            //            if (child.ItemType == DdiItemType.Instrument)
            //            {
            //                instrument = client.GetItem(
            //                    child.CompositeId,
            //                    ChildReferenceProcessing.Populate
            //                    ) as Instrument;
            //                break;
            //            }
            //        }
            //        if (instrument == null)
            //        {
            //            Console.WriteLine("\r\t->No instrument found");
            //        }
            //        else
            //        {
            //            var graphPopulator = new GraphPopulator(client);
            //            instrument.Accept(graphPopulator);
            //            var gatherer = new ItemGathererVisitor();
            //            instrument.Accept(gatherer);
            //            var repoItems = gatherer.FoundItems;
            //            var urnsToBeAdded = new List<Guid>();
            //            var urnsToBeRemoved = new List<Guid>();

            //            foreach (var wsItem in workingSet.Value) urnsToBeAdded.Add(wsItem.Identifier);
            //            foreach (var repoItem in repoItems)
            //            {
            //                counter++;
            //                Console.Write("\r\t->Comparing {0}%", Math.Round((float)counter * 100 / (float)total));
            //                IVersionable item;
            //                try
            //                {
            //                    item = client.GetItem(repoItem.CompositeId, ChildReferenceProcessing.Populate);
            //                }
            //                catch (Exception e)
            //                {
            //                    Console.WriteLine("\r\tCould not find {0} in repository.", repoItem);
            //                    Console.WriteLine(e.Message);
            //                    continue;
            //                }
                            
            //                if (item.UserIds.Count > 0)
            //                {
            //                    var wsMatch = workingSet.Value.Find(x => x.UserIds.Count > 0 ? item.UserIds[0].Identifier == x.UserIds[0].Identifier : false);
            //                    if (wsMatch != null)
            //                    {
            //                        urnsToBeAdded.Remove(item.Identifier);
            //                    }
            //                    else
            //                    {
            //                        Console.WriteLine("\r\t-> {0}", item.UserIds[0].Identifier);
            //                        urnsToBeRemoved.Add(item.Identifier);
            //                    }
            //                }
            //            }
            //            foreach (var urn in urnsToBeRemoved)
            //            {
            //                Console.WriteLine("\t\t-> {0}", urn);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        Console.WriteLine("\t{0}'s DC could not be found found.", workingSet.Key);
            //    }
            //});
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
            Console.WriteLine("{0} items...", toCommit.Count);
            client.RegisterItems(toCommit, new CommitOptions());
        }

        public ICollection<string> GetScopes()
        {
            return scopes.Keys;
        }
    }
}
