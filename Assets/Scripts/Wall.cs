


using UnityEngine;
using UnityEngine.Serialization;

public class Wall : LevelObject
{
    /// <summary>
    /// Selects which bounds can be seen.
    /// Bitmap:  
    ///     1. (0, LSb): Top
    ///     2. (1): Left
    ///     3. (2): Right
    ///     4. (3): Bottom
    ///     5. ...
    ///     6. (MSb)
    /// </summary>
    [SerializeField] private byte wallBitmap;
    
    [FormerlySerializedAs("topWall")] [SerializeField] private GameObject topBound;
    [SerializeField] private GameObject leftBound;
    [SerializeField] private GameObject rightBound;
    [SerializeField] private GameObject bottomBound;

    protected override void Start()
    {
        base.Start();

        if ((wallBitmap & 0x1) == 0)
        {
            Destroy(topBound);
        }
        
        if ((wallBitmap & 0x2) == 0)
        {
            Destroy(leftBound);
        }
        
        if ((wallBitmap & 0x4) == 0)
        {
            Destroy(rightBound);
        }
        
        if ((wallBitmap & 0x8) == 0)
        {
            Destroy(bottomBound);
        }
    }
}