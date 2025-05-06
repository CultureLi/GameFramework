using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace Launcher
{
    public partial class Launcher : MonoBehaviour
    {
        private void Awake()
        {
            Addressables.InitializeAsync().WaitForCompletion();
        }

        private void Start()
        {
            StartCoroutine(DoLaunch());
        }

        private IEnumerator DoLaunch()
        {
            yield return LoadMetaData();
            yield return DoHotFixTasks();

            EnterEntrance();
        }

        void EnterEntrance()
        {
            Debug.Log("EnterEntrance");

            var entranceAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "GameEntry");

            if (entranceAssembly == null)
            {
                Debug.LogError("没有找到Entrance");
                return;
            }
            Type entry = entranceAssembly.GetType("GameEntry.GameEntryMgr");
            if (entry == null)
            {
                Debug.LogError("没有找到GameEntryMgr");
                return;
            }
            var method = entry.GetMethod("Entry");
            if (method == null)
            {
                Debug.LogError("没有找到Entry Method");
                return;
            }
            method.Invoke(null, null);
        }

        private void OnDestroy()
        {

        }
    }

}
