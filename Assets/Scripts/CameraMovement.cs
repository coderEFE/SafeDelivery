using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {
	PlayerMovement player;
	GuyMovement buddy;
	private Vector2 velocity;
	float camSpeedRatio = 10f;
	float boundRadius = 5f;
	float smoothSpeed = 0.25f;
	bool playerCam = true;
	float yDeadzone = 0.5f;
	float lookFurtherDistance = 4f;
	Vector2 target = new Vector2();
	//world bounds
	Vector2 boundLowerLeft = new Vector2(-10, -10);
	Vector2 boundTopRight = new Vector2(10, 10);
	//screen boundaries
	Vector2 bottomLeftScreen;
	Vector2 topRightScreen;

	bool freeFall = false;

	// Start is called before the first frame update
	void Start() {
		player = GameObject.Find("Player").GetComponent<PlayerMovement>();
		buddy = GameObject.Find("LittleGuy").GetComponent<GuyMovement>();
		bottomLeftScreen = new Vector2(0, 0);
		topRightScreen = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
	}

	// Update is called once per frame
	void Update() {
		if (Input.GetKeyUp("c")) {
			playerCam = !playerCam;
		}
	}

	void FixedUpdate()
	{

		bool groundBelow = player.groundBelow.collider;
		bool ceilingAbove = player.ceilingAbove.collider;
		//TODO: I don't need the hitGround and hitCeiling any more
		bool hitGround = player.hitGround.collider;
		bool hitCeiling = player.hitCeiling.collider;

		Vector2 screenOffset = new Vector2();
		Vector2 smoothOffset = new Vector2();
		/*if (Mathf.Abs(player.rb.velocity.x) > 10f) {
		    screenOffset.x = player.rb.velocity.x * 0.1f;
		   } else {*/
		screenOffset.x = 0f;
		//}
		/*if ((groundBelow || ceilingAbove) && !freeFall) {
		    screenOffset.y = 0f;
		    smoothSpeed = 0.25f;
		    //Debug.Log("On surface");
		   } else {
		    screenOffset.y = player.rb.velocity.y * 0.12f;
		    freeFall = true;
		    smoothSpeed = 0.1f;
		    //Debug.Log("freefall");
		   }
		   if (freeFall) {
		    //screenOffset.y = player.rb.velocity.y * 0.5f;
		    //Debug.Log("crash landing");
		    if (screenOffset.y == 0f) {
		        freeFall = false;
		    }
		   }*/
		//Debug.Log(smoothSpeed);
		Vector2 playerScreenPos = new Vector2(player.transform.position.x - transform.position.x, player.transform.position.y - transform.position.y);
		//determine if player is in extreme top or bottom of screen, and if so, set freeFall to true
		if (playerScreenPos.y > (topRightScreen.y / 1.5f) || playerScreenPos.y < -(topRightScreen.y / 1.5f)) {
			freeFall = true;
		} else if (player.colliding && Mathf.Abs(player.rb.velocity.y) <= 0.5f) {
			freeFall = false;
		}
		//change screenOffset based on whether player is in freeFall or not
		if (freeFall) {
			screenOffset.y = player.rb.velocity.y * 0.33f;
			smoothSpeed = 0.3f;
		} else {
			screenOffset.y = 0f;
			//look further down or up
			if (player.colliding && Mathf.Abs(player.rb.velocity.y) <= 0.5f) {
				float yAxis = Input.GetAxisRaw("Vertical");
				if (yAxis >= yDeadzone) {
					screenOffset.y = lookFurtherDistance;
					smoothSpeed = 0.4f;
				} else if (yAxis <= -yDeadzone) {
					screenOffset.y = -lookFurtherDistance;
					smoothSpeed = 0.4f;
				} else {
					smoothSpeed = 0.25f;
				}
			}
		}

		/*if (Mathf.Abs(player.rb.velocity.y) > 10f) {
		    screenOffset.y = player.rb.velocity.y * 0.1f;
		   } else {
		    screenOffset.y = 0f;
		   }*/

		//screenOffset.x = Mathf.Clamp(screenOffset.x, -10f, 10f);
		//screenOffset.y = Mathf.Clamp(screenOffset.y, -10f, 10f);

		//smoothOffset.x = Mathf.SmoothDamp(smoothOffset.x, screenOffset.x, ref velocity.x, 0.1f);
		//smoothOffset.y = Mathf.SmoothDamp(smoothOffset.y, screenOffset.y, ref velocity.y, 0.1f);



		float posX = Mathf.SmoothDamp(transform.position.x, (playerCam || GameObject.Find("LittleGuy") == null ? player.transform.position.x + screenOffset.x : buddy.transform.position.x), ref velocity.x, smoothSpeed);
		float posY = Mathf.SmoothDamp(transform.position.y, (playerCam || GameObject.Find("LittleGuy") == null ? player.transform.position.y + screenOffset.y : buddy.transform.position.y), ref velocity.y, smoothSpeed);
		transform.position = new Vector3(posX, posY, -10f);

		//TODO: cap camera off at boundaries of the current level/world
		//posX = Mathf.Clamp(posX, boundLowerLeft.x, boundUpperRight.x);
		//posY = Mathf.Clamp(posY, boundLowerLeft.y, boundUpperRight.y);

		/*EnemyMovement enemy = GameObject.Find("Enemy").GetComponent<EnemyMovement>();
		float eX = Mathf.SmoothDamp(transform.position.x, enemy.transform.position.x, ref velocity.x, 0.25f);
		float eY = Mathf.SmoothDamp(transform.position.y, enemy.transform.position.y, ref velocity.y, 0.25f);
		transform.position = new Vector3(eX, eY, -10f);*/

		//transform.position = new Vector3(Mathf.Clamp(posX, playerPos.x - boundRadius, playerPos.x + boundRadius), Mathf.Clamp(posY, playerPos.y - boundRadius, playerPos.y + boundRadius), -10f);
		/*if (playerCam) {
		   //if (Mathf.Sqrt(Mathf.Pow(playerPos.x - transform.position.x, 2) + Mathf.Pow(playerPos.y - transform.position.y, 2)) > boundRadius) {
		    if (playerPos.x < transform.position.x) {
		      transform.position = transform.position + new Vector3(-movementAcceleration, 0, 0);
		      movementAcceleration += 0.001f;
		    }
		    transform.position = new Vector3(transform.position.x + (playerPos.x - transform.position.x) / camSpeedRatio, transform.position.y + (playerPos.y - transform.position.y) / camSpeedRatio, -10);
		   //}
		   } else {
		   transform.position = new Vector3(transform.position.x + (buddyPos.x - transform.position.x) / camSpeedRatio, transform.position.y + (buddyPos.y - transform.position.y) / camSpeedRatio, -10);
		   }*/
	}
}
