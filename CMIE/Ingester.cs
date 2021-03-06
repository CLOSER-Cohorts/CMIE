﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysCon = System.Console;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;

using CMIE.ControllerSystem;

namespace CMIE
{
    class Ingester : WorkArea
    {
        bool keepGoing;
        Controller controller;

        public void Init(Controller controller, bool keepGoing) 
        {
            MultilingualString.CurrentCulture = "en-GB";
            VersionableBase.DefaultAgencyId = "uk.closer";
            this.controller = controller;
            this.keepGoing = keepGoing;
            base.Init();
        }

        public void Build()
        {
            foreach (var group in controller.groups)
            {
                group.Build();
            }
        }

        public void CompareWithRepository()
        {
            foreach (var group in controller.groups)
            {
                group.CompareWithRepository();
            }
        }

        public void Commit()
        {
            foreach (var group in controller.groups)
            {
                group.Commit();
            }
        }

        public bool Prepare()
        {
            return true;
        }

        public void RunByGroup(bool prepare = false)
        {
            var prepared = false;
            if (prepare)
            {
                prepared = Prepare();
            }
            if (prepare == prepared)
            {
                foreach (var group in controller.groups)
                {
                    var startTime = DateTime.Now;
                    SysCon.WriteLine("{0}: Building...", group.name);
                    group.Build(true);
                    Console.WriteLine("{0}: Done. ({1})", group.name, (DateTime.Now - startTime).ToString("%m' min. '%s' sec.'"));
                    PublishConsole();
                    startTime = DateTime.Now;
                    SysCon.WriteLine("{0}: Comparing with repo...", group.name);
                    group.CompareWithRepository();
                    Console.WriteLine("{0}: Done. ({1})", group.name, (DateTime.Now - startTime).ToString("%m' min. '%s' sec.'"));

                    Console.WriteLine("{0}: {1} items to commit.", group.name, group.numberItemsToCommit);
                    var response = "";
                    if (!keepGoing && group.numberItemsToCommit != 0)
                    {
                        Console.WriteLine("{0}: About to commit to repository, do you want to continue? (y/N)", group.name);
                        Console.Publish();
                        response = SysCon.ReadLine().ToLower();
                    }
                    else
                    {
                        Console.Publish();
                    }

                    if (((response.Length > 0 && response[0].Equals('y')) || keepGoing) && group.numberItemsToCommit > 0)
                    {
                        Console.Write("{0}: Committing... ", group.name);
                        Console.Publish();
                        group.Commit();
                        Console.WriteLine("Done.", group.name);
                    }
                    else
                    {
                        Console.WriteLine("{0}: No changes committed.", group.name);
                    }
                    Console.Publish();
                }
            }
            else
            {
                Console.WriteLine("Failed to prepare build.");
                Console.Publish();
            }
        }

        public void Run()
        {
            if (Prepare())
            {
                Build();
            }
        }
    }
}
