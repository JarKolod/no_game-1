using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent( typeof( CharacterController2D ) )]
public class PlayerCharacterController2D : MonoBehaviour
{
    private CharacterController2D thisCharacterController;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [Range( 0f , 3f )]
    [SerializeField] private float accelerationTimeAirborn = .2f;
    [Range( 0f , 3f )]
    [SerializeField] private float accelerationTimeGrounded = .2f;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float timeToJumpApex = 0.3f;
    [SerializeField] private float fallGravityMultiplier = 2.0f;


    private float velocityXSmooth;

    private float gravity;
    private float initialJumpVelocity = 10f;
    private bool isJumping = false;

    private float movementInput;
    private bool jumpInput = false; //button

    private Vector3 nextVelocity;
    private bool isFalling;

    //for jump to work as intended for now
    private float tempVar = 1.52f;

    private void Awake()
    {
        thisCharacterController = GetComponent<CharacterController2D>();

        SetupJumpVariales();

    }


    private void SetupJumpVariales()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex,2);
        initialJumpVelocity = tempVar * (2 * jumpHeight) / (timeToJumpApex);
        //initialJumpVelocity = Mathf.Abs( gravity * timeToJumpApex );

        print( "Gravity: " + gravity + " jump vel: " + initialJumpVelocity );
    }


    private void Start()
    {


    }


    private void FixedUpdate()
    {

        if( thisCharacterController.collisions.top || thisCharacterController.collisions.bottom )
        {
            nextVelocity.y = 0f;
        }


        HandleInputSmoothing();
        HandleGravity();
        HandleJumping();

        thisCharacterController.Move( nextVelocity * Time.fixedDeltaTime );


    }


    private void HandleInputSmoothing( )
    {
        float targetVelocityX = movementInput * moveSpeed;
        float accelerationTime = (thisCharacterController.collisions.bottom) ? accelerationTimeGrounded : accelerationTimeAirborn;

        nextVelocity.x = Mathf.SmoothDamp( nextVelocity.x , targetVelocityX , ref velocityXSmooth , accelerationTime );
    }


    private void HandleGravity()
    {
        isFalling = nextVelocity.y <= 0f;

        if( !isFalling )
        {
            float prevYVelocity = nextVelocity.y;
            float newYVelocity = nextVelocity.y + (gravity * Time.deltaTime);
            float nextYVelocity = (prevYVelocity + newYVelocity) * 0.5f;
            nextVelocity.y = nextYVelocity;
        }
        else
        {
            float prevYVelocity = nextVelocity.y;
            float newYVelocity = nextVelocity.y + (gravity * fallGravityMultiplier * Time.deltaTime);
            float nextYVelocity = (prevYVelocity + newYVelocity) * 0.5f;
            nextVelocity.y = nextYVelocity;
        }

    }


    private void HandleJumping()
    {
        if(!isJumping && jumpInput && thisCharacterController.collisions.bottom )
        {
            isJumping = true;

            float prevYVelocity = nextVelocity.y;
            float newYVelocity = nextVelocity.y + initialJumpVelocity;
            nextVelocity.y = (prevYVelocity + newYVelocity) * 0.5f;

        } 
        else if(isJumping && !jumpInput && thisCharacterController.collisions.bottom)
        {
            isJumping = false;
        }
    }


    public void OnMovement( InputAction.CallbackContext ctx )
    {
        movementInput = ctx.ReadValue<float>();
    }


    public void OnJump( InputAction.CallbackContext ctx )
    {
        if( ctx.performed || ctx.started )
        {
            jumpInput = true;
        }
        else if( ctx.canceled )
        {
            jumpInput = false;
        }
        print( jumpInput );
    }

}