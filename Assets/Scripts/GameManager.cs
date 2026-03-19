using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;


public class GameManager : MonoBehaviour
{
    public static float gameCenterX;
    public static string texturePath;
    private static int GameManagerCount = 0;

    [SerializeField] private string textureDir;
    [SerializeField] private float gameSpeed;
    [SerializeField] private LevelObject spike;
    [SerializeField] private Material wallMaterial;


    public static Sprite GetTexture(string textureName)
    {
        var path = texturePath + textureName;
        return Resources.Load<Sprite>(path);
    }
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (GameManagerCount++ != 0)
        {
            Debug.LogError("Too many game managers");
        }
        
        textureDir = textureDir.Replace("$ASSETS_PATH", Application.dataPath);
        texturePath = textureDir;
        
        wallMaterial.SetColor("_CenterColor", Color.blue);
    }

    private void OnDestroy()
    {
        GameManagerCount--;
    }

    // Update is called once per frame
    private void Update()
    {
        GameManager.gameCenterX += gameSpeed * Time.deltaTime;
    }
    
    
    
    private void ProcessCommand(byte[] command, ref int index)
    {
        switch ((char)command[index])
        {
            case 's':
                Vector2 position = MemoryMarshal.Cast<byte, float>(command[(index + 1)..(index + 9)]).ToArray() switch
                {
                    var arr when arr.Length >= 2 => new Vector2(arr[0], arr[1]),
                    _ => throw new InvalidOperationException("Bytes missing")
                };
                half rotation = MemoryMarshal.Cast<byte, half>(command[(index + 1)..(index + 9)]).ToArray() switch
                {
                    var arr when arr.Length >= 2 => arr[0],
                    _ => throw new InvalidOperationException("Bytes missing")
                };
                index += 16;
                
                var newSpike = Instantiate(spike);
                newSpike.transform.position = position;
                break;
            default:
                Debug.LogError("Invalid command");
                break;
        }
    }
}
