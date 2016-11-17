using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private IDictionary<string, List<IAction>> allActions;
        private ConcurrentDictionary<string, List<IVersionable>> workingSets;
        
        public Group(string name)
        {
            this.name = name;
            allActions = new Dictionary<string, List<IAction>>();
            workingSets = new ConcurrentDictionary<string, List<IVersionable>>();
        }

        public void AddAction(string scope, IAction action)
        {
            if (!allActions.ContainsKey(scope))
            {
                allActions[scope] = new List<IAction>();
                workingSets[scope] = new List<IVersionable>();
            }
            allActions[scope].Add(action);
        }

        public void Build()
        {
            foreach (KeyValuePair<string, List<IAction>> actions in allActions)
            {
                foreach (IAction action in actions.Value)
                {
                    try
                    {
                        action.Validate();
                        workingSets[actions.Key].AddRange(action.Build());
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("{0}", e.Message);
                    }
                }
            }
        }

        public void CompareWithRepository()
        {
            var client = Utility.GetClient();
            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.DataCollection);
            SearchResponse response = client.Search(facet);
            var total = 0;
            var counter = 0;
            foreach (var ws in workingSets) total += ws.Value.Count;
            Parallel.ForEach < KeyValuePair<string, List<IVersionable>>>(workingSets, workingSet =>
            {
                var result = response.Results.FirstOrDefault(x => x.ItemName.Best == workingSet.Key);
                if (result != null)
                {
                    Console.WriteLine("\r\t{0}'s DC found.", workingSet.Key);
                    var dc = client.GetItem(
                        result.CompositeId,
                        ChildReferenceProcessing.Populate) as DataCollection;

                    Instrument instrument = null;
                    foreach (var child in dc.GetChildren())
                    {
                        if (child.ItemType == DdiItemType.Instrument)
                        {
                            instrument = client.GetItem(
                                child.CompositeId,
                                ChildReferenceProcessing.Populate
                                ) as Instrument;
                            break;
                        }
                    }
                    if (instrument == null)
                    {
                        Console.WriteLine("\r\t->No instrument found");
                    }
                    else
                    {
                        var graphPopulator = new GraphPopulator(client);
                        instrument.Accept(graphPopulator);
                        var gatherer = new ItemGathererVisitor();
                        instrument.Accept(gatherer);
                        var repoItems = gatherer.FoundItems;
                        var urnsToBeAdded = new List<Guid>();
                        var urnsToBeRemoved = new List<Guid>();

                        foreach (var wsItem in workingSet.Value) urnsToBeAdded.Add(wsItem.Identifier);
                        foreach (var repoItem in repoItems)
                        {
                            counter++;
                            Console.Write("\r\t->Comparing {0}%", Math.Round((float)counter * 100 / (float)total));
                            IVersionable item;
                            try
                            {
                                item = client.GetItem(repoItem.CompositeId, ChildReferenceProcessing.Populate);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("\r\tCould not find {0} in repository.", repoItem);
                                Console.WriteLine(e.Message);
                                continue;
                            }
                            
                            if (item.UserIds.Count > 0)
                            {
                                var wsMatch = workingSet.Value.Find(x => x.UserIds.Count > 0 ? item.UserIds[0].Identifier == x.UserIds[0].Identifier : false);
                                if (wsMatch != null)
                                {
                                    urnsToBeAdded.Remove(item.Identifier);
                                }
                                else
                                {
                                    Console.WriteLine("\r\t-> {0}", item.UserIds[0].Identifier);
                                    urnsToBeRemoved.Add(item.Identifier);
                                }
                            }
                        }
                        foreach (var urn in urnsToBeRemoved)
                        {
                            Console.WriteLine("\t\t-> {0}", urn);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\t{0}'s DC could not be found found.", workingSet.Key);
                }
            });
        }

        public List<string> GetScopes()
        {
            return new List<string>(this.allActions.Keys);
        }
    }
}
