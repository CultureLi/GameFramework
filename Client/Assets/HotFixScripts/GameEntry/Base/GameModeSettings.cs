namespace GameEntry
{
    public enum GamePlayMode
    {
        SinglePlayer = 0,
        Online = 1,
    }

    /// <summary>
    /// 客户端运行模式。默认单机模式；登录前由 UIGameEntryMsgBox 的下拉框选择。
    /// </summary>
    public static class GameModeSettings
    {
        public static GamePlayMode Mode { get; set; } = GamePlayMode.SinglePlayer;

        public static bool IsSinglePlayer => Mode == GamePlayMode.SinglePlayer;
    }
}
