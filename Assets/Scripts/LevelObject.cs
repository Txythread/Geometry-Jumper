using System;
using System.Collections;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public  class LevelObject : MonoBehaviour
{
    [SerializeField] public GameLayer layer;
    [SerializeField] private Vector2 levelPosition;
    [SerializeField] private string textureName;
    [SerializeField] private bool reloadTexture;
    
    private bool initialized;

    protected virtual void Awake() { }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        if (reloadTexture) StartCoroutine(LateStart());
        else initialized = true;
        levelPosition = (Vector2)transform.position;
    }

    public IEnumerator LateStart()
    {
        yield return null; // Wait one frame
        
        GetComponent<SpriteRenderer>().sprite = GameManager.GetTexture(textureName);
        /* + Vector2.right * GameManager.gameCenterX*/;
        
        initialized = true;
    }


    

    // Update is called once per frame
    private void Update()
    {
        if (!initialized) return;
        
        transform.position = new Vector3(levelPosition.x - GameManager.gameCenterX, levelPosition.y, 0);
        
        EndUpdate();
    }
    
    protected virtual void EndUpdate() {}
}

public enum GameObjectType: byte
{
    Spike,
}

public enum GameLayer
{
    Background,
    Ground,
    Spike,
}
