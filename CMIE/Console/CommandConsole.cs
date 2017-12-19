using System;
using System.Collections.Generic;
using System.Linq;
using SysCon = System.Console;

using CMIE.Events;

namespace CMIE.Console
{
    internal class CommandConsole : IEventListener
    {
        private readonly bool _verbose;
        private readonly EventManager _eventManager;
        private readonly List<string> _prompt;
        private readonly List<ICommand> _availableCommands;
        public CommandConsole(EventManager eventManager, bool verbose = false)
        {
            _verbose = verbose;
            _eventManager = eventManager;
            _prompt = new List<string>();
            _availableCommands = new List<ICommand>();
        }

        public void DeregisterCommand(ICommand command)
        {
            if (IsCommandRegistered(command))
            {
                _availableCommands.Remove(command);
            }
            else
            {
                if (_verbose) SysCon.WriteLine("Attempted to deregister command \"{0}\", but not present.", command.GetType().Name);
            }
        }

        public void RegisterCommand(ICommand command)
        {
            if (IsCommandRegistered(command))
            {
                if (_verbose) SysCon.WriteLine("Attempted to register command \"{0}\" twice.", command.GetType().Name);
            }
            else
            {
                _availableCommands.Add(command);
            }
        }

        public void Run()
        {
            while (!ParseCommand(Prompt())) {}
        }

        private bool IsCommandRegistered(ICommand command)
        {
            return _availableCommands.Any(x => x.Type == command.Type);
        }

        private bool ParseCommand(string command)
        {
            if (command == "") return false;
            
            var cmd = _availableCommands.Find(x => x.IsMatch(command));

            if (cmd != default(ICommand)) return cmd.Do();
            
            SysCon.WriteLine("Command not recogonised: {0}", command.Split(' ').First().ToLower());
            return false;
        }

        private bool ListOptionsCommand(IReadOnlyList<string> arguments)
        {
            if (arguments.Count > 1)
            {
                if (arguments[1].ToLower() == "scopes")
                {
                    _eventManager.FireEvent(new ListScopeOptionsEvent() { Groups = false });
                }
                else if (arguments[1].ToLower() == "groups")
                {
                    _eventManager.FireEvent(new ListScopeOptionsEvent() { Scopes = false });
                }
                else
                {
                    SysCon.WriteLine("ls only accepts 'scopes', 'groups' or neither");
                    return false;
                }
            }
            else
            {
                _eventManager.FireEvent(new ListScopeOptionsEvent());
            }
            return true;
        }

        private string Prompt()
        {
            foreach (var line in _prompt)
            {
                SysCon.WriteLine(line);
            }
            SysCon.Write("-> ");
            return SysCon.ReadLine();
        }

        public void OnEvent(IEvent _event)
        {
            switch(_event.Type)
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
            foreach (var cmd in _availableCommands)
            {
                SysCon.WriteLine(cmd.GetFormattedGuidance());
            }
        }

        private void OnUpdateCommand(IEvent _event)
        {
            var updateCommandEvent = (UpdateCommandEvent)_event;
            if (updateCommandEvent.Action == UpdateCommandEvent.Actions.ADD)
            {
                RegisterCommand(ResolveCommand(updateCommandEvent.Command));
            }
            else
            {
                DeregisterCommand(ResolveCommand(updateCommandEvent.Command));
            }
        }

        private ICommand ResolveCommand(Commands commandType)
        {
            switch (commandType)
            {
                case Commands.UPDATE:
                    return new UpdateCommand(_eventManager);
                case Commands.COMMIT:
                    return new CommitCommand(_eventManager);
                case Commands.MAP:
                    return new MapCommand(_eventManager);
                case Commands.REMOVE_SELECTION:
                    return new RemoveSelectionCommand(_eventManager);
            }
            throw new Exception("Command type not recongisised.");
        }
    }
}
