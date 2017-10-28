using System;
using System.Linq;
using SysCon = System.Console;

using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;

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
                SysCon.WriteLine("Error: Nothing selected for evaluation.");
                return;
            }
            _client = Utility.GetClient(_host);
            foreach (var scope in _controller.GetSelectedScopes())
            {
                scope.Build();
                Guid[] bindingPoints = {
                                           DdiItemType.Instrument, 
                                           DdiItemType.PhysicalInstance,
                                           DdiItemType.ResourcePackage
                                       };

                var facet = new SearchFacet();
                foreach (var itemType in bindingPoints)
                {
                    facet.ItemTypes.Add(itemType);
                }
                facet.SearchTargets.Add(DdiStringType.UserId);

                foreach (var bindingPoint in scope.WorkingSet.Where(x => Array.Exists(bindingPoints, y => y == x.ItemType)))
                {
                    facet.SearchTerms.Add(bindingPoint.UserIds[0].Identifier);
                }

                if (!facet.SearchTerms.Any())
                {
                    SysCon.WriteLine("{0,-15}: {1}", scope.name, "Error");
                    continue;
                }

                if (_client.Search(facet).TotalResults > 0)
                {
                    scope.update = true;
                }

                SysCon.WriteLine("{0,-15}: {1}", scope.name, scope.update ? "Update" : "New");
            }

            _eventManager.FireEvent(new JobCompletedEvent(JobCompletedEvent.JobType.EVALUATION));
        }
    }
}
