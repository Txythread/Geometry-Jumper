using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;


using UnityEngine;
using UnityEngine.Serialization;

public static class ColorExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}


public class Orb : LevelObject, IInteractable
{
    /// <summary>
    /// How a growth/shrinking stage takes
    /// </summary>
    private const float InversionTime = 0.25f;

    private const float GrowthMultiplier = 1.5f;
    
    [FormerlySerializedAs("mainOrb")] [SerializeField] private GameObject mainBody;
    [SerializeField] private GameObject surroundingCircle;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private ParticleSystem secondaryParticles;

    [SerializeField] private Color color;
    /// <summary>
    /// The interaction type used when the player interacted with the
    /// orb "willingly", meaning they pressed a button.
    /// </summary>
    [FormerlySerializedAs("interactionType")] [SerializeField] private InteractionType primaryInteractionType;
    /// <summary>
    /// The interaction type used whenever a player touches the orb.
    /// </summary>
    [SerializeField] private InteractionType secondaryInteractionType;
    [SerializeField] private float interactionVisualDelayTime;
    
    private float _currentStageTimeCounter;
    private bool _isGrowing = true;
    private float _originalSurrounderSize;
    private float _lastInteractionTime = -1f;

    protected override void Awake()
    {
        if (mainBody != null) mainBody.GetComponent<SpriteRenderer>().color = color.WithAlpha(1f);
        if (surroundingCircle != null)
        {
            surroundingCircle.GetComponent<SpriteRenderer>().color = color.WithAlpha(0.5f);
            _originalSurrounderSize = surroundingCircle.transform.localScale.x;
        }

        if (particles != null)
        {
            var main = particles.main;
            main.startColor = color.WithAlpha(1f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(color, Color.white.WithAlpha(0.2f));
        }

        if (secondaryParticles != null)
        {
            var main = secondaryParticles.main;
            main.startColor = color.WithAlpha(1f);

            var colorOverLifetime = secondaryParticles.colorOverLifetime;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(color, Color.white.WithAlpha(0.2f));
        }
    }

    public void Interact(Player player, bool actionPresent)
    {
        
        if (actionPresent)
        {
            //if (_lastInteractionTime != -1) return;
            Debug.Log("Interaction with orb");
            _lastInteractionTime = interactionVisualDelayTime;
            mainBody.GetComponent<SpriteRenderer>().color = Color.white;
            ProcessInteractionType(primaryInteractionType, player);
        }
        else
        {
            ProcessInteractionType(secondaryInteractionType, player);
        }
        
    }

    static void ProcessInteractionType(InteractionType interactionType, Player player)
    {
        switch (interactionType)
        {
            case InteractionType.None: 
                Debug.LogWarning("Orb with null interaction type");
                break;
            case InteractionType.JumpLow:
                player.Jump(0.5f);
                break;
            case InteractionType.JumpMedium:
                player.Jump();
                break;
            case InteractionType.JumpHigh:
                Debug.Log("Orb jumping high");
                player.Jump(4f);
                break;
            case InteractionType.Gravity:
                player.ReverseGravity();
                player.ReverseVelocity();
                break;
            case InteractionType.ExplicitNone:
                break;
            case InteractionType.GravityJump:
                player.ZeroVelocity();
                player.ReverseGravity();
                player.Jump();
                break;
        }
    }

    protected override void EndUpdate()
    {

        if (surroundingCircle == null) return;
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
    None, ExplicitNone, JumpLow, JumpMedium, JumpHigh, Gravity, GravityJump
}