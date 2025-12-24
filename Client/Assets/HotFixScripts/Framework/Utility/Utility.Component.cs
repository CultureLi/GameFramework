using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public static partial class Utility
    {
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            var com = self.GetComponent<T>();
            if (!com)
                com = self.AddComponent<T>();
            return com;
        }
        public static T GetOrAddComponent<T>(this Component self) where T : Component
        {
            var com = self.GetComponent<T>();
            if (!com)
                com = self.gameObject.AddComponent<T>();
            return com;
        }
        public static Component GetOrAddComponent(this GameObject self, Type type)
        {
            var com = self.GetComponent(type);
            if (!com)
                com = self.AddComponent(type);
            return com;
        }
        public static Component GetOrAddComponent(this Component self, Type type)
        {
            var com = self.GetComponent(type);
            if (!com)
                com = self.gameObject.AddComponent(type);
            return com;
        }
    }
}