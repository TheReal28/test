using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject[] roomPrefabs; // Массив префабов комнат
    public int numberOfRooms = 5;    // Сколько комнат спавнить
    public Vector3 roomSize = new Vector3(10, 1, 10); // Размер комнаты

    private Vector3 spawnPosition = Vector3.zero; // Начальная позиция

    void Start()
    {
        SpawnLevel();
    }

    void SpawnLevel()
    {
        for (int i = 0; i < numberOfRooms; i++)
        {
            SpawnRoom();
        }
    }

    void SpawnRoom()
    {
        if (roomPrefabs.Length == 0) return;

        // Выбираем случайный префаб комнаты
        GameObject selectedRoom = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

        // Спавним комнату
        GameObject room = Instantiate(selectedRoom, spawnPosition, Quaternion.identity);

        // Смещаем позицию для следующей комнаты
        spawnPosition += new Vector3(0, 0, roomSize.z);
    }
}
