using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : MonoBehaviour
{
	public int damage = 1;
	public float speed = 6f;
	public float lifeTime = 6f;

	[Header("Homing")]
	public bool homing = true;
	public float stopFollowDistance;
	public float rotationSpeed = 720f; // degrees per second
	public float rotationOffset = 0f; // tweak if sprite forward is not +X

	[Header("Homing Activation")]
	public float homingActivationDelay = 0f; // delay after curve before homing starts
	public float stopCheckGrace = 0.02f; // grace time after homing starts before applying stop distance

	[Header("Curve Entry")]
	public bool curvedEntry = true;
	public float curveDuration = 0.35f;
	public float curveAngularSpeed = 360f;
	public bool randomizeCurveSide = true;
	public int forcedCurveSide = 1; // 1 = clockwise, -1 = counter-clockwise

	private Rigidbody2D rb;
	private GameObject player;

	private float currentSpeed = 0f;
	private bool following = false;
	private bool curvePhaseActive = false;
	private float curveTimer = 0f;
	private int curveSide = 1;
	private Vector2 moveDir = Vector2.right; // decoupled movement direction
	private bool homingArmed = false;
	private float homingArmedTimer = 0f;
	private float homingActiveTimer = 0f;
	private bool homingHasExitedStopRadius = false;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		player = GameObject.FindGameObjectWithTag("Player");
	}

	private void Start()
	{
		Destroy(gameObject, lifeTime);
	}

	public void Initialize(Vector2 direction, float stopDistance, float speedMultiplier = 1f, int damageAmount = 1)
	{
		damage = damageAmount;
		currentSpeed = speed * speedMultiplier;
		curvePhaseActive = curvedEntry && curveDuration > 0f;
		curveTimer = 0f;
		int fallbackSide = forcedCurveSide == 0 ? 1 : (forcedCurveSide > 0 ? 1 : -1);
		curveSide = randomizeCurveSide ? (Random.value > 0.5f ? 1 : -1) : fallbackSide;
		// Homing is explicitly disabled during curve and can be delayed after curve
		homingArmed = !curvePhaseActive && homingActivationDelay <= 0f;
		homingArmedTimer = 0f;
		homingActiveTimer = 0f;
		homingHasExitedStopRadius = false;
		following = homing && homingArmed;
		stopFollowDistance = stopDistance;

		if (rb == null)
			rb = GetComponent<Rigidbody2D>();

		if (rb == null)
		{
			rb = gameObject.AddComponent<Rigidbody2D>();
			rb.gravityScale = 0f;
			rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		}

		rb.gravityScale = 0f;
		moveDir = direction.sqrMagnitude > 0f ? direction.normalized : (Vector2)transform.right;
		rb.linearVelocity = moveDir * currentSpeed;
		// initial facing
		float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg + rotationOffset;
		transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
	}

	private void FixedUpdate()
	{
		if (curvePhaseActive)
		{
			RunCurvePhase();
			return;
		}

		// After curve, arm homing with optional delay
		if (!homingArmed && homing)
		{
			homingArmedTimer += Time.fixedDeltaTime;
			if (homingArmedTimer >= homingActivationDelay)
			{
				homingArmed = true;
				if (!following)
				{
					following = true;
					homingActiveTimer = 0f;
					homingHasExitedStopRadius = false;
				}
			}
		}

		if (!following) return;
		if (player == null) return;

		// Track how long homing has been active (used for stop grace)
		homingActiveTimer += Time.fixedDeltaTime;

		// Use the player's Collider2D center if available so homing aims to the collider center
		Vector2 playerPos;
		var playerCol = player.GetComponent<Collider2D>();
		if (playerCol != null)
			playerPos = playerCol.bounds.center;
		else
			playerPos = player.transform.position;

		float dist = Vector2.Distance(transform.position, playerPos);
		// Track if we've ever been outside the stop radius since activation
		if (dist > stopFollowDistance) homingHasExitedStopRadius = true;

		// Only stop homing after we've first been outside since activation
		if (homingActiveTimer >= stopCheckGrace && homingHasExitedStopRadius && dist <= stopFollowDistance)
		{
			following = false; // stop homing, keep last velocity/direction
			return;
		}

		Vector2 toPlayer = (playerPos - (Vector2)transform.position);
		float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg + rotationOffset;
		float currentAngle = transform.eulerAngles.z;
		float delta = Mathf.DeltaAngle(currentAngle, targetAngle);
		float maxStep = rotationSpeed * Time.fixedDeltaTime;
		float newAngle = currentAngle + Mathf.Clamp(delta, -maxStep, maxStep);
		transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);

		if (rb != null)
		{
			rb.linearVelocity = transform.right * currentSpeed;
		}
	}

	private void RunCurvePhase()
	{
		if (rb == null)
		{
			curvePhaseActive = false;
			homingArmedTimer = 0f;
			homingArmed = homingActivationDelay <= 0f;
			if (homingArmed && homing)
			{
				following = true;
				homingActiveTimer = 0f;
				homingHasExitedStopRadius = false;
			}
			return;
		}

		curveTimer += Time.fixedDeltaTime;
		float angleDelta = curveSide * curveAngularSpeed * Time.fixedDeltaTime;
		float rad = angleDelta * Mathf.Deg2Rad;
		float c = Mathf.Cos(rad);
		float s = Mathf.Sin(rad);
		var x = moveDir.x;
		var y = moveDir.y;
		moveDir = new Vector2(x * c - y * s, x * s + y * c).normalized;
		rb.linearVelocity = moveDir * currentSpeed;

		// Rotate the visual to match the decoupled direction
		float visualAng = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg + rotationOffset;
		transform.rotation = Quaternion.AngleAxis(visualAng, Vector3.forward);

		if (curveTimer >= curveDuration)
		{
			curvePhaseActive = false;
			homingArmedTimer = 0f;
			homingArmed = homingActivationDelay <= 0f;
			if (homingArmed && homing)
			{
				following = true;
				homingActiveTimer = 0f;
				homingHasExitedStopRadius = false;
			}
		}
	}

	private void OnDrawGizmos()
	{
		// Stop-follow/homing distance
		if (stopFollowDistance > 0f)
		{
			Gizmos.color = new Color(1f, 0.4f, 0.0f, 0.7f);
			Gizmos.DrawWireSphere(transform.position, stopFollowDistance);
		}

		// Velocity vector
		Vector3 vel = Vector3.zero;
		if (rb != null)
			vel = (Vector3)rb.linearVelocity;
		else
			vel = transform.right * currentSpeed;
		Gizmos.color = Color.cyan;
		Gizmos.DrawLine(transform.position, transform.position + vel * 0.1f);
		Gizmos.DrawSphere(transform.position + vel * 0.1f, 0.03f);

		// Target line if homing toward the player
		if (following && player != null)
		{
			Gizmos.color = Color.magenta;
			Vector3 tgtPos;
			var playerCol = player.GetComponent<Collider2D>();
			if (playerCol != null)
				tgtPos = playerCol.bounds.center;
			else
				tgtPos = player.transform.position;
			Gizmos.DrawLine(transform.position, tgtPos);
			Gizmos.DrawSphere(tgtPos, 0.04f);
		}
	}


	private void OnCollisionEnter2D(Collision2D collision)
	{
		handleCollision(collision.gameObject);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		handleCollision(other.gameObject);
	}

	private void handleCollision(GameObject other)
	{
		if (other == null) return;

		if (other.CompareTag("Player"))
		{
			var hs = other.GetComponent<HealthSystem>();
			if (hs != null)
			{
				hs.TakeDamage(damage, gameObject);
			}

			Destroy(gameObject);
			return;
		}

		int layer = other.layer;
		if (layer == LayerMask.NameToLayer("Default") || layer == LayerMask.NameToLayer("Ground"))
		{
			Destroy(gameObject);
		}
	}
}
