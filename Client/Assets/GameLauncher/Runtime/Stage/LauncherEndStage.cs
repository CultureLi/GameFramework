using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Launcher.Runtime.Stage
{
    internal class LauncherEndStage : StageBase
    {
        protected internal override void OnEnter()
        {
            Assembly assembly = null;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var ass in assemblies)
            {
                var name = ass.GetName().Name;

                if (name.Contains("Base.Runtime"))
                {
                    Debug.Log($"basesssss: {name} {ass.Location}");
                }

                if (name.Contains("Logic"))
                {
                    Debug.Log($"asName: {name} {ass.Location}");
                    if (string.IsNullOrEmpty(ass.Location))
                    {
                        assembly = ass;
                        break;
                    }
                    Debug.Log($"asName: {name} {ass.Location}");
                }
            }

            //var gameMain = assemblies.First(a => a.GetName().Name == "Logic.Runtime");
            Type entry = assembly?.GetType("GameMain.Runtime.Logic.GameMainEntry");
            entry?.GetMethod("Entry").Invoke(null, null);
        }
    }
}
