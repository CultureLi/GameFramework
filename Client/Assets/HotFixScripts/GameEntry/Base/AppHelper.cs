using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEntry
{
    public class AppHelper
    {
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
        }

        static void RestartApp()
        {
#if UNITY_ANDROID
            RestartAppAndroid();
#elif UNITY_STANDALONE_WIN
        QuitGame();
#else
        QuitGame();
#endif
        }

#if UNITY_ANDROID
        static void RestartAppAndroid()
        {
            try
            {
                const int kIntent_FLAG_ACTIVITY_CLEAR_TASK = 0x00008000;
                const int kIntent_FLAG_ACTIVITY_NEW_TASK = 0x10000000;

                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                using var intent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", Application.identifier);

                Debug.Log($"RestartOrKillApp {currentActivity} {pm} {intent}");

                using var intent2 = intent.Call<AndroidJavaObject>("setFlags", kIntent_FLAG_ACTIVITY_NEW_TASK | kIntent_FLAG_ACTIVITY_CLEAR_TASK);
                currentActivity.Call("startActivity", intent);

                Debug.Log($"RestartOrKillApp killprocess");
                using var process = new AndroidJavaClass("android.os.Process");
                int pid = process.CallStatic<int>("myPid");
                process.CallStatic("killProcess", pid); // 尝试使用 Application.Quit() 不能触发重启
            }
            catch (Exception e)
            {
                Debug.LogError($"RestartAppAndroid failed: {e}");
                QuitGame();
            }
        }
#endif
    }
}
