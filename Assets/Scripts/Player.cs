using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework.Constraints;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Player : MonoBehaviour
{
    /// <summary>
    ///  Gravity in m/s.
    /// </summary>
    [FormerlySerializedAs("Gravity")] [SerializeField] private float gravity;
    [SerializeField] private bool grounded;
    [SerializeField] private GameObject center;
    /// <summary>
    /// The current velocity on the jumping axis
    /// </summary>
    [SerializeField] private float velocityY;
    [SerializeField] private InputActionMap gameActionMap;
    /// <summary>
    /// How strong a normal jump is
    /// </summary>
    [SerializeField] private float jumpVelocity;
    /// <summary>
    /// The speed used to rotate the player while in the air
    /// </summary>
    [SerializeField] private float rotationSpeed;
    /// <summary>
    /// The speed used to rotate the player back into normal after touching the ground
    /// </summary>
    [SerializeField] private float rotationSpeedOnGround;

    /// <summary>
    /// The maximum velocity in either direction that the SetVelocity function supports
    /// </summary>
    [SerializeField] private float maximumVelocity;
    
    /// <summary>
    /// How many seconds an input might be early before it doesn't count anymore.
    /// </summary>
    [SerializeField] private float maximumInputDiscrepancy;
    

    private int _frameCount = 0;

    [FormerlySerializedAs("targetRotation")] [SerializeField] private float targetRotationDistance;
    
    private const float PlayerHeight = 1f;
    private bool _lastFrameGrounded;
    /// <summary>
    /// The game blocks jumping for 3 frames to prevent
    /// one input being processed as multiple inputs
    /// </summary>
    private byte _jumpingBlockedFrames;
    private Vector2 _lastFramePosition;
    
    ////////////////////////////////
    ////  Variables as buffers  ////
    //// Better for performance ////
    ////////////////////////////////
    private RaycastHit2D _rayBuffer;
    private Vector3 _bottomPos;
    private Vector3 _topPos;
    private Vector3 _forwardPos;
    private Vector2 _origin;
    private InputAction _jumpAction;
    private string[] _wallSearchMap;

    /// <summary>
    /// How many seconds there are left before the input is invalid.
    /// Special values:
    /// Above 0: The action would still be valid
    /// 0: The action was triggered but the start expired although the action persists
    /// -1: The action start expired and the action already ended
    ///
    /// It might expire due to a multitude of factors:
    /// 1. Timer Expires, self-explanatory
    /// 2. It has been used already
    /// </summary>
    [SerializeField] private float _inputEarlyDelay = -1;
    
    private ControlMode _mode = ControlMode.Normal;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      //  Application.targetFrameRate = 240;
        gameActionMap = InputActionMap.FromJson(System.IO.File.ReadAllText(Application.dataPath + "/InputSystem_Actions.inputactions"))[0];
        gameActionMap.Enable();

        
        gameActionMap.FindAction("Jump").Enable();
        _jumpAction = gameActionMap.FindAction("Jump");

        _wallSearchMap = new [] { "Wall" };
        
        
        
        _bottomPos = transform.position + Vector3.down * (PlayerHeight / 2 + 0.01f);
        _topPos = transform.position + Vector3.up * (PlayerHeight / 2 + 0.01f);

        _lastFramePosition = transform.position;

        //gravity = -gravity;
    }

   


    // Update is called once per frame
    void LateUpdate()
    {
        _frameCount++;
        
        if (_frameCount < 2) return;

        var (actionStart,  actionPersist) = GetInputs();

        SnapToContinuousWallCheckPosition();
        _lastFramePosition = transform.position;
        
        CheckDead();
        
        // Update globals to current values
        _origin = transform.position - (Vector3.left + Vector3.up) * (PlayerHeight / 2 - 0.1f);
        _forwardPos = transform.position + Vector3.right * (PlayerHeight / 2 - 0.1f);
        
        
        HandleGravity();

        var posChangeY = velocityY * Time.deltaTime;

        
        PerformInteractions(actionStart);
        
        // Jump if necessary
        if (CheckGrounded() && actionPersist)
        {
            _inputEarlyDelay = 0;

            if (grounded) Jump();
        }

        var currentRotation = transform.rotation.eulerAngles.z % 90;
        HandleRotationAndLanding(ref currentRotation, ref posChangeY);
        
        _lastFrameGrounded = grounded;
        
        gameObject.transform.position += Vector3.up * posChangeY;

        if (_jumpingBlockedFrames > 0)
        {
            _jumpingBlockedFrames--;
        }
        
        
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_lastFramePosition, new Vector3(PlayerHeight, PlayerHeight));
    }


    /// <summary>
    /// Performs a simple continuous check to get if the player hit the wall
    /// and snaps the player to the position of the wall if said event took
    /// place.
    /// This method requires _lastFramePosition to have been set accordingly
    /// </summary>
    private void SnapToContinuousWallCheckPosition()
    {
        // Skip the check if the jump was currently initiated
        if (_jumpingBlockedFrames > 0) return;
        
        
        // Use a box cast to get the wall
        var origin = _lastFramePosition;
        var size = new Vector2(PlayerHeight, PlayerHeight);
        var angle = transform.rotation.eulerAngles.z;
        var positionChange = (Vector2)transform.position - origin;
        var direction = positionChange.normalized;
        var distance = positionChange.magnitude;
        var layerMask = LayerMask.GetMask(_wallSearchMap);
        Debug.DrawRay(origin, direction, Color.magenta);
        var hit = Physics2D.BoxCast(origin, size, angle, direction, distance, layerMask);

        if (hit.collider == null) return;
        
        // Get the position of the wall
        var wallPos = hit.point;
        
        // Calculate what position that would be for the player
        // We only care about the y position for now
        //Debug.Break();
        var rotationDegrees = transform.rotation.eulerAngles.z % 90;
        var diagonalLength = 1.4142136f;
        var centerToGroundDistance = Mathf.Cos(Mathf.Deg2Rad * (45 - rotationDegrees)) * (diagonalLength / 2);

        if (gravity < 0)
        {
            centerToGroundDistance = -centerToGroundDistance;
        }
        
        var correspondingPlayerY = wallPos.y - centerToGroundDistance;
        var playerChangeY = -transform.position.y + correspondingPlayerY;
        
        // Skip if the player is moving in the direction already
        if (positionChange.y > 0 != playerChangeY > 0)
        {
            return;} 
        
        // Set the player to the calculated position
        transform.position += new Vector3(0, playerChangeY, 0);
        
        //if (centerToGroundDistance < 0.48 | centerToGroundDistance > 0.52) {Debug.Log("Dist: " + centerToGroundDistance);Debug.Break();}
    }

    private void HandleGravity()
    {
        // Update gravity
        switch (_mode)
        {
            case ControlMode.Normal:
                var deltaTime = Time.deltaTime > 0.05f ? 0.05f : Time.deltaTime;
                SetVelocityY(velocityY + gravity * deltaTime);
                break;
        }
    }

    private void HandleRotationAndLanding(ref float currentRotation, ref float posChangeY)
    {
        // Calculate the rotation distance if the player is grounded but not
        // in a position where it can stay (non 90° aligned rotation)
        if (grounded && targetRotationDistance == 0)
        {
            targetRotationDistance = Mathf.Abs((currentRotation % 90));

            if (targetRotationDistance < 5 | targetRotationDistance > 85)
            {
                targetRotationDistance = 0;
            }
            else
            {
                if (targetRotationDistance > 45)
                {
                    targetRotationDistance = 90 - targetRotationDistance;
                }

                if (Mathf.Abs(targetRotationDistance) < 5)
                {
                    targetRotationDistance = 0;
                }
            }

        }

        if (grounded)
        {
            posChangeY = 0;
            velocityY = 0;

            // Make sure to get to the ground exactly
            if (!_lastFrameGrounded)
            {
                //SetPositionOnGround();
            }

            // Handle rotation on ground
            if (targetRotationDistance > 3 | targetRotationDistance < -3)
            {
                var frameNewRotation = rotationSpeedOnGround * Time.deltaTime;
                if (frameNewRotation + currentRotation > Mathf.Abs(targetRotationDistance))
                {
                    transform.Rotate(Vector3.back, targetRotationDistance);
                    targetRotationDistance = 0;
                }
                else
                {
                    if (targetRotationDistance > 0)
                    {
                        transform.Rotate(Vector3.back, frameNewRotation);
                        targetRotationDistance -= frameNewRotation;
                        
                    }
                    else
                    {
                        transform.Rotate(Vector3.back, -frameNewRotation);
                        targetRotationDistance += frameNewRotation;
                    }
                    
                }
            }
        }
        else
        {
            targetRotationDistance = 0;
            var frameNewRotation = rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.back, frameNewRotation);
        }
    }

    /// <summary>
    /// Gets the inputs and preprocesses them
    /// </summary>
    /// <returns>(inputStart, persistentInput)</returns>
    private (bool, bool) GetInputs()
    {
        // True if it should count as if the player clicked in this 'frame' (tho it's actually more complicated)
        var initialActionStart = false;
        
        // True if the player inputs something or it should still count as if
        var actionPersist = false;
        
        if (_jumpAction.inProgress)
        {
            actionPersist = true;
            
            if (_inputEarlyDelay == -1) _inputEarlyDelay = maximumInputDiscrepancy;
            
        }
        else if (_inputEarlyDelay == 0)
        {
            _inputEarlyDelay = -1;
        }
        
        if (_inputEarlyDelay > 0)
        {
            initialActionStart = true;
            _inputEarlyDelay -= Time.deltaTime;
            
            if (_inputEarlyDelay <= 0) _inputEarlyDelay = 0; 
        }
        
        return (initialActionStart, actionPersist);
    }
    
    public void Jump(float multiplier = 1)
    {
        if (_jumpingBlockedFrames > 0) return;
        
        grounded = false;
        var directionVec = gravity < 0 ? Vector3.up : Vector3.down;
        var directionFactor = gravity < 0 ? 1 : -1;
        transform.position += directionVec * 0.4f;
        velocityY = jumpVelocity * multiplier * directionFactor;

        _jumpingBlockedFrames = 3;
    }

    /// <summary>
    /// Checks which interactable objects the player touches and
    /// potentially triggers their onInteract actions.
    /// </summary>
    private void PerformInteractions(bool interactionStarted)
    {
        if (!interactionStarted) return;
        
        Vector2 origin = transform.position;
        var size = new Vector2(PlayerHeight, PlayerHeight);
        float angle = 0;
        const string maskName = "Interactable";
        var layer = LayerMask.GetMask(maskName);
        var hit = Physics2D.OverlapBox(origin, size, angle, layer);

        if (hit != null)
        {
            var interactable = hit.GetComponent<IInteractable>();

            if (interactable == null) return;
            
            interactable.Interact(this);
            
            // Make sure no other interaction can follow until it's pressed again
            _inputEarlyDelay = 0;
        }
    }

    /// <summary>
    /// Changes the velocity and checks that the maximum velocity is not exceeded.
    /// </summary>
    /// <param name="force">How much to add.</param>
    private void SetVelocityY(float force)
    {
        var newVelocityY = force;
        
        if (force > velocityY && newVelocityY > maximumVelocity)
        {
            newVelocityY = maximumVelocity;
        }
        else if (force < velocityY && newVelocityY < -maximumVelocity)
        {
            newVelocityY = -maximumVelocity;
        }

        velocityY = newVelocityY;
    }

    private void SetPositionOnGround(int depth = 0)
    {
        const int maxDepth = 5;
        
        // The minimal height for the player to count as ungrounded.‚
        _rayBuffer = Physics2D.Raycast(center.transform.position, Vector2.down, 0.78f);

        if (_rayBuffer.collider == null) return;
        
        float minHeightUngrounded = _rayBuffer.distance - 0.5f;
        
        Debug.DrawLine(center.transform.position + (Vector3.down * 0.5f), center.transform.position + Vector3.down * (0.5f + minHeightUngrounded), Color.coral);
        

        if (minHeightUngrounded <= 0) return;
        
        transform.position += Vector3.up * (minHeightUngrounded - 0.05f);

        if (depth < maxDepth)
        {
            SetPositionOnGround(++depth);
        }
    }
  

    private bool CheckGrounded()
    {
        const float rayLength = 0.01f;
        grounded = false;
        
        // Find the bottom

        if (Debug.isDebugBuild)
        {
            Debug.DrawLine(_topPos, _topPos + (Vector3.down * rayLength), Color.green);
        }
        
        _bottomPos = transform.position + Vector3.down * (PlayerHeight / 2 + 0.01f);
        _topPos = transform.position + Vector3.up * (PlayerHeight / 2 + 0.01f);
        var searchPos = gravity < 0 ? _bottomPos : _topPos;
        var searchDir = gravity < 0 ? Vector2.down : Vector2.up;
        
        
        
        _rayBuffer = Physics2D.Raycast(searchPos, searchDir, 0.01f, LayerMask.GetMask(_wallSearchMap));
        grounded |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(searchPos - Vector3.left * 0.5f, searchDir, 0.01f, LayerMask.GetMask(_wallSearchMap));
        grounded |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(searchPos - Vector3.right * 0.5f, searchDir, 0.01f, LayerMask.GetMask(_wallSearchMap));
        grounded |= _rayBuffer.collider != null;

        return grounded;
    }
    
    /// <summary>
    /// Checks for spikes in the way or walls that the player might currently touch
    /// to determine whether there is a reason for the player to be currently dead.
    /// </summary>
    private void CheckDead()
    {
        // Check if blocked in front
        var mask = LayerMask.GetMask("Wall", "Deadly");
        var angle = transform.rotation.eulerAngles.z;
        var overlapResult = Physics2D.OverlapBox(transform.position, new Vector2(PlayerHeight/2-0.1f, PlayerHeight/2-0.1f), 0, mask);
        var hit = overlapResult != null;

        if (hit) { Debug.Log("Dying due to wall") ; Die(); }
        
        // Check for spikes and such
        /*if (Physics2D.BoxCast(_origin, new Vector2(0.8f, 0.8f), 0, Vector2.zero, 0.1f, LayerMask.GetMask("Deadly"))
                .collider != null)
        {
            Debug.Log("Dying due to spike");
            Die();
        }*/
    }
    
    private void Die()
    { 
        Debug.Log("Died");
        Debug.Break();
        //Destroy(gameObject);
    }

    public void ReverseGravity()
    {
        gravity = -gravity;

        if (gravity < 0)
        {
            transform.position += Vector3.up;
        }
        else
        {
            transform.position -= Vector3.down;
        }
    }

    public void ReverseVelocity()
    {
        velocityY = -velocityY;
    }

    public void ZeroVelocity()
    {
        velocityY = 0;
    }
}

public enum ControlMode
{
    Normal,
}
