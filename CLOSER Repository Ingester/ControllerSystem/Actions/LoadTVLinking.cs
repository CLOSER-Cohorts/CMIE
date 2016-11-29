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
        VariableScheme vs;
        protected override int[] numberOfColumns
        {
            get { return new int[]{2}; }
        }
        public LoadTVLinking(string _filepath)
        {
            Init(_filepath);
        }

        public IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            var client = Utility.GetClient();
            var facet = new SearchFacet();
            facet.ItemTypes.Add(DdiItemType.VariableScheme);
            facet.SearchTargets.Add(DdiStringType.Name);
            facet.SearchTerms.Add("Topic Variable Groups");
            SearchResponse response = client.Search(facet);

            if (response.Results.Count == 1)
            {
                vs = client.GetItem(
                    response.Results[0].CompositeId, 
                    ChildReferenceProcessing.Populate) as VariableScheme;

                RunFile(Runner, ws);
            }

            return new List<IVersionable>();
        }

        public override void Runner(string[] parts, IEnumerable<IVersionable> ws)
        {
            string vref = parts[0].Trim();
            string tref = parts[1].Trim();

            if (tref == "0") return;

            var variable = ws.OfType<Variable>().Where(x => x.ItemName.Best == vref);

            //if (srcs.Count() > 0 && DVs.Count() > 0)
            //{
            //    DVs.First().SourceVariables.Add(srcs.First());
            //}
        }
    }
}
