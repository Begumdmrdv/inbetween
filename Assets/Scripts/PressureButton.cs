using UnityEngine;
using System.Collections.Generic;

public class PressureButton : MonoBehaviour
{
    [Header("References")]
    public GameObject gate;

    private readonly HashSet<Collider> pressingColliders = new();

    private void Start()
    {
        UpdateGate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<BlobController>() != null)
        {
            pressingColliders.Add(other);
            UpdateGate();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        pressingColliders.Remove(other);
        UpdateGate();
    }

    private void FixedUpdate()
    {
        pressingColliders.RemoveWhere(collider =>
            collider == null ||
            !collider.enabled ||
            !collider.gameObject.activeInHierarchy);

        UpdateGate();
    }

    private void UpdateGate()
    {
        if (gate != null)
        {
            bool isPressed = pressingColliders.Count > 0;
            gate.SetActive(!isPressed);
        }
    }
}