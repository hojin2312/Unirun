using UnityEngine;

// 발판으로서 필요한 동작을 담은 스크립트
public class Platform : MonoBehaviour {
    public GameObject[] obstacles; // 장애물 오브젝트들
    private bool stepped = false; // 플레이어 캐릭터가 밟았었는가
    [SerializeField] private CoinPickup coin;
    [SerializeField, Min(0.5f)] private float coinHeight = 1.1f;
    [SerializeField, Min(0.5f)] private float coinEdgePadding = 0.65f;
    [SerializeField, Min(0.5f)] private float coinObstacleClearance = 0.6f;

    public void ApplyTheme(Sprite platformSprite, Sprite obstacleSprite)
    {
        if (platformSprite != null)
            GetComponent<SpriteRenderer>().sprite = platformSprite;

        if (obstacleSprite == null) return;
        foreach (var obstacle in obstacles)
        {
            var renderer = obstacle.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = obstacleSprite;
        }
    }

    // 컴포넌트가 활성화될때 마다 매번 실행되는 메서드
    private void OnEnable() {
        // 발판을 리셋하는 처리
        stepped = false;
        for(int i = 0; i < obstacles.Length; i++)
        {
            if (Random.Range(0, 3) == 0)
            {
                obstacles[i].SetActive(true);
            }
            else
            {
                obstacles[i].SetActive(false);
            }
        }
        PlaceCoin();
    }

    private void PlaceCoin()
    {
        if (coin == null) return;
        var surface = GetComponent<BoxCollider2D>();
        float halfWidth = surface.size.x * 0.5f;
        float padding = Mathf.Min(coinEdgePadding, halfWidth);
        float minX = surface.offset.x - halfWidth + padding;
        float maxX = surface.offset.x + halfWidth - padding;
        float y = surface.offset.y + surface.size.y * 0.5f + coinHeight;

        // Include both edges so a clear position is found even with all three
        // obstacles enabled. Work in local space before the next physics step.
        const int candidates = 17;
        int first = Random.Range(0, candidates);
        for (int i = 0; i < candidates; i++)
        {
            float x = Mathf.Lerp(minX, maxX, ((first + i) % candidates) / (float)(candidates - 1));
            if (IsClearOfObstacles(x))
            {
                coin.ResetForSpawn(new Vector3(x, y, 0f));
                return;
            }
        }

        // If the obstacle layout becomes denser, still spawn exactly one coin,
        // raised above the obstacles instead of skipping it or overlapping them.
        foreach (var obstacle in obstacles)
            if (obstacle.activeSelf)
                y = Mathf.Max(y, ObstacleBounds(obstacle).max.y + coinHeight);
        coin.ResetForSpawn(new Vector3(surface.offset.x, y, 0f));
    }

    private bool IsClearOfObstacles(float x)
    {
        foreach (var obstacle in obstacles)
        {
            if (!obstacle.activeSelf) continue;
            var bounds = ObstacleBounds(obstacle);
            if (x >= bounds.min.x - coinObstacleClearance &&
                x <= bounds.max.x + coinObstacleClearance) return false;
        }
        return true;
    }

    private Bounds ObstacleBounds(GameObject obstacle)
    {
        var box = obstacle.GetComponent<BoxCollider2D>();
        var toPlatform = transform.worldToLocalMatrix * obstacle.transform.localToWorldMatrix;
        var center = toPlatform.MultiplyPoint3x4(box.offset);
        var x = toPlatform.MultiplyVector(new Vector3(box.size.x * 0.5f, 0f, 0f));
        var y = toPlatform.MultiplyVector(new Vector3(0f, box.size.y * 0.5f, 0f));
        var extent = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x), Mathf.Abs(x.y) + Mathf.Abs(y.y), 0f);
        return new Bounds(center, extent * 2f);
    }

    void OnCollisionEnter2D(Collision2D collision) {
        // 플레이어 캐릭터가 자신을 밟았을때 점수를 추가하는 처리
        if(collision.collider.tag == "Player" && !stepped)
        {
            stepped = true;
            GameManager.instance.AddScore(1);
        }
    }
}
