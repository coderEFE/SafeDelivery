using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour {
    public Rigidbody2D rb;
    public Transform firingPoint;
    public GameObject bulletPrefab;

    public float fireRate = 0.4f;
    float bulletSpeed = 15f;
    float timeUntilFire;

    //targeting player vars
    Vector3 playerFirstPos;
    Vector3 playerSecondPos;

    Vector2 logPrediction = new Vector2();
    Vector2 logPrediction2 = new Vector2();

    // Start is called before the first frame update
    void Start() {
      playerFirstPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
    }

    // Update is called once per frame
    void Update() {
      Vector3 playerPos = GameObject.Find("Player").GetComponent<PlayerManager>().transform.position;
      //Debug.Log(timeUntilFire + " : " + Time.time);
      if (timeUntilFire < Time.time + 0.02 && timeUntilFire > Time.time + 0.01) {
        playerFirstPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
      }
      if (Vector2.Distance(transform.position, playerPos) < 10 && timeUntilFire < Time.time) {
        playerSecondPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
        ShootAtPlayer();
        timeUntilFire = Time.time + fireRate;
      }
    }

    void ShootAtPlayer () {
      bool playerGrounded = GameObject.Find("Player").GetComponent<PlayerMovement>().IsGrounded();

      Vector3 playerPos = GameObject.Find("Player").GetComponent<PlayerMovement>().transform.position;
      Vector2 playerVel = GameObject.Find("Player").GetComponent<PlayerMovement>().rb.velocity;
      float distToPlayer = Vector2.Distance(transform.position, playerPos);
      Vector2 playerVelocity = playerSecondPos - playerFirstPos;
      float time = Mathf.Abs((playerPos.y - transform.position.y + playerPos.x - transform.position.x) / (bulletSpeed - playerVelocity.x - playerVelocity.y));
      float bulletVelX = (playerPos.x + (playerVelocity.x * time) - transform.position.x) / (bulletSpeed * time);
      float bulletVelY = (playerPos.y + (playerVelocity.y * time) - transform.position.y) / (bulletSpeed * time);
      //Debug.Log(new Vector2(bulletVelX, bulletVelY).normalized);
      //have the AI predict where the player will be based on player's position and velocity
      //TODO: make this prediction for angle line up with prediction in the EnemyBullet class
      //TODO: change "position" to "firing point" when using bullet starting position
      logPrediction = (Vector2) playerPos + (playerVelocity * 5) * (distToPlayer);
      Debug.Log(playerVelocity);
      //logPrediction = (Vector2) playerPos + (playerVelocity * distToPlayer) + (playerGrounded ? new Vector2(0f, -0.07f) : new Vector2());
      //float distToPrediction = Vector2.Distance(transform.position, prediction);
      //Debug.Log("Prediction: " + prediction + ", Position: " + playerPos);
      float angle = Mathf.Atan((playerPos.y - firingPoint.position.y) / (playerPos.x - firingPoint.position.x));
      //Debug.Log(angle);
      EnemyBullet bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.Euler(new Vector3(0f, 0f, angle))).GetComponent<EnemyBullet>();
      bullet.target = logPrediction;
      logPrediction2 = ((Vector2)transform.position + new Vector2(bulletVelX, bulletVelY).normalized * bulletSpeed * time);
      //bullet.rb.velocity = new Vector2(bulletVelX, bulletVelY).normalized * bulletSpeed;
      Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), GameObject.Find("Enemy").GetComponent<Collider2D>());
    }

    void OnDrawGizmos() {
      Gizmos.color = Color.yellow;
      Gizmos.DrawSphere(logPrediction, 0.5f);
      Gizmos.color = Color.green;
      Gizmos.DrawSphere(logPrediction2, 0.5f);
    }
}
