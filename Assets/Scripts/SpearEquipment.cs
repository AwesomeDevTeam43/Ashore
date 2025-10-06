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
            Rigidbody rb = spear.GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                rb.AddForce(throwPoint.forward * throwForce, ForceMode.Impulse);
            }
        }
    }
}