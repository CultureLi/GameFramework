using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Assets.TestScripts.Runtime.EventTest
{
    public class HpChangedEvent : EventBase, IReference
    {
        public int hp;

        public void Clear()
        {
            throw new NotImplementedException();
        }
    }

    public class CustomEvent : EventBase, IReference
    {
        public void Clear()
        {
            throw new NotImplementedException();
        }
    }

    public class EventTest : MonoBehaviour
    {
        IEventMgr eventMgr;

        private void Awake()
        {
            eventMgr = FrameworkEntry.GetModule<IEventMgr>();
        }

        public void SubscribeHpChangedHandler1()
        {
            eventMgr.Subscribe<HpChangedEvent>(HpChangedHandler);
        }

        public void SubscribeHpChangedHandler2()
        {
            eventMgr.Subscribe<HpChangedEvent>(HpChangedHandler2);
        }

        public void SubscribeCustomEventHandler()
        {
            eventMgr.Subscribe<CustomEvent>(CustomEventHandler);
        }

        public void UnSubscribeHpChangedHandler1()
        {
            eventMgr.Unsubscribe<HpChangedEvent>(HpChangedHandler);
        }

        public void UnSubscribeHpChangedHandler2()
        {
            eventMgr.Unsubscribe<HpChangedEvent>(HpChangedHandler2);
        }

        public void UnSubscribeCustomEventHandler()
        {
            eventMgr.Unsubscribe<CustomEvent>(CustomEventHandler);
        }


        float timeInterval = 2;
        public void Update()
        {
            timeInterval -= Time.deltaTime;
            if (timeInterval < 0)
            {
                var e = ReferencePool.Acquire<HpChangedEvent>();
                e.hp = e.hp + 1;
                eventMgr.Fire(e);

                eventMgr.Fire<CustomEvent>();
            }
                
        }



        void HpChangedHandler(HpChangedEvent ev)
        {
            Debug.Log($"HpChangedHandler: {ev.hp}");
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
