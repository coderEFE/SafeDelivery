using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;


public class EnemyMovement : MonoBehaviour {
	public Transform feet;
	public float speed = 5f;
	public float jumpForce = 20f;
	public Rigidbody2D rb;
	Vector2 gravity = new Vector2(0f, -40f);
	public LayerMask groundLayers;
	public LayerMask enemyLayer;
	public LayerMask shieldLayer;
	PlayerMovement player;
	GuyMovement littleGuy;
	RaycastHit2D lookAtGuy;

	public Transform groundCheck;
	bool isFacingRight = true;
	//variables for enemy "kip up"
	float kipupVelocity;
	float kipupTime = 0.25f;
	//whether or not enemy is flipped over (not standing)
	bool flipped = false;

	RaycastHit2D checkSide;
	bool following = false;
	public Tilemap tilemap;
	NavPoint[,] navMesh;
	bool genNavMesh = true;
	bool genPath = false;
	bool genTarget = false;
	Vector2[,] tileWorldPoints;

	//TODO: update these and A* path everytime the player moves to a different location or when they enter AI's range
	ArrayList completedPath = new ArrayList();
	double currGCost = double.PositiveInfinity;
	int currentStep = 0;
	NavPoint startPoint = new NavPoint();
	NavPoint targetPoint = new NavPoint();
	int start = 0;
	int target = 0;
	Vector2 targetVector2 = new Vector2();
	Vector2 movementPos = new Vector2();
	bool jumped = false;
	bool align = true;
	//TODO: write explanation of states
	enum States {
		Patrolling,
		Suspicious,
		Alert,
		Following,
		Flee,
		Resting
	};
	States AIState = States.Patrolling;
	enum Targets {
		Player,
		LittleGuy,
		Flee
	};
	Targets currTarget = Targets.Player;
	Vector2 patrolStart;
	//trigger radii
	int alertRadius = 10;
	int susRadius = 5;
	//int followRadius = 10;
	int outOfFollowRadius = 20;
	//timer for AI suspicion
	float susTime = 3f;
	float timeUntilNotSus = 3f;
	//rerun step if AI somehow failed to reach it within 2 seconds
	public float failSafeTime = 2f;
	float timeUntilFailSafe = 2f;
	//TODO: update navMesh and navLinks evertime the AI enters a new chunk of the world or conditions change

	// Start is called before the first frame update
	void Start() {
		player = GameObject.Find("Player").GetComponent<PlayerMovement>();
		littleGuy = GameObject.Find("LittleGuy").GetComponent<GuyMovement>();
		targetVector2 = (Vector2)player.transform.position;
		patrolStart = (Vector2)transform.position;
	}

