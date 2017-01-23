using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using System;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;

namespace CLOSER_Repository_Ingester
{
    public class Comparator
    {
        public List<IVersionable> amendments;
        public List<IVersionable> repoSet;

        public Comparator(List<IVersionable> _repoSet)
        {
            amendments = new List<IVersionable>();
            repoSet = _repoSet;
        }

        public int Compare(IVersionable A, IVersionable B)
        {
            var gathererA = new ItemGathererVisitor();
            var gathererB = new ItemGathererVisitor();
            A.Accept(gathererA);
            B.Accept(gathererB);

            var childrenA = gathererA.FoundItems.Where(x => x.UserIds.Count > 0).ToCollection();
            var childrenB = gathererB.FoundItems.Where(x => x.UserIds.Count > 0).ToCollection();

            int compared = 0;

            foreach (var childA in childrenA)
            {
                compared++;

                var childB = childrenB.FirstOrDefault(x => x.UserIds[0].Identifier == childA.UserIds[0].Identifier);
                if (childB != default(IVersionable))
                {
                    var amendmended = false;
                    if (childB.ItemType == DdiItemType.Loop)
                    {
                        bool stop = true;
                    }
                    amendmended |= Compare<DescribableBase>(
                        new[] { "Label", "ItemName", "Description" },
                        childA,
                        childB
                    );
                    amendmended |= Compare<QuestionActivity>(
                        new[] { "ResponseUnit", "Question", "QuestionGrid" },
                        childA,
                        childB
                    );
                    amendmended |= Compare<CustomLoopActivity>(
                        new[] { "Condition", "InitialValue" },
                        childA,
                        childB
                    );
                    amendmended |= Compare<CustomIfElseActivity>(
                        new[] { "Branches" },
                        childA,
                        childB
                    );
                    var question_properties = new[] { "EstimatedTime", "QuestionIntent", "QuestionText", "ResponseDomains" };
                    amendmended |= Compare<Question>(
                        question_properties,
                        childA,
                        childB
                    );
                    var qgrid_properties = new[] { "Dimensions" };
                    amendmended |= Compare<QuestionGrid>(
                        question_properties.Concat(qgrid_properties).ToArray(),
                        childA,
                        childB
                    );
                    amendmended |= Compare<InterviewerInstruction>(
                        new[] { "Instructions" },
                        childA,
                        childB
                    );
                    amendmended |= Compare<StatementActivity>(
                        new[] { "EstimatedTime",  "StatementText" },
                        childA,
                        childB
                    );
                    amendmended |= Compare<CodeList>(
                        new[] { "Codes" },
                        childA,
                        childB
                    );
                    amendmended |= Compare<Variable>(
                        new[] { "SourceQuestions", "SourceQuestionGrids", "SourceVariables" },
                        childA,
                        childB
                    );

                    if (childA is CustomSequenceActivity)
                    {
                        var seqA = (CustomSequenceActivity)childA;
                        var seqB = (CustomSequenceActivity)childB;

                        var ccsA = seqA.GetChildren();
                        var ccsB = seqB.GetChildren();

                        var reorderControlConstructs = false;

                        if (ccsA.Count != ccsB.Count) 
                            reorderControlConstructs = true;
                        else
                        {
                            for (var i = 0; i < ccsA.Count; i++)
                            {
                                if (ccsA[i].UserIds[0].Identifier != ccsB[i].UserIds[0].Identifier)
                                {
                                    reorderControlConstructs = true;
                                    break;
                                }
                            }
                        }
                        if (reorderControlConstructs)
                        {
                            foreach (var cc in ccsA)
                            {
                                seqA.RemoveChild(cc.CompositeId);
                            }
                            for (var i = 0; i < ccsB.Count; i++)
                            {
                                var tmp = repoSet.Where(x => x.UserIds.Count > 0).ToList();
                                IVersionable found = tmp.FirstOrDefault(x => x.UserIds[0].Identifier == ccsB[i].UserIds[0].Identifier);
                                seqA.AddChild(found);
                            }
                            childA.IsDirty = true;
                            amendmended = true;
                        }
                    }

                    if (amendmended)
                    {
                        amendments.Add(childA);
                    }
                }
            }
            return compared;
        }

