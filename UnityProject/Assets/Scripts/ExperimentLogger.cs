using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class ExperimentLogger
{
    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, "experiment_results.csv");

    public static void SaveResult(
        string participantId,
        float completionTime,
        int mistakes,
        int itemsFound)
    {
        bool fileExists = File.Exists(FilePath);

        using (StreamWriter writer = new StreamWriter(
                   FilePath, true, Encoding.UTF8))
        {
            if (!fileExists)
            {
                writer.WriteLine(
                    "ParticipantID,Timestamp,CompletionTime,Mistakes,ItemsFound");
            }

            string timestamp = DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture);

            string time = completionTime.ToString(
                "F2",
                CultureInfo.InvariantCulture);

            writer.WriteLine(
                $"{participantId},{timestamp},{time},{mistakes},{itemsFound}");
        }

        Debug.Log($"Результат сохранён: {FilePath}");
    }
}