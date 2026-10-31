using UnityEngine;
using LSL;
public class EEGReceiver : MonoBehaviour
{
    private StreamInlet inlet;
    private float[] sampleBuffer;
    private int expectedChannels = 8;

    // Для визуализации в инспекторе
    public float[] latestSample;
    public float averageAmplitude;

    void Start()
    {
        // Поиск потока по имени
        StreamInfo[] results = LSL.LSL.resolve_stream("name", "EEGSimulator");

        if (results.Length > 0)
        {
            inlet = new StreamInlet(results[0]);
            expectedChannels = inlet.info().channel_count();
            sampleBuffer = new float[expectedChannels];

            Debug.Log($"Connected to stream: {inlet.info().name()}, channels: {expectedChannels}");
        }
        else
        { Debug.LogError("LSL stream 'EEGSimulator' not found. Is the Python script running?"); }
    }

    void Update()
    {
        Debug.Log("seeing if inlet is null...");
        Debug.Log(inlet);
        if (inlet == null)
        {
            Debug.Log("inlet IS null BRUH");
            return;
        }

        float[] sample = new float[expectedChannels];

        // Timeout 0.0f makes this a non-blocking check
        double timestamp = inlet.pull_sample(sample, 0.0f);

        // Keep pulling until the buffer is cleared for this frame
        while (timestamp != 0.0)
        {
            ProcessSample(sample);
            timestamp = inlet.pull_sample(sample, 0.0f);
        }

        // Debug.Log($"inlet.samples_available is {inlet.samples_available()} deciding what to do...");



        // // Чтение всех доступных сэмплов
        // while (inlet.samples_available() > 0)
        // {
        //     inlet.pull_sample(sampleBuffer);
        //     ProcessSample(sampleBuffer);
        // }


    }

    void ProcessSample(float[] sample)
    {
        // Сохраняем последний сэмпл для отладки
        latestSample = (float[])sample.Clone();

        Debug.Log("INFO INCOMINGG!!!!!");
        Debug.Log(latestSample[0]);

        // Простой пример обработки: средняя амплитуда по каналам
        float sum = 0;
        foreach (float val in sample)
            sum += Mathf.Abs(val);
        averageAmplitude = sum / sample.Length;

        // Здесь можно добавить логику управления, например:
        // if (averageAmplitude > threshold) { ... }
    }
}
