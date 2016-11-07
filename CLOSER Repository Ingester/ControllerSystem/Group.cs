using System;
using System.Collections.Generic;
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
        private IDictionary<string, List<IVersionable>> workingSets;
        
        public Group(string name)
        {
            this.name = name;
            allActions = new Dictionary<string, List<IAction>>();
            workingSets = new Dictionary<string, List<IVersionable>>();
        }

        public void AddAction(string scope, IAction action)
        {
            if (allActions[scope] == null)
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
            foreach (var workingSet in workingSets)
            {
                facet.SearchTerms.Add(workingSet.Key);
                SearchResponse response = client.Search(facet);
                if (response.TotalResults == 1)
                {
                    var dc = client.GetItem(
                        response.Results[0].CompositeId,
                        ChildReferenceProcessing.Populate) as DataCollection;
                    var gatherer = new ItemGathererVisitor();
                    dc.Accept(gatherer);
                    var repoItems = gatherer.FoundItems;
                    var urnsToBeAdded = new List<Guid>();
                    foreach (var wsItem in workingSet.Value)
                    {
                        urnsToBeAdded.Add(wsItem.Identifier);
                    }
                    foreach (var repoItem in repoItems)
                    {
                        workingSet.Value.Find(x => repoItem.UserIds[0].Identifier == x.CompositeId.Identifier.ToString());
                    }
                }
            }
        }

        public List<string> GetScopes()
        {
            return new List<string>(this.allActions.Keys);
        }
    }
}
