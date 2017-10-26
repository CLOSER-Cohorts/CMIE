using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysCon = System.Console;

using CMIE.Events;

namespace CMIE.Console
{
    class CommandConsole : IEventListener
    {
        private bool verbose;
        private EventManager eventManager;
        private List<string> prompt;
        private List<ICommand> availableCommands;
        public CommandConsole(EventManager eventManager, bool verbose = false)
        {
            this.verbose = verbose;
            this.eventManager = eventManager;
            this.prompt = new List<string>();
            this.availableCommands = new List<ICommand>();
        }

        public void DeregisterCommand(ICommand command)
        {
            if (IsCommandRegistered(command))
            {
                availableCommands.Remove(command);
            }
            else
            {
                if (verbose) SysCon.WriteLine("Attempted to deregister command \"{0}\", but not present.", command.GetType().Name);
            }
        }

        public void RegisterCommand(ICommand command)
        {
            if (IsCommandRegistered(command))
            {
                if (verbose) SysCon.WriteLine("Attempted to register command \"{0}\" twice.", command.GetType().Name);
            }
            else
            {
                availableCommands.Add(command);
            }
        }

        public void Run()
        {
            while (!ParseCommand(Prompt())) {}
        }

        private bool IsCommandRegistered(ICommand command)
        {
            return availableCommands.Any(x => x.Type == command.Type);
        }

        private bool ParseCommand(string command)
        {
            if (command == "")
            {
                return false;
            }
            else
            {
                ICommand cmd = availableCommands.Find(x => x.IsMatch(command));

                if (cmd == default(ICommand))
                {
                    SysCon.WriteLine("Command not recogonised: {0}", command.Split(' ').First().ToLower());
                    return false;
                }
                else
                {
                    return cmd.Do();
                }
            }
        }

        private bool ListOptionsCommand(string[] arguments)
        {
            if (arguments.Length > 1)
            {
                if (arguments[1].ToLower() == "scopes")
                {
                    eventManager.FireEvent(new ListScopeOptionsEvent() { Groups = false });
                }
                else if (arguments[1].ToLower() == "groups")
                {
                    eventManager.FireEvent(new ListScopeOptionsEvent() { Scopes = false });
                }
                else
                {
                    SysCon.WriteLine("ls only accepts 'scopes', 'groups' or neither");
                    return false;
                }
            }
            else
            {
                eventManager.FireEvent(new ListScopeOptionsEvent());
            }
            return true;
        }

        private string Prompt()
        {
            foreach (var line in prompt)
            {
                SysCon.WriteLine(line);
            }
            SysCon.Write("-> ");
            return SysCon.ReadLine();
        }

        public void OnEvent(IEvent _event)
        {
            switch(_event.GetEventType())
            {
                case EventType.LIST_AVAILABLE_COMMANDS:
                    OnListAvailabeCommands(_event);
                    break;

                case EventType.UPDATE_COMMAND:
                    OnUpdateCommand(_event);
                    break;

                default:
                    SysCon.WriteLine("Command Console could not handle event");
                    break;
            }
        }

        private void OnListAvailabeCommands(IEvent _event)
        {
            SysCon.WriteLine("Available commands:");
            foreach (var cmd in availableCommands)
            {
                SysCon.WriteLine(cmd.GetFormattedGuidance());
            }
        }

        private void OnUpdateCommand(IEvent _event)
        {
            var updateCommandEvent = (UpdateCommandEvent)_event;
            if (updateCommandEvent.action == UpdateCommandEvent.Actions.ADD)
            {
                RegisterCommand(ResolveCommand(updateCommandEvent.command));
            }
            else
            {
                DeregisterCommand(ResolveCommand(updateCommandEvent.command));
            }
        }

        private ICommand ResolveCommand(Commands commandType)
        {
            if (commandType == Commands.UPDATE)
            {
                return new UpdateCommand(eventManager);
            }
            else if (commandType == Commands.COMMIT)
            {
                return new CommitCommand(eventManager);
            }
            else if (commandType == Commands.REMOVE_SELECTION)
            {
                return new RemoveSelectionCommand(eventManager);
            }
            throw new Exception("Command type not recongisised.");
        }
    }
}
