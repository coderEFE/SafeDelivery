using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuyMovement : MonoBehaviour {
  public Rigidbody2D rb;
  public Transform shield;
  public Transform feet;
  public float maxHealth;
  public float currentHealth;
  //public Collider2D collider2D;
  bool isFreebody = true;

  Vector2 gravity = new Vector2(0f, -40f);
  public LayerMask groundLayers;
  public LayerMask shieldLayer;
  PlayerMovement player;
  Vector2 axis;

  // Start is called before the first frame update
  void Start() {
    currentHealth = maxHealth;
    player = GameObject.Find("Player").GetComponent<PlayerMovement>();
    Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), GameObject.Find("Player").GetComponent<Collider2D>(), true);
  }

  //TODO: Fix wierd collisions with other objects when being held by player
  // Update is called once per frame
  void Update() {
    if (currentHealth <= 0f) {
			Destroy(gameObject);
		}
    axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    if (Input.GetKeyUp("f") && !isFreebody) {
      //Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), GameObject.Find("Player").GetComponent<Collider2D>(), false);
      transform.parent = null;
      rb.isKinematic = false;
      rb.velocity = player.rb.velocity * 0.8f;
      //transform.position = new Vector3(player.facingRight ? player.transform.position.x - 1f : player.transform.position.x + 1f, player.transform.position.y, player.transform.position.z);
      /*Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      rb.velocity = new Vector2(Mathf.Clamp(mousePos.x - transform.position.x, -1, 1) * 10f, Mathf.Clamp(mousePos.y - transform.position.y, -1, 1) * 10f);
      rb.angularVelocity = Random.Range(-1000f, 1000f);*/
      //Debug.Log(axis);
      //shield.gameObject.GetComponent<SpriteRenderer>().enabled = true;
      isFreebody = true;
    } else if (Vector2.Distance(player.transform.position, transform.position) < 2 && Input.GetKeyUp("f") && isFreebody) {
      //Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), GameObject.Find("Player").GetComponent<Collider2D>(), true);
      transform.parent = player.transform;
      rb.isKinematic = true;
      rb.velocity = new Vector2();
      rb.rotation = 0f;
      shield.gameObject.SetActive(false);
      //shield.gameObject.GetComponent<SpriteRenderer>().enabled = false;
      transform.position = new Vector3(player.transform.position.x - (player.facingDirection/2f), player.transform.position.y, player.transform.position.z);
      isFreebody = false;
    }
    //this.transform.position = collision.gameObject.transform.position;
  }

  void FixedUpdate () {
    if (isFreebody) {
        rb.AddForce(gravity);
        if (!shield.gameObject.GetComponent<ShieldManager>().destroyed && !shield.gameObject.activeSelf && IsGrounded()) shield.gameObject.SetActive(true);
    }
  }

  public bool IsGrounded() {
    Collider2D groundCheck = Physics2D.OverlapCircle(feet.position, 0.5f, groundLayers);

    return groundCheck != null;
  }

  private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("EnemyBullet")) {
			currentHealth -= collision.gameObject.GetComponent<EnemyBullet>().bulletDamage;
		}
	}
}
