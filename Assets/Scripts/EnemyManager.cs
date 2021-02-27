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
		Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), GameObject.Find("LittleGuy").GetComponent<Collider2D>(), true);
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
		healthBar.transform.position = new Vector2(transform.position.x, transform.position.y + 1.25f);
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Bullet")) {
			currentHealth -= collision.gameObject.GetComponent<Bullet>().bulletDamage;
		}
	}
}
