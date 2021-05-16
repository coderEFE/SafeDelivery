using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Escape : MonoBehaviour {
  //GuyMovement buddy;
  // Start is called before the first frame update
  void Start() {
    //if (GameObject.Find("LittleGuy") != null) buddy = GameObject.Find("LittleGuy").GetComponent<GuyMovement>();
  }

  private void OnTriggerEnter2D (Collider2D collider) {
    if (collider.gameObject.CompareTag("LittleGuy")) {
      SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
  }
}
