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

using CMIE.Events;
using CMIE.ControllerSystem;
using CMIE.ControllerSystem.Actions;

namespace CMIE
{
    class QvMapping : IJob
    {
        private List<IVersionable> UpdatedItems;
        private Repository Repository;
        private Scope Scope;

        public QvMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            UpdatedItems = updatedItems;
            Repository = repository;
            Scope = scope;
        }

        public void Run()
        {
            var qvActions = Scope.actions.OfType<LoadQVMapping>();
            foreach (var action in qvActions)
            {
                UpdatedItems.AddRange(action.Build(Repository));
            }
        }
    }

    class DvMapping : IJob
    {
        private EventManager EventManager;
        private List<IVersionable> UpdatedItems;
        private Repository Repository;
        private Scope Scope;

        public DvMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            UpdatedItems = updatedItems;
            Repository = repository;
            Scope = scope;
        }

        public void Run()
        {
            var qvActions = Scope.actions.OfType<LoadDVMapping>();
            foreach (var action in qvActions)
            {
                UpdatedItems.AddRange(action.Build(Repository));
            }
        }
    }

    class RvMapping : IJob
    {
        private EventManager EventManager;
        private List<IVersionable> UpdatedItems;
        private Repository Repository;
        private Scope Scope;

        public RvMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            UpdatedItems = updatedItems;
            Repository = repository;
            Scope = scope;
        }

        public void Run()
        {
            var rvActions = Scope.actions.OfType<LoadRVMapping>();
            foreach (var action in rvActions)
            {
                UpdatedItems.AddRange(action.Build(Repository));
            }
        }
    }

    class Mapper
    {
        private List<IVersionable> UpdatedItems;
        private Repository Repository;

        public Mapper(Repository repository)
        {
            UpdatedItems = new List<IVersionable>();
            Repository = repository;
        }

        public IJob QV(Scope scope)
        {
            return new QvMapping(UpdatedItems, Repository, scope);
        }

        public IJob DV(Scope scope)
        {
            return new DvMapping(UpdatedItems, Repository, scope);
        }

        public IJob RV(Scope scope)
        {
            return new RvMapping(UpdatedItems, Repository, scope);
        }

        public List<IVersionable> Clear()
        {
            var items = new List<IVersionable>();
            while (UpdatedItems.Any())
            {
                items.Add(UpdatedItems[0]);
                UpdatedItems.RemoveAt(0);
            }
            return items;
        }
    }
}
