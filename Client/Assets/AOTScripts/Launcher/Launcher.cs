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
        Assembly entranceAssembly;
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

            entranceAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Entrance");

            if (entranceAssembly == null)
            {
                Debug.LogError("û���ҵ�Entrance");
                return;
            }
            Type entry = entranceAssembly.GetType("Entrance.EntranceMgr");
            if (entry == null)
            {
                Debug.LogError("û���ҵ�EntranceMgr");
                return;
            }
            var method = entry.GetMethod("Entry");
            if (method == null)
            {
                Debug.LogError("û���ҵ�Entry Method");
                return;
            }
            method.Invoke(null, null);
        }

        private void OnDestroy()
        {

        }
    }

}
