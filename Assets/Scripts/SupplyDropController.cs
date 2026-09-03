using UnityEngine;

public class SupplyDropController : MonoBehaviour
{
    public GameObject supplyPrefab;

    public void DropSupplies(Vector3 position)
    {
        Debug.Log(
            "📦 DROPPING EMERGENCY MEDICAL SUPPLIES!"
        );

        if (supplyPrefab != null)
        {
            Instantiate(
                supplyPrefab,
                position + Vector3.up * 2f,
                Quaternion.identity
            );
        }
    }
}