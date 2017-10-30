using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Resources
{
    class LoadDataset : DDIFileResource
    {
        public LoadDataset(string parent, string filepath)
        {
            Parent = parent;
            this.filepath = filepath;
        }

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            Collection<IVersionable> allItems = getAllItems();

            var pi = allItems.OfType<PhysicalInstance>().FirstOrDefault();
            if (pi != default(PhysicalInstance))
            {
                if (pi.RecordLayouts.Count > 0)
                {
                    foreach (var dr in allItems.OfType<DataRelationship>())
                    {
                        pi.DataRelationships.Add(dr);
                    }
                    pi.RecordLayouts.Clear();
                }
            }

            BindingPoints.AddRange(allItems.OfType<PhysicalInstance>());
            BindingPoints.AddRange(allItems.OfType<PhysicalProduct>());
            BindingPoints.AddRange(allItems.OfType<LogicalProduct>());

            return allItems;
        }
    }
}
