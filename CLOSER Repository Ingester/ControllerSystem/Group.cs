using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLOSER_Repository_Ingester.ControllerSystem
{
    class Group
    {
        public string name { get; private set; }
        List<IAction> actions;
        
        public Group(string name)
        {
            this.name = name;
            actions = new List<IAction>();
        }

        public void addAction(IAction action)
        {
            actions.Add(action);
        }

        public void Build()
        {
            foreach (IAction action in this.actions)
            {
                try
                {
                    action.Validate();
                    action.Build();
                }
                catch(Exception e)
                {
                    Console.WriteLine("{0}", e.Message);
                }
            }
        }

        public List<string> GetAllScopes()
        {
            List<string> scopes = new List<string>();

            foreach (IAction action in this.actions)
            {
                scopes.Add(action.scope);
            }

            return scopes;
        }
    }
}
