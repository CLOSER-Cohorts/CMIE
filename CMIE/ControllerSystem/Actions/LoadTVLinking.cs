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
    class LoadTVLinking : TXTFileAction
    {
        private SearchFacet Facet;
        private SetSearchFacet vgFacet;
        private Dictionary<string, IdentifierTriple> VariableGroupCache;
        private Dictionary<string, IdentifierTriple> VariableSchemeCache;

        protected override int[] numberOfColumns
        {
            get { return new int[]{2,3}; }
        }

        public LoadTVLinking(string _filepath) : base(_filepath) 
        {
            VariableGroupCache = new Dictionary<string, IdentifierTriple>();
            VariableSchemeCache = new Dictionary<string, IdentifierTriple>();
        }

        protected override void RunFile(Action<string[]> _runner)
        {
            Facet = new SearchFacet();
            Facet.ItemTypes.Add(DdiItemType.Variable);
            Facet.SearchTargets.Add(DdiStringType.Name);
            Facet.SearchLatestVersion = true;
            vgFacet = new SetSearchFacet();
            vgFacet.ItemTypes.Add(DdiItemType.VariableGroup);
            vgFacet.LeafItemTypes.Add(DdiItemType.VariableGroup);
            vgFacet.ReverseTraversal = true;
            base.RunFile(_runner);
        }

        public override void Runner(string[] parts)
        {
            string vref = parts[parts.Length - 2].Trim();
            string tref = parts[parts.Length - 1].Trim();

            if (tref == "0") return;

            Facet.SearchTerms.Clear();
            Facet.SearchSets.Clear();

            if (parts.Length > 2)
            {
                var vsId = GetVariableScheme(parts[0].Trim());
                if (vsId == default(IdentifierTriple))
                {
                    Logger.Instance.Log.ErrorFormat("VariableScheme '{0}' could not be found in the repository.", parts[0]);
                    counter[Counters.Skipped] += 1;
                    return;
                }
                Facet.SearchSets.Add(vsId);
            }

            Facet.SearchTerms.Add(vref);
            var variables = Repository.Search(Facet);

            if (variables.Count != 1)
            {
                if (variables.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No variable was found named '{0}' within the scope. Please check {1}", vref, filepath);
                }
                else
                {
                    Logger.Instance.Log.ErrorFormat("{0} variables were found named '{1}' within the scope. Please check {2}", variables.Count, vref, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }

            var variable = variables.First() as Variable;

            var vgId = GetVariableGroup(tref);
            var variableGroup = Repository.GetItem(vgId) as VariableGroup;

            // VariableGroup already contains the Variable
            if (variableGroup.Items.Any(x => x.AgencyId == variable.AgencyId && x.Identifier == variable.Identifier))
            {
                return;
            }

            var oldVgs = Repository.FilterOldVersions(Repository.SearchTypedSet(variable.CompositeId, vgFacet));
            
            if (oldVgs.Count > 0)               //New topic mapping
            {
                foreach (var oldVg in oldVgs)
                {
                    oldVg.RemoveChild(variable.CompositeId);
                    UpdatedItems.Add(oldVg);
                }
            }
            variableGroup.Items.Add(variable);
            UpdatedItems.Add(variableGroup);
        }

        private IdentifierTriple GetVariableGroup(string name)
        {
            if (VariableGroupCache.ContainsKey(name))
            {
                return VariableGroupCache[name];
            }
            var result = GetItemByTypeAndName(DdiItemType.VariableGroup, name);
            if (result == default(IVersionable))
            {
                return default(IdentifierTriple);
            }
            else
            {
                VariableGroupCache[name] = result.CompositeId;
                return result.CompositeId;
            }
        }

        private IdentifierTriple GetVariableScheme(string name)
        {
            if (VariableSchemeCache.ContainsKey(name))
            {
                return VariableSchemeCache[name];
            }
            var result = GetItemByTypeAndName(DdiItemType.VariableScheme, name);
            if (result == default(IVersionable))
            {
                return default(IdentifierTriple);
            }
            else
            {
                VariableSchemeCache[name] = result.CompositeId;
                return result.CompositeId;
            }
        }
    }
}
