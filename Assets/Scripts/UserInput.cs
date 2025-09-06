using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System;

// fuer die Nutzerinteraktion zustaendig

public class UserInput : MonoBehaviour
{
    public enum InteractionMode // verschiedene Interaktionsmodi
    {
        None,
        Click,
        Swing,
        Hold
    }

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    public InputActionAsset inputActions;

    private InputAction button;
    private InputAction controllerPos;

    private InteractionMode currentMode = InteractionMode.None; // none als Startmodus

    private int threshold = 5; // Anzahl der Klicks/ Trommelbewegungen bis MovingLine gestartet wird
    private List<float> timestamps = new List<float>(); // die Zeitpunkte der letzten Klicks/ Trommelbewegungen

    private GenerateGrid generateGrid;

    private float lastY;
    private float currentY;
    private float lastDirection = 0f;
    private float lastExtremaY = 0f;
    private float minYDifference = 0.05f; // Mindestabstand zwischen Wendepunkten

    private TextMeshPro statusText;

    private float currentBPM = 0f;

    // fuer Modus Hold:
    private bool buttonHeld = false;
    private float holdStartX = 0f;
    private float baseSpeed = 0f;
    private float holdBaseBPM = 120f; // Geschwindigkeit, die Balken hat, wenn Button gedrueckt wird
    private float maxXOffset = 0.3f;
    private float speedChangeFactor = 2f;
    private float holdExponent = 1.0f;
    private float lastHoldSpeed = -1f;
    private float lastSpeed = 0f;

    // visuelles Feedback
    public GameObject pointPrefab;
    private GameObject activePoint;

    // fuer fadeout
    private Coroutine fadeCoroutine = null;

    // fuer Lautstaerke der Toene
    private float currentVelocity = 0.5f;
    private float lastHoldVelocity = -1f;

    private float lastClickVelocity = -1f;
    private float clickStartY = 0f;

    private bool swingInitialized = false;
    private float swingBaseY;

    private float holdStartY = 0f;
    private float maxYOffset = 0.3f;

    // fuer Feedback-Punkt im Hold-Modus
    private Coroutine holdBlinkCoroutine = null;

    private void OnEnable()
    {
        var mapbutton = inputActions.FindActionMap("XRI Right Interaction", true);
        button = mapbutton.FindAction("DynamicButton");
        controllerPos = mapbutton.FindAction("Position");

        button.started += OnButtonPressed;
        button.canceled += OnButtonReleased;

    }


    void Start()
    {
        generateGrid = FindObjectOfType<GenerateGrid>();

        CreateStatusText();
        UpdateStatusText();
    }

