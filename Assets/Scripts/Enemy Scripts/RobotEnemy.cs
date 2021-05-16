using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotEnemy : MonoBehaviour {
	public GameObject body;
	public Transform feet;
	public float speed = 5f;
	public float jumpForce = 20f;
	public Rigidbody2D rb;
	public Animator anim;
	protected Vector2 gravity = new Vector2(0f, -40f);
	public LayerMask groundLayers;
	public LayerMask enemyLayer;
	public LayerMask cosmeticLayer;
	public LayerMask playerLayer;
	public LayerMask shieldLayer;
	protected PlayerMovement player;
	protected GuyMovement littleGuy;
	protected RaycastHit2D lookAtGuy;
	protected RaycastHit2D lookAtPlayer;
	[HideInInspector] public enum RobotTypes {
		Melee,
		MidRange,
		Sniper
	}
	public RobotTypes type = RobotTypes.MidRange;
	public bool isSmart;

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
	protected RaycastHit2D checkEdge;

	[HideInInspector] public enum States {
		Patrolling,
		Suspicious,
		Alert,
		Following,
		Flee,
		Resting
	};//[HideInInspector]
	public States AIState = States.Patrolling;

	//trigger radii
	public int alertRadius = 10;
	public int susRadius = 5;
	public int meleeFollowRadius = 5;
	int outOfFollowRadius = 10;
	public float canMeleeRadius = 2f;
	//timer for AI suspicion
	public float susTime = 3f;
	protected float timeUntilNotSus = 3f;
	//rerun step if AI somehow failed to reach it within 2 seconds
	public float failSafeTime = 2f;
	protected float timeUntilFailSafe = 2f;

	public static float stunTime;

	//shooting variables
	public Transform firingPoint;
	public GameObject bulletPrefab;
	float fireRate = 0.4f;
	float bulletSpeed = 15f;
	float bulletDamage = 10f;
	float shootingRange = 10f;
	float timeUntilFire = 0f;
	//targeting player vars
	Vector3 playerFirstPos;
	Vector3 playerSecondPos;
	Vector2 logPrediction = new Vector2();
	Vector2 logPrediction2 = new Vector2();

	//bool genNavMesh = true;
	bool genPath = false;
	bool genTarget = false;

	//TODO: update navMesh and A* path everytime the player moves to a different location or when they enter AI's range
	NavigationalMesh nav;
	Pathfinding pathfinding;
	//Vector2 lastNavMeshCenter;
	ArrayList completedPath = new ArrayList();
	int currentStep = 0;
	Vector2 targetVector2 = new Vector2();
	Vector2 movementPos = new Vector2();
	Vector2 lastSeenPos = new Vector2();
	bool jumped = false;
	bool align = true;

	enum Targets {
		Player,
		LittleGuy,
		Flee,
		Inspect
	};
	Targets currTarget = Targets.Player;
	Vector2 patrolStart;
  public float patrolPauseTime;
  float timeUntilPause;
	float fleeDistance = 20f;
	//TODO: update navMesh and navLinks evertime the AI enters a new chunk of the world or conditions change

	// Start is called before the first frame update
	void Start() {
		nav = GameObject.Find("NavigationalMesh").GetComponent<NavigationalMesh>();
		pathfinding = this.GetComponent<Pathfinding>();
		player = GameObject.Find("Player").GetComponent<PlayerMovement>();
		if (GameObject.Find("LittleGuy") != null) littleGuy = GameObject.Find("LittleGuy").GetComponent<GuyMovement>();
		targetVector2 = (Vector2)player.transform.position;
		patrolStart = (Vector2)body.transform.position;

		//determine characteristics of enemies based on type and intelligence
		switch (type) {
			case RobotTypes.Melee:
				if (isSmart) {
					outOfFollowRadius = 20;
				} else {
					outOfFollowRadius = 10;
				}
				break;
			case RobotTypes.MidRange:
				shootingRange = 10f;
				fireRate = 0.4f;
				bulletSpeed = 15f;
				bulletDamage = 10f;
				if (isSmart) {
					outOfFollowRadius = 20;
				} else {
					outOfFollowRadius = 10;
				}
				break;
			case RobotTypes.Sniper:
				shootingRange = 30f;
				fireRate = 1f;
				bulletSpeed = 20f;
				bulletDamage = 20f;
				outOfFollowRadius = 20;
				break;
		}
	}

	// Update is called once per frame
	void Update() {
		//update some vars
		//checkSide = Physics2D.Raycast(new Vector2(body.transform.position.x + (isFacingRight ? 0.5f : -0.5f), body.transform.position.y - 0.3f), -body.transform.up, 1f, groundLayers);

		//feet.position = new Vector3(body.transform.position.x, body.transform.position.y - 0.5f, body.transform.position.z);
		//groundCheck.position = new Vector2(body.transform.position.x + (isFacingRight ? 0.5f : -0.5f), body.transform.position.y - 0.3f);
		/*if (Vector2.Distance(player.transform.position, lastNavMeshCenter) > 10) {
			genNavMesh = true;
		}*/
		/*NAVMESH*/
		//generate the navMesh in the first frame of update
		if (nav.genNavMesh) {
			Debug.Log(Vector2.Distance(transform.position, player.transform.position));
		}
		/*if (genNavMesh) {
			pathfinding.GenerateNavMesh(gravity, new Vector2Int((int)player.transform.position.x - 30, (int)player.transform.position.y - 30), new Vector2Int((int)player.transform.position.x + 30, (int)player.transform.position.y + 30));
			lastNavMeshCenter = (Vector2)player.transform.position;
			//Debug.Log(totalJumps);
			genNavMesh = false;
		}*/
		/*GENERATE PATH FOR AI*/
		if (genTarget && !nav.genNavMesh) {
			/*if (AIState == States.Flee) {
				float fleeDistance = 20f;
				Vector2 fleeVector = ((Vector2)body.transform.position - (Vector2)player.transform.position).normalized * fleeDistance;
				//Vector2 fleeVector = (body.transform.position.x + oppositeVector.x < tileWorldPoints[0, 0].x || body.transform.position.x + oppositeVector.x > tileWorldPoints[navMesh.GetLength(0) - 1, navMesh.GetLength(1) - 1].x) ? new Vector2(-oppositeVector.x, oppositeVector.y) : oppositeVector;
				//Debug.Log(fleeVector);
				movementPos = (Vector2)body.transform.position + fleeVector;
			}*/
			if (AIState == States.Patrolling) {
				movementPos = new Vector2(Random.Range(patrolStart.x - 5f, patrolStart.x + 5f), Random.Range(patrolStart.y - 5f, patrolStart.y + 5f));
			} else {
				switch (currTarget) {
					case Targets.Player:
						movementPos = (Vector2)player.transform.position;
						break;
					case Targets.LittleGuy:
						if (littleGuy != null) movementPos = (Vector2)littleGuy.transform.position;
						break;
					case Targets.Flee:
						Vector2 fleeVector = ((Vector2)body.transform.position - (Vector2)player.transform.position).normalized * fleeDistance;
						//Vector2 fleeVector = (body.transform.position.x + oppositeVector.x < tileWorldPoints[0, 0].x || body.transform.position.x + oppositeVector.x > tileWorldPoints[navMesh.GetLength(0) - 1, navMesh.GetLength(1) - 1].x) ? new Vector2(-oppositeVector.x, oppositeVector.y) : oppositeVector;
						//Debug.Log(fleeVector);
						movementPos = (Vector2)body.transform.position + fleeVector;
						break;
					case Targets.Inspect:
						movementPos = lastSeenPos;
						break;
				}
			}
			//TODO: make a reference to Pathfinding navmesh here
			pathfinding.GenerateTarget(movementPos, AIState == States.Flee || AIState == States.Patrolling);
			genTarget = false;
		}
		//use the A* algorithm to find the most effecient and fast route from the AI to the player
		if (genPath && !genTarget && !nav.genNavMesh) {
			ArrayList outputPath = pathfinding.GeneratePath();
			if (outputPath != null) {
				currentStep = 0;
				completedPath = outputPath;
				//Debug.Log("new path cost: " + pathfinding.currGCost);
			}
			if (outputPath == null) {
				//Debug.Log("null path");
			}
			genPath = false;
		}
		//if (stunTime < Time.time && (AIState != States.Patrolling || timeUntilPause < Time.time)) {
			MoveEnemy();
		//}
		if (type == RobotTypes.Melee) {
			//attack player with melee
			if (Vector2.Distance(body.transform.position, player.transform.position) <= canMeleeRadius && timeUntilAttack < Time.time) {
				//anim.SetBool("Slash", true);
				Slash();
				timeUntilAttack = Time.time + attackRate;
			} /*else if (timeUntilAttack < Time.time) {
				anim.SetBool("Slash", false);
			}*/
		} else {
			HandleShooting();
		}
		//Debug.Log(stunTime < Time.time);
		//Debug.Log(jumped);
		//AI decision making based on different States of the AI
		if (!flipped) {
			//TODO: change length of "sight" for AI
			RaycastHit2D inShootingRange = Physics2D.CircleCast(body.transform.position, 0.2f, (getTargetVector2() - (Vector2)body.transform.position).normalized, shootingRange, groundLayers);
			RaycastHit2D inSight = Physics2D.Raycast(body.transform.position, (getTargetVector2() - (Vector2)body.transform.position).normalized, alertRadius, groundLayers);
			switch (AIState) {
				case States.Patrolling:
					if (currentStep >= completedPath.Count - 1) {
						timeUntilPause = Time.time + patrolPauseTime;
						speed = 3f;
						//Debug.Log("new pos");
						align = true;
						genTarget = true;
						genPath = true;
					}
					if (Vector2.Distance(body.transform.position, getTargetVector2()) <= alertRadius && IsGrounded()) {
						if (inSight.collider == null) {
							AIState = States.Alert;
						} else if (player.rb.velocity.magnitude >= 5f && Vector2.Distance(body.transform.position, getTargetVector2()) <= susRadius) {
							TriggerSus();
						}
					}
					break;
				case States.Suspicious:
					if (timeUntilNotSus + 1 - susTime < Time.time && timeUntilNotSus >= Time.time && player.rb.velocity.magnitude >= 5f) {
						AIState = States.Alert;
					} else if (timeUntilNotSus < Time.time) {
						Debug.Log("patrol after sus");
						AIState = States.Patrolling;
						patrolStart = (Vector2)body.transform.position;
						currTarget = Targets.Player;
					}
					break;
				case States.Alert:
					//start to follow player if within a certain range
					//use circle cast to see if a bullet with radius 0.2f would hit player
					if (currTarget == Targets.LittleGuy || Vector2.Distance(body.transform.position, getTargetVector2()) > alertRadius || inShootingRange.collider != null || (type == RobotTypes.Melee && pathfinding.currGCost != double.PositiveInfinity)) {
						speed = 5f;
						AIState = States.Following;
						//targetVector2 = (Vector2)player.transform.position;
						align = true;
						genTarget = true;
						genPath = true;
						Debug.Log("FOLLOW");
					}
					break;
				case States.Following:
					/*if (pathfinding.currGCost == double.PositiveInfinity) {
						currTarget = Targets.Inspect;
					}*/
					/*if (currTarget == Targets.Inspect && pathfinding.currGCost == double.PositiveInfinity) {
						AIState = States.Patrolling;
						patrolStart = (Vector2)body.transform.position;
						currTarget = Targets.Player;
					}*/
					if ((Vector2.Distance(body.transform.position, getTargetVector2()) > outOfFollowRadius || pathfinding.currGCost == double.PositiveInfinity) && currentStep >= completedPath.Count - 1) {
						Debug.Log("patrol");
						AIState = States.Patrolling;
						patrolStart = (Vector2)body.transform.position;
						currTarget = Targets.Player;
					}
					/*if (Vector2.Distance(body.transform.position, targetVector2) <= shootingRange && inSight.collider == null) {
						AIState = States.Alert;
					}*/
					break;
				case States.Flee:
					fleeDistance = 20f;
					/*if (body.GetComponent<EnemyManager>().currentHealth <= 30f) {
						currTarget = Targets.Flee;
						speed = 5f;
						//align = true;
						genTarget = true;
						genPath = true;
					}*/
					if (pathfinding.currGCost == double.PositiveInfinity && fleeDistance == 20f) {
						fleeDistance = 10f;
					}
					if (pathfinding.currGCost == double.PositiveInfinity && fleeDistance == 10f) {
						AIState = States.Alert;
					}
					break;
				case States.Resting:
					if (body.GetComponent<EnemyManager>().currentHealth < body.GetComponent<EnemyManager>().maxHealth) {
						body.GetComponent<EnemyManager>().currentHealth += 100f * Time.deltaTime;
					} else {
						body.GetComponent<EnemyManager>().currentHealth = body.GetComponent<EnemyManager>().maxHealth;
					}
					if (body.GetComponent<EnemyManager>().currentHealth >= body.GetComponent<EnemyManager>().maxHealth) {
						AIState = States.Following;
						currTarget = Targets.Inspect;
					}
					break;
				default:
					Debug.Log("AIState Unknown");
					break;
			}
		}
		if (AIState != States.Flee) {
			if (littleGuy != null) lookAtGuy = Physics2D.Raycast(body.transform.position, ((Vector2)littleGuy.transform.position - (Vector2)body.transform.position).normalized, alertRadius, ~enemyLayer);
			if (player != null) lookAtPlayer = Physics2D.Raycast(body.transform.position, ((Vector2)player.transform.position - (Vector2)body.transform.position).normalized, alertRadius, ~enemyLayer);
			if (littleGuy != null && lookAtGuy.collider != null && (lookAtGuy.collider.gameObject.name.Equals("LittleGuy") || lookAtGuy.collider.gameObject.name.Equals("Shield"))) {
				currTarget = Targets.LittleGuy;
				//Debug.Log("LittleGuy");
			} else if ((player != null && lookAtPlayer.collider != null && lookAtPlayer.collider.gameObject.name.Equals("Player")) || littleGuy == null) {
				currTarget = Targets.Player;
				//Debug.Log("player");
			}
		}

		//refind path to player if something happens to go wrong (AI hasn't moved to correct step in failsafeTime seconds)
		// && (AIState == States.Following || AIState == States.Flee)
		if (!flipped && timeUntilFailSafe < Time.time && IsGrounded()) {
			Debug.Log("failSafe");
			align = true;
			timeUntilFailSafe = Time.time + failSafeTime;
			if (Vector2.Distance(body.transform.position, getTargetVector2()) <= outOfFollowRadius && AIState == States.Following) {
				//targetVector2 = (Vector2)player.transform.position;
				genTarget = true;
			}
			genPath = true;
			rb.velocity = new Vector2(Random.Range(-10f, 10f), Random.Range(2f, 3f));
		}
		if (AIState != States.Flee && AIState != States.Resting && body.GetComponent<EnemyManager>().currentHealth <= 30f && Vector2.Distance(body.transform.position, getTargetVector2()) <= alertRadius) {
			lastSeenPos = getTargetVector2();
			AIState = States.Flee;
			currTarget = Targets.Flee;
			speed = 5f;
			align = true;
			genTarget = true;
			genPath = true;
		}
		//Debug.Log(currentStep + ", " + completedPath.Count);
		//Debug.Log(AIState + ", " + currTarget);
	}

	Vector2 getTargetVector2() {
		Vector2 targetVector2 = new Vector2();
		if (littleGuy != null && currTarget == Targets.LittleGuy) {
			targetVector2 = (Vector2)littleGuy.transform.position;
		}
		if (player != null && currTarget == Targets.Player) {
			targetVector2 = (Vector2)player.transform.position;
		}
		return targetVector2;
	}

	void MoveEnemy () {
		//move the AI to the player using the steps listed in the generated path
		// && (AIState == States.Following || AIState == States.Flee) //
		if (!flipped && AIState != States.Suspicious && !nav.genNavMesh && !genPath && !genTarget && completedPath != null && completedPath.Count > 0) {
			if (currentStep < completedPath.Count - 1) {
				//find current navlink
				NavLink currentLink = null;
				foreach (NavLink navlink in ((NavPoint)completedPath[currentStep]).navlinks) {
					if (navlink.destPoint == completedPath[currentStep + 1]) {
						currentLink = navlink;
						break;
					}
				}
				//if there is a link, move to the link's destination navPoint
				if (currentLink != null) {
					if (stunTime < Time.time && (AIState != States.Patrolling || timeUntilPause < Time.time)) {

						//Debug.Log("move to: " + currentLink.destPoint.coors);
						//make AI move to next step
						if (!align && !jumped && currentLink.jumpToDest.jumpForce == new Vector2()) {
							//Debug.Log("move");
							if (Vector2.Distance(body.transform.position, new Vector2(currentLink.destPoint.coors.x, body.transform.position.y)) >= 0.1f) {
								if (currentLink.destPoint.coors.x < body.transform.position.x) {
									isFacingRight = false;
									rb.velocity = new Vector2(-speed, rb.velocity.y);
								} else if (currentLink.destPoint.coors.x > body.transform.position.x) {
									isFacingRight = true;
									rb.velocity = new Vector2(speed, rb.velocity.y);
								}
								/*if (Vector2.Distance(body.transform.position, currentLink.destPoint.coors) < 0.05f) {
									body.transform.position = currentLink.destPoint.coors;
									rb.velocity = new Vector2(0f, rb.velocity.y);
								}*/
							}
							//if its a fall-link
							if (currentLink.destPoint.coors.y < body.transform.position.y && Vector2.Distance(body.transform.position, new Vector2(currentLink.destPoint.coors.x, body.transform.position.y)) < 0.1f) {
								//Debug.Log("drop");
								rb.rotation = 0f;
								body.transform.position = new Vector3(currentLink.destPoint.coors.x, body.transform.position.y, body.transform.position.z);
							}
						}

						//make AI jump to next step
						if (!align && !jumped && currentLink.jumpToDest.jumpForce != new Vector2()) {
							//Debug.Log("jump" + (Vector2)currentLink.jumpToDest.jumpForce);
							//Debug.Log("jump to: " + currentLink.destPoint.coors);
							//special case if the jump is being impeded by an adjacent block
							isFacingRight = ((Vector2)currentLink.jumpToDest.jumpForce).x > 0f;
							if (Mathf.Abs(currentLink.destPoint.coors.x - ((NavPoint)completedPath[currentStep]).coors.x) == 1f) {
								if (Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep]).coors) < 0.1f) {
									rb.velocity = new Vector2(0f, currentLink.jumpToDest.jumpForce.y * 1.05f);
								}
								if (body.transform.position.y >= currentLink.destPoint.coors.y) {
									body.transform.position = new Vector2(currentLink.jumpToDest.jumpForce.x > 0f ? currentLink.destPoint.coors.x - 1 : currentLink.destPoint.coors.x + 1, currentLink.destPoint.coors.y);
									rb.velocity = new Vector2(currentLink.jumpToDest.jumpForce.x * 1f, rb.velocity.y);
									jumped = true;
								}
							} else {
								//jump normally
								isFacingRight = ((Vector2)currentLink.jumpToDest.jumpForce).x > 0f;
								rb.velocity = (Vector2)currentLink.jumpToDest.jumpForce * 1.05f;
								jumped = true;
							}
						}
						//Debug.Log("time " + (timeUntilFailSafe - Time.time));
						if (rb.velocity.y <= 0 && IsGrounded() && jumped) {
							jumped = false;
							align = true;
							// && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep]).coors) > 1f
							if (currentLink.jumpToDest.jumpForce != new Vector2() && currentStep < completedPath.Count - 1 && body.transform.position.y >= ((NavPoint)completedPath[currentStep + 1]).coors.y) {
								//Debug.Log("next");
								currentStep++;
								timeUntilFailSafe = Time.time + failSafeTime;
								//genPath = true;
							}
						}
						/*if (!jumped && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep + 1]).coors) < 0.1f) {
							body.transform.position = ((NavPoint)completedPath[currentStep + 1]).coors;
							rb.rotation = 0f;
						}*/
					}
				}
				//if there's something in AI's path
				//TODO: Make it so that smart enemies can sense bullets coming towards it and jump over it
				RaycastHit2D blockPath = Physics2D.CircleCast(new Vector2(body.transform.position.x + (isFacingRight ? 0.6f : -0.6f), body.transform.position.y), 1f, (Vector2)(currentLink.destPoint.coors - ((NavPoint)completedPath[currentStep]).coors).normalized, Vector2.Distance(((NavPoint)completedPath[currentStep]).coors, currentLink.destPoint.coors), ~groundLayers & ~playerLayer & ~cosmeticLayer);
				if (IsGrounded() && blockPath.collider != null && !blockPath.collider.gameObject.CompareTag("EnemyBullet") && blockPath.collider != body.gameObject.GetComponent<Collider2D>() && ((body.transform.position.x > blockPath.collider.gameObject.transform.position.x) ? blockPath.collider.gameObject.GetComponent<Rigidbody2D>().velocity.x > 0 : blockPath.collider.gameObject.GetComponent<Rigidbody2D>().velocity.x < 0) && IsGrounded() && rb.velocity.y < 20f) {
					rb.velocity = new Vector2(rb.velocity.x, 15f);
				}
				//align AI before it jumps // && (Vector2)body.transform.position != ((NavPoint)completedPath[currentStep]).coors && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep]).coors) < 1f
				if (align && !jumped) {
					Align(((NavPoint)completedPath[currentStep]).coors);
				}
				if (!jumped && IsGrounded() && currentStep < completedPath.Count - 1 && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep + 1]).coors) < 0.1f) {
					//Debug.Log("step");
					currentStep++;
					align = true;
					timeUntilFailSafe = Time.time + failSafeTime;
					if (Vector2.Distance(body.transform.position, getTargetVector2()) <= outOfFollowRadius && AIState == States.Following && Vector2.Distance(getTargetVector2(), movementPos) > 1f) {
						//targetVector2 = (Vector2)player.transform.position;
						genTarget = true;
						genPath = true;
					}
					//genPath = true;
				}
				if (!jumped && IsGrounded() && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep]).coors) < 0.1f) {
					if (Vector2.Distance(body.transform.position, getTargetVector2()) <= outOfFollowRadius && AIState == States.Following && Vector2.Distance(getTargetVector2(), movementPos) > 1f) {
						//targetVector2 = (Vector2)player.transform.position;
						genTarget = true;
						genPath = true;
					}
					//genPath = true;
				}
			} else if (currentStep >= completedPath.Count - 1 && pathfinding.currGCost != double.PositiveInfinity) {
				//align AI at final step if it has reached its destination
				//Debug.Log("STOP");
				if (!jumped && (Vector2)body.transform.position != ((NavPoint)completedPath[completedPath.Count - 1]).coors && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[completedPath.Count - 1]).coors) < 1f) {
					Align(((NavPoint)completedPath[completedPath.Count - 1]).coors);
				}
				if ((Vector2)body.transform.position == ((NavPoint)completedPath[completedPath.Count - 1]).coors) {
					rb.velocity = new Vector2(0f, rb.velocity.y);
					if (AIState == States.Following) {
						AIState = States.Alert;
					} else if (AIState == States.Flee) {
						AIState = States.Resting;
					}
					if (currTarget == Targets.Inspect && Vector2.Distance(body.transform.position, lastSeenPos) < 1f) {
						currTarget = Targets.Player;
						AIState = States.Patrolling;
						patrolStart = (Vector2)body.transform.position;
					}
				}
			}
		}
	}

	//Function to align AI's position with a certain position
	void Align(Vector2 alignPos) {
		//Debug.Log("align");
		if (alignPos.x < body.transform.position.x) {
			isFacingRight = false;
			rb.velocity = new Vector2(-speed, rb.velocity.y);
		} else if (alignPos.x > body.transform.position.x) {
			isFacingRight = true;
			rb.velocity = new Vector2(speed, rb.velocity.y);
		}
		//Mathf.Abs(body.transform.position.x - alignPos.x)
		if (Vector2.Distance(body.transform.position, alignPos) < 0.05f) {
			align = false;
			body.transform.position = alignPos;
			rb.velocity = new Vector2(0f, rb.velocity.y);
		}
	}

	void HandleShooting() {
		//assuming that bullets are 0.2f size
		lookAtPlayer = Physics2D.CircleCast(body.transform.position, 0.2f, ((Vector2)player.transform.position - (Vector2)body.transform.position).normalized, shootingRange, ~enemyLayer);
		if (littleGuy != null) lookAtGuy = Physics2D.CircleCast(body.transform.position, 0.2f, ((Vector2)littleGuy.transform.position - (Vector2)body.transform.position).normalized, shootingRange, ~enemyLayer);
		//Debug.Log(timeUntilFire + " : " + Time.time);
		if (timeUntilFire < Time.time + 0.02 && timeUntilFire > Time.time + 0.01) {
			playerFirstPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
		}
		if (timeUntilFire < Time.time && (AIState == States.Alert || AIState == States.Following || AIState == States.Flee)) {
			if (littleGuy != null && lookAtGuy.collider != null && (lookAtGuy.collider.gameObject.name.Equals("LittleGuy") || lookAtGuy.collider.gameObject.name.Equals("Shield"))) {
				//Debug.Log(lookAtGuy.collider.gameObject.name);
				ShootAtLittleGuy();
				timeUntilFire = Time.time + fireRate;
			} else if (lookAtPlayer.collider != null && lookAtPlayer.collider.gameObject.name.Equals("Player")) {
				playerSecondPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
				ShootAtPlayer();
				timeUntilFire = Time.time + fireRate;
			}
		}
	}

	void ShootAtLittleGuy () {
		Vector3 guyPos = GameObject.Find("LittleGuy").GetComponent<GuyMovement>().transform.position;
		float angle = Mathf.Atan((guyPos.y - firingPoint.position.y) / (guyPos.x - firingPoint.position.x));
		//Debug.Log(angle);
		EnemyBullet bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.Euler(new Vector3(0f, 0f, angle))).GetComponent<EnemyBullet>();
		bullet.firingPoint = firingPoint.position;
		bullet.target = guyPos;
		Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), body.GetComponent<Collider2D>());
	}

	void ShootAtPlayer () {
		//bool playerGrounded = GameObject.Find("Player").GetComponent<PlayerMovement>().IsGrounded();

		Vector3 playerPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
		Vector2 playerVel = GameObject.Find("Player").GetComponent<PlayerMovement>().rb.velocity;
		float distToPlayer = Vector2.Distance(body.transform.position, playerPos);
		Vector2 playerVelocity = playerSecondPos - playerFirstPos;
		//TODO: get rid of this if not using this formula
		float time = Mathf.Abs((playerPos.y - body.transform.position.y + playerPos.x - body.transform.position.x) / (bulletSpeed - playerVelocity.x - playerVelocity.y));
		float bulletVelX = (playerPos.x + (playerVelocity.x * time) - body.transform.position.x) / (bulletSpeed * time);
		float bulletVelY = (playerPos.y + (playerVelocity.y * time) - body.transform.position.y) / (bulletSpeed * time);
		//Debug.Log(new Vector2(bulletVelX, bulletVelY).normalized);
		//have the AI predict where the player will be based on player's position and velocity
		//TODO: make this prediction for angle line up with prediction in the EnemyBullet class
		//TODO: change "position" to "firing point" when using bullet starting position
		logPrediction = (Vector2) playerPos + ((playerVelocity * 15f) * (distToPlayer / 5f));
		//Debug.Log(playerVel);
		float angle = Mathf.Atan((playerPos.y - firingPoint.position.y) / (playerPos.x - firingPoint.position.x));
		//Debug.Log(angle);
		EnemyBullet bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.Euler(new Vector3(0f, 0f, angle))).GetComponent<EnemyBullet>();
		bullet.firingPoint = firingPoint.position;
		bullet.target = logPrediction;
		bullet.bulletSpeed = bulletSpeed;
		bullet.bulletDamage = bulletDamage;
		logPrediction2 = ((Vector2)body.transform.position + new Vector2(bulletVelX, bulletVelY).normalized * bulletSpeed * time);
		//bullet.rb.velocity = new Vector2(bulletVelX, bulletVelY).normalized * bulletSpeed;
		Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), body.GetComponent<Collider2D>());
	}

	public void TriggerSus() {
		AIState = States.Suspicious;
		timeUntilNotSus = Time.time + susTime;
	}

	public void SetStunTime(float newTime) {
		stunTime = Time.time + newTime;
	}

	//attack with staff
	public void Slash() {
		Vector2 attackPoint = new Vector2(body.transform.position.x + (isFacingRight ? 1f : -1f), body.transform.position.y);
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
				Debug.Log(knockback);
				//player.rb.velocity = ((Vector2)(player.transform.position - body.transform.position).normalized * knockback);
				player.rb.AddForce(((Vector2)(player.transform.position - body.transform.position).normalized * knockback), ForceMode2D.Impulse);
			}
		}
	}

	//check if bottom of enemy is touching any groundLayers
	public bool IsGrounded() {
		Collider2D onGround = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

		return onGround != null;
	}

	void OnDrawGizmos() {
		//test Debug
		//if (tilemap != null && navMesh != null) {
			int numLinks = 0;
			for (int y = 0; y < nav.navMesh.GetLength(0); y++) {
				for (int x = 0; x < nav.navMesh.GetLength(1); x++) {
					if (nav.navMesh[y, x].type != "none") {
						if (nav.navMesh[y, x].type == "leftEdge") {
							Gizmos.color = Color.white;
						} else if (nav.navMesh[y, x].type == "platform") {
							Gizmos.color = Color.blue;
						} else if (nav.navMesh[y, x].type == "rightEdge") {
							Gizmos.color = Color.black;
						} else if (nav.navMesh[y, x].type == "solo") {
							Gizmos.color = Color.red;
						}
						Gizmos.DrawSphere(new Vector2(nav.tileWorldPoints[y, x].x, nav.tileWorldPoints[y, x].y), 0.25f);
						//Debug.Log("x: " + tileIndexX + ", y: " + tileIndexY + ", type: " + navMesh[tileIndexY, tileIndexX].platformIndex);
						foreach (NavLink navlink in nav.navMesh[y, x].navlinks) {
							//Debug.Log("y: " + y + ", x: " + x + ", " + navlink.destPoint.coors);
							if (navlink.jumpToDest.jumpForce != new Vector2()) {
								Gizmos.color = Color.cyan;
								numLinks++;
							} else {
								Gizmos.color = Color.green;
							}
							Gizmos.DrawLine(new Vector2(nav.tileWorldPoints[y, x].x, nav.tileWorldPoints[y, x].y), new Vector2(navlink.destPoint.coors.x, navlink.destPoint.coors.y));
							foreach (Vector2 point in navlink.jumpToDest.GetJumpPoints()) {
								Gizmos.color = Color.red;
								Gizmos.DrawSphere(point, 0.1f);
							}
						}
					}
				}
			}
			//Debug.Log(numLinks);
			for (int n = 0; n < completedPath.Count - 1; n++) {
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(((NavPoint)completedPath[n]).coors, 0.25f);

				foreach (NavLink navlink in ((NavPoint)completedPath[n]).navlinks) {
					if (navlink.destPoint == completedPath[n + 1]) {
						if (navlink.jumpToDest.jumpForce != new Vector2()) {
							Gizmos.color = Color.cyan;
						} else {
							Gizmos.color = Color.green;
						}
						Gizmos.DrawLine(((NavPoint)completedPath[n]).coors, new Vector2(navlink.destPoint.coors.x, navlink.destPoint.coors.y));
						foreach (Vector2 point in navlink.jumpToDest.GetJumpPoints()) {
							Gizmos.color = Color.red;
							Gizmos.DrawSphere(point, 0.1f);
						}
					}
				}
			}
		//}
		if (completedPath.Count > 0 && currentStep < completedPath.Count - 1) {
			Gizmos.color = Color.black;
			Gizmos.DrawSphere(((NavPoint)completedPath[currentStep + 1]).coors, 0.25f);
		}

		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(movementPos, 0.1f);

		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(pathfinding.targetPoint.coors, 0.1f);

		Gizmos.color = Color.green;
		Gizmos.DrawSphere(pathfinding.startPoint.coors, 0.1f);
	}

	void FixedUpdate () {
		rb.AddForce(gravity);

		//Debug.Log(jumped);
		//float trueRotation = (rb.rotation % 360) + 360f;
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
	}

	//check if bottom of enemy is touching any groundLayers
	/*public bool IsGrounded() {
		Collider2D onGround = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

		return onGround != null;
	}*/
}
