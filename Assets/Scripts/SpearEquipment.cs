using UnityEngine;

public class SpearEquipment : Equipment
{
    [SerializeField] private GameObject spearPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 20f;

    void Start()
    {

    }

    void Update()
    {

    }

    public override void OnUse()
    {
        ThrowSpear();
        OnUnequip();
    }

    private void ThrowSpear()
    {
        if (spearPrefab != null && throwPoint != null)
        {
            GameObject spear = Instantiate(spearPrefab, throwPoint.position, throwPoint.rotation);
            Rigidbody2D rb = spear.GetComponent<Rigidbody2D>();
            
            if (rb != null)
            {
                Vector2 throwDirection = transform.parent.localScale.x > 0 ? Vector2.right : Vector2.left;
                rb.AddForce(throwDirection * throwForce, ForceMode2D.Impulse);
            }
        }
    }
}