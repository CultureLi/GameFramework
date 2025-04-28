using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    internal class FSMMgr : IFSMMgr, FrameworkModule
    {
        private List<FSM> _fsms = new List<FSM>();
         int Priority => 1;

        public IFSM CreateFSM<StartState>(string name, params FSMState[] states) where StartState : FSMState
        {
            if (name == null || GetFSM(name) != null)
            {
                throw new Exception(String.Format("CreateFSM by invalid name {0}", name));
            }
            FSM fsm = new FSM(name);
            fsm.Initialize<StartState>(states);

            _fsms.Add(fsm);
            return fsm;
        }

        public IFSM CreateFSM(string name, params FSMState[] states)
        {
            if (name == null || GetFSM(name) != null)
            {
                throw new Exception(String.Format("CreateFSM by invalid name {0}", name));
            }
            FSM fsm = new FSM(name);
            fsm.Initialize(states);

            _fsms.Add(fsm);
            return fsm;
        }

        public IFSM GetFSM(string name)
        {
            foreach (var fsm in _fsms)
            {
                if (fsm.Name == name)
                {
                    return fsm;
                }
            }
            return null;
        }

        public IEnumerable<IFSM> GetAllFSMs()
        {
            return _fsms;
        }

        public void DestroyFSM(IFSM fsm)
        {
            var f = fsm as FSM;
            if (f == null)
            {
                throw new Exception("DestroyFSM by invalid fsm object");
            }
            _fsms.Remove(f);

            try
            {
                f.Destory();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception occurs in DestroyFSM {fsm.CurrentState}");
            }
        }

        public void Shutdown()
        {
            for (int i = 0; i < _fsms.Count; i++)
            {
                var fsm = _fsms[i];
                try
                {
                    fsm.Destory();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception occurs in FSM.Destory {fsm.CurrentState}");
                }
            }
            _fsms.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            for (int i = 0; i < _fsms.Count; i++)
            {
                var fsm = _fsms[i];
                try
                {
                    fsm.Update(elapseSeconds, realElapseSeconds);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception occurs in FSM.Update {fsm.CurrentState}");
                }
            }
        }
    }
}