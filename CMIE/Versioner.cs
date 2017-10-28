using System.Collections.Generic;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;

namespace CMIE
{
    internal class Versioner
    {
        private readonly Dictionary<IVersionable, List<IVersionable>> _parents;
        private readonly List<IVersionable> _incremented;

        public Versioner()
        {
            _parents = new Dictionary<IVersionable, List<IVersionable>>();
            _incremented = new List<IVersionable>();
        }

        public void IncrementDityItemAndParents(IVersionable item)
        {
            Dig(item);
            var dirtyGthr = new DirtyItemGatherer();
            item.Accept(dirtyGthr);
            foreach (var dirtyItem in dirtyGthr.DirtyItems)
            {
                Increment(dirtyItem);
                var allParents = GetAllParents(dirtyItem);
                foreach (var parent in allParents)
                {
                    Increment(parent);
                }
            }
        }

        private void Dig(IVersionable item)
        {
            foreach (var child in item.GetChildren())
            {
                AddParent(child, item);
                Dig(child);
            }
        }

        private void AddParent(IVersionable item, IVersionable parent)
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

        private IEnumerable<IVersionable> GetAllParents(IVersionable item)
        {
            var output = new List<IVersionable>();

            if (!_parents.ContainsKey(item)) return output;
            
            foreach (var parent in _parents[item])
            {
                output.Add(parent);
                output.AddRange(GetAllParents(parent));
            }

            return output;
        }

        private void Increment(IVersionable item)
        {
            if (_incremented.Contains(item)) return;
            
            item.Version++;
            _incremented.Add(item);
        }
    }
}
