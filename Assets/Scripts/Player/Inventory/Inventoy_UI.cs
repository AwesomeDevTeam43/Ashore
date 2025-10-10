using System.Security;
using UnityEngine;

public class Inventoy_UI : MonoBehaviour
{
    private Player_InputHandler player_InputHandler;
    private bool isOpen;

    [SerializeField] private GameObject canvasInventory;
    [SerializeField] private GameObject playerUI;

    void Awake()
    {
        player_InputHandler = GetComponent<Player_InputHandler>();

        canvasInventory.SetActive(false);
        isOpen = false;
    }

    void Update()
    {
        if (player_InputHandler.InventoryActionTriggered)
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        isOpen = !isOpen;
        canvasInventory.SetActive(isOpen);
        playerUI.SetActive(!isOpen);
    }
}
