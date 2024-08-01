using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [Header("Refrences")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private GameObject rowGameObject;
    [SerializeField] private GameObject[] potionsPrefab;

    [Header("Attributes")]
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private bool isProcessingMove;
    [SerializeField] List<Potion> potionsToRemove = new List<Potion>();


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


        for (int x = 0; x < width; x++)
        {
            GameObject row = Instantiate(rowGameObject, Vector3.zero, Quaternion.identity, transform);

            for (int y = height - 1; y >= 0; y--)
            {
                GameObject nodeGameObject = Instantiate(nodePrefab, Vector3.zero, Quaternion.identity, row.transform);
                Node node = nodeGameObject.GetComponent<Node>();
                node.Init(true, null);
                board[x, y] = node;
            }

        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int randomIndex = Random.Range(0, potionsPrefab.Length);
                GameObject potionGameObject = Instantiate(potionsPrefab[randomIndex], Vector3.zero, Quaternion.identity);

                potionGameObject.transform.SetParent(board[x, y].potionStandPos, false);

                potionGameObject.transform.position = board[x, y].potionStandPos.position;

                potionGameObject.GetComponent<Potion>().SetCoordinates(x, y);

                board[x, y].Init(true, potionGameObject);
            }

        }
        rowGameObject.SetActive(false);

        if (CheckBoard())
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
    public bool CheckBoard()
    {
        bool hasMatched = false;

        potionsToRemove.Clear();

        foreach (Node node in board)
        {
            if (node.potionGameObject != null)
            {
                node.potionGameObject.GetComponent<Potion>().isMatched = false;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //checking if potion node is usable
                if (board[x, y].isUsable)
                {
                    //then proceed to get potion class in node.
                    Potion potion = board[x, y].potionGameObject.GetComponent<Potion>();

                    //ensure its not matched
                    if (!potion.isMatched)
                    {
                        //run some matching logic

                        MatchResult matchedPotions = IsConnected(potion);

                        if (matchedPotions.connectedPotions.Count >= 3)
                        {
                            MatchResult superMatchedPotions = SuperMatch(matchedPotions);

                            potionsToRemove.AddRange(superMatchedPotions.connectedPotions);

                            foreach (Potion pot in superMatchedPotions.connectedPotions)
                                pot.isMatched = true;

                            hasMatched = true;
                        }
                    }
                }
            }
        }
        return hasMatched;
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

        board[potionA.xCoordinate, potionA.yCoordinate].Init(true, board[potionB.xCoordinate, potionB.yCoordinate].potionGameObject);

        board[potionB.xCoordinate, potionB.yCoordinate].Init(true, tempGameObject);

        potionA.MoveToTarget(board[potionB.xCoordinate, potionB.yCoordinate].potionStandPos);

        potionB.MoveToTarget(board[potionA.xCoordinate, potionA.yCoordinate].potionStandPos);

        int tempXCoordinate = potionA.xCoordinate;
        int tempYCoordinate = potionA.yCoordinate;

        potionA.SetCoordinates(potionB.xCoordinate, potionB.yCoordinate);
        potionB.SetCoordinates(tempXCoordinate, tempYCoordinate);
    }

    private IEnumerator ProcessMatches(Potion potionA, Potion potionB)
    {
        yield return new WaitForSeconds(0.2f);

        if (CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(true));
        }
        else
        {
            DoSwap(potionA, potionB);

        }

        isProcessingMove = false;
    }
    public IEnumerator ProcessTurnOnMatchedBoard(bool _subtractMoves)
    {
        foreach (Potion potionToRemove in potionsToRemove)
        {
            potionToRemove.isMatched = false;
        }

        RemoveAndRefill(potionsToRemove);
        yield return new WaitForSeconds(0.4f);

        if (CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(false));
        }
    }

    #endregion

    #region Cascading Potions 

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
            board[tempX, tempY].Init(true, null);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y].potionGameObject == null)
                {
                    ReFillPotion(x, y);
                }
            }
        }
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
            //Move to location
            potionAbove.MoveToTarget(board[x, y].potionStandPos);
            //Update Coordinates
            potionAbove.SetCoordinates(x, y);
            //Update Board
            board[x, y].Init(true, board[x, y + yOffset].potionGameObject);
            board[x, y + yOffset].Init(true, null);

        }
        //if we've hit the top of the board without finding a potion
        if (y + yOffset == height)
        {
            SpawnPotionAtTop(x);
        }
    }

    private void SpawnPotionAtTop(int x)
    {
        int yIndex = FindIndexOfLowesNull(x);
        int locationToMove = height - yIndex;
        //Get a Random potion
        int randomIndex = Random.Range(0, potionsPrefab.Length);
        GameObject potionGameObject = Instantiate(potionsPrefab[randomIndex], board[x, height - 1].potionStandPos.position + new Vector3(0, 108f), Quaternion.identity, board[x, yIndex].potionStandPos);
        //Set Coordinates
        potionGameObject.GetComponent<Potion>().SetCoordinates(x, yIndex);
        //Set it on the board
        board[x, yIndex].Init(true, potionGameObject);
        //Move it to that location
        potionGameObject.GetComponent<Potion>().MoveToTarget(board[x, yIndex].potionStandPos);
    }

    private int FindIndexOfLowesNull(int x)
    {
        int lowestNull = 99;
        for (int y = height - 1; y >= 0; y--)
        {
            if (board[x, y].potionGameObject == null)
            {
                lowestNull = y;
            }
        }
        return lowestNull;
    }

    #endregion

    #region Matching Logic

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
