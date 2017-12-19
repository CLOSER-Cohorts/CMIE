using System;
using System.Linq;
using SysCon = System.Console;

using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model;

using CMIE.Events;
using CMIE.ControllerSystem;

namespace CMIE
{
    internal class Evaluation : IJob
    {
        private readonly EventManager _eventManager;
        private readonly Controller _controller;
        private readonly string _host;
        private RepositoryClientBase _client;

        public Evaluation(EventManager eventManager, Controller controller, string host)
        {
            _eventManager = eventManager;
            _controller = controller;
            _host = host;
        }

        public void Run()
        {
            if (!_controller.HasSelected())
            {
                Logger.Instance.Log.Warn("Nothing selected for evaluation.");
                return;
            }
            _client = Utility.GetClient(_host);
            foreach (var scope in _controller.GetSelectedScopes())
            {
                scope.Build();
                Guid[] bindingTypes = {
                                           DdiItemType.Instrument, 
                                           DdiItemType.PhysicalInstance,
                                           DdiItemType.ResourcePackage
                                       };

                var bindingPoints = scope.WorkingSet.Where(x => Array.Exists(bindingTypes, y => y == x.ItemType));

                foreach (var bp in bindingPoints)
                {
                    var userIds = bp.UserIds.Select(y => y.Identifier).ToList();
                    if (userIds.Count > 0)
                    {
                        var facet = new SearchFacet();
                        foreach (var itemType in bindingTypes)
                        {
                            facet.ItemTypes.Add(itemType);
                        }
                        facet.SearchTargets.Add(DdiStringType.UserId);
                        foreach (var userId in userIds)
                        {
                            facet.SearchTerms.Add(userId);
                        }
                        if (!facet.SearchTerms.Any())
                        {
                            SysCon.WriteLine("{0,-15}: {1}", scope.name, "Error");
                            continue;
                        }

                        var itemsFound = _client.Search(facet);
                        if (itemsFound.TotalResults > 0)
                        {
                            foreach (var item in itemsFound.Results)
                            {
                                scope.AddBindingPoint(item.CompositeId, bp);
                            }
                            scope.update = true;
                        }
                    }
                    else 
                    {
                        try
                        {
                            var result = _client.GetLatestItem(bp.Identifier, bp.AgencyId);
                            if (result != default(IVersionable))
                            {
                                scope.AddBindingPoint(result.CompositeId, bp);
                                scope.update = true;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }


                SysCon.WriteLine("{0,-15}: {1}", scope.name, scope.update ? "Update" : "New");
            }

            _eventManager.FireEvent(new JobCompletedEvent(JobCompletedEvent.JobType.EVALUATION));
        }
    }
}
