using TMPro;
using UnityEngine;

public class StudyPhaseManager : MonoBehaviour
{
    public enum StudyPhase
    {
        Explore,
        Search,
        Finished
    }

    [Header("Settings")]
    [SerializeField] private float explorationDuration = 60f;

    [Header("UI")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject shoppingListCanvas;
    [SerializeField] private GameObject EEGCanvas;

    [Header("References")]
    [SerializeField] private ShopTaskManager shopTaskManager;

    public StudyPhase CurrentPhase { get; private set; }

    private float phaseStartTime;
    private bool phaseStarted;

    private void Start()
    {
        StartExploration();
    }

    private void Update()
    {
        if (!phaseStarted)
            return;

        if (CurrentPhase == StudyPhase.Explore)
        {
            float elapsed = Time.time - phaseStartTime;
            float remaining = Mathf.Max(0f, explorationDuration - elapsed);

            if (timerText != null)
                timerText.text = $"Изучение магазина: {remaining:F0} сек.";

            if (remaining <= 0f)
                StartSearch();
        }
    }

    private void StartExploration()
    {
        CurrentPhase = StudyPhase.Explore;
        phaseStartTime = Time.time;
        phaseStarted = true;

        if (phaseText != null)
            phaseText.text = "Осмотри магазин и запомни расположение товаров";

        if (shoppingListCanvas != null)
            shoppingListCanvas.SetActive(false);

        if (EEGCanvas != null)
            EEGCanvas.SetActive(false);

        // if (shopTaskManager != null)
        //     shopTaskManager.gameObject.SetActive(false);

        Debug.Log("Фаза Explore: изучение магазина.");
    }

    private void StartSearch()
    {
        CurrentPhase = StudyPhase.Search;

        if (shoppingListCanvas != null)
            shoppingListCanvas.SetActive(true);

        if (EEGCanvas != null)
            EEGCanvas.SetActive(true);


        if (phaseText != null)
            phaseText.text = "Найди товары по памяти";

        if (timerText != null)
            timerText.text = "Найди все товары";

        if (shopTaskManager != null)
            shopTaskManager.BeginTask();

        Debug.Log("Фаза Search: поиск товаров.");
    }

    public void FinishStudy()
    {
        if (CurrentPhase == StudyPhase.Finished)
            return;

        CurrentPhase = StudyPhase.Finished;

        if (phaseText != null)
            phaseText.text = "Исследование завершено";

        if (timerText != null)
            timerText.text = "";

        Debug.Log("Исследование завершено.");
    }
}