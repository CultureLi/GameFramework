using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework
{
    public class FSM : IFSM
    {
        private readonly Dictionary<Type, FSMState> _states = new Dictionary<Type, FSMState>();
        private FSMState _startState;

        public string Name { private set; get; }
        public bool IsDestroyed { get; set; }
        public FSMState CurrentState { get; set; }

        public FSM(string name)
        {
            Name = name;
        }

        public void Initialize<StartState>(params FSMState[] states) where StartState : FSMState
        {
            Initialize(typeof(StartState), states);
        }

        public void Initialize(IEnumerable<FSMState> states)
        {
            var startType = states?.FirstOrDefault()?.GetType() ?? null;
            Initialize(startType, states);
        }

        void Initialize(Type startType, IEnumerable<FSMState> states)
        {
            IsDestroyed = false;
            CurrentState = null;

            void LoopBody(FSMState state)
            {
                AddState(state);
                if (state.GetType() == startType) _startState = state;
            }

            if (states is IList<FSMState> listStates) // Avoid Boxing
            {
                for (var i = 0; i < listStates.Count; i++)
                    LoopBody(listStates[i]);
            }
            else
            {
                foreach (var state in states)
                    LoopBody(state);
            }
        }
        public int StateCount
        {
            get { return _states.Count; }
        }

        public bool IsRunning
        {
            get { return CurrentState != null; }
        }

        public FSMState[] GetAllStates()
        {
            int index = 0;
            FSMState[] results = new FSMState[_states.Count];
            foreach (KeyValuePair<Type, FSMState> state in _states)
            {
                results[index++] = state.Value;
            }

            return results;
        }

        public T GetState<T>() where T : FSMState
        {
            return GetState(typeof(T)) as T;
        }

        private FSMState GetState(Type type)
        {
            FSMState state = null;
            if (_states.TryGetValue(type, out state))
            {
                return state;
            }
            return null;
        }

        public bool HasState<T>() where T : FSMState
        {
            return _states.ContainsKey(typeof(T));
        }

        public void AddState(FSMState state)
        {
            if (state == null)
            {
                throw new Exception("FSM state is invalid.");
            }

            Type stateType = state.GetType();
            if (_states.ContainsKey(stateType))
            {
                throw new Exception(String.Format("FSM '{0}' state '{1}' is already exist.", Name, stateType));
            }

            _states.Add(stateType, state);
        }

        public void Start(object userData = null)
        {
            if (IsRunning)
            {
                throw new Exception("FSM is running, can not start again.");
            }

            if (_startState == null)
            {
                throw new Exception(String.Format("FSM '{0}' can not start state which is not exist.", Name));
            }

            ChangeCurrentState(_startState, userData);
        }

        public void Start(Type type, object userData = null)
        {
            if (type != null)
            {
                if (IsRunning)
                {
                    throw new Exception("FSM is running, can not start again.");
                }

                _startState = GetState(type);
            }

            Start(userData);
        }

        public void Restart(object userData = null)
        {
            Stop();
            Start(userData);
        }

        public void Restart(Type type, object userData = null)
        {
            Stop();
            Start(type, userData);
        }

        public void Stop()
        {
            if (IsRunning)
            {
                ClearMsgs();
                CurrentState.Leave();
                CurrentState = null;
            }
        }

        public void ChangeState<T>(object userData = null) where T : FSMState
        {
            ChangeState(typeof(T), userData);
        }

        public void ChangeState(Type type, object userData = null)
        {
            FSMState state = GetState(type);
            if (state == null)
            {
                throw new Exception(String.Format("FSM '{0}' can not change state to '{1}' which is not exist.", Name, type.FullName));
            }

            if (state == CurrentState)
            {
                UnityEngine.Debug.LogWarning("you are entering the current active state, make sure this is what you want.");
            }

            CurrentState.Leave();
            ChangeCurrentState(state, userData);
        }

        void ChangeCurrentState(FSMState state, object userData)
        {
            CurrentState = state;
            if (!CurrentState.Inited)
                CurrentState.Init(this);
            CurrentState.Enter(userData);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            CurrentState?.Update(elapseSeconds, realElapseSeconds);
            PumpMsg();
        }

        public void Destory()
        {
            Stop();

            ClearMsgs();
            foreach (KeyValuePair<Type, FSMState> state in _states)
            {
                state.Value.Destroy();
            }

            Name = null;
            _states.Clear();
            CurrentState = null;
            IsDestroyed = true;
        }

        internal bool HandleMsg(FSMMsg msg)
        {
            if (IsRunning) return CurrentState.HandleMsg(msg);

            return false;
        }

        private List<FSMMsg> _msgs = new List<FSMMsg>();
        private List<FSMMsg> _toAddMsgs = new List<FSMMsg>();

        public bool SendMessage(FSMMsg msg)
        {
            if (!HandleMsg(msg))
            {
                if (msg.MustHandle) PostMessage(msg);

                return false;
            }

            return true;
        }
        public void PostMessage(FSMMsg msg)
        {
            _toAddMsgs.Add(msg);
        }

        void PumpMsg()
        {
            if (_toAddMsgs.Count > 0)
            {
                for (int i = 0; i < _toAddMsgs.Count; i++)
                {
                    _msgs.Add(_toAddMsgs[i]);
                }
                _toAddMsgs.Clear();
            }

            if (_msgs.Count > 0)
            {
                for (int i = 0; i < _msgs.Count; i++)
                {
                    FSMMsg msg = _msgs[i];
                    try
                    {
                        if (HandleMsg(msg) || !msg.MustHandle)
                        {
                            if (_msgs.Count == 0) return; // restart and cleared 
                            _msgs.RemoveAt(i--);
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogErrorFormat("Exception while handle msg: {0} {1}", msg, e);
                        if (_msgs.Count == 0) return; // restart and cleared 
                        _msgs.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        void ClearMsgs()
        {
            _toAddMsgs.Clear();
            _msgs.Clear();
        }
    }
}