using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class MenuManager : MonoBehaviour
{
    public Button levelButton;
    public TMPro.TMP_Text levelButtonText;

    private void Start()
    {
        // debugging
        //PlayerPrefs.SetInt("CurrentLevel", 1);

        // Check the current level from PlayerPrefs
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        // Assuming 10 is the total number of levels
        if (currentLevel > 10)
        {
            levelButtonText.text = "Finished";
        }
        else
        {
            levelButtonText.text = "Level " + currentLevel.ToString();
            levelButton.onClick.AddListener(() => { LoadLevelScene(currentLevel); });
        }

        // Add a listener to the button
    }

    void LoadLevelScene(int level)
    {
        SceneManager.LoadScene("LevelScene");
    }
}
