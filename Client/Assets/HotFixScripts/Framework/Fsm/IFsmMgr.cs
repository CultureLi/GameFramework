﻿using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 有限状态机管理器。
    /// </summary>
    public interface IFsmMgr
    {
        /// <summary>
        /// 获取有限状态机数量。
        /// </summary>
        int Count
        {
            get;
        }


        /// <summary>
        /// 检查是否存在有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <returns>是否存在有限状态机。</returns>
        bool HasFsm(string name);


        /// <summary>
        /// 获取有限状态机。
        /// </summary>
        /// <param name="ownerType">有限状态机持有者类型。</param>
        /// <param name="name">有限状态机名称。</param>
        /// <returns>要获取的有限状态机。</returns>
        Fsm GetFsm(string name);

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <returns>所有有限状态机。</returns>
        Fsm[] GetAllFsms();

        /// <summary>
        /// 获取所有有限状态机。
        /// </summary>
        /// <param name="results">所有有限状态机。</param>
        void GetAllFsms(List<Fsm> results);

        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        Fsm CreateFsm(string name, params FsmState[] states);


        /// <summary>
        /// 创建有限状态机。
        /// </summary>
        /// <typeparam name="T">有限状态机持有者类型。</typeparam>
        /// <param name="name">有限状态机名称。</param>
        /// <param name="owner">有限状态机持有者。</param>
        /// <param name="states">有限状态机状态集合。</param>
        /// <returns>要创建的有限状态机。</returns>
        Fsm CreateFsm(string name, List<FsmState> states);

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        bool DestroyFsm(string name);

        /// <summary>
        /// 销毁有限状态机。
        /// </summary>
        /// <param name="fsm">要销毁的有限状态机。</param>
        /// <returns>是否销毁有限状态机成功。</returns>
        bool DestroyFsm(Fsm fsm);
    }
}
