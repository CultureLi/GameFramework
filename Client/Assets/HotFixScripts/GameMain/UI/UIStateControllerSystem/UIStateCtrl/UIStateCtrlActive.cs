using UnityEngine;

namespace GameMain.UI
{
    [SerializeField]
    public sealed class UIStateCtrlActive : UIStateCtrlBase<bool>
    {
        protected override bool TargetValue
        {
            set
            {
                gameObject.SetActive(value);
            }

            get
            {
                return gameObject.activeSelf;
            }
        }
    }
}
