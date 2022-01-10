using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple 3D character controller.
/// Inspired by: https://github.com/prime31/CharacterController2D
/// </summary>
public class Character3DController : MonoBehaviour
{
    #region internal types
    
    struct CharacterRaycastOrigins
    {
        public Vector3 topLeft;
        public Vector3 bottomRight;
        public Vector3 bottomLeft;
    }

    public class CharacterCollisionState3D
    {
        public bool right;
        public bool left;
        public bool above;
        public bool below;
        public bool becameGroundedThisFrame;
        public bool wasGroundedLastFrame;
        public bool movingDownSlope;
        public float slopeAngle;

        public bool hasCollision()
        {
            return below || right || left || above;
        }


        public void reset()
        {
            right = left = above = below = becameGroundedThisFrame = movingDownSlope = false;
            slopeAngle = 0f;
        }


        public override string ToString()
        {
            return string.Format( "[CharacterCollisionState3D] r: {0}, l: {1}, a: {2}, b: {3}, movingDownSlope: {4}, angle: {5}, wasGroundedLastFrame: {6}, becameGroundedThisFrame: {7}",
                                 right, left, above, below, movingDownSlope, slopeAngle, wasGroundedLastFrame, becameGroundedThisFrame );
        }
    }

    #endregion

    #region events, properties and fields
    
    /// <summary>
    /// Wraper around information about a collision.
    /// </summary>
    public class ColliderHit
    {
        internal Collider mCollider;
        internal Vector3 mPoint;
        internal Vector3 mNormal;
        internal Vector3 mMoveDirection;
        internal float mMoveLength;

        /// <summary> The collider that was hit by the controller. </summary>
        public Collider collider => this.mCollider;
        /// <summary> The rigidbody that was hit by the controller. </summary>
        public Rigidbody rigidbody => this.mCollider.attachedRigidbody;
        /// <summary> The game object that was hit by the controller. </summary>
        public GameObject gameObject => this.mCollider.gameObject;
        /// <summary> The transform that was hit by the controller. </summary>
        public Transform transform => this.mCollider.transform;
        /// <summary> The impact point in world space. </summary>
        public Vector3 point => this.mPoint;
        /// <summary> The normal of the surface we collided with in world space. </summary>
        public Vector3 normal => this.mNormal;
        /// <summary> The direction the CharacterController was moving in when the collision occured. </summary>
        public Vector3 moveDirection => this.mMoveDirection;
        /// <summary> How far the character has travelled until it hit the collider. </summary>
        public float moveLength => this.mMoveLength;
    } // class ColliderHit

    public event Action<RaycastHit> onControllerCollidedEvent;
    public event Action<ColliderHit> onColliderHit;
    public event Action<Collider> onTriggerEnterEvent;
    public event Action<Collider> onTriggerStayEvent;
    public event Action<Collider> onTriggerExitEvent;

    /// <summary>
    /// when true, one way platforms will be ignored when moving vertically for a single frame
    /// </summary>
    public bool ignoreOneWayPlatformsThisFrame;

    [SerializeField]
    [Range( 0.001f, 0.3f )]
    float mSkinWidth = 0.02f;

    /// <summary>
    /// defines how far in from the edges of the collider rays are cast from. If cast with a 0 extent it will often result in ray hits that are
    /// not desired (for example a foot collider casting horizontally from directly on the surface can result in a hit)
    /// </summary>
    public float MSkinWidth
    {
        get { return mSkinWidth; }
        set
        {
            mSkinWidth = value;
            recalculateDistanceBetweenRays();
        }
    }


    /// <summary>
    /// mask with all layers that the player should interact with
    /// </summary>
    public LayerMask platformMask = 0;

    /// <summary>
    /// mask with all layers that trigger events should fire when intersected
    /// </summary>
    public LayerMask triggerMask = 0;

