using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gem : MonoBehaviour
{
    public enum GemType
    {
        Green,
        Purple,
        Red,
        Orange,
        Yellow,
        Blue
        
    };
    public GemType Type;
    public Vector2 Coordinates;
    public Vector2 TargetCoordinates;
    public bool IsMatched;
    public string DefaultName;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsMatched)
        {
            GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else
        {
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    public void UpdateEditorName()
    {
        gameObject.name = "(" + Coordinates.x + ", " + Coordinates.y + ") " + DefaultName;
    }
}
