using UnityEngine;

public class Drop_Materials : MonoBehaviour
{
    [SerializeField] private ItemData stone;
    [SerializeField] private ItemData wood;
    [SerializeField] private ItemData rope;

    [SerializeField] private GameObject material_particle;

    void Start()
    {
        
    }

    void Update()
    {
        // Test dropping materials (remove this in production)
        if (Input.GetKeyDown(KeyCode.O))
        {
            DropMaterial(2, 1, 3); // Drop 2 wood, 1 stone, 3 rope
        }
    }

    private void DropMaterial(int woodAmount, int stoneAmount, int ropeAmount)
    {
        // Drop wood particles
        for (int i = 0; i < woodAmount; i++)
        {
            DropSingleMaterial(wood);
        }

        // Drop stone particles
        for (int i = 0; i < stoneAmount; i++)
        {
            DropSingleMaterial(stone);
        }

        // Drop rope particles
        for (int i = 0; i < ropeAmount; i++)
        {
            DropSingleMaterial(rope);
        }
    }

    private void DropSingleMaterial(ItemData itemData)
    {
        if (itemData == null || material_particle == null) return;

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            0f
        );

        Vector3 spawnPosition = transform.position + randomOffset;

        GameObject materialParticle = Instantiate(material_particle, spawnPosition, Quaternion.identity);

        // Set the ItemData on the spawned particle
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

    // Public method to call from other scripts
    public void DropRandomMaterials()
    {
        int woodCount = Random.Range(0, 3);
        int stoneCount = Random.Range(0, 2);
        int ropeCount = Random.Range(0, 2);
        
        DropMaterial(woodCount, stoneCount, ropeCount);
    }
}