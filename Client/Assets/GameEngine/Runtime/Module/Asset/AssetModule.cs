using GameEngine.Runtime.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.Runtime.Module.Asset
{
    public class AssetModule : ModuleBase
    {


        public override void OnInit(InitModuleParamBase param = null)
        {
            var assetLoadMode = AssetLoadMode.Editor;

            switch (assetLoadMode)
            {
                case AssetLoadMode.Editor:
                    {
                        break;
                    }
                case AssetLoadMode.Single:
                    {
                        break;
                    }
                case AssetLoadMode.HotUpdate:
                    {
                        break;
                    }
                default:
                    break;
            }
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            throw new NotImplementedException();
        }
        public override void OnFixUpdate(float elapseSeconds, float realElapseSeconds)
        {
            throw new NotImplementedException();
        }
        public override void OnLateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            throw new NotImplementedException();
        }

        public override void Release()
        {
            throw new NotImplementedException();
        }
    }
}
