using SysCon = System.Console;

using Algenta.Colectica.Model.Repository;

using CMIE.Events;
using CMIE.ControllerSystem;

namespace CMIE
{
    internal class Comparison : IJob
    {
        private readonly EventManager _eventManager;
        private Scope _scope;
        private readonly string _host;
        private RepositoryClientBase _client;

        public Comparison(EventManager eventManager, Scope scope, string host)
        {
            _eventManager = eventManager;
            _scope = scope;
            _host = host;
        }

        public void Run()
        {
            _client = Utility.GetClient(_host);
            


            _eventManager.FireEvent(new JobCompletedEvent(JobCompletedEvent.JobType.COMPARISON));
        }
    }
}
