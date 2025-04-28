using System.Collections.Generic;

namespace Framework
{
    public interface IFSMMgr
    {
        IFSM CreateFSM<StartState>(string name, params FSMState[] states) where StartState : FSMState;
        IFSM CreateFSM(string name, params FSMState[] states);
        void DestroyFSM(IFSM fsm);
        IFSM GetFSM(string name);
        IEnumerable<IFSM> GetAllFSMs();
    }
}