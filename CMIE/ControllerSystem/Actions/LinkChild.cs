using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;

namespace CMIE.ControllerSystem.Actions
{
    class LinkChild : IAction
    {
        private string _parentId;
        private string _childId;

        public LinkChild(string parentId, string childId) 
        {
            _parentId = parentId;
            _childId = childId;
        }

        public override void Validate() { }

        public bool Evaluate(RepositoryClientBase client)
        {
            var parentPeices = _parentId.Split(':');
            if (parentPeices.Length != 2)
            {
                throw new Exception("Parent identifier is wrong for link:'" + _parentId + "'");
            }
            var childPeices = _childId.Split(':');
            if (childPeices.Length != 2)
            {
                throw new Exception("Parent identifier is wrong for link:'" + _childId + "'");
            }
            var parentIdentifier = Guid.Parse(parentPeices[1]);
            var childIdentifier = Guid.Parse(childPeices[1]);
            var parentTriples = client.GetVersions(parentIdentifier, parentPeices[0]);
            var childTriples = client.GetVersions(childIdentifier, childPeices[0]);

            var latestParent = client.GetItem(parentTriples.First());
            var latestChild = client.GetItem(childTriples.First());

            var map = new Dictionary<long, long>();
            foreach (var parentTriple in parentTriples)
            {
                var parent = client.GetItem(parentTriple);
                foreach (var parentChild in parent.GetChildren())
                {
                    var childFound = childTriples.FirstOrDefault(x => x == parentChild.CompositeId);
                    if (childFound != default(IdentifierTriple))
                    {
                        map[parent.CompositeId.Version] = childFound.Version;
                        break;
                    }
                }
            }

            System.Console.WriteLine(
                "Parent: {0}", 
                latestParent.GetType().GetProperty("DisplayLabel").GetValue(latestParent, null)
                );
            System.Console.WriteLine(
                "Child:  {0}",
                latestChild.GetType().GetProperty("DisplayLabel").GetValue(latestChild, null)
                );

            System.Console.WriteLine("Parent    Child");

            var good = true;
            long max = 0, min = 999999999;
            if (map.Values.Any())
            {
                max = map.Values.Max();
                min = map.Values.Min();
            }
            foreach (var childVersion in childTriples.Select(x => x.Version).Where(x => x > max))
            {
                System.Console.WriteLine("          {0,-3}", childVersion);
                good = false;
            }

            foreach (var parentTriple in parentTriples)
            {
                if (map.Any(x => x.Key == parentTriple.Version))
                {
                    System.Console.WriteLine("{0,6} -> {1,-3}", parentTriple.Version, map[parentTriple.Version]);
                }
                else
                {
                    System.Console.WriteLine("{0,6}", parentTriple.Version);
                }
            }
            return good;
        }

        public override IEnumerable<IVersionable> Build(Repository repository)
        {
            var parent = repository.GetLatestItem(_parentId);
            var child = repository.GetLatestItem(_childId);

            var oldChild = parent.GetChildren().FirstOrDefault(
                x => x.AgencyId == child.AgencyId && x.Identifier == child.Identifier
                );
            if (oldChild == default(IVersionable))
            {
                parent.AddChild(child);
            }
            else
            {
                parent.ReplaceChild(oldChild.CompositeId, child);
            }
            return new List<IVersionable> { parent };
        }
        
    }
}
