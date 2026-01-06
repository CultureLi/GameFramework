using Sirenix.OdinInspector;
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
        // 异步加载
        public virtual bool AsyncLoad
        {
            get; set;
        } = false;
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

        public virtual void OnHide()
        {
        }

        public void Close()
        {
            Wrapper?.CloseSelf();
            Wrapper = null;
        }

        public virtual void SecondUpdate()
        {
        
        }

    }
}
