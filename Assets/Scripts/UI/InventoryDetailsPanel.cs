using System.Text;
using UnityEngine;
using TMPro;

// Simple info panel for inventory selection: shows item name, description, and crafting cost if craftable.
public class InventoryDetailsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI craftHeaderText;
    [SerializeField] private TextMeshProUGUI craftCostText;

    private void Awake()
    {
        // Auto-find labels by common names if not assigned
        if (nameText == null) nameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (descriptionText == null) descriptionText = transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        if (craftHeaderText == null) craftHeaderText = transform.Find("CraftHeader")?.GetComponent<TextMeshProUGUI>();
        if (craftCostText == null) craftCostText = transform.Find("CraftCost")?.GetComponent<TextMeshProUGUI>();
    }

    public void Show(ItemData item, int quantity)
    {
        if (item == null)
        {
            Clear();
            return;
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (nameText) nameText.text = item.itemName;
        if (descriptionText) descriptionText.text = item.description;

        if (item.isCraftable && item.craftIngredients != null && item.craftIngredients.Count > 0)
        {
            if (craftHeaderText) craftHeaderText.text = "Crafting Cost";
            if (craftCostText)
            {
                var sb = new StringBuilder();
                foreach (var ing in item.craftIngredients)
                {
                    if (ing == null || ing.material == null) continue;
                    sb.AppendLine($"- {ing.material.itemName} x{Mathf.Max(1, ing.amount)}");
                }
                craftCostText.text = sb.ToString();
            }
        }
        else
        {
            if (craftHeaderText) craftHeaderText.text = string.Empty;
            if (craftCostText) craftCostText.text = string.Empty;
        }

    }

    public void Clear()
    {
        if (nameText) nameText.text = string.Empty;
        if (descriptionText) descriptionText.text = string.Empty;
        if (craftHeaderText) craftHeaderText.text = string.Empty;
        if (craftCostText) craftCostText.text = string.Empty;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}
