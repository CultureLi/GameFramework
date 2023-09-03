using GameEngine.Runtime.Base.Procedure;
using System;
using UnityEngine;

namespace GameEngine.Runtime.Base.Launcher
{
    public partial class LauncherBase : MonoBehaviour,ILauncher
    {
        ProcedureManager procedureMgr = new();
        Type entranceProcedure = null;

        public Type EntranceProcedure
        {
            set => entranceProcedure = value;
        }

        public ProcedureBase CurrentProcedure
        {
            get { return procedureMgr.CurrentProcedure; }
        }

        public void Initialize(params ProcedureBase[] procedures)
        {
            procedureMgr.Initialize(this,procedures);
        }

        protected void Start()
        {
            procedureMgr.StartProcedure(entranceProcedure);
        }

        // Update is called once per frame
        protected void Update()
        {
            procedureMgr.Update(Time.deltaTime, Time.realtimeSinceStartup);
        }

        protected void OnDestroy()
        {
            procedureMgr.Release();
        }
    }

}
