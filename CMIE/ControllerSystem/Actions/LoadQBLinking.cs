using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Actions
{
    class LoadQBLinking : TXTFileAction
    {
        private SearchFacet Facet;
        private Dictionary<string, IdentifierTriple> QuestionConstructSchemeCache;
        protected override int[] numberOfColumns
        {
            get { return new int[]{4}; }
        }
        public LoadQBLinking(string _filepath)
            : base(_filepath) 
        {
            QuestionConstructSchemeCache = new Dictionary<string, IdentifierTriple>();
        }

        protected override void RunFile(Action<string[]> _runner)
        {
            Facet = new SearchFacet();
            Facet.ItemTypes.Add(DdiItemType.QuestionConstruct);
            Facet.SearchTargets.Add(DdiStringType.Name);
            Facet.SearchLatestVersion = true;
            base.RunFile(_runner);
        }

        public override void Runner(string[] parts)
        {
            string baseQuestion, derivedQuestion;

            baseQuestion = parts[1].Trim();
            derivedQuestion = parts[3].Trim();

            if (baseQuestion == "0" || derivedQuestion == "0") return;

            Facet.SearchSets.Clear();
            Facet.SearchTerms.Clear();

            List<IVersionable> bases, deriveds;

            var bsId = GetControlConstructScheme(parts[0].Trim());
            if (bsId == default(IdentifierTriple))
            {
                Logger.Instance.Log.ErrorFormat("ControlConstructScheme '{0}' could not be found in the repository.", parts[0]);
                counter[Counters.Skipped] += 1;
                return;
            }
            var dsId = GetControlConstructScheme(parts[2].Trim());
            if (dsId == default(IdentifierTriple))
            {
                Logger.Instance.Log.ErrorFormat("ControlConstructScheme '{0}' could not be found in the repository.", parts[2]);
                counter[Counters.Skipped] += 1;
                return;
            }

            Facet.SearchSets.Add(bsId);
            Facet.SearchTerms.Add(baseQuestion);
            bases = Repository.Search(Facet);
            Facet.SearchSets.Clear();
            Facet.SearchTerms.Clear();
            Facet.SearchSets.Add(dsId);
            Facet.SearchTerms.Add(derivedQuestion);
            deriveds = Repository.Search(Facet);

            if (bases.Count != 1)
            {
                if (bases.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No variable was found named '{0}' within the scope. Please check {1}", baseQuestion, filepath);
                }
                else
                {
                    Logger.Instance.Log.ErrorFormat("{0} questions were found named '{1}' within the scope. Please check {2}", bases.Count, baseQuestion, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }

            if (deriveds.Count != 1)
            {
                if (deriveds.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No variable was found named '{0}' within the scope. Please check {1}", derivedQuestion, filepath);
                }
                else
                {
                    Logger.Instance.Log.ErrorFormat("{0} variables were found named '{1}' within the scope. Please check {2}", deriveds.Count, derivedQuestion, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }

            var baseQc = bases.First() as QuestionActivity;
            var derivedQc = deriveds.First() as QuestionActivity;

            derivedQc.Question.BasedOn.Items.Add(new TypedIdTriple(baseQc.Question.CompositeId, DdiItemType.QuestionItem));

            UpdatedItems.Add(derivedQc);
        }

        private IdentifierTriple GetControlConstructScheme(string name)
        {
            if (QuestionConstructSchemeCache.ContainsKey(name))
            {
                return QuestionConstructSchemeCache[name];
            }
            var result = GetItemByTypeAndName(DdiItemType.ControlConstructScheme, name);
            if (result == default(IVersionable))
            {
                return default(IdentifierTriple);
            }
            else
            {
                QuestionConstructSchemeCache[name] = result.CompositeId;
                return result.CompositeId;
            }
        }
    }
}
