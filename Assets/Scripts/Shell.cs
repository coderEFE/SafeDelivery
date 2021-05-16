using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shell : MonoBehaviour {
  public Rigidbody2D rb;
    Vector2 gravity = new Vector2(0f, -40f);
    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void FixedUpdate() {
      rb.AddForce(gravity);
    }
}
