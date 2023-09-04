using GameEngine.Runtime.Base.Variable;
using System.Collections.Generic;
namespace GameEngine.Runtime.Base.Blackboard
{
    public class BlackboardBase
    {
        protected Dictionary<string, VariableBase> data = new();

        public void SetValue(string key, VariableBase value)
        {
            data[key] = value;
        }

        public bool Has(string key)
        {
            return data.ContainsKey(key);
        }

        public T GetValue<T>(string key) where T : VariableBase
        {
            return (T)data[key];
        }
    }
}
