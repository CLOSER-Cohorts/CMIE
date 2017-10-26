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
    class AddToCommit : IJob
    {
        private string Host;
        private Utility.ObservableCollectionFast<IVersionable> ToBeCommitted;
        private Scope Scope;

        public AddToCommit(string host, Utility.ObservableCollectionFast<IVersionable> toBeCommitted, Scope scope)
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
        private Utility.ObservableCollectionFast<IVersionable> ToBeCommitted;
        private Versioner Versioner;

        public Committer(EventManager eventManager, string host)
        {
            EventManager = eventManager;
            Host = host;
            Reset();
            ToBeCommitted.CollectionChanged += CollectionUpdated;
        }

        public AddToCommit AddToCommit(Scope scope)
        {
            return new AddToCommit(Host, ToBeCommitted, scope);
        }

        public void Commit(string rationale = "")
        {
            var client = Utility.GetClient(Host);

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
