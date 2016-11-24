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

        public Scope(string _name)
        {
            name = _name;
            actions = new List<IAction>();
            workingSet = new List<IVersionable>();
            toBeAdded = new List<IVersionable>();
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
            foreach (var wsRP in wsRPs)
            {
                foreach (var item in wsRP.GetChildren())
                {
                    var rpFinds = rpItems.Where(x => x.UserIds.Count > 0 ? item.UserIds[0].Identifier == x.UserIds[0].Identifier : false);
                    if (rpFinds.Count() == 0)
                    {
                        Console.WriteLine("Adding item");
                        rp.AddItem(item);
                        Guid[] rpBindings = { 
                                                DdiItemType.InstrumentScheme,
                                                DdiItemType.Instrument
                                            };
                        if (dc != null && rpBindings.Contains(item.ItemType))
                        {
                            dc.AddChild(item);
                            continue;
                        }
                        Guid[] suBindings = { 
                                                DdiItemType.LogicalProduct,
                                                DdiItemType.PhysicalDataProduct,
                                                DdiItemType.PhysicalInstance,
                                            };
                        if (su != default(StudyUnit) && suBindings.Contains(item.ItemType))
                        {
                            su.AddChild(item);
                            continue;
                        }
                    }
                    else
                    {

                    }
                }
            }
            
            var gthr = new ItemGathererVisitor();
            rp.Accept(gthr);
            toBeAdded.AddRange(gthr.FoundItems);
        }
    }
}
