using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {
	public float maxHealth;
	public float currentHealth;

	public HealthBar healthBar;
	float healthVelocity;

	// Start is called before the first frame update
	void Start() {
		currentHealth = maxHealth;
		healthBar.SetMaxHealth(maxHealth);
		if (GameObject.Find("LittleGuy") != null) Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), GameObject.Find("LittleGuy").GetComponent<Collider2D>(), true);
	}

	// Update is called once per frame
	void Update() {
		if (healthBar.slider.value != currentHealth) {
			healthBar.SetHealth(Mathf.SmoothDamp(healthBar.slider.value, currentHealth, ref healthVelocity, 0.1f));
		}
		//die
		if (currentHealth <= 0f) {
			Destroy(transform.parent.gameObject);
		}
	}

	void FixedUpdate() {
		//healthBar.transform.position = new Vector2(transform.position.x, transform.position.y + 1.25f);
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Bullet")) {
			//Debug.Log();
			currentHealth -= collision.gameObject.GetComponent<Bullet>().bulletDamage;
			//stun enemy to let it get knocked back
			transform.parent.gameObject.GetComponent<EnemyMovement>().SetStunTime((collision.gameObject.GetComponent<Bullet>().bulletSpeed / 10f) * (1f - currentHealth / maxHealth));
			//make enemy suspicious if it is shot while patrolling
			/*if (transform.parent.gameObject.GetComponent<SmartEnemy>().AIState == EnemyMovement.States.Patrolling) {
				Debug.Log("sus");
				transform.parent.gameObject.GetComponent<SmartEnemy>().TriggerSus();
			}*/
		}
	}
}
