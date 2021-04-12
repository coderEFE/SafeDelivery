using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseEnemy : EnemyMovement {
  // Start is called before the first frame update
	void Start() {
    player = GameObject.Find("Player").GetComponent<PlayerMovement>();
		if (GameObject.Find("LittleGuy") != null) littleGuy = GameObject.Find("LittleGuy").GetComponent<GuyMovement>();
	}

	// Update is called once per frame
	void Update() {
		//Debug.Log(stunTime);
		//update some vars
		checkEdge = Physics2D.Raycast(new Vector2(body.transform.position.x + (isFacingRight ? 0.5f : -0.5f), body.transform.position.y), -body.transform.up, 1f, groundLayers);
		checkSide = Physics2D.Raycast(new Vector2(body.transform.position.x + (isFacingRight ? 0.5f : -0.5f), body.transform.position.y), body.transform.right, 0.1f, groundLayers);

		RaycastHit2D inSight = Physics2D.Raycast(body.transform.position, ((Vector2)player.transform.position - (Vector2)body.transform.position).normalized, meleeFollowRadius, groundLayers);
		if (Vector2.Distance(body.transform.position, player.transform.position) < meleeFollowRadius && inSight.collider == null) {
			AIState = States.Following;
		} else if (Vector2.Distance(body.transform.position, player.transform.position) > outOfFollowRadius) {
			AIState = States.Patrolling;
		}
	}

	void FixedUpdate () {
		rb.AddForce(gravity);

		//Enemy can do a "kip up" when knocked on its side. This move jumps and rotates the enemy so that it is facing the right direction.
		//Debug.Log(rb.rotation);
		if (rb.rotation >= 20f || rb.rotation <= -20f) {
			flipped = true;
			if (IsGrounded()) {
				if (Vector2.Distance(rb.velocity, new Vector2()) < 0.1f) {
					rb.velocity = new Vector2(rb.velocity.x, jumpForce);
				}
			} else {
				rb.rotation = Mathf.SmoothDamp(rb.rotation, 0f, ref kipupVelocity, kipupTime);
			}
		} else {
			flipped = false;
		}

		//Debug.Log(checkEdge.collider != null || checkSide.collider == null);
		//patrol platform if too far from player and not following
		if (!flipped && AIState == States.Patrolling && IsGrounded()) {
			speed = 3f;
			if (checkEdge.collider != null && checkSide.collider == null) {
				//Debug.Log("ground");
				if (isFacingRight) {
					rb.velocity = new Vector2(speed, rb.velocity.y);
				} else {
					rb.velocity = new Vector2(-speed, rb.velocity.y);
				}
			} else {
				Debug.Log("not ground");
				isFacingRight = !isFacingRight;
				body.transform.localScale = new Vector3(-body.transform.localScale.x, body.transform.localScale.y, body.transform.localScale.z);
			}
		}

		//follow player
		if (!flipped && AIState == States.Following) {
			speed = 5f;
			isFacingRight = player.transform.position.x >= body.transform.position.x;
			if (checkEdge.collider != null) {
				if (player.transform.position.x > body.transform.position.x) {
					rb.velocity = new Vector2(speed, rb.velocity.y);
				} else if (player.transform.position.x < body.transform.position.x) {
					rb.velocity = new Vector2(-speed, rb.velocity.y);
				}
			}
			//attack player with melee
			if (Vector2.Distance(body.transform.position, player.transform.position) < canMeleeRadius && timeUntilAttack < Time.time) {
				Slash();
				timeUntilAttack = Time.time + attackRate;
			}
		}

	}
}
