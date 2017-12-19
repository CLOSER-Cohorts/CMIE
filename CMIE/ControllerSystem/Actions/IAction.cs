using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Actions
{
    public abstract class IAction
    {
        public string scope { protected set; get; }
        public bool valid { protected set; get; }
        protected string filepath;
        protected Repository Repository;
        protected List<IVersionable> UpdatedItems;

        public IAction()
        {
            valid = false;
        }
        public abstract void Validate();
        public abstract IEnumerable<IVersionable> Build(Repository repository);
    }

    public abstract class TXTFileAction : IAction
    {
        public enum Counters {Total, Bad, Skipped};
        public Dictionary<Counters, int> counter { get; protected set; }
        public abstract void Runner(string[] parts);
        protected abstract int[] numberOfColumns { get; }

        public TXTFileAction(string _filepath) : base()
        {
            this.filepath = _filepath;
            this.valid = false;
            counter = new Dictionary<Counters, int>();
            counter[Counters.Total] = 0;
            counter[Counters.Bad] = 0;
            counter[Counters.Skipped] = 0;
            UpdatedItems = new List<IVersionable>();
        }

        public override void Validate()
        {
            if (!System.IO.File.Exists(this.filepath))
            {
                throw new System.Exception("Missing file: " + this.filepath);
            }
            valid = true;
        }

        protected IVersionable GetItemByTypeAndName(Guid type, string name)
        {
            var facet = new SearchFacet();
            facet.ItemTypes.Add(type);
            facet.SearchTargets.Add(DdiStringType.Name);
            facet.SearchLatestVersion = true;
            facet.SearchTerms.Add(name);
            var response = Repository.Search(facet);
            return response.FirstOrDefault();
        }
        
        protected virtual void RunFile(Action<string[]> _runner)
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

                _runner(parts);
            }
        }

        public override IEnumerable<IVersionable> Build(Repository repository)
        {
            Repository = repository;
            RunFile(Runner);
            if (UpdatedItems.Any())
            {
                Logger.Instance.Log.Info("Mapping completed with no updates required.");
            }
            return UpdatedItems;
        }
    }
}
