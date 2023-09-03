using GameEngine.Runtime.Base.Utilitys;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Runtime.Base.Procedure
{
    public partial class LauncherBase : MonoBehaviour
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
            procedureMgr.Initialize(procedures);
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
