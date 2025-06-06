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
        public IUIManager UIManager
        {
            get;set;
        }

        public virtual void OnShow(bool isInitShow, ViewData data)
        {
        }
        public virtual void OnClose()
        {
        }

        protected void Close()
        {
            //UIManager.CloseUI();
        }

    }
}