    /// <summary>
    /// mask with all layers that should act as one-way platforms. Note that one-way platforms should always be EdgeCollider2Ds. This is because it does not support being
    /// updated anytime outside of the inspector for now.
    /// </summary>
    [SerializeField]
    LayerMask oneWayPlatformMask = 0;

    /// <summary>
    /// the max slope angle that the CC2D can climb
    /// </summary>
    /// <value>The slope limit.</value>
    [Range( 0f, 90f )]
    public float slopeLimit = 30f;

    /// <summary>
    /// the threshold in the change in vertical movement between frames that constitutes jumping
    /// </summary>
    /// <value>The jumping threshold.</value>
    public float jumpingThreshold = 0.07f;


    /// <summary>
    /// curve for multiplying speed based on slope (negative = down slope and positive = up slope)
    /// </summary>
    public AnimationCurve slopeSpeedMultiplier = new AnimationCurve( 
        new Keyframe( -90f, 1.5f ), 
        new Keyframe( 0f, 1f ), 
        new Keyframe( 90f, 0f ) 
    );

    [Range( 2, 20 )]
    public int totalHorizontalRays = 8;
    [Range( 2, 20 )]
    public int totalVerticalRays = 4;


    /// <summary>
    /// this is used to calculate the downward ray that is cast to check for slopes. We use the somewhat arbitrary value 75 degrees
    /// to calculate the length of the ray that checks for slopes.
    /// </summary>
    float _slopeLimitTangent = Mathf.Tan( 75f * Mathf.Deg2Rad );

    [HideInInspector][NonSerialized]
    public GameObject model;
    [HideInInspector][NonSerialized]
    public new Transform transform;
    [HideInInspector][NonSerialized]
    public BoxCollider boxCollider;
    [HideInInspector][NonSerialized]
    public Rigidbody rigidBody;

    [HideInInspector][NonSerialized]
    public CharacterCollisionState3D collisionState = new CharacterCollisionState3D();
    [HideInInspector][NonSerialized]
    public Vector3 velocity;
    public bool isGrounded { get { return collisionState.below; } }

    const float kSkinWidthFloatFudgeFactor = 0.001f;

    #endregion

    /// <summary>
    /// holder for our raycast origin corners (TR, TL, BR, BL)
    /// </summary>
    CharacterRaycastOrigins mRaycastOrigins;

    /// <summary>
    /// stores our raycast hit during movement
    /// </summary>
    RaycastHit mRaycastHit;
    
    /// <summary>
    /// stores our unstuck raycast hit during movement
    /// </summary>
    RaycastHit mUnstuckRaycastHit;

    /// <summary>
    /// stores any raycast hits that occur this frame. we have to store them in case we get a hit moving
    /// horizontally and vertically so that we can send the events after all collision state is set
    /// </summary>
    List<RaycastHit> mRaycastHitsThisFrame = new List<RaycastHit>( 2 );

    // horizontal/vertical movement data
    float mVerticalDistanceBetweenRays;
    float mHorizontalDistanceBetweenRays;

    // we use this flag to mark the case where we are travelling up a slope and we modified our delta.y to allow the climb to occur.
    // the reason is so that if we reach the end of the slope we can make an adjustment to stay grounded
    bool mIsGoingUpSlope = false;

    #region Monobehaviour

    void Awake()
    {
        // add our one-way platforms to our normal platform mask so that we can land on them from above
        platformMask |= oneWayPlatformMask;

        // cache some components
        transform = GetComponent<Transform>();
        boxCollider = GetComponent<BoxCollider>();
        rigidBody = GetComponent<Rigidbody>();

        // here, we trigger our properties that have setters with bodies
        MSkinWidth = mSkinWidth;

        // we want to set our CC2D to ignore all collision layers except what is in our triggerMask
        for( var i = 0; i < 32; i++ )
        {
            // see if our triggerMask contains this layer and if not ignore it
            if( ( triggerMask.value & 1 << i ) == 0 )
                Physics.IgnoreLayerCollision( gameObject.layer, i );
        }
    }

