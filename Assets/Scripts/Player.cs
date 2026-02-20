using System;
using System.Linq;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    /// <summary>
    ///  Gravity in m/s.
    /// </summary>
    [SerializeField] private float Gravity;
    private RaycastHit2D _rayBuffer;
    private Vector3 _bottomPos;
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
    private byte jumpingBlockedFrames = 0;
    

    private ControlMode mode = ControlMode.Normal;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Application.targetFrameRate = 240;
        gameActionMap = InputActionMap.FromJson(System.IO.File.ReadAllText(Application.dataPath + "/InputSystem_Actions.inputactions"))[0];
        gameActionMap.Enable();

        
        gameActionMap.FindAction("Jump").Enable();
        gameActionMap.FindAction("Jump").performed += _ => Jump();
    }

    public void Jump()
    {
        Debug.Log("Jumping - v0.0");
        // The minimal height for the player to count as ungrounded.
        SetPositionOnGround();
        if (grounded)
        {
            grounded = false;
            transform.position += Vector3.up * 0.4f;
            velocityY = jumpVelocity;
        }

        jumpingBlockedFrames = 3;
    }

    public void SetPositionOnGround(int depth = 0)
    {
        const int maxDepth = 5;
        
        // The minimal height for the player to count as ungrounded.‚
        _bottomPos = transform.position + Vector3.down * (PlayerHeight / 2 + 0.01f);
        _rayBuffer = Physics2D.Raycast(center.transform.position, Vector2.down, 1f);
        float minHeightUngrounded = _rayBuffer.distance - 0.5005f;

        if (minHeightUngrounded > 0)
        {
            transform.position += Vector3.up * (minHeightUngrounded - 0.015f);
            Debug.Log("Grounded");

            if (_rayBuffer.collider != null && depth < maxDepth)
            {
                SetPositionOnGround(++depth);
            }
        }
        
        
    }


    // Update is called once per frame
    void LateUpdate()
    {
        const float rayLength = 0.01f;
        float currentRotation = transform.rotation.eulerAngles.z;


        // Update gravity
        switch (mode)
        {
            case ControlMode.Normal:
                velocityY += Gravity * Time.deltaTime;
                break;
        }

        var posChangeY = velocityY;


        // Find the bottom
        _bottomPos = transform.position + Vector3.down * (PlayerHeight / 2 + 0.01f);

        if (Debug.isDebugBuild)
        {
            Debug.DrawLine(_bottomPos, _bottomPos + (Vector3.down * rayLength), Color.green);
        }

        bool wallHitBottom = false;

        _rayBuffer = Physics2D.Raycast(_bottomPos, Vector2.down, 0.01f, LayerMask.GetMask("Wall"));
        wallHitBottom |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(_bottomPos - Vector3.left * 0.5f, Vector2.down, 0.01f, LayerMask.GetMask("Wall"));
        wallHitBottom |= _rayBuffer.collider != null;
        _rayBuffer = Physics2D.Raycast(_bottomPos - Vector3.right * 0.5f, Vector2.down, 0.01f, LayerMask.GetMask("Wall"));
        wallHitBottom |= _rayBuffer.collider != null;

        if (wallHitBottom)
        {
            grounded = true;
            
            targetRotationDistance = -((currentRotation % 180) - 90);
            
            if (Mathf.Abs(targetRotationDistance) < 5)
            {
                targetRotationDistance = 0;
            }
            
        }
        else
        {
            grounded = false;
        }
        

        if (grounded && jumpingBlockedFrames == 0) 
        {
            posChangeY = 0;
            velocityY = 0;

            if (!_lastFrameGrounded)
            {
                SetPositionOnGround();
            }

            if (targetRotationDistance != 0)
            {
                var frameNewRotation = rotationSpeedOnGround * Time.deltaTime;
                if (currentRotation + frameNewRotation > Mathf.Abs(targetRotationDistance))
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
            var frameNewRotation = rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.back, frameNewRotation);
        }
        
        _lastFrameGrounded = grounded;
        
        gameObject.transform.position += Vector3.up * posChangeY;

        if (jumpingBlockedFrames > 0)
        {
            jumpingBlockedFrames--;
        }
    }
}

public enum ControlMode
{
    Normal,
}
