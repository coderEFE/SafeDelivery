using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Tilemaps;
using UnityEngine.Assertions;

public class Pathfinding : MonoBehaviour {
  public GameObject body;
  //public Tilemap tilemap;
	//public NavPoint[,] navMesh;
  //public Vector2[,] tileWorldPoints;
  //public LayerMask groundLayers;
  NavigationalMesh nav;
  //might not be necessary var
  public double currGCost = double.PositiveInfinity;

  public NavPoint startPoint = new NavPoint();
	public NavPoint targetPoint = new NavPoint();
  int start = 0;
	int target = 0;
  //BoundsInt bounds;

  // Start is called before the first frame update
  void Start() {
    nav = GameObject.Find("NavigationalMesh").GetComponent<NavigationalMesh>();
    /*if (tilemap != null) {
      tilemap.CompressBounds();
      bounds = tilemap.cellBounds;
      //bounds.SetMinMax(new Vector3Int(bounds.xMin, bounds.yMin, bounds.zMin), new Vector3Int(bounds.xMax, bounds.yMax, bounds.zMax));
    }*/
  }

  // Update is called once per frame
  /*void Update() {

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

  public void GenerateNavMesh (Vector2 gravity, Vector2Int minPoint, Vector2Int maxPoint) {
    if (tilemap != null) {
      //tilemap.CompressBounds();
      //BoundsInt bounds = tilemap.cellBounds;
      bounds.SetMinMax(new Vector3Int(minPoint.x, minPoint.y, bounds.zMin), new Vector3Int(maxPoint.x, maxPoint.y, bounds.zMax));
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

      //GENERATE NAVPOINTS
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
        }
      }
      //GENERATE NAVLINKS
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
  }*/

  public void GenerateTarget (Vector2 movementPos, bool targetThroughWalls) {
    //if (nav.tilemap != null && nav.navMesh != null) {
      //find the ending point on the navMesh
			targetPoint = new NavPoint();
      for (int y = 0; y < nav.navMesh.GetLength(0); y++) {
        for (int x = 0; x < nav.navMesh.GetLength(1); x++) {
          if (nav.navMesh[y, x].type != "none") {
            RaycastHit2D checkWalls = Physics2D.Raycast(movementPos, ((Vector2)nav.navMesh[y, x].coors - movementPos).normalized, Vector2.Distance(nav.navMesh[y, x].coors, movementPos), nav.groundLayers);
            if ((targetPoint == new NavPoint() || Vector2.Distance(movementPos, nav.navMesh[y, x].coors) < Vector2.Distance(movementPos, targetPoint.coors)) && (targetThroughWalls || checkWalls.collider == null)) {
              targetPoint = nav.navMesh[y, x];
              target = y * nav.navMesh.GetLength(1) + x;
            }
          }
        }
      }
      if (targetPoint.type == "none") {
        Debug.Log("none");
        //currGCost = double.PositiveInfinity;
      }
    //}
  }

  public ArrayList GeneratePath () {
    // && targetPoint.type != "none" //nav.tilemap != null && nav.navMesh != null &&
    if (startPoint != new NavPoint() && targetPoint != new NavPoint()) {
			//find the starting point on the navMesh
			for (int y = 0; y < nav.navMesh.GetLength(0); y++) {
				for (int x = 0; x < nav.navMesh.GetLength(1); x++) {
					if (nav.navMesh[y, x].type != "none") {
						if (startPoint == new NavPoint() || Vector2.Distance(body.transform.position, nav.navMesh[y, x].coors) < Vector2.Distance(body.transform.position, startPoint.coors)) {
							startPoint = nav.navMesh[y, x];
							start = y * nav.navMesh.GetLength(1) + x;
						}
					}
				}
			}
			//1 dimensional size of navMesh
			int n = nav.navMesh.GetLength(0) * nav.navMesh.GetLength(1);
			//Debug.Log(startPoint.coors + ", " + targetPoint.coors);
			return ReconstructPath(n, start, target, targetPoint);
		}
    return null;
  }

  double AStar(int n, int start, int target, NavPoint targetPoint, ref NavPoint[] prevPoints) {
  		/*if (startPoint != new NavPoint() && targetPoint != new NavPoint()) {
  			Debug.Log(start + ", " + target + " out of: " + n);
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
  			int posX = (pointID % nav.navMesh.GetLength(1));
  			int posY = Mathf.FloorToInt(pointID / nav.navMesh.GetLength(1));
  			//Debug.Log("(" + posX + ", " + posY + ") : " + minValue);
  			foreach (NavLink navLink in nav.navMesh[posY, posX].navlinks) {
  				//Debug.Log("link");
  				int tileIndexY = (int)(((Vector2)navLink.destPoint.coors).y - nav.bounds.yMin);
  				int tileIndexX = (int)(((Vector2)navLink.destPoint.coors).x - nav.bounds.xMin);
  				//Debug.Log("(" + tileIndexX + ", " + tileIndexY + ")");
  				int neighbor = tileIndexY * nav.navMesh.GetLength(1) + tileIndexX;
  				if (visited[neighbor]) continue;

  				double newGCost = gCost[pointID] + navLink.linkScore;
  				if (newGCost < gCost[neighbor]) {
  					prev[neighbor] = nav.navMesh[posY, posX];
  					gCost[neighbor] = newGCost;
  					//heurstic
  					int neighborX = (neighbor % nav.navMesh.GetLength(1));
  					int neighborY = Mathf.FloorToInt(neighbor / nav.navMesh.GetLength(1));
  					int scaleFactor = 50;
  					double hCost = Vector2.Distance(nav.navMesh[neighborY, neighborX].coors, targetPoint.coors) / scaleFactor;
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
      //Debug.Log("unreachable");
  		//if the target is unreachable.
    	return double.PositiveInfinity;
  	}

  public ArrayList ReconstructPath(int n, int start, int target, NavPoint targetPoint) {
  		ArrayList path = new ArrayList();
  		NavPoint[] prev = new NavPoint[n];
  		currGCost = AStar(n, start, target, targetPoint, ref prev);
  		//Debug.Log(currGCost);
  		if (currGCost == double.PositiveInfinity) return null;
  		//Debug.Log(prev[((int)(((Vector2)targetPoint.coors).y - tilemap.cellBounds.yMin) * navMesh.GetLength(1) + (int)(((Vector2)targetPoint.coors).x - tilemap.cellBounds.xMin))]);
  		//path.Add(targetPoint);
  		for (NavPoint at = targetPoint; at != null; at = prev[((int)(((Vector2)at.coors).y - nav.bounds.yMin) * nav.navMesh.GetLength(1) + (int)(((Vector2)at.coors).x - nav.bounds.xMin))]) {
  			path.Add(at);
  			//Debug.Log("adding: " + at.coors);
  		}
  		path.Reverse();
  		return path;
  	}
}

/*CLASSES USED BY ENEMY PAHTFINDING*/

//NavPoint class
/*public class NavPoint {
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
}*/

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
