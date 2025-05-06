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

        public void Fire<T>(T evt = null) where T : EventBase;
        public void FireAsync<T>(T evt = null) where T : EventBase;

    }
}
