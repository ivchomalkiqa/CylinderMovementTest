using UnityEngine;
using System.Collections;
using UnityEditor;

public class PlayerMovement : MonoBehaviour {

	enum Direction {Left, Right, Stopped};

	public float jumpVelocity;
	// The script sets the global gravity of the physics engine to this value
	public float gravityAcceleration;
	public float maxVerticalVelocity;

	public float movingSpeed;
	// This is the position of the column, around which the player is moving
	public Vector3 columnPosition;
	// This is the horizontal movement direction of the player
	Direction movementDirection;
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
	// These are used to keep track of which collider we are currently on top of, 
	// and which one we are hitting from the side
	Collider floor, wall;

	float currentVerticalVelocity;

	//Number of jumps allowed
	public int maxJumps;
	private int jumps;

	void Start () {
		// Initally the player is not moving
		movementDirection = Direction.Stopped;

		boxCollider = GetComponent<BoxCollider> ();
		if (boxCollider == null) {
			Debug.LogError ("Could not find a collider in PlayerMovement.cs!");
		}

		//Initializing number of jumps to 0
		jumps = 0;

		currentVerticalVelocity = 0;
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
		if (Input.GetAxis ("Horizontal") > 0 && movementDirection != Direction.Right) {
			movementDirection = Direction.Right;
			collidedInFront = false;	// Set this to false to solve the problem where
									// a player cannot reverse once hit an obstacle in front (probably won't be necessary in the real game)
		} else if (Input.GetAxis ("Horizontal") < 0 && movementDirection != Direction.Left) {
			movementDirection = Direction.Left;
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
			currentVerticalVelocity = jumpVelocity;
			landed = false;		// We jumped, what'd you expect!
			shouldJump = false;
		}
		if (!landed) {
			AddGravity ();
		}
		// Move the player if it is not colliding with something in the front
		if (!collidedInFront && movementDirection != Direction.Stopped) {	
			MovePlayer(movementDirection, movingSpeed, Time.deltaTime);
		}
	}

