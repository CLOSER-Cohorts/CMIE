
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using SysCon = System.Console;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Actions
{
    class LoadQVMapping : TXTFileAction
    {
        private SearchFacet QcFacet;
        private SearchFacet VarFacet;
        private Dictionary<string, IdentifierTriple> QuestionConstructSchemeCache;
        private Dictionary<string, IdentifierTriple> VariableSchemeCache;
        protected override int[] numberOfColumns
        {
            get { return new int[]{2,4}; }
        }
        public LoadQVMapping(string _filepath) : base(_filepath) 
        {
            QuestionConstructSchemeCache = new Dictionary<string, IdentifierTriple>();
            VariableSchemeCache = new Dictionary<string, IdentifierTriple>();
        }

        protected override void RunFile(Action<string[]> _runner)
        {
            QcFacet = new SearchFacet();
            QcFacet.ItemTypes.Add(DdiItemType.QuestionConstruct);
            QcFacet.SearchTargets.Add(DdiStringType.Name);
            QcFacet.SearchLatestVersion = true;

            VarFacet = new SearchFacet();
            VarFacet.ItemTypes.Add(DdiItemType.Variable);
            VarFacet.SearchTargets.Add(DdiStringType.Name);
            VarFacet.SearchLatestVersion = true;
            base.RunFile(_runner);
        }

        public override void Runner(string[] parts)
        {
            string questionName, variableName;

            int qindex = (parts.Length/2)-1;
            int vindex = parts.Length-1;
            string questionColumn = parts[qindex].Trim();
            string[] questionNameParts = questionColumn.Split(new char[] { '$' });     //remove grid cell info
            questionName = questionNameParts[0];
            variableName = parts[vindex].Trim();

            if (questionName == "0" || variableName == "0") return;

            QcFacet.SearchSets.Clear();
            VarFacet.SearchSets.Clear();
            QcFacet.SearchTerms.Clear();
            VarFacet.SearchTerms.Clear();

            if (parts.Length == 4)
            {
                var ccsId = GetControlConstructScheme(parts[0].Trim());
                if (ccsId == default(IdentifierTriple))
                {
                    SysCon.WriteLine("ControlConstructScheme '{0}' could not be found in the repository.", parts[0]);
                    counter[Counters.Skipped] += 1;
                    return;
                }
                QcFacet.SearchSets.Add(ccsId);

                var vsId = GetVariableScheme(parts[2].Trim());
                if (vsId == default(IdentifierTriple))
                {
                    Logger.Instance.Log.ErrorFormat("VariableScheme '{0}' could not be found in the repository.", parts[2]);
                    counter[Counters.Skipped] += 1;
                    return;
                }
                VarFacet.SearchSets.Add(vsId);
            }

            QcFacet.SearchTerms.Add(questionName);
            VarFacet.SearchTerms.Add(variableName);

            var questions = Repository.Search(QcFacet);
            var variables = Repository.Search(VarFacet);

            if (questions.Count != 1)
            {
                if (questions.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No question was found named '{0}' within the scope. Please check {1}", questionName, filepath);
                }
                else
                {
                    Logger.Instance.Log.ErrorFormat("{0} questions were found named '{1}' within the scope. Please check {2}", questions.Count, questionName, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }

            if (variables.Count != 1)
            {
                if (variables.Count == 0)
                {
                    Logger.Instance.Log.ErrorFormat("No variable was found named '{0}' within the scope. Please check {1}", variableName, filepath);
                }
                else
                {
                    Logger.Instance.Log.ErrorFormat("{0} variables were found named '{1}' within the scope. Please check {2}", variables.Count, variableName, filepath);
                }
                counter[Counters.Skipped] += 1;
                return;
            }

            var question = questions.First() as QuestionActivity;
            var variable = variables.First() as Variable;

            if (question.Question != null)
            {
                variable.SourceQuestions.Add(question.Question);
            }
            if (question.QuestionGrid != null)
            {
                variable.SourceQuestionGrids.Add(question.QuestionGrid);
            }

            UpdatedItems.Add(variable);
        }

        private IdentifierTriple GetControlConstructScheme(string name)
        {
            if (QuestionConstructSchemeCache.ContainsKey(name))
            {
                return QuestionConstructSchemeCache[name];
            }
            var result = GetItemByTypeAndName(DdiItemType.ControlConstructScheme, name);
            if (result == default(IVersionable))
            {
                return default(IdentifierTriple);
            }
            else
            {
                QuestionConstructSchemeCache[name] = result.CompositeId;
                return result.CompositeId;
            }
        }

        private IdentifierTriple GetVariableScheme(string name)
        {
            if (VariableSchemeCache.ContainsKey(name))
            {
                return VariableSchemeCache[name];
            }
            var result = GetItemByTypeAndName(DdiItemType.VariableScheme, name);
            if (result == default(IVersionable))
            {
                return default(IdentifierTriple);
            }
            else
            {
                VariableSchemeCache[name] = result.CompositeId;
                return result.CompositeId;
            }
        }
    }
}
