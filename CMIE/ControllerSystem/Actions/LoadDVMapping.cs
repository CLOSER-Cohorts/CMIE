using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Actions
{
    class LoadDVMapping : TXTFileAction
    {
        protected override int[] numberOfColumns
        {
            get { return new int[]{2}; }
        }
        public LoadDVMapping(string _filepath) : base(_filepath) {}

        public override void Runner(string[] parts, IEnumerable<IVersionable> ws)
        {
            string sourceVariable = parts[1].Trim();
            string derivedVariable = parts[0].Trim();

            if (sourceVariable == "0" || derivedVariable == "0") return;

            var srcs = ws.OfType<Variable>().Where(x => x.ItemName.Best == sourceVariable);
            var DVs = ws.OfType<Variable>().Where(x => x.ItemName.Best == derivedVariable);

            if (srcs.Count() > 0 && DVs.Count() > 0)
            {
                DVs.First().SourceVariables.Add(srcs.First());
            }
        }
    }
}
