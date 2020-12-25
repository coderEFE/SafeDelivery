using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour {
    public float speed = 10f;
    public Rigidbody2D rb;
    Vector2 gravity = new Vector2(0f, -40f);
    public LayerMask groundLayers;

    public Transform groundCheck;
    bool isFacingRight = true;

    RaycastHit2D hit;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        hit = Physics2D.Raycast(groundCheck.position, -transform.up, 1f, groundLayers);
    }

    void FixedUpdate () {
        rb.AddForce(gravity);
        Vector3 playerPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
        if (playerPos.x < transform.position.x - 1f) {
            rb.velocity = new Vector2(-speed, rb.velocity.y);
        } else if (playerPos.x > transform.position.x + 1f) {
            rb.velocity = new Vector2(speed, rb.velocity.y);
        }
        //Debug.Log(rb.velocity);
        
    }
}
