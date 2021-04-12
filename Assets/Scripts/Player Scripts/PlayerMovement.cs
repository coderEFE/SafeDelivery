using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
	public Transform feet;
	public LayerMask groundLayers;
	public Transform playerSight;
	public Rigidbody2D rb;
	public Animator anim;

	public bool facingRight = true;
	public float movementSpeed = 10f;
	float currentSpeed = 0f;
	float mx;

	bool wallGrab = false;
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
	public RaycastHit2D groundBelow;
	public RaycastHit2D ceilingAbove;
	public RaycastHit2D hitGround;
	public RaycastHit2D hitCeiling;
	//return if player is colliding with any object
	public bool colliding;
	bool isGrounded;
	float maxWallPushTime = 0.5f;
	float wallPushTimer = 0f;

	Vector2 gravity = new Vector2(0f, -50f);

	//attack vars
	public float fireRate = 0.25f;
	public Transform firingPoint;
	float timeUntilFire;
	public Transform attackPoint;
	public float attackRate = 0.25f;
	public float attackRange = 1f;
	public float attackDamage = 10f;
	float timeUntilAttack;
	public LayerMask enemyLayers;
	public LayerMask playerLayer;
	public GameObject bulletPrefab;
	Vector2 axis;

	// Start is called before the first frame update
	void Start() {
		jumpsLeft = jumpsAvailable;
		isGrounded = false;
		colliding = false;
	}

	// Update is called once per frame
	void Update() {
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
				if (wallGrab) {
					//push player away from wall
					RaycastHit2D onLeftWall = Physics2D.Raycast(transform.position, -Vector2.right, 0.6f, groundLayers);
					RaycastHit2D onRightWall = Physics2D.Raycast(transform.position, Vector2.right, 0.6f, groundLayers);
					//Debug.Log(onLeftWall.collider != null ? Vector2.right * 10000f : (onRightWall.collider != null ? -Vector2.right * 10000f : new Vector2()));
					if ((mx > 0 && onRightWall) || (mx < 0 && onLeftWall)) {
						rb.AddForce(onLeftWall.collider != null ? Vector2.right * 13f : (onRightWall.collider != null ? -Vector2.right * 13f : new Vector2()), ForceMode2D.Impulse);
						wallPushTimer = Time.time + maxWallPushTime;
					}
				}
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
		//Debug.Log(colliding);

		//attacking
		axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		if (axis.y == 0) {
			attackPoint.position = new Vector2(transform.position.x + (facingRight ? 1f : -1f), transform.position.y + axis.normalized.y);
		} else {
			attackPoint.position = (Vector2) transform.position + axis.normalized;
		}
		//maybe use Time.deltaTime?
		if (Input.GetMouseButtonDown(0) && timeUntilFire < Time.time) {
			Shoot();
			timeUntilFire = Time.time + fireRate;
		}
		//Debug.Log(Input.GetButtonDown("Fire1") + ", " + Input.GetButtonDown("Fire2") + ", " + Input.GetButtonDown("Fire3"));
		if (Input.GetButtonDown("Fire2") && timeUntilAttack < Time.time) {
			Slash();
			//Debug.Log("slash");
			timeUntilAttack = Time.time + attackRate;
		}
	}

	private void FixedUpdate() {
		//stop acceleration of gravity at a terminal velocity of 40
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
		//Vector2 movement = new Vector2(mx != 0 ? mx * currentSpeed : (facingRight ? currentSpeed : -currentSpeed), rb.velocity.y);
		//rb.velocity = movement;

		/*if (wallPushTimer < Time.time && timeUntilAttack < Time.time) {
			if (mx == 0 && currentSpeed == 0) {
				rb.velocity = new Vector2(0f, rb.velocity.y);
			}
		}*/
		/*if ((rb.velocity.x < 0 ? mx >= 0 : mx <= 0) && Mathf.Abs(rb.velocity.x) > 0) {
			rb.AddForce(new Vector2(rb.velocity.x > 0 ? -70f : 70f, 0f), ForceMode2D.Force);
		}*/
		if ((rb.velocity.x < 0 ? mx >= 0 : mx <= 0)) {
			rb.AddForce(new Vector2(-rb.velocity.x * 9f, 0f), ForceMode2D.Force);
		}
		if (mx != 0 && Mathf.Abs(rb.velocity.x) < currentSpeed) {
			rb.AddForce(new Vector2(mx * 70f, 0f), ForceMode2D.Force);
		}
	}

	//attack with staff
	void Slash() {
		Collider2D[] collidersHit = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
		//TODO: implement other effects on objects from slash, possibly move littleGuy with it
		foreach (Collider2D collider in collidersHit) {
			//slash enemies
			if (collider.gameObject.name.Equals("EnemyBody")) {
				collider.GetComponent<EnemyManager>().currentHealth -= attackDamage;
				//could either apply force coming from transform.position or attackPoint.position
				float maxKnockback = 10f;
				float minRatio = 0.3f;
				float knockbackRatio = (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position));
				float knockback = 0f;
				if (knockbackRatio <= 0.1f) {
					knockback = maxKnockback * minRatio;
				} else if (knockbackRatio >= 0.9f) {
					knockback = maxKnockback;
				} else {
					knockback = maxKnockback * knockbackRatio;
				}
				//float knockback = (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position)) <= 0f ? 0f : (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position));
				//float knockback = (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position));
				Debug.Log(knockback);
				collider.transform.parent.gameObject.GetComponent<EnemyMovement>().SetStunTime(knockback / 10f);
				collider.transform.parent.gameObject.GetComponent<EnemyMovement>().rb.velocity = ((Vector2)(collider.gameObject.GetComponent<EnemyManager>().transform.position - transform.position).normalized * knockback);
			}
		}
		Collider2D colliderHit = Physics2D.OverlapCircle(attackPoint.position, 0.1f, ~playerLayer);
		//slash ground
		if (colliderHit != null) {
			//Debug.Log("pogo");
			float pogoKnockBack = 10f;
			Vector2 pogo = ((Vector2)(transform.position - attackPoint.position)).normalized * pogoKnockBack;
			rb.AddForce(pogo, ForceMode2D.Impulse);
		}
	}
	//shoot with gun
	void Shoot() {
		//TODO: make shooting controls for mobile and controller
		Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		facingRight = (mousePos.x >= transform.position.x);
		transform.localScale = new Vector3(facingRight ? 1f : -1f, transform.localScale.y, transform.localScale.z);
		/*if (mousePos >= transform.position) {
		   facingRight = true;
		   }*/
		float angle = Mathf.Atan((mousePos.y - firingPoint.position.y) / (mousePos.x - firingPoint.position.x));
		//Debug.Log(angle);
		Bullet bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.Euler(new Vector3(0f, 0f, angle))).GetComponent<Bullet>();
		//could set bullet damage here
		Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), this.GetComponent<Collider2D>());
	}

	public bool IsGrounded() {
		Collider2D groundCheck = Physics2D.OverlapCircle(feet.position, 0.1f, groundLayers);

		return groundCheck != null;
	}

	//colliding functions
	void OnCollisionEnter2D (Collision2D collision) {
		//Debug.Log(colliding);
		if (IsGrounded()) {
			jumpsLeft = jumpsAvailable;
		}
		if (!colliding && (!hitCeiling || gravity.y > 0) && !collision.gameObject.CompareTag("EnemyBullet")) {
			colliding = collision.collider != null;
			if (wallGrab || (!collision.gameObject.CompareTag("Ground") && collision.transform.position.y < transform.position.y - 0.5f)) {
				Debug.Log("reset");
				jumpsLeft = jumpsAvailable;
			}
		}
	}
	void OnCollisionExit2D (Collision2D collision) {
		if (colliding && !collision.gameObject.CompareTag("EnemyBullet")) {
			colliding = false;
		}
	}
}
