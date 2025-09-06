using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using System.Linq;

// fuer Ablauf der Studie zustaendig
public class StudyFlow : MonoBehaviour
{

    private List<string> modes = new List<string> { "Click", "Swing", "Hold" };
    private List<int> modesIndices = new List<int> { 0, 1, 2 };
    class StudyEvent
    {
        public int step;
        public string mode;
        public string info;
        public VideoClip video;
        public AudioClip audio;
    }

    private List<StudyEvent> studyFlow = new List<StudyEvent>();
    private int currentStudyEvent = 0;

    [SerializeField] GameObject videoObject;
    [SerializeField] GameObject sceneObject;
    [SerializeField] GameObject nextButton;

    [SerializeField]
    private TextMeshProUGUI studyText;

    [SerializeField] 
    private VideoPlayer videoPlayer;

    [SerializeField] private VideoClip introVideo;
    [SerializeField] private VideoClip modiVideo;
    [SerializeField] private VideoClip clickVideo;
    [SerializeField] private VideoClip swingVideo;
    [SerializeField] private VideoClip holdVideo;

    private List<VideoClip> modeVideos;

    private UserInput userInput;

    [SerializeField] 
    private AudioSource audioSource;

    [SerializeField] private AudioClip step2audio;
    [SerializeField] private AudioClip step5audio;
    [SerializeField] private AudioClip step6audio;
    [SerializeField] private AudioClip step7audio;
    [SerializeField] private AudioClip step8audio;
    [SerializeField] private AudioClip step9audio;

    private void OnEnable()
    {
        modeVideos = new List<VideoClip> { clickVideo, swingVideo, holdVideo }; // Videos zu verschiedenen Modi der Liste hinzufuegen (in gleicher Reihenfolge wie Modi in modes-Liste, damit Zuweisung ueber Index funktioniert)
        modesIndices = modesIndices.OrderBy(x => UnityEngine.Random.value).ToList(); // Reihenfolge der Modi randomisieren

        // Schritt 0-3: einmalig

        // Schritt 0: Willkommen
        studyFlow.Add(new StudyEvent()
        {
            step = 0,
            mode = null,
            info = "Herzlich Willkommen zur Studie Lego-VR! Bitte klicken Sie auf 'Next', um zum Einführungs-Video zu gelangen.",
            video = null,
            audio = null
        });

        // Schritt 1: Einfuehrungsvideo
        studyFlow.Add(new StudyEvent() {
            step = 1,
            mode = null, 
            info = "Bitte sehen Sie sich das Video an.", 
            video = introVideo,
            audio = null
        });
        // Schritt 2: Mauern anordnen
        studyFlow.Add(new StudyEvent() {
            step = 2,
            mode = null, 
            info = "Bitte platzieren Sie nun beliebig viele Lego-Mauern am Grid. Es gibt dabei kein richtig oder falsch. Klicken Sie auf 'Next', wenn Sie mit Ihrer Anordnung zufrieden sind.", 
            video = null,
            audio = step2audio
        });
        // Schritt 3: generelles Video zu verschiedenen Modi
        studyFlow.Add(new StudyEvent() {
            step = 3,
            mode = null, 
            info = "Bitte sehen Sie sich das Video an.", 
            video = modiVideo,
            audio = null
        });


        // Schritt 4-9: fuer jeden Modus wiederholen

        foreach (int i in modesIndices)
        {
            // Schritt 4: Video zu Modus
            studyFlow.Add(new StudyEvent()
            {
                step = 4,
                mode = modes[i],
                info = "Bitte sehen Sie sich das Video an.",
                video = modeVideos[i],
                audio = null
            });
            // Schritt 5: ausprobieren
            studyFlow.Add(new StudyEvent()
            {
                step = 5,
                mode = modes[i],
                info = "Probieren Sie nun den Modus aus, damit Sie ein Gefühl für die Steuerung bekommen. Wenn Sie der Meinung sind, dass Sie diese verstanden haben und damit umgehen können, klicken Sie auf 'Next'.",
                video = null,
                audio = step5audio
            });
            // Schritt 6: Geschwindigkeit halten
            studyFlow.Add(new StudyEvent()
            {
                step = 6,
                mode = modes[i],
                info = "Versuchen Sie nun bitte, die Geschwindigkeit 60BPM für 20 Sekunden zu halten. Wenn Sie der Meinung sind, dies erreicht zu haben, klicken Sie auf 'Next'.",
                video = null,
                audio = step6audio
            });
            // Schritt 7: dynamisch gestalten
            studyFlow.Add(new StudyEvent()
            {
                step = 7,
                mode = modes[i],
                info = "Nun gilt es, Dynamik in die Klanggestaltung zu bringen! Gestalten Sie den Klang nach Ihrem Empfinden und nutzen Sie dabei sowohl Geschwindigkeit als auch Lautstärke, um Dynamik zu erzeugen. Klicken Sie auf 'Next', wenn Sie fertig sind.",
                video = null,
                audio = step7audio
            });
            // Schritt 8: VR-Brille abnehmen
            studyFlow.Add(new StudyEvent()
            {
                step = 8,
                mode = modes[i],
                info = "Bitte ziehen Sie nun die VR-Brille ab und wenden Sie sich an die Versuchsleitung.",
                video = null,
                audio = step8audio
            });
            // Schritt 9: Mauern neu anordnen
            studyFlow.Add(new StudyEvent()
            {
                step = 9,
                mode = modes[i],
                info = "Bitte ordnen Sie nun die Mauern neu am Grid an, um eine neue Tonabfolge zu erzeugen. Klicken Sie auf 'Next', wenn Sie mit Ihrer Anordnung zufrieden sind.",
                video = null,
                audio = step9audio
            });

        }

    }
    void Start()
    {

        userInput = FindObjectOfType<UserInput>();
        ShowCurrentStep();
    }

