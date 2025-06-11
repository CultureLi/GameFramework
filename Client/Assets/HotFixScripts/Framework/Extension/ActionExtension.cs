using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public static class ActionExtension
    {
        public static bool InvokeSafely(this Action action)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public static bool InvokeSafely<T>(this Action<T> action, T arg)
        {
            try
            {
                action?.Invoke(arg);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

    }
}
