using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Test.Runtime.TimerTest
{
    internal class TimerTest : MonoBehaviour
    {

        ITimerMgr timerMgr;

        int timerId1;
        int timerId2;
        int timerId3;
        private void Awake()
        {
            timerMgr = Framework.FrameworkMgr.GetModule<ITimerMgr>();
            timerId1 = timerMgr.AddTimer(2, (context) =>
            {
                Debug.Log($"Timer1 无限: {context.triggeredCnt}");
            },-1);

            timerId2 = timerMgr.AddTimer(4, (context) => { Debug.Log("Timer2 一次"); });
            timerId3 = timerMgr.AddTimer(4, (context) => { Debug.Log("Timer3 N秒"); }, 10);
        }

        private void Update()
        {
            FrameworkMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
}
