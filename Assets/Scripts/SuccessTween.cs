using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuccessTween : MonoBehaviour
{
    [SerializeField] GameObject BigStar, LittleStar, PerfectText, bg, canvas;
    public void LevelSucceed()
    {
        LeanTween.moveLocal(PerfectText, new Vector3(1f, 1100f, 1f), 2f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(PerfectText, new Vector3(100f, 100f, 100f), 2f).setDelay(.5f).setEase(LeanTweenType.easeOutElastic);
        LeanTween.moveLocal(PerfectText, new Vector3(1f, -100f, 1f), 2f).setDelay(1.7f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(PerfectText, new Vector3(1f, 1f, 1f), 2f).setDelay(1.7f).setEase(LeanTweenType.easeInOutCubic);

        LeanTween.moveLocal(BigStar, new Vector3(1f, 800f, 1f), 2f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.moveLocal(BigStar, new Vector3(1f, -100f, 1f), 2f).setDelay(1.7f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(BigStar, new Vector3(1f, 1f, 1f), 2f).setDelay(1.7f).setEase(LeanTweenType.easeInOutCubic);

        LeanTween.moveLocal(bg, new Vector3(1f, 700f, 1f), 0f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.moveLocal(bg, new Vector3(1f, -100f, 1f), 0f).setDelay(3f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(bg, new Vector3(1f, 1f, 1f), 0f).setDelay(1.7f).setEase(LeanTweenType.easeInOutCubic);

        Destroy(canvas);
    }
}