    /// <summary>
    /// Callback when the current character changes.
    /// </summary>
    void OnCharacterChange(CharacterSelector selector)
    {
        boxCollider.size = selector.characterBB.size;
        boxCollider.center = selector.characterBB.center;
    }

    void Start()
    { }

    public void OnTriggerEnter( Collider col )
    {
        if ( onTriggerEnterEvent != null )
        { onTriggerEnterEvent( col ); }
    }

    public void OnTriggerStay( Collider col )
    {
        if ( onTriggerStayEvent != null )
        { onTriggerStayEvent( col ); }
    }

    public void OnTriggerExit( Collider col )
    {
        if ( onTriggerExitEvent != null )
        { onTriggerExitEvent( col ); }
    }

    #endregion

    [System.Diagnostics.Conditional( "DEBUG_CC2D_RAYS" )]
    void DrawRay( Vector3 start, Vector3 dir, Color color )
    {
        Debug.DrawRay( start, dir, color );
    }

    #region Public

    /// <summary>
    /// attempts to move the character to position + deltaMovement. Any colliders in the way will cause the movement to
    /// stop when run into.
    /// </summary>
    /// <param name="deltaMovement">Delta movement.</param>
    public void Move( Vector3 deltaMovement )
    {
        // Save off our current grounded state which we will use for wasGroundedLastFrame and becameGroundedThisFrame
        collisionState.wasGroundedLastFrame = collisionState.below;

        // Clear our state
        collisionState.reset();
        mRaycastHitsThisFrame.Clear();
        mIsGoingUpSlope = false;

        recalculateDistanceBetweenRays();
        
        primeRaycastOrigins();

        // First, we check for a slope below us before moving. Only check slopes if we are going down and grounded
        if (deltaMovement.y < 0f && collisionState.wasGroundedLastFrame)
        { handleVerticalSlope(ref deltaMovement); }

        // Now we check movement in the horizontal dir
        if (deltaMovement.x != 0f)
        { moveHorizontally(ref deltaMovement); }

        // Next, check movement in the vertical dir
        if (deltaMovement.y != 0f)
        { moveVertically(ref deltaMovement); }

        // Move then update our state
        deltaMovement.z = 0;
        transform.Translate( deltaMovement, Space.World );

        // Only calculate velocity if we have a non-zero deltaTime
        if (Time.deltaTime > 0f)
        { velocity = deltaMovement / Time.deltaTime; }

        // Set our becameGrounded state based on the previous and current collision state
        if (!collisionState.wasGroundedLastFrame && collisionState.below)
        { collisionState.becameGroundedThisFrame = true; }

        // If we are going up a slope we artificially set a y velocity so we need to zero it out here
        if (mIsGoingUpSlope)
        { velocity.y = 0; }

        // Send off the collision events if we have a listener
        if (onControllerCollidedEvent != null)
        {
            foreach (var raycastHit in mRaycastHitsThisFrame)
            { onControllerCollidedEvent(raycastHit); }
        }

        if (onColliderHit != null)
        {
            foreach (var raycastHit in mRaycastHitsThisFrame)
            {
                var colliderHit = new ColliderHit();
                colliderHit.mCollider = raycastHit.collider;
                colliderHit.mPoint = raycastHit.point;
                colliderHit.mNormal = raycastHit.normal;
                colliderHit.mMoveDirection = deltaMovement.normalized;
                colliderHit.mMoveLength = deltaMovement.magnitude;
                onColliderHit(colliderHit);
            } 
        }

        ignoreOneWayPlatformsThisFrame = false;
    }


    /// <summary>
    /// moves directly down until grounded
    /// </summary>
    public void WarpToGrounded()
    {
        do
        {
            Move( new Vector3( 0, -1f, 0 ) );
        } while( !isGrounded );
    }


