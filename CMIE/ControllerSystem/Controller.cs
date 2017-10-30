using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysCon = System.Console;

using CMIE.ControllerSystem.Actions;
using CMIE.ControllerSystem.Resources;
using CMIE.Console;
using CMIE.Events;

namespace CMIE.ControllerSystem
{
    class Controller : IEventListener
    {
        string filepath;
        bool verbose;
        private EventManager eventManager;
        public string basePath { get; set; }
        public List<Group> groups { get; private set; }
        public List<IAction> globalActions { get; private set; }
        public List<IResource> globalResources { get; private set; }
        private Utility.ObservableCollectionUnique<string> selected;

        public Controller(EventManager eventManager, string filepath, bool verbose = false)
        {
            this.eventManager = eventManager;
            this.filepath = filepath;
            this.verbose = verbose;
            Reset();
            selected.CollectionChanged += SelectedUpdated;
        }

        public void AddSelected(string scope)
        {
            if (GetOptions().Any(x => x == scope))
            {
                if (!selected.Add(scope))
                {
                    SysCon.WriteLine("Error: '{0}' is already selected.", scope);
                }
            }
            else
            {
                SysCon.WriteLine("Error: '{0}' is not a valid selection.", scope);
            }
        }

        public List<string> GetOptions(bool groups = true, bool scopes = true)
        {
            var output = new List<string>();

            foreach (var group in this.groups)
            {
                if (groups) output.Add(group.name);
                if (scopes) output.AddRange(group.GetScopes());
            }

            return output;
        }

        public List<Scope> GetSelectedScopes()
        {
            var selectedScopes = new List<Scope>();

            foreach (var scope in selected)
            {
                var group = groups.FirstOrDefault(x => x.name == scope);
                if (group == default(Group))
                {
                    foreach (var g in groups)
                    {
                        if (g.scopes.ContainsKey(scope))
                        {
                            selectedScopes.Add(g.scopes[scope]);
                            break;
                        }
                    }
                }
                else
                {
                    selectedScopes.AddRange(group.scopes.Values);
                }
            }

            return selectedScopes;
        }

        public Scope GetScope(string scopeName)
        {
            foreach (var group in groups)
            {
                if (group.scopes.ContainsKey(scopeName))
                {
                    return group.scopes[scopeName];
                }
            }
            throw new Exception(scopeName + " was now found in any group.");
        }

        public bool HasSelected()
        {
            return selected.Any();
        }

        public void loadFile()
        {
            loadFile(filepath);
        }

