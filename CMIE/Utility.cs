using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository.Client;

namespace CMIE
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

        public static IVersionable GetItem(RepositoryClientBase client, string urn)
        {
            string[] pieces = urn.Split(':');
            if (pieces.Count() == 3)
            {
                return client.GetItem(
                    new Guid(pieces[1]),
                    pieces[0],
                    Convert.ToInt64(pieces[2])
                    );
            }
            if (pieces.Count() == 2)
            {
                return client.GetLatestItem(
                    new Guid(pieces[1]),
                    pieces[0]
                    );
            }
            throw new Exception("Poorly formatted URN");
        }

        public class ObservableCollectionFast<T> : ObservableCollection<T>
        {
            public ObservableCollectionFast() : base() { }

            public ObservableCollectionFast(IEnumerable<T> collection) : base(collection) { }

            public ObservableCollectionFast(List<T> list) : base(list) { }

            public void AddRange(IEnumerable<T> range)
            {
                foreach (var item in range)
                {
                    Items.Add(item);
                }

                this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            public void Reset(IEnumerable<T> range)
            {
                this.Items.Clear();

                AddRange(range);
            }
        }

        public class ObservableCollectionUnique<T> : ObservableCollection<T>
        {
            public ObservableCollectionUnique() : base() { }

            public ObservableCollectionUnique(IEnumerable<T> collection) : base(collection) { }

            public ObservableCollectionUnique(List<T> list) : base(list) { }

            public new bool Add(T item)
            {
                if (!Items.Contains(item))
                {
                    Items.Add(item);
                    this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                    this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                    //this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
