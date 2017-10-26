using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Resources
{
    public abstract class IResource
    {
        public bool valid { protected set; get; }
        public string Parent { protected set; get; }
        public List<IVersionable> BindingPoints { protected set; get; }
        protected string filepath;
        public IResource()
        {
            valid = false;
            BindingPoints = new List<IVersionable>();
        }
        public abstract void Validate();
        public abstract IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws);
    }

    public abstract class DDIFileResource : IResource
    {
        protected XDocument doc;

        public DDIFileResource() : base() {}

        public override void Validate()
        {
            if (!System.IO.File.Exists(this.filepath))
            {
                throw new System.Exception("Missing file: " + this.filepath);
            }

            var validator = new DdiValidator(this.filepath, DdiFileFormat.Ddi32);
            if (validator.Validate())
            {
                doc = validator.ValidatedXDocument;
            }
            else
            {
                throw new System.Exception("Invalid file: " + this.filepath);
            }
            valid = true;
        }

        protected Collection<IVersionable> getAllItems()
        {
            var deserializer = new Ddi32Deserializer();
            var harmonized = deserializer.HarmonizeIdentifiers(this.doc, DdiFileFormat.Ddi32);

            var instance = deserializer.GetDdiInstance(this.doc.Root);

            var gatherer = new ItemGathererVisitor();
            instance.Accept(gatherer);
            return gatherer.FoundItems;
        }
    }
}
