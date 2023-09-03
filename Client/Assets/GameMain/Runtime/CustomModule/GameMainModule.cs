using GameEngine.Runtime.Module;
using GameEngine.Runtime.Base;

namespace GameMain.Runtime.CustomModule
{
    public partial class GameMainModule:ModuleBase
    {
        public override void OnInit(InitModuleParamBase param)
        {
            Log.Info("GameMainModule Init ... ");
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            Log.Info("GameMainModule Update ... ");
        }

        public override void OnFixUpdate(float elapseSeconds, float realElapseSeconds)
        {
            Log.Info("GameMainModule OnFixUpdate ... ");
        }


        public override void OnLateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            Log.Info("GameMainModule OnLateUpdate ... ");
        }


        public override void Release()
        {
        }

       
    }
}
