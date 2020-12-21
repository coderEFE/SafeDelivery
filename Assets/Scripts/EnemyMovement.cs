using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour {
    public float speed = 2f;
    public Rigidbody2D rb;
    Vector2 gravity = new Vector2(0f, -0.07f);
    public LayerMask groundLayers;

    public Transform groundCheck;
    bool isFacingRight = true;

    RaycastHit2D hit;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        rb.velocity += gravity;

        hit = Physics2D.Raycast(groundCheck.position, -transform.up, 1f, groundLayers);
    }

    void FixedUpdate () {
      if (hit.collider != false) {
        //Debug.Log("Hitting ground");
      } else {
        //Debug.Log("Not hitting ground");
      }
    }
}
