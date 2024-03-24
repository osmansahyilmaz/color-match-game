using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.IO;
using UnityEngine.ParticleSystemJobs;

public class GameManager : MonoBehaviour
{
    public int Width, Height, PaddingTop, remainingMoves, BoxGoalInt, StoneGoalInt, VaseGoalInt;
    public string[] data_grid;
    public Button levelButton, menuButton;
    public TMPro.TMP_Text remainingMovesText, BoxGoalText, StoneGoalText, VaseGoalText;
    [SerializeField] private GameObject[] NormalCubePrefabs;
    [SerializeField] private GameObject[] ObstacleCubePrefabs;
    [SerializeField] private GameObject[] BreakCubePrefabs;
    [SerializeField] private Transform SetBg, SetItem;
    [SerializeField] private GameObject GridCanvas, GridBg, BoxGoal, StoneGoal, VaseGoal, TickSprite;
    public static Dictionary<Tuple<int, int>, PickUp> Item = new Dictionary<Tuple<int, int>, PickUp>();
    private static List<GameObject> DeleteObject = new List<GameObject>();
    public static Dictionary<Tuple<int, int>, List<GameObject>> Square = new Dictionary<Tuple<int, int>, List<GameObject>>();
    public List<GameObject> Ticks = new List<GameObject>();
    private void Awake()
    {
        LoadPlayerLevel();
        Spawn_FillGrid();
        //CenterGrid(Width, Height);
        levelButton.onClick.AddListener(() => { LoadLevelScene(); });
        menuButton.onClick.AddListener(() => { LoadMainScene(); });
        // #1 Start Game
        StartCoroutine(Wait(0.1f, () =>
        {
            StartSquare();
        })); 
    }
    //private void CenterGrid(int currentWidth, int currentHeight)
    //{
    //    int optimalWidth = 9; // The design is optimized for a 9-width grid
    //    float gridElementWidth = 1.0f; // Assuming each grid element is 1 unit wide
    //    float centerOffset = (optimalWidth - currentWidth) * gridElementWidth / 2;

    //    // Assuming you have a reference to the parent GameObject of the grid elements
    //    Vector3 startPosition = GridCanvas.transform.position;
    //    startPosition.x += centerOffset; // Adjust the start position based on the calculated offset
    //    GridCanvas.transform.position = startPosition;
    //    GridBg.transform.localScale.Set(gridElementWidth * currentWidth, gridElementWidth * currentHeight, 1f);
    //}

    private void LoadPlayerLevel()
    {
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        if (currentLevel < 11)
        {
            Debug.Log("Loading player level: " + currentLevel);

            // Load the level data from JSON
            LevelData levelData = LoadLevelData(currentLevel);
            if (levelData != null)
            {
                InitializeLevel(levelData);
            }
            else
            {
                Debug.LogError("Failed to load level data for level: " + currentLevel);
            }
        }
    }

