using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework.Constraints;
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
    [SerializeField] private float Gravity;
    
    [SerializeField] private bool grounded;
    [SerializeField] private GameObject center;
    [SerializeField] private float velocityY = 0;
    [SerializeField] private InputActionMap gameActionMap;
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float rotationSpeed;
    /// <summary>
    /// The speed used to rotate the player back into normal after touching the ground
    /// </summary>
    [SerializeField] private float rotationSpeedOnGround;
    [FormerlySerializedAs("targetRotation")] [SerializeField] private float targetRotationDistance;
    
    private const float PlayerHeight = 1f;
    private bool _lastFrameGrounded = false;
    private byte _jumpingBlockedFrames;
    
    private RaycastHit2D _rayBuffer;
    private Vector3 _bottomPos;
    private Vector3 _forwardPos;
    private Vector2 _origin;
    
    private ControlMode _mode = ControlMode.Normal;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Application.targetFrameRate = 240;
        gameActionMap = InputActionMap.FromJson(System.IO.File.ReadAllText(Application.dataPath + "/InputSystem_Actions.inputactions"))[0];
        gameActionMap.Enable();

        
        gameActionMap.FindAction("Jump").Enable();
        gameActionMap.FindAction("Jump").performed += _ => Jump();
        
        
        _bottomPos = transform.position + Vector3.down * (PlayerHeight / 2 + 0.01f);
    }

   


    // Update is called once per frame
    void LateUpdate()
    {
        _origin = transform.position - (Vector3.left + Vector3.up) * (PlayerHeight / 2 - 0.1f);
        _forwardPos = transform.position + Vector3.right * (PlayerHeight / 2 - 0.1f);
        
        float currentRotation = transform.rotation.eulerAngles.z;


        // Update gravity
        switch (_mode)
        {
            case ControlMode.Normal:
                velocityY += Gravity * Time.deltaTime;
                break;
        }

        var posChangeY = velocityY;


        if (CheckGrounded() && targetRotationDistance == 0)
        {
            var originalDist = targetRotationDistance;
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
        

        if (grounded && _jumpingBlockedFrames == 0)
        {
            posChangeY = 0;
            velocityY = 0;

            if (!_lastFrameGrounded)
            {
                SetPositionOnGround();
            }

            if (targetRotationDistance > 3 | targetRotationDistance < -3)
            {
                var frameNewRotation = rotationSpeedOnGround * Time.deltaTime;
                if (currentRotation + frameNewRotation > Mathf.Abs(targetRotationDistance))
                {
                    transform.Rotate(Vector3.back, targetRotationDistance);
                    targetRotationDistance = 0;
                    Debug.Log("Reseted target rot");
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
        
        
        _lastFrameGrounded = grounded;
        
        gameObject.transform.position += Vector3.up * posChangeY;

        if (_jumpingBlockedFrames > 0)
        {
            _jumpingBlockedFrames--;
        }
        
        CheckDead();
    }
    
     private void Jump()
    {
        // The minimal height for the player to count as ungrounded.
        SetPositionOnGround();
        if (grounded)
        {
            grounded = false;
            transform.position += Vector3.up * 0.4f;
            velocityY = jumpVelocity;
        }

        _jumpingBlockedFrames = 3;
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
        
        transform.position += Vector3.up * (minHeightUngrounded);

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
            Debug.DrawLine(_bottomPos, _bottomPos + (Vector3.down * rayLength), Color.green);
        }
        
        _bottomPos = transform.position + Vector3.down * (PlayerHeight / 2 + 0.01f);
        
        _rayBuffer = Physics2D.Raycast(_bottomPos, Vector2.down, 0.01f, LayerMask.GetMask("Wall"));
        grounded |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(_bottomPos - Vector3.left * 0.5f, Vector2.down, 0.01f, LayerMask.GetMask("Wall"));
        grounded |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(_bottomPos - Vector3.right * 0.5f, Vector2.down, 0.01f, LayerMask.GetMask("Wall"));
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
        var hit = false;
        
        const float rayLength = 0.01f;
        
        
        _rayBuffer = Physics2D.Raycast(_forwardPos, Vector2.right, rayLength, LayerMask.GetMask("Wall"));
        hit |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(_forwardPos - Vector3.up * 0.3f, Vector2.right, rayLength, LayerMask.GetMask("Wall"));
        hit |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(_forwardPos - Vector3.down * 0.3f, Vector2.right, rayLength, LayerMask.GetMask("Wall"));
        hit |= _rayBuffer.collider != null;

        if (hit) Die();
        
        // Check for spikes and such
        if (Physics2D.BoxCast(_origin, new Vector2(0.8f, 0.8f), 0, Vector2.zero, 0.1f, LayerMask.GetMask("Deadly"))
                .collider != null)
        {
            Die();
        }
    }
    
    private void Die()
    {
        Destroy(gameObject);
    }
}

public enum ControlMode
{
    Normal,
}
