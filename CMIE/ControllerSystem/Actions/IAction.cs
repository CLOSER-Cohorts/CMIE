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

namespace CMIE.ControllerSystem.Actions
{
    public abstract class IAction
    {
        public string scope { protected set; get; }
        public bool valid { protected set; get; }
        protected string filepath;
        public IAction()
        {
            valid = false;
        }
        public abstract void Validate();
        public abstract IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws);
    }

    public abstract class TXTFileAction : IAction
    {
        public enum Counters {Total, Bad, Skipped};
        public Dictionary<Counters, int> counter { get; protected set; }
        public abstract void Runner(string[] parts,IEnumerable<IVersionable> ws);

        protected abstract int[] numberOfColumns { get; }

        public TXTFileAction(string _filepath) : base()
        {
            this.filepath = _filepath;
            this.valid = false;
            counter = new Dictionary<Counters, int>();
            counter[Counters.Total] = 0;
            counter[Counters.Bad] = 0;
            counter[Counters.Skipped] = 0;
        }

        public override void Validate()
        {
            if (!System.IO.File.Exists(this.filepath))
            {
                throw new System.Exception("Missing file: " + this.filepath);
            }
            valid = true;
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

        public override IEnumerable<IVersionable> Build(IEnumerable<IVersionable> ws)
        {
            RunFile(Runner, ws);
            return new List<IVersionable>();
        }
    }
}
