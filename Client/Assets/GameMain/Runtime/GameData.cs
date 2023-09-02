using GameEngine.Runtime.Logic.Data;
using GameMain.Runtime.Data.Chat;
using GameMain.Runtime.Data.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.Runtime
{
    internal class GameData
    {

        public static ChatData ChatData = DataMgr.Instance.CreateModel<ChatData>();
        public static PlayerAssetData PlayerAssetData = DataMgr.Instance.CreateModel<PlayerAssetData>();
    }
}
