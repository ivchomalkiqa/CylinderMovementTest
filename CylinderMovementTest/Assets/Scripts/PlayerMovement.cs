using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

	enum Direction {Left, Right, Stopped};

	public float jumpVelocity;
	// The script sets the global gravity of the physics engine to this value
	public float gravityAcceleration;
	// This is the terminal velocity for the player
	public float maxVerticalVelocity;
	// The sideway speed of the player
	public float movingSpeed;
	// This is the position of the column, around which the player is moving
	public Vector3 columnPosition;
	public Vector3 playerExtents;

	// This is the horizontal movement direction of the player
	Direction movementDirection;
	// If the "Jump" button is pressed down, this flag is true and
	// indicates that the player should jump as soon as he is allowed
	bool shouldJump = false;
	// Turns true when the player lands on a platform, 
	// thus allowing the player to jump
	bool landed = false;

	float currentVerticalVelocity;

	// A reference to the raycasting script, which will detect the side on which we collide
	CollisionRaycasting cr;

	//Number of jumps allowed
	public int maxJumps;
	private int jumps;

	void Start () {
		// Initally the player is not moving
		movementDirection = Direction.Stopped;

		// Get a reference to the ray casting script
		cr = GetComponent <CollisionRaycasting> ();

		//Initializing number of jumps to 0
		jumps = 0;

		currentVerticalVelocity = 0;
	}

	void Update () {
		// TODO Delete this if this script is not the main controller script for the application
		if (Input.GetKey (KeyCode.Escape)) {
			Application.Quit ();
		}

		// Check if the user pressed the jump button
		if (Input.GetButtonDown ("Jump")) {
			shouldJump = true;

		} else if (Input.GetButtonUp ("Jump")) {
			// When the button is released, reset the jumping flag
			shouldJump = false;
		}
		// Check if the user pressed the left or right arrow keys, or axis, wtv.
		if (Input.GetAxis ("Horizontal") > 0 && movementDirection != Direction.Right) {
			movementDirection = Direction.Right;
		} else if (Input.GetAxis ("Horizontal") < 0 && movementDirection != Direction.Left) {
			movementDirection = Direction.Left;
		}
		// Restart if player falls off
		if (transform.position.y < -10) {
			Application.LoadLevel(Application.loadedLevel);
		}
	}

	void FixedUpdate () {
		// Do collision detection
		cr.CheckForCollisions ();

		// Treat the jumps before resolving vertical collisions, because the code that physically moves the 
		// player is executed when the ResolveVerticalCollisions method is called
		if (shouldJump && jumps < maxJumps) {// Jump only if number of jumps done in the air are less than maximum allowed jumps
			jumps++;
			currentVerticalVelocity = jumpVelocity;
			landed = false;		// We jumped, what'd you expect!
			shouldJump = false;
		}

		ResolveVerticalCollisions (Time.deltaTime);

		// JUMP code
		//if (shouldJump && landed && !collidedInFront) { // Jump only if on the ground and not colliding in front


		ResolveHorizontalCollisions (Time.deltaTime);
	}


	// Deals with resolving all collisions and solving potential overlaps of the
	// player and obstacles in the vertical direction
	void ResolveVerticalCollisions (float deltaTime) {
		// Deal with collisions from above
		if (WillHitCeilingThisUpdate (deltaTime)) {
			// If we collide above, simply set the vertical speed to 0
			currentVerticalVelocity = 0;
			// Also avoid overlapping by positioning the player vertically to touch the obstacle
			transform.position = new Vector3 (transform.position.x,
			                                  cr.collisionPointAbove.y - playerExtents.y,
			                                  transform.position.z);
		} 

		// Deal with collisions from below
		if (landed) {
			if (!cr.collidedBelow) {
				landed = false;
				AddGravityAndMoveVertically (deltaTime);
			} else {
				// Do nothing, we are still on the platform
			}
		} else {
			// We have not landed and we are approaching surface from below, check if we will
			// land on it this update or not.
			if (WillLandThisUpdate (deltaTime)) {
				// We are landing now!
				landed = true;
				currentVerticalVelocity = 0;
				jumps = 0;
				// Set the position of the player to be on top of the platform on which we have landed
				transform.position = new Vector3 (transform.position.x,
				                                  cr.collisionPointBelow.y + playerExtents.y,
				                                  transform.position.z);
			} else {
				// We will not land this update, so just add gravity as usual
				AddGravityAndMoveVertically (deltaTime);
			}
		}
	}

	bool WillHitCeilingThisUpdate (float deltaTime) {
		if (!cr.collidedAbove) {
			return false;
		}
		// See if we will move past the projected point of collision 
		// above in this frame
		float heighestPointAfterMovement = transform.position.y 
			+ (currentVerticalVelocity * deltaTime)
				+ playerExtents.y;
		return heighestPointAfterMovement >= cr.collisionPointAbove.y;
	}

	bool WillLandThisUpdate (float deltaTime) { 
		if (!cr.collidedBelow) {
			return false;
		}
		// See if this frame we will move past the projected point of collision
		// under us in this frame
		float lowestPointAfterMovement = transform.position.y 
			+ (currentVerticalVelocity * deltaTime)
				- playerExtents.y;
		return lowestPointAfterMovement <= cr.collisionPointBelow.y;
	}

	// Accelerates the player down using the specified gravity acceleration
	void AddGravityAndMoveVertically (float deltaTime) {
		// Accelerate vertically due to gravity
		currentVerticalVelocity -= gravityAcceleration * deltaTime;
		if (currentVerticalVelocity > maxVerticalVelocity) {
			currentVerticalVelocity = maxVerticalVelocity;
		}

		// Update position
		Vector3 tmp = transform.position;
		tmp.y += currentVerticalVelocity * deltaTime;
		transform.position = tmp;
	}

	void ResolveHorizontalCollisions (float deltaTime) {
		// Check only for the direction that we are moving in
		if ((movementDirection == Direction.Right && cr.collidedRight) ||
		    (movementDirection == Direction.Left && cr.collidedLeft)) {
		} else {
			MovePlayer (movementDirection, movingSpeed, deltaTime);
		}
	}

	// Moves the player left or right on a cirular arc around the cylinder/pole/level
	// whose center is provided by the vector columnPosition.
	void MovePlayer (Direction direction, float speed, float deltaTime) {
		// Radius is the vector between player's position and center of circle, 
		// but disregard the y-axis (we are looking at a 2D projection from the top)
		Vector2 radius = new Vector2 (columnPosition.x - transform.position.x,
		                              columnPosition.z - transform.position.z);
		float distanceTravelled = speed * deltaTime;	// How much distance have we passed, 
		// remember, this is distance along the arc or arclength

		// Now we find what is the angle between our old position and the new one
		float angleTravelled = (distanceTravelled * 360) / 
			(2 * Mathf.PI * radius.magnitude);
		// Rotate around the centre of the arc along which we are moving (cylinder centre)
		if (direction == Direction.Left) {
			transform.RotateAround (columnPosition, Vector3.up, angleTravelled);
		} else if (direction == Direction.Right) {
			transform.RotateAround (columnPosition, Vector3.up, -angleTravelled);
		}
	}
}