        public void loadFile(string filepath)
        {
            if (verbose) SysCon.WriteLine("Started loading {0}", filepath);
            var lines = File.ReadAllLines(filepath);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length == 0) continue;
                if (trimmedLine[0] == '#') continue;
                if (verbose) SysCon.WriteLine("Parsing lined: {0}", trimmedLine);
                parseLine(trimmedLine);
            }
            if (verbose) SysCon.WriteLine("Finished loading {0}", filepath);
            Validate();
        }

        public void RemoveSelected(string scope)
        {
            if (GetOptions().Any(x => x == scope))
            {
                if (!selected.Remove(scope))
                {
                    SysCon.WriteLine("Error: '{0}' was not selected.", scope);
                }
            }
            else
            {
                SysCon.WriteLine("Error: '{0}' is not a valid selection.", scope);
            }
        }

        public void Reset()
        {
            groups = new List<Group>();
            globalActions = new List<IAction>();
            selected = new Utility.ObservableCollectionUnique<string>();

            
        }

        public void Validate()
        {
            foreach (var group in groups)
            {
                group.Validate();
            }
        }

        private void parseLine(string line)
        {
            var pieces = line.Split(new char[] { '\t' });
            Group group;

            if (pieces.Length > 1)
            {
                switch (pieces[0].ToLower())
                {
                    case "group":
                        groups.Add(new Group(pieces[1]));
                        return;

                    case "rootdir":
                        basePath = pieces[1];
                        return;

                    case "control":
                        loadFile(Path.Combine(Path.GetDirectoryName(filepath), pieces[1]));
                        return;

                    case "concepts":
                        globalResources.Add(new LoadTopics(BuildFilePath(pieces[1])));
                        return;

                    default:
                        group = groups.Find(x => string.Compare(x.name, pieces[1]) == 0);
                        break;
                }
                if (group != null)
                {
                    switch (pieces[0].ToLower())
                    {
                        case "instrument":
                            if (pieces.Length > 5)
                            {
                                group.AddResource(pieces[2], new LoadInstrument(pieces[3],BuildFilePath(pieces[4]), pieces[5]));
                            }
                            else
                            {
                                group.AddResource(pieces[2], new LoadInstrument(pieces[3],BuildFilePath(pieces[4])));
                            }
                            break;

                        case "rpackage":
                            group.AddResource(pieces[2], new LoadResourcePackage(pieces[3], BuildFilePath(pieces[4])));
                            break;

                        case "studysweep":
                            group.AddResource(new LoadStudySweep(BuildFilePath(pieces[2])));
                            break;

                        case "dataset":
                                group.AddResource(pieces[2], new LoadDataset(pieces[3],BuildFilePath(pieces[4])));
                            break;

                        case "qvmapping":
                            if (pieces.Length > 3)
                            {
                                group.AddAction(pieces[2], new LoadQVMapping(BuildFilePath(pieces[3])));
                            }
                            else
                            {
                                group.AddAction(new LoadQVMapping(BuildFilePath(pieces[2])));
                            }
                            break;

                        case "dvmapping":
                            group.AddAction(pieces[2], new LoadDVMapping(BuildFilePath(pieces[3])));
                            break;

                        case "rvmapping":
                            group.AddAction(pieces[2], new LoadRVMapping(BuildFilePath(pieces[3])));
                            break;

                        case "qblinking":
                            group.AddAction(pieces[2], new LoadQBLinking(BuildFilePath(pieces[3])));
                            break;

                        case "tqlinking":
                            group.AddAction(pieces[2], new LoadTQLinking(BuildFilePath(pieces[3])));
                            break;

                        case "tvlinking":
                            if (pieces.Length > 3)
                            {
                                group.AddAction(pieces[2], new LoadTVLinking(BuildFilePath(pieces[3])));
                            }
                            else
                            {
                                group.AddAction(new LoadTVLinking(BuildFilePath(pieces[2])));
                            }
                            break;

                        case "images":
                            string dirName, dirPath, ccsName;
                            if (pieces.Length > 4)
                            {
                                dirName = pieces[4];
                                dirPath = BuildFilePath(pieces[4]);
                                ccsName = pieces[3];
                            }
                            else
                            {
                                dirName = pieces[3];
                                dirPath = BuildFilePath(pieces[3]);
                                ccsName = pieces[2] + "_ccs01";
                            }
                            group.AddAction(pieces[2], new AttachExternalAids(ccsName, dirName, dirPath));
                            break;

                        case "pdf":
                            group.AddAction(pieces[2], new AttachExternalInstrument(pieces[3], BuildFilePath(pieces[4])));
                            break;

                        default:
                            SysCon.WriteLine("\"{0}\" could not be parsed. The group was found.", line);
                            break;
                    }
                }
                else
                {
                    if (verbose) SysCon.WriteLine("\"{0}\" could not be parsed. No group was found.", line);

                }
            }
        }

        private string BuildFilePath(string filepath)
        {
            var output = Path.Combine(basePath, filepath);

            output = output.Replace('/', Path.DirectorySeparatorChar);
            output = output.Replace('\\', Path.DirectorySeparatorChar);

            return output;
        }

        private void SelectedUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (selected.Count > 0)
            {
                eventManager.FireEvent(new UpdateCommandEvent(UpdateCommandEvent.Actions.ADD, Console.Commands.REMOVE_SELECTION));
            }
            else
            {
                eventManager.FireEvent(new UpdateCommandEvent(UpdateCommandEvent.Actions.REMOVE, Console.Commands.REMOVE_SELECTION));
            }
        }

        public void OnEvent(IEvent _event)
        {
            switch (_event.GetEventType())
            {
                case EventType.JOB_COMPLETED:
                    OnJobCompleted(_event);
                    break;

                case EventType.LIST_SCOPE_OPTIONS:
                    OnListScopeOptions(_event);
                    break;

                case EventType.LOAD_CONTROL_FILE:
                    OnLoadControlFile(_event);
                    break;

                case EventType.STATUS:
                    OnStatus();
                    break;

                case EventType.UPDATE_SELECTED:
                    OnUpdateSelected(_event);
                    break;

                default:
                    SysCon.WriteLine("Controller could not handle event.");
                    break;
            }
        }

        private void OnJobCompleted(IEvent _event)
        {
            var jobCompletedEvent = (JobCompletedEvent)_event;
            if (jobCompletedEvent.JobTypeCompleted == JobCompletedEvent.JobType.EVALUATION)
            {
                eventManager.FireEvent( 
                    new UpdateCommandEvent(UpdateCommandEvent.Actions.ADD, Commands.UPDATE)
                    );
                eventManager.FireEvent(
                    new UpdateCommandEvent(UpdateCommandEvent.Actions.ADD, Commands.MAP)
                    );
            }
        }

        private void OnListScopeOptions(IEvent _event)
        {
            var listScopeOptionsEvent = (ListScopeOptionsEvent)_event;
            SysCon.WriteLine("Options:");
            foreach (var option in GetOptions(listScopeOptionsEvent.Groups, listScopeOptionsEvent.Scopes))
            {
                SysCon.WriteLine(" - {0}", option);
            }
        }

        private void OnLoadControlFile(IEvent _event)
        {
            var loadControlFileEvent = (LoadControlFileEvent)_event;
            if (loadControlFileEvent.Reset)
            {
                Reset();
            }

            if (loadControlFileEvent.HasNewFile())
            {
                loadFile(loadControlFileEvent.Filepath);
            }
            else
            {
                loadFile();
            }
        }

        private void OnStatus()
        {
            SysCon.WriteLine("");
            SysCon.WriteLine("*** Controller ***");
            SysCon.WriteLine("Verbose:      {0}", verbose ? "Yes" : "No");
            SysCon.WriteLine("Control file: {0}", filepath);
            SysCon.WriteLine("Build dir:    {0}", basePath);
            SysCon.WriteLine("# of Groups:  {0}", groups.Count);
            SysCon.WriteLine("# of Scopes:  {0}", groups.Sum(x => x.GetScopes().Count));
            SysCon.WriteLine("# of Actions: {0}", globalActions.Count);

            SysCon.WriteLine("");
            SysCon.WriteLine("*** Selected ***");
            foreach (var scope in selected)
            {
                SysCon.WriteLine(" - {0}", scope);
            }
        }

        private void OnUpdateSelected(IEvent _event)
        {
            var updateSelectedEvent = (UpdateSelectedEvent)_event;
            switch (updateSelectedEvent.Action)
            {
                case UpdateSelectedEvent.Actions.ADD:
                    AddSelected(updateSelectedEvent.Scope);
                    break;

                case UpdateSelectedEvent.Actions.REMOVE:
                    RemoveSelected(updateSelectedEvent.Scope);
                    break;
            }
        }
    }
}
