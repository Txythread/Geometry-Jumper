using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Orb : LevelObject, Interactable
{
    /// <summary>
    /// How a growth/shrinking stage takes
    /// </summary>
    private const float InversionTime = 0.25f;

    private const float GrowthMultiplier = 1.5f;
    
    [SerializeField] private GameObject mainOrb;
    [SerializeField] private GameObject surroundingCircle;
    [SerializeField] private ParticleSystem particles;

    [SerializeField] private Color color;
    [SerializeField] private InteractionType interactionType;
    
    private float _currentStageTimeCounter;
    private bool _isGrowing = true;
    private float _originalSurrounderSize;

    protected override void Awake()
    {
        mainOrb.GetComponent<SpriteRenderer>().color = color.WithAlpha(1f);
        surroundingCircle.GetComponent<SpriteRenderer>().color = color.WithAlpha(0.5f);

        var main = particles.main;
        main.startColor = color.WithAlpha(1f);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(color, Color.white.WithAlpha(0.2f));
        
        _originalSurrounderSize = surroundingCircle.transform.localScale.x;
    }

    public void Interact(Player player)
    {
        switch (interactionType)
        {
            case InteractionType.None: 
                Debug.LogWarning("Orb with null interaction type");
                break;
            case InteractionType.JumpMedium:
                Debug.Log("Triggering jump on player: " + player);
                player.Jump();
                break;
        }
    }

    protected override void EndUpdate()
    {
        _currentStageTimeCounter += Time.deltaTime;
        if (_currentStageTimeCounter > InversionTime)
        {
            _currentStageTimeCounter = 0;
            _isGrowing = !_isGrowing;

            if (_isGrowing)
            {
                surroundingCircle.gameObject.transform.localScale = new Vector3(_originalSurrounderSize, _originalSurrounderSize, 1);
            }
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

enum InteractionType
{
    None, JumpMedium
}