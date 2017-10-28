using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMIE.Events
{
    enum EventType
    {
        QUIT,
        STATUS,
        LIST_SCOPE_OPTIONS,
        UPDATE_SELECTED,
        EVALUATE,
        LOAD_CONTROL_FILE,
        LIST_AVAILABLE_COMMANDS,
        UPDATE_COMMAND,
        BUILD,
        JOB_COMPLETED,
        COMMIT,
        MAP
    }
}
