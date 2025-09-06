using MidiPlayerTK;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

// dafuer zustaendig, die Grid-Koordinaten der Klemmbausteine im Grid zu berechnen,
// wenn diese die movingLine beruehren, und die enstprechenden Noten abzuspielen

public class BrickTracker : MonoBehaviour
{
    private float cellWidth;
    private float cellHeight;

    public int duration;

    public MidiStreamPlayer midi;

    private float lineSpeed;

    public GenerateWall wallScript;
    
    public static float currentMovingLineSpeed = 0f;

    public void SetGridSize(float width, float height)
    {
        cellWidth = width;
        cellHeight = height;
    }

    void Start()
    {
        GenerateGrid grid = FindObjectOfType<GenerateGrid>();

        lineSpeed = grid.GetMovingLineSpeed();

        if (midi != null)
        {

            midi.MPTK_ChannelPresetChange(0, 0);
        }

    }

    void OnTriggerEnter(Collider other)
    {
        int movingLineLayer = LayerMask.NameToLayer("MovingLine"); // Nummer des Layers MovingLine
        if (other.gameObject.layer == movingLineLayer && wallScript.isSnappedToGrid && currentMovingLineSpeed > 0)
        {
            PlayNote();
        }
    }
    
    private void PlayNote()
    {
        // Grid-Koordinaten berechnen
        int gridX = Mathf.FloorToInt(transform.position.x / cellWidth); // faengt bei (0,0) an
        int gridY = Mathf.FloorToInt(transform.position.y / cellHeight);

        int[] whiteKeys = { 12, 14, 16, 17, 19, 21, 23 }; // CDEFGAH

        int octave = gridY / whiteKeys.Length;
        int indexInOctave = gridY % whiteKeys.Length;

        int midiNote = whiteKeys[indexInOctave] + 12 * octave;

        int velocity = 127;

        NoteStart(midiNote, velocity, duration);
    }

    void NoteStart(int note, int velocity, float duration)
    {
        // delay anpassen an Geschwindigkeit des Balkens
        float correctionDistance = 10f; // = Collider-Verschiebung
        float delaySeconds = correctionDistance / lineSpeed;
        long delayMilliseconds = (long)(delaySeconds * 1000f);

        midi.MPTK_PlayEvent(new MPTKEvent()
        {
            Command = MPTKCommand.NoteOn,
            Value = note,
            Channel = 0,
            Velocity = velocity,
            Delay = delayMilliseconds
        });

        StartCoroutine(NoteStop(note, 0, duration));
    }

    IEnumerator NoteStop(int note, int channel, float delay)
    {
        yield return new WaitForSeconds(delay);
        midi.MPTK_PlayEvent(new MPTKEvent()
        {
            Command = MPTKCommand.NoteOff,
            Value = note,
            Channel = channel,
            Velocity = 0
        });
    }
}