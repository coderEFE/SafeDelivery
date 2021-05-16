using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAfterImageSprite : MonoBehaviour {
  private Transform player;

  private SpriteRenderer SR;
  private SpriteRenderer playerSR;

  private Color color;
  [SerializeField]
  private float activeTime = 0.1f;
  private float timeActivated;
  private float alpha;
  [SerializeField]
  private float alphaSet = 0.8f;
  private float alphaMultiplier = 5f;

  private void OnEnable() {
    SR = GetComponent<SpriteRenderer>();
    player = GameObject.FindWithTag("Player").transform;
    playerSR = player.GetComponent<SpriteRenderer>();

    alpha = alphaSet;
    SR.sprite = playerSR.sprite;
    transform.position = player.position;
    transform.rotation = player.rotation;
    timeActivated = Time.time;
  }

  private void Update() {
    //alpha *= alphaMultiplier;
    alpha -= Time.deltaTime * alphaMultiplier;
    color = new Color(1f, 1f, 1f, alpha);
    SR.color = color;
    transform.localScale = player.localScale;

    if (Time.time >= (timeActivated + activeTime)) {
      PlayerAfterImagePool.Instance.AddToPool(gameObject);
    }
  }
}
