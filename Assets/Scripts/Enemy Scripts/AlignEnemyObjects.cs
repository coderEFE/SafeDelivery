using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignEnemyObjects : MonoBehaviour {
  //public Rigidbody2D rb;
  public Transform feet;
  public Transform firingPoint;
  public HealthBar healthBar;

  // Start is called before the first frame update
  void Start() {

  }

  void FixedUpdate() {
    //rb.AddForce(gravity);
    healthBar.transform.position = new Vector2(transform.position.x, transform.position.y + 1.25f);
    feet.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
    firingPoint.position = transform.position;
  }
}
