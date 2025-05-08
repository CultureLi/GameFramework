using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public interface IEventMgr
    {
        public void Subscribe<T>(Action<T> listener) where T : EventBase;

        public void Unsubscribe<T>(Action<T> listener) where T : EventBase;

        public void Broadcast<T>(T evt = null) where T : EventBase;
        public void BroadcastAsync<T>(T evt = null) where T : EventBase;

    }
}
