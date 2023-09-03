using GameEngine.Runtime.Logic.Data;
using GameMain.Runtime.Data.Chat;
using GameMain.Runtime.Data.Player;

namespace GameMain.Runtime
{
    internal class GameData
    {

        public static ChatData ChatData = DataMgr.Instance.CreateModel<ChatData>();
        public static PlayerAssetData PlayerAssetData = DataMgr.Instance.CreateModel<PlayerAssetData>();
    }
}
