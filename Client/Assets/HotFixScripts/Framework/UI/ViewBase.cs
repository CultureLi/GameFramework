using UnityEngine;

namespace Framework
{
    public class ViewData
    {
        /// <summary>
        /// 界面默认是否异步加载。
        /// 同步加载会走 Addressables 的 WaitForCompletion，在首次打开界面时卡住主线程；
        /// 需要"打开后立刻使用"的特殊界面，可以在自己的 ViewData 里把 AsyncLoad 设成 false。
        /// </summary>
        public static bool DefaultAsyncLoad = true;

        // 加载方式，默认异步
        public virtual bool AsyncLoad
        {
            get; set;
        } = DefaultAsyncLoad;
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
