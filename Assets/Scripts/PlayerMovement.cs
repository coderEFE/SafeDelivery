using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    public float movementSpeed = 10;
    public Rigidbody2D rb;
    float mx;

    public Animator anim;

    public float jumpForce = 20f;
    public Transform feet;
    public LayerMask groundLayers;

    Vector2 gravity = new Vector2(0f, -0.07f);

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
      rb.velocity += gravity;

      //change gravity
      if (Input.GetKeyUp("g")) {
        if (gravity.y < 0) {
          gravity.y = 0.07f;
          transform.localScale = new Vector3(transform.localScale.x, -1f, transform.localScale.z);
        } else {
          gravity.y = -0.07f;
          transform.localScale = new Vector3(transform.localScale.x, 1f, transform.localScale.z);
        }
      }

      mx = Input.GetAxisRaw("Horizontal");

      if (Input.GetButtonDown("Jump") && IsGrounded()) {
        Jump();
      }

      //flip horizontally
      if (mx > 0f) {
        transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);
      } else if (mx < 0f) {
        transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);
      }
      //flip vertically
      /*if (gravity.y < 0 && transform.localScale.y == -1f) {
        transform.localScale *= new Vector3(1f, 1f, 1f);
      } else if (gravity.y > 0 && transform.localScale.y == 1f) {
        transform.localScale *= new Vector3(1f, -1f, 1f);
      }*/

      //TODO: change isGrounded criteria
      anim.SetBool("IsGrounded", rb.velocity.y < 0.05f);
    }

    private void FixedUpdate() {
      Vector2 movement = new Vector2(mx * movementSpeed, rb.velocity.y);

      rb.velocity = movement;
    }

    //apply jumpForce
    void Jump() {
        Vector2 movement = new Vector2(rb.velocity.x, jumpForce * (gravity.y > 0 ? -1f : 1f));

      rb.velocity = movement;
    }

    public bool IsGrounded() {
      Collider2D groundCheck = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

      return groundCheck != null;
    }
}
