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
        private readonly Dictionary<Tuple<string, Guid>, long> _latestVersions;

        public Repository(string host)
        {
            _client = Utility.GetClient(host);
            _cache = new Dictionary<IdentifierTriple, IVersionable>();
            _parents = new Dictionary<IdentifierTriple, List<IVersionable>>();
            _latestVersions = new Dictionary<Tuple<string, Guid>, long>();
        }

        public IVersionable AddToCache(IVersionable item, bool force = false)
        {
            if (!_cache.ContainsKey(item.CompositeId) || force)
            {
                _cache[item.CompositeId] = item;
                FindChildren(item);
                AttachToParents(item);
            }
            return _cache[item.CompositeId];
        }

        public List<IVersionable> AddToCache(List<IVersionable> items, bool force = false)
        {
            var output = new List<IVersionable>();
            foreach (var item in items)
            {
                output.Add(AddToCache(item, force));
            }
            return output;
        }

        public List<IVersionable> FilterOldVersions(List<IVersionable> items)
        {
            var output = new List<IVersionable>();

            foreach (var item in items)
            {
                var versionlessId = new Tuple<string, Guid>(item.AgencyId, item.Identifier);
                if (!_latestVersions.ContainsKey(versionlessId))
                {
                    _latestVersions[versionlessId] = _client.GetLatestVersionNumber(item.Identifier, item.AgencyId);
                }
                if (_latestVersions[versionlessId] == item.Version)
                {
                    output.Add(item);
                }
            }

            return output;
        }

        public List<IVersionable> GetCache()
        {
            return _cache.Values.ToList();
        }

        public RepositoryClientBase GetClient()
        {
            return _client;
        }

        public IVersionable GetItem(IdentifierTriple id, ChildReferenceProcessing processing = ChildReferenceProcessing.InstantiateLatest)
        {
            if (_cache.ContainsKey(id))
            {
                return _cache[id];
            }

            _cache[id] = _client.GetItem(id, processing);
            _cache[id].IsDirty = false;
            MakeClean(_cache[id]);
            FindChildren(_cache[id]);
            AttachToParents(_cache[id]);

            if (processing == ChildReferenceProcessing.Populate || processing == ChildReferenceProcessing.PopulateLatest)
            {
                foreach (var child in _cache[id].GetChildren())
                {
                    AddToCache(child);
                }
            }

            return _cache[id];
        }

        public IVersionable GetItem(string userId, ChildReferenceProcessing processing = ChildReferenceProcessing.InstantiateLatest)
        {
            //Check cache
            foreach(var item in _cache.Values)
            {
                if (item.UserIds.Any(x => x.Identifier == userId))
                {
                    return item;
                }
            }

            //Go find repo item
            var facet = new SearchFacet {SearchDepricatedItems = false, SearchLatestVersion = true };
            facet.SearchTargets.Add(DdiStringType.UserId);
            facet.SearchTerms.Add(userId);
            return Search(facet, processing).FirstOrDefault();
        }

        public IVersionable GetLatestItem(string urn)
        {
            var pieces = urn.Split(':');
            return GetLatestItem(Guid.Parse(pieces[1]), pieces[0]);
        }

        public IVersionable GetLatestItem(Guid id, string agency, ChildReferenceProcessing processing = ChildReferenceProcessing.InstantiateLatest)
        {
            var version = _client.GetLatestVersionNumber(id, agency);
            return GetItem(new IdentifierTriple(id, version, agency), processing);
        }

        public void PopulateChildren(IVersionable item)
        {
            foreach (var child in item.GetChildren())
            {
                if (!child.IsPopulated)
                {
                    item.ReplaceChild(child.CompositeId, GetItem(child.CompositeId));
                }
            }
        }

        public List<IVersionable> Search(SearchFacet facet, ChildReferenceProcessing processing = ChildReferenceProcessing.InstantiateLatest)
        {
            var response = _client.Search(facet);
            return response.Results.Select(result => GetItem(result.CompositeId, processing)).ToList();
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
                    bool isDirty = item.IsDirty;
                    _cache[parent.CompositeId].ReplaceChild(item.CompositeId, item);
                    item.IsDirty = isDirty;
                }
            }
        }

        private void FindChildren(IVersionable item)
        {
            foreach (var child in item.GetChildren())
            {
                AddParent(child.CompositeId, item);
                if (_cache.ContainsKey(child.CompositeId))
                {
                    bool isDirty = item.IsDirty;
                    item.ReplaceChild(child.CompositeId, _cache[child.CompositeId]);
                    item.IsDirty = isDirty;
                }
            }
        }

        private void MakeClean(IVersionable item)
        {
            item.IsDirty = false;
            foreach (var child in item.GetChildren())
            {
                MakeClean(child);
            }
        }
    }
}
