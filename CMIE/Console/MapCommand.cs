using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CMIE.Events;

namespace CMIE.Console
{
    class MapCommand : ICommand
    {
        public MapCommand(EventManager em) : base(em)
        {
            aliases = new string[] { "map" };
        }

        public override Commands Type
        {
            get
            {
                return Commands.MAP;
            }
        }

        public override bool Do(string[] arguments)
        {
            if (arguments.Length == 1)
            {
                EventManager.FireEvent(new MapEvent());
            }
            else
            {
                var i = 2;
                MapEvent.MappingType mappingType;
                switch(arguments[1])
                {
                    case "qv":
                        mappingType = MapEvent.MappingType.QV;
                        break;

                    case "dv":
                        mappingType = MapEvent.MappingType.DV;
                        break;

                    case "rv":
                        mappingType = MapEvent.MappingType.RV;
                        break;

                    default:
                        mappingType = MapEvent.MappingType.ALL;
                        i--;
                        break;
                }
                if (arguments.Length == i)
                {
                    EventManager.FireEvent(new MapEvent(mappingType));
                }
                for (; i < arguments.Length; i++)
                    EventManager.FireEvent(
                        new MapEvent(
                            arguments[i],
                            mappingType
                            )
                        );
            }
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "map             Performs all forms of mapping between items already in the repository.";
        }
    }
}
