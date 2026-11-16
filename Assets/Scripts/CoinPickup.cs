using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(AudioSource))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private Sprite[] coinFrames = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] pickupFrames = Array.Empty<Sprite>();
    [SerializeField] private SpriteRenderer pickupRenderer;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Min(1)] private int scoreValue = 3;
    [SerializeField, Min(1f)] private float coinFramesPerSecond = 8f;
    [SerializeField, Min(1f)] private float pickupFramesPerSecond = 12f;

    private SpriteRenderer coinRenderer;
    private CircleCollider2D pickupTrigger;
    private AudioSource audioSource;
    private bool collected;
    private bool playingEffect;
    private float animationTime;

    private void Awake()
    {
        CacheComponents();
        if (pickupSound != null && pickupSound.loadState == AudioDataLoadState.Unloaded)
            pickupSound.LoadAudioData();
    }

    private void CacheComponents()
    {
        if (coinRenderer == null) coinRenderer = GetComponent<SpriteRenderer>();
        if (pickupTrigger == null) pickupTrigger = GetComponent<CircleCollider2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    // The parent platform calls this once per pooled spawn, after choosing obstacles.
    public void ResetForSpawn(Vector3 localPosition)
    {
        CacheComponents();
        transform.localPosition = localPosition;
        collected = false;
        playingEffect = false;
        animationTime = 0f;
        audioSource.Stop();
        coinRenderer.enabled = true;
        if (coinFrames.Length > 0) coinRenderer.sprite = coinFrames[0];
        pickupTrigger.enabled = true;
        if (pickupRenderer != null)
        {
            pickupRenderer.enabled = false;
            pickupRenderer.sprite = null;
        }
    }

    private void Update()
    {
        if (!collected)
        {
            if (GameManager.instance == null || !GameManager.instance.IsPlaying) return;
            animationTime += Time.deltaTime;
            if (coinFrames.Length > 0)
                coinRenderer.sprite = coinFrames[Mathf.FloorToInt(animationTime * coinFramesPerSecond) % coinFrames.Length];
        }
        else if (playingEffect)
        {
            animationTime += Time.deltaTime;
            int frame = Mathf.FloorToInt(animationTime * pickupFramesPerSecond);
            if (frame >= pickupFrames.Length)
            {
                playingEffect = false;
                pickupRenderer.enabled = false;
                pickupRenderer.sprite = null;
            }
            else pickupRenderer.sprite = pickupFrames[frame];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var manager = GameManager.instance;
        if (collected || manager == null || !manager.IsPlaying ||
            other.GetComponentInParent<PlayerController>() == null) return;

        // Guard first so multiple player contacts cannot score twice.
        collected = true;
        pickupTrigger.enabled = false;
        coinRenderer.enabled = false;
        manager.AddScore(scoreValue);

        animationTime = 0f;
        playingEffect = pickupRenderer != null && pickupFrames.Length > 0;
        if (playingEffect)
        {
            pickupRenderer.sprite = pickupFrames[0];
            pickupRenderer.enabled = true;
        }
        if (pickupSound != null) audioSource.PlayOneShot(pickupSound);
        // Keep this pooled object active until its sound and sparkle finish.
    }

    private void OnDisable()
    {
        playingEffect = false;
        if (audioSource != null) audioSource.Stop();
        if (pickupRenderer != null) pickupRenderer.enabled = false;
    }
}