    private void OnDisable() // Event-Handler entfernen, wenn Skript deaktiviert wird
    {
        button.started -= OnButtonPressed;
        button.canceled -= OnButtonReleased;
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        if (currentMode == InteractionMode.Click)
        {
            ShowPoint();
            // aktuellen Zeitpunkt beim Klick speichern
            float currentTime = Time.time;
            timestamps.Add(currentTime);

            // Lautstaerke berechnen
            if (lastClickVelocity < 0f)
            {
                currentVelocity = 0.5f; // erste Lautstaerke soll mittellaut sein
                float currentY = controllerPos.ReadValue<Vector3>().y;
                clickStartY = currentY;
            }

            // immer nur die letzten 5 Timestamps speichern
            if (timestamps.Count > threshold)
                timestamps.RemoveAt(0);

            if (timestamps.Count == threshold) // sobald 5mal geklickt wurde
                CalculateSpeed();
        }
        else if (currentMode == InteractionMode.Hold)
        {
            buttonHeld = true;

            holdStartY = controllerPos.ReadValue<Vector3>().y; // y-Pos. beim Druecken des Buttons
            holdStartX = controllerPos.ReadValue<Vector3>().x; // x-Pos. beim Druecken des Buttons

            if (lastHoldSpeed <= 0f)
            {
                baseSpeed = (holdBaseBPM / 60f) * generateGrid.GetCellWidth(); // Startgeschwindigkeit 120bpm
                currentBPM = holdBaseBPM;
            }
            else
            {
                baseSpeed = lastHoldSpeed; // Startgeschwindigkeit ist Geschwindigkeit, bei der losgelassen wurde
                currentBPM = baseSpeed * 60f / generateGrid.GetCellWidth();
            }

            float currentY = controllerPos.ReadValue<Vector3>().y;

            if (lastHoldVelocity < 0f) // beim ersten Mal mittlere Lautstaerke als Startlaustaerke benutzen
            {
                currentVelocity = 0.5f;
                holdStartY = currentY;
            }
            else
            {
                currentVelocity = lastHoldVelocity; // mit zuletzt losgelassener Lautstaerke weitermachen
                float deltaVelocity = (lastHoldVelocity - 0.5f) * 2f; // berechnen, wie weit alte Laustaerke von Mitte entfernt
                holdStartY = currentY - (deltaVelocity * maxYOffset); // holdStartY so verschieben, dass bei aktueller Position wieder die Lautstaerke ist
            }

            generateGrid.SetMovingLineSpeed(baseSpeed);

            ShowPoint();

            if (holdBlinkCoroutine != null)
                StopCoroutine(holdBlinkCoroutine); // falls schon Coroutine existiert -> stoppen
            holdBlinkCoroutine = StartCoroutine(BlinkHoldPoint()); // Coroutine fuer Blinken des Feedback-Points starten
        }

        string mode = currentMode.ToString();

        //StudyLogger.AddLogPart(timestamp);
        StudyLogger.AddLogPart("-");
        StudyLogger.AddLogPart("-");
        StudyLogger.AddLogPart("button_down");
        StudyLogger.AddLogPart("-");
        StudyLogger.AddLogPart("-");
        StudyLogger.WriteLog();
    }
    private void OnButtonReleased(InputAction.CallbackContext context)
    {
        if (currentMode == InteractionMode.Hold)
        {
            if (holdBlinkCoroutine != null)
            {
                StopCoroutine(holdBlinkCoroutine); // Coroutine stoppen
                holdBlinkCoroutine = null;
            }
            Destroy(activePoint); // Feedback-Point entfernen

            buttonHeld = false;
            StartFadeOut();

            string mode = currentMode.ToString();

            StudyLogger.AddLogPart("-");
            StudyLogger.AddLogPart(mode);
            StudyLogger.AddLogPart("button_up");
            StudyLogger.AddLogPart("-");
            StudyLogger.AddLogPart("-");
            StudyLogger.WriteLog();
        }
    }
    private void Update()
    {
        if (currentMode == InteractionMode.Swing)
        {
            currentY = controllerPos.ReadValue<Vector3>().y;
            float deltaY = currentY - lastY;
            float direction = Mathf.Sign(deltaY);

            if (direction != 0 && direction != lastDirection && lastDirection != 0f) // nur bei Wendepunkt
            {
                // Bewegung nur dann erkennen, wenn Abstand zu letztem Wendepunkt gross genug (damit minimale Bewegungen nicht faelschlicherweise erkannt werden)
                if (Mathf.Abs(currentY - lastExtremaY) >= minYDifference)
                {
                    lastExtremaY = currentY; // neuen Wendepunkt speichern

                    if (lastDirection < 0 && direction > 0) // nur untere Wendepunkte
                    {
                        float currentTime = Time.time;

                        if (!swingInitialized) // Position von erstem Wendepunkt als Basis (mittlere Lautstaerke) setzen
                        {
                            swingBaseY = currentY;
                            swingInitialized = true;
                        }

                        // Lautstaerke berechnen
                        float deltaYVol = currentY - swingBaseY;

                        deltaYVol = Mathf.Clamp(deltaYVol, -maxYOffset, maxYOffset);
                        float normalizedY = deltaYVol / maxYOffset;
                        currentVelocity = Mathf.Clamp01(0.5f + normalizedY * 0.5f);

                        // Geschwindigkeit berechnen
                        timestamps.Add(currentTime);
                        ShowPoint();

                        if (timestamps.Count > threshold)
                            timestamps.RemoveAt(0);

                        if (timestamps.Count == threshold)
                            CalculateSpeed();

                        // Logging
                        string mode = currentMode.ToString();
                        
                        StudyLogger.AddLogPart("-");
                        StudyLogger.AddLogPart("-");
                        StudyLogger.AddLogPart("swing_detected");
                        StudyLogger.AddLogPart("-");
                        StudyLogger.AddLogPart("-");
                        StudyLogger.WriteLog();

                    }
                }
            }

            lastDirection = direction;
            lastY = currentY;
        }

        if (currentMode == InteractionMode.Hold && buttonHeld)
        {

            float currentX = controllerPos.ReadValue<Vector3>().x;
            float deltaX = currentX - holdStartX;
            // Begrenzung der Deltawerte durch maxYOffset
            deltaX = Mathf.Clamp(deltaX, -maxXOffset, maxXOffset);

            // Faktor, mit dem Geschwindigkeit veraendert wird (Bewegung nach oben -> schneller, nach unten -> langsamer)
            float factor = 1f + (deltaX / maxXOffset) * speedChangeFactor; // lineare Veraenderung der Geschwindigkeit

            float factorPos = Mathf.Max(0.1f, factor); // sicherstellen, dass Geschwindigkeit nicht negativ wird

            float speed = lastSpeed;

            if(factorPos >= 0.1f)
            {
                speed = baseSpeed * factorPos;
            }

            lastSpeed = speed;
            generateGrid.SetMovingLineSpeed(speed);
            lastHoldSpeed = speed;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            currentBPM = speed * 60f / generateGrid.GetCellWidth();

            // Lautstaerke berechnen
            float currentY = controllerPos.ReadValue<Vector3>().y;
            float deltaY = currentY - holdStartY;

            deltaY = Mathf.Clamp(deltaY, -maxYOffset, maxYOffset);
            float normalizedY = deltaY / maxYOffset;
            currentVelocity = Mathf.Clamp01(0.5f + normalizedY * 0.5f);
            lastHoldVelocity = currentVelocity;
        }


        if(currentMode == InteractionMode.Click && generateGrid.GetMovingLineSpeed() > 0) // Lautstaerke immer anpassen, wenn Balken laeuft
        {
            float currentY = controllerPos.ReadValue<Vector3>().y;
            float deltaY = currentY - clickStartY;

            deltaY = Mathf.Clamp(deltaY, -maxYOffset, maxYOffset);
            float normalizedY = deltaY / maxYOffset;
            currentVelocity = Mathf.Clamp01(0.5f + normalizedY * 0.5f);
            lastClickVelocity = currentVelocity;
        }

        audioSource.volume = currentVelocity;

    }

