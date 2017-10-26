
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using SysCon = System.Console;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Actions
{
    class LoadQVMapping : TXTFileAction
    {
        protected override int[] numberOfColumns
        {
            get { return new int[]{2,4}; }
        }
        public LoadQVMapping(string _filepath) : base(_filepath) {}

        public override void Runner(string[] parts, IEnumerable<IVersionable> ws)
        {
            IEnumerable<IVersionable> subFocusedWS = ws;
            string questionName, variableName;

            int qindex = (parts.Length/2)-1;
            int vindex = parts.Length-1;
            string questionColumn = parts[qindex].Trim();
            string[] questionNameParts = questionColumn.Split(new char[] { '$' });     //remove grid cell info
            questionName = questionNameParts[0];
            variableName = parts[vindex].Trim();

            if (questionName == "0" || variableName == "0") return;

            if (parts.Length == 4)
            {
                var foundCcs = ws.OfType<ControlConstructScheme>().Where(x => x.ItemName.Best == parts[0].Trim());
                if (foundCcs.Count() == 0)
                {
                    SysCon.WriteLine("Invalid question scheme: {0}", parts[0]);
                    counter[Counters.Skipped] += 1;
                    return;
                }

                var foundVs = ws.OfType<VariableScheme>().Where(x => x.ItemName.Best == parts[2].Trim());
                if (foundVs.Count() == 0)
                {
                    SysCon.WriteLine("Invalid variable scheme: {0}", parts[2]);
                    counter[Counters.Skipped] += 1;
                    return;
                }

                var gatherer = new ItemGathererVisitor();
                foreach (var foundCc in foundCcs) foundCc.Accept(gatherer);
                foreach (var foundV in foundVs) foundV.Accept(gatherer);

                subFocusedWS = gatherer.FoundItems;
            }

            var qs = subFocusedWS.OfType<QuestionActivity>().Where(x => x.ItemName.Best == questionName);
            var vs = subFocusedWS.OfType<Variable>().Where(x => x.ItemName.Best == variableName);

            if (qs.Count() > 0 && vs.Count() > 0)
            {
                if (qs.First().Question != null)
                {
                    vs.First().SourceQuestions.Add(qs.First().Question);
                }
                if (qs.First().QuestionGrid != null)
                {
                    vs.First().SourceQuestionGrids.Add(qs.First().QuestionGrid);
                }
            }
        }
    }
}
