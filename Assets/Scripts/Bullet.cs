using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {
    public Rigidbody2D rb;

    public float bulletSpeed = 15f;
    public float bulletDamage = 10f;

    // Start is called before the first frame update
    void Start() {
      Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      Vector3 firingPoint = GameObject.Find("Player").GetComponent<PlayerManager>().firingPoint.position;
      //rb.velocity = new Vector2(Mathf.Clamp(mousePos.x - firingPoint.x, -1, 1) * bulletSpeed, Mathf.Clamp(mousePos.y - firingPoint.y, -1, 1) * bulletSpeed);
      rb.velocity = (new Vector3(mousePos.x, mousePos.y, 0f) - new Vector3(firingPoint.x, firingPoint.y, 0f)).normalized * bulletSpeed;
      //Debug.Log((mousePos - firingPoint).normalized);
    }

    private void OnCollisionEnter2D (Collision2D collision) {
      Destroy(gameObject);
    }
}
