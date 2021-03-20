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
		pm = this.GetComponent<PlayerMovement>();
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
			//Debug.Log("slash");
			timeUntilAttack = Time.time + attackRate;
		}
	}

	/*void OnDrawGizmos() {
		Gizmos.color = Color.magenta;
		Gizmos.DrawSphere(attackPoint.position, attackRange);
	}*/

	//attack with staff
	void Slash() {
		Collider2D[] collidersHit = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
		//TODO: implement other effects on objects from slash, possibly move littleGuy with it
		foreach (Collider2D collider in collidersHit) {
			if (collider.gameObject.name.Equals("EnemyBody")) {
				collider.GetComponent<EnemyManager>().currentHealth -= attackDamage;
				//could either apply force coming from transform.position or attackPoint.position
				float maxKnockback = 15f;
				float minRatio = 0.3f;
				float knockbackRatio = (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position));
				float knockback = 0f;
				if (knockbackRatio <= 0.1f) {
					knockback = maxKnockback * minRatio;
				} else if (knockbackRatio >= 0.9f) {
					knockback = maxKnockback;
				} else {
					knockback = maxKnockback * knockbackRatio;
				}
				//float knockback = (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position)) <= 0f ? 0f : (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position));
				//float knockback = (1 - Vector2.Distance(attackPoint.position, (Vector2)collider.gameObject.GetComponent<EnemyManager>().transform.position));
				Debug.Log(knockback);
				collider.transform.parent.gameObject.GetComponent<EnemyMovement>().rb.velocity = ((Vector2)(collider.gameObject.GetComponent<EnemyManager>().transform.position - transform.position).normalized * knockback);
			}
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
		Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), this.GetComponent<Collider2D>());
	}
}
