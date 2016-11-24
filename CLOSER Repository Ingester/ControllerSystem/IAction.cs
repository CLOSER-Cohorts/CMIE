using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

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
        IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws);
    }

    public abstract class TXTFileAction : IAction
    {
        public enum Counters {Total, Bad, Skipped};
        public string scope { get; protected set; }
        public Dictionary<Counters, int> counter { get; protected set; }
        public abstract void Runner(string[] parts,IEnumerable<IVersionable> ws);

        protected string filepath;
        protected abstract int[] numberOfColumns { get; }

        public virtual void Init(string _filepath)
        {
            this.filepath = _filepath;
            counter = new Dictionary<Counters, int>();
            counter[Counters.Total] = 0;
            counter[Counters.Bad] = 0;
            counter[Counters.Skipped] = 0;
        }

        public void Validate()
        {
            if (!System.IO.File.Exists(this.filepath))
            {
                throw new System.Exception("Missing file: " + this.filepath);
            }
        }

        protected void RunFile(Action<string[],IEnumerable<IVersionable>> _runner, IEnumerable<IVersionable> ws)
        {
            string[] lines = File.ReadAllLines(this.filepath);
            foreach (string line in lines)
            {
                counter[Counters.Total] += 1;
                string[] parts = line.Split(new char[] { '\t' });

                if (!numberOfColumns.Contains(parts.Length))
                {
                    counter[Counters.Bad] += 1;
                    continue;
                }

                _runner(parts, ws);
            }
        }

        public IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            RunFile(Runner, ws);
            return new List<IVersionable>();
        }
    }

    public abstract class DDIFileAction : IAction
    {
        public string scope { get; protected set; }
        public abstract IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws);

        protected XDocument doc;
        protected string filepath;

        public void Validate()
        {
            if (!System.IO.File.Exists(this.filepath))
            {
                throw new System.Exception("Missing file: " + this.filepath);
            }

            DdiValidator validator = new DdiValidator(this.filepath, DdiFileFormat.Ddi32);
            if (validator.Validate())
            {
                doc = validator.ValidatedXDocument;
            }
            else
            {
                throw new System.Exception("Invalid file: " + this.filepath);
            }
        }

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
