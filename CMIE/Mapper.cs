using System.Collections.Generic;
using System.Linq;

using Algenta.Colectica.Model;

using CMIE.Events;
using CMIE.ControllerSystem;
using CMIE.ControllerSystem.Actions;

namespace CMIE
{
    internal class QvMapping : IJob
    {
        private readonly List<IVersionable> _updatedItems;
        private readonly Repository _repository;
        private readonly Scope _scope;

        public QvMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            _updatedItems = updatedItems;
            _repository = repository;
            _scope = scope;
        }

        public void Run()
        {
            var qvActions = _scope.Actions.OfType<LoadQVMapping>();
            foreach (var action in qvActions)
            {
                _updatedItems.AddRange(action.Build(_repository));
            }
        }
    }

    internal class DvMapping : IJob
    {
        private readonly List<IVersionable> _updatedItems;
        private readonly Repository _repository;
        private readonly Scope _scope;

        public DvMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            _updatedItems = updatedItems;
            _repository = repository;
            _scope = scope;
        }

        public void Run()
        {
            var qvActions = _scope.Actions.OfType<LoadDVMapping>();
            foreach (var action in qvActions)
            {
                _updatedItems.AddRange(action.Build(_repository));
            }
        }
    }

    internal class TqMapping : IJob
    {
        private readonly List<IVersionable> _updatedItems;
        private readonly Repository _repository;
        private readonly Scope _scope;

        public TqMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            _updatedItems = updatedItems;
            _repository = repository;
            _scope = scope;
        }

        public void Run()
        {
            var tqActions = _scope.Actions.OfType<LoadTQLinking>();
            foreach (var action in tqActions)
            {
                _updatedItems.AddRange(action.Build(_repository));
            }
        }
    }

    internal class TvMapping : IJob
    {
        private readonly List<IVersionable> _updatedItems;
        private readonly Repository _repository;
        private readonly Scope _scope;

        public TvMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            _updatedItems = updatedItems;
            _repository = repository;
            _scope = scope;
        }

        public void Run()
        {
            var tvActions = _scope.Actions.OfType<LoadTVLinking>();
            foreach (var action in tvActions)
            {
                _updatedItems.AddRange(action.Build(_repository));
            }
        }
    }

    internal class RvMapping : IJob
    {
        private readonly List<IVersionable> _updatedItems;
        private readonly Repository _repository;
        private readonly Scope _scope;

        public RvMapping(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            _updatedItems = updatedItems;
            _repository = repository;
            _scope = scope;
        }

        public void Run()
        {
            var rvActions = _scope.Actions.OfType<LoadRVMapping>();
            foreach (var action in rvActions)
            {
                _updatedItems.AddRange(action.Build(_repository));
            }
        }
    }

    internal class QbLinking : IJob
    {
        private readonly List<IVersionable> _updatedItems;
        private readonly Repository _repository;
        private readonly Scope _scope;

        public QbLinking(List<IVersionable> updatedItems, Repository repository, Scope scope)
        {
            _updatedItems = updatedItems;
            _repository = repository;
            _scope = scope;
        }

        public void Run()
        {
            var qbActions = _scope.Actions.OfType<LoadQBLinking>();
            foreach (var action in qbActions)
            {
                _updatedItems.AddRange(action.Build(_repository));
            }
        }
    }

    internal class Mapper
    {
        private readonly List<IVersionable> _updatedItems;
        private readonly Repository _repository;

        public Mapper(Repository repository)
        {
            _updatedItems = new List<IVersionable>();
            _repository = repository;
        }

        public IJob QV(Scope scope)
        {
            return new QvMapping(_updatedItems, _repository, scope);
        }

        public IJob DV(Scope scope)
        {
            return new DvMapping(_updatedItems, _repository, scope);
        }

        public IJob TQ(Scope scope)
        {
            return new TqMapping(_updatedItems, _repository, scope);
        }

        public IJob TV(Scope scope)
        {
            return new TvMapping(_updatedItems, _repository, scope);
        }

        public IJob RV(Scope scope)
        {
            return new RvMapping(_updatedItems, _repository, scope);
        }

        public IJob QB(Scope scope)
        {
            return new QbLinking(_updatedItems, _repository, scope);
        }

        public List<IVersionable> Clear()
        {
            var items = new List<IVersionable>();
            while (_updatedItems.Any())
            {
                items.Add(_updatedItems[0]);
                _updatedItems.RemoveAt(0);
            }
            return items;
        }
    }
}
