using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NavigationalMesh : MonoBehaviour {
  public Tilemap tilemap;
	public NavPoint[,] navMesh;
  public Vector2[,] tileWorldPoints;
  public LayerMask groundLayers;
  public bool genNavMesh = true;
  Vector2 lastNavMeshCenter;
  PlayerMovement player;
  [HideInInspector] public BoundsInt bounds;
  private BoundsInt absoluteBounds;
  Vector2 gravity = new Vector2(0f, -40f);

  void Start() {
    player = GameObject.Find("Player").GetComponent<PlayerMovement>();
    if (tilemap != null) {
      tilemap.CompressBounds();
      bounds = tilemap.cellBounds;
      absoluteBounds = tilemap.cellBounds;
    }
  }

  void Update() {
    if (Vector2.Distance((Vector2)player.transform.position, lastNavMeshCenter) > 10) {
			genNavMesh = true;
		}
    if (genNavMesh) {
      GenerateNavMesh(new Vector2Int(Mathf.Max(absoluteBounds.xMin, Mathf.CeilToInt(player.transform.position.x - 30)), Mathf.Max(absoluteBounds.yMin, Mathf.CeilToInt(player.transform.position.y - 30))), new Vector2Int(Mathf.Min(absoluteBounds.xMax, Mathf.CeilToInt(player.transform.position.x + 30)), Mathf.Min(absoluteBounds.yMax, Mathf.CeilToInt(player.transform.position.y + 30))));
      //GenerateNavMesh(new Vector2Int(bounds.xMin, bounds.yMin), new Vector2Int(bounds.xMax, bounds.yMax));
      //GenerateNavMesh(new Vector2Int(absoluteBounds.xMin, absoluteBounds.yMin), new Vector2Int(absoluteBounds.xMax, absoluteBounds.yMax - 10));
      lastNavMeshCenter = (Vector2)player.transform.position;
      genNavMesh = false;
    }
  }

  //initialize an array with world coordinates for the tiles in the tilemap
  void SetTileWorldPoints() {
    if (tilemap != null && navMesh != null) {
      //BoundsInt bounds = tilemap.cellBounds;
      for (int y = bounds.yMin; y < bounds.yMax + 1; y++) {
        for (int x = bounds.xMin; x < bounds.xMax; x++) {

          Vector3Int localPlace = (new Vector3Int(x, y, (int)tilemap.transform.position.z));//.y
          Vector3 place = tilemap.CellToWorld(localPlace);
          int tileIndexY = (y - bounds.yMin);
          int tileIndexX = (x - bounds.xMin);

          tileWorldPoints[tileIndexY, tileIndexX] = new Vector2(place.x + 0.5f, place.y + 0.5f);
        }
      }
    }
  }

  public void GenerateNavMesh (Vector2Int minPoint, Vector2Int maxPoint) {
    if (tilemap != null) {
      //tilemap.CompressBounds();
      //bounds = tilemap.cellBounds;
      bounds.SetMinMax(new Vector3Int(minPoint.x, minPoint.y, bounds.zMin), new Vector3Int(maxPoint.x, maxPoint.y, bounds.zMax));
      Debug.Log((bounds.xMax - bounds.xMin) + ", " + (bounds.yMax - bounds.yMin));
      //Debug.Log(bounds.size.x + ", " + bounds.size.y);
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
      for (int y = 0; y < navMesh.GetLength(0); y++) {
        platformStarted = false;
        for (int x = 0; x < navMesh.GetLength(1); x++) {
          TileBase tile = null;
          if (y < bounds.size.y) {
            tile = allTiles[x + y * bounds.size.x];
          }
          //Debug.Log((y + 1) < bounds.size.y);
          if (!platformStarted) {
            //checks if lower tile is not null and a platform hasn't been started, and then add a navPoint of type "leftEdge"
            if ((y == absoluteBounds.size.y || (y < bounds.size.y && tile == null)) && (y - 1) >= 0 && allTiles[x + ((y - 1) * bounds.size.x)] != null) {
              platformIndex++;
              navMesh[y, x].Init(tileWorldPoints[y, x], platformIndex, "leftEdge");
              platformStarted = true;
              //Debug.Log(x + ", " + y + ": " + (tile == null));
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
                      int tileIndexY = (int)(((Vector2)jump.GetJumpPoints()[j]).y - bounds.yMin);
                      int tileIndexX = (int)(((Vector2)jump.GetJumpPoints()[j]).x - bounds.xMin);
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
    }
  }
}

//NavPoint class
public class NavPoint {
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
public class NavLink {
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
public class JumpTrajectory {
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
