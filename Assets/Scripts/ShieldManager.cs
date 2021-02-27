using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldManager : MonoBehaviour {
  public float maxHealth;
  public float currentHealth;
  public bool destroyed = false;

  // Start is called before the first frame update
  void Start() {
    currentHealth = maxHealth;
  }

  // Update is called once per frame
  void Update() {
    if (currentHealth <= 0f && !destroyed) {
			gameObject.SetActive(false);
      destroyed = true;
		}
  }

  private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("EnemyBullet")) {
			currentHealth -= collision.gameObject.GetComponent<EnemyBullet>().bulletDamage;
		}
	}
}
