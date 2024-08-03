using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Node : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] private TextMeshProUGUI coordinatesText;

    [Header("Attributes")]
    [SerializeField] public bool isUsable;
    [SerializeField] private bool debugCoordinates;

    public GameObject potionGameObject;

    public Transform potionStandPos;

    public Node(bool isUsable, GameObject potionGameObject)
    {
        Init(isUsable, potionGameObject);
    }
    public void Init(bool isUsable, GameObject potionGameObject)
    {
        this.isUsable = isUsable;
        this.potionGameObject = potionGameObject;

        foreach (Transform child in potionStandPos)
        {
            if (!child.GetComponent<Potion>())
            {
                Destroy(child.gameObject);
            }
        }
    }
    public void SetCoordinates(int x, int y)
    {
        if (!debugCoordinates)
        {
            coordinatesText.enabled = false;
        }
        else
        {
            coordinatesText.enabled = true;
        }
        coordinatesText.text = $"({x},{y})";
    }

    public void Clear()
    {
        Destroy(potionGameObject);
        Init(isUsable, null);
    }

    private void Update()
    {
        if (Time.frameCount % 600 == 0)
        {
            if (!debugCoordinates)
            {
                coordinatesText.enabled = false;
            }
            else
            {
                coordinatesText.enabled = true;
            }
        }
    }
}
