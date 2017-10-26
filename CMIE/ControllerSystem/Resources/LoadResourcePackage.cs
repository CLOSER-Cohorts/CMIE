using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Resources
{
    class LoadResourcePackage : DDIFileResource
    {
        public LoadResourcePackage(string parent, string filepath)
        {
            Parent = parent;
            this.filepath = filepath;
        }

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            Collection<IVersionable> allItems = getAllItems();

            BindingPoints.AddRange(allItems.OfType<ResourcePackage>());

            return allItems;
        }
    }
}
