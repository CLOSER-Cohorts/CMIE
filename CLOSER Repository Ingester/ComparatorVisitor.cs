using System.Collections.Generic;
using System.Linq;
using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using QuickGraph;

namespace CLOSER_Repository_Ingester
{
    public class ComparatorVisitor : VersionableVisitorBase
    {
        private enum Property
        {
            Label,
            Name
        };

        private List<IVersionable> other;
        private List<IVersionable> additions;
        private List<IVersionable> amendments;
        private List<IVersionable> removals;
        private bool same;

        public ComparatorVisitor(List<IVersionable> _other)
        {
            other = _other;
            additions = new List<IVersionable>();
            amendments = new List<IVersionable>();
            removals = new List<IVersionable>();
            removals.AddRange(other);
        }

        public bool Identical => additions.Count + amendments.Count + removals.Count == 0;

        public override void BeginVisitItem(IVersionable item)
        {
            base.BeginVisitItem(item);
            Compare(item);
        }

        private void Compare(IVersionable item)
        {
            var type = item.GetType();
            var found = other.Find(x => x.UserIds[0].ToString() == item.UserIds[0].ToString());

            if (found == default(IVersionable))
            {
                additions.Add(item);
                return;
            }
            removals.Remove(found);

            var good = true;
            good &= Compare<DescribableBase>(
                new string[] {"Label", "Name", "Description"},
                item,
                found
            );

        }

        private bool Compare<T>(string[] ps, IVersionable A, IVersionable B)
        {
            var type = A.GetType();
            if (type == typeof(T))
            {
                var a = (T) A;
                var b = (T) B;
                bool amendment = false;
                foreach (var prop in ps)
                {
                    var p = type.GetProperty(prop);
                    amendment |= p.GetValue(a, null) !=
                                 p.GetValue(b, null);
                }
                if (amendment)
                {
                    foreach (var prop in ps)
                    {
                        var p = type.GetProperty(prop);
                        p.SetValue(a, p.GetValue(b, null), null);
                    }
                    amendments.Add(A);
                }
                return true;
            }
            return false;
        }
    }
}