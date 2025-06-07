using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public class ViewData
    {
        public virtual bool AsyncLoad
        {
            get; set;
        }
    }
    public abstract class ViewBase : MonoBehaviour
    {
        internal UIViewWrapper Wrapper;

        public virtual void OnShow(bool isInitShow, ViewData data)
        {
        }
        public virtual void OnClose()
        {
        }

        public void Close()
        {
            Wrapper?.CloseSelf();
            Wrapper = null;
        }

    }
}
