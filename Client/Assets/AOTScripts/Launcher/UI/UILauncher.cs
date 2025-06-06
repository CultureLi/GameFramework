using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Launcher
{
    public class UILauncher : MonoBehaviour
    {
        public Image bg;
        public Slider slider;
        public TextMeshProUGUI textProgress;
        public TextMeshProUGUI textVersion;

        private static UILauncher instance;
        private static object locker = new();

        public static UILauncher Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        instance = FindObjectOfType<UILauncher>();
                        if (instance == null)
                        {
                            GameObject go = new GameObject(typeof(UILauncher).Name + " (Singleton)");
                            instance = go.AddComponent<UILauncher>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as UILauncher;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            slider.gameObject.SetActive(false);
        }

        public void SetBg(string sprite)
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(sprite);
            handle.WaitForCompletion();
            bg.overrideSprite = handle.Result;
        }

        public void SetSliderVisible(bool visible)
        {
            slider.gameObject.SetActive(visible);
        }
        public void RefreshProgress(float progress, ulong totalSize)
        {
            slider.value = progress;
            textProgress.gameObject.SetActive(totalSize > 0);
            if (totalSize > 0)
            {
                textProgress.text = $"{totalSize * progress}/{totalSize}";
            }
        }
    }
}
