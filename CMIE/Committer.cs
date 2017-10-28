using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
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

    class AddItemsToCommit : IJob
    {
        private Repository Repository;
        private Utility.ObservableCollectionFast<IVersionable> ToBeCommitted;
        protected List<IVersionable> Items;

        public AddItemsToCommit(Repository repository, Utility.ObservableCollectionFast<IVersionable> toBeCommitted, List<IVersionable> items)
        {
            Repository = repository;
            ToBeCommitted = toBeCommitted;
            Items = items;
        }

        public virtual void Run()
        {
            var facet = new SetSearchFacet();
            facet.ReverseTraversal = true;
            var UpdatedItems = new List<IVersionable>();

            Repository.AddToCache(Items);

            foreach (var item in Items)
            {
                var response = Repository.SearchTypedSet(item.CompositeId, facet);
                foreach (var parent in response)
                {
                    if (parent.AgencyId == item.AgencyId && parent.Identifier == item.Identifier) continue;
                    UpdatedItems.Add(parent);
                    parent.IsDirty = true;
                }
                UpdatedItems.Add(item);
            }

            ToBeCommitted.AddRange(UpdatedItems.Distinct());
        }
    }

    class AddScopeToCommit : IJob
    {
        private string Host;
        private Utility.ObservableCollectionFast<IVersionable> ToBeCommitted;
        private Scope Scope;

        public AddScopeToCommit(string host, Utility.ObservableCollectionFast<IVersionable> toBeCommitted, Scope scope)
        {
            Host = host;
            ToBeCommitted = toBeCommitted;
            Scope = scope;
        }

        public void Run()
        {
            var client = Utility.GetClient(Host);
            foreach (var binding in Scope.GetBindings())
            {
                var parent = Utility.GetItem(client, binding.Item1);
                if (parent == default(IVersionable))
                {
                    throw new Exception(String.Format("Could not find parent ({0}) for scope '{1}'", binding.Item1, Scope.name));
                }
                else
                {
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

                        var localItem = ToBeCommitted.FirstOrDefault(x => x.AgencyId == remoteItem.AgencyId && x.Identifier == remoteItem.Identifier);
                        if (localItem == default(IVersionable))
                        {
                            set.Add(remoteItem);
                            ToBeCommitted.Add(remoteItem);

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
                                if (!child.IsPopulated)
                                {
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
                    }

                    var parentInSet = set.First(x => x.AgencyId == parent.AgencyId && x.Identifier == parent.Identifier);
                    parentInSet.AddChild(binding.Item2);
                    parentInSet.IsDirty = true;
                }
            }
            ToBeCommitted.AddRange(Scope.workingSet);
        }
    }

    class Committer
    {
        private EventManager EventManager;
        private string Host;
        private Repository Repository;
        private Utility.ObservableCollectionFast<IVersionable> ToBeCommitted;
        private Versioner Versioner;

        public Committer(EventManager eventManager, Repository repository, string host)
        {
            EventManager = eventManager;
            Host = host;
            Repository = repository;
            Reset();
            ToBeCommitted.CollectionChanged += CollectionUpdated;
        }

        public IJob AddToCommit(Scope scope)
        {
            return new AddScopeToCommit(Host, ToBeCommitted, scope);
        }

        public IJob AddToCommit(List<IVersionable> items)
        {
            return new AddItemsToCommit(Repository, ToBeCommitted, items);
        }

        public IJob AddToCommit(Func<List<IVersionable>> func)
        {
            return new AddFuncToCommit(Repository, ToBeCommitted, func);
        }

        public void Commit(string rationale = "")
        {
            var client = Utility.GetClient(Host);

            foreach (var item in ToBeCommitted.OfType<Algenta.Colectica.Model.Ddi.DdiInstance>())
            {
                Versioner.IncrementDityItemAndParents(item);
            }

            foreach (var item in ToBeCommitted.OfType<Algenta.Colectica.Model.Ddi.Group>())
            {
                Versioner.IncrementDityItemAndParents(item);
            }

            var options = new CommitOptions();
            options.VersionRationale["en-GB"] = rationale;
            client.RegisterItems(ToBeCommitted, options);
            Reset();
        }

        private void CollectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ToBeCommitted.Count > 0)
            {
                EventManager.FireEvent(new UpdateCommandEvent(UpdateCommandEvent.Actions.ADD, Console.Commands.COMMIT));
            }
            else
            {
                EventManager.FireEvent(new UpdateCommandEvent(UpdateCommandEvent.Actions.REMOVE, Console.Commands.COMMIT));
            }
        }

        private void Reset()
        {
            ToBeCommitted = new Utility.ObservableCollectionFast<IVersionable>();
            Versioner = new Versioner();
        }
    }
}
