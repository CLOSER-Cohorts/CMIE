using System;
using System.Collections.Generic;
using System.Linq;
using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using QuickGraph;
using System.Reflection;

namespace CLOSER_Repository_Ingester
{
    public class Comparator
    {
        public List<IVersionable> amendments;

        public Comparator()
        {
            amendments = new List<IVersionable>();
        }


        public void Compare(IVersionable A, IVersionable B)
        {
            var type = A.GetType();

            var gathererA = new ItemGathererVisitor();
            var gathererB = new ItemGathererVisitor();
            A.Accept(gathererA);
            B.Accept(gathererB);

            var childrenA = gathererA.FoundItems.Where(x => x.UserIds.Count > 0).ToCollection();
            var childrenB = gathererB.FoundItems.Where(x => x.UserIds.Count > 0).ToCollection();

            foreach (var childA in childrenA)
            {
                var childB = childrenB.Where(x => x.UserIds[0].Identifier == childA.UserIds[0].Identifier).FirstOrDefault();
                if (childB != default(IVersionable))
                {
                    var amendmended = false;
                    amendmended |= Compare<DescribableBase>(
                        new string[] { "Label", "ItemName", "Description" },
                        childA,
                        childB
                    );
                    amendmended |= Compare<QuestionActivity>(
                        new string[] { "ResponseUnit" },
                        childA,
                        childB
                    );
                    if (amendmended)
                    {
                        amendments.Add(childA);
                    }
                }
            }
        }

        private bool Compare<T>(string[] ps, IVersionable A, IVersionable B)
        {
            var type = A.GetType();
            if (typeof(T).IsInstanceOfType(A))
            {
                var a = (T) A;
                var b = (T) B;
                bool amendment = false;
                foreach (var prop in ps)
                {
                    var p = type.GetProperty(prop);
                    amendment |= CompareProperty<T, MultilingualString>(p, "Best", a, b);
                    amendment |= CompareProperty<T, CodeValue>(p, "Value", a, b);
                }
                if (amendment)
                {
                    foreach (var prop in ps)
                    {
                        var p = type.GetProperty(prop);
                        UpdateProperty<T, MultilingualString, string, string>(p, a, b);
                        UpdateProperty<T, CodeValue>(p, a, b);
                    }
                    A.Version++;
                    return true;
                }
            }
            return false;
        }

        private bool CompareProperty<T,S>(PropertyInfo p, string subproperty, T a, T b)
        {
            if (p.PropertyType != typeof(S)) return false;
            var sp = typeof(S).GetProperty(subproperty);
            S va = (S)p.GetValue(a, null);
            S vb = (S)p.GetValue(b, null);
            return sp.GetValue(va, null).ToString() != sp.GetValue(vb, null).ToString();
        }

        private void UpdateProperty<T,S>(PropertyInfo p, T a, T b)
        {
            if (p.PropertyType != typeof(S)) return;
            p.SetValue(a, p.GetValue(b, null), null);
        }
        private void UpdateProperty<T, S, R>(PropertyInfo p, T a, T b) where S : ICollection<R>
        {
            if (p.PropertyType != typeof(S)) return;
            S va = (S)p.GetValue(a, null);
            S vb = (S)p.GetValue(b, null);
            va.Clear();
            foreach (var val in vb)
            {
                va.Add(val);
            }
        }
        private void UpdateProperty<T, S, R, Q>(PropertyInfo p, T a, T b) where S : ICollection<KeyValuePair<R, Q>>
        {
            if (p.PropertyType != typeof(S)) return;
            S va = (S)p.GetValue(a, null);
            S vb = (S)p.GetValue(b, null);
            va.Clear();
            foreach (var val in vb)
            {
                va.Add(new KeyValuePair<R, Q>(val.Key, val.Value));
            }
        }
    }
}