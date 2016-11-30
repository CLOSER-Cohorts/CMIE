using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository.Client;
using Algenta.Colectica.Model.Ddi;

namespace CLOSER_Repository_Ingester.ControllerSystem
{
    class Scope
    {
        public string name { get; set; }
        public ResourcePackage rp { get; set; }
        public StudyUnit su { get; set; }
        private List<IAction> actions;
        private List<IVersionable> workingSet;
        public List<IVersionable> toBeAdded { get; private set; }
        private Comparator comparator;

        public Scope(string _name)
        {
            name = _name;
            actions = new List<IAction>();
            workingSet = new List<IVersionable>();
            toBeAdded = new List<IVersionable>();
            comparator = new Comparator();
        }

        public void AddAction(IAction _action)
        {
            actions.Add(_action);
        }

        public void Build()
        {
            foreach (IAction action in actions)
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
                rp = new ResourcePackage();
                rp.DublinCoreMetadata.Title = new MultilingualString(name, "en-GB");
            }

            var client = Utility.GetClient();
            var graphPopulator = new GraphPopulator(client);
            graphPopulator.ChildProcessing = ChildReferenceProcessing.PopulateLatest;
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

            bool updated = false;
            foreach (var wsRP in wsRPs)
            {
                foreach (var item in wsRP.GetChildren())
                {
                    var rpFinds = rpItems.Where(x => x.UserIds.Count > 0 ? item.UserIds[0].Identifier == x.UserIds[0].Identifier : false);
                    if (rpFinds.Count() == 0)
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
            Console.WriteLine("{0} items have been ammedned from {1}.", comparator.amendments.Count, name);

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
