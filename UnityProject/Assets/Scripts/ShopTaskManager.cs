using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopTaskManager : MonoBehaviour
{
    [SerializeField] private string participantId = "P001";
    public GameObject[] foods;
    public GameObject groceryList;
    private readonly string[] requiredItems =
    {
        "Молоко", "Хлеб", "красное яблоко", "приправа к рыбе"
    };

    private readonly HashSet<string> foundItems = new HashSet<string>();

    [Header("Shopping List UI")]
    [SerializeField] private TMP_Text milkText;
    [SerializeField] private TMP_Text breadText;
    [SerializeField] private TMP_Text appleText;
    [SerializeField] private TMP_Text condFishText;
    [SerializeField] private TMP_Text statusText;

    private float startTime;
    private int mistakes;
    private int selectionCount;
    private bool taskFinished;
    private bool taskActive = false;

    private void Start()
    {
        // Задание запускается через StudyPhaseManager.
    }

    // private void Start()
    // {
    //     startTime = Time.time;
    //     UpdateUI();

    //     Debug.Log("Задание началось: найди молоко, хлеб и яблоко.");
    // }

    public void BeginTask()
    {
        foundItems.Clear();
        mistakes = 0;
        selectionCount = 0;
        taskFinished = false;
        taskActive = true;
        startTime = Time.time;

        UpdateUI();

        Debug.Log("=== НАЧАЛО ПОИСКА ===");
        Debug.Log("Найди товары: Молоко, Хлеб, Яблоко.");
    }

    public void RegisterSelection(string itemName)
    {
        if (!taskActive)
            return;

        selectionCount++;

        if (System.Array.IndexOf(requiredItems, itemName) < 0)
        {
            mistakes++;

            Debug.Log($"Ненужный товар: {itemName}. Ошибок: {mistakes}");

            if (statusText != null)
                statusText.text = $"Это не тот товар! Ошибок: {mistakes}";

            return;
        }

        if (!foundItems.Add(itemName))
        {
            if (statusText != null)
                statusText.text = $"{itemName} уже найден!";

            return;
        }

        Debug.Log($"Найден товар: {itemName}");

        UpdateUI();

        if (foundItems.Count == requiredItems.Length)
            FinishTask();
    }

    private void UpdateUI()
    {
        if (milkText != null)
            milkText.text = MakeLabel("Молоко");

        if (breadText != null)
            breadText.text = MakeLabel("Хлеб");

        if (appleText != null)
            appleText.text = MakeLabel("Яблоко");

        if (condFishText != null)
            condFishText.text = MakeLabel("Приправа к рыбе");

        if (statusText != null)
            statusText.text = $"Найдено: {foundItems.Count}/4";
    }

    private string MakeLabel(string itemName)
    {
        return foundItems.Contains(itemName)
            ? $"✓ {itemName}"
            : $"○ {itemName}";
    }

    private void FinishTask()
    {
        taskFinished = true;
        taskActive = false;

        FindFirstObjectByType<StudyPhaseManager>()?.FinishStudy();

        float completionTime = Time.time - startTime;

        string result =
            $"Задание завершено!\n" +
            $"Время: {completionTime:F1} сек.\n" +
            $"Ошибки: {mistakes}";

        if (statusText != null)
            statusText.text = result;

        Debug.Log(result);

        ExperimentLogger.SaveResult(
            participantId,
            completionTime,
            mistakes,
            foundItems.Count
        );

        if (ExperimentReportManager.Instance != null)
        {
            ExperimentReportManager.Instance.SetVRResult(
                foundItems.Count,
                requiredItems.Length,
                selectionCount,
                completionTime
            );
        }


    }
}