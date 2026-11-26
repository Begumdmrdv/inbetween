using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        BehindBlob,
        Overhead,
        Isometric
    }

    [Header("Camera Style")]
    public CameraMode cameraMode = CameraMode.BehindBlob;

    [Header("Behind Blob Settings")]
    public Vector3 behindOffset = new Vector3(0, 8, -10);
    public float lookDownAngle = -6f;

    [Header("Follow Settings")]
    public float followSpeed = 4f;
    public float rotationSpeed = 3f;

    [Header("Controls")]
    public KeyCode switchTargetKey = KeyCode.Tab;

    [Header("Zoom")]
    public float defaultFOV = 60f;
    public float zoomOutFOV = 75f;
    public float zoomSpeed = 3f;

    private Transform currentTarget;
    private Transform blobA;
    private Transform blobB;
    private GameManager gameManager;
    private Camera cam;
    private bool followingBlobA = true;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        cam = GetComponent<Camera>();
        cam.fieldOfView = defaultFOV;

        FindTarget();
    }

    // LateUpdate: tüm hareketler bittikten SONRA çalışır (kamera için ideal)
    void LateUpdate()
    {
        // Takip ettiğimiz blob silindiyse (split/merge oldu) yenisini bul
        if (currentTarget == null)
        {
            FindTarget();
            if (currentTarget == null) return;
        }

        if (Input.GetKeyDown(switchTargetKey))
        {
            SwitchTarget();
        }

        FollowTarget();
        AdjustZoom();
    }

    void FindTarget()
    {
        if (gameManager != null && gameManager.IsSplit())
        {
            GameObject blobAObj = GameObject.Find("BlobA");
            GameObject blobBObj = GameObject.Find("BlobB");

            if (blobAObj != null) blobA = blobAObj.transform;
            if (blobBObj != null) blobB = blobBObj.transform;

            currentTarget = followingBlobA ? blobA : blobB;
        }
        else
        {
            GameObject mergedBlobObj = GameObject.FindGameObjectWithTag("Player");
            if (mergedBlobObj != null)
            {
                currentTarget = mergedBlobObj.transform;
                followingBlobA = true;
            }
        }
    }

    void SwitchTarget()
    {
        if (gameManager != null && gameManager.IsSplit())
        {
            followingBlobA = !followingBlobA;

            if (followingBlobA && blobA != null)
            {
                currentTarget = blobA;
                Debug.Log("Camera switched to GREEN blob (BlobA)");
            }
            else if (!followingBlobA && blobB != null)
            {
                currentTarget = blobB;
                Debug.Log("Camera switched to CYAN blob (BlobB)");
            }
        }
        else
        {
            Debug.Log("Cannot switch camera - blobs are merged");
        }
    }

    void FollowTarget()
    {
        Vector3 targetPosition;
        Quaternion targetRotation;
        Vector3 direction;

        switch (cameraMode)
        {
            case CameraMode.Overhead:
                targetPosition = currentTarget.position + new Vector3(0, 15, -10);
                direction = currentTarget.position - targetPosition;
                targetRotation = Quaternion.LookRotation(direction);
                break;

            case CameraMode.Isometric:
                targetPosition = currentTarget.position + new Vector3(10, 14.14f, -10);
                direction = currentTarget.position - targetPosition;
                targetRotation = Quaternion.LookRotation(direction);
                break;

            default: // BehindBlob
                targetPosition = currentTarget.position + behindOffset;
                Vector3 lookAtPoint = currentTarget.position + new Vector3(0, 0.5f, 5f);
                direction = lookAtPoint - targetPosition;
                targetRotation = Quaternion.LookRotation(direction);
                targetRotation *= Quaternion.Euler(lookDownAngle, 0, 0);
                break;
        }

        // Yumuşak takip
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    void AdjustZoom()
    {
        float targetFOV = (gameManager != null && gameManager.IsSplit()) ? zoomOutFOV : defaultFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }
}