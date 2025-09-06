using UnityEngine;

// dafuer zustaendig, Mauer an Grid Cubes zu snappen.

public class SnapWall : MonoBehaviour
{
    private ManageGrid gridManager;
    private Transform attachPoint;
    

    void Start()
    {
        gridManager = FindObjectOfType<ManageGrid>();

        // AttachPoint finden
        attachPoint = FindDirectChildByName("AttachPoint");

    }

    void Update()
    {
        if (gridManager == null || attachPoint == null) return;

        gridManager.FindClosestAvailableGridCube(attachPoint.position);
    }

    public Transform FindDirectChildByName(string targetName)
    {
        foreach (Transform child in transform)
        {
            if (child.name == targetName)
            return child.gameObject.transform;
        }
        return null;
    }
}
