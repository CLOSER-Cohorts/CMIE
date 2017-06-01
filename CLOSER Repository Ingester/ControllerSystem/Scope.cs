using System;
using System.Linq;

using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model;

namespace CLOSER_Repository_Ingester.ControllerSystem
{
    class Scope : WorkArea
    {
        public string name { get; set; }
        public ResourcePackage rp { get; set; }
        public StudyUnit su { get; set; }
        private Comparator comparator;

        public Scope(string _name)
        {
            name = _name;
            Init();
            comparator = new Comparator(workingSet);
        }

        public void AddAction(IAction _action)
        {
            actions.Add(_action);
        }

        public void Build()
        {
            foreach (var action in actions)
            {
                try
                {
                    action.Validate();
                    workingSet.AddRange(action.Build(workingSet));
                    counter[Counters.Total] = workingSet.Count;
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0}", e.StackTrace);
                    Console.WriteLine("{0}", e.Message);
                }
            }
        }

        public void Compare()
        {
            if (workingSet.Count == 0) return;

            if (rp == default(ResourcePackage))
            {
                //New resource package
                rp = new ResourcePackage
                {
                    DublinCoreMetadata =
                    {
                        Title = new MultilingualString(name, "en-GB")
                    }
                };
            }

            var client = Utility.GetClient();
            var graphPopulator = new GraphPopulator(client)
            {
                ChildProcessing = ChildReferenceProcessing.PopulateLatest
            };
            rp.Accept(graphPopulator);
            var gatherer = new ItemGathererVisitor();
            rp.Accept(gatherer);
            var rpItems = gatherer.FoundItems.ToList();
            comparator.repoSet = rpItems;

            foreach (var item in rpItems) item.IsDirty = false;


            DataCollection dc = null;
            if (rp.DataCollections.Count == 1)
            {
                dc = rp.DataCollections.First();
            }

            var wsRPs = workingSet.OfType<ResourcePackage>();

            Guid[] dcBindings = { 
                DdiItemType.Instrument
            };
            Guid[] suBindings = { 
                DdiItemType.LogicalProduct,
                DdiItemType.PhysicalDataProduct,
                DdiItemType.PhysicalInstance,
            };

            var updated = false;
            foreach (var wsRP in wsRPs)
            {
                foreach (var item in wsRP.GetChildren())
                {
                    item.IsDirty = false;
                    var rpFind = rpItems.FirstOrDefault(x => x.UserIds.Count > 0 ? item.UserIds[0].Identifier == x.UserIds[0].Identifier : false);
                    if (rpFind == default(IVersionable))
                    {
                        counter[Counters.Added] += item.GetChildren().Count + 1;
                        rp.AddItem(item);
                        if (dc != null && dcBindings.Contains(item.ItemType))
                        {
                            dc.AddChild(item);
                            continue;
                        }

                        if (dc != null && item.ItemType == DdiItemType.InstrumentScheme)
                        {
                            foreach (var instrument in item.GetChildren())
                            {
                                dc.AddChild(instrument);
                            }
                        }

                        if (su != default(StudyUnit) && suBindings.Contains(item.ItemType))
                        {
                            su.AddChild(item);
                            continue;
                        }

                        item.IsDirty = true;
                        var gthr = new ItemGathererVisitor();
                        item.Accept(gthr);
                        foreach (var i in gthr.FoundItems)
                        {
                            i.IsDirty = true;
                        }
                    }
                    else
                    {
                        rpFind.IsDirty = false;
                        updated = true;
                        counter[Counters.Compared] += comparator.Compare(rpFind, item);
                    }
                }
            }

            var allGthr = new ItemGathererVisitor();
            rp.Accept(allGthr);
            toBeAdded.AddRange(allGthr.FoundItems);
        }
    }
}
