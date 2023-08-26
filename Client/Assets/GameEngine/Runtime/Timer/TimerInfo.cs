using GameEngine.Runtime.Pool.ReferencePool;
using System;
namespace GameEngine.Runtime.Timer
{
    public class TimerInfo:IReference
    {
        /// <summary>
        /// timer唯一标识
        /// </summary>
        private int m_Id;
        public int Id { get { return m_Id; } }
        /// <summary>
        /// 触发间隔（单位ms）
        /// </summary>
        private int m_Interval;
        public int Interval { get { return m_Interval; } }
        /// <summary>
        /// 重复次数 <= 0 表示无限次
        /// </summary>
        private int m_RepeatCnt;
        public int RepeatCnt { get { return m_RepeatCnt; } }

        /// <summary>
        /// 用户数据
        /// </summary>
        private object m_UserData;
        /// <summary>
        /// 完成时回调
        /// </summary>
        private Action<object> m_CompletedCb;

        public void Trigger()
        {
            m_RepeatCnt--;
            m_CompletedCb?.Invoke(m_UserData);
        }


        public void Init(int id,int interval, Action<object> completedCb, int repeatCnt, object userData)
        {
            this.m_Id = id;
            this.m_Interval = interval;
            this.m_RepeatCnt = repeatCnt;
            this.m_UserData = userData;
            this.m_CompletedCb = completedCb;
        }

        public void Clear()
        {
            
        }

        public static TimerInfo Acquire()
        {
            return ReferencePool.Acquire(typeof(TimerInfo)) as TimerInfo;
        }


        public void Release()
        {
            ReferencePool.Release(this);
        }

    }
}
