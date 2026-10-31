using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EEGApiClient : MonoBehaviour
{



    [Header("API")]
    [SerializeField]
    private string apiBaseUrl =
    "http://127.0.0.1:8000";
    [SerializeField] private EEGGraphUI eegGraph;

    [SerializeField] private int sampleIndex = 0;

    [Header("UI")]
    [SerializeField] private TMP_InputField participantIdInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button analyzeButton;

    [Serializable]
    private class PredictionRequest
    {
        public int sample_index;
    }

    [Serializable]
    private class PredictionResponse
    {
        public string participant_id;
        public int prediction;
        public string class_name;
        public int feature_count;
        public int channel_count;
        public float duration_seconds;
        public string source;
        public Probabilities probabilities;
        public string channel_name;
        public float[] signal;
    }

    [Serializable]
    private class Probabilities
    {
        public float CN;
        public float AD;
    }

    private void Start()
    {
        if (analyzeButton != null)
            analyzeButton.onClick.AddListener(AnalyzeEEG);

        if (statusText != null)
            statusText.text = "EEG API: готов к подключению";

        if (resultText != null)
            resultText.text = "Результат пока отсутствует";
    }

    public void AnalyzeEEG()
    {
        StartCoroutine(SendPredictionRequest());
    }

    private IEnumerator SendPredictionRequest()
    {
        if (analyzeButton != null)
            analyzeButton.interactable = false;

        string participantId = participantIdInput != null
            ? participantIdInput.text.Trim()
            : "sub-001";

        if (string.IsNullOrWhiteSpace(participantId))
            participantId = "sub-001";

        if (!participantId.StartsWith("sub-"))
            participantId = "sub-" + participantId;

        if (statusText != null)
            statusText.text = $"Обработка EEG: {participantId}...";

        string url = $"{apiBaseUrl}/predict_file?participant_id={UnityWebRequest.EscapeURL(participantId)}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.timeout = 120;

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                PredictionResponse response =
                    JsonUtility.FromJson<PredictionResponse>(
                        webRequest.downloadHandler.text);

                if (eegGraph != null && response.signal != null)
                {
                    eegGraph.SetSignal(response.signal);
                }

                if (statusText != null)
                    statusText.text = "EEG API: подключено";

                if (resultText != null)
                {
                    resultText.text =
                        $"Участник: {participantId}\n" +
                        $"Результат модели: {response.class_name}\n" +
                        $"CN: {response.probabilities.CN * 100:F1}%\n" +
                        $"AD: {response.probabilities.AD * 100:F1}%\n" +
                        $"Каналов: {response.channel_count}\n" +
                        $"Признаков: {response.feature_count}\n" +
                        $"Длительность: {response.duration_seconds:F1} сек.";
                }

                Debug.Log(
                    $"EEG получен: participant={response.participant_id}, " +
                    $"class={response.class_name}, " +
                    $"CN={response.probabilities.CN}, " +
                    $"AD={response.probabilities.AD}"
                );

                if (ExperimentReportManager.Instance != null)
                {
                    ExperimentReportManager.Instance.SetParticipantId(
                        response.participant_id
                    );

                    ExperimentReportManager.Instance.SetEEGResult(
                        response.class_name,
                        response.probabilities.CN,
                        response.probabilities.AD,
                        response.feature_count,
                        response.channel_count,
                        response.duration_seconds
                    );

                    Debug.Log("EEG передан в ExperimentReportManager.");
                }

                Debug.Log(webRequest.downloadHandler.text);
            }
            else
            {
                if (statusText != null)
                    statusText.text = "Ошибка обработки EEG";

                if (resultText != null)
                    resultText.text = webRequest.downloadHandler.text;

                Debug.LogError(
                    $"EEG API error: {webRequest.error}\n" +
                    webRequest.downloadHandler.text);
            }
        }

        if (analyzeButton != null)
            analyzeButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (analyzeButton != null)
            analyzeButton.onClick.RemoveListener(AnalyzeEEG);
    }
}