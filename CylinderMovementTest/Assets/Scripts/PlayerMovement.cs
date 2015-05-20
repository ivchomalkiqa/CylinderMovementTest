using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

	enum Direction {Left, Right, Stopped};

	public float jumpForce;
	// The script sets the global gravity of the physics engine to this value
	public float gravity;

	public float movingSpeed;
	// This is the position of the column, around which the player is moving
	public Vector3 columnPosition;
	// Reference to the rigidbody component
	Rigidbody rb;
	// This is the horizontal movement direction of the player
	Direction dir;
	// If the "Jump" button is pressed down, this flag is true and
	// indicates that the player should jump as soon as he is allowed
	bool shouldJump = false;
	// Turns true when the player lands on a platform, 
	// thus allowing the player to jump
	bool landed = false;
	// If this flag turns true, it would stop the sideways
	// movement, since the player has collided with something in the front
	bool collidedInFront = false;
	// Reference to the Collider for purposes of determining its size
	BoxCollider boxCollider;

	//Number of jumps allowed
	public int maxJumps;
	private int jumps;

	void Start () {
		rb = GetComponent<Rigidbody> ();
		// Set the physics engine's gravity
		Physics.gravity = new Vector3(0, -gravity, 0);
		// Initally the player is not moving
		dir = Direction.Stopped;

		boxCollider = GetComponent<BoxCollider> ();
		if (boxCollider == null) {
			Debug.LogError ("Could not find a collider in PlayerMovement.cs!");
		}

		//Initializing number of jumps to 0
		jumps = 0;
	}

	void Update () {
		// Check if the user pressed the jump button
		if (Input.GetButtonDown ("Jump")) {
			jumps++;
			shouldJump = true;

		} else if (Input.GetButtonUp ("Jump")) {
			// When the button is released, reset the jumping flag
			shouldJump = false;
		}
		// Check if the user pressed the left or right arrow keys, or axis, wtv.
		if (Input.GetAxis ("Horizontal") > 0 && dir != Direction.Right) {
			dir = Direction.Right;
			collidedInFront = false;	// Set this to false to solve the problem where
									// a player cannot reverse once hit an obstacle in front (probably won't be necessary in the real game)
		} else if (Input.GetAxis ("Horizontal") < 0 && dir != Direction.Left) {
			dir = Direction.Left;
			collidedInFront = false;
		}
		// Restart if player falls off
		if (transform.position.y < -10) {
			Application.LoadLevel(Application.loadedLevel);
		}
	}

	void FixedUpdate () {
		if(landed){
			jumps = 0;
		}
		//if (shouldJump && landed && !collidedInFront) { // Jump only if on the ground and not colliding in front
		if (shouldJump && jumps <= maxJumps) {// Jump only if number of jumps done in the air are less than maximum allowed jumps
			rb.AddForce (Vector3.up * jumpForce); // Jump up with the specified force
			landed = false;		// We jumped, what'd you expect!
			shouldJump = false;
		}
		// Move the player if it is not colliding with something in the front
		if (!collidedInFront && dir != Direction.Stopped) {	
			MovePlayer(dir, movingSpeed, Time.deltaTime);
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

	void OnCollisionStay (Collision collision) {
		// Check whether the player has landed on a platform
		// in OnCollisionStay, to make sure that all vertical
		// velocity that the player might have had has been lost
		// after the physics engine updates the collision
		if (CollidedFromBelow (collision)) {
			// We have landed on something
			landed = true;
		} else {
			landed = false;
		}
		if (CollidedFromTheFrontSide (collision)) {
			collidedInFront = true;
		}
	}

	void OnCollisionEnter (Collision collision) {
		if (CollidedFromTheFrontSide (collision)) {
			collidedInFront = true;
		}
	}

	void OnCollisionExit (Collision collision) {
		collidedInFront = false;
		landed = false;
	}

	// Returns true if this collider has hit something
	// from the front
	bool CollidedFromTheFrontSide (Collision collision) {
		bool result = false;
		float playerLowestY = transform.position.y - boxCollider.bounds.extents.y;
		float playerHighestY = transform.position.y + boxCollider.bounds.extents.y;
		// Check every contact point
		foreach (ContactPoint cp in collision.contacts) {
			if (Vector3.Dot (GetForwardVector (), cp.normal) < 0	// This dot product determines whether the collision point is in the direction of movement
			    && cp.point.y >= playerLowestY && cp.point.y <= playerHighestY) {	// This determines whether the collision is on the side, and not on the top or bottom of the box
				result = true;
				break;
			}
		}
		return result;
	}

	// This is like CollidedFromFrontSide, but
	// it detects hits from both sides
//	bool CollidedFromTheSide (Collision collision) {
//		bool result = false;
//		// Check every contact point
//		foreach (ContactPoint cp in collision.contacts) {
//			if (Mathf.Abs (Vector3.Dot(Vector3.up, cp.normal)) < 0.005f) {	// This determines whether the collision is on the side, and not on the top or bottom of the box
//				result = true;
//				break;
//			}
//		}
//		return result;
//	}

	// Returns a unit vector pointing in the direction of movement of the player.
	Vector3 GetForwardVector () {
		Vector3 forward = Vector3.right;
		if (dir == Direction.Left) {
			forward *= -1;	// Well fine, if we are not moving right, I am flipping the vector.
		}
		// Now rotate the forward vector to match player's current rotation
		forward = transform.rotation * forward;
		return forward;
	}

	// Returns true if this collider has hit something
	// underneath it, such as the player landing on a platform.
	// This method works because the player only rotates around the y-axis.
	bool CollidedFromBelow (Collision collision) {
		bool result = false;
		// For a hit to be from below, its collision point has to have a y component
		// lower than the lowest point on the player
		float playerLowestY = transform.position.y - boxCollider.bounds.extents.y;
		foreach (ContactPoint cp in collision.contacts) {
			if (cp.point.y <= playerLowestY) {
				result = true;
				break;
			}
		}
		return result;
	}

	// Returns true if this collider has hit something
	// above it, such as the player landing on a platform.
	// This method works because the player only rotates around the y-axis.
	bool CollidedFromAbove (Collision collision) {
		bool result = false;
		// For a hit to be from below, its collision point has to have a y component
		// lower than the lowest point on the player
		float playerHighestY = transform.position.y + boxCollider.bounds.extents.y;
		
		foreach (ContactPoint cp in collision.contacts) {
			if (cp.point.y >= playerHighestY) {
				result = true;
				break;
			}
		}
		return result;
	}
}
