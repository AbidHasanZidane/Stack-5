using UnityEngine;

public class AnimateStacks : MonoBehaviour
{
    public float fallDelay = 0.1f;
    public float fallDuration = 0.3f;
    public float fallDistance = 200f;

    // Animate stacking down (like falling)
    public void AnimateStackIn(RectTransform[] elements)
    {
        for (int i = 0; i < elements.Length; i++)
        {
            RectTransform el = elements[i];
            Vector2 originalPos = el.anchoredPosition;

            el.anchoredPosition = originalPos + Vector2.up * fallDistance;

            LeanTween.moveLocalY(el.gameObject, originalPos.y, fallDuration)
                     .setDelay(i * fallDelay)
                     .setEase(LeanTweenType.easeOutBounce);
        }
    }

    // Animate stacking out (like falling out of stack or sliding away)
    public void AnimateStackOut(RectTransform[] elements, System.Action onComplete = null)
    {
        for (int i = 0; i < elements.Length; i++)
        {
            RectTransform el = elements[i];

            LeanTween.moveLocalY(el.gameObject, el.anchoredPosition.y - fallDistance, fallDuration)
                     .setDelay(i * fallDelay)
                     .setEase(LeanTweenType.easeInBack);
        }

        if (onComplete != null)
        {
            // Wait until the last animation finishes
            float totalTime = fallDelay * elements.Length + fallDuration;
            Invoke(nameof(InvokeCallback), totalTime);

            void InvokeCallback()
            {
                onComplete?.Invoke();
            }
        }
    }
}
