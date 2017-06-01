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
    class LoadTQLinking : TXTFileAction
    {
        string filepath;
        static ControlConstructScheme ccs;

        protected override int[] numberOfColumns
        {
            get { return new int[]{2}; }
        }

        public LoadTQLinking(string _filepath)
        {
            Init(_filepath);
            if (ccs == default(ControlConstructScheme))
            {
                var client = Utility.GetClient();
                var facet = new SearchFacet();
                facet.ItemTypes.Add(DdiItemType.ControlConstructScheme);
                facet.SearchTargets.Add(DdiStringType.Name);
                facet.SearchTerms.Add("Topic Question Construct Groups");
                facet.SearchLatestVersion = true;
                SearchResponse response = client.Search(facet);

                var graphPopulator = new GraphPopulator(client)
                {
                    ChildProcessing = ChildReferenceProcessing.PopulateLatest
                };
                graphPopulator.TypesToPopulate.Add(DdiItemType.ControlConstructGroup);

                if (response.Results.Count == 1)
                {
                    ccs = client.GetItem(
                        response.Results[0].CompositeId,
                        ChildReferenceProcessing.PopulateLatest) as ControlConstructScheme;
                    ccs.Accept(new GraphPopulator(client));
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
            if (ccs == default(ControlConstructScheme))
            {
                return new List<IVersionable>();
            }
            else
            {
                var gthr = new ItemGathererVisitor();
                gthr.TypesToFind.Add(DdiItemType.ControlConstructScheme);
                gthr.TypesToFind.Add(DdiItemType.ControlConstructGroup);
                ccs.Accept(gthr);

                var foundItems = gthr.FoundItems;

                if (clear_vs_reference)
                {
                    ccs = default(ControlConstructScheme);
                }

                return foundItems;
            }
        }

        public override void Runner(string[] parts, IEnumerable<IVersionable> ws)
        {
            string qref = parts[parts.Length - 2].Trim();
            string tref = parts[parts.Length - 1].Trim();

            if (tref == "0") return;

            var scopedWS = ws;

            if (parts.Length > 2)
            {
                var gthr = new ItemGathererVisitor();
                var ccs = scopedWS.OfType<ControlConstructScheme>().FirstOrDefault(x => x.ItemName.Best == parts[0].Trim());
                if (ccs != default(ControlConstructScheme))
                {
                    ccs.Accept(gthr);
                    scopedWS = gthr.FoundItems;
                }
            }

            var question = scopedWS.OfType<QuestionActivity>().FirstOrDefault(x => x.ItemName.Best == qref);

            if (question != default(QuestionActivity))
            {
                var ccg = ccs.ControlConstructGroups.FirstOrDefault(x => x.ItemName.Best == tref);
                if (ccg != default(ControlConstructGroup))
                {
                    var in_group = ccg.GetChildren().OfType<QuestionActivity>().Any(x => x.ItemName.Best == question.ItemName.Best);
                    if (!in_group)
                    {
                        var old_ccgs = new List<ControlConstructGroup>();
                        for (var j = 0; j < ccs.ControlConstructGroups.Count; j++ )
                        {
                            var children = ccs.ControlConstructGroups[j].ItemsList;
                            for (var i = 0; i < children.Count; i++)
                            {
                                if (children[i] is QuestionActivity)
                                {
                                    if (((DescribableBase)children[i]).ItemName.Best == question.ItemName.Best)
                                    {
                                        old_ccgs.Add(ccs.ControlConstructGroups[j]);
                                    }
                                }
                            }
                        }
                        for (var i = 0; i < old_ccgs.Count; i++)
                        {
                            var to_be_removed = old_ccgs[i].GetChildren().OfType<QuestionActivity>().FirstOrDefault(x => x.ItemName.Best == question.ItemName.Best);
                            old_ccgs[i].RemoveChild(to_be_removed);
                        }
                        ccg.AddChild(question);
                    }
                }
            }
        }
    }
}
