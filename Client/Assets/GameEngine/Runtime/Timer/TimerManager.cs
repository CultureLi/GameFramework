using System;
using System.Collections.Generic;

namespace GameEngine.Runtime.Timer
{
    public class TimerManager
    {
        private long m_TimeElapsed;
        private int m_TimerUid;
        private Dictionary<int, TimerInfo> m_TimerDict = new();
        private SortedDictionary<long, List<int>> m_SortedIdDict = new();
        private HashSet<int> m_RemoveTimerIdSet = new();


        private bool m_IsPause = false;
        public TimerManager()
        {
            m_TimerUid = 0;
        }

        private int GenTimerUid()
        {
            return ++m_TimerUid;
        }

        /// <summary>
        /// 创建timer
        /// </summary>
        /// <param name="interval">触发间隔 单位ms</param>
        /// <param name="completedCb">完成回调</param>
        /// <param name="repeatCnt">重复次数</param>       
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        public int CreateTimer(int interval, Action<object> completedCb, int repeatCnt = 1, object userData = null)
        {
            var timer = TimerInfo.Acquire();
            var id = GenTimerUid();
            timer.Init(id, interval, completedCb, repeatCnt, userData);

            InternalAddTimer(timer);

            return id;
        }

        /// <summary>
        /// 删除timer
        /// </summary>
        /// <param name="timerId"></param>
        public void DeleteTimer(int timerId)
        {
            m_RemoveTimerIdSet.Add(timerId);
        }

        private void InternalAddTimer(TimerInfo timer)
        {
            m_TimerDict[timer.Id] = timer;
            var completeTime = m_TimeElapsed + timer.Interval;
            if (!m_SortedIdDict.TryGetValue(completeTime, out var lst))
            {
                lst = new List<int>();
                m_SortedIdDict[completeTime] = lst;
            }
            lst.Add(timer.Id);
        }

        private void InternalDeleteTimer()
        {
            foreach (var timerId in m_RemoveTimerIdSet)
            {
                if(m_TimerDict.TryGetValue(timerId,out var timer))
                {
                    timer.Release();
                    m_TimerDict.Remove(timerId);
                }				
            }
            m_RemoveTimerIdSet.Clear();
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            m_IsPause = true;
        }

        /// <summary>
        /// 唤醒
        /// </summary>
        public void Resume()
        {
            m_IsPause = false;
        }

        
        private List<long> tempTimeOutKeyList = new();
        private List<int> tempTimeOutTimerList = new();
        /// <summary>
        /// 更新
        /// </summary>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            InternalDeleteTimer();

            if (m_IsPause)
                return;

            m_TimeElapsed += (long)(elapseSeconds * 1000);

            tempTimeOutKeyList.Clear();
            tempTimeOutTimerList.Clear();
            foreach (var kv in m_SortedIdDict)
            {
                if (kv.Key > m_TimeElapsed)
                {
                    break;
                }

                tempTimeOutKeyList.Add(kv.Key);
                if (m_SortedIdDict.TryGetValue(kv.Key, out var lst))
                    tempTimeOutTimerList.AddRange(lst);
            }

            foreach (var key in tempTimeOutKeyList)
            {
                m_SortedIdDict.Remove(key);
            }

            foreach (var timerId in tempTimeOutTimerList)
            {
                if(m_TimerDict.TryGetValue(timerId, out var timer))
                {
                    var reCreate = (timer.RepeatCnt != 1);

                    timer.Trigger();

                    if (reCreate)
                        InternalAddTimer(timer);
                    else
                        DeleteTimer(timerId);
                }
            }
        }

        public void Release()
        {
            m_TimerDict.Clear();
            m_SortedIdDict.Clear();
            tempTimeOutKeyList.Clear();
            tempTimeOutTimerList.Clear();
        }

    }
}
