using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;

using CMIE.ControllerSystem;
using CMIE.Events;

namespace CMIE
{
    class AddFuncToCommit : AddItemsToCommit
    {
        protected Func<List<IVersionable>> Func;
        public AddFuncToCommit(Repository repository, Utility.ObservableCollectionFast<IVersionable> toBeCommitted, Func<List<IVersionable>> func) : 
            base(repository, toBeCommitted, new List<IVersionable>())
        {
            Func = func;
        }

        public override void Run()
        {
            Items = Func();
            base.Run();
        }
    }

    internal class AddItemsToCommit : IJob
    {
        private readonly Repository _repository;
        private readonly Utility.ObservableCollectionFast<IVersionable> _toBeCommitted;
        protected List<IVersionable> Items;

        public AddItemsToCommit(Repository repository, Utility.ObservableCollectionFast<IVersionable> toBeCommitted, List<IVersionable> items)
        {
            _repository = repository;
            _toBeCommitted = toBeCommitted;
            Items = items;
        }

        public virtual void Run()
        {
            var facet = new SetSearchFacet();
            facet.ReverseTraversal = true;
            var updatedItems = new List<IVersionable>();

            foreach (var item in _repository.AddToCache(Items))
            {
                var response = _repository.SearchTypedSet(item.CompositeId, facet);
                foreach (var parent in response)
                {
                    if (parent.AgencyId == item.AgencyId && parent.Identifier == item.Identifier) continue;
                    updatedItems.Add(parent);
                }
                updatedItems.Add(item);
            }

            _toBeCommitted.AddRange(updatedItems.Distinct());
        }
    }

    internal class AddScopeToCommit : IJob
    {
        private readonly string _host;
        private readonly Utility.ObservableCollectionFast<IVersionable> _toBeCommitted;
        private readonly Scope _scope;

        public AddScopeToCommit(string host, Utility.ObservableCollectionFast<IVersionable> toBeCommitted, Scope scope)
        {
            _host = host;
            _toBeCommitted = toBeCommitted;
            _scope = scope;
        }

        public void Run()
        {
            var client = Utility.GetClient(_host);
            foreach (var binding in _scope.GetBindings())
            {
                try
                {
                    var parent = Utility.GetItem(client, binding.Item1);
                    var facet = new SetSearchFacet();
                    facet.ReverseTraversal = true;
                    var typedIds = client.SearchTypedSet(parent.CompositeId, facet);
                    var ids = new Collection<IdentifierTriple>();
                    foreach (var typedId in typedIds)
                    {
                        ids.Add(typedId.CompositeId);
                    }
                    var remoteSet = client.GetItems(ids).ToList();
                    var set = new Collection<IVersionable>();
                    while (remoteSet.Any())
                    {
                        var remoteItem = remoteSet[0];
                        remoteSet.RemoveAt(0);

                        var localItem = _toBeCommitted.FirstOrDefault(x => x.AgencyId == remoteItem.AgencyId && x.Identifier == remoteItem.Identifier);
                        if (localItem == default(IVersionable))
                        {
                            set.Add(remoteItem);
                            _toBeCommitted.Add(remoteItem);

                            foreach (var child in remoteItem.GetChildren())
                            {
                                if (!child.IsPopulated)
                                {
                                    var localChild = set.FirstOrDefault(x => x.AgencyId == child.AgencyId && x.Identifier == child.Identifier);
                                    if (localChild != default(IVersionable))
                                    {

                                        remoteItem.ReplaceChild(child.CompositeId, localChild);
                                    }
                                }
                            }
                        }
                        else
                        {
                            set.Add(localItem);
                            foreach (var child in localItem.GetChildren())
                            {
                                if (child.IsPopulated) continue;
                                var remoteChild = remoteSet.FirstOrDefault(x => x.AgencyId == child.AgencyId && x.Identifier == child.Identifier);
                                if (remoteChild != default(IVersionable))
                                {
                                    remoteSet.Remove(remoteChild);
                                    localItem.ReplaceChild(child.CompositeId, remoteChild);
                                    continue;
                                }
                                var localChild = set.FirstOrDefault(x => x.AgencyId == child.AgencyId && x.Identifier == child.Identifier);
                                if (localChild != default(IVersionable))
                                {

                                    localItem.ReplaceChild(child.CompositeId, localChild);
                                }
                            }
                        }

                    }

                    var parentInSet = set.First(x => x.AgencyId == parent.AgencyId && x.Identifier == parent.Identifier);
                    parentInSet.AddChild(binding.Item2);
                    parentInSet.IsDirty = true;
                } 
                catch (Exception e)
                {
                    throw new Exception(string.Format("Could not find parent ({0}) for scope '{1}'",binding.Item1,_scope.name));
                }
            }
            _toBeCommitted.AddRange(_scope.WorkingSet);
        }
    }

