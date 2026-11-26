using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public GameObject blobPrefab;
    public Transform spawnPoint;

    [Header("Split Settings")]
    public float splitTimer = 30f;
    public float mergeDistance = 3.0f;
    public KeyCode splitKey = KeyCode.E;

    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float splitJumpForce = 9f;
    public float mergedJumpForce = 3f;

    [Header("Merge Visual Feedback")]
    public bool showMergeMessages = true;

    private GameObject blobA;
    private GameObject blobB;
    private bool isSplit = false;
    private float currentSplitTimer;
    private bool hasShownCloseMessage = false;

    void Start()
    {
        if (blobPrefab != null && spawnPoint != null)
        {
            GameObject initialBlob = Instantiate(blobPrefab, spawnPoint.position, Quaternion.identity);
            initialBlob.name = "Blob";
            SetupController(initialBlob, KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S, KeyCode.Space, mergedJumpForce);

            Debug.Log("Blob spawned! WASD to move, SPACE to jump, E to split");
        }
        else
        {
            Debug.LogError("Blob Prefab or Spawn Point not assigned!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(splitKey) && !isSplit)
        {
            SplitBlob();
        }

        if (isSplit)
        {
            currentSplitTimer -= Time.deltaTime;

            if (blobA == null || blobB == null)
            {
                Debug.LogWarning("One blob was destroyed! Resetting split state.");
                isSplit = false;
                hasShownCloseMessage = false;
                return;
            }

            float distance = Vector3.Distance(blobA.transform.position, blobB.transform.position);

            if (showMergeMessages && distance < mergeDistance * 1.5f && distance >= mergeDistance)
            {
                if (!hasShownCloseMessage)
                {
                    Debug.Log("Blobs getting close! Distance: " + distance.ToString("F1"));
                    hasShownCloseMessage = true;
                }
            }

            if (distance >= mergeDistance * 1.5f)
            {
                hasShownCloseMessage = false;
            }

            if (distance < mergeDistance &&
                !IsMergeBlocked() &&
                CanMergeSafely())
            {
                MergeBlobs();
            }
        }
    }

    void SplitBlob()
    {
        GameObject originalBlob = GameObject.FindGameObjectWithTag("Player");
        if (originalBlob == null)
        {
            Debug.LogError("No blob found with Player tag!");
            return;
        }

        Vector3 pos = originalBlob.transform.position;
        Destroy(originalBlob);

        // Blob A: yeşil, WASD + Space
        blobA = Instantiate(blobPrefab, pos + Vector3.left * 2.5f, Quaternion.identity);
        blobA.name = "BlobA";
        blobA.transform.localScale = Vector3.one * 0.7f;
        SetupController(blobA, KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S, KeyCode.Space, splitJumpForce);
        SetBlobColor(blobA, Color.green);

        // Blob B: mavi, ok tuşları + Sağ Shift
        blobB = Instantiate(blobPrefab, pos + Vector3.right * 2.5f, Quaternion.identity);
        blobB.name = "BlobB";
        blobB.transform.localScale = Vector3.one * 0.7f;
        SetupController(blobB, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.RightShift, splitJumpForce);
        SetBlobColor(blobB, Color.cyan);

        isSplit = true;
        currentSplitTimer = splitTimer;
        hasShownCloseMessage = false;

        Debug.Log("BLOB SPLIT! Green: WASD + Space | Cyan: Arrows + Right Shift");
    }

    void MergeBlobs()
    {
        if (blobA == null || blobB == null)
        {
            isSplit = false;
            hasShownCloseMessage = false;
            return;
        }

        Vector3 midPoint = (blobA.transform.position + blobB.transform.position) / 2f;

        Destroy(blobA);
        Destroy(blobB);

        GameObject mergedBlob = Instantiate(blobPrefab, midPoint, Quaternion.identity);
        mergedBlob.name = "Blob";
        mergedBlob.transform.localScale = Vector3.one;
        SetupController(mergedBlob, KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S, KeyCode.Space, mergedJumpForce);
        //SetBlobColor(mergedBlob, Color.green);

        isSplit = false;
        hasShownCloseMessage = false;

        Debug.Log("BLOBS MERGED! Press E to split again.");
    }
    
    //Checks that if  we have an obstacle between two blobs
    private bool IsMergeBlocked()
    {
        Vector3 start = blobA.transform.position;
        Vector3 end = blobB.transform.position;

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction.normalized,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<BlobController>() != null)
                continue;

            return true;
        }

        return false;
    }
    
    //Checks that if the big one blob (the gray one) have enogh area for merge
    private bool CanMergeSafely()
    {
        SphereCollider sphere = blobPrefab.GetComponent<SphereCollider>();

        if (sphere == null)
            return false;

        Vector3 midPoint =
            (blobA.transform.position + blobB.transform.position) / 2f;

        float radius = sphere.radius;

        Vector3 center = midPoint + sphere.center;

        Collider[] overlaps = Physics.OverlapSphere(
            center,
            radius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider col in overlaps)
        {
            if (col.GetComponentInParent<BlobController>() != null)
                continue;

            if (col.gameObject.name == "Ground")
                continue;

            return false;
        }

        return true;
    }

    // Bir blob'a tuşlarını, hızını ve zıplama gücünü verir
    void SetupController(GameObject blob, KeyCode left, KeyCode right, KeyCode forward, KeyCode back, KeyCode jump, float jumpForce)
    {
        BlobController c = blob.GetComponent<BlobController>();
        if (c == null) return;

        c.leftKey = left;
        c.rightKey = right;
        c.forwardKey = forward;
        c.backKey = back;
        c.jumpKey = jump;
        c.moveSpeed = moveSpeed;
        c.jumpForce = jumpForce;
    }

    // Blob'un kendisini ve içindeki tüm parçaları (robotun gövdesi, gözü, kolları...) boyar
    void SetBlobColor(GameObject blob, Color color)
    {
        Renderer[] renderers = blob.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;   // her parçanın materyallerinin kopyası
            foreach (Material m in mats)
            {
                m.color = color;
            }
            r.materials = mats;
        }
    }

    public bool IsSplit()
    {
        return isSplit;
    }

    public void WinLevel()
    {
        Debug.Log("LEVEL COMPLETE! Reloading in 2 seconds...");
        Invoke("ReloadScene", 2f);
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}