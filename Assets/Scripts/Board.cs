using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Attributes")]
    [SerializeField] private int width;
    [SerializeField] private int height;

    private Tile[,] allTiles;

    private void Start()
    {
        allTiles = new Tile[width, height];
        Init();
    }

    private void Init()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Debug.Log($"({i},{j})");

                Vector2 tempPosition = new Vector2(i, j);

                GameObject tileGameObject = Instantiate(tilePrefab, tempPosition, Quaternion.identity, transform);
            }
        }
    }
}
