using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Test.Runtime.EventTest
{
    public class HpChangedEvent : EventBase, IReference
    {
        public int hp;
        public GameObject go;
        public void Reset()
        {

        }
    }

    public class CustomEvent : EventBase, IReference
    {
        public void Reset()
        {

        }
    }

    public class EventTest : MonoBehaviour
    {
        public bool isSync = true;

        public void SubscribeHpChangedHandler1()
        {
            FW.EventMgr.Subscribe<HpChangedEvent>(HpChangedHandler);
        }

        public void SubscribeHpChangedHandler2()
        {
            FW.EventMgr.Subscribe<HpChangedEvent>(HpChangedHandler2);
        }

        public void SubscribeCustomEventHandler()
        {
            FW.EventMgr.Subscribe<CustomEvent>(CustomEventHandler);
        }

        public void UnSubscribeHpChangedHandler1()
        {
            FW.EventMgr.Unsubscribe<HpChangedEvent>(HpChangedHandler);
        }

        public void UnSubscribeHpChangedHandler2()
        {
            FW.EventMgr.Unsubscribe<HpChangedEvent>(HpChangedHandler2);
        }

        public void UnSubscribeCustomEventHandler()
        {
            FW.EventMgr.Unsubscribe<CustomEvent>(CustomEventHandler);
        }

        public void BroadCastEvent()
        {
            var e = ReferencePool.Acquire<HpChangedEvent>();
            e.hp = UnityEngine.Random.Range(1, 100);
            if (isSync)
            {
                FW.EventMgr.Broadcast(e);

                FW.EventMgr.Broadcast<CustomEvent>();
            }
            else
            {
                FW.EventMgr.BroadcastAsync(e);

                FW.EventMgr.BroadcastAsync<CustomEvent>();
            }
        }

        void HpChangedHandler(HpChangedEvent ev)
        {
            ev.go.name = "name";//特意报错，看是否会阻断后续回调的执行
            //Debug.Log($"HpChangedHandler: {ev.hp}");
        }

        void HpChangedHandler2(HpChangedEvent ev)
        {
            Debug.Log($"HpChangedHandler2: {ev.hp}");
        }

        void CustomEventHandler(CustomEvent ev)
        {
            Debug.Log("CustomEventHandler");
        }
    }
}
