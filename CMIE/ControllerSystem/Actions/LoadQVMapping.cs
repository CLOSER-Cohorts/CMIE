
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
        private Dictionary<IdentifierTriple, List<string>> _updateTracker;
        protected override int[] numberOfColumns
        {
            get { return new int[]{2,4}; }
        }
        public LoadQVMapping(string _filepath) : base(_filepath) 
        {
            QuestionConstructSchemeCache = new Dictionary<string, IdentifierTriple>();
            VariableSchemeCache = new Dictionary<string, IdentifierTriple>();
            _updateTracker = new Dictionary<IdentifierTriple, List<string>>();
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

            RemoveOldMappings();
        }

        public override void Runner(string[] parts)
        {
            string questionName, variableName;
            bool updated = false;

            int qindex = (parts.Length/2)-1;
            int vindex = parts.Length-1;
            string questionColumn = parts[qindex].Trim();
            string[] questionNameParts = questionColumn.Split(new char[] { '$' });     //remove grid cell info
            questionName = questionNameParts[0];
            variableName = parts[vindex].Trim();

            if (variableName == "0") return;

            bool loadQuestion = questionName != "0";

            QcFacet.SearchSets.Clear();
            VarFacet.SearchSets.Clear();
            QcFacet.SearchTerms.Clear();
            VarFacet.SearchTerms.Clear();

            if (parts.Length == 4)
            {
                if (loadQuestion) 
                {
                    var ccsId = GetControlConstructScheme(parts[0].Trim());
                    if (ccsId == default(IdentifierTriple))
                    {
                        SysCon.WriteLine("ControlConstructScheme '{0}' could not be found in the repository.", parts[0]);
                        counter[Counters.Skipped] += 1;
                        return;
                    }
                    QcFacet.SearchSets.Add(ccsId);
                }

                var vsId = GetVariableScheme(parts[2].Trim());
                if (vsId == default(IdentifierTriple))
                {
                    Logger.Instance.Log.ErrorFormat("VariableScheme '{0}' could not be found in the repository.", parts[2]);
                    counter[Counters.Skipped] += 1;
                    return;
                }
                VarFacet.SearchSets.Add(vsId);
            }

            VarFacet.SearchTerms.Add(variableName);
            var variables = Repository.Search(VarFacet);

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

            var variable = variables.First() as Variable;

            if (loadQuestion) 
            {
                QcFacet.SearchTerms.Add(questionName.Split('-').First());
                var questions = Repository.Search(QcFacet);
                QuestionActivity question;

                if (questions.Count != 1)
                {
                    if (questions.Count == 0)
                    {
                        Logger.Instance.Log.ErrorFormat("No question was found named '{0}' within the scope. Please check {1}", questionName, filepath);
                        counter[Counters.Skipped] += 1;
                        return;
                    }

                    question = questions.OfType<QuestionActivity>().FirstOrDefault(x => x.ItemName.Best == questionName);
                    if (question == default(QuestionActivity))
                    {
                        Logger.Instance.Log.ErrorFormat("{0} questions were found named '{1}' within the scope. Please check {2}", questions.Count, questionName, filepath);
                        counter[Counters.Skipped] += 1;
                        return;
                    }
                }
                else
                {
                    question = questions.First() as QuestionActivity;
                }
                Repository.PopulateChildren(question);

                if (!_updateTracker.ContainsKey(variable.CompositeId))
                {
                    _updateTracker[variable.CompositeId] = new List<string>();
                    Repository.PopulateChildren(variable);
                    foreach (var sq in variable.SourceQuestions)
                    {
                        _updateTracker[variable.CompositeId].Add(sq.UserIds[0].Identifier);
                    }
                    foreach (var sq in variable.SourceQuestionGrids)
                    {
                        _updateTracker[variable.CompositeId].Add(sq.UserIds[0].Identifier);
                    }
                }

                if (question.Question != null)
                {
                    Func<string, bool> predicate = x => x == question.Question.UserIds[0].Identifier;
                    if (_updateTracker[variable.CompositeId].Any(predicate))
                    {
                        _updateTracker[variable.CompositeId].RemoveAll(new Predicate<string>(predicate));
                    }
                    else
                    {
                        variable.SourceQuestions.Add(question.Question);
                        updated = true;
                    }
                }
                if (question.QuestionGrid != null)
                {
                    Func<string, bool> predicate = x => x == question.QuestionGrid.UserIds[0].Identifier;
                    if (_updateTracker[variable.CompositeId].Any(predicate))
                    {
                        _updateTracker[variable.CompositeId].RemoveAll(new Predicate<string>(predicate));
                    }
                    else
                    {
                        variable.SourceQuestionGrids.Add(question.QuestionGrid);
                        updated = true;
                    }
                }
            } 
            else 
            {
                variable.SourceQuestions.Clear();
                variable.SourceQuestionGrids.Clear();
                updated = true;
            }
            if (updated)
            {
                UpdatedItems.Add(variable);
            }
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

        private void RemoveOldMappings()
        {
            foreach (var update in _updateTracker)
            {
                if (update.Value.Count > 0)
                {
                    var variable = Repository.GetItem(update.Key) as Variable;
                    foreach (var userId in update.Value)
                    {
                        var source = Repository.GetItem(userId);
                        var question = source as Question;
                        if (question != null)
                        {
                            variable.SourceQuestions.Remove(question);
                            continue;
                        }
                        var questionGrid = source as QuestionGrid;
                        if (questionGrid != null)
                        {
                            variable.SourceQuestionGrids.Remove(questionGrid);
                        }
                        UpdatedItems.Add(variable);
                    }
                }
            }
            _updateTracker.Clear();
        }
    }
}