    void Update()
    {
        var currentEvent = studyFlow[currentStudyEvent];

        if (currentEvent.step == 6 || currentEvent.step == 7)
        {
            string step = currentEvent.step.ToString();
            string mode = currentEvent.mode ?? "none";
            float speed = userInput.GetSpeed();
            float volume = userInput.GetVolume();

            StudyLogger.AddLogPart(step); // Schritt
            StudyLogger.AddLogPart(mode); // Modus
            StudyLogger.AddLogPart("-"); // Event
            StudyLogger.AddLogPart(speed.ToString("F2")); // Geschwindigkeit
            StudyLogger.AddLogPart(volume.ToString("F2")); // Lautstaerke
            StudyLogger.WriteLog();
        }
    }

    private void ShowCurrentStep()
    {
        if (currentStudyEvent < 0 || currentStudyEvent >= studyFlow.Count)
        {
            return;
        }

        var currentEvent = studyFlow[currentStudyEvent];

        studyText.text = currentEvent.info;

        if (currentEvent.video != null)
        {
            videoObject.SetActive(true);
            sceneObject.SetActive(false);

            videoPlayer.clip = currentEvent.video;
        }
        else
        {
            videoObject.SetActive(false);
            sceneObject.SetActive(true);
        }

        if (currentEvent.audio != null)
        {
            audioSource.clip = currentEvent.audio;
            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
        }

        // InteractionMode setzen
        if (currentEvent.mode != null)
        {
            var interactionMode = (UserInput.InteractionMode)System.Enum.Parse(typeof(UserInput.InteractionMode), currentEvent.mode); // String in InteractionMode umwandeln
            userInput.SetMode(interactionMode);
        }

        if (currentEvent.step == 1)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(79f)); // erst nach 79sec freischalten (Video 81sec lang)
        }
        else if (currentEvent.step == 2)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(15f)); // erst nach 15sec freischalten (Audio 10sec lang)
        }
        else if (currentEvent.step == 3)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(16f)); // erst nach 16sec freischalten (Video 18sec lang)
        }
        else if (currentEvent.step == 4 && currentEvent.mode == "Click")
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(51f)); // erst nach 51sec freischalten (Video 53sec lang)
        }
        else if (currentEvent.step == 4 && currentEvent.mode == "Swing")
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(60f)); // erst nach 60sec freischalten (Video 62sec lang)
        }
        else if (currentEvent.step == 4 && currentEvent.mode == "Hold")
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(64f)); // erst nach 64sec freischalten (Video 66sec lang)
        }
        else if (currentEvent.step == 5)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(15f)); // erst nach 15sec freischalten (Audio 10sec lang)
        }
        else if (currentEvent.step == 6)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(14f)); // erst nach 14sec freischalten (Audio 9sec lang)
        }
        else if (currentEvent.step == 7)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(19f)); // erst nach 19sec freischalten (Audio 14sec lang)
        }
        else if (currentEvent.step == 8)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(30f)); // erst nach 30sec freischalten (-> Brille muss abgesetzt werden)
        }
        else if (currentEvent.step == 9)
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            StartCoroutine(EnableNextAfterDelay(14f)); // erst nach 14sec freischalten (Audio 9sec lang)
        }
        else
        {
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = true;
        }

    }

    private IEnumerator EnableNextAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        nextButton.GetComponent<UnityEngine.UI.Button>().interactable = true;
    }



    public void Next()
    {
        currentStudyEvent++;
        ShowCurrentStep();
    }
}
