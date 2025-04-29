using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 有限状态机。
    /// </summary>
    /// <typeparam name="T">有限状态机持有者类型。</typeparam>
    public sealed class Fsm : IFsm
    {
        private readonly Dictionary<Type, FsmState> states;
        private FsmState currentState;
        private float currentStateTime;
        private bool isDestroyed;
        private string name;

        public string Name => name;

        /// <summary>
        /// 获取有限状态机中状态的数量。
        /// </summary>
        public int FsmStateCount
        {
            get
            {
                return states.Count;
            }
        }

        /// <summary>
        /// 获取有限状态机是否正在运行。
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return currentState != null;
            }
        }

        /// <summary>
        /// 获取有限状态机是否被销毁。
        /// </summary>
        public bool IsDestroyed
        {
            get
            {
                return isDestroyed;
            }
        }

        /// <summary>
        /// 获取当前有限状态机状态。
        /// </summary>
        public FsmState CurrentState
        {
            get
            {
                return currentState;
            }
        }

        /// <summary>
        /// 获取当前有限状态机状态名称。
        /// </summary>
        public string CurrentStateName
        {
            get
            {
                return currentState != null ? currentState.GetType().FullName : null;
            }
        }

        /// <summary>
        /// 获取当前有限状态机状态持续时间。
        /// </summary>
        public float CurrentStateTime
        {
            get
            {
                return currentStateTime;
            }
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>创建的有限状态机。</returns>
        public static Fsm Create(string name, params FsmState[] states)
        {
            var stateList = new List<FsmState> {};
            stateList.AddRange(states);

            return Create(name, stateList);
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>创建的有限状态机。</returns>
        public static Fsm Create(string name, List<FsmState> states)
        {
            if (states == null || states.Count < 1)
            {
                throw new Exception("FSM states is invalid.");
            }

            Fsm fsm = new()
            {
                name = name,
                isDestroyed = false
            };
            foreach (FsmState state in states)
            {
                if (state == null)
                {
                    throw new Exception("FSM states is invalid.");
                }

                Type stateType = state.GetType();
                if (fsm.states.ContainsKey(stateType))
                {
                    throw new Exception(Utility.Text.Format("FSM state '{1}' is already exist.", stateType.FullName));
                }

                fsm.states.Add(stateType, state);
                state.OnInit(fsm);
            }

            return fsm;
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <typeparam name="TState">要开始的有限状态机状态类型。</typeparam>
        public void Start<TState>() where TState : FsmState
        {
            Start(typeof(TState));
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <param name="stateType">要开始的有限状态机状态类型。</param>
        public void Start(Type stateType)
        {
            if (IsRunning)
            {
                throw new Exception("FSM is running, can not start again.");
            }

            if (stateType == null)
            {
                throw new Exception("State type is invalid.");
            }

            if (!typeof(FsmState).IsAssignableFrom(stateType))
            {
                throw new Exception(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
            }

            FsmState state = GetState(stateType);
            if (state == null)
            {
                throw new Exception(Utility.Text.Format("FSM can not start state '{1}' which is not exist.", stateType.FullName));
            }

            currentStateTime = 0f;
            currentState = state;
            currentState.OnEnter();
        }

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要检查的有限状态机状态类型。</typeparam>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState<TState>() where TState : FsmState
        {
            return states.ContainsKey(typeof(TState));
        }

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <param name="stateType">要检查的有限状态机状态类型。</param>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState(Type stateType)
        {
            if (stateType == null)
            {
                throw new Exception("State type is invalid.");
            }

            if (!typeof(FsmState).IsAssignableFrom(stateType))
            {
                throw new Exception(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
            }

            return states.ContainsKey(stateType);
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要获取的有限状态机状态类型。</typeparam>
        /// <returns>要获取的有限状态机状态。</returns>
        public TState GetState<TState>() where TState : FsmState
        {
            FsmState state = null;
            if (states.TryGetValue(typeof(TState), out state))
            {
                return (TState)state;
            }

            return null;
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <param name="stateType">要获取的有限状态机状态类型。</param>
        /// <returns>要获取的有限状态机状态。</returns>
        public FsmState GetState(Type stateType)
        {
            if (stateType == null)
            {
                throw new Exception("State type is invalid.");
            }

            if (!typeof(FsmState).IsAssignableFrom(stateType))
            {
                throw new Exception(Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
            }

            FsmState state = null;
            if (states.TryGetValue(stateType, out state))
            {
                return state;
            }

            return null;
        }

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <returns>有限状态机的所有状态。</returns>
        public FsmState[] GetAllStates()
        {
            int index = 0;
            FsmState[] results = new FsmState[states.Count];
            foreach (KeyValuePair<Type, FsmState> state in states)
            {
                results[index++] = state.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <param name="results">有限状态机的所有状态。</param>
        public void GetAllStates(List<FsmState> results)
        {
            if (results == null)
            {
                throw new Exception("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<Type, FsmState> state in states)
            {
                results.Add(state.Value);
            }
        }

        /// <summary>
        /// 有限状态机轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (currentState == null)
            {
                return;
            }

            currentStateTime += elapseSeconds;
            currentState.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        internal void Destroy()
        {
            Clear();
        }

        /// <summary>
        /// 清理有限状态机。
        /// </summary>
        public void Clear()
        {
            if (currentState != null)
            {
                currentState.OnLeave();
            }

            foreach (KeyValuePair<Type, FsmState> state in states)
            {
                state.Value.OnDestroy();
            }

            name = null;
            states.Clear();
            currentState = null;
            currentStateTime = 0f;
            isDestroyed = true;
        }

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型。</typeparam>
        internal void ChangeState<TState>() where TState : FsmState
        {
            ChangeState(typeof(TState));
        }

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <param name="stateType">要切换到的有限状态机状态类型。</param>
        internal void ChangeState(Type stateType)
        {
            if (currentState == null)
            {
                throw new Exception("Current state is invalid.");
            }

            FsmState state = GetState(stateType);
            if (state == null)
            {
                throw new Exception(Utility.Text.Format("FSM can not change state to '{1}' which is not exist.",  stateType.FullName));
            }

            currentState.OnLeave();
            currentStateTime = 0f;
            currentState = state;
            currentState.OnEnter();
        }
    }
}
