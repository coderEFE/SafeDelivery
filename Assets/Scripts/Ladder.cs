using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour {
  PlayerMovement player;
  // Start is called before the first frame update
  void Start() {
    if (GameObject.Find("Player") != null) player = GameObject.Find("Player").GetComponent<PlayerMovement>();
  }

  // Update is called once per frame
  void Update() {

  }

  private void OnTriggerEnter2D (Collider2D collider) {
    if (collider.gameObject.CompareTag("Player")) {
      player.onLadder = true;
    }
  }
  private void OnTriggerExit2D (Collider2D collider) {
    if (collider.gameObject.CompareTag("Player")) {
      player.onLadder = false;
    }
  }
  private void OnTriggerStay2D (Collider2D collider) {
    if (collider.gameObject.CompareTag("Player") && Input.GetAxis("Vertical") != 0 && !player.onLadder && player.IsGrounded()) {
      player.onLadder = true;
    }
  }
}
