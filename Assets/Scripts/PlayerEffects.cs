using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerEffects : MonoBehaviour
{
    [Serializable]
    public class EffectStyle
    {
        public Sprite[] jump = Array.Empty<Sprite>();
        public Sprite[] land = Array.Empty<Sprite>();
    }

    [SerializeField] private EffectStyle defaultStyle = new EffectStyle();
    [SerializeField] private ScrollingObject worldScroll;
    [SerializeField, Min(1)] private int poolSize = 8;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;
    [SerializeField, Min(0.1f)] private float effectScale = 1f;

    private sealed class EffectInstance
    {
        public SpriteRenderer renderer;
        public Sprite[] frames;
        public float age;
        public float scrollSpeed;
    }

    private EffectStyle currentStyle;
    private EffectInstance[] pool;
    private Transform poolRoot;
    private SpriteRenderer playerRenderer;
    private int nextEffect;

    private void Awake()
    {
        playerRenderer = GetComponent<SpriteRenderer>();
        currentStyle = defaultStyle;
        // Keep effects in world space so they do not follow the jumping player
        // or teleport when a pooled platform is recycled.
        poolRoot = new GameObject("Player Effects Pool").transform;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(poolRoot.gameObject, gameObject.scene);
        pool = new EffectInstance[Mathf.Max(1, poolSize)];
        for (int i = 0; i < pool.Length; i++)
        {
            var effectObject = new GameObject("Dust " + i);
            effectObject.layer = gameObject.layer;
            effectObject.transform.SetParent(poolRoot, false);
            var renderer = effectObject.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = playerRenderer.sharedMaterial;
            renderer.enabled = false;
            pool[i] = new EffectInstance { renderer = renderer };
        }
    }

    public void SetStyle(EffectStyle style)
    {
        currentStyle = style ?? defaultStyle;
    }

    public void PlayJump(Vector3 feetPosition, ScrollingObject surface)
    {
        Play(currentStyle?.jump, feetPosition, surface);
    }

    public void PlayLanding(Vector3 feetPosition, ScrollingObject surface)
    {
        Play(currentStyle?.land, feetPosition, surface);
    }

    private void Play(Sprite[] frames, Vector3 position, ScrollingObject surface)
    {
        if (frames == null || frames.Length == 0 || pool == null ||
            GameManager.instance == null || !GameManager.instance.IsPlaying) return;

        var effect = pool[nextEffect];
        nextEffect = (nextEffect + 1) % pool.Length;
        var scroll = surface != null ? surface : worldScroll;
        effect.frames = frames;
        effect.age = 0f;
        effect.scrollSpeed = scroll != null ? scroll.speed : 0f;
        effect.renderer.transform.position = position;
        effect.renderer.transform.localScale = Vector3.one * effectScale;
        effect.renderer.sortingLayerID = playerRenderer.sortingLayerID;
        effect.renderer.sortingOrder = playerRenderer.sortingOrder + 1;
        effect.renderer.sprite = frames[0];
        effect.renderer.enabled = true;
    }

    private void LateUpdate()
    {
        if (pool == null) return;
        foreach (var effect in pool)
        {
            if (!effect.renderer.enabled) continue;
            effect.age += Time.deltaTime;
            int frame = Mathf.FloorToInt(effect.age * Mathf.Max(1f, framesPerSecond));
            if (frame >= effect.frames.Length)
            {
                effect.renderer.enabled = false;
                effect.renderer.sprite = null;
                continue;
            }

            effect.renderer.sprite = effect.frames[frame];
            if (GameManager.instance != null && GameManager.instance.IsPlaying)
                effect.renderer.transform.position += Vector3.left * effect.scrollSpeed * Time.deltaTime;
        }
    }

    private void OnDisable()
    {
        if (pool == null) return;
        foreach (var effect in pool)
            if (effect.renderer != null) effect.renderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (poolRoot != null) Destroy(poolRoot.gameObject);
    }
}
