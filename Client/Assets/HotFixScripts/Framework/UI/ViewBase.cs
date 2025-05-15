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
    
    }
    public abstract class ViewBase : MonoBehaviour
    {
        public string UIName;
        public IUIGroup Group;

        public virtual void OnOpen(ViewData data)
        {
        }
        public virtual void OnClose()
        {
        }

    }
}
