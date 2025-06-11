using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreserveStrip
{
    /// <summary>
    /// 先在link.xml中加入要保留的程序集或类
    /// 如果你主工程中没有引用过某个程序集的任何代码，即使在link.xml中保留，该程序集也会被完全裁剪。因此对于每个要保留的AOT程序集， 请确保在主工程代码中显式引用过它的某个类或函数
    /// </summary>
    public class PreserveStripTypeRef
    {
        public static void Ref()
        {
            //_ = typeof(UnityEngine.UI.Button);
        }
    }
}
