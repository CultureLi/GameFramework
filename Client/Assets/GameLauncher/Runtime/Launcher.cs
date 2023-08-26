using GameEngine.Runtime.Fsm;
using GameEngine.Runtime.Procedure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLauncher.Runtime
{
    public partial class Launcher : MonoBehaviour
    {
        [SerializeField]
        private string[] m_AvailableProcedureTypeNames = null;

        [SerializeField]
        private string m_EntranceProcedureTypeName = null;


        ProcedureManager m_ProcedureMgr = new();
        FsmManager m_FsmManager = new();
        ProcedureBase m_EntranceProcedure = null;
        public ProcedureBase CurrentProcedure
        {
            get { return m_ProcedureMgr.CurrentProcedure; }
        }


        void Awake()
        {
            ///创建所有Procedure
            List<ProcedureBase> procedureList = new();

            foreach (var procedureName in m_AvailableProcedureTypeNames)
            {
                Type procedureType = Type.GetType(procedureName);
                if (procedureType == null)
                {
                    throw new Exception($"Can not create procedure instance '{procedureName}'.");
                }

                var procedure = (ProcedureBase)Activator.CreateInstance(procedureType);
                if (procedure == null)
                {
                    throw new Exception($"Can not create procedure instance '{procedureName}'.");
                }

                procedureList.Add(procedure);

                if (m_EntranceProcedureTypeName == procedureName)
                {
                    m_EntranceProcedure = procedure;
                }
            }

            if (m_EntranceProcedure == null)
            {
                throw new Exception($"entranceProcedure {m_EntranceProcedureTypeName} not exist !!");
            }
            m_ProcedureMgr.Initialize(m_FsmManager, procedureList.ToArray());
        }


        void Start()
        {
            //开始入口Procedure
            m_ProcedureMgr.StartProcedure(m_EntranceProcedure.GetType());
        }

        // Update is called once per frame
        void Update()
        {
            m_ProcedureMgr.Update(Time.deltaTime, Time.realtimeSinceStartup);
            m_FsmManager.Update(Time.deltaTime, Time.realtimeSinceStartup);
        }

        void OnDestroy()
        {
            m_ProcedureMgr.Release();
            m_FsmManager.Release();
        }
    }

}
