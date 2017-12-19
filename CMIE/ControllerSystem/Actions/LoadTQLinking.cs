using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Actions
{
    class LoadTQLinking : TXTFileAction
    {
        private SearchFacet Facet;
        private SetSearchFacet ccgFacet;
        private Dictionary<string, IdentifierTriple> ControlConstructGroupCache;
        private Dictionary<string, IdentifierTriple> ControlConstructSchemeCache;

        protected override int[] numberOfColumns
        {
            get { return new int[]{2,3}; }
        }

        public LoadTQLinking(string _filepath) : base(_filepath) 
        {
            ControlConstructGroupCache = new Dictionary<string, IdentifierTriple>();
            ControlConstructSchemeCache = new Dictionary<string, IdentifierTriple>();
        }

        protected override void RunFile(Action<string[]> _runner)
        {
            Facet = new SearchFacet();
            Facet.ItemTypes.Add(DdiItemType.QuestionConstruct);
            Facet.SearchTargets.Add(DdiStringType.Name);
            Facet.SearchLatestVersion = true;
            ccgFacet = new SetSearchFacet();
            ccgFacet.ItemTypes.Add(DdiItemType.ControlConstructGroup);
            ccgFacet.LeafItemTypes.Add(DdiItemType.ControlConstructGroup);
            ccgFacet.ReverseTraversal = true;
            base.RunFile(_runner);
        }    

        public override void Runner(string[] parts)
        {
            string qref = parts[parts.Length - 2].Trim();
            string tref = parts[parts.Length - 1].Trim();

            if (tref == "0") return;

            Facet.SearchTerms.Clear();
            Facet.SearchSets.Clear();

            if (parts.Length > 2)
            {
                var ccsId = GetControlConstructScheme(parts[0].Trim());
                if (ccsId == default(IdentifierTriple))
                {
                    Logger.Instance.Log.ErrorFormat("ControlConstructScheme '{0}' could not be found in the repository.", parts[0]);
                    counter[Counters.Skipped] += 1;
                    return;
                }
                Facet.SearchSets.Add(ccsId);
            }

            Facet.SearchTerms.Add(qref.Split('-').First());
            var questions = Repository.Search(Facet);
            QuestionActivity question;

            if (questions.Count != 1)
            {
                if (questions.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No question was found named '{0}' within the scope. Please check {1}", qref, filepath);
                }

                question = questions.OfType<QuestionActivity>().FirstOrDefault(x => x.ItemName.Best == qref);
                if (question == default(QuestionActivity))
                {
                    Logger.Instance.Log.ErrorFormat("{0} questions were found named '{1}' within the scope. Please check {2}", questions.Count, qref, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }
            else
            {
                question = questions.First() as QuestionActivity;
            }

            var ccgId = GetControlConstructGroup(tref);
            var controlConstructGroup = Repository.GetItem(ccgId) as ControlConstructGroup;

            // ControlConstructGroup already contains the Variable
            if (controlConstructGroup.Items.Any(x => x.AgencyId == question.AgencyId && x.Identifier == question.Identifier))
            {
                return;
            }

            var oldCcgs = Repository.FilterOldVersions(Repository.SearchTypedSet(question.CompositeId, ccgFacet));

            if (oldCcgs.Count > 0)               //New topic mapping
            {
                foreach (var oldCcg in oldCcgs)
                {
                    oldCcg.RemoveChild(question.CompositeId);
                    UpdatedItems.Add(oldCcg);
                }
            }
            controlConstructGroup.Items.Add(question);
            UpdatedItems.Add(controlConstructGroup);
        }

        private IdentifierTriple GetControlConstructGroup(string name)
        {
            if (ControlConstructGroupCache.ContainsKey(name))
            {
                return ControlConstructGroupCache[name];
            }
            var result = GetItemByTypeAndName(DdiItemType.ControlConstructGroup, name);
            if (result == default(IVersionable))
            {
                return default(IdentifierTriple);
            }
            else
            {
                ControlConstructGroupCache[name] = result.CompositeId;
                return result.CompositeId;
            }
        }

        private IdentifierTriple GetControlConstructScheme(string name)
        {
            if (ControlConstructSchemeCache.ContainsKey(name))
            {
                return ControlConstructSchemeCache[name];
            }
            var result = GetItemByTypeAndName(DdiItemType.ControlConstructScheme, name);
            if (result == default(IVersionable))
            {
                return default(IdentifierTriple);
            }
            else
            {
                ControlConstructSchemeCache[name] = result.CompositeId;
                return result.CompositeId;
            }
        }
    }
}
