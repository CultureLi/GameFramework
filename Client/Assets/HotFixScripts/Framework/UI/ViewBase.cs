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
 
        public IUIGroup Group;

        public virtual void OnShow(bool isInitShow, ViewData data)
        {
        }
        public virtual void OnClose()
        {
        }

    }
}
