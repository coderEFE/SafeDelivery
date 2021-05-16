using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyManager : MonoBehaviour {
	public float maxHealth;
	public float currentHealth;
	public HealthBar healthBar;
	float healthVelocity;
	public TMP_Text enemyStatus;
	RobotEnemy enemy;

	// Start is called before the first frame update
	void Start() {
		enemy = transform.parent.gameObject.GetComponent<RobotEnemy>();
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
		enemyStatus.transform.position = new Vector2(transform.position.x, transform.position.y + 2f);
		if (enemy.AIState == RobotEnemy.States.Suspicious) {
			enemyStatus.text = "?";
		} else if (enemy.AIState == RobotEnemy.States.Alert) {
			enemyStatus.text = "!";
		} else {
			enemyStatus.text = "";
		}
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Bullet")) {
			//Debug.Log();
			currentHealth -= collision.gameObject.GetComponent<Bullet>().bulletDamage;
			//stun enemy to let it get knocked back
			//TODO: make it so that enemy's don't get stunned so much, and that they can't be stunned while fleeing
			//transform.parent.gameObject.GetComponent<RobotEnemy>().SetStunTime((collision.gameObject.GetComponent<Bullet>().bulletSpeed / 10f) * (1f - currentHealth / maxHealth));
			//make enemy suspicious if it is shot while patrolling
			if (transform.parent.gameObject.GetComponent<RobotEnemy>().AIState == RobotEnemy.States.Patrolling) {
				Debug.Log("sus");
				transform.parent.gameObject.GetComponent<RobotEnemy>().TriggerSus();
			}
		}
	}
}
