using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FailedTween : MonoBehaviour
{
    [SerializeField] GameObject canvas, inner_canvas;
    public void LevelFailed()
    {
        LeanTween.moveLocal(inner_canvas, new Vector3(14.5f, 0f, 1f), 2f).setEase(LeanTweenType.easeOutQuint);

        Destroy(canvas);
    }
}
