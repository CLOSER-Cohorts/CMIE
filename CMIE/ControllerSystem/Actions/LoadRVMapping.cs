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
    class LoadRVMapping : TXTFileAction
    {
        private SearchFacet Facet;
        private Dictionary<string, IdentifierTriple> VariableSchemeCache;
        protected override int[] numberOfColumns
        {
            get { return new int[]{3}; }
        }
        public LoadRVMapping(string _filepath) : base(_filepath) 
        {
            VariableSchemeCache = new Dictionary<string, IdentifierTriple>();
        }

        protected override void RunFile(Action<string[]> _runner)
        {
            Facet = new SearchFacet();
            Facet.ItemTypes.Add(DdiItemType.Variable);
            Facet.SearchTargets.Add(DdiStringType.Name);
            Facet.SearchLatestVersion = true;
            base.RunFile(_runner);
        }

        public override void Runner(string[] parts)
        {
            string rvId, variableName;

            rvId = parts[0].Trim();
            variableName = parts[2].Trim();

            Facet.SearchSets.Clear();
            Facet.SearchTerms.Clear();

            List<IVersionable> variables;
            var vsId = GetVariableScheme(parts[1].Trim());
            if (vsId == default(IdentifierTriple))
            {
                Logger.Instance.Log.ErrorFormat("VariableScheme '{0}' could not be found in the repository.", parts[1]);
                counter[Counters.Skipped] += 1;
                return;
            }
            Facet.SearchSets.Add(vsId);
            Facet.SearchTerms.Add(variableName);
            variables = Repository.Search(Facet);


            if (variables.Count != 1)
            {
                if (variables.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No variable was found named '{0}' within the scope. Please check {1}", variableName, filepath);
                }
                else
                {
                    Logger.Instance.Log.ErrorFormat("{0} questions were found named '{1}' within the scope. Please check {2}", variables.Count, variableName, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }

            var variable = variables.First() as Variable;

            var rv = Repository.GetLatestItem(rvId) as RepresentedVariable;

            variable.RepresentedVariable = rv;

            UpdatedItems.Add(variable);
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
