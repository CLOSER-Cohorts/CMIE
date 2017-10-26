using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Resources
{
    class LoadStudySweep : DDIFileResource
    {
        public LoadStudySweep(string filepath)
        {
            Parent = "";
            this.filepath = filepath;
        }

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            Collection<IVersionable> allItems = getAllItems();

            var sus = allItems.OfType<StudyUnit>().ToList();
            for (var i = 0; i < sus.Count(); i++ )
            {
                foreach (var dc in sus[i].DataCollections)
                {
                    var rp = new ResourcePackage();
                    rp.DublinCoreMetadata.Title = dc.ItemName;
                    allItems.Add(rp);
                    sus[i].AddChild(rp);
                    rp.AddChild(dc);
                }
            }
            return allItems;
        }
    }
}
