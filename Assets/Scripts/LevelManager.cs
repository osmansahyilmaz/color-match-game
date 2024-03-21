using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class LevelManager : MonoBehaviour
{
    // Method to load level data from a JSON file
    public LevelData LoadLevelData(int levelNumber)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "level_" + levelNumber.ToString("00") + ".json");
        if (File.Exists(path))
        {
            string jsonContents = File.ReadAllText(path);
            LevelData levelData = JsonUtility.FromJson<LevelData>(jsonContents);
            return levelData;
        }
        else
        {
            Debug.LogError("Level file not found: " + path);
            return null;
        }
    }

    // Example method to initialize a level
    public void InitializeLevel(int levelNumber)
    {
        LevelData levelData = LoadLevelData(levelNumber);
        if (levelData != null)
        {
            // Initialize the level grid, player moves, etc., using levelData
            Debug.Log("Level " + levelData.level_number + " loaded with " + levelData.grid.Length + " tiles.");
        }
    }

    // For testing, load level 1 on start
    void Start()
    {
        InitializeLevel(1);
    }
}

