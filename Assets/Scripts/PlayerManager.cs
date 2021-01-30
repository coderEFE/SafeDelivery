using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {
public float fireRate = 0.25f;
public Transform firingPoint;
float timeUntilFire;
public Transform attackPoint;
public float attackRate = 0.25f;
public float attackRange = 1f;
public float attackDamage = 10f;
float timeUntilAttack;
public LayerMask enemyLayers;
public GameObject bulletPrefab;
Vector2 axis;

PlayerMovement pm;
// Start is called before the first frame update
void Start() {
	pm = GameObject.Find("Player").GetComponent<PlayerMovement>();
}

// Update is called once per frame
void Update() {
	axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
	if (axis.y == 0) {
		attackPoint.position = new Vector2(transform.position.x + (pm.facingRight ? 1f : -1f), transform.position.y + axis.normalized.y);
	} else {
		attackPoint.position = (Vector2) transform.position + axis.normalized;
	}
	//maybe use Time.deltaTime?
	if (Input.GetMouseButtonDown(0) && timeUntilFire < Time.time) {
		Shoot();
		timeUntilFire = Time.time + fireRate;
	}
	//Debug.Log(Input.GetButtonDown("Fire1") + ", " + Input.GetButtonDown("Fire2") + ", " + Input.GetButtonDown("Fire3"));
	if (Input.GetButtonDown("Fire2") && timeUntilAttack < Time.time) {
		Slash();
		Debug.Log("slash");
		timeUntilAttack = Time.time + attackRate;
	}
}

//attack with staff
void Slash() {
	Collider2D[] enemiesToDamage = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
	//TODO: fix occasional null reference errors
	foreach (Collider2D enemy in enemiesToDamage) {
		enemy.GetComponent<EnemyManager>().currentHealth -= attackDamage;
		//could either apply force coming from transform.position or attackPoint.position
		enemy.GetComponent<EnemyMovement>().rb.velocity = ((Vector2)(enemy.GetComponent<EnemyMovement>().transform.position - attackPoint.position).normalized * 15f);
	}
	//for (int i = 0; i < enemiesToDamage.Length; i++) {
	//}
}
//shoot with gun
void Shoot() {
	//TODO: make shooting controls for mobile and controller
	Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
	pm.facingRight = (mousePos.x >= transform.position.x);
	transform.localScale = new Vector3(pm.facingRight ? 1f : -1f, transform.localScale.y, transform.localScale.z);
	/*if (mousePos >= transform.position) {
	   pm.facingRight = true;
	   }*/
	float angle = Mathf.Atan((mousePos.y - firingPoint.position.y) / (mousePos.x - firingPoint.position.x));
	//Debug.Log(angle);
	Bullet bullet = Instantiate(bulletPrefab, firingPoint.position, Quaternion.Euler(new Vector3(0f, 0f, angle))).GetComponent<Bullet>();
	//could set bullet damage here
	Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), GameObject.Find("Player").GetComponent<Collider2D>());
}
}
