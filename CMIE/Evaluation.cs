using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysCon = System.Console;

using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;

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
                Guid[] bindingPoints = {
                                           DdiItemType.Instrument, 
                                           DdiItemType.PhysicalInstance
                                       };

                var facet = new SearchFacet();
                foreach (var itemType in bindingPoints)
                {
                    facet.ItemTypes.Add(itemType);
                }
                facet.SearchTargets.Add(DdiStringType.UserId);

                foreach (var bindingPoint in scope.workingSet.Where(x => Array.Exists(bindingPoints, y => y == x.ItemType)))
                {
                    facet.SearchTerms.Add(bindingPoint.UserIds[0].Identifier);
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

                SysCon.WriteLine("{0,-15}: {1}", scope.name, scope.update ? "Update" : "New");
            }

            eventManager.FireEvent(new JobCompletedEvent(JobCompletedEvent.JobType.EVALUATION));
        }
    }
}
