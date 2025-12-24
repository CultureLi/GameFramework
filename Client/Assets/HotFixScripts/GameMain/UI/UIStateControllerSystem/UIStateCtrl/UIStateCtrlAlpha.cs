using UnityEngine;
using UnityEngine.UI;

namespace GameMain.UI
{
    [SerializeField]
    public sealed class UIStateCtrlAlpha : UIStateCtrlBase<float>
    {
        protected override float TargetValue
        {
            set
            {
                if (GetComponent<Graphic>() is Graphic graphic)
                {
                    graphic.color = graphic.color.WithAlpha(value);
                }
                else if (GetComponent<SpriteRenderer>() is SpriteRenderer spriteRenderer)
                {
                    spriteRenderer.color = spriteRenderer.color.WithAlpha(value);
                }
                else if (GetComponent<CanvasGroup>() is CanvasGroup canvasGroup)
                {
                    canvasGroup.alpha = value;
                }
            }

            get
            {
                var alpha = 1f;
                if (GetComponent<Graphic>() is Graphic graphic)
                {
                    alpha = graphic.color.a;
                }
                else if (GetComponent<SpriteRenderer>() is SpriteRenderer spriteRenderer)
                {
                    alpha = spriteRenderer.color.a;
                }
                else if (GetComponent<CanvasGroup>() is CanvasGroup canvasGroup)
                {
                    alpha = canvasGroup.alpha;
                }
                return alpha;
            }
        }
    }
}