    /// <summary>
    /// this should be called anytime you have to modify the BoxCollider at runtime. It will recalculate the distance between the rays used for collision detection.
    /// It is also used in the skinWidth setter in case it is changed at runtime.
    /// </summary>
    public void recalculateDistanceBetweenRays()
    {
        // figure out the distance between our rays in both directions
        // horizontal
        var colliderUseableHeight = boxCollider.size.y * Mathf.Abs( transform.localScale.y ) - ( 2f * mSkinWidth );
        mVerticalDistanceBetweenRays = colliderUseableHeight / ( totalHorizontalRays - 1 );

        // vertical
        var colliderUseableWidth = boxCollider.size.x * Mathf.Abs( transform.localScale.x ) - ( 2f * mSkinWidth );
        mHorizontalDistanceBetweenRays = colliderUseableWidth / ( totalVerticalRays - 1 );
    }

    #endregion


    #region Movement Methods

    /// <summary>
    /// resets the raycastOrigins to the current extents of the box collider inset by the skinWidth. It is inset
    /// to avoid casting a ray from a position directly touching another collider which results in wonky normal data.
    /// </summary>
    /// <param name="futurePosition">Future position.</param>
    /// <param name="deltaMovement">Delta movement.</param>
    void primeRaycastOrigins()
    {
        // our raycasts need to be fired from the bounds inset by the skinWidth
        var modifiedBounds = boxCollider.bounds;
        modifiedBounds.Expand( -2f * mSkinWidth );

        mRaycastOrigins.topLeft = new Vector2( modifiedBounds.min.x, modifiedBounds.max.y );
        mRaycastOrigins.bottomRight = new Vector2( modifiedBounds.max.x, modifiedBounds.min.y );
        mRaycastOrigins.bottomLeft = modifiedBounds.min;
    }


    /// <summary>
    /// we have to use a bit of trickery in this one. The rays must be cast from a small distance inside of our
    /// collider (skinWidth) to avoid zero distance rays which will get the wrong normal. Because of this small offset
    /// we have to increase the ray distance skinWidth then remember to remove skinWidth from deltaMovement before
    /// actually moving the player
    /// </summary>
    void moveHorizontally( ref Vector3 deltaMovement )
    {
        var isGoingRight = deltaMovement.x > 0;
        var rayDistance = Mathf.Abs( deltaMovement.x ) + mSkinWidth;
        var rayDirection = isGoingRight ? Vector2.right : -Vector2.right;
        var initialRayOrigin = isGoingRight ? mRaycastOrigins.bottomRight : mRaycastOrigins.bottomLeft;

        for( var i = 0; i < totalHorizontalRays; i++ )
        {
            var forwardRay = new Ray( 
                new Vector3(initialRayOrigin.x, initialRayOrigin.y + i * mVerticalDistanceBetweenRays, initialRayOrigin.z), 
                rayDirection
            );
            var rayHit = false;
            var backwardRay = new Ray( 
                new Vector3(initialRayOrigin.x, initialRayOrigin.y + i * mVerticalDistanceBetweenRays, initialRayOrigin.z), 
                -rayDirection
            );
            var unstuckRayHit = false;

            // if we are grounded we will include oneWayPlatforms only on the first ray (the bottom one). this will allow us to
            // walk up sloped oneWayPlatforms
            if (i == 0 && collisionState.wasGroundedLastFrame)
            {
                rayHit = Physics.Raycast( forwardRay, out mRaycastHit, rayDistance, platformMask );
                unstuckRayHit = Physics.Raycast( backwardRay, out mUnstuckRaycastHit, rayDistance, platformMask );
            }
            else
            {
                rayHit = Physics.Raycast( forwardRay, out mRaycastHit, rayDistance, platformMask & ~oneWayPlatformMask );
                unstuckRayHit = Physics.Raycast( backwardRay, out mUnstuckRaycastHit, rayDistance, platformMask & ~oneWayPlatformMask );
            }

            if (unstuckRayHit)
            {
                if (isGoingRight)
                { deltaMovement.x = mUnstuckRaycastHit.point.x - boxCollider.bounds.max.x; }
                else
                { deltaMovement.x = mUnstuckRaycastHit.point.x - boxCollider.bounds.min.x; }

                break;
            }

            if(rayHit)
            {
                // the bottom ray can hit a slope but no other ray can so we have special handling for these cases
                if( i == 0 && handleHorizontalSlope( ref deltaMovement, Vector2.Angle( mRaycastHit.normal, Vector2.up ) ) )
                {
                    mRaycastHitsThisFrame.Add( mRaycastHit );
                    // if we weren't grounded last frame, that means we're landing on a slope horizontally.
                    // this ensures that we stay flush to that slope
                    if ( !collisionState.wasGroundedLastFrame )
                    {
                        float flushDistance = Mathf.Sign( deltaMovement.x ) * ( mRaycastHit.distance - MSkinWidth );
                        transform.Translate( new Vector2( flushDistance, 0 ) );
                    }
                    break;
                }

                // set our new deltaMovement and recalculate the rayDistance taking it into account
                deltaMovement.x = mRaycastHit.point.x - forwardRay.origin.x;
                rayDistance = Mathf.Abs( deltaMovement.x );

                // remember to remove the skinWidth from our deltaMovement
                if( isGoingRight )
                {
                    deltaMovement.x -= mSkinWidth;
                    collisionState.right = true;
                }
                else
                {
                    deltaMovement.x += mSkinWidth;
                    collisionState.left = true;
                }

                mRaycastHitsThisFrame.Add( mRaycastHit );

                // we add a small fudge factor for the float operations here. if our rayDistance is smaller
                // than the width + fudge bail out because we have a direct impact
                if( rayDistance < mSkinWidth + kSkinWidthFloatFudgeFactor )
                    break;
            }
        }
    }


