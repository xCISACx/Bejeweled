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
    
    private Gem[,] Gems;
    
    public GameObject[] GemPrefabs;

    public int MinimumMatches = 3;
    
    public Gem CurrentGem;
    public Gem TargetGem;

    public List<Gem> GemsToMark = new List<Gem>();
    
    // Start is called before the first frame update
    void Start()
    {
        Generate();
        GemsToMark.Clear();
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
                        CurrentGem = null;
                        TargetGem = null;
                    }
                    else
                    {
                        SwapGems(CurrentGem, TargetGem);
                        
                        if (CheckForMatch(CurrentGem, true) || CheckForMatch(TargetGem, true))
                        {
                            ResetSelectedGems();
                            Debug.Log("matched");
                        }
                        else
                        {
                            StartCoroutine(SwapGemsAfterDelay(TargetGem, CurrentGem, 0.5f));
                            Debug.Log("illegal move, swapping back");
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
    
    IEnumerator SwapGemsAfterDelay(Gem gemA, Gem gemB, float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);
        
        gemA.IsMatched = false;
        gemB.IsMatched = false;

        SwapGems(gemB, gemA);
        ResetSelectedGems();
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
                    newGem.name = "(" + i + ", " + j + ") " + newGem.name;
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
    }
    
    private void DestroyMatchedGems()
    {
        foreach (var gem in GemsToMark)
        {
            gem.IsMatched = false;
            Gems[(int) gem.Coordinates.x, (int) gem.Coordinates.y] = null;
            Destroy(gem.gameObject);
        }

        GemsToMark.Clear();
        HandleFallingGems();
    }

    private int CountAdjacentGems(Gem gem, Vector2 dir)
    {
        int count = 0;

        var currentCoords = gem.Coordinates + dir;

        // keep checking the given direction as long as the type matches the initial gem's type
        while (IsInsideBoard(currentCoords) && gem.Type == Gems[(int) currentCoords.x, (int) currentCoords.y].Type)
        {
            GemsToMark.Add(Gems[(int)currentCoords.x, (int)currentCoords.y]);
            count++;
            currentCoords += dir;
        }

        return count;
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
                        Gems[i, j] = Gems[i, k];
                        Gems[i, j].Coordinates = new Vector2(i, j);
                        Gems[i, k] = null;
                    }
                    
                    // Update the gem positions
                    Vector2 newPosition = new Vector2(i * Offset, j * Offset);
                    if (Gems[i, j])
                    {
                        Gems[i, j].transform.position = newPosition;
                    }
                    
                }
            }
        }
    }
    
    bool IsInsideBoard(Vector2 coords)
    {
        return coords.x >= 0 && coords.x < Gems.GetLength(0) && coords.y >= 0 && coords.y < Gems.GetLength(1);
    }
}
