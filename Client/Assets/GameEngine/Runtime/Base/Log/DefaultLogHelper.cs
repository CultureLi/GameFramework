using GameEngine.Runtime.Utilitys;
using System;
using UnityEngine;

namespace GameEngine.Runtime.Base
{
    /// <summary>
    /// 默认游戏框架日志辅助器。
    /// </summary>
    public class DefaultLogHelper : Log.ILogHelper
    {
        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(LogLevel level, object message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log(Utility.Text.Format("<color=#888888>{0}</color>", message.ToString()));
                    break;

                case LogLevel.Info:
                    Debug.Log(message.ToString());
                    break;

                case LogLevel.Warning:
                    Debug.LogWarning(message.ToString());
                    break;

                case LogLevel.Error:
                    Debug.LogError(message.ToString());
                    break;

                default:
                    throw new Exception(message.ToString());
            }
        }
    }
}
