using System;

namespace Framework
{
    public abstract class FSMMsg
    {
        public bool MustHandle { get; set; }
        public bool SubFirst
        {
            get => _subFirst != SubFirstState.False;
            set
            {
                if (!TrySetSubFirst(value))
                {
                    UnityEngine.Debug.LogWarningFormat("{0} can only be set once.", nameof(SubFirst));
                }
            }
        }
        public bool TrySetSubFirst(bool value)
        {
            if (_subFirst == SubFirstState.Unset)
            {
                _subFirst = value ? SubFirstState.True : SubFirstState.False;
                return true;
            }
            return false;
        }
        SubFirstState _subFirst = SubFirstState.Unset;
        enum SubFirstState { Unset, False, True }

        public new virtual Type GetType() => base.GetType();
    }

    public interface IFSM
    {
        string Name { get; }
        int StateCount { get; }
        bool IsRunning { get; }
        bool IsDestroyed { get; }
        FSMState CurrentState { get; }
        void Start(object userData = null);
        void Start(Type type, object userData = null);
        void Restart(object userData = null);
        void Restart(Type type, object userData = null);
        bool HasState<T>() where T : FSMState;
        T GetState<T>() where T : FSMState;
        FSMState[] GetAllStates();
        void ChangeState<T>(object userData = null) where T : FSMState;
        void ChangeState(Type type, object userData = null);
        bool SendMessage(FSMMsg msg);
        void PostMessage(FSMMsg msg);
    }
}
