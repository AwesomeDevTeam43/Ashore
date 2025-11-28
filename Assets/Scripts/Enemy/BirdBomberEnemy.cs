using UnityEngine;

public class BirdBomberEnemy : EnemyBase
{
    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float leftX = -5f;
    [SerializeField] private float rightX = 5f;
    [SerializeField] private bool startMovingRight = true;
    [SerializeField] private bool flipSpriteOnDirection = true;
    [SerializeField] private bool boundsRelativeToSpawn = true;

    [Header("Dropping Attack")]
    [SerializeField] private GameObject droppingsPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropInterval = 3.0f;
    [SerializeField] private float randomIntervalJitter = 0.4f;
    [SerializeField] private bool requireLineBelow = false;
    [SerializeField] private LayerMask linecastBlockers;
    [SerializeField] private float linecastDistance = 30f;

    [Header("Lifecycle")]
    [SerializeField] private bool clampToBoundariesOnStart = true;

    private float _nextDropTime;
    private bool _movingRight;
    private SpriteRenderer _sr;
    private float _leftBoundary;
    private float _rightBoundary;
    private bool _boundsInitialized;
    private float _spawnX;
    private BirdBomber_Stats _typedStats;
    private Enemy_Health _enemyHealth;

    private void Awake()
    {
        _enemyHealth = GetComponent<Enemy_Health>();
        CacheStatsFromAssignedAsset();
        _movingRight = startMovingRight;
        _sr = GetComponent<SpriteRenderer>();
        if (dropPoint == null) dropPoint = this.transform;
        InitializeBoundaries();
    }

    private void Start()
    {
        InitializeHealthFromStats();
        if (clampToBoundariesOnStart)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, _leftBoundary, _rightBoundary);
            transform.position = p;
        }
        ScheduleNextDrop();
    }

    private void Update()
    {
        Patrol();
        TryDrop();
    }

    private void Patrol()
    {
        float dir = _movingRight ? 1f : -1f;
        transform.Translate(Vector3.right * dir * patrolSpeed * Time.deltaTime);

        if (_movingRight && transform.position.x >= _rightBoundary)
        {
            _movingRight = false;
            OnDirectionChanged();
        }
        else if (!_movingRight && transform.position.x <= _leftBoundary)
        {
            _movingRight = true;
            OnDirectionChanged();
        }
    }

    private void OnDirectionChanged()
    {
        if (flipSpriteOnDirection && _sr != null)
        {
            _sr.flipX = !_movingRight; // assume default facing right
        }
    }

    private void TryDrop()
    {
        if (droppingsPrefab == null) return;
        if (Time.time < _nextDropTime) return;

        if (requireLineBelow)
        {
            var origin = dropPoint.position;
            var hit = Physics2D.Raycast(origin, Vector2.down, linecastDistance, linecastBlockers);
            if (!hit) return; // only drop if something (e.g. ground) below within distance
        }

        Instantiate(droppingsPrefab, dropPoint.position, Quaternion.identity);
        ScheduleNextDrop();
    }

    private void ScheduleNextDrop()
    {
        float jitter = randomIntervalJitter > 0f ? Random.Range(-randomIntervalJitter, randomIntervalJitter) : 0f;
        _nextDropTime = Time.time + Mathf.Max(0.5f, dropInterval + jitter);
    }

    private void InitializeBoundaries()
    {
        float baseX;
        if (boundsRelativeToSpawn)
        {
            if (!_boundsInitialized || !Application.isPlaying)
            {
                _spawnX = transform.position.x;
            }
            baseX = _spawnX;
        }
        else
        {
            baseX = 0f;
        }

        _leftBoundary = boundsRelativeToSpawn ? baseX + leftX : leftX;
        _rightBoundary = boundsRelativeToSpawn ? baseX + rightX : rightX;
        if (_leftBoundary > _rightBoundary)
        {
            var temp = _leftBoundary;
            _leftBoundary = _rightBoundary;
            _rightBoundary = temp;
        }
        _boundsInitialized = true;
    }

    public override void SetStats(Enemy_Stats s)
    {
        stats = s;
        base.SetStats(s);
        _typedStats = s as BirdBomber_Stats;
        if (_typedStats == null)
        {
            if (s != null)
            {
                Debug.LogWarning($"{name}: Provided stats is not a BirdBomber_Stats asset.", this);
            }
            return;
        }

        ApplyStatsFromScriptable();
        _movingRight = startMovingRight;
        InitializeBoundaries();
    }

    private void OnValidate()
    {
        InitializeBoundaries();
    }

    // Optional gizmos for patrol range & drop line
    private void OnDrawGizmosSelected()
    {
        float baseX = boundsRelativeToSpawn ? (_boundsInitialized ? _spawnX : transform.position.x) : 0f;
        float previewLeft = boundsRelativeToSpawn ? baseX + leftX : leftX;
        float previewRight = boundsRelativeToSpawn ? baseX + rightX : rightX;
        if (previewLeft > previewRight)
        {
            var tmp = previewLeft;
            previewLeft = previewRight;
            previewRight = tmp;
        }
        Gizmos.color = Color.yellow;
        Vector3 a = new Vector3(previewLeft, transform.position.y, transform.position.z);
        Vector3 b = new Vector3(previewRight, transform.position.y, transform.position.z);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.1f);
        Gizmos.DrawSphere(b, 0.1f);

        if (dropPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(dropPoint.position, 0.08f);
            if (requireLineBelow)
            {
                Gizmos.DrawLine(dropPoint.position, dropPoint.position + Vector3.down * linecastDistance);
            }
        }
    }

    private void CacheStatsFromAssignedAsset()
    {
        if (stats == null)
        {
            _typedStats = null;
            return;
        }

        _typedStats = stats as BirdBomber_Stats;
        if (_typedStats == null)
        {
            Debug.LogWarning($"{name}: Assigned stats is not a BirdBomber_Stats asset.", this);
            return;
        }

        ApplyStatsFromScriptable();
    }

    private void ApplyStatsFromScriptable()
    {
        if (_typedStats == null) return;

        patrolSpeed = _typedStats.patrolSpeed;
        leftX = _typedStats.leftBoundaryOffset;
        rightX = _typedStats.rightBoundaryOffset;
        startMovingRight = _typedStats.startMovingRight;
        flipSpriteOnDirection = _typedStats.flipSpriteOnDirection;
        boundsRelativeToSpawn = _typedStats.boundsRelativeToSpawn;
        clampToBoundariesOnStart = _typedStats.clampToBoundariesOnStart;
        dropInterval = _typedStats.dropInterval;
        randomIntervalJitter = _typedStats.randomIntervalJitter;
        requireLineBelow = _typedStats.requireLineBelow;
        linecastDistance = _typedStats.linecastDistance;
    }

    private void InitializeHealthFromStats()
    {
        if (_enemyHealth == null)
        {
            _enemyHealth = GetComponent<Enemy_Health>();
        }
    }
}