    private void CalculateSpeed()
    {
        // zeitl. Abstaende zwischen den Klicks berechnen und zusammenzaehlen
        float totalTime = 0f;
        for (int i = 1; i < timestamps.Count; i++)
            totalTime += timestamps[i] - timestamps[i - 1];

        // durchschnittl. Abstand zwischen Klickes berechnen
        float averageTime = totalTime / (threshold - 1);
        // Geschwindigkeit berechnen
        float newSpeed = generateGrid.GetCellWidth() / averageTime;

        // aktuelle BPM berechnen
        currentBPM = 60f / averageTime;

        // Geschwindigkeit an GenerateGrid uebergeben
        generateGrid.SetMovingLineSpeed(newSpeed);

        StartFadeOut();
    }

    private void CreateStatusText()
    {
        // Grid-Objekt
        GameObject gridObject = generateGrid.gameObject;

        // Neues GameObject fuer Text erstellen und gridObject als Parent setzen
        GameObject textObject = new GameObject("StatusText");
        textObject.transform.SetParent(gridObject.transform);

        // TextMeshPro-Komponente zu textObject hinzufuegen
        TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
        statusText = textMesh;

        statusText.fontSize = 0.1f;
        statusText.color = Color.white;

        statusText.alignment = TextAlignmentOptions.TopLeft; // Text linksbuendig anordnen
        textMesh.rectTransform.pivot = new Vector2(0, 1);

        // Grid-Dimensionen holen
        float gridWidth = generateGrid.GetGridWidth();
        float gridHeight = generateGrid.GetGridHeight();
        float gridZ = generateGrid.GetGridZPos();

        textObject.transform.localPosition = new Vector3(generateGrid.GetCellWidth(), gridHeight - generateGrid.GetCellHeight() + 0.05f, gridZ);
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;
        if (currentMode == InteractionMode.None) return;

        string modeState = "Modus: " + currentMode.ToString();
        statusText.text = $"{modeState}";
    }

    private void ShowPoint()
    {
        if (pointPrefab == null) return;

        if (activePoint != null)
            Destroy(activePoint);

        float gridHeight = generateGrid.GetGridHeight();
        float gridZ = generateGrid.GetGridZPos();


        Vector3 pos = new Vector3(generateGrid.GetCellWidth() + generateGrid.GetCellWidth() * 3f, gridHeight + generateGrid.GetCellHeight() * 6.25f, gridZ);

        activePoint = Instantiate(pointPrefab, pos, Quaternion.identity);
        if(currentMode != InteractionMode.Hold)
        {
            Destroy(activePoint, 0.15f); // nach 0.15 Sekunden wieder loeschen
        }
        
    }

    private IEnumerator FadeOutSpeed(float duration)
    {
        float startSpeed = generateGrid.GetMovingLineSpeed();
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            float speed = Mathf.Lerp(startSpeed, 0f, t);
            generateGrid.SetMovingLineSpeed(speed);
            currentBPM = speed * 60f / generateGrid.GetCellWidth();
            time += Time.deltaTime;
            yield return null;
        }

        generateGrid.SetMovingLineSpeed(0f);
        currentBPM = 0f;
        fadeCoroutine = null;
    }

    private void StartFadeOut()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutSpeed(3f)); // Fade out der Geschwindigkeit in 3sek
    }

    private IEnumerator BlinkHoldPoint()
    {
        while (buttonHeld) // Point blinkt, wenn Button gedrueckt wird
        {

            if (activePoint != null) // Point aus (blinken)
                activePoint.SetActive(false);

            float beatInterval = 60f / currentBPM; // Zeitintervall fuer 1 Beat

            yield return new WaitForSeconds(beatInterval / 2f); // halbe Beat-Dauer warten

            if (activePoint != null) // Point an (blinken)
                activePoint.SetActive(true);

            yield return new WaitForSeconds(beatInterval / 2f); // halbe Beat-Dauer warten
        }
    }

    public void SetMode(InteractionMode newMode) 
    {
        currentMode = newMode;
        UpdateStatusText();
        lastHoldSpeed = -1f;
        lastHoldVelocity = -1f;
        lastClickVelocity = -1f;
        lastExtremaY = 0f;
    }

    public float GetSpeed()
    {
        return currentBPM;
    }

    public float GetVolume()
    {
        return currentVelocity * 127;
    }
}