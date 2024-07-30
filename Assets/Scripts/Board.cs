using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [Header("Refrences")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject rowGameObject;
    [SerializeField] private GameObject[] potionsPrefab;

    [Header("Attributes")]
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float spacingX = 10;
    [SerializeField] private float spacingY = 10;
    [SerializeField] private bool isProcessingMove;


    private Node[,] board;
    private Potion selectedPotion;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Init();

        Potion.OnAnyPotionClick += ((potion) =>
        {
            if (isProcessingMove) return;

            SelectPotion(board[potion.x, potion.y].potionGameObject.GetComponent<Potion>());
        });
    }

    #region Initializing Board
    private void Init()
    {
        ClearBoard();

        rowGameObject.SetActive(true);

        board = new Node[width, height];

        GetComponent<HorizontalLayoutGroup>().spacing = spacingX;

        for (int y = 0; y < height; y++)
        {
            GameObject row = Instantiate(rowGameObject, Vector3.zero, Quaternion.identity, transform);

            row.GetComponent<VerticalLayoutGroup>().spacing = spacingY;

            for (int x = 0; x < width; x++)
            {
                int randomIndex = Random.Range(0, potionsPrefab.Length);
                GameObject potionGameObject = Instantiate(potionsPrefab[randomIndex], Vector3.zero, Quaternion.identity, row.transform);

                potionGameObject.GetComponent<Potion>().SetCoordinates(x, y);

                board[x, y] = new Node(true, potionGameObject);
            }

        }
        rowGameObject.SetActive(false);

        if (CheckBoard(false))
        {
            Init();
        }
    }

    private void ClearBoard()
    {
        foreach (Transform child in transform)
        {
            if (child == rowGameObject.transform) continue;

            Destroy(child.gameObject);
        }
    }

    #endregion

    #region Checking Board
    public bool CheckBoard(bool takeAction)
    {
        Debug.Log("Checking ...");
        bool hasMatched = false;

        List<Potion> potionsToRemove = new List<Potion>();

        foreach (Node node in board)
        {
            if (node.potionGameObject != null)
                node.potionGameObject.GetComponent<Potion>().isMatched = false;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Check Potion Node is usable
                if (board[x, y].isUsable)
                {
                    //then procced to get Potion in Node
                    Potion potion = board[x, y].potionGameObject.GetComponent<Potion>();

                    //ensure is not matched
                    if (!potion.isMatched)
                    {
                        //run matching logic
                        MatchResult matchedPotions = IsConnected(potion);

                        if (matchedPotions.connectedPotions.Count >= 3)
                        {
                            //Matching

                            MatchResult superMatchPotions = SuperMatch(matchedPotions);

                            potionsToRemove.AddRange(superMatchPotions.connectedPotions);

                            foreach (Potion pot in superMatchPotions.connectedPotions)
                            {
                                pot.isMatched = true;
                            }

                            hasMatched = true;
                        }
                    }
                }
            }
        }
        if (takeAction)
        {
            foreach (Potion potionToRemove in potionsToRemove)
            {
                potionToRemove.isMatched = false;
            }

            RemoveAndRefill(potionsToRemove);
            //if (CheckBoard(false))
            //{
            //    CheckBoard(true);
            //}
        }
        return hasMatched;
    }

    private void RemoveAndRefill(List<Potion> potionsToRemove)
    {
        //Removing the potion and clearing board at that location
        foreach (Potion potion in potionsToRemove)
        {
            int tempX = potion.xCoordinate;
            int tempY = potion.yCoordinate;

            //Destroy Potion Object
            Destroy(potion);

            //Clear Board at that location and create blank node
            board[tempX, tempY] = new Node(true, null);
        }

        //for (int x = 0; x < width; x++)
        //{
        //    for (int y = 0; y < height; y++)
        //    {
        //        if (board[x, y].potionGameObject == null)
        //        {
        //            ReFillPotion(x, y);
        //        }
        //    }
        //}
    }

    private void ReFillPotion(int x, int y)
    {
        int yOffset = 1;

        while (y + yOffset < height && board[x, y + yOffset].potionGameObject == null)
        {
            yOffset++;
        }

        if (y + yOffset < height && board[x, y + yOffset].potionGameObject != null)
        {
            //we've found a potion
            Potion potionAbove = board[x, y + yOffset].potionGameObject.GetComponent<Potion>();

            //let's move it to the current location
            Vector2 targetPos = new Vector2(x - spacingX, y - spacingY);
            //Move to location
            potionAbove.MoveToTarget(targetPos);
            //Update Coordinates
            potionAbove.SetCoordinates(x, y);
            //Update Board
            board[x, y] = board[x, y + yOffset];
            board[x, y + yOffset] = new Node(true, null);

        }
        //if we've hit the top of the board without finding a potion
        if (y + yOffset == height)
        {
            SpawnPotionAtTop(x);
        }
    }

    private void SpawnPotionAtTop(int x)
    {
        //throw new System.NotImplementedException();
    }

    private MatchResult IsConnected(Potion potion)
    {
        List<Potion> connectedPotions = new List<Potion>();
        PotionType potionType = potion.potionType;

        connectedPotions.Add(potion);

        //Checl Right
        CheckDirection(potion, new Vector2Int(1, 0), connectedPotions);
        //Checl Left
        CheckDirection(potion, new Vector2Int(-1, 0), connectedPotions);

        if (connectedPotions.Count == 3)
        {
            Debug.Log($"Normal Horizontal Match ({potionType})");

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Horizontal
            };
        }
        else if (connectedPotions.Count > 3)
        {
            Debug.Log($"Long Horizontal Match ({potionType})");

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongHorizontal
            };
        }

        connectedPotions.Clear();
        connectedPotions.Add(potion);

        //Checl Up
        CheckDirection(potion, new Vector2Int(0, 1), connectedPotions);
        //Checl Down
        CheckDirection(potion, new Vector2Int(0, -1), connectedPotions);

        if (connectedPotions.Count == 3)
        {
            Debug.Log($"Normal Vertical Match ({potionType})");

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Vertical
            };
        }
        else if (connectedPotions.Count > 3)
        {
            Debug.Log($"Long Vertical Match ({potionType})");

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongVertical
            };
        }
        else
        {
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.None
            };
        }
    }
    private void CheckDirection(Potion potion, Vector2Int direction, List<Potion> connectedPotions)
    {
        PotionType potionType = potion.potionType;
        int x = potion.xCoordinate + direction.x;
        int y = potion.yCoordinate + direction.y;

        //Check that wa're within the boundaries of the board
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (board[x, y].isUsable)
            {
                Potion neighbourPotion = board[x, y].potionGameObject.GetComponent<Potion>();

                if (!neighbourPotion.isMatched && neighbourPotion.potionType == potionType)
                {
                    connectedPotions.Add(neighbourPotion);

                    x += direction.x;
                    y += direction.y;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

    }
    private MatchResult SuperMatch(MatchResult matchedResult)
    {
        //if wa've Horizontal or Long Horizontal Match
        if (matchedResult.direction == MatchDirection.Horizontal || matchedResult.direction == MatchDirection.LongHorizontal)
        {
            //for each potion
            foreach (Potion potion in matchedResult.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new List<Potion>();

                //Check Up
                CheckDirection(potion, new Vector2Int(0, 1), extraConnectedPotions);

                //Check Down
                CheckDirection(potion, new Vector2Int(0, -1), extraConnectedPotions);

                //do we've 2 or more potions that have been matched against this current potion
                if (extraConnectedPotions.Count >= 2)
                {
                    Debug.Log($"Super Horizontal Match ({potion.potionType})");

                    extraConnectedPotions.AddRange(matchedResult.connectedPotions);

                    //return our super match
                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super
                    };
                }
            }
            //we didn't have a super match, so return our normal match
            return new MatchResult
            {
                connectedPotions = matchedResult.connectedPotions,
                direction = matchedResult.direction,
            };
        }
        //if wa've Vertical or Long Vertical Match
        else if (matchedResult.direction == MatchDirection.Vertical || matchedResult.direction == MatchDirection.LongVertical)
        {
            foreach (Potion potion in matchedResult.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new List<Potion>();

                //Check Up
                CheckDirection(potion, new Vector2Int(1, 0), extraConnectedPotions);

                //Check Down
                CheckDirection(potion, new Vector2Int(-1, 0), extraConnectedPotions);

                //do we've 2 or more potions that have been matched against this current potion
                if (extraConnectedPotions.Count >= 2)
                {
                    Debug.Log($"Super Vertical Match ({potion.potionType})");

                    extraConnectedPotions.AddRange(matchedResult.connectedPotions);

                    //return our super match
                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super
                    };
                }
            }
            //we didn't have a super match, so return our normal match
            return new MatchResult
            {
                connectedPotions = matchedResult.connectedPotions,
                direction = matchedResult.direction,
            };
        }

        return null;
    }


    #endregion

    #region Swaping Potions

    public void SelectPotion(Potion potion)
    {
        if (selectedPotion == null)
        {
            selectedPotion = potion;
        }
        else if (selectedPotion == potion)
        {
            selectedPotion = null;
        }
        else if (selectedPotion != potion)
        {
            SwapPotion(selectedPotion, potion);
            selectedPotion = null;
        }
    }

    private void SwapPotion(Potion potionA, Potion potionB)
    {
        if (!IsAdjacent(potionA, potionB)) return;

        DoSwap(potionA, potionB);

        isProcessingMove = true;

        StartCoroutine(ProcessMatches(potionA, potionB));
    }

    private bool IsAdjacent(Potion potionA, Potion potionB)
    {
        return Mathf.Abs(potionA.xCoordinate - potionB.xCoordinate) + Mathf.Abs(potionA.yCoordinate - potionB.yCoordinate) <= 1;
    }

    private void DoSwap(Potion potionA, Potion potionB)
    {
        GameObject tempGameObject = board[potionA.xCoordinate, potionA.yCoordinate].potionGameObject;

        board[potionA.xCoordinate, potionA.yCoordinate].potionGameObject = board[potionB.xCoordinate, potionB.yCoordinate].potionGameObject;

        board[potionB.xCoordinate, potionB.yCoordinate].potionGameObject = tempGameObject;

        int tempXCoordinate = potionA.xCoordinate;
        int tempYCoordinate = potionA.yCoordinate;

        potionA.xCoordinate = potionB.xCoordinate;
        potionA.yCoordinate = potionB.yCoordinate;

        potionB.xCoordinate = tempXCoordinate;
        potionB.yCoordinate = tempYCoordinate;

        potionA.MoveToTarget(board[potionB.xCoordinate, potionB.yCoordinate].potionGameObject.transform.position);
        potionB.MoveToTarget(board[potionA.xCoordinate, potionA.yCoordinate].potionGameObject.transform.position);
    }

    private IEnumerator ProcessMatches(Potion potionA, Potion potionB)
    {
        yield return new WaitForSeconds(0.2f);

        if (!CheckBoard(true))
        {
            DoSwap(potionA, potionB);
        }

        isProcessingMove = false;
    }

    #endregion

    #region Cascading Potions 

    #endregion
}

public class MatchResult
{
    public List<Potion> connectedPotions;
    public MatchDirection direction;
}
public enum MatchDirection
{
    Vertical,
    Horizontal,
    LongVertical,
    LongHorizontal,
    Super,
    None
}
