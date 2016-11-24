using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CLOSER_Repository_Ingester.ControllerSystem.Actions
{
    class LoadTopics : DDIFileAction
    {
        public LoadTopics(string filepath)
        {
            this.filepath = filepath;
        }

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            Collection<IVersionable> allItems = getAllItems();

            var rp = allItems.OfType<ResourcePackage>().First();
            var qcs = new ControlConstructScheme();
            qcs.ItemName.Add("en-GB", "Topic Question Construct Groups");
            var vs = new VariableScheme();
            vs.ItemName.Add("en-GB", "Topic Variable Groups");
            rp.ControlConstructSchemes.Add(qcs);
            rp.VariableSchemes.Add(vs);

            foreach (var concept in allItems.OfType<Concept>())
            {
                var qcg = new ControlConstructGroup();
                qcg.TypeOfGroup = "ConceptGroup";
                qcg.Concept = concept;
                qcg.Label.Add("en-GB", concept.Label.Best + " Question Construct Group");
                qcg.ItemName.Add("en-GB", concept.ItemName.Best);
                qcs.ControlConstructGroups.Add(qcg);

                var vg = new VariableGroup();
                vg.TypeOfGroup = "ConceptGroup";
                vg.Concept = concept;
                vg.Label.Add("en-GB", concept.Label.Best + " Variable Group");
                vg.ItemName.Add("en-GB", concept.ItemName.Best);
                vs.VariableGroups.Add(vg);
            }

            return allItems;
        }
    }
}
