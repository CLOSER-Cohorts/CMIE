using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CLOSER_Repository_Ingester.ControllerSystem.Actions
{
    class LoadDataset : DDIFileAction
    {
        public LoadDataset(string filepath)
        {
            this.filepath = filepath;
        }

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            Collection<IVersionable> allItems = getAllItems();

            var pi = allItems.OfType<PhysicalInstance>().FirstOrDefault();
            if (pi != default(PhysicalInstance))
            {
                foreach (var dr in allItems.OfType<DataRelationship>())
                {
                    pi.DataRelationships.Add(dr);
                }
                pi.RecordLayouts.Clear();
            }

            return allItems;
        }
    }
}
