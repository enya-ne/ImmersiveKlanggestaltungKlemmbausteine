using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// fuer die Datenstruktur der Mauern zustaendig

[Serializable]
public class Voice
{
    public int duration;
    public int row;
    public int start;
}

[Serializable]
public class Voices
{
    public Voice[] voice_1; // Array von "Voice"-Objekten unter Schluessel voice_1
}

[Serializable]
public class JsonData
{
    public Voices voices;
}
