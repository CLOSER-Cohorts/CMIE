using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CLOSER_Repository_Ingester.ControllerSystem
{
    public interface IAction
    {
        string scope { get; }
        void Validate();
        IEnumerable<IVersionable> Build();
    }

    public abstract class DDIFileAction : IAction
    {
        public string scope { get; protected set; }
        public abstract void Validate();
        public abstract IEnumerable<IVersionable> Build();

        protected XDocument doc;

        protected Collection<IVersionable> getAllItems()
        {
            Ddi32Deserializer deserializer = new Ddi32Deserializer();
            HarmonizationResult harmonized = deserializer.HarmonizeIdentifiers(this.doc, DdiFileFormat.Ddi32);

            DdiInstance instance = deserializer.GetDdiInstance(this.doc.Root);

            var gatherer = new ItemGathererVisitor();
            instance.Accept(gatherer);
            return gatherer.FoundItems;
        }
    }
}