    /// <summary>
    /// handles adjusting deltaMovement if we are going up a slope.
    /// </summary>
    /// <returns><c>true</c>, if horizontal slope was handled, <c>false</c> otherwise.</returns>
    /// <param name="deltaMovement">Delta movement.</param>
    /// <param name="angle">Angle.</param>
    bool handleHorizontalSlope( ref Vector3 deltaMovement, float angle )
    {
        // disregard 90 degree angles (walls)
        if( Mathf.RoundToInt( angle ) == 90 )
            return false;

        // if we can walk on slopes and our angle is small enough we need to move up
        if( angle < slopeLimit )
        {
            // we only need to adjust the deltaMovement if we are not jumping
            // TODO: this uses a magic number which isn't ideal! The alternative is to have the user pass in if there is a jump this frame
            if( deltaMovement.y < jumpingThreshold )
            {
                // apply the slopeModifier to slow our movement up the slope
                var slopeModifier = slopeSpeedMultiplier.Evaluate( angle );
                deltaMovement.x *= slopeModifier;

                // we dont set collisions on the sides for this since a slope is not technically a side collision.
                // smooth y movement when we climb. we make the y movement equivalent to the actual y location that corresponds
                // to our new x location using our good friend Pythagoras
                deltaMovement.y = Mathf.Abs( Mathf.Tan( angle * Mathf.Deg2Rad ) * deltaMovement.x );
                var isGoingRight = deltaMovement.x > 0;

                // safety check. we fire a ray in the direction of movement just in case the diagonal we calculated above ends up
                // going through a wall. if the ray hits, we back off the horizontal movement to stay in bounds.
                var ray = new Ray(
                    isGoingRight ? mRaycastOrigins.bottomRight : mRaycastOrigins.bottomLeft, 
                    deltaMovement.normalized
                );
                RaycastHit raycastHit;
                var rayHit = false;
                if (collisionState.wasGroundedLastFrame)
                {
                    rayHit = Physics.Raycast( ray, out raycastHit, deltaMovement.magnitude, platformMask );
                }
                else
                {
                    rayHit = Physics.Raycast( ray, out raycastHit, deltaMovement.magnitude, platformMask & ~oneWayPlatformMask );
                }

                if( rayHit )
                {
                    // we crossed an edge when using Pythagoras calculation, so we set the actual delta movement to the ray hit location
                    deltaMovement = raycastHit.point - ray.origin;
                    if( isGoingRight )
                        deltaMovement.x -= mSkinWidth;
                    else
                        deltaMovement.x += mSkinWidth;
                }

                mIsGoingUpSlope = true;
                collisionState.below = true;
                collisionState.slopeAngle = -angle;
            }
        }
        else // too steep. get out of here
        {
            deltaMovement.x = 0;
        }

        return true;
    }

