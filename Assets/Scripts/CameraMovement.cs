using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    float camSpeedRatio = 10f;
    float boundRadius = 5f;
    bool playerCam = true;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update() {
      if (Input.GetKeyUp("c")) {
        playerCam = !playerCam;
      }
    }

    void FixedUpdate()
    {
        Vector3 playerPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
        Vector3 buddyPos = GameObject.Find("LittleGuy").GetComponent<GuyMovement>().transform.position;
        if (playerCam) {
          //if (Mathf.Sqrt(Mathf.Pow(playerPos.x - transform.position.x, 2) + Mathf.Pow(playerPos.y - transform.position.y, 2)) > boundRadius) {
            /*if (playerPos.x < transform.position.x) {
              transform.position = transform.position + new Vector3(-movementAcceleration, 0, 0);
              movementAcceleration += 0.001f;
            }*/
            transform.position = new Vector3(transform.position.x + (playerPos.x - transform.position.x) / camSpeedRatio, transform.position.y + (playerPos.y - transform.position.y) / camSpeedRatio, -10);
          //}
        } else {
          transform.position = new Vector3(transform.position.x + (buddyPos.x - transform.position.x) / camSpeedRatio, transform.position.y + (buddyPos.y - transform.position.y) / camSpeedRatio, -10);
        }
    }
}
