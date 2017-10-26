using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SysCon = System.Console;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;

using CMIE.ControllerSystem.Actions;
using CMIE.ControllerSystem.Resources;

namespace CMIE.ControllerSystem
{
    class Group : WorkArea
    {
        public string name { get; private set; }
        public int numberItemsToCommit
        {
            get
            {
                var count = 0;
                count += toBeAdded.Count;
                foreach (var scope in scopes)
                    count += scope.Value.toBeAdded.Count;
                return count;
            }
        }
        public ConcurrentDictionary<string, Scope> scopes;

        public Group(string name)
        {
            this.name = name;
            scopes = new ConcurrentDictionary<string, Scope>();
            Init();
        }

        public void AddAction(IAction action)
        {
            actions.Add(action);
        }

        public void AddResource(IResource resource)
        {
            resources.Add(resource);
        }

        public void AddAction(string scope, IAction action)
        {
            if (!scopes.ContainsKey(scope))
            {
                scopes[scope] = new Scope(scope);
            }
            scopes[scope].AddAction(action);
        }

        public void AddResource(string scope, IResource resource)
        {
            if (!scopes.ContainsKey(scope))
            {
                scopes[scope] = new Scope(scope);
            }
            scopes[scope].AddResource(resource);
        }

        public void Build(bool include_globals = false)
        {
            Parallel.ForEach<IAction>(actions, action =>
            {
                if (action is TXTFileAction) return;
                SysCon.WriteLine("{0}: Validating {1}", name, action.scope);
                action.Validate();
                workingSet.AddRange(action.Build(workingSet));
            });
            PublishConsole();
            var progress = new ParallelProgressMonitor(scopes.Count);
            foreach (var scope in scopes)
            {
                string text = String.Format("{0}: Building {1}", name, scope.Value.name);
                progress.StartThread(
                    Thread.CurrentThread.ManagedThreadId,
                    text
                    );
                scope.Value.Build();
                progress.FinishThread(
                    Thread.CurrentThread.ManagedThreadId,
                    text.PadRight(40, '-') + "> done. (" + String.Format("{0} items)", scope.Value.counter[Counters.Total]).PadLeft(12)
                    );
            }
            var globalWS = new List<IVersionable>();
            globalWS.AddRange(workingSet);
            foreach (var scope in scopes)
            {
                globalWS.AddRange(scope.Value.workingSet);
            }
            Parallel.ForEach<IAction>(actions, action =>
            {
                if (!(action is TXTFileAction)) return;
                SysCon.WriteLine("{0}: Validating {1}", name, action.scope);
                action.Validate();
                workingSet.AddRange(action.Build(globalWS));
            });
            PublishConsole();
            //Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            //{
            //    string text = String.Format("{0}: Building {1}", name, scope.Value.name);
            //    progress.StartThread(
            //        Thread.CurrentThread.ManagedThreadId, 
            //        text
            //        );
            //    scope.Value.Build();
            //    progress.FinishThread(
            //        Thread.CurrentThread.ManagedThreadId,
            //        text.PadRight(40,'-') + "> done. (" + String.Format("{0} items)", scope.Value.counter[Counters.Total]).PadLeft(12)
            //        );
            //});
            if (include_globals)
            {
                workingSet.AddRange(ControllerSystem.Actions.LoadTVLinking.FinishedAllBuilds());
                workingSet.AddRange(ControllerSystem.Actions.LoadTQLinking.FinishedAllBuilds());
            }
            
            foreach (var scope in scopes)
            {
                scope.Value.PublishConsole();
            }
        }

