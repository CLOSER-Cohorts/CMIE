using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using SysCon = System.Console;

using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model;

using CMIE.Events;
using CMIE.ControllerSystem;

namespace CMIE
{
    class Evaluation : IJob
    {
        private EventManager eventManager;
        private Controller controller;
        private string host;
        private RepositoryClientBase client;

        public Evaluation(EventManager eventManager, Controller controller, string host)
        {
            this.eventManager = eventManager;
            this.controller = controller;
            this.host = host;
        }

        public void Run()
        {
            if (!controller.HasSelected())
            {
                SysCon.WriteLine("Error: Nothing selected for evaluation.");
                return;
            }
            client = Utility.GetClient(host);
            foreach (var scope in controller.GetSelectedScopes())
            {
                scope.Build();
                Guid[] bindingTypes = {
                                           DdiItemType.Instrument, 
                                           DdiItemType.PhysicalInstance,
                                           DdiItemType.ResourcePackage
                                       };

                var bindingPoints = scope.workingSet.Where(x => Array.Exists(bindingTypes, y => y == x.ItemType));
                var userIds = bindingPoints.SelectMany(x => x.UserIds.Select(y => y.Identifier).ToList()).ToList();

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

                    if (client.Search(facet).TotalResults > 0)
                    {
                        scope.update = true;
                    }
                }
                else
                {
                    foreach (var bp in bindingPoints)
                    {
                        try
                        {
                            var result = client.GetLatestItem(bp.Identifier, bp.AgencyId);
                            if (result != default(IVersionable))
                            {
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

            eventManager.FireEvent(new JobCompletedEvent(JobCompletedEvent.JobType.EVALUATION));
        }
    }
}
