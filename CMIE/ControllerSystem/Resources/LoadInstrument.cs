using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Resources
{
    class LoadInstrument : DDIFileResource
    {
        private string external_path;

        public LoadInstrument(string parent, string filepath, string external_path = null) : base()
        {
            Parent = parent;
            this.filepath = filepath;
            this.external_path = external_path;
        }

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            var allItems = getAllItems();

            var questionSchemes = allItems.OfType<QuestionScheme>();
            foreach (var questionScheme in questionSchemes)
            {
                foreach (var qi in questionScheme.Questions)
                {
                    var p = new Parameter();
                    p.ParameterType = InstrumentParameterType.Out;
                    p.Name.Add("en-GB", "p_" + qi.ItemName.Best);
                    qi.OutParameters.Add(p);
                }
            }

            //I assume that there are the corresponding caddies question constructs
            //add an InParameter based on the first (the only) OutParameter of its QuestionItem,
            //an OutputParameter based on the construct name
            //and a binding between the two
            foreach (var qc in allItems.OfType<QuestionActivity>())
            {
                if (qc.Question != null)
                {
                    var p = new Parameter();
                    p.ParameterType = InstrumentParameterType.In;
                    p.Name.Add("en-GB", qc.Question.OutParameters.First().Name.Best);
                    qc.InParameters.Add(p);
                    var p2 = new Parameter();
                    p2.ParameterType = InstrumentParameterType.Out;
                    p2.Name.Add("en-GB", "p_" + qc.ItemName.Best);
                    qc.OutParameters.Add(p2);
                }
                else if (qc.QuestionGrid != null)
                {
                    continue;
                }
                else
                {
                    //Trace.WriteLine("   question construct with no source");
                }
            }

            if (external_path != null)
            {
                var instrument = allItems.OfType<Instrument>().FirstOrDefault();
                Actions.AttachExternalInstrument.Attach(instrument, external_path);
            }

            BindingPoints.AddRange(allItems.OfType<Instrument>());

            return allItems;
        }
    }
}