        public void CompareWithRepository()
        {
            /*var client = Utility.GetClient();

            var facet = new SearchFacet() 
            { 
                SearchLatestVersion = true
            };
            facet.ItemTypes.Add(DdiItemType.StudyUnit);
            var response = client.Search(facet);
            foreach (var result in response.Results)
            {
                var su = client.GetItem(
                        result.CompositeId,
                        ChildReferenceProcessing.PopulateLatest) as StudyUnit;
                foreach (var dc in su.DataCollections)
                {
                    try
                    {
                        var scope = scopes[dc.ItemName.Best];
                        scope.su = su;
                        scope.rp = su.ResourcePackages.Where(x => x.ItemName.Best == dc.ItemName.Best).FirstOrDefault();
                    } catch(KeyNotFoundException)
                    {
                    }
                }
            }

            foreach (var scope in scopes)
            {
                if (scope.Value.rp != default(ResourcePackage)) continue;

                var wsRps = workingSet.OfType<ResourcePackage>().Where( x => string.Compare(
                    x.DublinCoreMetadata.Title.Best, scope.Value.name
                    ) == 0
                );
                if (wsRps.Any())
                {
                    scope.Value.rp = wsRps.First();
                    var bubbleOut = false;
                    foreach (var g in workingSet.OfType<Algenta.Colectica.Model.Ddi.Group>())
                    {
                        foreach (var su in g.StudyUnits)
                        {
                            if (su.DataCollections.Count(x => x.ItemName.Best == scope.Key) > 0)
                            {
                                scope.Value.su = su;
                                var gatherer = new ItemGathererVisitor();
                                g.Accept(gatherer);
                                toBeAdded.AddRange(gatherer.FoundItems);
                                bubbleOut = true;
                            }
                            if (bubbleOut) break;
                        }
                        if (bubbleOut) break;
                    }
                }
                else
                {
                    var rp = new ResourcePackage();
                    rp.DublinCoreMetadata.Title["en-GB"] = scope.Key;
                    scope.Value.su.AddChild(rp);
                    toBeAdded.Add(scope.Value.su);
                    rp.AddChild(scope.Value.su.DataCollections.First(x => x.ItemName.Best == scope.Key));
                    scope.Value.rp = rp;
                }
            }

            var ccgs = workingSet.OfType<ControlConstructGroup>();
            if (ccgs.Count() > 1)
            {
                facet.ItemTypes.Clear();
                facet.SearchTargets.Clear();
                facet.ItemTypes.Add(DdiItemType.QuestionConstruct);
                facet.SearchTargets.Add(DdiStringType.UserId);
                foreach (var ccg in ccgs)
                {
                    var qcs = ccg.GetChildren().OfType<QuestionActivity>().ToList();
                    foreach (var qc in ccg.GetChildren().OfType<QuestionActivity>())
                    {
                        facet.SearchTerms.Clear();
                        facet.SearchTerms.Add(qc.UserIds.First().Identifier);

                        response = client.Search(facet);
                        if (response.Results.Count > 1)
                        {
                            SysCon.WriteLine("{0} question constrcuts found during CCG syncing for the question '{1}'", response.Results.Count, qc.UserIds.First().Identifier);
                        }
                        else if (response.Results.Count < 1)
                        {
                            SysCon.WriteLine("No question constructs were found for the CCG syncing matching '{0}'", qc.UserIds.First().Identifier);
                        }
                        else
                        {
                            var remote_qc = client.GetItem(response.Results.First().CompositeId) as IVersionable;
                            if (remote_qc != default(IVersionable))
                            {
                                ccg.ReplaceChild(qc.CompositeId, remote_qc);
                            }
                        }
                    }
                }
            }

            var vgs = workingSet.OfType<VariableGroup>();
            if (vgs.Count() > 0) 
            {
                facet.ItemTypes.Clear();
                facet.ItemTypes.Add(DdiItemType.Variable);
                foreach (var vg in vgs)
                {
                    foreach (var variable in vg.GetChildren().OfType<Variable>())
                    {

                        facet.SearchTerms.Clear();
                        facet.SearchTargets.Clear();
                        bool closer_id_found = false;
                        foreach (var user_id in variable.UserIds)
                        {
                            if (user_id.Type == "closer:id")
                            {
                                closer_id_found = true;
                                facet.SearchTerms.Add(user_id.Identifier);
                                facet.SearchTargets.Add(DdiStringType.UserId);
                                break;
                            }
                        }
                        if (!closer_id_found) 
                        {
                            facet.SearchTerms.Add(variable.ItemName.Best);
                            facet.SearchTargets.Add(DdiStringType.Name);
                        }
                        response = client.Search(facet);
                        if (response.Results.Count > 1)
                        {
                            SysCon.WriteLine("{0} variables found during variable group syncing for the variable '{1}:{2}'", response.Results.Count, name, variable.ItemName.Best);
                        }
                        else if (response.Results.Count < 1)
                        {
                            SysCon.WriteLine("No variables were found for the variable grouping syncing matching '{0}:{1}'", name, variable.ItemName.Best);
                        }
                        else
                        {
                            var remote_variable = client.GetItem(response.Results.First().CompositeId) as IVersionable;
                            if (remote_variable != default(IVersionable))
                            {
                                vg.ReplaceChild(variable.CompositeId, remote_variable);
                            }
                        }
                    }
                }
            }

            var progress = new ParallelProgressMonitor(scopes.Count);
            Parallel.ForEach<KeyValuePair<string, Scope>>(scopes, scope =>
            {
                string text = String.Format("{0}: Comparing {1}", name, scope.Value.name);
                progress.StartThread(
                    Thread.CurrentThread.ManagedThreadId,
                    text
                    );
                scope.Value.Compare();
                progress.FinishThread(
                    Thread.CurrentThread.ManagedThreadId,
                    text.PadRight(40, '-') + 
                    "> done." + 
                    String.Format("{0} compared.", scope.Value.counter[Counters.Compared]).PadLeft(16) +
                    String.Format("{0} updated.", scope.Value.counter[Counters.Updated]).PadLeft(16) +
                    String.Format("{0} added.", scope.Value.counter[Counters.Added]).PadLeft(16) +
                    String.Format("{0} removed.", scope.Value.counter[Counters.Removed]).PadLeft(16)
                    );
            });*/
        }

