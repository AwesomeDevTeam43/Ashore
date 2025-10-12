using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    public Image icon;
    ItemData item;
    int quantity;

    // Optional callback for when this slot is selected (click). The UI will wire this up.
    public System.Action<InventorySlot> onSelected;

    public void AddItem(ItemData newItem, int qty = 1)
    {
        item = newItem;
        quantity = qty;
        
        // Ensure icon reference exists (auto-find if not assigned in Inspector)
        if (icon == null)
        {
            // Prefer a child named "Icon" so we don't accidentally pick the slot background image.
            Transform t = transform.Find("Icon");
            if (t != null) icon = t.GetComponent<Image>();

            // If not found, search child Images (exclude an Image on the slot root itself)
            if (icon == null)
            {
                Image[] imgs = GetComponentsInChildren<Image>(true);
                foreach (var im in imgs)
                {
                    if (im.gameObject == this.gameObject) continue; // skip root (background)
                    icon = im;
                    break;
                }
            }

            // If still not found, create a child GameObject named "Icon" with an Image component
            if (icon == null)
            {
                GameObject go = new GameObject("Icon", typeof(RectTransform));
                go.transform.SetParent(this.transform, false);
                icon = go.AddComponent<Image>();
                RectTransform goRt = go.GetComponent<RectTransform>();
                goRt.anchorMin = Vector2.zero;
                goRt.anchorMax = Vector2.one;
                goRt.offsetMin = Vector2.zero;
                goRt.offsetMax = Vector2.zero;
                go.transform.SetAsLastSibling();
            }
        }

        if (icon == null)
        {
            Debug.LogWarning($"InventorySlot: no Image assigned for slot '{gameObject.name}'. Cannot display item '{item?.itemName}'");
            return;
        }

        // Set sprite
        if (item.icon == null)
        {
            Debug.LogWarning($"InventorySlot: item '{item.itemName}' has no icon assigned.");
        }
        icon.sprite = item.icon;
        icon.enabled = (item.icon != null);
        icon.raycastTarget = false;

        // Rest of your existing AddItem code...
        if (icon.transform.parent == this.transform)
        {
            icon.transform.SetAsLastSibling();
        }

        Transform clickArea = transform.Find("ClickArea");
        if (clickArea != null)
        {
            clickArea.SetAsLastSibling();
        }
        else
        {
            var childButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in childButtons)
            {
                if (b != null && b.transform.parent == this.transform)
                {
                    b.transform.SetAsLastSibling();
                }
            }
        }

        if (!icon.gameObject.activeInHierarchy)
        {
            icon.gameObject.SetActive(true);
        }

        if (icon.color.a == 0f)
        {
            Color c = icon.color; c.a = 1f; icon.color = c;
        }
    }

    // Overload for backward compatibility
    public void AddItem(ItemData newItem)
    {
        AddItem(newItem, 1);
    }

    public void OnUseItem()
    {
        if (item == null) return;

        EquipmentData ed = item as EquipmentData;
        if (ed != null)
        {
            if (EquipmentManager.instance != null)
            {
                EquipmentManager.instance.EquipFromInventory(ed);
            }
            else
            {
                Debug.LogWarning("No EquipmentManager instance found to equip item");
            }
        }
    }

    public void ClearSlot()
    {
        item = null;
        quantity = 0;
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
            icon.raycastTarget = false;
        }
    }

    public ItemData GetItem()
    {
        return item;
    }

    public int GetQuantity()
    {
        return quantity;
    }

    public void Select()
    {
        onSelected?.Invoke(this);
    }
}