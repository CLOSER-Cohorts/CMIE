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
    class LoadDVMapping : TXTFileAction
    {
        private SearchFacet Facet;
        private Dictionary<string, IdentifierTriple> VariableSchemeCache;
        private Dictionary<IdentifierTriple, List<string>> _updateTracker;
        protected override int[] numberOfColumns
        {
            get { return new int[]{2,4}; }
        }
        public LoadDVMapping(string _filepath) : base(_filepath) 
        {
            VariableSchemeCache = new Dictionary<string, IdentifierTriple>();
            _updateTracker = new Dictionary<IdentifierTriple, List<string>>();
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
            string sourceVariable, derivedVariable;
            var dindex = (parts.Length / 2) - 1;
            var sindex = parts.Length - 1;

            sourceVariable = parts[sindex].Trim();
            derivedVariable = parts[dindex].Trim();

            if (derivedVariable == "0") return;

            bool loadSource = sourceVariable != "0";

            Facet.SearchSets.Clear();
            Facet.SearchTerms.Clear();

            List<IVersionable> sources = default(List<IVersionable>), deriveds;

            if (parts.Length == 4)
            {
                var dsId = GetVariableScheme(parts[0].Trim());
                if (dsId == default(IdentifierTriple))
                {
                    Logger.Instance.Log.ErrorFormat("VariableScheme '{0}' could not be found in the repository.", parts[0]);
                    counter[Counters.Skipped] += 1;
                    return;
                }

                if (loadSource)
                {
                    var ssId = GetVariableScheme(parts[2].Trim());
                    if (ssId == default(IdentifierTriple))
                    {
                        Logger.Instance.Log.ErrorFormat("VariableScheme '{0}' could not be found in the repository.", parts[2]);
                        counter[Counters.Skipped] += 1;
                        return;
                    }

                    Facet.SearchSets.Add(ssId);
                    Facet.SearchTerms.Add(sourceVariable);
                    sources = Repository.Search(Facet);
                    Facet.SearchSets.Clear();
                    Facet.SearchTerms.Clear();
                }
                Facet.SearchSets.Add(dsId);
                Facet.SearchTerms.Add(derivedVariable);
                deriveds = Repository.Search(Facet);
            }
            else
            {
                if (loadSource)
                {
                    Facet.SearchTerms.Add(sourceVariable);
                    sources = Repository.Search(Facet);
                    Facet.SearchSets.Clear();
                    Facet.SearchTerms.Clear();
                }
                Facet.SearchTerms.Add(derivedVariable);
                deriveds = Repository.Search(Facet);
            }

            if (deriveds.Count != 1)
            {
                if (deriveds.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No variable was found named '{0}' within the scope. Please check {1}", derivedVariable, filepath);
                }
                else
                {
                    Logger.Instance.Log.ErrorFormat("{0} variables were found named '{1}' within the scope. Please check {2}", deriveds.Count, derivedVariable, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }
            var derived = deriveds.First() as Variable;

            if (loadSource)
            {
                if (!_updateTracker.ContainsKey(derived.CompositeId))
                {
                    _updateTracker[derived.CompositeId] = new List<string>();
                    foreach (var sv in derived.SourceVariables)
                    {
                        _updateTracker[derived.CompositeId].Add(sv.UserIds[0].Identifier);
                    }
                }

                if (sources.Count != 1)
                {
                    if (sources.Count == 0)
                    {
                        Logger.Instance.Log.ErrorFormat("No variable was found named '{0}' within the scope. Please check {1}", sourceVariable, filepath);
                    }
                    else
                    {
                        Logger.Instance.Log.ErrorFormat("{0} questions were found named '{1}' within the scope. Please check {2}", sources.Count, sourceVariable, filepath);
                    }
                    counter[Counters.Skipped] += 1;
                    return;
                }

                var source = sources.First() as Variable;
                Func<string, bool> predicate = x => x == source.UserIds[0].Identifier;
                if (_updateTracker[derived.CompositeId].Any(predicate))
                {
                    _updateTracker[derived.CompositeId].RemoveAll(new Predicate<string>(predicate));
                }
                else
                {
                    derived.SourceVariables.Add(source);
                }
            }
            else
            {
                derived.SourceVariables.Clear();
            }

            UpdatedItems.Add(derived);
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

        private void RemoveOldMappings()
        {
            foreach (var update in _updateTracker)
            {
                if (update.Value.Count > 0)
                {
                    var derived = Repository.GetItem(update.Key) as Variable;
                    foreach (var userId in update.Value)
                    {
                        var source = Repository.GetItem(userId) as Variable;
                        derived.SourceVariables.Remove(source);
                        UpdatedItems.Add(derived);
                    }
                }
            }
            _updateTracker.Clear();
        }
    }
}
