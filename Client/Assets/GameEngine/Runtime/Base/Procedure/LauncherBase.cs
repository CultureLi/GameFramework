using GameEngine.Runtime.Base.Utilitys;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Runtime.Base.Procedure
{
    public partial class LauncherBase : MonoBehaviour
    {

        [SerializeField]
        private string[] m_AvailableProcedureTypeNames = null;

        [SerializeField]
        private string m_EntranceProcedureTypeName = null;


        ProcedureManager m_ProcedureMgr = new();
        ProcedureBase m_EntranceProcedure = null;
        public ProcedureBase CurrentProcedure
        {
            get { return m_ProcedureMgr.CurrentProcedure; }
        }


        protected void Awake()
        {
            List<ProcedureBase> procedureList = new();

            foreach (var procedureName in m_AvailableProcedureTypeNames)
            {
                Type procedureType = Utility.Assembly.GetType(procedureName);
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
            m_ProcedureMgr.Initialize(procedureList.ToArray());
        }


        protected void Start()
        {
            m_ProcedureMgr.StartProcedure(m_EntranceProcedure.GetType());
        }

        // Update is called once per frame
        protected void Update()
        {
            m_ProcedureMgr.Update(Time.deltaTime, Time.realtimeSinceStartup);
        }

        protected void OnDestroy()
        {
            m_ProcedureMgr.Release();
        }
    }

}
