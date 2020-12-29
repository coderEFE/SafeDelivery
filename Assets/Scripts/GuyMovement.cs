using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuyMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    //public Collider2D collider2D;
    bool isFreebody = true;
    
    Vector2 gravity = new Vector2(0f, -40f);
    PlayerMovement player;
    Vector2 axis;

    // Start is called before the first frame update
    void Start() {
      player = GameObject.Find("Player").GetComponent<PlayerMovement>();
    }

    //TODO: Fix wierd collisions with other objects when being held by player
    // Update is called once per frame
    void Update() {
      axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
      
      if (Input.GetKeyUp("f") && !isFreebody) {
        transform.parent = null;
        rb.isKinematic = false;
        isFreebody = true;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //rb.velocity = new Vector2(Mathf.Clamp(mousePos.x - transform.position.x, -1, 1) * 10f, Mathf.Clamp(mousePos.y - transform.position.y, -1, 1) * 10f);
        //rb.angularVelocity = Random.Range(-10, 10);
        //Debug.Log(axis);
      }
      if (Vector2.Distance(player.transform.position, transform.position) < 2 && Input.GetKeyUp("e")) {
        transform.parent = player.transform;
        rb.isKinematic = true;
        rb.velocity = new Vector2();
        isFreebody = false;
        transform.position = new Vector3(player.facingRight ? player.transform.position.x - 1 : player.transform.position.x + 1, player.transform.position.y, player.transform.position.z);
      }
      //this.transform.position = collision.gameObject.transform.position;
    }

    void FixedUpdate () {
        if (isFreebody) {
            rb.AddForce(gravity);
        }
    }
    /*private void OnCollisionEnter2D(Collision2D collision) {
      if (collision.gameObject.CompareTag("Player") && Input.GetKeyUp("e")) {
        //spawned = false;
        transform.parent = GameObject.Find("Player").GetComponent<PlayerMovement>().transform;
        rb.isKinematic = true;
        transform.position = new Vector3(GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position.x + 1, GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position.y, GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position.z);
      }
    }*/
}
