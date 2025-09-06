using MidiPlayerTK;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using static UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor;
using DG.Tweening;
using System.Collections.Generic;

// erstellt eine Mauer aus Klemmbausteinen basierend auf dem zugehoerigen JSON-File
// und fuegt benoetigte Komponenten zu Mauer und Klemmbausteinen hinzu;
// außerdem fuer Rotation der Mauern zustaendig

public class GenerateWall : MonoBehaviour
{
    public TextAsset jsonFile;
    public string wallID { get; set; }
    public GameObject legoBrickPrefab;
    public GameObject legoBrickTransparentPrefab;
    public float startHeight; // wird beim Instanziieren aus ManageWalls uebergeben, damit Mauern verschiedene Positionen haben

    public MidiStreamPlayer midi;

    private GameObject wallParent;
    private GameObject legoWall;
    private SnapWall snapScript;

    private float brickSizeX = 0.0228f;
    private float brickSizeY = 0.0066f;

    private int max_row;
    private int max_column;
    private int min_row;
    private int min_column;

    public bool isSnappedToGrid = false;

    // fuer Rotation:
    private InputAction thumbstickAction;
    private float threshold = 0.8f;
    private bool canRotate = true;

    // fuer Klonen:
    public bool isCloneable = false;
    public int cloneCount = 0;

    // for coroutine management:
    private Coroutine delayedInteractionLayerCoroutine;

    public GameObject sceneObject;

    // Object Pooling fuer performance optimization
    private static Dictionary<GameObject, Queue<GameObject>> brickPools = new Dictionary<GameObject, Queue<GameObject>>();
    private List<GameObject> activeBricks = new List<GameObject>();
    private const int INITIAL_POOL_SIZE = 50;

    void Awake()
    {
        thumbstickAction = new InputAction(
            name: "Joystick",
            type: InputActionType.Value,
            binding: "<XRController>{RightHand}/thumbstick"
        );
        thumbstickAction.Enable();
        
        // Initialisieren der brick pools
        InitializeBrickPool(legoBrickPrefab);
        InitializeBrickPool(legoBrickTransparentPrefab);
    }

