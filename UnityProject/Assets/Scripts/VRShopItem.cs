using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRShopItem : MonoBehaviour
{
    [SerializeField] private string itemName = "Milk";

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"На объекте {name} отсутствует XR Simple Interactable.");
            return;
        }

        interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Debug.Log($"Выбран товар: {itemName}");

        ShopTaskManager manager = FindFirstObjectByType<ShopTaskManager>();

        if (manager != null)
        {
            manager.RegisterSelection(itemName);
        }
        else
        {
            Debug.LogError("На сцене не найден ShopTaskManager!");
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelected);
    }
}