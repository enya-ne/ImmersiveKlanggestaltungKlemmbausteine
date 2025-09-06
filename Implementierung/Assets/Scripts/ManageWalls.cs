using UnityEngine;
using System.IO;
using MidiPlayerTK;

// erstellt fuer jedes JSON-File eine Mauer mit GenerateWall-Skript

public class ManageWalls : MonoBehaviour
{
    public GameObject wallGeneratorPrefab;
    public string jsonFilesDirectory = "JsonFiles"; // relativer Pfad innerhalb persistent data directory
    public MidiStreamPlayer midiStreamPlayer;
    [SerializeField] GameObject sceneObject; // ermoeglicht, bei Studie alle Objekte, die zur Szene gehoeren, waehrend der Videos auszublenden

    void Start()
    {
        // kompletten Pfad zusammenstellen -> Android compatibility
        string fullJsonFilesDirectory = Path.Combine(Application.persistentDataPath, jsonFilesDirectory);
        // directory erstellen, falls es nicht existiert
        if (!Directory.Exists(fullJsonFilesDirectory))
        {
            Directory.CreateDirectory(fullJsonFilesDirectory);
        }

        // Array der Pfade der Json-Files
        string[] jsonFiles = Directory.GetFiles(fullJsonFilesDirectory, "*.json");

        // alle Mauern aus den JSON-Dateien generieren
        GenerateWalls(jsonFiles);
    }

    void GenerateWalls(string[] jsonFiles)
    {
        float currentYPosition = 0f;

        foreach (var jsonFile in jsonFiles)
        {
            GameObject newWall = Instantiate(wallGeneratorPrefab, new Vector3(0, currentYPosition, 0), Quaternion.identity);
            GenerateWall wallGenerator = newWall.GetComponent<GenerateWall>();
            wallGenerator.midi = midiStreamPlayer;
            wallGenerator.wallID = jsonFile;
            wallGenerator.sceneObject = sceneObject;
             
            TextAsset jsonText = new TextAsset(File.ReadAllText(jsonFile));
            wallGenerator.jsonFile = jsonText;

            wallGenerator.startHeight = currentYPosition; // startHeight uebergeben

            wallGenerator.Generate();
            wallGenerator.isCloneable = true;
            float wallHeight = wallGenerator.GetWallHeight();
            currentYPosition += wallHeight + 0.05f;
        }
    }
}