    private void InitializeBrickPool(GameObject prefab)
    {
        if (prefab == null) return;
        
        if (!brickPools.ContainsKey(prefab))
        {
            brickPools[prefab] = new Queue<GameObject>();
            
            // Bricks vorinstanziieren fuer bessere Performance
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                GameObject pooledBrick = Instantiate(prefab);
                pooledBrick.SetActive(false);
                brickPools[prefab].Enqueue(pooledBrick);
            }
        }
    }

    private GameObject GetBrickFromPool(GameObject prefab)
    {
        if (brickPools[prefab].Count > 0)
        {
            GameObject brick = brickPools[prefab].Dequeue();
            brick.SetActive(true);
            return brick;
        }
        else
        {
            // Pool leer -> neues Prefab instanziieren
            return Instantiate(prefab);
        }
    }

    private void ReturnBrickToPool(GameObject brick, GameObject prefab)
    {
        if (brick != null)
        {
            brick.SetActive(false);
            brick.transform.SetParent(null);
            brickPools[prefab].Enqueue(brick);
        }
    }

    public void ClearWall()
    {
        // alle bricks in entsprechende Pools zurueck sortieren
        foreach (GameObject brick in activeBricks)
        {
            if (brick != null)
            {
                bool isTransparent = brick.name.Contains("Transparent") || brick.GetComponent<Renderer>()?.material?.name.Contains("Transparent") == true;
                GameObject originalPrefab = isTransparent ? legoBrickTransparentPrefab : legoBrickPrefab;
                ReturnBrickToPool(brick, originalPrefab);
            }
        }
        activeBricks.Clear();
    }

    void OnDestroy()
    {
        ClearWall();
    }

    public void Generate()
    {
        if (jsonFile == null) return;

        ClearWall();

        JsonData jsonData = JsonUtility.FromJson<JsonData>(jsonFile.text);

        // Groesse der Mauer (Ausschnitt mit Toenen) bestimmen
        max_row = 0;
        max_column = 0;

        min_row = int.MaxValue;
        min_column = int.MaxValue;

        foreach (var voice in jsonData.voices.voice_1)
        {
            if (voice.row > max_row) max_row = voice.row;
            if (voice.start + voice.duration > max_column) max_column = voice.start + voice.duration - 1;

            if (voice.row < min_row) min_row = voice.row - 1; // -1, weil sonst unterste Zeile und Spalte nicht betrachtet werden
            if (voice.start < min_column) min_column = voice.start - 1;
        }

        int height = max_row - min_row;
        int width = max_column - min_column;

        float totalWidth = width * brickSizeX;
        float totalHeight = height * brickSizeY;

        // Parent fuer Rotation erstellen
        wallParent = new GameObject("WallParent");
        wallParent.transform.SetParent(sceneObject.transform);
        wallParent.transform.position = new Vector3(-0.2f - totalWidth / 2, startHeight + totalHeight / 2, 0.5f); // Mauern hoeren alle bei 0.2f rechts auf und sind uebereinander angeordnet (startHeight wird aus ManageWalls.cs uebergeben)

        // LegoWall (Parent fuer Legosteine) erstellen
        legoWall = new GameObject("LegoWall");
        legoWall.transform.parent = wallParent.transform;
        legoWall.transform.localPosition = new Vector3(-totalWidth / 2, -totalHeight / 2, 0);

        // SnapWall-Skript hinzufuegen (einmal pro Mauer)
        snapScript = wallParent.AddComponent<SnapWall>();
        snapScript.enabled = false;

        // Komponenten zu WallParent hinzufuegen
        BoxCollider boxCollider = wallParent.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(totalWidth, totalHeight, 0.0114f);
        boxCollider.center = Vector3.zero;

        Rigidbody rigidbody = wallParent.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractableLegoWall = wallParent.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grabInteractableLegoWall.useDynamicAttach = true;
        grabInteractableLegoWall.smoothPosition = true;
        grabInteractableLegoWall.smoothRotation = false;
        grabInteractableLegoWall.trackRotation = false;
        grabInteractableLegoWall.selectEntered.AddListener(OnGrabEntered);
        grabInteractableLegoWall.selectExited.AddListener(OnGrabExited);
        
        // Attach-Point fuer Socket-Interaktion mit Grid erstellen
        GameObject attachPoint = new GameObject("AttachPoint");
        attachPoint.transform.parent = wallParent.transform;
        attachPoint.transform.localPosition = new Vector3(-totalWidth / 2 + brickSizeX / 2, -totalHeight / 2 + brickSizeY / 2, 0); // Attach-Point ist Mitte des Legosteins unten links
        grabInteractableLegoWall.attachTransform = attachPoint.transform;

        // Legosteine erstellen
        for (int i = min_row; i < max_row; i++)
        {
            for (int j = min_column; j < max_column; j++)
            {
                Vector3 position = new Vector3((j - min_column) * brickSizeX + brickSizeX / 2, (i - min_row) * brickSizeY + brickSizeY / 2, 0);
                GameObject brick = null;
                bool isVoice = false;
                bool isFirst = false;
                int duration = 0;

                foreach (var voice in jsonData.voices.voice_1)
                {
                    int start = voice.start - 1;
                    int end = start + voice.duration;
                    int row = voice.row - 1;

                    if (j >= start && j < end && i == row)
                    {
                        isVoice = true;
                        if (j == start)
                        {
                            isFirst = true;
                            duration = voice.duration;
                        }
                        break;
                    }
                }

                // Wenn an dieser Position eine Note ist -> weißer Legostein, sonst transparenter
                GameObject brickPrefab = isVoice ? legoBrickPrefab : legoBrickTransparentPrefab;

                Quaternion rotation = Quaternion.Euler(0, 0, 0);

                // Legostein erstellen mit Parent LegoWall
                brick = GetBrickFromPool(brickPrefab);
                brick.transform.parent = legoWall.transform;
                brick.transform.localPosition = position;
                brick.transform.localRotation = rotation;
                activeBricks.Add(brick); // zu activebricks-Liste hinzufuegen

                if (isFirst) // damit Folge an Klemmbausteinen nur ein BrickTracker-Skript bekommt
                {
                    // Leeres GameObject fuer den Collider und das BrickTracker-Skript
                    GameObject brickCollider = new GameObject("BrickCollider");
                    brickCollider.transform.parent = brick.transform.parent; // LegoWall als Parent setzen

                    brickCollider.transform.localPosition = position;
                    brickCollider.transform.localRotation = Quaternion.identity;

                    // Collider hinzufuegen
                    BoxCollider boxColliderBrick = brickCollider.AddComponent<BoxCollider>();
                    boxColliderBrick.isTrigger = true;

                    float totalLength = duration * brickSizeX;
                    float brickSizeZ = 0.0114f;

                    // Collider auf gesamte Abfolge an hintereinanderkommenden Steinen setzen
                    boxColliderBrick.size = new Vector3(totalLength, brickSizeY, brickSizeZ);
                    boxColliderBrick.center = new Vector3((totalLength - brickSizeX) / 2f, 0f, 0f);

                    // BrickTracker-Skript an Gameobject haengen
                    var tracker = brickCollider.AddComponent<BrickTracker>();
                    tracker.SetGridSize(brickSizeX, brickSizeY);
                    tracker.duration = duration;
                    tracker.midi = midi;
                    tracker.wallScript = this; // GenerateWall-Skript uebergeben
                }
            }
        }
    }
    
    public void OnGrabEntered(SelectEnterEventArgs args)
    {
       isSnappedToGrid = false;

        if (args.interactorObject is not XRSocketInteractor) // nur bei Greifen mit Hand, nicht bei Interaktion mit Socket
        {
           if (isCloneable)
            {
                isCloneable = false; // Mauer, die man herauszieht, ist nicht mehr klonbar
                if (cloneCount <= 4)
                {
                    // geklonte Mauer erzeugen
                    GameObject clone = Instantiate(gameObject);
                    GenerateWall cloneScript = clone.GetComponent<GenerateWall>();

                    // Eigenschaften der Mauer auf geklonte Mauer uebertragen
                    cloneScript.jsonFile = this.jsonFile;
                    cloneScript.midi = this.midi;
                    cloneScript.startHeight = this.startHeight;
                    cloneScript.isCloneable = true; // Geklonte Mauer ist erneut klonbar
                    cloneScript.cloneCount = cloneCount + 1;

                // geklonte Mauer generieren
                cloneScript.Generate();
                }
            }

            snapScript.enabled = true;
            
            if (args.interactableObject is XRGrabInteractable grabInteractable)
            {
                if (delayedInteractionLayerCoroutine != null)
                {
                    StopCoroutine(delayedInteractionLayerCoroutine);
                    delayedInteractionLayerCoroutine = null;
                }

                
                grabInteractable.interactionLayers = InteractionLayerMask.GetMask("Default");
            } 
        }
       
    }

    public void OnGrabExited(SelectExitEventArgs args)
    {
        if (args.interactableObject.transform.position.z < 0.51f && args.interactableObject.transform.position.z > -0.49f)
        {
            isSnappedToGrid = true;
        }


        if (args.interactorObject is not XRSocketInteractor)
        {
            if (args.interactableObject is XRGrabInteractable grabInteractable)
            {
                snapScript.enabled = false;
                //  Referenz speichern fuer Ausfuehrung mit Delay
                delayedInteractionLayerCoroutine = StartCoroutine(SetInteractionLayerAfterDelay(grabInteractable, 1.0f));
            }
        }
    }

    void Update()
    {
        if (!snapScript.enabled) return;

        Vector2 input = thumbstickAction.ReadValue<Vector2>();
        float magnitude = input.magnitude;

        if (magnitude > threshold && canRotate)
        {
            if (input.y > threshold)
            {
                canRotate = false;
                wallParent.transform.DORotate(new Vector3(180f, 0f, 0f), 0.5f, DG.Tweening.RotateMode.LocalAxisAdd).OnComplete(() => canRotate = true);
            }
            else if (input.y < -threshold)
            {
                canRotate = false;
                wallParent.transform.DORotate(new Vector3(-180f, 0f, 0f), 0.5f, DG.Tweening.RotateMode.LocalAxisAdd).OnComplete(() => canRotate = true);
            }
            else if (input.x < -threshold)
            {
                canRotate = false;
                wallParent.transform.DORotate(new Vector3(0f, -180f, 0f), 0.5f, DG.Tweening.RotateMode.LocalAxisAdd).OnComplete(() => canRotate = true);
            }
            else if (input.x > threshold)
            {
                canRotate = false;
                wallParent.transform.DORotate(new Vector3(0f, 180f, 0f), 0.5f, DG.Tweening.RotateMode.LocalAxisAdd).OnComplete(() => canRotate = true);
            }
        }
    }


    private System.Collections.IEnumerator SetInteractionLayerAfterDelay(XRGrabInteractable grabInteractable, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (grabInteractable != null)
        {
            grabInteractable.interactionLayers = InteractionLayerMask.GetMask("NotGrabbed");
        }

        delayedInteractionLayerCoroutine = null;
    }

    public float GetWallHeight()
    {
        return (max_row - min_row) * brickSizeY;
    }
}