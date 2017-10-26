using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    interface IEventListener
    {
        void OnEvent(IEvent _event);
    }
}
