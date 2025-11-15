using UnityEngine;

public class Drop_Item : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount;

    [SerializeField] private GameObject material_particle;

    public void DropItem()
    {
        for (int i = 0; i < amount; i++)
        {
            Drop(itemData);
        }
    }

    private void Drop(ItemData itemData)
    {
        if (itemData == null || material_particle == null) return;

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            0f
        );

        Vector3 spawnPosition = transform.position + randomOffset;

        GameObject materialParticle = Instantiate(material_particle, spawnPosition, Quaternion.identity);

        Materials materialScript = materialParticle.GetComponent<Materials>();
        if (materialScript != null)
        {
            materialScript.SetItemData(itemData);
        }

        Rigidbody2D rb = materialParticle.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomForce = new Vector2(
                UnityEngine.Random.Range(-2f, 2f),
                UnityEngine.Random.Range(1f, 4f)
            );
            rb.AddForce(randomForce, ForceMode2D.Impulse);
        }
    }
}