        public void Commit()
        {
            var client = Utility.GetClient();
            var facet = new SetSearchFacet();
            facet.ItemTypes.Add(DdiItemType.Group);
            facet.ItemTypes.Add(DdiItemType.DdiInstance);
            facet.ReverseTraversal = true;
            var toCommit = new List<IVersionable>();
            toCommit.AddRange(toBeAdded);
            toCommit.AddRange(workingSet.OfType<VariableScheme>());
            toCommit.AddRange(workingSet.OfType<VariableGroup>());
            toCommit.AddRange(workingSet.OfType<ControlConstructScheme>());
            toCommit.AddRange(workingSet.OfType<ControlConstructGroup>());
            foreach (var scope in scopes)
            {
                toCommit.AddRange(scope.Value.toBeAdded);
            }
            var versioner = new Versioner();

            var acceptedTypes = new List<Guid>() {
                DdiItemType.ResourcePackage,
                DdiItemType.DataCollection,
                DdiItemType.InstrumentScheme,
                DdiItemType.Instrument,
                DdiItemType.VariableScheme,
                DdiItemType.ControlConstructScheme
            };
            var joints = toCommit.Where(x => acceptedTypes.Contains(x.ItemType)).ToList();
            var tops = new Dictionary<IdentifierTriple,IVersionable>();
            foreach (var joint in joints)
            {
                var set = client.SearchTypedSet(joint.CompositeId, facet);
                foreach (var parent in set)
                {
                    var top = client.GetItem(
                            parent.CompositeId,
                            ChildReferenceProcessing.PopulateLatest
                            ) as IVersionable;
                    if (top != null)
                    {
                        tops[top.CompositeId] = top;
                    }
                }
            }

            foreach (var top in tops)
            {
                toCommit.Add(top.Value);
                var one_down = top.Value.GetChildren().ToList();
                for (var i = 0; i < one_down.Count; i++)
                {
                    toCommit.Add(one_down[i]);
                    IVersionable su_joint;
                    if ((su_joint = toCommit.FirstOrDefault(x => one_down[i].CompositeId.Identifier == x.CompositeId.Identifier)) != default(IVersionable))
                    {
                        top.Value.ReplaceChild(one_down[i].CompositeId, su_joint);
                    }
                    else
                    {
                        foreach (var child in one_down[i].GetChildren().ToList())
                        {
                            var bottom_joint = toCommit.FirstOrDefault(x => x.CompositeId == child.CompositeId);
                            if (bottom_joint != default(IVersionable))
                            {
                                one_down[i].ReplaceChild(child.CompositeId, bottom_joint);
                            }

                        }
                    }
                }
                versioner.IncrementDityItemAndParents(top.Value);
            }
            client.RegisterItems(toCommit, new CommitOptions());
        }

        public ICollection<string> GetScopes()
        {
            return scopes.Keys;
        }

        public void Validate()
        {
            foreach(var scope in scopes)
            {
                scope.Value.Validate();
            }
        }
    }
}
