using System;
using System.IO;
using System.Text;
using UnityEngine;

public class ExperimentReportManager : MonoBehaviour
{
    public static ExperimentReportManager Instance { get; private set; }

    private string participantId = "sub-001";

    private string eegClass = "Не выполнено";
    private float cnProbability;
    private float adProbability;
    private int eegFeatureCount;
    private int eegChannelCount;
    private float eegDuration;

    private int foundItems;
    private int totalItems;
    private int selectionCount;
    private float vrDuration;

    private bool eegCompleted;
    private bool vrCompleted;

    private float experimentStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        experimentStartTime = Time.realtimeSinceStartup;
    }

    public void SetParticipantId(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
            participantId = id.Trim();
    }

    public void SetEEGResult(
        string className,
        float cn,
        float ad,
        int featureCount,
        int channelCount,
        float duration)
    {
        eegClass = className;
        cnProbability = cn;
        adProbability = ad;
        eegFeatureCount = featureCount;
        eegChannelCount = channelCount;
        eegDuration = duration;

        eegCompleted = true;

        Debug.Log("Результат EEG сохранён в памяти отчёта.");
    }

    public void SetVRResult(
        int found,
        int total,
        int selections,
        float duration)
    {
        foundItems = found;
        totalItems = total;
        selectionCount = selections;
        vrDuration = duration;

        vrCompleted = true;

        Debug.Log("Результат VR сохранён в памяти отчёта.");
    }


    public void CompleteExperiment()
    {
        string path = SaveReport();
        Debug.Log("Итоговый отчёт эксперимента: " + path);
    }

    public string SaveReport()
    {
        string folder = Path.Combine(
            Application.persistentDataPath,
            "ExperimentReports"
        );

        Directory.CreateDirectory(folder);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string filePath = Path.Combine(
            folder,
            $"Experiment_{participantId}_{timestamp}.csv"
        );

        float totalDuration =
            Time.realtimeSinceStartup - experimentStartTime;

        StringBuilder csv = new StringBuilder();

        csv.AppendLine(
            "ParticipantID,Timestamp,EEGCompleted,EEGClass," +
            "CNProbability,ADProbability,FeatureCount,ChannelCount," +
            "EEGDurationSeconds,VRCompleted,FoundItems,TotalItems," +
            "SelectionCount,VRDurationSeconds,TotalDurationSeconds"
        );

        csv.AppendLine(string.Join(",",
            Escape(participantId),
            Escape(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            eegCompleted,
            Escape(eegClass),
            cnProbability.ToString(
                "F4", System.Globalization.CultureInfo.InvariantCulture),
            adProbability.ToString(
                "F4", System.Globalization.CultureInfo.InvariantCulture),
            eegFeatureCount,
            eegChannelCount,
            eegDuration.ToString(
                "F2", System.Globalization.CultureInfo.InvariantCulture),
            vrCompleted,
            foundItems,
            totalItems,
            selectionCount,
            vrDuration.ToString(
                "F2", System.Globalization.CultureInfo.InvariantCulture),
            totalDuration.ToString(
                "F2", System.Globalization.CultureInfo.InvariantCulture)
        ));

        File.WriteAllText(
            filePath,
            csv.ToString(),
            new UTF8Encoding(true)
        );

        Debug.Log($"Отчёт эксперимента сохранён: {filePath}");

        return filePath;
    }

    private string Escape(string value)
    {
        if (value == null)
            return "";

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}