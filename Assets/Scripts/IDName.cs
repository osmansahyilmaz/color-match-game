using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IDName : MonoBehaviour, IDamageable
{
    public int ID;
    public int health = 1;

    public enum CubeType { Normal, Bomb, Box, Stone, Vase_01, Vase_02}

    public CubeType TypeOfCube;

    public Sprite[] ChangeSprites;

    public Sprite Bomb;
    public Sprite Vase02Sprite;
    public bool IsBomb;
    public bool IsRunChangeSprites;
    public bool IsSpecialCube = false;
    public bool IsObstacle = false;

    public void TakeDamage(int amount)
    {
        if (IsObstacle)
        {
            health -= amount;
        }
    }

    public bool IsDestroyed()
    {
        return health <= 0;
    }

    public void Update()
    {
        if (IsRunChangeSprites)
        {
            // Debugging: Log the length of the array and the attempted access index
            //Debug.Log($"ChangeSprites Length: {ChangeSprites.Length}, TypeOfCube: {TypeOfCube}");

            int index = -1; // Default invalid index
            switch (TypeOfCube)
            {
                case CubeType.Normal:
                    index = 0;
                    break;
                case CubeType.Bomb:
                    index = 1;
                    break;
                case CubeType.Box:
                    index = 2;
                    break;
                case CubeType.Stone:
                    index = 3;
                    break;
                case CubeType.Vase_01:
                    index = 4;
                    break;
                case CubeType.Vase_02:
                    index = 5;
                    break;
            }

            // Check if the index is valid before accessing the array
            if (index >= 0 && index < ChangeSprites.Length)
            {
                GetComponent<SpriteRenderer>().sprite = ChangeSprites[index];
            }
            else
            {
                //Debug.LogWarning($"Attempted to access ChangeSprites with invalid index: {index}");
            }
        }
    }

}
