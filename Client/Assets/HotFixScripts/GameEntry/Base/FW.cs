using Framework;
using UnityEngine;

namespace GameEntry
{
    /// <summary>
    /// 游戏入口单例：负责场景生命周期（协程宿主、Update 驱动、退出/重启），
    /// 模块实例统一由 <see cref="FrameworkMgr"/> 缓存，这里只做转发，
    /// Framework 内部与业务代码取模块的方式保持一致（都走 FrameworkMgr）。
    /// </summary>
    public class FW : SingletonMono<FW>
    {
        public static IEventMgr EventMgr => FrameworkMgr.EventMgr;
        public static IFsmMgr FsmMgr => FrameworkMgr.FsmMgr;
        public static IResourceMgr ResMgr => FrameworkMgr.ResMgr;
        public static ITimerMgr TimerMgr => FrameworkMgr.TimerMgr;
        public static INetworkMgr NetMgr => FrameworkMgr.NetMgr;
        public static IObjectPoolMgr ObjectPoolMgr => FrameworkMgr.ObjectPoolMgr;
        public static IUIManager UIMgr => FrameworkMgr.UIMgr;
        public static IConfigMgr CfgMgr => FrameworkMgr.CfgMgr;
        public static ILocalizationMgr LocalizationMgr => FrameworkMgr.LocalizationMgr;
        public static ISpriteMgr SpriteMgr => FrameworkMgr.SpriteMgr;

        public static IGameLogicMgr GameLogicMgr => FrameworkMgr.GameLogicMgr;

        public static MonoBehaviour CoroutineRunner
        {
            get; private set;
        }

        public void Initialize()
        {
            base.Awake();
            CoroutineRunner = this;

            // 保持与改造前一致的启动期初始化：所有模块在进入游戏前创建完毕
            // （FrameworkMgr 的属性本身是懒加载，这里显式触碰以还原原 FW.Initialize 的时序）。
            _ = EventMgr;
            _ = FsmMgr;
            _ = ResMgr;
            _ = TimerMgr;
            _ = NetMgr;
            _ = ObjectPoolMgr;
            _ = UIMgr;
            _ = CfgMgr;
            _ = LocalizationMgr;
            _ = SpriteMgr;

            // 显式初始化：依赖资源模块的 UI/Sprite 模块在这里完成注入，
            // 顺序一目了然，也不要挪进模块构造函数（会触发 FrameworkMgr 的重入创建）。
            UIMgr.Init();
            SpriteMgr.Init();

            _ = GameLogicMgr;

            Application.quitting -= OnApplicationShutdown;
            Application.quitting += OnApplicationShutdown;
        }

        void Update()
        {
            FrameworkMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        void OnApplicationShutdown()
        {
            CoroutineRunner.StopAllCoroutines();
            FrameworkMgr.Shutdown(EShutdownType.Shutdown);
        }

        public void ReStart()
        {
            CoroutineRunner.StopAllCoroutines();
            FrameworkMgr.Shutdown(EShutdownType.Restart);
            UIMgr.CloseAll();

            System.GC.Collect();


            GameEntryMgr.Entry().Forget();
        }
    }
}
