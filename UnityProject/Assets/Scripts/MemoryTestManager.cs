using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small, standalone cognitive-task prototype: remember a sequence of digits,
/// then enter it. Saves anonymized trial metrics to a CSV file.
/// Attach to an empty GameObject in a scene with a Canvas.
/// </summary>
public class MemoryTestManager : MonoBehaviour
{
    [Header("UI references (create with UI > Text - TextMeshPro / Button / Input Field)")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button startButton;

    [Header("Task settings")]
    [SerializeField, Min(3)] private int sequenceLength = 5;
    [SerializeField, Min(1)] private float displaySeconds = 4f;

    private string targetSequence;
    private float taskStartTime;
    private bool acceptingAnswer;
    private readonly List<string> csvRows = new List<string>();

    private void Start()
    {
        if (instructionText == null || answerInput == null || submitButton == null || startButton == null)
        {
            Debug.LogError("MemoryTestManager: assign all UI references in the Inspector.");
            enabled = false;
            return;
        }

        submitButton.onClick.AddListener(SubmitAnswer);
        startButton.onClick.AddListener(StartTask);
        answerInput.characterLimit = sequenceLength;
        answerInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        answerInput.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(false);
        instructionText.text = "Тест памяти\nНажми «Начать», запомни цифры и введи их после исчезновения.";
    }

    public void StartTask()
    {
        acceptingAnswer = false;
        targetSequence = MakeSequence(sequenceLength);
        answerInput.text = "";
        answerInput.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(false);
        instructionText.text = "Запомни последовательность:\n\n" + targetSequence;
        CancelInvoke(nameof(HideSequence));
        Invoke(nameof(HideSequence), displaySeconds);
    }

    private string MakeSequence(int length)
    {
        var random = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
            random.Append(UnityEngine.Random.Range(0, 10));
        return random.ToString();
    }

    private void HideSequence()
    {
        instructionText.text = "Введи запомненную последовательность цифр:";
        answerInput.gameObject.SetActive(true);
        submitButton.gameObject.SetActive(true);
        answerInput.ActivateInputField();
        taskStartTime = Time.realtimeSinceStartup;
        acceptingAnswer = true;
    }

    private void SubmitAnswer()
    {
        if (!acceptingAnswer) return;
        acceptingAnswer = false;

        string answer = answerInput.text.Trim();
        float responseTime = Time.realtimeSinceStartup - taskStartTime;
        bool correct = answer == targetSequence;
        int errors = CountPositionErrors(targetSequence, answer);
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        csvRows.Add($"{timestamp},{sequenceLength},{displaySeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Csv(answer)},{correct.ToString().ToLowerInvariant()},{errors},{responseTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
        SaveCsv();

        instructionText.text = (correct ? "Верно!" : "Последовательность не совпала.") +
            $"\nОшибки: {errors}\nВремя ответа: {responseTime:F2} сек.\n\nРезультат сохранён в CSV.";
        answerInput.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
        startButton.GetComponentInChildren<TMP_Text>().text = "Повторить";
    }

    private int CountPositionErrors(string target, string answer)
    {
        int errors = Mathf.Abs(target.Length - answer.Length);
        int commonLength = Mathf.Min(target.Length, answer.Length);
        for (int i = 0; i < commonLength; i++)
            if (target[i] != answer[i]) errors++;
        return errors;
    }

    private void SaveCsv()
    {
        string path = Path.Combine(Application.persistentDataPath, "memory_test_results.csv");
        bool needsHeader = !File.Exists(path);
        using (var writer = new StreamWriter(path, true, new UTF8Encoding(true)))
        {
            if (needsHeader)
                writer.WriteLine("timestamp_utc,sequence_length,display_seconds,answer,correct,position_errors,response_time_seconds");
            foreach (string row in csvRows) writer.WriteLine(row);
            csvRows.Clear();
        }
        Debug.Log("Memory test CSV saved to: " + path);
    }

    private string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
