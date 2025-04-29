using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 有限状态机管理器。
    /// </summary>
    internal sealed class FsmMgr : IFrameworkModule, IFsmMgr
    {
        private readonly Dictionary<string, Fsm> fsms;
        private readonly List<Fsm> tempFsms;

        /// <summary>
        /// 初始化有限状态机管理器的新实例。
        /// </summary>
        public FsmMgr()
        {
            fsms = new Dictionary<string, Fsm>();
            tempFsms = new List<Fsm>();
        }

        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        internal int Priority
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 获取有限状态机数量。
        /// </summary>
        public int Count
        {
            get
            {
                return fsms.Count;
            }
        }

        /// <summary>
        /// 有限状态机管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            tempFsms.Clear();
            if (fsms.Count <= 0)
            {
                return;
            }

            foreach (KeyValuePair<string, Fsm> fsm in fsms)
            {
                tempFsms.Add(fsm.Value);
            }

            foreach (Fsm fsm in tempFsms)
            {
                if (fsm.IsDestroyed)
                {
                    continue;
                }

                fsm.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理有限状态机管理器。
        /// </summary>
        public void Shutdown()
        {
            foreach (KeyValuePair<string, Fsm> fsm in fsms)
            {
                fsm.Value.Destroy();
            }

            fsms.Clear();
            tempFsms.Clear();
        }

        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        public bool HasFsm(string name)
        {
            return InternalHasFsm(name);
        }

        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <returns>要获取的有限状态机。</returns>
        public Fsm GetFsm(string name)
        {
            return InternalGetFsm(name);
        }

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <returns>所有有限状态机。</returns>
        public Fsm[] GetAllFsms()
        {
            int index = 0;
            Fsm[] results = new Fsm[fsms.Count];
            foreach (KeyValuePair<string, Fsm> fsm in fsms)
            {
                results[index++] = fsm.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <param name="results">所有有限状态机。</param>
        public void GetAllFsms(List<Fsm> results)
        {
            if (results == null)
            {
                throw new Exception("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, Fsm> fsm in fsms)
            {
                results.Add(fsm.Value);
            }
        }

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        public Fsm CreateFsm(string name, params FsmState[] states)
        {
            if (HasFsm(name))
            {
                throw new Exception(Utility.Text.Format("Already exist FSM '{0}'.", name));
            }

            Fsm fsm = Fsm.Create(name, states);
            fsms.Add(name, fsm);
            return fsm;
        }

        public Fsm CreateFsm(string name, List<FsmState> states)
        {
            if (HasFsm(name))
            {
                throw new Exception(Utility.Text.Format("Already exist FSM '{0}'.", name));
            }

            Fsm fsm = Fsm.Create(name, states);
            fsms.Add(name, fsm);
            return fsm;
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">要销毁的有限状态机名称。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(string name)
        {
            return InternalDestroyFsm(name);
        }

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        public bool DestroyFsm(Fsm fsm)
        {
            if (fsm == null)
            {
                throw new Exception("FSM is invalid.");
            }

            return InternalDestroyFsm(fsm.Name);
        }

        private bool InternalHasFsm(string name)
        {
            return fsms.ContainsKey(name);
        }

        private Fsm InternalGetFsm(string name)
        {
            Fsm fsm = null;
            if (fsms.TryGetValue(name, out fsm))
            {
                return fsm;
            }

            return null;
        }

        private bool InternalDestroyFsm(string name)
        {
            Fsm fsm = null;
            if (fsms.TryGetValue(name, out fsm))
            {
                fsm.Destroy();
                return fsms.Remove(name);
            }

            return false;
        }
    }
}