    bool hitBackface(Vector3 rayOrigin, Vector3 rayDirection, RaycastHit raycastHit)
    { return raycastHit.collider && raycastHit.collider.bounds.Contains(rayOrigin); }
    
    bool hitFromBottom(Vector3 rayOrigin, Vector3 rayDirection, RaycastHit raycastHit)
    { return raycastHit.collider && raycastHit.normal.y < 0.0f; }

    bool hitOneWayPlatform(RaycastHit raycastHit)
    { return raycastHit.collider && (1 << raycastHit.collider.gameObject.layer & oneWayPlatformMask) != 0; }

    void moveVertically( ref Vector3 deltaMovement )
    {
        var isGoingUp = deltaMovement.y > 0;
        var rayDistance = Mathf.Abs( deltaMovement.y ) + mSkinWidth;
        var rayDirection = isGoingUp ? Vector2.up : -Vector2.up;
        var initialRayOrigin = isGoingUp ? mRaycastOrigins.topLeft : mRaycastOrigins.bottomLeft;

        // apply our horizontal deltaMovement here so that we do our raycast from the actual position we would be in if we had moved
        initialRayOrigin.x += deltaMovement.x;

        // if we are moving up, we should ignore the layers in oneWayPlatformMask
        var mask = platformMask;
        if( ( isGoingUp && !collisionState.wasGroundedLastFrame ) || ignoreOneWayPlatformsThisFrame )
            mask &= ~oneWayPlatformMask;

        for( var i = 0; i < totalVerticalRays; i++ )
        {
            var forwardRay = new Ray( 
                new Vector3( initialRayOrigin.x + i * mHorizontalDistanceBetweenRays, initialRayOrigin.y, initialRayOrigin.z ), 
                rayDirection
            );
            var rayHit = false;
            var backwardRay = new Ray( 
                new Vector3( initialRayOrigin.x + i * mHorizontalDistanceBetweenRays, initialRayOrigin.y, initialRayOrigin.z ),  
                -rayDirection
            );
            var unstuckRayHit = false;

            rayHit = Physics.Raycast( forwardRay, out mRaycastHit, rayDistance, mask );
            unstuckRayHit = Physics.Raycast( backwardRay, out mUnstuckRaycastHit, rayDistance, mask );

            if (unstuckRayHit && !hitOneWayPlatform(mUnstuckRaycastHit))
            {
                if (isGoingUp)
                { deltaMovement.y = mUnstuckRaycastHit.point.y - boxCollider.bounds.max.y; }
                else
                { deltaMovement.y = mUnstuckRaycastHit.point.y - boxCollider.bounds.min.y; }

                break;
            }
            
            if (rayHit)
            {
                // Invalidate hits for back-faces within one-way platforms.
                if (rayHit && hitOneWayPlatform(mRaycastHit))
                {
                    rayHit = !hitBackface(forwardRay.origin, forwardRay.direction, mRaycastHit) && 
                             !hitFromBottom(forwardRay.origin, forwardRay.direction, mRaycastHit);
                }
                else
                { rayHit = true; }
            }
            
            if (rayHit)
            {
                // set our new deltaMovement and recalculate the rayDistance taking it into account
                deltaMovement.y = mRaycastHit.point.y - forwardRay.origin.y;
                rayDistance = Mathf.Abs( deltaMovement.y );

                // remember to remove the skinWidth from our deltaMovement
                if (isGoingUp)
                {
                    deltaMovement.y -= mSkinWidth;
                    collisionState.above = true;
                }
                else
                {
                    deltaMovement.y += mSkinWidth;
                    collisionState.below = true;
                }

                mRaycastHitsThisFrame.Add( mRaycastHit );

                // this is a hack to deal with the top of slopes. if we walk up a slope and reach the apex we can get in a situation
                // where our ray gets a hit that is less then skinWidth causing us to be ungrounded the next frame due to residual velocity.
                if (!isGoingUp && deltaMovement.y > 0.00001f)
                { mIsGoingUpSlope = true; }

                // we add a small fudge factor for the float operations here. if our rayDistance is smaller
                // than the width + fudge bail out because we have a direct impact
                if (rayDistance < mSkinWidth + kSkinWidthFloatFudgeFactor)
                { break; }
            }
        }
    }


