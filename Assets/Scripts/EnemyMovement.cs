using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour {
    public Transform feet;
    public float speed = 10f;
    public float jumpForce = 20f;
    public Rigidbody2D rb;
    Vector2 gravity = new Vector2(0f, -40f);
    public LayerMask groundLayers;

    public Transform groundCheck;
    bool isFacingRight = true;
    //variables for enemy "kip up"
    float kipupVelocity;
    float kipupTime = 0.25f;

    RaycastHit2D hit;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        hit = Physics2D.Raycast(groundCheck.position, -transform.up, 1f, groundLayers);

        feet.position = transform.position;
        groundCheck.position = new Vector2(transform.position.x + 0.5f, transform.position.y - 0.3f);
    }

    void FixedUpdate () {
        rb.AddForce(gravity);
        Vector3 playerPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
        if (playerPos.x < transform.position.x - 1f) {
            //rb.velocity = new Vector2(-speed, rb.velocity.y);
        } else if (playerPos.x > transform.position.x + 1f) {
            //rb.velocity = new Vector2(speed, rb.velocity.y);
        }
        
        //float trueRotation = (rb.rotation % 360) + 360f;
        //Enemy can do a "kip up" when knocked on its side. This move jumps and rotates the enemy so that it is facing the right direction.
        Debug.Log(rb.rotation);
        if (rb.rotation >= 20f || rb.rotation <= -20f) {
            if (IsGrounded()) {
                if (Vector2.Distance(rb.velocity, new Vector2()) < 0.1f) {
                  rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                }
            } else {
                rb.rotation = Mathf.SmoothDamp(rb.rotation, 0f, ref kipupVelocity, kipupTime);
            }
        }
    }

  public bool IsGrounded() {
    //TODO: change feet position to always face down
    Collider2D onGround = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

    return onGround != null;
  }
}
