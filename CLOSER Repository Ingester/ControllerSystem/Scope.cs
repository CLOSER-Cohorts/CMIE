using System;
using System.Linq;

using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;

namespace CLOSER_Repository_Ingester.ControllerSystem
{
    class Scope : WorkArea
    {
        public string name { get; set; }
        public ResourcePackage rp { get; set; }
        public StudyUnit su { get; set; }
        private readonly Comparator comparator;

        public Scope(string _name)
        {
            name = _name;
            comparator = new Comparator();
            Init();
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
                }
                catch (Exception e)
                {
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
            var rpItems = gatherer.FoundItems;

            DataCollection dc = null;
            if (rp.DataCollections.Count == 1)
            {
                dc = rp.DataCollections.First();
            }

            var wsRPs = workingSet.OfType<ResourcePackage>();
            Guid[] rpBindings = { 
                DdiItemType.InstrumentScheme,
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
                    var rpFinds = rpItems.Where(x => x.UserIds.Count > 0 ? item.UserIds[0].Identifier == x.UserIds[0].Identifier : false);
                    if (!rpFinds.Any())
                    {
                        rp.AddItem(item);
                        if (dc != null && rpBindings.Contains(item.ItemType))
                        {
                            dc.AddChild(item);
                            continue;
                        }
                        
                        if (su != default(StudyUnit) && suBindings.Contains(item.ItemType))
                        {
                            su.AddChild(item);
                            continue;
                        }
                    }
                    else
                    {
                        updated = true;
                        comparator.Compare(rpFinds.First(), item);
                    }
                }
            }

            if (updated)
            {
                var dirtyGthr = new DirtyItemGatherer(false, true);
                rp.Accept(dirtyGthr);
                foreach (var item in dirtyGthr.DirtyItems)
                {
                    item.Version++;
                }
            }

            var gthr = new ItemGathererVisitor();
            rp.Accept(gthr);
            toBeAdded.AddRange(gthr.FoundItems);
        }
    }
}
