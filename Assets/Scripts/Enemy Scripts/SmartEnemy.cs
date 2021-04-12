	using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartEnemy : EnemyMovement {

	bool genNavMesh = true;
	bool genPath = false;
	bool genTarget = false;

	//TODO: update navMesh and A* path everytime the player moves to a different location or when they enter AI's range
	Pathfinding pathfinding;
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
		pathfinding = this.GetComponent<Pathfinding>();
		player = GameObject.Find("Player").GetComponent<PlayerMovement>();
		if (GameObject.Find("LittleGuy") != null) littleGuy = GameObject.Find("LittleGuy").GetComponent<GuyMovement>();
		targetVector2 = (Vector2)player.transform.position;
		patrolStart = (Vector2)body.transform.position;
	}

	// Update is called once per frame
	void Update() {
		//update some vars
		//checkSide = Physics2D.Raycast(new Vector2(body.transform.position.x + (isFacingRight ? 0.5f : -0.5f), body.transform.position.y - 0.3f), -body.transform.up, 1f, groundLayers);

		//feet.position = new Vector3(body.transform.position.x, body.transform.position.y - 0.5f, body.transform.position.z);
		//groundCheck.position = new Vector2(body.transform.position.x + (isFacingRight ? 0.5f : -0.5f), body.transform.position.y - 0.3f);
		/*NAVMESH*/
		//generate the navMesh in the first frame of update
		if (genNavMesh) {
			pathfinding.GenerateNavMesh(gravity);
			//Debug.Log(totalJumps);
			genNavMesh = false;
		}
		/*GENERATE PATH FOR AI*/
		if (genTarget) {
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
		if (genPath) {
			ArrayList outputPath = pathfinding.GeneratePath();
			if (outputPath != null) {
				currentStep = 0;
				completedPath = outputPath;
			}
			genPath = false;
		}
		if (stunTime < Time.time && (AIState != States.Patrolling || timeUntilPause < Time.time)) {
			MoveEnemy();
		}
		//Debug.Log(stunTime < Time.time);
		//Debug.Log(jumped);
		//AI decision making based on different States of the AI
		if (!flipped) {
			//TODO: change length of "sight" for AI
			RaycastHit2D inShootingRange = Physics2D.CircleCast(body.transform.position, 0.2f, ((Vector2)targetVector2 - (Vector2)body.transform.position).normalized, alertRadius, groundLayers);
			RaycastHit2D inSight = Physics2D.Raycast(body.transform.position, ((Vector2)targetVector2 - (Vector2)body.transform.position).normalized, alertRadius, groundLayers);
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
					if (Vector2.Distance(body.transform.position, targetVector2) <= alertRadius && IsGrounded()) {
						if (inSight.collider == null) {
							AIState = States.Alert;
						} else if (player.rb.velocity.magnitude >= 5f && Vector2.Distance(body.transform.position, targetVector2) <= susRadius) {
							TriggerSus();
						}
					}
					break;
				case States.Suspicious:
					if (timeUntilNotSus + 1 - susTime < Time.time && timeUntilNotSus >= Time.time && player.rb.velocity.magnitude >= 5f) {
						AIState = States.Alert;
					} else if (timeUntilNotSus < Time.time) {
						Debug.Log("patrol");
						AIState = States.Patrolling;
					}
					break;
				case States.Alert:
					//start to follow player if within a certain range
					//use circle cast to see if a bullet with radius 0.2f would hit player
					if (currTarget == Targets.LittleGuy || Vector2.Distance(body.transform.position, targetVector2) > alertRadius || inShootingRange.collider != null) {
						speed = 5f;
						AIState = States.Following;
						//targetVector2 = (Vector2)player.transform.position;
						align = true;
						genTarget = true;
						genPath = true;
						//Debug.Log("FOLLOW");
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
					if ((Vector2.Distance(body.transform.position, targetVector2) > outOfFollowRadius || pathfinding.currGCost == double.PositiveInfinity) && currentStep >= completedPath.Count - 1) {
						AIState = States.Patrolling;
						patrolStart = (Vector2)body.transform.position;
						currTarget = Targets.Player;
					}
					/*if (Vector2.Distance(body.transform.position, targetVector2) <= alertRadius && inSight.collider == null) {
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
			} else if (player != null && lookAtPlayer.collider != null && lookAtPlayer.collider.gameObject.name.Equals("Player")) {
				currTarget = Targets.Player;
				//Debug.Log("player");
			}
			if (littleGuy != null && currTarget == Targets.LittleGuy) {
				targetVector2 = (Vector2)littleGuy.transform.position;
			} else if (littleGuy == null) {
				currTarget = Targets.Player;
			}
			if (player != null && currTarget == Targets.Player) {
				targetVector2 = (Vector2)player.transform.position;
			}
		}

		//refind path to player if something happens to go wrong (AI hasn't moved to correct step in failsafeTime seconds)
		// && (AIState == States.Following || AIState == States.Flee)
		if (!flipped && timeUntilFailSafe < Time.time && IsGrounded()) {
			Debug.Log("failSafe");
			align = true;
			timeUntilFailSafe = Time.time + failSafeTime;
			if (Vector2.Distance(body.transform.position, targetVector2) <= outOfFollowRadius && AIState == States.Following) {
				//targetVector2 = (Vector2)player.transform.position;
				genTarget = true;
			}
			genPath = true;
		}
		if (AIState != States.Flee && AIState != States.Resting && body.GetComponent<EnemyManager>().currentHealth <= 30f && Vector2.Distance(body.transform.position, player.transform.position) <= alertRadius) {
			lastSeenPos = targetVector2;
			AIState = States.Flee;
			currTarget = Targets.Flee;
			speed = 5f;
			align = true;
			genTarget = true;
			genPath = true;
		}
		//Debug.Log(completedPath.Count);
		//Debug.Log(AIState + ", " + currTarget);
	}

	void MoveEnemy () {
		//move the AI to the player using the steps listed in the generated path
		// && (AIState == States.Following || AIState == States.Flee)
		if (!flipped && AIState != States.Suspicious && !genNavMesh && !genPath && !genTarget && completedPath.Count > 0) {
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
					//Debug.Log("move to: " + currentLink.destPoint.coors);
					//make AI move to next step
					if (!align && !jumped && currentLink.jumpToDest.jumpForce == new Vector2()) {
						//Debug.Log("move");
						if (Vector2.Distance(body.transform.position, new Vector2(currentLink.destPoint.coors.x, body.transform.position.y)) >= 0.1f) {
							if (currentLink.destPoint.coors.x < body.transform.position.x) {
								rb.velocity = new Vector2(-speed, rb.velocity.y);
							} else if (currentLink.destPoint.coors.x > body.transform.position.x) {
								rb.velocity = new Vector2(speed, rb.velocity.y);
							}
						}
						//if its a fall-link
						if (currentLink.destPoint.coors.y < body.transform.position.y && Vector2.Distance(body.transform.position, new Vector2(currentLink.destPoint.coors.x, body.transform.position.y)) < 0.1f) {
							//Debug.Log("drop");
							rb.rotation = 0f;
							body.transform.position = new Vector3(currentLink.destPoint.coors.x, body.transform.position.y, body.transform.position.z);
						}
					}
					//align AI before it jumps
					if (align && !jumped && (Vector2)body.transform.position != ((NavPoint)completedPath[currentStep]).coors && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep]).coors) < 1f) {
						//Debug.Log("align");
						Align(((NavPoint)completedPath[currentStep]).coors);
					}
					//make AI jump to next step
					if (!align && !jumped && currentLink.jumpToDest.jumpForce != new Vector2()) {
						//Debug.Log("jump" + (Vector2)currentLink.jumpToDest.jumpForce);
						//Debug.Log("jump to: " + currentLink.destPoint.coors);
						//special case if the jump is being impeded by an adjacent block
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
							rb.velocity = (Vector2)currentLink.jumpToDest.jumpForce * 1.05f;
							jumped = true;
						}
					}
					//Debug.Log("time " + (timeUntilFailSafe - Time.time));
					if (rb.velocity.y < 0 && IsGrounded() && jumped) {
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
				if (!jumped && IsGrounded() && currentStep < completedPath.Count - 1 && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep + 1]).coors) < 0.1f) {
					//Debug.Log("step");
					currentStep++;
					align = true;
					timeUntilFailSafe = Time.time + failSafeTime;
					if (Vector2.Distance(body.transform.position, targetVector2) <= outOfFollowRadius && AIState == States.Following) {
						//targetVector2 = (Vector2)player.transform.position;
						genTarget = true;
					}
					genPath = true;
				}
				if (!jumped && IsGrounded() && Vector2.Distance(body.transform.position, ((NavPoint)completedPath[currentStep]).coors) < 0.1f) {
					if (Vector2.Distance(body.transform.position, targetVector2) <= outOfFollowRadius && AIState == States.Following) {
						//targetVector2 = (Vector2)player.transform.position;
						genTarget = true;
					}
					genPath = true;
				}
			} else if (currentStep >= completedPath.Count - 1) {
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
		if (alignPos.x < body.transform.position.x) {
			rb.velocity = new Vector2(-speed, rb.velocity.y);
		} else if (alignPos.x > body.transform.position.x) {
			rb.velocity = new Vector2(speed, rb.velocity.y);
		}
		if (Vector2.Distance(body.transform.position, alignPos) < 0.05f) {
			align = false;
			body.transform.position = alignPos;
		}
	}

	void OnDrawGizmos() {
		//test Debug
		//if (tilemap != null && navMesh != null) {
			/*int numLinks = 0;
			for (int y = 0; y < navMesh.GetLength(0); y++) {
				for (int x = 0; x < navMesh.GetLength(1); x++) {
					if (navMesh[y, x].type != "none") {
						if (navMesh[y, x].type == "leftEdge") {
							Gizmos.color = Color.white;
						} else if (navMesh[y, x].type == "platform") {
							Gizmos.color = Color.blue;
						} else if (navMesh[y, x].type == "rightEdge") {
							Gizmos.color = Color.black;
						} else if (navMesh[y, x].type == "solo") {
							Gizmos.color = Color.red;
						}
						Gizmos.DrawSphere(new Vector2(tileWorldPoints[y, x].x, tileWorldPoints[y, x].y), 0.25f);
						//Debug.Log("x: " + tileIndexX + ", y: " + tileIndexY + ", type: " + navMesh[tileIndexY, tileIndexX].platformIndex);
						foreach (NavLink navlink in navMesh[y, x].navlinks) {
							//Debug.Log("y: " + y + ", x: " + x + ", " + navlink.destPoint.coors);
							if (navlink.jumpToDest.jumpForce != new Vector2()) {
								Gizmos.color = Color.cyan;
								numLinks++;
							} else {
								Gizmos.color = Color.green;
							}
							Gizmos.DrawLine(new Vector2(tileWorldPoints[y, x].x, tileWorldPoints[y, x].y), new Vector2(navlink.destPoint.coors.x, navlink.destPoint.coors.y));
							foreach (Vector2 point in navlink.jumpToDest.GetJumpPoints()) {
								Gizmos.color = Color.red;
								Gizmos.DrawSphere(point, 0.1f);
							}
						}
					}
				}
			}*/
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
