using Assets.GameEngine.Runtime.Module;
using Assets.GameMain.Runtime.Events;
using Bright.Serialization;
using GameEngine.Runtime.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameMain.Runtime.Entrance
{
    public partial class GameMainModule:ModuleBase
    {
        public override void OnInit(InitModuleParamBase param)
        {
            ModuleManager.Instance.Init();
            GameModule.InitBuiltinModules();
            GameModule.InitCustomModules();
        }

        public override void OnFixUpdate(float elapseSeconds, float realElapseSeconds)
        {
            throw new System.NotImplementedException();
        }

        public override void OnLateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            throw new System.NotImplementedException();
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            throw new System.NotImplementedException();
        }

        public override void Release()
        {
            throw new System.NotImplementedException();
        }

        private void Start()
        {
            //var tables = new cfg.Tables(LoadByteBuf);
            //var itemTable = new cfg.item.TbItem(LoadByteBuf("item_tbitem"));
            //UnityEngine.Debug.LogFormat("item[1].name:{0}", itemTable[10000].Name);
            //UnityEngine.Debug.LogFormat("item[1].name:{0}", tables.TbItem[10000].Name);
            //Debug.LogFormat("bag init capacity:{0}", tables.TbItem);

            UnityEngine.Debug.Log("== load succ==Hello world !!!!");

            /*GameModule.EventModule.BroadCastAsync<LogEvent>(e =>
            {
                e.content = "Hello World";
            });*/
        }
        

        private static ByteBuf LoadByteBuf(string file)
        {
            return new ByteBuf(File.ReadAllBytes($"{Application.dataPath}/BundleRes/Config/{file}.bytes"));
        }

        private void Update()
        {
            ModuleManager.Instance.Update(Time.deltaTime,Time.realtimeSinceStartup);
        }

        private void FixedUpdate()
        {
            ModuleManager.Instance.FixUpdate(Time.deltaTime, Time.realtimeSinceStartup);
        }

        private void LateUpdate()
        {
            ModuleManager.Instance.LateUpdate(Time.deltaTime, Time.realtimeSinceStartup);
        }

        private void OnDestroy()
        {
            ModuleManager.Instance.Release();
        }
    }
}
