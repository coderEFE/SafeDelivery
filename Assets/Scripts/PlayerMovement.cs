using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
	public float movementSpeed = 10;
	public Rigidbody2D rb;
	float mx;

	public Animator anim;

	public float jumpForce = 20f;
	//TODO: change double jumping to a jet pack mechanic that steadily accelerates
	public int jumpsAvailable = 1;
	int jumpsLeft;
	public Transform feet;
	public LayerMask groundLayers;
	public Transform playerSight;
	public RaycastHit2D groundBelow;
	public RaycastHit2D ceilingAbove;
	public RaycastHit2D hitGround;
	public RaycastHit2D hitCeiling;
	//return if player is colliding with any object
	public bool colliding;

	public bool facingRight = true;

	Vector2 gravity = new Vector2(0f, -40f);

	// Start is called before the first frame update
	void Start() {
		jumpsLeft = jumpsAvailable;
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

		mx = Input.GetAxisRaw("Horizontal");
		if (Input.GetButtonDown("Jump")) {
			if (IsGrounded()) {
				jumpsLeft = jumpsAvailable;
				Jump();
			}
			if (jumpsLeft > 0) {
				jumpsLeft--;
				Jump();
			}
		}

		//flip horizontally
		if (mx > 0) {
			facingRight = true;
		} else if (mx < 0) {
			facingRight = false;
		}
		transform.localScale = new Vector3(facingRight ? 1f : -1f, transform.localScale.y, transform.localScale.z);

		//flip vertically
		/*if (gravity.y < 0 && transform.localScale.y == -1f) {
		   transform.localScale *= new Vector3(1f, 1f, 1f);
		   } else if (gravity.y > 0 && transform.localScale.y == 1f) {
		   transform.localScale *= new Vector3(1f, -1f, 1f);
		   }*/

		//TODO: change isGrounded criteria for player animation
		anim.SetBool("IsGrounded", rb.velocity.y < 0.05f);

		groundBelow = Physics2D.Raycast(transform.position, -transform.up, 10f, groundLayers);
		ceilingAbove = Physics2D.Raycast(transform.position, transform.up, 10f, groundLayers);
		hitGround = Physics2D.Raycast(transform.position, -transform.up, 1f, groundLayers);
		hitCeiling = Physics2D.Raycast(transform.position, transform.up, 1f, groundLayers);
	}

	private void FixedUpdate() {
		rb.AddForce(gravity);
		Vector2 movement = new Vector2(mx * movementSpeed, rb.velocity.y);

		rb.velocity = movement;
	}

	//apply jumpForce
	void Jump() {
		Vector2 movement = new Vector2(rb.velocity.x, jumpForce * (gravity.y > 0 ? -1f : 1f));

		rb.velocity = movement;
	}

	public bool IsGrounded() {
		Collider2D groundCheck = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

		return groundCheck != null;
	}

	//colliding functions
	void OnCollisionEnter2D (Collision2D collision) {
		colliding = collision.collider != null;
	}
	void OnCollisionExit2D (Collision2D collision) {
		colliding = false;
	}
}
