using System;
using System.Collections.Generic;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;

namespace CMIE
{
    public class Repository
    {
        private readonly RepositoryClientBase _client;
        private readonly Dictionary<IdentifierTriple, IVersionable> _cache;
        private readonly Dictionary<IdentifierTriple, List<IVersionable>> _parents;

        public Repository(string host)
        {
            _client = Utility.GetClient(host);
            _cache = new Dictionary<IdentifierTriple, IVersionable>();
            _parents = new Dictionary<IdentifierTriple, List<IVersionable>>();
        }

        public void AddToCache(List<IVersionable> items, bool force = false)
        {
            foreach (var item in items)
            {
                if (!_cache.ContainsKey(item.CompositeId) || force)
                {
                    _cache[item.CompositeId] = item;
                }
            }
        }

        public List<IVersionable> GetCache()
        {
            return _cache.Values.ToList();
        }

        public IVersionable GetItem(IdentifierTriple id)
        {
            if (_cache.ContainsKey(id))
            {
                return _cache[id];
            }
            
            _cache[id] = _client.GetItem(id);
            foreach (var child in _cache[id].GetChildren())
            {
                AddParent(child.CompositeId, _cache[id]);
                if (_cache.ContainsKey(child.CompositeId))
                {
                    _cache[id].ReplaceChild(child.CompositeId, _cache[child.CompositeId]);
                }
            }
            AttachToParents(_cache[id]);
            return _cache[id];
        }

        public IVersionable GetLatestItem(string urn)
        {
            var pieces = urn.Split(':');
            return GetLatestItem(Guid.Parse(pieces[1]), pieces[0]);
        }

        public IVersionable GetLatestItem(Guid id, string agency)
        {
            var version = _client.GetLatestVersionNumber(id, agency);
            return GetItem(new IdentifierTriple(id, version, agency));
        }

        public List<IVersionable> Search(SearchFacet facet)
        {
            var response = _client.Search(facet);
            return response.Results.Select(result => GetItem(result.CompositeId)).ToList();
        }

        public List<IVersionable> SearchTypedSet(IdentifierTriple id, SetSearchFacet facet)
        {
            var response = _client.SearchTypedSet(id, facet);
            return response.Select(result => GetItem(result.CompositeId)).ToList();
        }

        private void AddParent(IdentifierTriple item, IVersionable parent)
        {
            if (!_parents.ContainsKey(item))
            {
                _parents[item] = new List<IVersionable>();
            }
            if (!_parents[item].Contains(parent))
            {
                _parents[item].Add(parent);
            }
        }

        private void AttachToParents(IVersionable item)
        {
            if (!_parents.ContainsKey(item.CompositeId)) return;
            foreach (var parent in _parents[item.CompositeId])
            {
                if (_cache.ContainsKey(parent.CompositeId))
                {
                    _cache[parent.CompositeId].ReplaceChild(item.CompositeId, item);
                }
            }
        }
    }
}
