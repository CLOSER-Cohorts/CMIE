using System;
using System.Collections.Generic;
using SysCon = System.Console;

using Algenta.Colectica.Model.Utility;

using CMIE.Events;
using CMIE.ControllerSystem;
using CMIE.Console;

namespace CMIE
{
    class Application : IEventListener
    {
        private string buildDirectory;
        private string controlFile;
        private string host;
        private bool keepGoing;
        private bool quit;
        private EventManager eventManager;
        private Controller controller;
        private CommandConsole console;
        private Committer committer;
        private Repository repository;
        private Mapper mapper;
        private Queue<IJob> pendingJobs;
        private List<IJob> completedJobs;

        public Application(string[] args = null)
        {
            buildDirectory = null;
            controlFile = null;
            keepGoing = false;
            quit = false;
            host = "localhost";
            if (args != null) ParseArgs(args);
        }

        public bool Initialize()
        {
            if (controlFile == null)
            {
                SysCon.WriteLine("No control file was specified.");
                return false;
            }
            SysCon.SetWindowSize(
                Math.Min(150, SysCon.LargestWindowWidth),
                Math.Min(60, SysCon.LargestWindowHeight)
            );

            eventManager = new EventManager();
            controller = new Controller(eventManager, controlFile);
            repository = new Repository(host);
            committer = new Committer(eventManager, repository, host);
            mapper = new Mapper(repository); 

            console = new CommandConsole(eventManager);

            pendingJobs = new Queue<IJob>();
            completedJobs = new List<IJob>();

            AddListeners();
            AddCommands();

            eventManager.FireEvent(new LoadControlFileEvent());

            return true;
        }

        public void ParseArgs(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-b")
                {
                    buildDirectory = args[i + 1];
                    i++;
                    continue;
                }
                if (args[i] == "-c")
                {
                    controlFile = args[i + 1];
                    i++;
                }
                if (args[i] == "-y")
                {
                    keepGoing = true;
                }
                if (args[i] == "-h")
                {
                    host = args[i + 1];
                    i++;
                    continue;
                }
            }
        }

        public void Run()
        {
            SysCon.WriteLine("Please enter the group or scope you would like to process.");
            SysCon.WriteLine("(Write ls [groups/scopes] to get a list of all available groups and scopes)");
            while (!quit)
            {
                RunJobs();
                console.Run();
            }
        }

        private void AddListeners()
        {
            eventManager.AddListener(EventType.LOAD_CONTROL_FILE, controller);
            eventManager.AddListener(EventType.LIST_SCOPE_OPTIONS, controller);
            eventManager.AddListener(EventType.UPDATE_SELECTED, controller);
            eventManager.AddListener(EventType.STATUS, controller);
            eventManager.AddListener(EventType.LIST_AVAILABLE_COMMANDS, console);
            eventManager.AddListener(EventType.UPDATE_COMMAND, console);
            eventManager.AddListener(EventType.JOB_COMPLETED, controller);
            eventManager.AddListener(EventType.JOB_COMPLETED, this);
            eventManager.AddListener(EventType.EVALUATE, this);
            eventManager.AddListener(EventType.BUILD, this);
            eventManager.AddListener(EventType.MAP, this);
            eventManager.AddListener(EventType.COMMIT, this);
            eventManager.AddListener(EventType.QUIT, this);
        }

        private void AddCommands()
        {
            console.RegisterCommand(new QuitCommand(eventManager));
            console.RegisterCommand(new HelpCommand(eventManager));
            console.RegisterCommand(new StatusCommand(eventManager));
            console.RegisterCommand(new EvaluateCommand(eventManager));
            console.RegisterCommand(new ListOptionsCommand(eventManager));
            console.RegisterCommand(new AddSelectionCommand(eventManager));
        }

        private void RunJobs()
        {
            while (pendingJobs.Count > 0)
            {
                var job = pendingJobs.Dequeue();

                SysCon.WriteLine("Processing job. {0} jobs remaining.", pendingJobs.Count);

                job.Run();

                completedJobs.Add(job);
            }
        }

        public void OnEvent(IEvent _event)
        {
            switch (_event.Type)
            {
                case EventType.QUIT:
                    quit = true;
                    break;

                case EventType.BUILD:
                    OnBuild(_event);
                    break;

                case EventType.COMMIT:
                    OnCommit(_event);
                    break;

                case EventType.EVALUATE:
                    pendingJobs.Enqueue(new Evaluation(eventManager, controller, host));
                    break;

                case EventType.JOB_COMPLETED:
                    OnJobCompleted(_event);
                    break;

                case EventType.MAP:
                    OnMap(_event);
                    break;

                default:
                    SysCon.WriteLine("Application could not handle event.");
                    break;
            }
        }

        private void OnBuild(IEvent _event)
        {
            var buildEvent = (BuildEvent)_event;
            if (buildEvent.All)
            {
                foreach (var scope in controller.GetSelectedScopes())
                {
                    // Remove duplicate code
                    if (scope.update)
                    {
                        pendingJobs.Enqueue(new Comparison(eventManager, scope, host));
                    }
                    else
                    {
                        pendingJobs.Enqueue(committer.AddToCommit(scope));
                    }
                }
            }
            else
            {
                var scope = controller.GetScope(buildEvent.Scope);
                if (scope.update)
                {
                    pendingJobs.Enqueue(new Comparison(eventManager, scope, host));
                }
                else
                {
                    pendingJobs.Enqueue(committer.AddToCommit(scope));
                }
            }
        }

        private void OnCommit(IEvent _event)
        {
            var commitEvent = (CommitEvent)_event;
            committer.Commit(commitEvent.Rationale);
        }

        private void OnJobCompleted(IEvent _event)
        {
            throw new NotImplementedException();
        }

        private void OnMap(IEvent _event)
        {
            try
            {
                var mapEvent = (MapEvent)_event;
                if (mapEvent.AllScopes)
                {
                    foreach (var scope in controller.GetSelectedScopes())
                    {
                        if (mapEvent.QVMap())
                        {
                            pendingJobs.Enqueue(mapper.QV(scope));
                        }
                        if (mapEvent.DVMap())
                        {
                            pendingJobs.Enqueue(mapper.DV(scope));
                        }
                        if (mapEvent.RVMap())
                        {
                            pendingJobs.Enqueue(mapper.RV(scope));
                        }
                    }
                }
                else
                {
                    var scope = controller.GetScope(mapEvent.Scope);
                    if (mapEvent.QVMap())
                    {
                        pendingJobs.Enqueue(mapper.QV(scope));
                    }
                    if (mapEvent.DVMap())
                    {
                        pendingJobs.Enqueue(mapper.DV(scope));
                    }
                    if (mapEvent.RVMap())
                    {
                        pendingJobs.Enqueue(mapper.RV(scope));
                    }
                }
                pendingJobs.Enqueue(committer.AddToCommit(mapper.Clear));
            }
            catch(Exception e)
            {
                Logger.Instance.Log.Error(e.Message);
            }
        }
    }
}
