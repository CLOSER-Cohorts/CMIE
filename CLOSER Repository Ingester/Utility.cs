using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository.Client;

namespace CLOSER_Repository_Ingester
{
    public static class Utility
    {
        public static RepositoryClientBase GetClient(string url = "localhost")
        {
            // The WcfRepositoryClient takes a configation object
            // detailing how to connect to the Repository.
            var connectionInfo = new RepositoryConnectionInfo()
            {
                Url = url,
                AuthenticationMethod = RepositoryAuthenticationMethod.Windows,
                TransportMethod = RepositoryTransportMethod.NetTcp,
            };

            var repo = new WcfRepositoryClient(connectionInfo);

            return repo;
        }
    }
}
