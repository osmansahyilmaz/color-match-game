using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class PickUp : MonoBehaviour
{
    public int x, y;

    // Calculate #1 Call 1 Frame when Clicked on that Gameobject
    private void OnMouseDown()
    {
        bool IsAlone = IsCubeAlone(GetComponent<PickUp>());
        int ID = GetComponent<IDName>().ID;
        if ((!IsAlone) && ID < 5 )
        {
            FindObjectOfType<GameManager>().PlayerMadeMove();
        }

        if (GetComponent<IDName>().IsSpecialCube)
        {
            var i = GetComponent<IDName>();

            if (i.IsBomb)
            {
                FindObjectOfType<GameManager>().CalculateCubeForBomb(i);
            }
        }
        else if (GetComponent<IDName>().ID < 5)
        {
            GameManager.Calculate_CubeCallBack(GetComponent<PickUp>(), GetComponent<IDName>());
            FindObjectOfType<GameManager>().Delete_CallBack();
        }
    }

    // Calculate #2 // Break Cube   
    public void Continue_CalculateCallback()
    {
        GameManager.Calculate_CubeCallBack(GetComponent<PickUp>(), GetComponent<IDName>());
    }

    // Calculate #3 // ChangeSprites
    public void Continue_CalculateCallback(Tuple<int, int> id)
    {
        GameManager.Calculate_CubeCallBack(GetComponent<PickUp>(), GetComponent<IDName>(), id);
    }

    public bool IsCubeAlone(PickUp cube)
    {
        var cubeID = cube.GetComponent<IDName>().ID; // Get the ID or type of the cube
        var x = cube.x;
        var y = cube.y;

        // Define tuples for adjacent positions
        var adjacentPositions = new Tuple<int, int>[]
        {
        new Tuple<int, int>(x, y + 1), // Top
        new Tuple<int, int>(x, y - 1), // Bottom
        new Tuple<int, int>(x + 1, y), // Right
        new Tuple<int, int>(x - 1, y)  // Left
        };

        foreach (var pos in adjacentPositions)
        {
            if (GameManager.Item.TryGetValue(pos, out PickUp adjacentCube))
            {
                // Check if the adjacent cube has the same ID/type
                if (adjacentCube.GetComponent<IDName>().ID == cubeID)
                {
                    return false; // Found a similar cube, not alone
                }
            }
        }

        return true; // No similar cubes found, it's alone
    }
}

