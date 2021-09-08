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

    [Header( "wallJumping" )]
    [SerializeField] private float wallSlideSpeedMax = 3f;
    [SerializeField] private float wallStickTime = .2f;
    [SerializeField] private Vector2 wallJumpClimb;
    [SerializeField] private Vector2 wallJumpOff;
    [SerializeField] private Vector2 wallJumpAway;
    
    private float timeToWallUnstick;


    private float velocityXSmooth;

    private float gravity;
    private float initialJumpVelocity = 10f;
    private bool isJumping = false;

    private float movementInput;
    private bool isJumpPressed = false; //button

    private Vector3 velocity;
    private bool isFalling;
    private float maxCharacterPositionOffset = 100f;

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


    private void FixedUpdate()
    {
        //this is here for walljumping to work, i know... dumb implementation
        HandleInputSmoothing();

        #region wallJumping

        int wallDirX = (thisCharacterController.collisions.left) ? -1 : 1;
        bool isWallSlliding = false;

        if((thisCharacterController.collisions.left || thisCharacterController.collisions.right) && !thisCharacterController.collisions.bottom && velocity.y < 0f)
        {
            isWallSlliding = true;

            if(velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            if(timeToWallUnstick > 0f)
            {
                //this should not be happening here
                velocityXSmooth = 0f;

                velocity.x = 0f;

                if( movementInput != wallDirX && movementInput != 0f )
                    timeToWallUnstick -= Time.fixedDeltaTime;
                else
                    timeToWallUnstick = wallStickTime;
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }


        }

        #endregion

        if( thisCharacterController.collisions.top || thisCharacterController.collisions.bottom )
        {
            velocity.y = 0f;
        }


        HandleGravity();
        HandleJumping( isWallSlliding, wallDirX );

        thisCharacterController.Move( velocity * Time.fixedDeltaTime );


    }


    private void HandleInputSmoothing( )
    {
        float targetVelocityX = movementInput * moveSpeed;
        float accelerationTime = (thisCharacterController.collisions.bottom) ? accelerationTimeGrounded : accelerationTimeAirborn;

        //if collides with a wall (aka head on collision) then kill momentum
        if( thisCharacterController.collisions.slopeAngle <= thisCharacterController.MaxClimbAngle ) 
        {
            velocity.x = Mathf.SmoothDamp( velocity.x , targetVelocityX , ref velocityXSmooth , accelerationTime ); 
        }
        else
        {
            velocity.x = Mathf.SmoothDamp( 0f , targetVelocityX , ref velocityXSmooth , accelerationTime ); 
        }
    }


    private void HandleGravity()
    {
        isFalling = velocity.y <= 0f;

        //this implementation causes wall jumping to be a bit undesirable
        if( !isFalling && isJumpPressed )
        {
            float prevYVelocity = velocity.y;
            float newYVelocity = velocity.y + (gravity * Time.fixedDeltaTime);
            float nextYVelocity = Mathf.Clamp((prevYVelocity + newYVelocity) * 0.5f, -maxCharacterPositionOffset , maxCharacterPositionOffset );
            velocity.y = nextYVelocity;
        }
        else
        {
            float prevYVelocity = velocity.y;
            float newYVelocity = velocity.y + (gravity * fallGravityMultiplier * Time.fixedDeltaTime);
            float nextYVelocity = (prevYVelocity + newYVelocity) * 0.5f;
            velocity.y = nextYVelocity;
        }

    }


    private void HandleJumping(bool isWallSliding, int wallDirX)
    {
        if(!isJumping && isJumpPressed ) //&& thisCharacterController.collisions.bottom )
        {


            if(isWallSliding)
            {
                isJumping = true;
                Vector2 prevVelocity = velocity;

                //TODO: implement Verlet velocity calculation
                if( wallDirX == movementInput )
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                else if(wallDirX == 0)
                {
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y;
                }
                else //if input is oposite of the wall (jump away)
                {
                    velocity.x = -wallDirX * wallJumpAway.x;
                    velocity.y = wallJumpAway.y;
                }

            }
            else if(thisCharacterController.collisions.bottom)
            {
                isJumping = true;
                float prevYVelocity = velocity.y;
                float newYVelocity = velocity.y + initialJumpVelocity;
                velocity.y = (prevYVelocity + newYVelocity) * 0.5f; 
            }

        } 
        else if( (isJumping && !isJumpPressed && thisCharacterController.collisions.bottom ) || isWallSliding)
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
            isJumpPressed = true;
        }
        else if( ctx.canceled )
        {
            isJumpPressed = false;
        }
    }

}
