using System;
using System.Linq;
using System.Collections.Generic;
using SysCon = System.Console;

using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model;

using CMIE.ControllerSystem.Actions;
using CMIE.ControllerSystem.Resources;

namespace CMIE.ControllerSystem
{
    class Scope : WorkArea
    {
        public string name { get; set; }
        public bool update { get; set; }
        public bool IsValid
        {
            get
            {
                return Actions.All(x => x.valid) && Resources.All(x => x.valid);
            }
        }
        private Dictionary<IdentifierTriple, IVersionable> repoBindingPoints;

        public Scope(string _name)
        {
            name = _name;
            update = false;
            Init();
            repoBindingPoints = new Dictionary<IdentifierTriple, IVersionable>();
        }

        public void AddAction(IAction action)
        {
            Actions.Add(action);
        }

        public void AddBindingPoint(IdentifierTriple repoId, IVersionable localItem)
        {
            repoBindingPoints[repoId] = localItem;
        }

        public void AddResource(IResource resource)
        {
            Resources.Add(resource);
        }

        public void Build()
        {
            if (!Resources.All(x => x.valid))
            {
                SysCon.WriteLine("Error: '{0}' could not build as not all resources are valid.", name);
                return;
            }

            foreach (var resource in Resources)
            {
                try
                {
                    WorkingSet.AddRange(resource.Build(WorkingSet));
                    Counter[Counters.Total] = WorkingSet.Count;
                }
                catch (Exception e)
                {
                    SysCon.WriteLine("{0}", e.StackTrace);
                    SysCon.WriteLine("{0}", e.Message);
                }
            }
        }

        public void Compare()
        {
/*            if (workingSet.Count == 0) return;

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
            toBeAdded.AddRange(allGthr.FoundItems);*/
        }

        public Dictionary<IdentifierTriple, IVersionable> GetBindingPoints()
        {
            return repoBindingPoints;
        }

        public List<IVersionable> GetUpdates()
        {
            return ToBeAdded;
        }

        public void  FindUpdates()
        {
            if (!Actions.All(x => x.valid))
            {
                SysCon.WriteLine("Error: '{0}' could not find updates as not all actions are valid.", name);
                return;
            }

            if (update)
            {
                Compare();
            }
            else
            {
                ToBeAdded.AddRange(WorkingSet);
            }
        }

        public List<Tuple<string, IVersionable>> GetBindings()
        {
            var output = new List<Tuple<string, IVersionable>>();

            foreach (var resource in Resources)
            {
                if (resource.Parent.Length < 2) continue;

                foreach (var bp in resource.BindingPoints)
                {
                    output.Add(Tuple.Create(resource.Parent, bp));
                }
            }

            return output;
        }

        public void Validate()
        {
            foreach (var action in Actions)
            {
                try
                {
                    action.Validate();
                }
                catch (Exception e)
                {
                    Logger.Instance.Log.ErrorFormat("{0}", e.Message);
                }
            }

            foreach (var resource in Resources)
            {
                try
                {
                    resource.Validate();
                }
                catch (Exception e)
                {
                    Logger.Instance.Log.ErrorFormat("{0}", e.Message);
                }
            }
        }
    }
}
