using UnityEngine;

namespace Framework
{
    public class ViewData
    {
        // 加载方式，默认同步加载
        public virtual bool AsyncLoad
        {
            get; set;
        } = false;
    }
    public abstract class ViewBase : MonoBehaviour
    {
        internal UIViewWrapper Wrapper;

        /// <summary>
        /// 资源是否可以被释放，有些UI可能会频繁打开关闭，设置为false可以避免重复加载资源
        /// </summary>
        public virtual bool CanBeReleased
        {
            get; set;
        } = true;

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
