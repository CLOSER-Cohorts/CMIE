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
    class Comparison : IJob
    {
        private EventManager eventManager;
        private Scope scope;
        private string host;
        private RepositoryClientBase client;

        public Comparison(EventManager eventManager, Scope scope, string host)
        {
            this.eventManager = eventManager;
            this.scope = scope;
            this.host = host;
        }

        public void Run()
        {
            client = Utility.GetClient(host);
            


            eventManager.FireEvent(new JobCompletedEvent(JobCompletedEvent.JobType.COMPARISON));
        }
    }
}
