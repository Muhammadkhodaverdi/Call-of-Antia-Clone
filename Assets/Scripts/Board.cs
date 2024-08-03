using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    public event EventHandler<OnPotionParticlesSpwanEventArgs> OnPotionParticlesSpwan;
    public class OnPotionParticlesSpwanEventArgs : EventArgs
    {
        public float damage;
    }

    [Header("Refrences")]

    [SerializeField] private List<Node> nodeList;
    [SerializeField] private List<Transform> potionParticleSpawnPointList;
    [SerializeField] private PotionListSO potionListSO;
    [SerializeField] private CanvasGroup lockScreenCanvasGroup;
    [SerializeField] private Transform uiRootGameObject;


    [Header("Attributes")]
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private bool isProcessingMove;
    [SerializeField] List<Potion> potionsToRemove = new List<Potion>();
    [SerializeField] private float normalMatchDamagePower = 25f;
    [SerializeField] private float longMatchDamagePower = 50f;
    [SerializeField] private float superMatchDamagePower = 75f;


    private Node[,] board;
    private Potion selectedPotion;

    bool enable = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Enable();

        Init();

        Potion.OnAnyPotionClick += ((potion) =>
        {
            if (!enable) return;

            if (isProcessingMove) return;

            SelectPotion(board[potion.x, potion.y].potionGameObject.GetComponent<Potion>());
        });

        Skeleton.Instance.OnStateChanged.AddListener((args) =>
        {
            switch (args.state)
            {
                case Skeleton.State.Idle:
                    Enable();
                    break;
                case Skeleton.State.Attack:
                    Disable();
                    break;
                case Skeleton.State.Die:
                    Disable();
                    break;
                default:
                    break;
            }
        });
    }

    #region Initializing Board
    private void Init()
    {
        ClearBoard();


        board = new Node[width, height];

        int i = 0;
        for (int y = 0; y < height; y++)
        {

            for (int x = 0; x < width; x++)
            {
                nodeList[i].Init(true, null);
                nodeList[i].SetCoordinates(x, y);
                board[x, y] = nodeList[i];
                i++;
            }

        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject potionGameObject = Instantiate(potionListSO.GetRandomPotionPrefab(), Vector3.zero, Quaternion.identity);

                potionGameObject.transform.SetParent(board[x, y].potionStandPos, false);

                potionGameObject.transform.position = board[x, y].potionStandPos.position;

                potionGameObject.GetComponent<Potion>().SetCoordinates(x, y);

                board[x, y].Init(true, potionGameObject);
            }

        }
        if (CheckBoard(false))
        {
            Init();
        }
    }

    private void ClearBoard()
    {
        foreach (Node node in nodeList)
        {
            node.Clear();
        }
    }

    #endregion

    #region Checking Board
    public bool CheckBoard(bool spawnParticle)
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

                            if (spawnParticle)
                                SpawnPotionParticle(potion.potionType, superMatchedPotions.direction);

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

        //potionA.transform.SetParent(board[potionB.xCoordinate, potionB.yCoordinate].potionStandPos);
        potionA.MoveToTarget(board[potionB.xCoordinate, potionB.yCoordinate].potionStandPos);

        //potionB.transform.SetParent(board[potionA.xCoordinate, potionA.yCoordinate].potionStandPos);
        potionB.MoveToTarget(board[potionA.xCoordinate, potionA.yCoordinate].potionStandPos);

        int tempXCoordinate = potionA.xCoordinate;
        int tempYCoordinate = potionA.yCoordinate;

        potionA.SetCoordinates(potionB.xCoordinate, potionB.yCoordinate);
        potionB.SetCoordinates(tempXCoordinate, tempYCoordinate);
    }

    private IEnumerator ProcessMatches(Potion potionA, Potion potionB)
    {
        yield return new WaitForSeconds(0.2f);

        if (CheckBoard(true))
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

        if (CheckBoard(true))
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
        //y offset
        int yOffset = 1;

        //while the cell above our current cell is null and we're below the height of the board
        while (y + yOffset < height && board[x, y + yOffset].potionGameObject == null)
        {
            //increment y offset
            yOffset++;
        }

        //we've either hit the top of the board or we found a potion

        if (y + yOffset < height && board[x, y + yOffset].potionGameObject != null)
        {
            //we've found a potion

            Potion potionAbove = board[x, y + yOffset].potionGameObject.GetComponent<Potion>();

            //Move it to the correct location
            //Vector3 targetPos = board[x, y].potionStandPos.position;
            //Move to location
            // potionAbove.transform.SetParent(board[x, y].potionStandPos, false);
            potionAbove.MoveToTarget(board[x, y].potionStandPos);
            //update incidices
            potionAbove.SetCoordinates(x, y);
            //update our potionBoard
            board[x, y].Init(true, board[x, y + yOffset].potionGameObject);
            //set the location the potion came from to null
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
        int index = FindIndexOfLowestNull(x);
        int locationToMoveTo = (height) - index;
        //get a random potion
        GameObject newPotion = Instantiate(potionListSO.GetRandomPotionPrefab(), board[x, height - 1].potionStandPos.position, Quaternion.identity,uiRootGameObject);
        newPotion.transform.SetParent(null);
        //set Coordinates
        newPotion.GetComponent<Potion>().SetCoordinates(x, index);
        //set it on the potion board
        board[x, index].Init(true, newPotion);
        //move it to that location
        //Vector3 targetPosition = board[x, index].potionStandPos.position;
        newPotion.GetComponent<Potion>().MoveToTarget(board[x, index].potionStandPos);
    }

    private int FindIndexOfLowestNull(int x)
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

    private void SpawnPotionParticle(PotionType potionType, MatchDirection matchDirection)
    {
        if (matchDirection == MatchDirection.Horizontal || matchDirection == MatchDirection.Vertical)
        {
            Instantiate(potionListSO.GetPotionParticle(potionType), potionParticleSpawnPointList[1].position, Quaternion.identity);

            OnPotionParticlesSpwan?.Invoke(this, new OnPotionParticlesSpwanEventArgs { damage = normalMatchDamagePower });
        }
        else if (matchDirection == MatchDirection.LongHorizontal || matchDirection == MatchDirection.LongVertical)
        {
            Instantiate(potionListSO.GetPotionParticle(potionType), potionParticleSpawnPointList[0].position, Quaternion.identity);
            Instantiate(potionListSO.GetPotionParticle(potionType), potionParticleSpawnPointList[2].position, Quaternion.identity);

            OnPotionParticlesSpwan?.Invoke(this, new OnPotionParticlesSpwanEventArgs { damage = longMatchDamagePower });
        }
        else if (matchDirection == MatchDirection.Super)
        {
            for (int i = 0; i < potionParticleSpawnPointList.Count; i++)
            {
                Instantiate(potionListSO.GetPotionParticle(potionType), potionParticleSpawnPointList[i].position, Quaternion.identity);
            }

            OnPotionParticlesSpwan?.Invoke(this, new OnPotionParticlesSpwanEventArgs { damage = superMatchDamagePower });
        }
    }


    private void Enable()
    {
        enable = true;
        float delay = 0.1f;
        StartCoroutine(Fade(lockScreenCanvasGroup, 0, delay));
    }
    private void Disable()
    {
        enable = false;
        float delay = 0.2f;
        StartCoroutine(Fade(lockScreenCanvasGroup, 1, delay));
    }
    private IEnumerator Fade(CanvasGroup canvasGroup, float to, float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float duration = 0.5f;
        float from = canvasGroup.alpha;
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;

            yield return null;
        }

        canvasGroup.alpha = to;
    }
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