	// Accelerate the player down due to the force of gravity
	void AddGravity () {
		// Accelerate vertically due to gravity
		currentVerticalVelocity -= gravityAcceleration * Time.deltaTime;
		if (currentVerticalVelocity > maxVerticalVelocity) {
			currentVerticalVelocity = maxVerticalVelocity;
		}
		// Update position
		Vector3 tmp = transform.position;
		tmp.y += currentVerticalVelocity * Time.deltaTime;
		transform.position = tmp;
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

	// We are using triggers now. This method is called for every other collider
	// that enters this trigger (The collider which is a component of this game object
	// and is set to trigger).
	void OnTriggerEnter(Collider other) {
		Vector3 closestPoint;
		Vector3 faceNormal;
		GenerateCollisionInformation (out closestPoint, out faceNormal, other);
		if (CollidedFromBelow (faceNormal)) {
			landed = true;
			floor = other;
			FixOverlap (closestPoint, faceNormal);
		} else if (CollidedFromTheFrontSide (faceNormal)) {
			collidedInFront = true;
			wall = other;
		} else if (CollidedFromAbove (faceNormal) && currentVerticalVelocity > 0) {
			currentVerticalVelocity = 0;
		}
	}

	void OnTriggerExit (Collider other) {
		if (other == floor) {
			// We have separated from the floor, i.e. we have jumped
			landed = false;
			floor = null;
		} else if (other == wall) {
			collidedInFront = false;
			wall = null;
		}
	}

	// This method finds the point on the other collider's bounds that is closest to this gameObject,
	// and also determines the normal of the local face which collided with the other collider.
	// The method also returns false or true depending if a normal was found or not.
	bool GenerateCollisionInformation (out Vector3 closestPointOnOther, out Vector3 faceNormal, 
	                                   Collider other) {
		// Find the closest point to our centre on the other collider that we have collided with
		closestPointOnOther = GetClosestPoint (other);
		// The problem with raycasting is that if we start the ray from within our collider, it will
		// not detect our collider. And there's a very very good chance that when this method gets called
		// our collider and the other collider will be overlapping, which means that the closestPointOnOther
		// will be within us... problem. We need to find a point outside our collider on the same line as the
		// line defined by the centre of us and the closestPointOnOther
		Vector3 rayOrigin = closestPointOnOther + (closestPointOnOther - transform.position);

		// Construct the ray and see which of our collider's faces it hits
		Ray myRay = new Ray (rayOrigin, transform.position - closestPointOnOther);	
		RaycastHit rayHit;

		if (Physics.Raycast (myRay, out rayHit, 10, 1 << 8)) {
			// If the ray hit the player
			faceNormal = rayHit.normal;	// Get the normal

			//Debug.DrawLine (myRay.origin, rayHit.point, Color.white, 5f);
			Debug.DrawRay (closestPointOnOther, Vector3.up, Color.cyan, 5f);
			//Debug.DrawRay (myRay.origin, myRay.direction, Color.yellow, 5f);
			//Debug.Log (boxCollider.transform.InverseTransformDirection (faceNormal));
			//EditorApplication.isPaused = true;	// These 4 lines are all debug code
			return true;
		} else {
			faceNormal = Vector3.zero;
			return false;
		}
	}

	// This is a cheat really. It returns a close point, but not the closest
	// Wtv. Since unity's closestPointOnBounds works with axis-oriented bounding boxes I have no other choice
	Vector3 GetClosestPoint (Collider other) {
		// The basic idea is to shoot a ray in every direction, until we hit the collider
		// specified in the argument. The place where we hit is the place where we have
		// the closest point.
		// Shoot down.
		RaycastHit hit;
		if (CastRayFromCenter (Vector3.down, out hit) == other) {
			return hit.point;
		} else if (CastRayFromCenter (Vector3.right, out hit) == other) {
			return hit.point;
		} else if (CastRayFromCenter (Vector3.left, out hit) == other) {
			return hit.point;
		} else if (CastRayFromCenter (Vector3.up, out hit) == other) {
			return hit.point;
		} else {
			return Vector3.zero;
		}
	}

	Collider CastRayFromCenter (Vector3 direction, out RaycastHit hit) {
		// Find the point in the centre of the back face of the player
		Vector3 origin = transform.position;
		Ray myRay = new Ray (origin, transform.TransformDirection (direction));
		Physics.Raycast (myRay, out hit);
		return hit.collider;
	}

	// When this collider collides with another one and the trigger
	// is fired, the two colliders might be overlapping a bit.
	// This mathod un-overlaps the player from the other collider.
	void FixOverlap (Vector3 closestPoint, Vector3 localFaceNormal) {
		// Move us in opposite direction of the face normal until the closest
		// point of the other collider coincides with the edge of the player (they are just touching)

		// This is the projection of the extend vector defining half of the size of the player's box, along the normal
		// In other words, this calculation determines half of the length of the box in the direction of the normal
		float widthAlongNormal = Mathf.Abs (Vector3.Dot (boxCollider.bounds.extents, localFaceNormal));
		// This is the projection of the vector from the centre of the player to the closestPoint on the other collider, along the normal
		// In other words, this is the length in direction of the normal between the centre of the transform up to the closestPoint
		float distanceToClosestPointAlongNormal = Mathf.Abs (Vector3.Dot (closestPoint - transform.position, localFaceNormal));
		// The difference between these two projectsion is how much we should move along in the normal direction to remove any overlaps
		Vector3 overlapCompensation = (distanceToClosestPointAlongNormal - widthAlongNormal) * localFaceNormal;
		// Apply compensation to transform
		Vector3 tmp = transform.position;
		tmp += overlapCompensation;
		transform.position = tmp;
	}

	// Returns true if the local face normal corresponds
	// the the bottom face of the player
	bool CollidedFromBelow (Vector3 faceNormal) {
		// Transform the face normal from global space coordinates into local space coordinates
		Vector3 localFaceNormal = transform.InverseTransformDirection (faceNormal);
		return localFaceNormal == Vector3.down;
	}

	// Returns true if this collider has hit something
	// from the front
	bool CollidedFromTheFrontSide (Vector3 faceNormal) {
		Vector3 localFaceNormal = transform.InverseTransformDirection (faceNormal);
		return localFaceNormal == GetForwardVector ();
	}

	// Returns a unit vector pointing in the direction of movement of the player 
	// IN LOCAL COORDINATE SYSTEM FOR THE PLAYER (i.e. returns Vector.right or Vector.left)
	Vector3 GetForwardVector () {
		if (movementDirection == Direction.Left) {
			return Vector3.left;
		} else {
			return Vector3.right;
		}
	}

	// Returns true if this collider has hit something
	// above it.
	bool CollidedFromAbove (Vector3 faceNormal) {
		// Transform the face normal from global space coordinates into local space coordinates
		Vector3 localFaceNormal = transform.TransformDirection (faceNormal);
		return localFaceNormal == Vector3.up;
	}
}
