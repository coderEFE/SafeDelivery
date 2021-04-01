using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
	public float movementSpeed = 10f;
	float currentSpeed = 0f;
	public Rigidbody2D rb;
	float mx;

	public Animator anim;

	public float jumpForce = 20f;
	//TODO: change double jumping to a jet pack mechanic that steadily accelerates
	public int jumpsAvailable;
	int jumpsLeft;
	float jumpTimeCounter = 0f;
	public float maxJumpTime;
	bool isJumping = false;
	float timeInAir = 0f;
	float maxAirTime = 0.1f;
	bool countTime = false;
	public Transform feet;
	public LayerMask groundLayers;
	public Transform playerSight;
	public RaycastHit2D groundBelow;
	public RaycastHit2D ceilingAbove;
	public RaycastHit2D hitGround;
	public RaycastHit2D hitCeiling;
	//return if player is colliding with any object
	public bool colliding;
	bool isGrounded;

	public bool facingRight = true;

	Vector2 gravity = new Vector2(0f, -50f);

	// Start is called before the first frame update
	void Start() {
		jumpsLeft = jumpsAvailable;
		isGrounded = false;
		colliding = false;
	}

	// Update is called once per frame
	void Update() {
		//rb.velocity += gravity;

		//change gravity
		if (Input.GetKeyUp("g")) {
			gravity.y = -gravity.y;
			transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
		}

		//IsGrounded
		/*Collider2D groundCheck = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);
		if (groundCheck != null && !isGrounded) {
			//Debug.Log("on");
			isGrounded = true;
		} else if (groundCheck == null && isGrounded) {
			//Debug.Log("off");
			isGrounded = false;
		}*/
		//TODO: fix bug where player's head colliding resets their jumps left
		mx = Input.GetAxisRaw("Horizontal");
		if (!colliding && !IsGrounded()) {
			timeInAir += Time.deltaTime;
			if (timeInAir > maxAirTime && jumpsLeft > 0 && jumpsLeft == jumpsAvailable) {
				jumpsLeft--;
				//Debug.Log("too long");
			}
		} else if (timeInAir != 0f) {
			timeInAir = 0f;
		}

		if (Input.GetButtonDown("Jump") && !isJumping) {
			//Debug.Log("jump");
			if (IsGrounded()) {
				jumpsLeft = jumpsAvailable;
			}
			if (jumpsLeft > 0) {
				jumpsLeft--;
				//Jump();
				rb.velocity = new Vector2(rb.velocity.x, jumpForce * (gravity.y > 0 ? -1f : 1f));
				jumpTimeCounter = maxJumpTime;
				isJumping = true;
			}
		} else if (Input.GetButtonUp("Jump") && isJumping) {
			isJumping = false;
			/*if (jumpTime < maxJumpTime) {
				rb.velocity = new Vector2(rb.velocity.x, 0f);
			}*/
		}
		//jump is longer if player holds jump button longer
		if (isJumping) {
			//Jump();
			if (jumpTimeCounter > 0) {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce * (gravity.y > 0 ? -1f : 1f));
        jumpTimeCounter -= Time.deltaTime;
      } else {
        isJumping = false;
      }
		}
		//flip horizontally
		if (mx > 0) {
			facingRight = true;
		} else if (mx < 0) {
			facingRight = false;
		}
		transform.localScale = new Vector3(facingRight ? 1f : -1f, transform.localScale.y, transform.localScale.z);

		//TODO: change isGrounded criteria for player animation
		anim.SetBool("IsGrounded", rb.velocity.y < 0.05f);

		//groundBelow = Physics2D.Raycast(transform.position, -transform.up, 10f, groundLayers);
		//ceilingAbove = Physics2D.Raycast(transform.position, transform.up, 10f, groundLayers);
		//hitGround = Physics2D.Raycast(transform.position, -transform.up, 1.1f, groundLayers);
		hitCeiling = Physics2D.Raycast(transform.position, transform.up, 1.1f, groundLayers);
		Debug.Log(colliding);
	}

	private void FixedUpdate() {
		rb.AddForce(new Vector2(gravity.x, (rb.velocity.y > 40f && gravity.y > 0) ? 0f : ((rb.velocity.y < -40f && gravity.y < 0) ? 0f : gravity.y)));
		//rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + gravity.y);
		//accelerate to max speed and de-accelerate to stationary
		if (mx != 0) {
			if (currentSpeed < movementSpeed) {
				currentSpeed += 1f;
			}
		} else {
			if (currentSpeed > 0f) {
				currentSpeed -= 1f;
			} else {
				currentSpeed = 0f;
			}
		}
		Vector2 movement = new Vector2(mx != 0 ? mx * currentSpeed : (facingRight ? currentSpeed : -currentSpeed), rb.velocity.y);

		rb.velocity = movement;
	}

	//apply jumpForce
	//TODO: make a variable duration jump, make player able to jump a few frames after leaving ground, player cannot jump unless it leaves the platform and then comes back
	/*void Jump() {
		//Debug.Log("jump");
		//Vector2 movement = new Vector2(rb.velocity.x, jumpForce * (gravity.y > 0 ? -1f : 1f));
		//rb.velocity = movement;
		//rb.velocity = Vector2.zero;
		//Debug.Log(jumpTime);
		if (jumpTime < maxJumpTime / 2) {
			//Debug.Log("jumping");
			//Calculate how far through the jump we are as a percentage
			//apply the full jump force on the first frame, then apply less force
			//each consecutive frame

			//Vector2 thisFrameJumpVector = Vector2.Lerp(new Vector2(0f, 10f), Vector2.zero, proportionCompleted);
			//Vector2 thisFrameJumpVector = Vector2.Lerp(new Vector2(0f, 20f), new Vector2(0f, 0f), proportionCompleted);
			rb.velocity = new Vector2(0f, 15f);
			//rb.velocity = new Vector2(rb.velocity.x, jumpForce * (gravity.y > 0 ? -1f : 1f));
			//yield return null;
		} else if (jumpTime < maxJumpTime) {
			float proportionCompleted = jumpTime - (maxJumpTime / 2) / (maxJumpTime / 2);
			Vector2 thisFrameJumpVector = Vector2.Lerp(new Vector2(0f, rb.velocity.y), new Vector2(0f, 0f), proportionCompleted);
			rb.velocity = thisFrameJumpVector;
		}
		jumpTime += Time.deltaTime;
		//Vector2 movement = new Vector2(0f, jumpForce * (gravity.y > 0 ? -1f : 1f));
		//rb.AddForce(movement, ForceMode2D.Impulse);
	}*/

	public bool IsGrounded() {
		Collider2D groundCheck = Physics2D.OverlapCircle(feet.position, 0.1f, groundLayers);

		return groundCheck != null;
	}

	//colliding functions
	void OnCollisionEnter2D (Collision2D collision) {
		Debug.Log(colliding);
		if (!colliding && (!hitCeiling || gravity.y > 0) && !collision.gameObject.CompareTag("EnemyBullet")) {
			colliding = collision.collider != null;
			Debug.Log("reset");
			jumpsLeft = jumpsAvailable;
		}
	}
	void OnCollisionExit2D (Collision2D collision) {
		if (colliding) {
			colliding = false;
		}
	}
}