    private LevelData LoadLevelData(int levelNumber)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "level_" + levelNumber.ToString("00") + ".json");
        if (File.Exists(path))
        {
            string jsonContents = File.ReadAllText(path);
            //Debug.Log("JSON Contents: " + jsonContents);
            LevelData levelData = JsonUtility.FromJson<LevelData>(jsonContents);
            return levelData;
        }
        else
        {
            Debug.LogError("Level file not found: " + path);
            return null;
        }
    }

    private void InitializeLevel(LevelData levelData)
    {
        // Use levelData to initialize the game level
        Debug.Log($"Initializing Level: {levelData.level_number} with grid size {levelData.grid_width}x{levelData.grid_height} and {levelData.move_count} moves.");

        // Initialization
        BoxGoalInt = StoneGoalInt = VaseGoalInt = 0;
        Width = levelData.grid_width;
        Height = levelData.grid_height;
        remainingMoves = levelData.move_count;
        data_grid = levelData.grid;
        CalculateGoals(levelData.grid);
        InitGoalTexts();
        UpdateRemainingMovesText();
        // PopulateGrid(levelData.grid);
    }

    private void CalculateGoals(string[] grid)
    {
        foreach (var item in grid)
        {
            if (item == "bo")
            {
                BoxGoalInt++;
            }
            else if (item == "s")
            {
                StoneGoalInt++;
            }
            else if (item == "v")
            {
                VaseGoalInt++;
            }
        }
    }

    private void InitGoalTexts()
    {
        if (BoxGoalInt != 0)
        {
            BoxGoalText.SetText(BoxGoalInt.ToString());
        }
        else
        {
            Destroy(BoxGoal);
            Destroy(BoxGoalText);
        }

        if (StoneGoalInt != 0)
        {
            StoneGoalText.SetText(StoneGoalInt.ToString());
        }
        else
        {
            Destroy(StoneGoal);
            Destroy(StoneGoalText);

        }
        if (VaseGoalInt != 0)
        {
            VaseGoalText.SetText(VaseGoalInt.ToString());
        }
        else
        {
            Destroy(VaseGoal);
            Destroy(VaseGoalText);
        }
    }

    // Call this method whenever a move is made
    public void PlayerMadeMove()
    {
        if (remainingMoves > 0)
        {
            remainingMoves -= 1;
            UpdateRemainingMovesText();
            Debug.Log("PlayerMadeMove called. Remaining moves: " + remainingMoves);
            CheckGameState();
        }
        else
        {
            Debug.Log("Out of moves! Try again.");
            Item.Clear();
            FindObjectOfType<FailedTween>().LevelFailed();
            Debug.Log("Out of moves! Try again.");
        }
    }
    private void UpdateRemainingMovesText()
    {
        if (remainingMovesText != null) // Check if the text component is assigned
            remainingMovesText.text = remainingMoves.ToString();
        else
            Debug.LogWarning("RemainingMovesText is not assigned in the inspector");
    }

    private void CheckGameState()
    {
        // Check if the player has completed the level's objectives
        if (IsLevelCompleted())
        {
            Debug.Log("Level Completed!");
            // Proceed to the next level or show success message
            Item.Clear();
            FindObjectOfType<SuccessTween>().LevelSucceed();
            CompleteLevel();
            Invoke("LoadMainScene", 3.5f);
        }
        else if (remainingMoves <= 0)
        {
            Debug.Log("Out of moves! Try again.");
            Item.Clear();
            FindObjectOfType<FailedTween>().LevelFailed();
        }
    }
    
    private void LoadMainScene()
    {
        // Load the MainScene after the celebration
        SceneManager.LoadScene("MenuScene");
    }

    private void LoadLevelScene()
    {
        // Load the MainScene after the celebration
        SceneManager.LoadScene("LevelScene");
    }

    private bool IsLevelCompleted()
    {
        foreach (var item in Item.Values)
        {
            if (item.GetComponent<IDName>().IsObstacle || (item.GetComponent<IDName>().TypeOfCube == IDName.CubeType.Box) || (item.GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone) || (item.GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01) || (item.GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_02))
            {
                return false;
            }
        }
        Debug.Log("CONGRATS BRO.");
        return true; // Placeholder return value
    }


    public void CompleteLevel()
    {
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        PlayerPrefs.SetInt("CurrentLevel", currentLevel + 1);
        PlayerPrefs.Save();
        Debug.Log("Level completed. Player level now: " + (currentLevel + 1));
    }


    private void Spawn_FillGrid()
    {
        // Setup Cube Item # 1
        for (int x = 0; x < Width; x++) // Generate Horizontal Vector 2 Axis
        {
            for (int y = 0; y < Height; y++) // Generate Vertical Vector 2 Axis
            {
                var ch = data_grid[Height * x + y];
                //Debug.Log("Ch: " + ch);
                GameObject clone = null;
                if (ch == "b")
                {
                    clone = Instantiate(NormalCubePrefabs[0],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;
                }
                else if (ch == "g")
                {
                    clone = Instantiate(NormalCubePrefabs[1],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;

                }
                else if (ch == "r")
                {
                    clone = Instantiate(NormalCubePrefabs[2],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;

                }
                else if (ch == "y")
                {
                    clone = Instantiate(NormalCubePrefabs[3],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;

                }
                else if (ch == "rand")
                {
                    clone = Instantiate(NormalCubePrefabs[UnityEngine.Random.Range(0, NormalCubePrefabs.Length)],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;

                }
                else if (ch == "bo")
                {
                    clone = Instantiate(ObstacleCubePrefabs[0],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsObstacle = true;
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Box;

                }
                else if (ch == "s")
                {
                    clone = Instantiate(ObstacleCubePrefabs[1],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsObstacle = true;
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Stone;

                }
                else if (ch == "v")
                {
                    clone = Instantiate(ObstacleCubePrefabs[2],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsObstacle = true;
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Vase_01;

                }
                else if (ch == "t")
                {
                    clone = Instantiate(BreakCubePrefabs[0],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsObstacle = true;
                    clone.GetComponent<IDName>().IsBomb = true;
                    clone.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Bomb;
                    clone.GetComponent<IDName>().IsObstacle = false;
                    clone.GetComponent<IDName>().IsSpecialCube = true;
                    clone.GetComponent<IDName>().IsRunChangeSprites = false;
                }

                clone.transform.SetParent(SetItem);
                clone.AddComponent<CapsuleCollider2D>(); // Return to Collision
                clone.tag = "Item";
                clone.AddComponent<PickUp>();
                clone.name = "(" + x.ToString() + "," + y.ToString() + ")";
                clone.GetComponent<PickUp>().x = x;
                clone.GetComponent<PickUp>().y = y;
                Item.Add(new Tuple<int, int>(x, y), clone.GetComponent<PickUp>());
                clone.GetComponent<IDName>().IsRunChangeSprites = true;
            }
        }

        // Setup Bg # 2
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var ch = data_grid[Height * x + y];
                GameObject clone = null;
                if (ch == "b")
                {
                    clone = Instantiate(NormalCubePrefabs[0],
                    new Vector2(x, y), Quaternion.identity);
                }
                else if (ch == "g")
                {
                    clone = Instantiate(NormalCubePrefabs[1],
                    new Vector2(x, y), Quaternion.identity);
                }
                else if (ch == "r")
                {
                    clone = Instantiate(NormalCubePrefabs[2],
                    new Vector2(x, y), Quaternion.identity);
                }
                else if (ch == "y")
                {
                    clone = Instantiate(NormalCubePrefabs[3],
                    new Vector2(x, y), Quaternion.identity);
                }
                else if (ch == "rand")
                {
                    clone = Instantiate(NormalCubePrefabs[0],
                    new Vector2(x, y), Quaternion.identity);
                }
                else if (ch == "bo")
                {
                    clone = Instantiate(ObstacleCubePrefabs[0],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsObstacle = true;
                }
                else if (ch == "s")
                {
                    clone = Instantiate(ObstacleCubePrefabs[1],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsObstacle = true;
                }
                else if (ch == "v")
                {
                    clone = Instantiate(ObstacleCubePrefabs[2],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsObstacle = true;
                }
                else if (ch == "t")
                {
                    clone = Instantiate(BreakCubePrefabs[0],
                    new Vector2(x, y), Quaternion.identity);
                    clone.GetComponent<IDName>().IsBomb = true;
                }

                clone.transform.SetParent(SetBg);
                Destroy(clone.GetComponent<SpriteRenderer>());
                clone.AddComponent<BoxCollider2D>();
                clone.GetComponent<BoxCollider2D>().isTrigger = true; //return to Collider(Trigger)

                clone.AddComponent<Change>();
                clone.name = "(" + x.ToString() + "," + y.ToString() + ")";
                clone.GetComponent<Change>().x = x;
                clone.GetComponent<Change>().y = y;
                clone.GetComponent<Rigidbody2D>().gravityScale = 0f;
                clone.GetComponent<BoxCollider2D>().size = new Vector2(0.3f, 0.3f);
                clone.GetComponent<BoxCollider2D>().enabled = false;
            }
        }

        // Setup Bg Pos
        Invoke("Change_BgPos", 0.5f);
    }
    
    private void Change_BgPos()
    {
        for (int i = 0; i < SetBg.childCount; i++)
        {
            var _change = SetBg.GetChild(i).GetComponent<Change>();


            SetBg.GetChild(i).transform.position =
                Item[new Tuple<int, int>(_change.x, _change.y)].transform.position;
            SetBg.GetChild(i).GetComponent<BoxCollider2D>().enabled = false;
        }
    }
    
    private void StartSquare()
    {
        foreach (var item in Item.Values)
        {
            if (!item.GetComponent<IDName>().IsSpecialCube && !item.GetComponent<IDName>().IsObstacle) // False
            {
                if (!Square.ContainsKey(new Tuple<int, int>(item.x, item.y)))
                {
                    Square.Add(new Tuple<int, int>(item.x, item.y), new List<GameObject>());
                    Calculate_CubeCallBack(Item[new Tuple<int, int>(item.x, item.y)],
                        Item[new Tuple<int, int>(item.x, item.y)].GetComponent<IDName>(),
                        new Tuple<int, int>(item.x, item.y));
                }
            }
        }
        // Determining Cube Shapes
        foreach (var item in Square.Values)
        {
            if (item.Count == 1 || item.Count == 2 || item.Count == 3 || item.Count == 4)
            {
                for (int i = 0; i < item.Count; i++)
                {
                    item[i].GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;
                }
            }
            else
            {
                for (int i = 0; i < item.Count; i++)
                {
                    item[i].GetComponent<IDName>().TypeOfCube = IDName.CubeType.Bomb;
                }
            }
        }

        Square.Clear();
    }

    private IEnumerator Wait(float time, Action Call) // Wait for time
    {
        yield return new WaitForSeconds(time);
        if (Call != null)
        {
            Call.Invoke();
        }
    }

    public static void Calculate_CubeCallBack(PickUp p, IDName i, Tuple<int, int> id) // for the special cube finding 
    {
        var Top = new Tuple<int, int>(p.x, p.y + 1);
        var Down = new Tuple<int, int>(p.x, p.y - 1);
        var Right = new Tuple<int, int>(p.x + 1, p.y);
        var Left = new Tuple<int, int>(p.x - 1, p.y);

        // Break Top
        if (Item.ContainsKey(Top))
        {
            if (i.ID == Item[Top].GetComponent<IDName>().ID)
            {
                if (!Square[id].Contains(i.gameObject) && !i.IsSpecialCube && !i.IsObstacle)
                    Square[id].Add(i.gameObject);
                if (!Square[id].Contains(Item[Top].gameObject) && !Item[Top].GetComponent<IDName>().IsSpecialCube && !Item[Top].GetComponent<IDName>().IsObstacle)
                {
                    Square[id].Add(Item[Top].gameObject);
                    Item[Top].Continue_CalculateCallback(id);
                }
            }
        }


        // Break Down
        if (Item.ContainsKey(Down))
        {
            if (i.ID == Item[Down].GetComponent<IDName>().ID)
            {
                if (!Square[id].Contains(i.gameObject) && !i.IsSpecialCube && !i.IsObstacle)
                    Square[id].Add(i.gameObject);
                if (!Square[id].Contains(Item[Down].gameObject) && !Item[Down].GetComponent<IDName>().IsSpecialCube && !Item[Down].GetComponent<IDName>().IsObstacle)
                {
                    Square[id].Add(Item[Down].gameObject);
                    Item[Down].Continue_CalculateCallback(id);
                }
            }
        }

        // Break Right  
        if (Item.ContainsKey(Right))
        {
            if (i.ID == Item[Right].GetComponent<IDName>().ID)
            {
                if (!Square[id].Contains(i.gameObject) && !i.IsSpecialCube && !i.IsObstacle)
                    Square[id].Add(i.gameObject);
                if (!Square[id].Contains(Item[Right].gameObject) && !Item[Right].GetComponent<IDName>().IsSpecialCube && !Item[Right].GetComponent<IDName>().IsObstacle)
                {
                    Square[id].Add(Item[Right].gameObject);
                    Item[Right].Continue_CalculateCallback(id);
                }
            }
        }

        // Break Left
        if (Item.ContainsKey(Left))
        {
            if (i.ID == Item[Left].GetComponent<IDName>().ID)
            {
                if (!Square[id].Contains(i.gameObject) && !i.IsSpecialCube && !i.IsObstacle)
                    Square[id].Add(i.gameObject);
                if (!Square[id].Contains(Item[Left].gameObject) && !Item[Left].GetComponent<IDName>().IsSpecialCube && !Item[Left].GetComponent<IDName>().IsObstacle)
                {
                    Square[id].Add(Item[Left].gameObject);
                    Item[Left].Continue_CalculateCallback(id);
                }
            }
        }
    }
    public static void Calculate_CubeCallBack(PickUp p, IDName i) // for the breaking cubes
    {
        var Top = new Tuple<int, int>(p.x, p.y + 1);
        var Down = new Tuple<int, int>(p.x, p.y -1);
        var Right = new Tuple<int, int>(p.x + 1, p.y);
        var Left = new Tuple<int, int>(p.x - 1, p.y);
        // Break Top
        if (Item.ContainsKey(Top))
        {

            if (!Item[Top].GetComponent<IDName>().IsSpecialCube && !(Item[Top].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone))
            {
                if (i.ID == Item[Top].GetComponent<IDName>().ID)
                {
                    FindObjectOfType<GameManager>().ApplyBlastDamage(p);
                    if (!DeleteObject.Contains(i.gameObject))
                    {
                        DeleteObject.Add(i.gameObject);
                    }
                    if (!DeleteObject.Contains(Item[Top].gameObject))
                    {
                        DeleteObject.Add(Item[Top].gameObject);
                        Item[Top].Continue_CalculateCallback();
                    }
                }
            }
        }


        // Break Down
        if (Item.ContainsKey(Down))
        {
            if (!Item[Down].GetComponent<IDName>().IsSpecialCube && !(Item[Down].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone)) 
            {

                if (i.ID == Item[Down].GetComponent<IDName>().ID)
                {
                    FindObjectOfType<GameManager>().ApplyBlastDamage(p);
                    if (!DeleteObject.Contains(i.gameObject))
                    {
                        DeleteObject.Add(i.gameObject);
                    }
                    if (!DeleteObject.Contains(Item[Down].gameObject))
                    {
                        DeleteObject.Add(Item[Down].gameObject);
                        Item[Down].Continue_CalculateCallback();
                    }
                }
            }
        }

        // Break Right
        if (Item.ContainsKey(Right))
        {
            if (!Item[Right].GetComponent<IDName>().IsSpecialCube && !(Item[Right].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone))
            {


                if (i.ID == Item[Right].GetComponent<IDName>().ID)
                {
                    FindObjectOfType<GameManager>().ApplyBlastDamage(p);
                    if (!DeleteObject.Contains(i.gameObject))
                    {
                        DeleteObject.Add(i.gameObject);
                    }
                    if (!DeleteObject.Contains(Item[Right].gameObject))
                    {
                        DeleteObject.Add(Item[Right].gameObject);
                        Item[Right].Continue_CalculateCallback();
                    }
                }
            }
        }

        // Break Left
        if (Item.ContainsKey(Left))
        {
            if (!Item[Left].GetComponent<IDName>().IsSpecialCube && !(Item[Left].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone))
            {

                if (i.ID == Item[Left].GetComponent<IDName>().ID)
                {
                    
                    FindObjectOfType<GameManager>().ApplyBlastDamage(p);
                    if (!DeleteObject.Contains(i.gameObject))
                    {
                        DeleteObject.Add(i.gameObject);
                    }
                    if (!DeleteObject.Contains(Item[Left].gameObject))
                    {
                        DeleteObject.Add(Item[Left].gameObject);
                        Item[Left].Continue_CalculateCallback();
                    }
                }
            }
        }
    }
    public static bool Calculate_CubeCallBack(PickUp p) // for the single cubes
    {
        var Top = new Tuple<int, int>(p.x, p.y + 1);
        var Down = new Tuple<int, int>(p.x, p.y - 1);
        var Right = new Tuple<int, int>(p.x + 1, p.y);
        var Left = new Tuple<int, int>(p.x - 1, p.y);

        // Break Top
        if (Item.ContainsKey(Top))
        {
            if (!Item[Top].GetComponent<IDName>().IsSpecialCube && !Item[Top].GetComponent<IDName>().IsObstacle)
            {
                if (p.GetComponent<IDName>().ID == Item[Top].GetComponent<IDName>().ID)
                {
                    return true;
                }
            }
        }


        // Break Down
        if (Item.ContainsKey(Down))
        {
            if (!Item[Down].GetComponent<IDName>().IsSpecialCube && !Item[Down].GetComponent<IDName>().IsObstacle)
            {
                if (p.GetComponent<IDName>().ID == Item[Down].GetComponent<IDName>().ID)
                {
                    return true;
                }
            }
        }

        // Break Right
        if (Item.ContainsKey(Right))
        {
            if (!Item[Right].GetComponent<IDName>().IsSpecialCube && !Item[Right].GetComponent<IDName>().IsObstacle)
            {
                if (p.GetComponent<IDName>().ID == Item[Right].GetComponent<IDName>().ID)
                {
                    return true;
                }
            }
        }

        // Break Left
        if (Item.ContainsKey(Left))
        {
            if (!Item[Left].GetComponent<IDName>().IsSpecialCube && !Item[Left].GetComponent<IDName>().IsObstacle)
            {
                if (p.GetComponent<IDName>().ID == Item[Left].GetComponent<IDName>().ID)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void ApplyBlastDamage(PickUp p)
    {
        int x = p.x, y = p.y;
        // Logic to apply damage to adjacent objects
        var adjacentPositions = new List<Vector2Int>
        {
        new Vector2Int(x + 1, y),
        new Vector2Int(x - 1, y),
        new Vector2Int(x, y + 1),
        new Vector2Int(x, y - 1)
        // Add more positions for bomb explosion if needed
        };
        if (p.GetComponent<IDName>().ID < 5)
        {
            foreach (var pos in adjacentPositions)
            {
                var key = new Tuple<int, int>(pos.x, pos.y);
                if (Item.ContainsKey(key))
                {
                    var damageable = Item[key].GetComponent<IDName>() as IDamageable;
                    if (damageable != null && !(Item[key].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone))
                    {
                        if (Item[key].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
                        {
                            Item[key].GetComponent<IDName>().IsRunChangeSprites = false;
                            Item[key].GetComponent<SpriteRenderer>().sprite = Item[key].GetComponent<IDName>().Vase02Sprite;
                            Item[key].GetComponent<IDName>().ID = 8;
                        }
                        else
                        {
                            damageable.TakeDamage(1);
                            if (damageable.IsDestroyed())
                            {
                                if (Item[key].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Box && BoxGoal != null)
                                {
                                    BoxGoalUpdate();
                                }
                                else if (Item[key].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone && StoneGoal != null)
                                {
                                    StoneGoalUpdate();
                                }
                                else if (Item[key].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_02 && VaseGoal != null)
                                {
                                    VaseGoalUpdate();
                                }
                                SpawnBack(pos.x, pos.y);
                                Destroy(Item[key].gameObject);
                                Item.Remove(key);
                            }
                        }
                    }
                }
            }
        }
    }

    public void Delete_CallBack()
    {
        Invoke("Delete_Cubes", 0.1f);

        // Setup New Cubes
        Invoke("EnableBoxCollider2DCallback", 1f);

    }
    private void Delete_Cubes()
    {
        for (int i = 0; i < DeleteObject.Count; i++)
        {
            if (i == 0) // Switching Clicking Item to Bomb Object
            {
                if (DeleteObject[0].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Bomb)
                {
                    DeleteObject[0].GetComponent<IDName>().IsRunChangeSprites = false;
                    DeleteObject[0].GetComponent<SpriteRenderer>().sprite = DeleteObject[0].GetComponent<IDName>().Bomb;
                    DeleteObject[0].GetComponent<IDName>().IsSpecialCube = true;
                    DeleteObject[0].GetComponent<IDName>().IsBomb = true;
                }
                else
                {
                    SpawnBack(DeleteObject[i].GetComponent<PickUp>());
                    if (DeleteObject.Count != 0)
                    {
                        Destroy(DeleteObject[i]);
                    }
                }
            }
            else // Others wil be break
            {
                

                SpawnBack(DeleteObject[i].GetComponent<PickUp>());
                Destroy(DeleteObject[i]);
            }
        }
        Item.Clear();
        DeleteObject.Clear();
    }
    public void Delete_Invoke()
    {

        Invoke("Delete_CubesInvoke", 0.1f);
        // Setup New Cubes
        Invoke("EnableBoxCollider2DCallback", 1f);

    }
    private void Delete_CubesInvoke()
    {
        for (int i = 0; i < DeleteObject.Count; i++)
        {
            if (DeleteObject[i].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Box && BoxGoal != null)
            {
                BoxGoalUpdate();
            }
            else if (DeleteObject[i].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Stone && StoneGoal != null)
            {
                StoneGoalUpdate();
            }
            else if (DeleteObject[i].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_02 && VaseGoal != null)
            {
                VaseGoalUpdate();
            }
            SpawnBack(DeleteObject[i].GetComponent<PickUp>());
            Destroy(DeleteObject[i]);
        }
        if (remainingMoves == 0)
        {
            CheckGameState();
        }
        Item.Clear();
        DeleteObject.Clear();
    }
    private void SpawnBack(PickUp p)
    {
        GameObject prefabToUse = NormalCubePrefabs[UnityEngine.Random.Range(0, NormalCubePrefabs.Length)]; // Default to normal cubes
            
        GameObject clone = Instantiate(prefabToUse, new Vector2(p.x, p.y + PaddingTop), Quaternion.identity);
        clone.transform.SetParent(SetItem);
        clone.AddComponent<CapsuleCollider2D>();
        clone.tag = "Item";
        clone.AddComponent<PickUp>();
        clone.GetComponent<IDName>().IsRunChangeSprites = true;
        
    }

    private void SpawnBack(int x, int y)
    {
        var clone = Instantiate(NormalCubePrefabs[UnityEngine.Random.Range(0, NormalCubePrefabs.Length)],
                    new Vector2(x, y + PaddingTop), Quaternion.identity);
        clone.transform.SetParent(SetItem);
        clone.AddComponent<CapsuleCollider2D>();
        clone.tag = "Item";
        clone.AddComponent<PickUp>();
        clone.GetComponent<IDName>().IsRunChangeSprites = true;
        // Make sure to set other properties as needed, such as ID, TypeOfCube, etc.
    }


    private void EnableBoxCollider2DCallback()
    {
        if (SetBg)
        {
            for (int i = 0; i < SetBg.childCount; i++)
            {
                SetBg.GetChild(i).GetComponent<BoxCollider2D>().enabled = true;
            }
            Invoke("DisableBoxCollider2DCallback", 0.1f);
        }
    }
    private void DisableBoxCollider2DCallback()
    {

        for (int i = 0; i < SetBg.childCount; i++)
        {
            SetBg.GetChild(i).GetComponent<BoxCollider2D>().enabled = false;
        }

        // #1 Continous Cube Change
        StartCoroutine(Wait(0.1f, () =>
        {
            StartSquare();
            foreach (var item in Item.Values)
            {
                if (!Calculate_CubeCallBack(item))
                {
                    switch (item.GetComponent<IDName>().ID)
                    {
                        case 0:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Bomb;
                            break;
                        case 1:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;
                            break;
                        case 2:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;
                            break;
                        case 3:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;
                            break;
                        case 4:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Normal;
                            break;
                        case 5:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Box;
                            break;
                        case 6:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Stone;
                            break;
                        case 7:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Vase_01;
                            break;
                        case 8:
                            item.GetComponent<IDName>().TypeOfCube = IDName.CubeType.Vase_02;
                            break;
                        default:
                            break;
                    }
                    
                }
            }
        }));
        CheckGameState();
    }


    public void BoxGoalUpdate()
    {
        BoxGoalInt--;
        if (BoxGoalInt > 0)
        {
            Debug.Log("BoxGoalInt--");
            BoxGoalText.SetText(BoxGoalInt.ToString());
        }
        else
        {
            Debug.Log("BoxGoalText destroyed and TickSprite Instantiated.");
            GameObject clone = Instantiate(TickSprite, new Vector3(BoxGoalText.transform.position.x, BoxGoalText.transform.position.y, BoxGoalText.transform.position.z), Quaternion.identity);
            Ticks.Add(clone);
            Destroy(BoxGoalText);
        }
    }

    public void StoneGoalUpdate()
    {
        StoneGoalInt--;
        if (StoneGoalInt > 0)
        {
            Debug.Log("StoneGoalInt--");
            StoneGoalText.SetText(StoneGoalInt.ToString());
        }
        else
        {
            Debug.Log("StoneGoalText destroyed and TickSprite Instantiated.");
            GameObject clone = Instantiate(TickSprite, new Vector3(StoneGoalText.transform.position.x, StoneGoalText.transform.position.y, StoneGoalText.transform.position.z), Quaternion.identity);
            Ticks.Add(clone);
            Destroy(StoneGoalText);
        }
    }

    public void VaseGoalUpdate()
    {
        VaseGoalInt--;
        if (VaseGoalInt > 0)
        {
            Debug.Log("VaseGoalInt--");
            VaseGoalText.SetText(VaseGoalInt.ToString());
        }
        else
        {
            Debug.Log("VaseGoalText destroyed and TickSprite Instantiated.");
            GameObject clone = Instantiate(TickSprite, new Vector3(VaseGoalText.transform.position.x, VaseGoalText.transform.position.y, VaseGoalText.transform.position.z), Quaternion.identity);
            Ticks.Add(clone);
            Destroy(VaseGoalText);
        }
    }


    // Check
    public void CalculateCubeForBomb(IDName i)
    {
        var p = i.GetComponent<PickUp>();
        var Top = new Tuple<int, int>(p.x, p.y + 1);
        var Down = new Tuple<int, int>(p.x, p.y - 1);
        var Left = new Tuple<int, int>(p.x - 1, p.y);
        var Right = new Tuple<int, int>(p.x + 1, p.y);
        var TopLeft = new Tuple<int, int>(p.x - 1, p.y + 1);
        var TopRight = new Tuple<int, int>(p.x + 1, p.y + 1);
        var DownLeft = new Tuple<int, int>(p.x - 1, p.y - 1);
        var DownRight = new Tuple<int, int>(p.x + 1, p.y - 1);
        var TopTop = new Tuple<int, int>(p.x, p.y + 2);
        var TopTopLeft = new Tuple<int, int>(p.x - 1, p.y + 2);
        var TopTopLeftLeft = new Tuple<int, int>(p.x - 2, p.y + 2);
        var TopTopRight = new Tuple<int, int>(p.x + 1, p.y + 2);
        var TopTopRightRight = new Tuple<int, int>(p.x + 2, p.y + 2);
        var DownDown = new Tuple<int, int>(p.x, p.y - 2);
        var DownDownLeft = new Tuple<int, int>(p.x - 1, p.y - 2);
        var DownDownLeftLeft = new Tuple<int, int>(p.x - 2, p.y - 2);
        var DownDownRight = new Tuple<int, int>(p.x + 1, p.y - 2);
        var DownDownRightRight = new Tuple<int, int>(p.x + 2, p.y - 2);
        var RightRight = new Tuple<int, int>(p.x + 2, p.y);
        var RightRightTop = new Tuple<int, int>(p.x + 2, p.y + 1);
        var RightRightDown = new Tuple<int, int>(p.x + 2, p.y - 1);
        var LeftLeft = new Tuple<int, int>(p.x - 2, p.y);
        var LeftLeftTop = new Tuple<int, int>(p.x - 2, p.y + 1);
        var LeftLeftDown = new Tuple<int, int>(p.x - 2, p.y - 1);

        i.TypeOfCube = IDName.CubeType.Normal;

        DeleteObject.Add(i.gameObject);

        if (Item.ContainsKey(Top))
        {
            if (Item[Top].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[Top].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[Top].GetComponent<SpriteRenderer>().sprite = Item[Top].GetComponent<IDName>().Vase02Sprite;
                Item[Top].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[Top].gameObject))
            {
                DeleteObject.Add(Item[Top].gameObject);
                CheckContinueCube_CallBack(Item[Top].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(Down))
        {
            if (Item[Down].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[Down].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[Down].GetComponent<SpriteRenderer>().sprite = Item[Down].GetComponent<IDName>().Vase02Sprite;
                Item[Down].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[Down].gameObject))
            {
                DeleteObject.Add(Item[Down].gameObject);
                CheckContinueCube_CallBack(Item[Down].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(Left))
        {
            if (Item[Left].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[Left].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[Left].GetComponent<SpriteRenderer>().sprite = Item[Left].GetComponent<IDName>().Vase02Sprite;
                Item[Left].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[Left].gameObject))
            {
                DeleteObject.Add(Item[Left].gameObject);
                CheckContinueCube_CallBack(Item[Left].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(Right))
        {
            if (Item[Right].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[Right].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[Right].GetComponent<SpriteRenderer>().sprite = Item[Right].GetComponent<IDName>().Vase02Sprite;
                Item[Right].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[Right].gameObject))
            {
                DeleteObject.Add(Item[Right].gameObject);
                CheckContinueCube_CallBack(Item[Right].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopLeft))
        {
            if (Item[TopLeft].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[TopLeft].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[TopLeft].GetComponent<SpriteRenderer>().sprite = Item[TopLeft].GetComponent<IDName>().Vase02Sprite;
                Item[TopLeft].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[TopLeft].gameObject))
            {
                DeleteObject.Add(Item[TopLeft].gameObject);
                CheckContinueCube_CallBack(Item[TopLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopRight))
        {
            if (Item[TopRight].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[TopRight].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[TopRight].GetComponent<SpriteRenderer>().sprite = Item[TopRight].GetComponent<IDName>().Vase02Sprite;
                Item[TopRight].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[TopRight].gameObject))
            {
                DeleteObject.Add(Item[TopRight].gameObject);
                CheckContinueCube_CallBack(Item[TopRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownLeft))
        {
            if (Item[DownLeft].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[DownLeft].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[DownLeft].GetComponent<SpriteRenderer>().sprite = Item[TopRight].GetComponent<IDName>().Vase02Sprite;
                Item[DownLeft].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[DownLeft].gameObject))
            {
                DeleteObject.Add(Item[DownLeft].gameObject);
                CheckContinueCube_CallBack(Item[DownLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownRight))
        {
            if (Item[DownRight].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[DownRight].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[DownRight].GetComponent<SpriteRenderer>().sprite = Item[TopRight].GetComponent<IDName>().Vase02Sprite;
                Item[DownRight].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[DownRight].gameObject))
            {
                DeleteObject.Add(Item[DownRight].gameObject);
                CheckContinueCube_CallBack(Item[DownRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTop))
        {
            if (Item[TopTop].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[TopTop].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[TopTop].GetComponent<SpriteRenderer>().sprite = Item[TopTop].GetComponent<IDName>().Vase02Sprite;
                Item[TopTop].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[TopTop].gameObject))
            {
                DeleteObject.Add(Item[TopTop].gameObject);
                CheckContinueCube_CallBack(Item[TopTop].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopLeft))
        {
            if (Item[TopTopLeft].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[TopTopLeft].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[TopTopLeft].GetComponent<SpriteRenderer>().sprite = Item[TopTopLeft].GetComponent<IDName>().Vase02Sprite;
                Item[TopTopLeft].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[TopTopLeft].gameObject))
            {
                DeleteObject.Add(Item[TopTopLeft].gameObject);
                CheckContinueCube_CallBack(Item[TopTopLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopLeftLeft))
        {
            if (Item[TopTopLeftLeft].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[TopTopLeftLeft].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[TopTopLeftLeft].GetComponent<SpriteRenderer>().sprite = Item[TopTopLeftLeft].GetComponent<IDName>().Vase02Sprite;
                Item[TopTopLeftLeft].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[TopTopLeftLeft].gameObject))
            {
                DeleteObject.Add(Item[TopTopLeftLeft].gameObject);
                CheckContinueCube_CallBack(Item[TopTopLeftLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopRight))
        {
            if (Item[TopTopRight].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[TopTopRight].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[TopTopRight].GetComponent<SpriteRenderer>().sprite = Item[TopTopRight].GetComponent<IDName>().Vase02Sprite;
                Item[TopTopRight].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[TopTopRight].gameObject))
            {
                DeleteObject.Add(Item[TopTopRight].gameObject);
                CheckContinueCube_CallBack(Item[TopTopRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopRightRight))
        {
            if (Item[TopTopRightRight].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[TopTopRightRight].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[TopTopRightRight].GetComponent<SpriteRenderer>().sprite = Item[TopTopRightRight].GetComponent<IDName>().Vase02Sprite;
                Item[TopTopRightRight].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[TopTopRightRight].gameObject))
            {
                DeleteObject.Add(Item[TopTopRightRight].gameObject);
                CheckContinueCube_CallBack(Item[TopTopRightRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDown))
        {
            if (Item[DownDown].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[DownDown].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[DownDown].GetComponent<SpriteRenderer>().sprite = Item[DownDown].GetComponent<IDName>().Vase02Sprite;
                Item[DownDown].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[DownDown].gameObject))
            {
                DeleteObject.Add(Item[DownDown].gameObject);
                CheckContinueCube_CallBack(Item[DownDown].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownLeft))
        {
            if (Item[DownDownLeft].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[DownDownLeft].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[DownDownLeft].GetComponent<SpriteRenderer>().sprite = Item[DownDownLeft].GetComponent<IDName>().Vase02Sprite;
                Item[DownDownLeft].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[DownDownLeft].gameObject))
            {
                DeleteObject.Add(Item[DownDownLeft].gameObject);
                CheckContinueCube_CallBack(Item[DownDownLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownLeftLeft))
        {
            if (Item[DownDownLeftLeft].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[DownDownLeftLeft].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[DownDownLeftLeft].GetComponent<SpriteRenderer>().sprite = Item[DownDownLeftLeft].GetComponent<IDName>().Vase02Sprite;
                Item[DownDownLeftLeft].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[DownDownLeftLeft].gameObject))
            {
                DeleteObject.Add(Item[DownDownLeftLeft].gameObject);
                CheckContinueCube_CallBack(Item[DownDownLeftLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownRight))
        {
            if (Item[DownDownRight].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[DownDownRight].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[DownDownRight].GetComponent<SpriteRenderer>().sprite = Item[DownDownRight].GetComponent<IDName>().Vase02Sprite;
                Item[DownDownRight].GetComponent<IDName>().ID = 8;
            }

            else if (!DeleteObject.Contains(Item[DownDownRight].gameObject))
            {
                DeleteObject.Add(Item[DownDownRight].gameObject);
                CheckContinueCube_CallBack(Item[DownDownRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownRightRight))
        {
            if (Item[DownDownRightRight].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[DownDownRightRight].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[DownDownRightRight].GetComponent<SpriteRenderer>().sprite = Item[DownDownRightRight].GetComponent<IDName>().Vase02Sprite;
                Item[DownDownRightRight].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[DownDownRightRight].gameObject))
            {
                DeleteObject.Add(Item[DownDownRightRight].gameObject);
                CheckContinueCube_CallBack(Item[DownDownRightRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(RightRight))
        {
            if (Item[RightRight].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[RightRight].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[RightRight].GetComponent<SpriteRenderer>().sprite = Item[RightRight].GetComponent<IDName>().Vase02Sprite;
                Item[RightRight].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[RightRight].gameObject))
            {
                DeleteObject.Add(Item[RightRight].gameObject);
                CheckContinueCube_CallBack(Item[RightRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(RightRightTop))
        {
            if (Item[RightRightTop].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[RightRightTop].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[RightRightTop].GetComponent<SpriteRenderer>().sprite = Item[RightRightTop].GetComponent<IDName>().Vase02Sprite;
                Item[RightRightTop].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[RightRightTop].gameObject))
            {
                DeleteObject.Add(Item[RightRightTop].gameObject);
                CheckContinueCube_CallBack(Item[RightRightTop].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(RightRightDown))
        {
            if (Item[RightRightDown].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[RightRightDown].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[RightRightDown].GetComponent<SpriteRenderer>().sprite = Item[RightRightDown].GetComponent<IDName>().Vase02Sprite;
                Item[RightRightDown].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[RightRightDown].gameObject))
            {
                DeleteObject.Add(Item[RightRightDown].gameObject);
                CheckContinueCube_CallBack(Item[RightRightDown].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(LeftLeft))
        {
            if (Item[LeftLeft].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[LeftLeft].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[LeftLeft].GetComponent<SpriteRenderer>().sprite = Item[LeftLeft].GetComponent<IDName>().Vase02Sprite;
                Item[LeftLeft].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[LeftLeft].gameObject))
            {
                DeleteObject.Add(Item[LeftLeft].gameObject);
                CheckContinueCube_CallBack(Item[LeftLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(LeftLeftTop))
        {
            if (Item[LeftLeftTop].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[LeftLeftTop].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[LeftLeftTop].GetComponent<SpriteRenderer>().sprite = Item[LeftLeftTop].GetComponent<IDName>().Vase02Sprite;
                Item[LeftLeftTop].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[LeftLeftTop].gameObject))
            {
                DeleteObject.Add(Item[LeftLeftTop].gameObject);
                CheckContinueCube_CallBack(Item[LeftLeftTop].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(LeftLeftDown))
        {
            if (Item[LeftLeftDown].GetComponent<IDName>().TypeOfCube == IDName.CubeType.Vase_01)
            {
                Item[LeftLeftDown].GetComponent<IDName>().IsRunChangeSprites = false;
                Item[LeftLeftDown].GetComponent<SpriteRenderer>().sprite = Item[LeftLeftDown].GetComponent<IDName>().Vase02Sprite;
                Item[LeftLeftDown].GetComponent<IDName>().ID = 8;
            }
            else if (!DeleteObject.Contains(Item[LeftLeftDown].gameObject))
            {
                DeleteObject.Add(Item[LeftLeftDown].gameObject);
                CheckContinueCube_CallBack(Item[LeftLeftDown].GetComponent<IDName>());
            }
        }




        Delete_Invoke();
    }

    public void CalculateCubeForBomb_CallBack(IDName i)
    {
        var p = i.GetComponent<PickUp>();
        var Top = new Tuple<int, int>(p.x, p.y + 1);
        var Down = new Tuple<int, int>(p.x, p.y - 1);
        var Left = new Tuple<int, int>(p.x - 1, p.y);
        var Right = new Tuple<int, int>(p.x + 1, p.y);
        var TopLeft = new Tuple<int, int>(p.x - 1, p.y + 1);
        var TopRight = new Tuple<int, int>(p.x + 1, p.y + 1);
        var DownLeft = new Tuple<int, int>(p.x - 1, p.y - 1);
        var DownRight = new Tuple<int, int>(p.x + 1, p.y - 1);
        var TopTop = new Tuple<int, int>(p.x, p.y + 2);
        var TopTopLeft = new Tuple<int, int>(p.x - 1, p.y + 2);
        var TopTopLeftLeft = new Tuple<int, int>(p.x - 2, p.y + 2);
        var TopTopRight = new Tuple<int, int>(p.x + 1, p.y + 2);
        var TopTopRightRight = new Tuple<int, int>(p.x + 2, p.y + 2);
        var DownDown = new Tuple<int, int>(p.x, p.y - 2);
        var DownDownLeft = new Tuple<int, int>(p.x - 1, p.y - 2);
        var DownDownLeftLeft = new Tuple<int, int>(p.x - 2, p.y - 2);
        var DownDownRight = new Tuple<int, int>(p.x + 1, p.y - 2);
        var DownDownRightRight = new Tuple<int, int>(p.x + 2, p.y - 2);
        var RightRight = new Tuple<int, int>(p.x + 2, p.y);
        var RightRightTop = new Tuple<int, int>(p.x + 2, p.y + 1);
        var RightRightDown = new Tuple<int, int>(p.x + 2, p.y - 1);
        var LeftLeft = new Tuple<int, int>(p.x - 2, p.y);
        var LeftLeftTop = new Tuple<int, int>(p.x - 2, p.y + 1);
        var LeftLeftDown = new Tuple<int, int>(p.x - 2, p.y - 1);

        i.TypeOfCube = IDName.CubeType.Normal;

        DeleteObject.Add(i.gameObject);

        if (Item.ContainsKey(Top))
        {
            if (!DeleteObject.Contains(Item[Top].gameObject))
            {
                DeleteObject.Add(Item[Top].gameObject);
                CheckContinueCube_CallBack(Item[Top].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(Down))
        {
            if (!DeleteObject.Contains(Item[Down].gameObject))
            {
                DeleteObject.Add(Item[Down].gameObject);
                CheckContinueCube_CallBack(Item[Down].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(Left))
        {
            if (!DeleteObject.Contains(Item[Left].gameObject))
            {
                DeleteObject.Add(Item[Left].gameObject);
                CheckContinueCube_CallBack(Item[Left].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(Right))
        {
            if (!DeleteObject.Contains(Item[Right].gameObject))
            {
                DeleteObject.Add(Item[Right].gameObject);
                CheckContinueCube_CallBack(Item[Right].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopLeft))
        {
            if (!DeleteObject.Contains(Item[TopLeft].gameObject))
            {
                DeleteObject.Add(Item[TopLeft].gameObject);
                CheckContinueCube_CallBack(Item[TopLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopRight))
        {
            if (!DeleteObject.Contains(Item[TopRight].gameObject))
            {
                DeleteObject.Add(Item[TopRight].gameObject);
                CheckContinueCube_CallBack(Item[TopRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownLeft))
        {
            if (!DeleteObject.Contains(Item[DownLeft].gameObject))
            {
                DeleteObject.Add(Item[DownLeft].gameObject);
                CheckContinueCube_CallBack(Item[DownLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownRight))
        {
            if (!DeleteObject.Contains(Item[DownRight].gameObject))
            {
                DeleteObject.Add(Item[DownRight].gameObject);
                CheckContinueCube_CallBack(Item[DownRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTop))
        {
            if (!DeleteObject.Contains(Item[TopTop].gameObject))
            {
                DeleteObject.Add(Item[TopTop].gameObject);
                CheckContinueCube_CallBack(Item[TopTop].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopLeft))
        {
            if (!DeleteObject.Contains(Item[TopTopLeft].gameObject))
            {
                DeleteObject.Add(Item[TopTopLeft].gameObject);
                CheckContinueCube_CallBack(Item[TopTopLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopLeftLeft))
        {
            if (!DeleteObject.Contains(Item[TopTopLeftLeft].gameObject))
            {
                DeleteObject.Add(Item[TopTopLeftLeft].gameObject);
                CheckContinueCube_CallBack(Item[TopTopLeftLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopRight))
        {
            if (!DeleteObject.Contains(Item[TopTopRight].gameObject))
            {
                DeleteObject.Add(Item[TopTopRight].gameObject);
                CheckContinueCube_CallBack(Item[TopTopRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(TopTopRightRight))
        {
            if (!DeleteObject.Contains(Item[TopTopRightRight].gameObject))
            {
                DeleteObject.Add(Item[TopTopRightRight].gameObject);
                CheckContinueCube_CallBack(Item[TopTopRightRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDown))
        {
            if (!DeleteObject.Contains(Item[DownDown].gameObject))
            {
                DeleteObject.Add(Item[DownDown].gameObject);
                CheckContinueCube_CallBack(Item[DownDown].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownLeft))
        {
            if (!DeleteObject.Contains(Item[DownDownLeft].gameObject))
            {
                DeleteObject.Add(Item[DownDownLeft].gameObject);
                CheckContinueCube_CallBack(Item[DownDownLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownLeftLeft))
        {
            if (!DeleteObject.Contains(Item[DownDownLeftLeft].gameObject))
            {
                DeleteObject.Add(Item[DownDownLeftLeft].gameObject);
                CheckContinueCube_CallBack(Item[DownDownLeftLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownRight))
        {
            if (!DeleteObject.Contains(Item[DownDownRight].gameObject))
            {
                DeleteObject.Add(Item[DownDownRight].gameObject);
                CheckContinueCube_CallBack(Item[DownDownRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(DownDownRightRight))
        {
            if (!DeleteObject.Contains(Item[DownDownRightRight].gameObject))
            {
                DeleteObject.Add(Item[DownDownRightRight].gameObject);
                CheckContinueCube_CallBack(Item[DownDownRightRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(RightRight))
        {
            if (!DeleteObject.Contains(Item[RightRight].gameObject))
            {
                DeleteObject.Add(Item[RightRight].gameObject);
                CheckContinueCube_CallBack(Item[RightRight].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(RightRightTop))
        {
            if (!DeleteObject.Contains(Item[RightRightTop].gameObject))
            {
                DeleteObject.Add(Item[RightRightTop].gameObject);
                CheckContinueCube_CallBack(Item[RightRightTop].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(RightRightDown))
        {
            if (!DeleteObject.Contains(Item[RightRightDown].gameObject))
            {
                DeleteObject.Add(Item[RightRightDown].gameObject);
                CheckContinueCube_CallBack(Item[RightRightDown].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(LeftLeft))
        {
            if (!DeleteObject.Contains(Item[LeftLeft].gameObject))
            {
                DeleteObject.Add(Item[LeftLeft].gameObject);
                CheckContinueCube_CallBack(Item[LeftLeft].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(LeftLeftTop))
        {
            if (!DeleteObject.Contains(Item[LeftLeftTop].gameObject))
            {
                DeleteObject.Add(Item[LeftLeftTop].gameObject);
                CheckContinueCube_CallBack(Item[LeftLeftTop].GetComponent<IDName>());
            }
        }

        if (Item.ContainsKey(LeftLeftDown))
        {
            if (!DeleteObject.Contains(Item[LeftLeftDown].gameObject))
            {
                DeleteObject.Add(Item[LeftLeftDown].gameObject);
                CheckContinueCube_CallBack(Item[LeftLeftDown].GetComponent<IDName>());
            }
        }
    }


    // Check continue cube
    public void CheckContinueCube_CallBack(IDName i)
    {
        if (i.IsBomb)
        {
            CalculateCubeForBomb_CallBack(i);
        }
    }
}
