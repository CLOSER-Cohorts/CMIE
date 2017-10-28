using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository.Client;

namespace CMIE
{
    public class Repository
    {
        private RepositoryClientBase Client;
        private Dictionary<IdentifierTriple, IVersionable> Cache;
        private Dictionary<IdentifierTriple, List<IVersionable>> Parents;

        public Repository(string host)
        {
            Client = Utility.GetClient(host);
            Cache = new Dictionary<IdentifierTriple, IVersionable>();
            Parents = new Dictionary<IdentifierTriple, List<IVersionable>>();
        }

        public void AddToCache(List<IVersionable> items, bool force = false)
        {
            foreach (var item in items)
            {
                if (!Cache.ContainsKey(item.CompositeId) || force)
                {
                    Cache[item.CompositeId] = item;
                }
            }
        }

        public List<IVersionable> GetCache()
        {
            return Cache.Values.ToList();
        }

        public IVersionable GetItem(IdentifierTriple id)
        {
            if (Cache.ContainsKey(id))
            {
                return Cache[id];
            }
            else
            {
                Cache[id] = Client.GetItem(id);
                foreach (var child in Cache[id].GetChildren())
                {
                    AddParent(child.CompositeId, Cache[id]);
                    if (Cache.ContainsKey(child.CompositeId))
                    {
                        Cache[id].ReplaceChild(child.CompositeId, Cache[child.CompositeId]);
                    }
                }
                AttachToParents(Cache[id]);
                return Cache[id];
            }
        }

        public IVersionable GetLatestItem(string urn)
        {
            var pieces = urn.Split(':');
            return GetLatestItem(Guid.Parse(pieces[1]), pieces[0]);
        }

        public IVersionable GetLatestItem(Guid id, string agency)
        {
            var version = Client.GetLatestVersionNumber(id, agency);
            return GetItem(new IdentifierTriple(id, version, agency));
        }

        public List<IVersionable> Search(SearchFacet facet)
        {
            var response = Client.Search(facet);
            var output = new List<IVersionable>();

            foreach (var result in response.Results)
            {
                output.Add(GetItem(result.CompositeId));
            }

            return output;
        }

        public List<IVersionable> SearchTypedSet(IdentifierTriple id, SetSearchFacet facet)
        {
            var response = Client.SearchTypedSet(id, facet);
            var output = new List<IVersionable>();

            foreach (var result in response)
            {
                output.Add(GetItem(result.CompositeId));
            }

            return output;
        }

        private void AddParent(IdentifierTriple item, IVersionable parent)
        {
            if (!Parents.ContainsKey(item))
            {
                Parents[item] = new List<IVersionable>();
            }
            if (!Parents[item].Contains(parent))
            {
                Parents[item].Add(parent);
            }
        }

        private void AttachToParents(IVersionable item)
        {
            if (!Parents.ContainsKey(item.CompositeId)) return;
            foreach (var parent in Parents[item.CompositeId])
            {
                if (Cache.ContainsKey(parent.CompositeId))
                {
                    Cache[parent.CompositeId].ReplaceChild(item.CompositeId, item);
                }
            }
        }
    }
}
