using UnityEngine;
using System.Collections;

public class FallingPlatform : MonoBehaviour {

	// How much time after the player lands on the platform should it fall
	public float fallDelay;
	// How fast should the platform accelerate when falling
	public float fallingAcceleration;

	float currentVelocity = 0;
	bool isFalling = false, playerLanded = false;
	float timeUntilFall;
	
	// Update is called once per frame
	void Update () {
		if (isFalling) {
			currentVelocity += fallingAcceleration * Time.deltaTime;
			// Now move the transform along the y-axis
			Vector3 tmp = transform.position;
			tmp.y -= currentVelocity * Time.deltaTime;
			transform.position = tmp;
		} else if (playerLanded) {
			// Oho the player has landed, his living days are running out
			timeUntilFall -= Time.deltaTime;
			if (timeUntilFall <= 0) {
				// Time to drop
				isFalling = true;
			}
		}
	}

	void OnCollisionEnter (Collision collision) {
		if (collision.collider.tag == "Player") {
			// We gotsta fall
			timeUntilFall = fallDelay;
			playerLanded = true;
		}
	}
}
