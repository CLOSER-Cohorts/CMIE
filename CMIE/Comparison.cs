using System.Collections.Generic;
using System.Linq;

using SysCon = System.Console;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Utility;

using CMIE.Events;
using CMIE.ControllerSystem;

namespace CMIE
{
    internal class Comparison : IJob
    {
        private readonly EventManager _eventManager;
        private Repository _repository;
        private Scope _scope;
        private readonly string _host;
        private RepositoryClientBase _client;
        private readonly List<IVersionable> _updatedItems;

        public Comparison(EventManager eventManager, Scope scope, Repository repository)
        {
            _eventManager = eventManager;
            _scope = scope;
            _repository = repository;
            _updatedItems = new List<IVersionable>();
        }

        public void Run()
        {
            foreach (var bindingPoint in _scope.GetBindingPoints())
            {

                var repoVersion = _repository.GetItem(bindingPoint.Key);

                var comparator = new Comparator(_repository, _updatedItems);
                Compare(comparator, repoVersion, bindingPoint.Value);
            }

            Logger.Instance.Log.InfoFormat("Comparison compeleted for {0}, {1} items updated.", _scope.name, _updatedItems.Distinct().Count());
            _eventManager.FireEvent(new JobCompletedEvent(JobCompletedEvent.JobType.COMPARISON));
        }

        private void Compare(Comparator comparator, IVersionable A, IVersionable B)
        {
            comparator.Compare(A, B);

            if (A.ItemType == DdiItemType.LogicalProduct)
                SysCon.WriteLine("");

            foreach (var childA in A.GetChildren())
            {
                if (!childA.IsPopulated)
                {
                    A.ReplaceChild(childA.CompositeId, _repository.GetItem(
                            childA.CompositeId
                        )
                    );
                }
            }

            foreach (var childB in B.GetChildren())
            {
                if (childB.IsPopulated)
                {
                    //var childA = _repository.GetItem(childB.UserIds[0].Identifier, ChildReferenceProcessing.PopulateLatest);
                    IVersionable childA = default(IVersionable);
                    foreach (var child in A.GetChildren())
                    {
                        if (child.UserIds.Count == 0)
                        {
                            Logger.Instance.Log.ErrorFormat("Could not update child (urn: {0}) from parent (urn: {1}, closer-id: {2})", child.CompositeId, A.CompositeId, A.UserIds[0].Identifier);
                        }else if (child.UserIds[0].Identifier == childB.UserIds[0].Identifier)
                        {
                            childA = child;
                            break;
                        }
                    }
                    if (childA == default(IVersionable))
                    {
                        if (A.ChildTypesAccepted.Contains(childB.ItemType))
                        {
                            A.AddChild(childB);
                        }
                        _repository.AddToCache(childB);
                        _updatedItems.Add(childB);
                    }
                    else
                    {
                        Compare(comparator, childA, childB);
                    }
                }
            }
        }

        public List<IVersionable> GetUpdatedItems()
        {
            return _updatedItems;
        }
    }
}