        private bool Compare<T>(string[] ps, object A, object B)
        {
            if (A == null && B == null) return false;
            var type = A.GetType();
            if (A is T)
            {
                var a = (T) A;
                var b = (T) B;
                bool amendment = false, amendedReference = false, good = false;
                foreach (var prop in ps)
                {
                    var p = type.GetProperty(prop);
                    if (p.PropertyType == typeof(string))
                    {
                        amendment |= !(((string)p.GetValue(a, null) == null ^ (string)p.GetValue(b, null) == null) ^ String.Equals((string)p.GetValue(a, null), (string)p.GetValue(b, null)));
                    } 
                    else if (p.PropertyType.IsPrimitive || (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        try
                        {
                            amendment |= !p.GetValue(a, null).Equals(p.GetValue(b, null));
                        } catch (NullReferenceException e) {
                            amendment |= p.GetValue(a, null) == null ^ p.GetValue(b, null) == null;
                        }
                    }
                    else
                    {
                        amendment |= CompareProperty<T, MultilingualString>(p, "Best", a, b);
                        amendment |= CompareProperty<T, CodeValue>(p, "Value", a, b);
                        amendment |= CompareComplexProperty<T, QuestionRoster>(p, a, b);
                        amendment |= CompareComplexProperty<T, Condition>(p, a, b);
                        amendment |= CompareComplexPropertyCollection<T, ObservableCollection<ResponseDomain>, ResponseDomain>(p, a, b);
                        amendment |= CompareComplexPropertyCollection<T, ObservableCollection<Code>, Code>(p, a, b);
                        amendment |= CompareComplexPropertyCollection<T, ObservableCollection<QuestionGridDimension>, QuestionGridDimension>(p, a, b);
                        amendment |= CompareComplexPropertyCollection<T, SourceCodeCollection, SourceCode>(p, a, b);
                        amendment |= CompareComplexPropertyCollection<T, ObservableCollection<CustomIfElseBranchActivity>, CustomIfElseBranchActivity>(p, a, b);
                        
                        amendedReference |= CompareReferenceProperty<T>(p, a, b);
                        amendedReference |= CompareReferenceProperty<T, ObservableCollection<Question>, Question>(p, a, b);
                        amendedReference |= CompareReferenceProperty<T, ObservableCollection<QuestionGrid>, QuestionGrid>(p, a, b);
                        amendedReference |= CompareReferenceProperty<T, ObservableCollection<Variable>, Variable>(p, a, b);
                    }
                }
                if (amendment || amendedReference)
                {
                    if (amendment)
                    {
                        foreach (var prop in ps)
                        {
                            var p = type.GetProperty(prop);
                            UpdateProperty<T, MultilingualString, string, string>(p, a, b);
                            UpdateProperty<T, CodeValue>(p, a, b);
                            UpdateProperty<T, string>(p, a, b);
                            UpdateProperty<T, bool>(p, a, b);
                            UpdateProperty<T, Nullable<int>>(p, a, b);
                            UpdateProperty<T, Nullable<decimal>>(p, a, b);
                        }
                    }
                    if (amendedReference)
                    {
                        foreach (var prop in ps)
                        {
                            var p = type.GetProperty(prop);
                            UpdateReferenceProperty<T>(p, a, b);
                            UpdateReferenceProperty<T, ObservableCollection<Question>, Question>(p, a, b);
                            UpdateReferenceProperty<T, ObservableCollection<QuestionGrid>, QuestionGrid>(p, a, b);
                            UpdateReferenceProperty<T, ObservableCollection<Variable>, Variable>(p, a, b);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool Compare<T>(string[] ps, IVersionable A, IVersionable B)
        {
            if (Compare<T>(ps, (object)A, (object)B))
            {
                A.IsDirty = true;
                return true;
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

        private bool CompareReferenceProperty<T>(PropertyInfo p, T a, T b)
        {
            if (!typeof(IdentifiableBase).IsAssignableFrom(p.PropertyType)) return false;
            IdentifiableBase va = (IdentifiableBase)p.GetValue(a, null);
            IdentifiableBase vb = (IdentifiableBase)p.GetValue(b, null);
            if (va == null && vb == null) return false;
            if (!(va.UserIds.Any() && vb.UserIds.Any())) return false;
            return va.UserIds.First().Identifier != vb.UserIds.First().Identifier;
        }

        private bool CompareReferenceProperty<T, S, R>(PropertyInfo p, T a, T b) where R : IIdentifiable
        {
            if (p.PropertyType != typeof(S)) return false;
            var va = (Collection<R>)p.GetValue(a, null);
            var vb = (Collection<R>)p.GetValue(b, null);
            if (va.Count != vb.Count) return true;
            bool amendmended = false;
            for (var i = 0; i < va.Count; i++)
            {
                if (va[i] == null && vb[i] == null) continue;
                if (!(va[i].UserIds.Any() && vb[i].UserIds.Any())) continue;
                amendmended |= va[i].UserIds.First().Identifier != vb[i].UserIds.First().Identifier;
            }
            return amendmended;
        }

        private bool CompareComplexProperties<T>(T va, T vb)
        {
            bool amendmended = false;
            amendmended |= Compare<TextDomain>(new[] { "Label", "MaxLength" }, va, vb);
            amendmended |= Compare<NumericDomain>(new[] { "Label", "NumericType", "Low", "High" }, va, vb);
            amendmended |= Compare<DateTimeDomain>(new[] { "Label", "DateTimeType", "DateFormat" }, va, vb);
            amendmended |= Compare<Code>(new[] { "Value", "Category" }, va, vb);
            amendmended |= Compare<QuestionGridDimension>(new[] { "ShouldCodeBeDisplayed", "ShouldLabelBeDisplayed", "Roster", "CodeDomain" }, va, vb);
//            amendmended |= Compare<QuestionRoster>(new[] { "Label", "ShouldLabelBeDisplayed", "Roster" }, va, vb);
            amendmended |= Compare<Condition>(new[] { "Description", "SourceCodeExpressions" }, va, vb);
            amendmended |= Compare<SourceCode>(new[] { "Code", "Language" }, va, vb);
            amendmended |= Compare<CustomIfElseBranchActivity>(new[] { "Condition" }, va, vb);
            var question_properties = new[] { "EstimatedTime", "QuestionIntent", "QuestionText", "ResponseDomains" };
            amendmended |= Compare<Question>(question_properties, va, vb);
            var qgrid_properties = new[] { "Dimensions" };
            amendmended |= Compare<QuestionGrid>(question_properties.Concat(qgrid_properties).ToArray(), va, vb);
            amendmended |= Compare<Variable>(new[] {"SourceQuestions", "SourceQuestionGrids", "SourceVariables"}, va, vb);
            return amendmended;
        }

        private bool CompareComplexProperty<T,S>(PropertyInfo p, T a, T b)
        {
            if (p.PropertyType != typeof(S)) return false;
            S va = (S)p.GetValue(a, null);
            S vb = (S)p.GetValue(b, null);

            return CompareComplexProperties(va, vb);
        }

        private bool CompareComplexPropertyCollection<T,S,R>(PropertyInfo p, T a, T b)
        {
            if (p.PropertyType != typeof(S)) return false;
            Collection<R> va = (Collection<R>)p.GetValue(a, null);
            Collection<R> vb = (Collection<R>)p.GetValue(b, null);
            if (va.Count != vb.Count) return true;
            bool amendmended = false;
            for (var i = 0; i < va.Count; i++)
            {
                amendmended |= CompareComplexProperties(va[i], vb[i]);
            }
            return amendmended;
        }

        private void UpdateReferenceProperty<T>(PropertyInfo p, T a, T b)
        {
            if (!typeof(IdentifiableBase).IsAssignableFrom(p.PropertyType)) return;
            IdentifiableBase vb = (IdentifiableBase)p.GetValue(b, null);
            if (vb == null) return;
            if (!(vb.UserIds.Any())) return;
            var tmp = repoSet.Where(x => x.UserIds.Count > 0).ToList();
            IVersionable found = tmp.FirstOrDefault(x => x.UserIds[0].Identifier == vb.UserIds[0].Identifier);
            p.SetValue(a, found, null);
        }

        private void UpdateReferenceProperty<T, S, R>(PropertyInfo p, T a, T b) where R : VersionableBase
        {
            if (p.PropertyType != typeof(S)) return;
            var va = (Collection<R>)p.GetValue(a, null);
            var vb = (Collection<R>)p.GetValue(b, null);
            for (var i = 0; i < va.Count; i++)
            {
                ((IVersionable)va[i]).IsDirty = true;
                ((IVersionable)a).RemoveChild(((VersionableBase)va[i]).CompositeId);
            }
            if (!vb.Any()) return;
            var tmp = repoSet.Where(x => x.UserIds.Count > 0).ToList();
            for (var i = 0; i < vb.Count; i++)
            {
                IVersionable found = tmp.FirstOrDefault(x => x.UserIds[0].Identifier == vb[i].UserIds[0].Identifier);
                found.IsDirty = true;
                va.Add((R)found);
            }
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