    internal class Committer
    {
        private readonly EventManager _eventManager;
        private readonly string _host;
        private readonly Repository _repository;
        private Utility.ObservableCollectionFast<IVersionable> _toBeCommitted;
        private Versioner _versioner;

        public Committer(EventManager eventManager, Repository repository, string host)
        {
            _eventManager = eventManager;
            _host = host;
            _repository = repository;
            Reset();
            _toBeCommitted.CollectionChanged += CollectionUpdated;
        }

        public IJob AddToCommit(Scope scope)
        {
            return new AddScopeToCommit(_host, _toBeCommitted, scope);
        }

        public IJob AddToCommit(List<IVersionable> items)
        {
            return new AddItemsToCommit(_repository, _toBeCommitted, items);
        }

        public IJob AddToCommit(Func<List<IVersionable>> func)
        {
            return new AddFuncToCommit(_repository, _toBeCommitted, func);
        }

        public void Commit(string rationale = "")
        {
            var client = Utility.GetClient(_host);

            foreach (var item in _toBeCommitted)
            {
                _versioner.IncrementItemAndParents(item);
            }

            var options = new CommitOptions();
            options.VersionRationale["en-GB"] = rationale;

            for (var i = 0; i < _toBeCommitted.Count; i++)
            {
                if (_toBeCommitted[i].AgencyId == default(string) && i > 0)
                {
                    _toBeCommitted[i].AgencyId = _toBeCommitted[i-1].AgencyId;
                }
            }

            if (Confirm())
            {
                Logger.Instance.Log.InfoFormat("Commiting {0} to the repository ({1})", _toBeCommitted.Count, _host);
                
                
                
                client.RegisterItems(_toBeCommitted, options);
                Logger.Instance.Log.InfoFormat("The commit completed successfully.");
                Reset();
            }
            else
            {
                System.Console.WriteLine("Commit cancelled. You can still resume the process by entering 'commit' again.");
            }
        }

        private void CollectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            _eventManager.FireEvent(_toBeCommitted.Count > 0
                ? new UpdateCommandEvent(UpdateCommandEvent.Actions.ADD, Console.Commands.COMMIT)
                : new UpdateCommandEvent(UpdateCommandEvent.Actions.REMOVE, Console.Commands.COMMIT));
        }

        private bool Confirm()
        {
            System.Console.WriteLine(
                "Are you sure you would like to commit {0} items to the repository ({1})? (y/n)", 
                _toBeCommitted.Distinct().Count(), 
                _host
                );
            var key = System.Console.ReadKey().KeyChar.ToString().ToLower();
            System.Console.WriteLine("");
            if (key == "y")
            {
                return true;
            }
            else if (key == "n")
            {
                return false;
            }
            else
            {
                return Confirm();
            }
        }

        private void Reset()
        {
            _toBeCommitted = new Utility.ObservableCollectionFast<IVersionable>();
            _versioner = new Versioner();
        }
    }
}
