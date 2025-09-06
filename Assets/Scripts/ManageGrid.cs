using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// fuer effektives Speichern der GridCubes und Finden des naechsten GridCubes zustaendig
public class ManageGrid : MonoBehaviour
{
    [System.Serializable]
    public class GridSocketInfo
    {
        public GameObject cube;
        public XRSocketInteractor socket;
    }
    private Vector2Int? previousCell = null; // merken, welche Zelle aktiv war
    public List<GameObject> gridCubes = new List<GameObject>();
    private float cellHeight = 0.0066f;
    private float cellWidth = 0.0228f;
    private Dictionary<Vector2Int, List<GridSocketInfo>> spatialGrid = new();

    public void BuildSpatialGrid()
    {
        foreach (var cube in gridCubes)
        {
            var socket = cube.GetComponent<XRSocketInteractor>();
            if (socket == null) continue;

            var info = new GridSocketInfo { cube = cube, socket = socket };
            Vector2Int cell = GetCellIndex(cube.transform.position.x, cube.transform.position.y);

            if (!spatialGrid.ContainsKey(cell))
                spatialGrid[cell] = new List<GridSocketInfo>();

            spatialGrid[cell].Add(info);
        }
    }

    private Vector2Int GetCellIndex(float x, float y)
    {
        return new Vector2Int(
            Mathf.FloorToInt(x / cellWidth),
            Mathf.FloorToInt(y / cellHeight)
        );
    }

    public void FindClosestAvailableGridCube(Vector3 attachPoint)
    {

        Vector2Int currentCell = GetCellIndex(attachPoint.x, attachPoint.y);
        
        if (previousCell.HasValue && previousCell.Value == currentCell) return; // Falls es dieselbe Zelle ist wie vorher, nichts tun

        foreach (var list in spatialGrid.Values)
        {
            foreach (var info in list)
            {
                info.socket.enabled = false;

            }
        }

        spatialGrid.TryGetValue(currentCell, out var infos);

        if (infos?.Count > 0)
        {
                infos[0].socket.enabled = true;
                previousCell = currentCell;
        }
    }
}
