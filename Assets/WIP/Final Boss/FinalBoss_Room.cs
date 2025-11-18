using UnityEngine;

// Keeps the final boss encounter dormant until the player enters the room trigger.
[RequireComponent(typeof(BoxCollider2D))]
public class FinalBoss_Room : MonoBehaviour
{
	[Header("Encounter Objects")]
	[SerializeField] private OldFriend_Boss boss;
	[SerializeField] private GameObject roomDoor;

	[Header("Trigger Options")]
	[Tooltip("Disable this trigger collider after the encounter starts to prevent re-entry events.")]
	[SerializeField] private bool destroyTriggerAfterActivation = true;

	private bool encounterStarted;
	private BoxCollider2D triggerCollider;

	private void Awake()
	{
		triggerCollider = GetComponent<BoxCollider2D>();
		if (triggerCollider != null)
			triggerCollider.isTrigger = true;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (encounterStarted) return;
		if (other == null || !other.CompareTag("Player")) return;

		encounterStarted = true;

		if (boss != null)
		{
			boss.SetBossActive(true);
		}
		else
		{
			Debug.LogWarning("FinalBoss_Room: Boss reference missing; cannot activate encounter.", this);
		}

		if (roomDoor != null)
		{
			roomDoor.SetActive(true);
		}
		else
		{
			Debug.LogWarning("FinalBoss_Room: Room door reference missing; cannot enable door.", this);
		}

		if (destroyTriggerAfterActivation && triggerCollider != null)
		{
			Destroy(triggerCollider);
		}
	}
}
