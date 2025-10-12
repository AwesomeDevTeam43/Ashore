using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;
    ItemData item;

    // Optional callback for when this slot is selected (click). The UI will wire this up.
    public System.Action<InventorySlot> onSelected;

    // Expected hierarchy for automatic wiring (recommended):
    // Slot (this GameObject, has Button)
    //  - Background (Image)
    //  - Icon (Image)  <-- this Image will be used for the item sprite and should be set to raycastTarget = false

    public void AddItem(ItemData newItem)
    {
        item = newItem;
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
                    // prefer a child that is not the background
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
                // Ensure it stretches to fill the slot
                RectTransform goRt = go.GetComponent<RectTransform>();
                goRt.anchorMin = Vector2.zero;
                goRt.anchorMax = Vector2.one;
                goRt.offsetMin = Vector2.zero;
                goRt.offsetMax = Vector2.zero;
                // Put icon above background
                go.transform.SetAsLastSibling();
            }
        }

        if (icon == null)
        {
            Debug.LogWarning($"InventorySlot: no Image assigned for slot '{gameObject.name}'. Cannot display item '{item?.itemName}'");
            return;
        }

        // Set sprite and ensure the icon overlays the slot background instead of replacing it.
        if (item.icon == null)
        {
            Debug.LogWarning($"InventorySlot: item '{item.itemName}' has no icon assigned.");
        }
        icon.sprite = item.icon;
        icon.enabled = (item.icon != null);

        // Make sure the icon doesn't block pointer events so the slot's Button still receives clicks.
        icon.raycastTarget = false;

        // Ensure the icon is drawn above the slot background
        if (icon.transform.parent == this.transform)
        {
            icon.transform.SetAsLastSibling();
        }

        // Ensure any click target (ClickArea or Buttons) are above the icon so they receive pointer events.
        // We prefer a child named "ClickArea" (created by Inventoy_UI) but also handle Buttons on the slot.
        Transform clickArea = transform.Find("ClickArea");
        if (clickArea != null)
        {
            clickArea.SetAsLastSibling();
            Debug.Log($"InventorySlot:AddItem moved ClickArea to top for slot '{gameObject.name}'");
        }
        else
        {
            // Move any child Button(s) to the top so they are hit-tested before the icon
            var childButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in childButtons)
            {
                if (b != null && b.transform.parent == this.transform)
                {
                    b.transform.SetAsLastSibling();
                    Debug.Log($"InventorySlot:AddItem moved Button '{b.gameObject.name}' to top for slot '{gameObject.name}'");
                }
            }
        }

        // Ensure the icon GameObject and parents are active
        if (!icon.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"InventorySlot: icon GameObject for slot '{gameObject.name}' is not active in hierarchy. Activating it.");
            icon.gameObject.SetActive(true);
        }

        // Fix accidental full transparency
        if (icon.color.a == 0f)
        {
            Debug.LogWarning($"InventorySlot: icon Image for slot '{gameObject.name}' has alpha=0. Setting to 1.");
            Color c = icon.color; c.a = 1f; icon.color = c;
        }

        // Log diagnostics to help debug invisible icons
    RectTransform iconRt = icon.GetComponent<RectTransform>();
        string parentChain = "";
        Transform p = transform;
        while (p != null)
        {
            parentChain = p.name + (parentChain.Length > 0 ? "/" + parentChain : "");
            p = p.parent;
        }
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        string canvasInfo = parentCanvas != null ? $"Canvas='{parentCanvas.name}' renderMode={parentCanvas.renderMode}" : "No Canvas parent";

    Debug.Log($"InventorySlot.AddItem DIAG: slot='{gameObject.name}', item='{item?.itemName}', sprite={(item?.icon!=null?item.icon.name:"null")}, enabled={icon.enabled}, activeInHierarchy={icon.gameObject.activeInHierarchy}, color.a={icon.color.a}, rect={iconRt.rect.size}, siblingIndex={icon.transform.GetSiblingIndex()}, parents={parentChain}, {canvasInfo}");
    }

    // Called by the slot's Button OnClick
    public void OnUseItem()
    {
        if (item == null) return;

        EquipmentData ed = item as EquipmentData;
        if (ed != null)
        {
            // Equip using EquipmentManager on the player
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
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
            // Keep raycastTarget false on clear as well
            icon.raycastTarget = false;
        }
    }

    // Expose the stored item for UI queries
    public ItemData GetItem()
    {
        return item;
    }

    // Called by the slot Button (or UI) to indicate selection.
    // This wrapper lets the UI pass itself a callback when wiring slots so
    // the slot can notify the inventory UI which slot was clicked.
    public void Select()
    {
        onSelected?.Invoke(this);
    }
}