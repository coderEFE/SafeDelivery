using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {
    public float fireRate = 0.25f;
    public Transform firingPoint;
    public GameObject bulletPrefab;

    float timeUntilFire;
    //PlayerMovement pm;
    // Start is called before the first frame update
    void Start() {
      //pm = GameObject.GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update() {
      if (Input.GetMouseButtonDown(0) && timeUntilFire < Time.time) {
        Shoot();
        timeUntilFire = Time.time + fireRate;
      }
    }

    void Shoot () {
      Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      float angle = Mathf.Atan((mousePos.y - firingPoint.position.y) / (mousePos.x - firingPoint.position.x));
      //Debug.Log(angle);
      Bullet bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.Euler(new Vector3(0f, 0f, angle))).GetComponent<Bullet>();
      Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), GameObject.Find("Player").GetComponent<Collider2D>());
    }
}
