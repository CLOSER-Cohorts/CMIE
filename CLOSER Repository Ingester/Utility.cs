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
                Url = "colecticaint.inst.ioe.ac.uk",
                AuthenticationMethod = RepositoryAuthenticationMethod.Windows,
                TransportMethod = RepositoryTransportMethod.NetTcp,
                UserName = "inst\\pwidqssglsa",
                Password = "Telephone12&"
            };

            Console.WriteLine("before connection");
            var repo = new WcfRepositoryClient(connectionInfo);
            Console.WriteLine("after connection");
            Console.WriteLine(repo.GetRepositoryInfo());

            // Create the client object, passing in the connection information.
            return repo;;
        }
    }
}
