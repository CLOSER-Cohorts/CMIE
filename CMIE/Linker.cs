using System;
using System.Linq;
using SysCon = System.Console;

using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model;

using System.Collections.Generic;

using CMIE.Events;
using CMIE.ControllerSystem;

namespace CMIE
{
    internal class Linker : IJob
    {
        private readonly EventManager _eventManager;
        private readonly Controller _controller;
        private readonly string _host;
        private readonly Repository _repository;
        private RepositoryClientBase _client;
        private List<IVersionable> _updatedItems;

        public Linker(EventManager eventManager, Controller controller, string host, Repository repository)
        {
            _updatedItems = new List<IVersionable>();
            _eventManager = eventManager;
            _controller = controller;
            _host = host;
            _repository = repository;
        }

        public List<IVersionable> Clear()
        {
            var items = new List<IVersionable>();
            while (_updatedItems.Any())
            {
                items.Add(_updatedItems[0]);
                _updatedItems.RemoveAt(0);
            }
            return items;
        }

        public void Run()
        {
            _client = Utility.GetClient(_host);
            foreach (var link in _controller.links)
            {
                if (link.Evaluate(_client))
                {
                    var tmp = System.Console.ForegroundColor;
                    System.Console.ForegroundColor = System.ConsoleColor.Green;
                    System.Console.WriteLine("Good");
                    System.Console.ForegroundColor = tmp;
                }
                else
                {
                    bool updateParent;
                    while (true)
                    {
                        System.Console.WriteLine("Would you like to create a new parent version to connect to the latest child version? (y/n)");
                        var response = System.Console.ReadLine().ToLower().Substring(0, 1);
                        if (response == "y")
                        {
                            updateParent = true;
                            break;
                        }
                        if (response == "n")
                        {
                            updateParent = false;
                            break;
                        }
                        System.Console.WriteLine("I don't understand, try again.");
                    }

                    if (updateParent)
                    {
                        _updatedItems.AddRange(link.Build(_repository));
                    }
                }
            }
        }
    }
}
