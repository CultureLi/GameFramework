using UnityEngine;
using UnityEngine.UI;

namespace GameMain.UI
{
    [SerializeField]
    public sealed class UIStateCtrlColor : UIStateCtrlBase<Color>
    {
        protected override Color TargetValue
        {
            set
            {
                if (GetComponent<Graphic>() is Graphic graphic)
                {
                    graphic.color = value;
                }
                else if (GetComponent<SpriteRenderer>() is SpriteRenderer spriteRenderer)
                {
                    spriteRenderer.color = value;
                }
            }

            get
            {
                var c = Color.white;
                if (GetComponent<Graphic>() is Graphic graphic)
                {
                    c = graphic.color;
                }
                else if (GetComponent<SpriteRenderer>() is SpriteRenderer spriteRenderer)
                {
                    c = spriteRenderer.color;
                }
                return c;
            }
        }
    }
}
