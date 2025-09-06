using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

// erstellt das Grid: GridCubes, GridLines, movingLine + benoetigte Komponenten

public class GenerateGrid : MonoBehaviour
{
    public int columns = 40;
    public int octaves = 5;
    private int rows;
    private float cellWidth = 0.0228f;
    private float cellHeight = 0.0066f;
    private float gridZPos = 0.5f;

    public GameObject cubePrefab;
    public GameObject linePrefab;
    public GameObject movingLinePrefab;

    public Material hoverMaterial;
    public Material cantHoverMaterial;

    private GameObject gridCubes;
    private GameObject gridLines;

    private GameObject movingLine;
    private float movingLineSpeed;

    [SerializeField] GameObject sceneObject;

    void Start()
    {
        rows = octaves * 7;

        // Parent fuer Grid Cubes erstellen
        gridCubes = new GameObject("GridCubes");
        gridCubes.transform.SetParent(sceneObject.transform);

        CreateGridCubes();

        // Parent fuer Grid Lines erstellen
        gridLines = new GameObject("GridLines");
        gridLines.transform.SetParent(sceneObject.transform);

        CreateGridLines();

        CreateMovingLine();
    }

    void CreateGridCubes()
    {
        ManageGrid manageGrid = FindObjectOfType<ManageGrid>();
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float xPos = x * cellWidth + cellWidth * 0.5f; // um halbe Breite einer Zelle verschieben, damit Cubes richtig angeordnet sind
                float yPos = y * cellHeight + cellHeight * 0.5f;

                // Grid Cube instanziieren mit Parent gridCubes
                Vector3 cubePosition = new Vector3(xPos, yPos, gridZPos + 0.00166667f);
                GameObject gridCube = Instantiate(cubePrefab, cubePosition, Quaternion.identity, gridCubes.transform);
                gridCube.name = "GridCube_" + x + "_" + y;

                gridCube.GetComponent<BoxCollider>().isTrigger = true;

                // fuegt alle GridCubes der gridCube-Liste im ManageGrid-Skript hinzu              
                manageGrid.gridCubes.Add(gridCube);

                // Attach-Point erstellen
                GameObject attachPoint = new GameObject("AttachCube");
                attachPoint.transform.parent = gridCube.transform;
                attachPoint.transform.localPosition = new Vector3(0, 0, -0.05f);

                // Sockets zu Cubes hinzufuegen
                XRSocketInteractor XRInteractorCube = gridCube.AddComponent<XRSocketInteractor>();
                XRInteractorCube.attachTransform = attachPoint.transform; // Attach-Point hinzufuegen
                XRInteractorCube.showInteractableHoverMeshes = true;
                XRInteractorCube.interactionLayers = InteractionLayerMask.GetMask("Default");
                XRInteractorCube.recycleDelayTime = 0;
                XRInteractorCube.interactableHoverMeshMaterial = hoverMaterial;
                XRInteractorCube.interactableCantHoverMeshMaterial = cantHoverMaterial;

            }
        }
        manageGrid.BuildSpatialGrid();
    }

    void CreateGridLines()
    {
        // nach jedem Takt eine Linie (vertikal)
        for (int x = 4; x < columns; x += 4)
        {
            float xPos = x * cellWidth;
            CreateLine(new Vector3(xPos, 0, gridZPos), new Vector3(xPos, rows * cellHeight, gridZPos));
        }

        // nach jeder Oktave eine Linie (horizontal)
        for (int y = 7; y < rows; y += 7)
        {
            float yPos = y * cellHeight;
            CreateLine(new Vector3(0, yPos, gridZPos), new Vector3(columns * cellWidth, yPos, gridZPos));
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        // Line-Renderer erstellen
        GameObject lineObject = Instantiate(linePrefab);

        // Position des Line-Renderers setzen
        LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        // Breite der Linien verkleinern
        lineRenderer.startWidth = 0.001f;
        lineRenderer.endWidth = 0.001f;
        // gridLine unter gridLines (Parent) einordnen
        lineObject.transform.SetParent(gridLines.transform);
    }

    void CreateMovingLine()
    {
        float yStart = 0f;
        float yEnd = rows * cellHeight;
        float height = yEnd - yStart;

        movingLine = Instantiate(movingLinePrefab);
        movingLine.transform.SetParent(sceneObject.transform);
        movingLine.name = "MovingLine";

        movingLine.transform.localScale = new Vector3(0.001f, height, 0.001f);
        movingLine.transform.position = new Vector3(-0.022f, height / 2f, gridZPos);

        // Boxcollider anpassen
        BoxCollider movingLineBoxCollider = movingLine.GetComponent<BoxCollider>();
        movingLineBoxCollider.isTrigger = true;
        movingLineBoxCollider.center = new Vector3(10f, 0f, 0f); // nach rechts verschoben, damit Toene weniger versetzt abgespielt werden
    }


    void Update()
    {
        if (movingLine != null)
        {
            // Position holen
            Vector3 pos = movingLine.transform.position;

            // Bewegen
            pos.x += movingLineSpeed * Time.deltaTime;

            // Am Ende des Grids wieder an den Anfang zuruecksetzen
            if (pos.x > columns * cellWidth)
            {
                pos.x = 0;
            }

            movingLine.transform.position = pos;
        }
    }

    public float GetCellWidth()
    {
        return cellWidth;
    }

    public float GetCellHeight()
    {
        return cellHeight;
    }

    public void SetMovingLineSpeed(float speed)
    {
        movingLineSpeed = speed;
        BrickTracker.currentMovingLineSpeed = speed;
    }

    public float GetMovingLineSpeed()
    {
        return movingLineSpeed;
    }

    public float GetGridWidth()
    {
        return columns * cellWidth;
    }

    public float GetGridHeight()
    {
        return rows * cellHeight;
    }

    public float GetGridZPos()
    {
        return gridZPos;
    }


}
