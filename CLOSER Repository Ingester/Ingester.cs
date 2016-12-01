using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;

using CLOSER_Repository_Ingester.ControllerSystem;

namespace CLOSER_Repository_Ingester
{
    class Ingester : WorkArea
    {
        string controlFilepath;
        Controller controller;

        public void Init(string controlFilepath) 
        {
            MultilingualString.CurrentCulture = "en-GB";
            VersionableBase.DefaultAgencyId = "uk.closer";
            this.controlFilepath = controlFilepath;
        }

        public void SetBasePath(string basePath)
        {
            controller.basePath = basePath;
        }

        public bool Prepare(string basePath = null)
        {
            controller = new Controller(controlFilepath);
            controller.basePath = basePath;
            actions = controller.globalActions;

            bool good = true;
            try
            {
                controller.loadFile();
            } catch (Exception e) {
                console.WriteLine("{0}", e);
                good = false;
            }
            return good;
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

        public void RunGlobalActions(bool prepare = false)
        {
            bool prepared = false;
            if (prepare)
            {
                prepared = Prepare();
            }
            if (prepare == prepared)
            {
                foreach (var action in actions)
                {
                    try 
                    {
                        action.Validate();
                        workingSet.AddRange(action.Build(workingSet));
                    }
                    catch (Exception e)
                    {
                        console.WriteLine("{0}", e.Message);
                        continue;
                    }
                }
                var client = Utility.GetClient();
                var facet = new SearchFacet();
                facet.ItemTypes.Add(DdiItemType.DdiInstance);
                facet.SearchTargets.Add(DdiStringType.Name);

                //Compare
                var repoItems = new List<IVersionable>();
                var wsIs = workingSet.OfType<DdiInstance>();

                foreach (var wsI in wsIs)
                {
                    facet.SearchTerms.Clear();
                    facet.SearchTerms.Add(wsI.ItemName.Best);
                    var response = client.Search(facet);
                    foreach (var res in response.Results)
                    {
                        var rp = client.GetItem(
                        res.CompositeId,
                        ChildReferenceProcessing.PopulateLatest) as DdiInstance;
                        var graphPopulator = new GraphPopulator(client)
                        {
                            ChildProcessing = ChildReferenceProcessing.PopulateLatest
                        };
                        rp.Accept(graphPopulator);
                        var gatherer = new ItemGathererVisitor();
                        rp.Accept(gatherer);
                        repoItems.AddRange(gatherer.FoundItems);
                    }
                }
                toBeAdded = workingSet;
                var toBeRemoved = new List<IVersionable>();
                foreach (var repoItem in repoItems)
                {
                    if (repoItem.UserIds.Count == 0) continue;
                    var wsItem = workingSet.Find(x => (x.UserIds.Count > 0 ? x.UserIds[0].ToString() : "") == repoItem.UserIds[0].ToString());
                    if (wsItem != default(IVersionable))
                    {
                        if (toBeAdded.IndexOf(wsItem) != -1)
                        {
                            toBeAdded.Remove(wsItem);
                        }
                        else
                        {
                            var node = toBeAdded.Where(
                                item => item.UserIds.Count > 0
                            ).FirstOrDefault(
                                item => item.UserIds[0] == wsItem.UserIds[0]
                            );
                            if (node != default(IVersionable))
                            {
                                toBeAdded.Remove(node);
                            }
                        }
                    }
                    else
                    {
                        toBeRemoved.Add(repoItem);
                    }
                }

                console.WriteLine("Global: Commiting {0} items...", workingSet.Count);
                client.RegisterItems(workingSet, new CommitOptions());
            }
            else
            {
                console.WriteLine("Failed to prepare build.");
            }
            console.Publish();
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
                    group.Build();
                    console.Publish();
                    group.CompareWithRepository();

                    console.WriteLine("{0}: About to commit to repository, do you want to continue? (y/N)", group.name);
                    console.Publish();
                    var response = Console.ReadLine().ToLower();
                    if (response.Length > 0 && response[0].Equals('y'))
                    {
                        console.Write("{0}: Committing", group.name);
                        group.Commit();
                        console.WriteLine("{0}: Done.", group.name);
                    }
                    else
                    {
                        console.Write("{0}: No changes committed.", group.name);
                    }
                    console.Publish();
                }
            }
            else
            {
                console.WriteLine("Failed to prepare build.");
                console.Publish();
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
