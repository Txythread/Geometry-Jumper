using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class LevelObject : MonoBehaviour
{
    [SerializeField] public GameLayer layer;
    [SerializeField] private Vector2 levelPosition;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position -= Vector3.right * (Time.deltaTime * 3);
    }
}

public enum GameLayer
{
    Background,
    Ground,
    Spike,
}
