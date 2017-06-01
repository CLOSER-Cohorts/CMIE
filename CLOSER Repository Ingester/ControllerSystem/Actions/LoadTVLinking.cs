using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CLOSER_Repository_Ingester.ControllerSystem.Actions
{
    class LoadTVLinking : TXTFileAction
    {
        string filepath;
        static VariableScheme vs;

        protected override int[] numberOfColumns
        {
            get { return new int[]{2}; }
        }

        public LoadTVLinking(string _filepath)
        {
            Init(_filepath);
            if (vs == default(VariableScheme))
            {
                var client = Utility.GetClient();
                var facet = new SearchFacet();
                facet.ItemTypes.Add(DdiItemType.VariableScheme);
                facet.SearchTargets.Add(DdiStringType.Name);
                facet.SearchTerms.Add("Topic Variable Groups");
                facet.SearchLatestVersion = true;
                SearchResponse response = client.Search(facet);

                var graphPopulator = new GraphPopulator(client)
                {
                    ChildProcessing = ChildReferenceProcessing.PopulateLatest,
                };
                //graphPopulator.TypesToPopulate.Add(DdiItemType.Variable);
                graphPopulator.TypesToPopulate.Add(DdiItemType.VariableGroup);

                if (response.Results.Count == 1)
                {
                    vs = client.GetItem(
                        response.Results[0].CompositeId,
                        ChildReferenceProcessing.PopulateLatest) as VariableScheme;
                    vs.Accept(new GraphPopulator(client));
                }
            }
        }

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            RunFile(Runner, ws);
            return new List<IVersionable>();
        }

        public static IEnumerable<IVersionable> FinishedAllBuilds(bool clear_vs_reference = true)
        {
            if (vs == default(VariableScheme))
            {
                return new List<IVersionable>();
            }
            else
            {
                var gthr = new ItemGathererVisitor();
                gthr.TypesToFind.Add(DdiItemType.VariableScheme);
                gthr.TypesToFind.Add(DdiItemType.VariableGroup);
                vs.Accept(gthr);

                var foundItems = gthr.FoundItems;

                if (clear_vs_reference)
                {
                    vs = default(VariableScheme);
                }

                return foundItems;
            }
        }

        public override void Runner(string[] parts, IEnumerable<IVersionable> ws)
        {
            string vref = parts[0].Trim();
            string tref = parts[1].Trim();

            if (tref == "0") return;

            var variable = ws.OfType<Variable>().FirstOrDefault(x => x.ItemName.Best == vref);

            if (variable != default(Variable))
            {
                var vg = vs.VariableGroups.FirstOrDefault(x => x.ItemName.Best == tref);
                if (vg != default(VariableGroup))
                {
                    var in_group = vg.GetChildren().OfType<Variable>().Any(x => x.ItemName.Best == variable.ItemName.Best);
                    if (!in_group)
                    {
                        var old_vgs = new List<VariableGroup>();
                        for (var j = 0; j < vs.VariableGroups.Count; j++)
                        {
                            foreach (var v in vs.VariableGroups[j].ItemsList)
                            {
                                if (v is Variable)
                                {
                                    if (((DescribableBase)v).ItemName.Best == variable.ItemName.Best)
                                    {
                                        old_vgs.Add(vs.VariableGroups[j]);
                                    }
                                }
                            }
                        }
                        for (var i = 0; i < old_vgs.Count; i++ )
                        {
                            var to_be_removed = old_vgs[i].GetChildren().OfType<Variable>().FirstOrDefault(x => x.ItemName.Best == variable.ItemName.Best);
                            old_vgs[i].RemoveChild(to_be_removed);
                        }
                        vg.AddChild(variable);
                    }
                }
            }
        }
    }
}
