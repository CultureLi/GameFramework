namespace GameMain
{
    /// <summary>关卡信息里返回给某个玩家的状态。</summary>
    public static class GateStatus
    {
        public const int None = 0;
        public const int Pending = 1;
        public const int Approved = 2;
        public const int Rejected = 3;
        public const int Owner = 4;
        /// <summary>关卡还没有归属人，当前禁止所有人通行。</summary>
        public const int Unowned = 5;
    }

    /// <summary>关卡协议返回码。</summary>
    public static class GateResultCode
    {
        public const int Success = 0;
        public const int NotFound = 1;
        public const int NotGate = 2;
        public const int NotOwner = 3;
        public const int InvalidState = 4;
        public const int NotLoggedIn = 5;
        public const int InvalidParam = 6;
    }
}
