


using Unity.VisualScripting;
using UnityEngine;

public class Orb : LevelObject
{
    /// <summary>
    /// How a growth/shrinking stage takes
    /// </summary>
    private const float InversionTime = 0.25f;

    private const float GrowthMultiplier = 1f;
    
    [SerializeField] private GameObject mainOrb;
    [SerializeField] private GameObject surroundingCircle;

    [SerializeField] private Color color;

    private float _currentStageTimeCounter;
    private bool _isGrowing;

    protected override void Awake()
    {
        mainOrb.GetComponent<SpriteRenderer>().color = color.WithAlpha(1f);
        surroundingCircle.GetComponent<SpriteRenderer>().color = color.WithAlpha(0.5f);
    }

    protected override void EndUpdate()
    {
        _currentStageTimeCounter += Time.deltaTime;
        if (_currentStageTimeCounter > InversionTime)
        {
            _currentStageTimeCounter = 0;
            _isGrowing = !_isGrowing;
        }

        var growth = GrowthMultiplier * Time.deltaTime;
        if (_isGrowing)
        {
            surroundingCircle.gameObject.transform.localScale += new Vector3(growth, growth, 0f);
        }
        else
        {
            surroundingCircle.gameObject.transform.localScale -= new Vector3(growth, growth, 0f);
        }
        
    }
}