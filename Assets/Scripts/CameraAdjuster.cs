using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraAdjuster : MonoBehaviour
{
    public GameManager gameManager; // The parent object of your level
    public float padding = 1f; // Optional padding to add around the level

    void Start()
    {
        AdjustCamera();
    }

    void AdjustCamera()
    {
        // Assume the level width is determined by the renderer bounds of all children
        float levelWidth = gameManager.Width ;
        Camera.main.orthographicSize = CalculateOrthographicSize(levelWidth);
    }

    float CalculateLevelWidth(GameObject levelObject)
    {
        Renderer[] renderers = levelObject.GetComponentsInChildren<Renderer>();
        float minX = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;

        foreach (Renderer renderer in renderers)
        {
            minX = Mathf.Min(minX, renderer.bounds.min.x);
            maxX = Mathf.Max(maxX, renderer.bounds.max.x);
        }

        return maxX - minX + padding;
    }

    float CalculateOrthographicSize(float levelWidth)
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        // Calculate the orthographic size based on the level width and the screen's aspect ratio
        float orthographicSize = levelWidth / screenAspect / 2; // Divide by 2 because orthographic size is half the vertical height
        return orthographicSize;
    }

    void CenterCameraOnLevel(GameObject levelObject)
    {
        Renderer[] renderers = levelObject.GetComponentsInChildren<Renderer>();
        float sumX = 0f;

        foreach (Renderer renderer in renderers)
        {
            sumX += renderer.bounds.center.x;
        }

        float centerX = sumX / renderers.Length;
        Camera.main.transform.position = new Vector3(centerX, Camera.main.transform.position.y, Camera.main.transform.position.z);
    }
}