	// Update is called once per frame
	void Update() {
		//update some vars
		checkSide = Physics2D.Raycast(groundCheck.position, -transform.up, 1f, groundLayers);

		feet.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
		groundCheck.position = new Vector2(transform.position.x + (isFacingRight ? 0.5f : -0.5f), transform.position.y - 0.3f);
		/*NAVMESH*/
		//generate the navMesh in the first frame of update
		if (tilemap != null && genNavMesh) {
			tilemap.CompressBounds();
			BoundsInt bounds = tilemap.cellBounds;
			TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
			//navmesh is one block higher than tilemap since it needs to generate navpoints for the top tiles
			//initialize arrays for navMesh and tileWorldPoints
			navMesh = new NavPoint[bounds.size.y + 1, bounds.size.x];
			tileWorldPoints = new Vector2[bounds.size.y + 1, bounds.size.x];
			for (int y = 0; y < bounds.size.y + 1; y++) {
				for (int x = 0; x < bounds.size.x; x++) {
					navMesh[y, x] = new NavPoint();
				}
			}
			SetTileWorldPoints();

			/*GENERATE NAVPOINTS*/
			int platformIndex = 0;
			bool platformStarted = false;
			for (int y = 0; y < bounds.size.y + 1; y++) {
				platformStarted = false;
				for (int x = 0; x < bounds.size.x; x++) {
					TileBase tile = null;
					if (y < bounds.size.y) {
						tile = allTiles[x + y * bounds.size.x];
					}
					//Debug.Log((y + 1) < bounds.size.y);
					if (!platformStarted) {
						//checks if lower tile is not null and a platform hasn't been started, and then add a navPoint of type "leftEdge"
						if ((y == bounds.size.y || tile == null) && (y - 1) >= 0 && allTiles[x + ((y - 1) * bounds.size.x)] != null) {
							platformIndex++;
							navMesh[y, x].Init(tileWorldPoints[y, x], platformIndex, "leftEdge");
							platformStarted = true;
						}
					}
					if (platformStarted) {
						//checks if lower right tile is not null and right tile is null, and then add a navPoint of type "platform"
						if ((y - 1) >= 0 && (x + 1) < bounds.size.x && allTiles[(x + 1) + ((y - 1) * bounds.size.x)] != null && (y == bounds.size.y || allTiles[(x + 1) + (y * bounds.size.x)] == null) && navMesh[y, x].type != "leftEdge") {
							navMesh[y, x].Init(tileWorldPoints[y, x], platformIndex, "platform");
						}
						//checks if lower right tile is null or right tile is not null, and then end the platform
						if ((((y - 1) >= 0 && (x + 1) < bounds.size.x) && (y == bounds.size.y ? (allTiles[(x + 1) + ((y - 1) * bounds.size.x)] == null) : (allTiles[(x + 1) + ((y - 1) * bounds.size.x)] == null || allTiles[(x + 1) + ((y) * bounds.size.x)] != null))) || x == bounds.size.x - 1) {
							if (navMesh[y, x].type == "leftEdge") {
								navMesh[y, x].Init(tileWorldPoints[y, x], platformIndex, "solo");
							} else {
								navMesh[y, x].Init(tileWorldPoints[y, x], platformIndex, "rightEdge");
							}
							platformStarted = false;
						}
					}
					/*if (tile != null) {
						Debug.Log("x:" + x + " y:" + y + " tile:" + tile.name);
					} else {
						Debug.Log("x:" + x + " y:" + y + " tile: (null)");
					}*/
				}
			}
			/*GENERATE NAVLINKS*/
			//start platform, target platform//, left or right
			bool[,] rightVisited = new bool[platformIndex, platformIndex];
			//int totalJumps = 0;
			for (int y = 0; y < navMesh.GetLength(0); y++) {
				for (int x = 0; x < navMesh.GetLength(1); x++) {
					//Generate walkable navlinks
					if (navMesh[y, x].type != "none" && x < navMesh.GetLength(1) - 1 && navMesh[y, x + 1].type != "none") {
						//walking linkScore is 1f
						//add link from left to right
						NavLink link = new NavLink();
						link.Init(navMesh[y, x + 1], 1f, new JumpTrajectory());
						navMesh[y, x].AddNavLink(link);
						//add link from right to left
						NavLink link2 = new NavLink();
						link2.Init(navMesh[y, x], 1f, new JumpTrajectory());
						navMesh[y, x + 1].AddNavLink(link2);
					}
					//Generate falling navlinks
					if (navMesh[y, x].type != "none" && navMesh[y, x].type != "platform") {
						int a = 0;
						int b = 0;
						switch (navMesh[y, x].type) {
							case "rightEdge":
							a = 1;
							b = 1;
								break;
							case "leftEdge":
							a = 0;
							b = 0;
								break;
							case "solo":
							a = 0;
							b = 1;
								break;
						}
						for (int i = a; i <= b; i++) {
							if ((i == 0 && x > 0) || (i == 1 && x < navMesh.GetLength(1) - 1)) {
								int targetColumn = x;
								if (i == 0) {
									targetColumn = x - 1;
								} else {
									targetColumn = x + 1;
								}
								int targetRow = y;
								if (navMesh[y, targetColumn].type == "none") {
									targetRow = y - 1;
									while (targetRow > 0 && allTiles[targetColumn + targetRow * bounds.size.x] == null) {
										if (navMesh[targetRow, targetColumn].type != "none") {
											NavLink link = new NavLink();
											//linkScore is half the length of the fall
											link.Init(navMesh[targetRow, targetColumn], (y - targetRow) / 2f, new JumpTrajectory());
											navMesh[y, x].AddNavLink(link);
											break;
										}
										targetRow--;
									}
								}
							}
						}
					}
					//generate jumping navlinks
					if (navMesh[y, x].type != "none") {
						float maxJumpDistance = 6f; // 8f
						for (int y2 = 0; y2 < navMesh.GetLength(0); y2++) {
							for (int x2 = 0; x2 < navMesh.GetLength(1); x2++) {
								float xOffset = navMesh[y2, x2].coors.x - navMesh[y, x].coors.x;
								float yOffset = navMesh[y2, x2].coors.y - navMesh[y, x].coors.y;
								//yOffset >= 0 &&
								if (((navMesh[y2, x2].type == "rightEdge" && xOffset <= 0) || (navMesh[y2, x2].type == "leftEdge" && xOffset >= 0) || navMesh[y2, x2].type == "solo") && Vector2.Distance(navMesh[y, x].coors, navMesh[y2, x2].coors) < maxJumpDistance && navMesh[y2, x2].platformIndex != navMesh[y, x].platformIndex) {
									bool foundValidJump = false;
									//change the amount of jump time that it takes to travel to target navpoint in order to change angle of initial jump
									float minTime = 0.5f; float maxTime = 0.7f; //0.7f
									float timeDivisions = 3;
									float time = minTime;

									for (int i = 0; i < timeDivisions; i++) {
										time = minTime + ((i / (timeDivisions - 1)) * (maxTime - minTime));
										//Debug.Log(i + ", " + time);
										Vector2 velocity = new Vector2(xOffset / time, (yOffset / time) - (0.5f * gravity.y * time));
										JumpTrajectory jump = new JumpTrajectory();
										jump.Init(new Vector2(navMesh[y, x].coors.x, navMesh[y, x].coors.y), velocity, time, gravity);
										jump.CalculatePoints();
										bool jumpBlocked = false;
										for (int j = 1; j < jump.GetJumpPoints().Count; j++) {
											//Vector3Int localPlace = new Vector3Int(((Vector2)jump.GetJumpPoints()[j]).x, ((Vector2)jump.GetJumpPoints()[j]).y, (int)tilemap.transform.position.y);
											//Vector3 place = tilemap.CellToWorld(localPlace);
											int tileIndexY = (int)(((Vector2)jump.GetJumpPoints()[j]).y - tilemap.cellBounds.yMin);
											int tileIndexX = (int)(((Vector2)jump.GetJumpPoints()[j]).x - tilemap.cellBounds.xMin);
											//TileBase tile = allTiles[tileIndexX + tileIndexY * bounds.size.x];
											//change collider size (1f, 1f) if enemy size ever changes
											Collider2D pointCollider = Physics2D.OverlapBox(((Vector2)jump.GetJumpPoints()[j]), new Vector2(1f, 1f), 0f, groundLayers);
											RaycastHit2D upBlocked = Physics2D.Raycast(navMesh[y, x].coors, Vector2.up, navMesh[y2, x2].coors.y - navMesh[y, x].coors.y, groundLayers);
											//Debug.Log(pointCollider != null);
											//allTiles[tileIndexX + tileIndexY * bounds.size.x] != null &&
											//pointCollider != null
											//(Mathf.Abs(xOffset) != 1f || upBlocked.collider != null || yOffset < 1f)
											//(Mathf.Abs(xOffset) != 1f ? true : (upBlocked.collider != null && yOffset < 1f)) &&
											//Vector2.Distance((Vector2)jump.GetJumpPoints()[j], navMesh[y2, x2].coors) > 0.3f &&
											if ((Mathf.Abs(xOffset) != 1f || upBlocked.collider != null || yOffset < 1f) && Vector2.Distance((Vector2)jump.GetJumpPoints()[j], navMesh[y2, x2].coors) > 0.3f && (pointCollider != null || (tileIndexX >= 0 && tileIndexX < bounds.size.x && tileIndexY >= 0 && tileIndexY < bounds.size.y && allTiles[tileIndexX + tileIndexY * bounds.size.x] != null))) {
												//Debug.Log("blocked");
												jumpBlocked = true;
												break;
											}
										}
										//if jump was not blocked, add it to the navlink and quit from that target navpoint
										if (!jumpBlocked && jump != new JumpTrajectory() && !foundValidJump) {
											//platformsVisited[navMesh[y, x].platformIndex - 1, navMesh[y2, x2].platformIndex - 1, 0] = true;
											bool addJump = true;
											if (x > 0 && navMesh[y, x - 1].navlinks.Count > 0 && navMesh[y, x - 1].type != "none") {
												for (int n = navMesh[y, x - 1].navlinks.Count - 1; n >= 0; n--) {
													//either check if the time is less, or if the distance is less for criteria
													if (((NavLink)navMesh[y, x - 1].navlinks[n]).destPoint == (NavPoint)navMesh[y2, x2] && ((NavLink)navMesh[y, x - 1].navlinks[n]).jumpToDest != new JumpTrajectory()) {
														//if (time < ((NavLink)navMesh[y, x - 1].navlinks[n]).jumpToDest.timeJumping) {
														if (Vector2.Distance(navMesh[y, x].coors, navMesh[y2, x2].coors) <= Vector2.Distance(navMesh[y, x - 1].coors, navMesh[y2, x2].coors)) {
															navMesh[y, x - 1].navlinks.RemoveAt(n);
														} else {
															addJump = false;
														}
													}
												}
											}
											if (xOffset <= 0 && rightVisited[navMesh[y, x].platformIndex - 1, navMesh[y2, x2].platformIndex - 1]) {
												addJump = false;
											}
											if (addJump) {
												NavLink link = new NavLink();
												//linkScore is 5 times the amount of time it takes to jump (it needs to be high so that A* will prioritize walking)
												link.Init(navMesh[y2, x2], time * 5f, jump);
												navMesh[y, x].AddNavLink(link);
												foundValidJump = true;
												if (xOffset <= 0) {
													rightVisited[navMesh[y, x].platformIndex - 1, navMesh[y2, x2].platformIndex - 1] = true;
												}
												//totalJumps ++;
												break;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			//Debug.Log(totalJumps);
			genNavMesh = false;
		}
		/*GENERATE PATH FOR AI*/
		if (tilemap != null && navMesh != null && genTarget) {
			//find the ending point on the navMesh
			targetPoint = new NavPoint();
			/*if (AIState == States.Flee) {
				float fleeDistance = 20f;
				Vector2 fleeVector = ((Vector2)transform.position - (Vector2)player.transform.position).normalized * fleeDistance;
				//Vector2 fleeVector = (transform.position.x + oppositeVector.x < tileWorldPoints[0, 0].x || transform.position.x + oppositeVector.x > tileWorldPoints[navMesh.GetLength(0) - 1, navMesh.GetLength(1) - 1].x) ? new Vector2(-oppositeVector.x, oppositeVector.y) : oppositeVector;
				//Debug.Log(fleeVector);
				movementPos = (Vector2)transform.position + fleeVector;
			}*/
			if (AIState == States.Patrolling) {
				movementPos = new Vector2(Random.Range(patrolStart.x - 5f, patrolStart.x + 5f), Random.Range(patrolStart.y - 5f, patrolStart.y + 5f));
			} else {
				switch (currTarget) {
					case Targets.Player:
						movementPos = (Vector2)player.transform.position;
						break;
					case Targets.LittleGuy:
						movementPos = (Vector2)littleGuy.transform.position;
						break;
					case Targets.Flee:
						float fleeDistance = 20f;
						Vector2 fleeVector = ((Vector2)transform.position - (Vector2)player.transform.position).normalized * fleeDistance;
						//Vector2 fleeVector = (transform.position.x + oppositeVector.x < tileWorldPoints[0, 0].x || transform.position.x + oppositeVector.x > tileWorldPoints[navMesh.GetLength(0) - 1, navMesh.GetLength(1) - 1].x) ? new Vector2(-oppositeVector.x, oppositeVector.y) : oppositeVector;
						//Debug.Log(fleeVector);
						movementPos = (Vector2)transform.position + fleeVector;
						break;
				}
			}
			for (int y = 0; y < navMesh.GetLength(0); y++) {
				for (int x = 0; x < navMesh.GetLength(1); x++) {
					if (navMesh[y, x].type != "none") {
						RaycastHit2D checkWalls = Physics2D.Raycast(movementPos, ((Vector2)navMesh[y, x].coors - movementPos).normalized, Vector2.Distance(navMesh[y, x].coors, movementPos), groundLayers);
						if ((targetPoint == new NavPoint() || Vector2.Distance(movementPos, navMesh[y, x].coors) < Vector2.Distance(movementPos, targetPoint.coors)) && (AIState == States.Flee || checkWalls.collider == null)) {
							targetPoint = navMesh[y, x];
							target = y * navMesh.GetLength(1) + x;
						}
						//TODO; say (Vector2)
						/*RaycastHit2D checkWalls = Physics2D.Raycast(targetVector2, ((Vector2)navMesh[y, x].coors - (Vector2)player.transform.position).normalized, Vector2.Distance(navMesh[y, x].coors, (Vector2)player.transform.position), groundLayers);
						if ((targetPoint == new NavPoint() || Vector2.Distance((Vector2)player.transform.position, navMesh[y, x].coors) < Vector2.Distance((Vector2)player.transform.position, targetPoint.coors)) && checkWalls.collider == null) {
							targetPoint = navMesh[y, x];
							target = y * navMesh.GetLength(1) + x;
						}*/
					}
				}
			}
			genTarget = false;
		}
		//use the A* algorithm to find the most effecient and fast route from the AI to the player
		if (tilemap != null && genPath && navMesh != null && startPoint != new NavPoint() && targetPoint != new NavPoint()) {
			//find the starting point on the navMesh
			for (int y = 0; y < navMesh.GetLength(0); y++) {
				for (int x = 0; x < navMesh.GetLength(1); x++) {
					if (navMesh[y, x].type != "none") {
						if (startPoint == new NavPoint() || Vector2.Distance(transform.position, navMesh[y, x].coors) < Vector2.Distance(transform.position, startPoint.coors)) {
							startPoint = navMesh[y, x];
							start = y * navMesh.GetLength(1) + x;
						}
					}
				}
			}
			//1 dimensional size of navMesh
			int n = navMesh.GetLength(0) * navMesh.GetLength(1);
			//Debug.Log(target);
			ArrayList outputPath = ReconstructPath(n, start, target, targetPoint);
			if (outputPath != new ArrayList()) {
				currentStep = 0;
				completedPath = outputPath;
			}

			genPath = false;
		}
		//move the AI to the player using the steps listed in the generated path
		// && (AIState == States.Following || AIState == States.Flee)
		if (!flipped && !genNavMesh && !genPath && !genTarget && completedPath.Count > 0) {
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
						if (Vector2.Distance(transform.position, new Vector2(currentLink.destPoint.coors.x, transform.position.y)) >= 0.1f) {
							if (currentLink.destPoint.coors.x < transform.position.x) {
								rb.velocity = new Vector2(-speed, rb.velocity.y);
							} else if (currentLink.destPoint.coors.x > transform.position.x) {
								rb.velocity = new Vector2(speed, rb.velocity.y);
							}
						}
						//if its a fall-link
						if (currentLink.destPoint.coors.y < transform.position.y && Vector2.Distance(transform.position, new Vector2(currentLink.destPoint.coors.x, transform.position.y)) < 0.1f) {
							//Debug.Log("drop");
							rb.rotation = 0f;
							transform.position = new Vector3(currentLink.destPoint.coors.x, transform.position.y, transform.position.z);
						}
					}
					//align AI before it jumps
					if (align && !jumped && (Vector2)transform.position != ((NavPoint)completedPath[currentStep]).coors && Vector2.Distance(transform.position, ((NavPoint)completedPath[currentStep]).coors) < 1f) {
						//Debug.Log("align");
						Align(((NavPoint)completedPath[currentStep]).coors);
					}
					//make AI jump to next step
					if (!align && !jumped && currentLink.jumpToDest.jumpForce != new Vector2()) {
						//Debug.Log("jump" + (Vector2)currentLink.jumpToDest.jumpForce);
						//Debug.Log("jump to: " + currentLink.destPoint.coors);
						//special case if the jump is being impeded by an adjacent block
						if (Mathf.Abs(currentLink.destPoint.coors.x - ((NavPoint)completedPath[currentStep]).coors.x) == 1f) {
							if (Vector2.Distance(transform.position, ((NavPoint)completedPath[currentStep]).coors) < 0.1f) {
								rb.velocity = new Vector2(0f, currentLink.jumpToDest.jumpForce.y * 1.05f);
							}
							if (transform.position.y >= currentLink.destPoint.coors.y) {
								transform.position = new Vector2(currentLink.jumpToDest.jumpForce.x > 0f ? currentLink.destPoint.coors.x - 1 : currentLink.destPoint.coors.x + 1, currentLink.destPoint.coors.y);
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
						// && Vector2.Distance(transform.position, ((NavPoint)completedPath[currentStep]).coors) > 1f
						if (currentLink.jumpToDest.jumpForce != new Vector2() && currentStep < completedPath.Count - 1 && transform.position.y >= ((NavPoint)completedPath[currentStep + 1]).coors.y) {
							//Debug.Log("next");
							currentStep++;
							timeUntilFailSafe = Time.time + failSafeTime;
							//genPath = true;
						}
					}
					/*if (!jumped && Vector2.Distance(transform.position, ((NavPoint)completedPath[currentStep + 1]).coors) < 0.1f) {
						transform.position = ((NavPoint)completedPath[currentStep + 1]).coors;
						rb.rotation = 0f;
					}*/
				}
				if (!jumped && IsGrounded() && currentStep < completedPath.Count - 1 && Vector2.Distance(transform.position, ((NavPoint)completedPath[currentStep + 1]).coors) < 0.1f) {
					//Debug.Log("step");
					currentStep++;
					align = true;
					timeUntilFailSafe = Time.time + failSafeTime;
					if (Vector2.Distance(transform.position, targetVector2) <= outOfFollowRadius && AIState == States.Following) {
						//targetVector2 = (Vector2)player.transform.position;
						genTarget = true;
					}
					genPath = true;
				}
				if (!jumped && IsGrounded() && Vector2.Distance(transform.position, ((NavPoint)completedPath[currentStep]).coors) < 0.1f) {
					if (Vector2.Distance(transform.position, targetVector2) <= outOfFollowRadius && AIState == States.Following) {
						//targetVector2 = (Vector2)player.transform.position;
						genTarget = true;
					}
					genPath = true;
				}
			} else if (currentStep >= completedPath.Count - 1) {
				//align AI at final step if it has reached its destination
				//Debug.Log("STOP");
				if (!jumped && (Vector2)transform.position != ((NavPoint)completedPath[completedPath.Count - 1]).coors && Vector2.Distance(transform.position, ((NavPoint)completedPath[completedPath.Count - 1]).coors) < 1f) {
					Align(((NavPoint)completedPath[completedPath.Count - 1]).coors);
				}
				if ((Vector2)transform.position == ((NavPoint)completedPath[completedPath.Count - 1]).coors) {
					rb.velocity = new Vector2(0f, rb.velocity.y);
					if (AIState == States.Following) {
						AIState = States.Alert;
					} else if (AIState == States.Flee) {
						AIState = States.Resting;
					}
				}
			}
		}
		//Debug.Log(jumped);
		//AI decision making based on different States of the AI
		if (!flipped) {
			//TODO: change length of "sight" for AI
			//RaycastHit2D inSight = Physics2D.CircleCast(transform.position, 0.2f, ((Vector2)targetVector2 - (Vector2)transform.position).normalized, alertRadius, groundLayers);
			RaycastHit2D inSight = Physics2D.Raycast(transform.position, ((Vector2)targetVector2 - (Vector2)transform.position).normalized, alertRadius, groundLayers);
			switch (AIState) {
				case States.Patrolling:
					if (currentStep >= completedPath.Count - 1) {
						speed = 3f;
						//Debug.Log("new pos");
						align = true;
						genTarget = true;
						genPath = true;
					}
					if (Vector2.Distance(transform.position, targetVector2) <= alertRadius && IsGrounded()) {
						if (inSight.collider == null) {
							AIState = States.Alert;
						} else if (player.rb.velocity.magnitude >= 5f && Vector2.Distance(transform.position, targetVector2) <= susRadius) {
							AIState = States.Suspicious;
							timeUntilNotSus = Time.time + susTime;
						}
					}
					break;
				case States.Suspicious:
					if (timeUntilNotSus + 1 - susTime < Time.time && timeUntilNotSus >= Time.time && player.rb.velocity.magnitude >= 5f) {
						AIState = States.Alert;
					} else if (timeUntilNotSus < Time.time) {
						AIState = States.Patrolling;
					}
					break;
				case States.Alert:
					//start to follow player if within a certain range
					//use circle cast to see if a bullet with radius 0.2f would hit player
					if (currTarget == Targets.LittleGuy || Vector2.Distance(transform.position, targetVector2) > alertRadius || inSight.collider != null) {
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
					if (Vector2.Distance(transform.position, targetVector2) > outOfFollowRadius && currentStep >= completedPath.Count - 1) {
						AIState = States.Patrolling;
						patrolStart = (Vector2)transform.position;
					}
					/*if (Vector2.Distance(transform.position, targetVector2) <= alertRadius && inSight.collider == null) {
						AIState = States.Alert;
					}*/
					break;
				case States.Flee:
					break;
				case States.Resting:
					if (this.GetComponent<EnemyManager>().currentHealth < this.GetComponent<EnemyManager>().maxHealth) {
						this.GetComponent<EnemyManager>().currentHealth += 0.1f;
					}
					if (this.GetComponent<EnemyManager>().currentHealth >= this.GetComponent<EnemyManager>().maxHealth) {
						AIState = States.Patrolling;
						patrolStart = (Vector2)transform.position;
					}
					break;
				default:
					Debug.Log("AIState Unknown");
					break;
			}
		}
		if (AIState != States.Flee) {
			if (littleGuy != null) lookAtGuy = Physics2D.CircleCast(transform.position, 0.2f, ((Vector2)littleGuy.transform.position - (Vector2)transform.position).normalized, alertRadius, ~enemyLayer);
			if (littleGuy != null && lookAtGuy.collider != null && (lookAtGuy.collider.gameObject.name.Equals("LittleGuy") || lookAtGuy.collider.gameObject.name.Equals("Shield"))) {
				targetVector2 = (Vector2)littleGuy.transform.position;
				currTarget = Targets.LittleGuy;
				//Debug.Log("LittleGuy");
			} else {
				targetVector2 = (Vector2)player.transform.position;
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
			if (Vector2.Distance(transform.position, targetVector2) <= outOfFollowRadius && AIState == States.Following) {
				//targetVector2 = (Vector2)player.transform.position;
				genTarget = true;
			}
			genPath = true;
		}
		if (AIState != States.Flee && this.GetComponent<EnemyManager>().currentHealth <= 30f && Vector2.Distance(transform.position, player.transform.position) <= alertRadius) {
			AIState = States.Flee;
			currTarget = Targets.Flee;
			speed = 5f;
			align = true;
			genTarget = true;
			genPath = true;
		}
		Debug.Log(AIState);
	}

	//Function to align AI's position with a certain position
	void Align(Vector2 alignPos) {
		if (alignPos.x < transform.position.x) {
			rb.velocity = new Vector2(-speed, rb.velocity.y);
		} else if (alignPos.x > transform.position.x) {
			rb.velocity = new Vector2(speed, rb.velocity.y);
		}
		if (Vector2.Distance(transform.position, alignPos) < 0.05f) {
			align = false;
			transform.position = alignPos;
		}
	}

	double AStar(int n, int start, int target, NavPoint targetPoint, ref NavPoint[] prevPoints) {
		/*if (startPoint != new NavPoint() && targetPoint != new NavPoint()) {
			Debug.Log(start + ", " + target);
		}*/
		NavPoint[] prev = new NavPoint[n];
		double[] gCost = new double[n];
		bool[] visited = new bool[n];

		IndexedPriorityQueue<double> ipq = new IndexedPriorityQueue<double>(n);
		ipq.Insert(start, 0.0);

		for (int i = 0; i < n; i++) {
			gCost[i] = double.PositiveInfinity;
		}
		gCost[start] = 0.0;

		while (ipq.Count > 0) {
			int pointID = ipq.PeekMinKeyIndex();
			visited[pointID] = true;
			double minValue = ipq.Pop();
			//Debug.Log(ipq.Contains(pointID));
			//ipq.Pop();
			//Debug.Log(navMesh.GetLength(1));
			int posX = (pointID % navMesh.GetLength(1));
			int posY = Mathf.FloorToInt(pointID / navMesh.GetLength(1));
			//Debug.Log("(" + posX + ", " + posY + ") : " + minValue);
			foreach (NavLink navLink in navMesh[posY, posX].navlinks) {
				//Debug.Log("link");
				int tileIndexY = (int)(((Vector2)navLink.destPoint.coors).y - tilemap.cellBounds.yMin);
				int tileIndexX = (int)(((Vector2)navLink.destPoint.coors).x - tilemap.cellBounds.xMin);
				//Debug.Log("(" + tileIndexX + ", " + tileIndexY + ")");
				int neighbor = tileIndexY * navMesh.GetLength(1) + tileIndexX;
				if (visited[neighbor]) continue;

				double newGCost = gCost[pointID] + navLink.linkScore;
				if (newGCost < gCost[neighbor]) {
					prev[neighbor] = navMesh[posY, posX];
					gCost[neighbor] = newGCost;
					//heurstic
					int neighborX = (neighbor % navMesh.GetLength(1));
					int neighborY = Mathf.FloorToInt(neighbor / navMesh.GetLength(1));
					int scaleFactor = 50;
					double hCost = Vector2.Distance(navMesh[neighborY, neighborX].coors, targetPoint.coors) / scaleFactor;
					//fCost
					double fCost = newGCost + hCost;
					if (!ipq.Contains(neighbor)) {
						ipq.Insert(neighbor, fCost);
					} else {
						ipq.DecreaseIndex(neighbor, fCost);
					}
				}
			}
			//if reached the target point
			if (pointID == target) {
				//Debug.Log(posX + ", " + posY);
				prevPoints = prev;
				return gCost[target];
			}
		}
		//if the target is unreachable.
  	return double.PositiveInfinity;
	}

	ArrayList ReconstructPath(int n, int start, int target, NavPoint targetPoint) {
		ArrayList path = new ArrayList();
		NavPoint[] prev = new NavPoint[n];
		currGCost = AStar(n, start, target, targetPoint, ref prev);
		//Debug.Log(currGCost);
		if (currGCost == double.PositiveInfinity) return path;
		//Debug.Log(prev[((int)(((Vector2)targetPoint.coors).y - tilemap.cellBounds.yMin) * navMesh.GetLength(1) + (int)(((Vector2)targetPoint.coors).x - tilemap.cellBounds.xMin))]);
		//path.Add(targetPoint);
		for (NavPoint at = targetPoint; at != null; at = prev[((int)(((Vector2)at.coors).y - tilemap.cellBounds.yMin) * navMesh.GetLength(1) + (int)(((Vector2)at.coors).x - tilemap.cellBounds.xMin))]) {
			path.Add(at);
			//Debug.Log("adding: " + at.coors);
		}
		path.Reverse();
		return path;
	}

	//initialize an array with world coordinates for the tiles in the tilemap
	void SetTileWorldPoints() {
		if (tilemap != null && navMesh != null) {
			BoundsInt bounds = tilemap.cellBounds;
			for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax + 1; y++) {
				for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++) {

					Vector3Int localPlace = (new Vector3Int(x, y, (int)tilemap.transform.position.y));
					Vector3 place = tilemap.CellToWorld(localPlace);
					int tileIndexY = (y - tilemap.cellBounds.yMin);
					int tileIndexX = (x - tilemap.cellBounds.xMin);

					tileWorldPoints[tileIndexY, tileIndexX] = new Vector2(place.x + 0.5f, place.y + 0.5f);
				}
			}
		}
	}

	void OnDrawGizmos() {
		//test Debug
		if (tilemap != null && navMesh != null) {
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
		}
		if (completedPath.Count > 0 && currentStep < completedPath.Count - 1) {
			Gizmos.color = Color.black;
			Gizmos.DrawSphere(((NavPoint)completedPath[currentStep + 1]).coors, 0.25f);
		}

		//Gizmos.color = Color.blue;
		//Gizmos.DrawSphere(targetVector2, 0.1f);
	}

	void FixedUpdate () {
		rb.AddForce(gravity);
		Vector3 playerPos = player.transform.position;

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

		//TODO: allow for some more flexibility in patrolling, like ocassionally pathfinding to another platform
		//patrol platform if too far from player and not following
		/*if (!flipped && AIState == States.Patrolling && IsGrounded()) {
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
				transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
			}
		}*/
	}

	//check if bottom of enemy is touching any groundLayers
	public bool IsGrounded() {
		Collider2D onGround = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

		return onGround != null;
	}
}

/*CLASSES USED BY ENEMY PAHTFINDING*/

//NavPoint class
class NavPoint {
	public Vector2 coors = new Vector2();
	public int platformIndex;
	public string type = "none";
	public ArrayList navlinks = new ArrayList();

	public void Init(Vector2 _coors, int _platformIndex, string _type) {
		coors = _coors;
		platformIndex = _platformIndex;
		type = _type;
	}

	public void AddNavLink(NavLink newLink) {
		navlinks.Add(newLink);
	}
}

//NavLink class
class NavLink {
	//destination coors
	public NavPoint destPoint;
	public float linkScore = 1f;
	public JumpTrajectory jumpToDest = new JumpTrajectory();
	//Vector2 jumpToDest = new Vector2();

	public void Init(NavPoint _destPoint, float _linkScore, JumpTrajectory _jumpToDest) {
		destPoint = _destPoint;
		linkScore = _linkScore;
		jumpToDest = _jumpToDest;
	}
}

//class that holds a start position, an initial jump velocity, and calculates future positions in the jump
class JumpTrajectory {
	Vector2 startPoint;
	//Vector2 destination;
	public Vector2 jumpForce;
	public float timeJumping;
	Vector2 gravity;
	ArrayList jumpPoints = new ArrayList();
	//more complexity and shorter time increments means more complex calculations, but takes longer to calculate
	float complexity = 15f; //20

	public void Init(Vector2 _startPoint, Vector2 _jumpForce, float _timeJumping, Vector2 _gravity) {
		//destination = _destination;
		startPoint = _startPoint;
		jumpForce = _jumpForce;
		timeJumping = _timeJumping;
		gravity = _gravity;
		jumpPoints.Add(startPoint);
	}

	//use math from physics to calculate future points in jump by incrementing time in the formula
	public void CalculatePoints() {
		float time = 0;
		for (int i = 0; i < complexity; i++) {
			time += timeJumping / complexity;
			float dx = jumpForce.x * time;
			float dy = (0.5f * gravity.y * Mathf.Pow(time, 2)) + (jumpForce.y * time);
			jumpPoints.Add(new Vector2(startPoint.x + dx, startPoint.y + dy));
		}
	}

	public ArrayList GetJumpPoints() {
		return jumpPoints;
	}
}

//indexed priority queue
public sealed class IndexedPriorityQueue<T> where T : System.IComparable
{
    #region public
    public int Count
    {
        get { return m_count; }
    }

    public T this[int index]
    {
        get
        {
            Assert.IsTrue( index < m_objects.Length && index >= 0,
                           string.Format( "IndexedPriorityQueue.[]: Index '{0}' out of range", index ) );
            return m_objects[index];
        }

        set
        {
            Assert.IsTrue( index < m_objects.Length && index >= 0,
                           string.Format( "IndexedPriorityQueue.[]: Index '{0}' out of range", index ) );
            Set( index, value );
        }
    }

    public IndexedPriorityQueue( int maxSize )
    {
        Resize( maxSize );
    }

    ///
    /// Inserts a new value with the given index
    ///
    /// index to insert at
    /// value to insert
    public void Insert( int index, T value )
    {
        Assert.IsTrue( index < m_objects.Length && index >= 0,
                       string.Format( "IndexedPriorityQueue.Insert: Index '{0}' out of range", index ) );

        ++m_count;

        // add object
        m_objects[index] = value;

        // add to heap
        m_heapInverse[index] = m_count;
        m_heap[m_count] = index;

        // update heap
        SortHeapUpward( m_count );
    }

		public bool Contains(int index) {
			return m_heapInverse[index] != 0;
			//return m_heapInverse[index];
		}

		//peek at the index of the top element of the queue
		public int PeekMinKeyIndex() {
			return m_heap[1];
		}

    ///
    /// Gets the top element of the queue
    ///
    /// The top element
    public T Top()
    {
        // top of heap [first element is 1, not 0]
        return m_objects[m_heap[1]];
    }

    ///
    /// Removes the top element from the queue
    ///
    /// The removed element
    public T Pop()
    {
        Assert.IsTrue( m_count > 0, "IndexedPriorityQueue.Pop: Queue is empty" );

        if ( m_count == 0 )
        {
            return default( T );
        }

        // swap front to back for removal
        Swap( 1, m_count-- );

        // re-sort heap
        SortHeapDownward( 1 );

        // return popped object
        return m_objects[m_heap[m_count + 1]];
    }

    ///
    /// Updates the value at the given index. Note that this function is not
    /// as efficient as the DecreaseIndex/IncreaseIndex methods, but is
    /// best when the value at the index is not known
    ///
    /// index of the value to set
    /// new value
    public void Set( int index, T obj )
    {
        if ( obj.CompareTo( m_objects[index] ) <= 0 )
        {
            DecreaseIndex( index, obj );
        }
        else
        {
            IncreaseIndex( index, obj );
        }
    }

    ///
    /// Decrease the value at the current index
    ///
    /// index to decrease value of
    /// new value
    public void DecreaseIndex( int index, T obj )
    {
        Assert.IsTrue( index < m_objects.Length && index >= 0,
                       string.Format( "IndexedPriorityQueue.DecreaseIndex: Index '{0}' out of range",
                       index ) );
        Assert.IsTrue( obj.CompareTo( m_objects[index] ) <= 0,
                       string.Format( "IndexedPriorityQueue.DecreaseIndex: object '{0}' isn't less than current value '{1}'",
                       obj, m_objects[index] ) );

        m_objects[index] = obj;
        SortUpward( index );
    }

    ///
    /// Increase the value at the current index
    ///
    /// index to increase value of
    /// new value
    public void IncreaseIndex( int index, T obj )
    {
        Assert.IsTrue( index < m_objects.Length && index >= 0,
                      string.Format( "IndexedPriorityQueue.DecreaseIndex: Index '{0}' out of range",
                      index ) );
        Assert.IsTrue( obj.CompareTo( m_objects[index] ) >= 0,
                       string.Format( "IndexedPriorityQueue.DecreaseIndex: object '{0}' isn't greater than current value '{1}'",
                       obj, m_objects[index] ) );

        m_objects[index] = obj;
        SortDownward( index );
    }

    public void Clear()
    {
        m_count = 0;
    }

    ///
    /// Set the maximum capacity of the queue
    ///
    /// new maximum capacity
    public void Resize( int maxSize )
    {
        Assert.IsTrue( maxSize >= 0,
                       string.Format( "IndexedPriorityQueue.Resize: Invalid size '{0}'", maxSize ) );

        m_objects = new T[maxSize];
        m_heap = new int[maxSize + 1];
        m_heapInverse = new int[maxSize];
        m_count = 0;
    }
    #endregion // public

    #region private
    private T[] m_objects;
    private int[] m_heap;
    private int[] m_heapInverse;
    private int m_count;

    private void SortUpward( int index )
    {
        SortHeapUpward( m_heapInverse[index] );
    }

    private void SortDownward( int index )
    {
        SortHeapDownward( m_heapInverse[index] );
    }

    private void SortHeapUpward( int heapIndex )
    {
        // move toward top if better than parent
        while ( heapIndex > 1 &&
                m_objects[m_heap[heapIndex]].CompareTo( m_objects[m_heap[Parent( heapIndex )]] ) < 0 )
        {
            // swap this node with its parent
            Swap( heapIndex, Parent( heapIndex ) );

            // reset iterator to be at parents old position
            // (child's new position)
            heapIndex = Parent( heapIndex );
        }
    }

    private void SortHeapDownward( int heapIndex )
    {
        // move node downward if less than children
        while ( FirstChild( heapIndex ) <= m_count )
        {
            int child = FirstChild( heapIndex );

            // find smallest of two children (if 2 exist)
            if ( child < m_count &&
                 m_objects[m_heap[child + 1]].CompareTo( m_objects[m_heap[child]] ) < 0 )
            {
                ++child;
            }

            // swap with child if less
            if ( m_objects[m_heap[child]].CompareTo( m_objects[m_heap[heapIndex]] ) < 0 )
            {
                Swap( child, heapIndex );
                heapIndex = child;
            }
            // no swap necessary
            else
            {
                break;
            }
        }
    }

    private void Swap( int i, int j )
    {
        // swap elements in heap
        int temp = m_heap[i];
        m_heap[i] = m_heap[j];
        m_heap[j] = temp;

        // reset inverses
        m_heapInverse[m_heap[i]] = i;
        m_heapInverse[m_heap[j]] = j;
    }

    private int Parent( int heapIndex )
    {
        return ( heapIndex / 2 );
    }

    private int FirstChild( int heapIndex )
    {
        return ( heapIndex * 2 );
    }

    private int SecondChild( int heapIndex )
    {
        return ( heapIndex * 2 + 1 );
    }
    #endregion // private
}
