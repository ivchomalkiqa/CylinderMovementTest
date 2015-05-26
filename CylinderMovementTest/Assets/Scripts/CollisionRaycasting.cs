using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollisionRaycasting : MonoBehaviour {

	// How far should the rays shoot when detecting collisions
	public float horizontalRayDistance, verticalRayDistance;

	// Shows the rays in the editor for debugging
	public bool showRays;

	public bool collidedAbove;
	public bool collidedBelow;
	public bool collidedLeft;
	public bool collidedRight;

	// If there is a collision, this is the position of that collision
	public Vector3 collisionPointAbove;
	public Vector3 collisionPointBelow;
	public Vector3 collisionPointLeft;
	public Vector3 collisionPointRight;

	// Save only the collider below for now, to allow classes that use CollisionRaycasting.cs
	// to communicate with the platforms underneath.
	public Collider colliderBelow;

	RaycastHit hit;

	// These game objects are children of this gameobject with the same names as the 
	// arrays that they will appear in. These indicate the starting positions for the
	// rays shooting in the 4 directions: up, down, left and right
	List<Transform> up = new List<Transform> ();
	List<Transform> down = new List<Transform> ();
	List<Transform> left = new List<Transform> ();
	List<Transform> right = new List<Transform> ();

	// Use this for initialization
	void Start () {
		// Go through the transforms of the children of this game object and assign
		// them to the appropriate list according to their name
		foreach (Transform t in transform) {
			if (t.name == "Up") {
				up.Add (t);
			} else if (t.name == "Down") {
				down.Add (t);
			} else if (t.name == "Left") {
				left.Add (t);
			} else if (t.name == "Right") {
				right.Add (t);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (showRays) {
			DrawDebugRays ();
		}
	}

	// Draws lines representing the rays and their maximum length that will be used for collision raycasting
	// by this script
	void DrawDebugRays () {
		foreach (Transform t in up) {
			Debug.DrawLine (t.position, t.position + (verticalRayDistance * Vector3.up), Color.green);
		}
		foreach (Transform t in down) {
			Debug.DrawLine (t.position, t.position + (verticalRayDistance * Vector3.down), Color.green);
		}
		foreach (Transform t in left) {
			// Since the player rotates around the y-axis, we will need to rotate the left vector
			Debug.DrawLine (t.position, t.position + 
			                (horizontalRayDistance * (transform.rotation * Vector3.left)), Color.green);
		}
		foreach (Transform t in right) {
			// Since the player rotates around the y-axis, we will need to rotate the left vector
			Debug.DrawLine (t.position, t.position + 
			                (horizontalRayDistance * (transform.rotation * Vector3.right)), Color.green);
		}
	}

	public bool CheckForCollisions () {
		// Reset all flags
		collidedAbove = false;
		collidedBelow = false;
		collidedLeft = false;
		collidedRight = false;

		// Reset the collision points to be as far from the player as possible
		collisionPointAbove.y = Mathf.Infinity;
		collisionPointBelow.y = - Mathf.Infinity;

		bool hasCollisions = false;

		// Do collisions up
		foreach (Transform t in up) {
			Vector3 direction = Vector3.up;
			if (Physics.Raycast (t.position, direction, out hit, verticalRayDistance) &&
			    	collisionPointAbove.y >= hit.point.y) {
				// If we get a hit, and it is closer to the player than previous hits
				hasCollisions = true;
				collidedAbove = true;
				collisionPointAbove = hit.point;	// assign this hit as the newest closest hit from above
			}
		}
		// Do collisions down
		foreach (Transform t in down) {
			Vector3 direction = Vector3.down;
			if (Physics.Raycast (t.position, direction, out hit, verticalRayDistance) &&
			    	collisionPointBelow.y <= hit.point.y) {
				// If we get a hit, and it is closer to the player than previous hits
				hasCollisions = true;
				collidedBelow = true;
				collisionPointBelow = hit.point;	// assign this hit as the newest closest hit from below
				// Also save the collider below
				colliderBelow = hit.collider;
			}
		}
		// Do collisions right
		foreach (Transform t in right) {
			Vector3 direction = transform.rotation * Vector3.right;
			if (Physics.Raycast (t.position, direction, out hit, horizontalRayDistance)) {
				hasCollisions = true;
				collidedRight = true;
				collisionPointRight = hit.point;
				break;	// Stop looking at the first intersection of the ray that we find
			}
		}
		// Do collisions left
		foreach (Transform t in left) {
			Vector3 direction = transform.rotation * Vector3.left;
			if (Physics.Raycast (t.position, direction, out hit, horizontalRayDistance)) {
				hasCollisions = true;
				collidedLeft = true;
				collisionPointLeft = hit.point;
				break;	// Stop looking at the first intersection of the ray that we find
			}
		}
		return hasCollisions;
	}
}
