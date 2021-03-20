using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraAdjustments : MonoBehaviour {
    public GameObject player;
    public GameObject buddy;
    PlayerMovement playerMov;
    private CinemachineVirtualCamera baseCam;
    CinemachineComposer comp;
    bool playerCam = true;
    float yDeadzone = 0.5f;
  	float lookFurtherDistance = 4f;

    // Start is called before the first frame update
    void Start() {
      baseCam = GetComponent<CinemachineVirtualCamera>();
      comp = baseCam.GetCinemachineComponent<CinemachineComposer>();
      playerMov = player.GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update() {
      if (Input.GetKeyUp("c") && buddy != null) {
  			playerCam = !playerCam;
        baseCam.Follow = (playerCam ? player.transform : buddy.transform);
  		}
      if (buddy == null) {
        baseCam.Follow = player.transform;
      }

      if (playerMov.colliding && Mathf.Abs(playerMov.rb.velocity.y) <= 0.5f) {
				float yAxis = Input.GetAxisRaw("Vertical");
				if (yAxis >= yDeadzone) {
          comp.m_TrackedObjectOffset.y = lookFurtherDistance;
					//screenOffset.y = lookFurtherDistance;
					//smoothSpeed = 0.4f;
				} else if (yAxis <= -yDeadzone) {
          comp.m_TrackedObjectOffset.y = -lookFurtherDistance;
					//screenOffset.y = -lookFurtherDistance;
					//smoothSpeed = 0.4f;
				} else {
					//smoothSpeed = 0.25f;
				}
			}
    }
}
