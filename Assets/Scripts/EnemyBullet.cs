using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour {
    public Rigidbody2D rb;

    public float bulletSpeed = 15f;
    public float bulletDamage = 10f;
    public Vector2 target;

    // Start is called before the first frame update
    void Start() {
      //Vector3 playerPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
      //Vector2 playerVel = GameObject.Find("Player").GetComponent<PlayerMovement>().rb.velocity;
      //Debug.Log(playerVel);
      //have the AI predict where the player will be based on player's position and velocity
      //Vector2 prediction = (Vector2) playerPos + playerVel;
      Vector3 firingPoint = GameObject.Find("Enemy").GetComponent<EnemyShoot>().firingPoint.position;
      //rb.velocity = new Vector2(Mathf.Clamp(mousePos.x - firingPoint.x, -1, 1) * bulletSpeed, Mathf.Clamp(mousePos.y - firingPoint.y, -1, 1) * bulletSpeed);
      rb.velocity = (target - (Vector2)firingPoint).normalized * bulletSpeed;
      //Debug.Log((mousePos - firingPoint).normalized);
    }

    private void OnCollisionEnter2D (Collision2D collision) {
      Destroy(gameObject);
    }
}
