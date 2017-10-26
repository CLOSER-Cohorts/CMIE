using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysCon = System.Console;

using CMIE.Events;

namespace CMIE.Console
{
    class CommitCommand : ICommand
    {
        public CommitCommand(EventManager em) : base(em)
        {
            aliases = new string[] { "commit" };
        }

        public override Commands Type
        {
            get
            {
                return Commands.COMMIT;
            }
        }

        public override bool Do(string[] arguments)
        {
            SysCon.Write("Commit comment: ");
            var rationale = SysCon.ReadLine();
            EventManager.FireEvent(new CommitEvent(rationale));
            return true;
        }

        public override string GetFormattedGuidance()
        {
            return "commit          Submits all staged work to the repository.";
        }
    }
}