    /// <summary>
    /// checks the center point under the BoxCollider for a slope. If it finds one then the deltaMovement is adjusted so that
    /// the player stays grounded and the slopeSpeedModifier is taken into account to speed up movement.
    /// </summary>
    /// <param name="deltaMovement">Delta movement.</param>
    private void handleVerticalSlope( ref Vector3 deltaMovement )
    {
        // slope check from the center of our collider
        var centerOfCollider = ( mRaycastOrigins.bottomLeft.x + mRaycastOrigins.bottomRight.x ) * 0.5f;
        var rayDirection = -Vector2.up;

        // the ray distance is based on our slopeLimit
        var slopeCheckRayDistance = _slopeLimitTangent * ( mRaycastOrigins.bottomRight.x - centerOfCollider );

        var forwardRay = new Ray( 
            new Vector3( centerOfCollider, mRaycastOrigins.bottomLeft.y, mRaycastOrigins.bottomLeft.z), 
            rayDirection
        );
        var rayHit = false;
        var backwardRay = new Ray( 
            new Vector3( centerOfCollider, mRaycastOrigins.bottomLeft.y, mRaycastOrigins.bottomLeft.z), 
            -rayDirection
        );
        var unstuckRayHit = false;
        
        rayHit = Physics.Raycast(forwardRay, out mRaycastHit, slopeCheckRayDistance, platformMask);
        unstuckRayHit = Physics.Raycast(backwardRay, out mUnstuckRaycastHit, slopeCheckRayDistance, platformMask);

        if (unstuckRayHit)
        {
            //deltaMovement = slopeRay - _unstuckRaycastHit.point;
            //return;
        }
        
        if( rayHit )
        {
            // bail out if we have no slope
            var angle = Vector2.Angle( mRaycastHit.normal, Vector2.up );
            if( angle == 0 )
                return;

            // we are moving down the slope if our normal and movement direction are in the same x direction
            var isMovingDownSlope = Mathf.Sign( mRaycastHit.normal.x ) == Mathf.Sign( deltaMovement.x );
            if( isMovingDownSlope )
            {
                // going down we want to speed up in most cases so the slopeSpeedMultiplier curve should be > 1 for negative angles
                var slopeModifier = slopeSpeedMultiplier.Evaluate( -angle );
                // we add the extra downward movement here to ensure we "stick" to the surface below
                deltaMovement.y += mRaycastHit.point.y - forwardRay.origin.y - MSkinWidth;
                deltaMovement = new Vector3( 0, deltaMovement.y, 0 ) +
                                ( Quaternion.AngleAxis( -angle, Vector3.forward ) * new Vector3( deltaMovement.x * slopeModifier, 0, 0 ) );
                collisionState.movingDownSlope = true;
                collisionState.slopeAngle = angle;
            }
        }
    }

    #endregion
}
