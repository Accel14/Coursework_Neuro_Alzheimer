using UnityEngine;

public class PepperSpawner : MonoBehaviour
{
    public GameObject[] peppers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (peppers.Length > 0)
        {
            // Выбираем случайный индекс от 0 до items.Length - 1
            int randomIndex = Random.Range(0, peppers.Length);

            // Получаем элемент по этому индексу
            GameObject randomPepper = peppers[randomIndex];

            Debug.Log("Выбранный элемент: " + randomPepper.name);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
