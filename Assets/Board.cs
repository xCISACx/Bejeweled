using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngineInternal;

public class Board : MonoBehaviour
{
    public int Width = 8;
    public int Height = 8;
    public float Offset = 1.5f;
    public float GemFallDuration = 0.5f;
    
    private Gem[,] Gems;
    
    public GameObject[] GemPrefabs;

    public int MinimumMatches = 3;
    
    public Gem CurrentGem;
    public Gem TargetGem;
    private Coroutine swapCoroutine;
    private bool isSwapCoroutineRunning = false;

    public List<Gem> GemsToMark = new List<Gem>();
    //public List<Gem> VerticalMatches = new List<Gem>();
    //public List<Gem> HorizontalMatches = new List<Gem>();
    
    public GemArrayWrapper GemArrayWrapper;
    
    public void InitializeGemsArray()
    {
        GemArrayWrapper = new GemArrayWrapper(Width, Height);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        InitializeGemsArray();
        Generate();
        UpdateGemArray();
        GemsToMark.Clear();
        
        if (Gems != null)
        {
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (Gems[i, j] != null)
                    {
                        Debug.Log($"Gem at ({i}, {j}): {Gems[i, j].Type}");
                    }
                }
            }
        }
        else
        {
            Debug.Log("gems array is null");
        }
    }
    
    private void UpdateGemArray()
    {
        // Ensure the array is initialized
        if (GemArrayWrapper.Gems == null)
        {
            GemArrayWrapper.Gems = new Gem[Width, Height];
        }
        
        GemArrayWrapper.Gems = Gems;
        Debug.Log("updated gem array");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Cast a ray from the camera to the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            // Check if the ray hits a gem
            if (hit.collider != null && hit.collider.CompareTag("Gem"))
            {
                // If it does and we don't already have a CurrentGem...
                if (!CurrentGem)
                {
                    // Set CurrentGem to the hit gem
                    CurrentGem = hit.transform.GetComponent<Gem>();
                }
                // If we have a CurrentGem, set the TargetGem instead
                else
                {
                    TargetGem = hit.transform.GetComponent<Gem>();

                    if (!AreGemsAdjacent(CurrentGem, TargetGem))
                    {
                        ResetSelectedGems();
                    }
                    else
                    {
                        if (!isSwapCoroutineRunning)  // Check if the coroutine is not already running
                        {
                            StartCoroutine(SwapGemsAfterDelay(TargetGem, CurrentGem, 0.0f));
                            isSwapCoroutineRunning = true;  // Set the flag to indicate that the coroutine is running
                        }
                    }
                }
            }
            // If it doesn't, reset the CurrentGem
            else
            {
                CurrentGem = null;
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            CurrentGem = null;
        }
    }

    private void ResetSelectedGems()
    {
        CurrentGem = null;
        TargetGem = null;
    }

    private void Generate()
    {
        Gems = new Gem[Width, Height];
        
        do
        {
            // Generate gems on the board
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    var position = new Vector2(i * Offset, j * Offset);
                    
                    // Check if there's an existing gem at this position and destroy it
                    if (Gems[i, j] != null)
                    {
                        Destroy(Gems[i, j].gameObject);
                    }
                    
                    var randomIndex = Random.Range(0, GemPrefabs.Length);
                    var newGem = Instantiate(GemPrefabs[randomIndex], position, Quaternion.identity);
                    newGem.GetComponent<Gem>().Coordinates = new Vector2(i, j);
                    newGem.transform.parent = transform;
                    newGem.GetComponent<Gem>().DefaultName = newGem.name;
                    newGem.GetComponent<Gem>().UpdateEditorName();
                    Gems[i, j] = newGem.GetComponent<Gem>();
                }
            }
        }
        while (DoesBoardHaveMatches());
    }

    private bool DoesBoardHaveMatches()
    {
        // Check for matches on the entire board
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                if (CheckForMatch(Gems[i, j], false))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private bool CheckForMatch(Gem gem, bool mark)
    {
        GemsToMark.Clear();
        
        // Check for a straight-line match with a minimum of 3 adjacent gems
        int horizontalMatches = 1 + CountAdjacentGems(gem, Vector2.right) + CountAdjacentGems(gem, Vector2.left);
        int verticalMatches = 1 + CountAdjacentGems(gem, Vector2.down) + CountAdjacentGems(gem, Vector2.up);

        bool isMatch = (horizontalMatches >= MinimumMatches || verticalMatches >= MinimumMatches);

        Debug.Log((horizontalMatches >= MinimumMatches) + " | " + (verticalMatches >= MinimumMatches));
        
        // If there is a match, mark all the gems
        if (isMatch && mark)
        {
            MarkGemsInSequence(GemsToMark);

            DestroyMatchedGems();
        }

        return isMatch;
    }
    
    private int CountAdjacentGems(Gem gem, Vector2 dir)
    {
        int count = 0;
        
        if (gem == null)
        {
            return 0;
        }

        var currentCoords = gem.Coordinates + dir;

        if (!GemsToMark.Contains(gem))
        {
            GemsToMark.Add(gem);
        }

        if (IsInsideBoard(gem.Coordinates) && IsInsideBoard(currentCoords))
        {
            Gem adjacentGem = Gems[(int)currentCoords.x, (int)currentCoords.y];

            // Check if the adjacent gem is not null and has the same type
            while (adjacentGem != null && gem.Type == adjacentGem.Type)
            {
                GemsToMark.Add(adjacentGem);
                count++;

                // Move to the next adjacent gem
                currentCoords += dir;

                // Check if the new coordinates are inside the board
                if (IsInsideBoard(currentCoords))
                {
                    adjacentGem = Gems[(int)currentCoords.x, (int)currentCoords.y];
                }
                else
                {
                    adjacentGem = null;
                }
            }
        }

        return count;
    }
    
    private void MarkGemsInSequence(List<Gem> gemSequence)
    {
        int sequenceLength = gemSequence.Count;

        if (sequenceLength >= MinimumMatches)
        {
            foreach (var gemToMark in gemSequence)
            {
                gemToMark.IsMatched = true;
            }
        }
        
        //MarkGemsInList(VerticalMatches);
        //MarkGemsInList(HorizontalMatches);

        //VerticalMatches.Clear();
        //HorizontalMatches.Clear();
    }

    private void MarkGemsInList(List<Gem> gemList)
    {
        int sequenceLength = gemList.Count;

        if (sequenceLength >= MinimumMatches)
        {
            foreach (var gemToMark in gemList)
            {
                gemToMark.IsMatched = true;
            }
        }
    }
    
    /*private void MarkGemsInSequence(List<Gem> gemSequence)
    {
        int sequenceLength = gemSequence.Count;

        if (sequenceLength >= MinimumMatches)
        {
            foreach (var gemToMark in gemSequence)
            {
                gemToMark.IsMatched = true;
            }
        }
    }*/
    
    private void DestroyMatchedGems()
    {
        int comboCount = 0;
        
        foreach (var gem in GemsToMark)
        {
            gem.IsMatched = false;
            Gems[(int) gem.Coordinates.x, (int) gem.Coordinates.y] = null;
            Destroy(gem.gameObject);
            UpdateGemArray();
            
            comboCount++;
        }

        GemsToMark.Clear();
        
        // Check if a combo was detected
        if (comboCount > 1)
        {
            Debug.Log("Combo x" + comboCount);
        }
        
        HandleFallingGems();
        UpdateGemArray();
    }

    private bool AreGemsAdjacent(Gem GemA , Gem GemB)
    {
        Vector2 GemACoordinates = GemA.Coordinates;
        Vector2 GemBCoordinates = GemB.Coordinates;

        bool adjacent = (Mathf.Abs(GemACoordinates.x - GemBCoordinates.x) == 1 && GemACoordinates.y == GemBCoordinates.y) ||
                        (Mathf.Abs(GemACoordinates.y - GemBCoordinates.y) == 1 && GemACoordinates.x == GemBCoordinates.x);

        var debugText = GemA.name + " and " + GemB.name + " are ";
        
        if (!adjacent) debugText += "NOT ";
        
        debugText += "adjacent";
        
        Debug.Log(debugText);
        
        return adjacent;
    }

    private void SwapGems(Gem GemA, Gem GemB)
    {
        Vector2 tempCoordinates = GemA.Coordinates;
        GemA.Coordinates = GemB.Coordinates;
        GemB.Coordinates = tempCoordinates;

        Vector2 tempPosition = GemA.transform.position;
        GemA.transform.position = GemB.transform.position;
        GemB.transform.position = tempPosition;
        
        Gems[(int)GemA.Coordinates.x, (int)GemA.Coordinates.y] = GemA;
        Gems[(int)GemB.Coordinates.x, (int)GemB.Coordinates.y] = GemB;
        
        GemA.GetComponent<Gem>().UpdateEditorName();
        GemB.GetComponent<Gem>().UpdateEditorName();
        
        if (CheckForMatch(CurrentGem, true) || CheckForMatch(TargetGem, true))
        {
            ResetSelectedGems();
            Debug.Log("matched");
        }
        else
        {
            swapCoroutine = StartCoroutine(SwapGemsAfterDelay(TargetGem, CurrentGem, 0.5f));
            Debug.Log("illegal move, swapping back");
        }
        
        UpdateGemArray();
    }
    
    IEnumerator SwapGemsAfterDelay(Gem gemA, Gem gemB, float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);

        gemA.IsMatched = false;
        gemB.IsMatched = false;

        SwapGems(gemB, gemA);

        // Reset the coroutine reference and flag
        swapCoroutine = null;
        isSwapCoroutineRunning = false;
        
        ResetSelectedGems();
    }
    
    // Handle falling gems after matched gems are destroyed
    private void HandleFallingGems()
    {
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height - 1; j++)
            {
                if (Gems[i, j] == null)
                {
                    // Find the first gem above the empty slot
                    int k = j + 1;
                    while (k < Height && Gems[i, k] == null)
                    {
                        k++;
                    }

                    // If a gem is found, move it down to the empty slot
                    if (k < Height && Gems[i, k] != null)
                    {
                        StartCoroutine(MoveGemDown(Gems[i, k], j));
                        Gems[i, k].Coordinates = new Vector2(i, j);
                        Gems[i, j] = Gems[i, k];
                        Gems[i, k] = null;
                    }
                    
                    // Update the gem positions
                    Vector2 newPosition = new Vector2(i * Offset, j * Offset);
                    if (Gems[i, j])
                    {
                        Gems[i, j].transform.position = newPosition;
                    }

                    if (Gems[i, j])
                    {
                        Gems[i, j].GetComponent<Gem>().UpdateEditorName();
                    }
                }
            }
        }

        StartCoroutine(RefillBoardAfterDelay(0.5f));
    }
    
    private IEnumerator MoveGemDown(Gem gem, int targetRow)
    {
        float elapsedTime = 0f;

        Vector2 startPosition = gem.transform.position;
        Vector2 targetPosition = new Vector2(gem.Coordinates.x * Offset, targetRow * Offset);

        while (elapsedTime < GemFallDuration)
        {
            if (gem != null)
            {
                gem.transform.position = Vector2.Lerp(startPosition, targetPosition, elapsedTime / GemFallDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            else
            {
                // Break the coroutine if the gem is null
                yield break;
            }
        }

        if (gem != null)
        {
            gem.transform.position = targetPosition;
            gem.GetComponent<Gem>().UpdateEditorName();
            UpdateGemArray();   
        }
    }

    private void RefillBoard()
    {
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                if (Gems[i, j] == null)
                {
                    Vector2 tempPosition = new Vector2(i * Offset, j * Offset);
                    var randomIndex = Random.Range(0, GemPrefabs.Length);
                    var newGem = Instantiate(GemPrefabs[randomIndex], tempPosition, Quaternion.identity);
                    newGem.GetComponent<Gem>().Coordinates = new Vector2(i, j);
                    newGem.transform.parent = transform;
                    newGem.GetComponent<Gem>().DefaultName = newGem.name;
                    newGem.GetComponent<Gem>().UpdateEditorName();
                    Gems[i, j] = newGem.GetComponent<Gem>();

                }
            }
        }
    }
    
    IEnumerator RefillBoardAfterDelay(float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);
        
        RefillBoard();
        UpdateGemArray();

        GemsToMark.Clear();
        
        // Check for matches on the entire board after refilling
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                CheckForMatch(Gems[i, j], true);
            }
        }
    }

    bool IsInsideBoard(Vector2 coords)
    {
        return coords.x >= 0 && coords.x < Gems.GetLength(0) && coords.y >= 0 && coords.y < Gems.GetLength(1);
    }
}
