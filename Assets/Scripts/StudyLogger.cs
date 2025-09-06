using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// fuer Aufzeichnen der Daten waehrend der Studie zustaendig

public static class StudyLogger
{
    private static string logPath;
    private static List<string> logParts = new List<string>();

    static StudyLogger()
    {
        // Log-Dateipfad setzen
        string fileName = "Logger_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        logPath = Path.Combine(Application.persistentDataPath, fileName);

        // Header schreiben
        File.WriteAllText(logPath, "Zeit;Schritt;Modus;Event;Geschwindigkeit;Lautstärke\n");
    }

    /// <summary>
    /// Add a part to the log.
    /// Call this multiple times to accumulate the log data.
    /// </summary>
    public static void AddLogPart(string part)
    {
        logParts.Add(part);
    }

    /// <summary>
    /// When you're ready, call this method to log all accumulated parts.
    /// </summary>
    public static void WriteLog()
    {
        if (logParts.Count == 0) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string logLine = timestamp;

        // Join all accumulated parts with semicolon
        logLine += ";" + string.Join(";", logParts);

        Debug.Log("[SimpleLogger] " + logLine);
        AppendToFile(logLine);

        // Clear the list after logging
        logParts.Clear();
    }

    public static void LogSlices()
    {

        File.AppendAllText(logPath, ";;;;;Slices\n");

    }
    private static void AppendToFile(string line)
    {
        try
        {
            File.AppendAllText(logPath, line + "\n");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SimpleLogger] File write error: {e.Message}");
        }
    }

}
