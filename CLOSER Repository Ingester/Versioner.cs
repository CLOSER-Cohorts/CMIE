using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;

namespace CLOSER_Repository_Ingester
{
    class Versioner
    {
        private Dictionary<IVersionable, List<IVersionable>> parents;
        private List<IVersionable> incremented;

        public Versioner()
        {
            parents = new Dictionary<IVersionable, List<IVersionable>>();
            incremented = new List<IVersionable>();
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
            if (!parents.ContainsKey(item))
            {
                parents[item] = new List<IVersionable>();
            }
            if (!parents[item].Contains(parent))
            {
                parents[item].Add(parent);
            }
        }

        private List<IVersionable> GetAllParents(IVersionable item)
        {
            var output = new List<IVersionable>();

            if (parents.ContainsKey(item))
            {
                foreach (var parent in parents[item])
                {
                    output.Add(parent);
                    output.AddRange(GetAllParents(parent));
                }
            }

            return output;
        }

        private void Increment(IVersionable item)
        {
            if (!incremented.Contains(item))
            {
                item.Version++;
                incremented.Add(item);
            }
        }
    }
}
