using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour {
	public GameObject body;
	public Transform feet;
	public float speed = 5f;
	public float jumpForce = 20f;
	public Rigidbody2D rb;
	protected Vector2 gravity = new Vector2(0f, -40f);
	public LayerMask groundLayers;
	public LayerMask enemyLayer;
	public LayerMask playerLayer;
	protected PlayerMovement player;
	protected GuyMovement littleGuy;
	protected RaycastHit2D lookAtGuy;

	public Transform groundCheck;
	protected bool isFacingRight = true;
	//variables for enemy "kip up"
	protected float kipupVelocity;
	public float kipupTime = 0.25f;
	//whether or not enemy is flipped over (not standing)
	protected bool flipped = false;

	public float attackRate = 1f;
	public float attackRange = 1f;
	public float attackDamage = 10f;
	protected float timeUntilAttack;

	protected RaycastHit2D checkSide;

	protected enum States {
		Patrolling,
		Suspicious,
		Alert,
		Following,
		Flee,
		Resting
	};
	protected States AIState = States.Patrolling;

	//trigger radii
	public int alertRadius = 10;
	public int susRadius = 5;
	public int meleeFollowRadius = 5;
	public int outOfFollowRadius = 10;
	public int canMeleeRadius = 2;
	//timer for AI suspicion
	public float susTime = 3f;
	protected float timeUntilNotSus = 3f;
	//rerun step if AI somehow failed to reach it within 2 seconds
	public float failSafeTime = 2f;
	protected float timeUntilFailSafe = 2f;
	//TODO: update navMesh and navLinks evertime the AI enters a new chunk of the world or conditions change

	// Start is called before the first frame update
	void Start() {
		player = GameObject.Find("Player").GetComponent<PlayerMovement>();
		littleGuy = GameObject.Find("LittleGuy").GetComponent<GuyMovement>();
	}

	// Update is called once per frame
	void Update() {
		//update some vars
		checkSide = Physics2D.Raycast(new Vector2(body.transform.position.x + (isFacingRight ? 0.5f : -0.5f), body.transform.position.y - 0.3f), -body.transform.up, 1f, groundLayers);

		RaycastHit2D inSight = Physics2D.Raycast(body.transform.position, ((Vector2)player.transform.position - (Vector2)body.transform.position).normalized, meleeFollowRadius, groundLayers);
		if (inSight.collider == null) {
			AIState = States.Following;
		} else if (Vector2.Distance(body.transform.position, player.transform.position) > outOfFollowRadius) {
			AIState = States.Patrolling;
		}
		//Debug.Log(AIState);
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


		//patrol platform if too far from player and not following
		if (!flipped && AIState == States.Patrolling && IsGrounded()) {
			speed = 3f;
			if (checkSide.collider != null) {
				//Debug.Log("ground");
				if (isFacingRight) {
					rb.velocity = new Vector2(speed, rb.velocity.y);
				} else {
					rb.velocity = new Vector2(-speed, rb.velocity.y);
				}
			} else {
				//Debug.Log("not ground");
				isFacingRight = !isFacingRight;
				body.transform.localScale = new Vector3(-body.transform.localScale.x, body.transform.localScale.y, body.transform.localScale.z);
			}
		}

		//follow player
		if (!flipped && AIState == States.Following) {
			speed = 5f;
			isFacingRight = player.transform.position.x >= body.transform.position.x;
			if (checkSide.collider != null) {
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

	//attack with staff
	void Slash() {
		Vector2 attackPoint = new Vector2(body.transform.position.x + (isFacingRight ? 1f : -1), body.transform.position.y);
		Collider2D[] collidersHit = Physics2D.OverlapCircleAll(attackPoint, attackRange, playerLayer);
		//TODO: implement other effects on objects from slash, possibly move littleGuy with it
		foreach (Collider2D collider in collidersHit) {
			if (collider.gameObject.name.Equals("Player")) {
				//collider.GetComponent<PlayerManager>().currentHealth -= attackDamage;
				//could either apply force coming from transform.position or attackPoint.position
				float maxKnockback = 25f;
				float minRatio = 0.3f;
				float knockbackRatio = (1 - Vector2.Distance(attackPoint, (Vector2)player.transform.position));
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
				player.rb.velocity = ((Vector2)(player.transform.position - body.transform.position).normalized * knockback);
			}
		}
		//for (int i = 0; i < enemiesToDamage.Length; i++) {
		//}
	}

	//check if bottom of enemy is touching any groundLayers
	public bool IsGrounded() {
		Collider2D onGround = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

		return onGround != null;
	}
}
