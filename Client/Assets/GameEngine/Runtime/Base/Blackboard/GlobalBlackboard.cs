
using GameEngine.Runtime.Base.Blackboard;
using GameEngine.Runtime.Base.Variable;

namespace GameEngine.Runtime.Base
{
    public static class GlobalBlackboard
    {
        static BlackboardBase blackboard = new BlackboardBase();

        public static void SetValue(string key, VariableBase value)
        {
            blackboard.SetValue(key, value);
        }

        public static bool Has(string key)
        {
            return blackboard.Has(key);
        }

        public static T GetValue<T>(string key) where T : VariableBase
        {
            return blackboard.GetValue<T>(key);
        }
    }
}
