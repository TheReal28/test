using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public GameObject[] roomPrefabs; // Массив префабов комнат
    public int numberOfRooms = 5;    // Сколько комнат спавнить
    public Vector3 roomSize = new Vector3(10, 1, 10); // Размер комнаты
    public int seed = 0; // Сид для генерации
    public float deadEndProbability = 0.3f; // Вероятность создания тупика
    public float loopProbability = 0.1f; // Вероятность создания круга

    private Vector3 spawnPosition = Vector3.zero; // Начальная позиция
    private HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>(); // Используемые позиции
    private List<Vector3> spawnedRooms = new List<Vector3>(); // Позиции уже созданных комнат
    private System.Random random; // Случайный генератор
    private Queue<Vector3> pathQueue = new Queue<Vector3>(); // Очередь для отслеживания текущего пути
    private Vector3? lastDirection = null; // Направление последнего шага для контроля поворотов

    void Start()
    {
        Random.InitState(seed);
        random = new System.Random(seed);
        SpawnLevel();
    }

    void SpawnLevel()
    {
        // Спавним первую комнату в начальной позиции
        SpawnRoomAtPosition(spawnPosition);

        // Добавляем начальную позицию в список использованных
        usedPositions.Add(new Vector2Int((int)spawnPosition.x, (int)spawnPosition.z));

        // Сохраняем позицию как уже созданную комнату
        spawnedRooms.Add(spawnPosition);

        // Добавляем в очередь пути
        pathQueue.Enqueue(spawnPosition);

        // Спавним остальные комнаты
        for (int i = 1; i < numberOfRooms; i++)
        {
            SpawnRoom();
        }
    }

    void SpawnRoom()
    {
        if (roomPrefabs.Length == 0) return;

        // Определяем, создавать ли тупик
        bool createDeadEnd = random.NextDouble() < deadEndProbability && spawnedRooms.Count > 1;

        // Определяем, создавать ли круг (соединение с уже существующей комнатой)
        bool createLoop = random.NextDouble() < loopProbability && spawnedRooms.Count > 2;

        if (createLoop)
        {
            // Создаем круг, добавляя комнату между двумя уже существующими комнатами
            // Ищем позиции, которые находятся между двумя уже существующими комнатами
            List<Vector3> possibleLoopPositions = new List<Vector3>();

            // Проверяем все возможные позиции между комнатами
            foreach (Vector3 roomPos in spawnedRooms)
            {
                Vector3[] directions = {
                    roomPos + new Vector3(0, 0, roomSize.z),  // Вперед
                    roomPos + new Vector3(0, 0, -roomSize.z), // Назад
                    roomPos + new Vector3(roomSize.x, 0, 0),  // Вправо
                    roomPos + new Vector3(-roomSize.x, 0, 0)  // Влево
                };

                foreach (Vector3 dir in directions)
                {
                    Vector2Int newPosKey = new Vector2Int((int)dir.x, (int)dir.z);

                    // Если позиция не занята, проверяем, есть ли рядом другие комнаты
                    if (!usedPositions.Contains(newPosKey))
                    {
                        // Проверяем, есть ли рядом хотя бы 2 занятые позиции, чтобы создать соединение
                        Vector3[] neighborDirections = {
                            dir + new Vector3(0, 0, roomSize.z),   // Вперед от новой позиции
                            dir + new Vector3(0, 0, -roomSize.z),  // Назад от новой позиции
                            dir + new Vector3(roomSize.x, 0, 0),   // Вправо от новой позиции
                            dir + new Vector3(-roomSize.x, 0, 0)   // Влево от новой позиции
                        };

                        int neighborCount = 0;
                        foreach (Vector3 neighbor in neighborDirections)
                        {
                            Vector2Int neighborKey = new Vector2Int((int)neighbor.x, (int)neighbor.z);
                            if (usedPositions.Contains(neighborKey))
                            {
                                neighborCount++;
                            }
                        }

                        // Если рядом есть хотя бы 2 комнаты, добавляем позицию как возможную для круга
                        if (neighborCount >= 2)
                        {
                            possibleLoopPositions.Add(dir);
                        }
                    }
                }
            }

            if (possibleLoopPositions.Count > 0)
            {
                Vector3 loopPosition = possibleLoopPositions[random.Next(possibleLoopPositions.Count)];

                // Создаем комнату в позиции, соединяющей две части лабиринта
                Vector2Int loopPosKey = new Vector2Int((int)loopPosition.x, (int)loopPosition.z);
                SpawnRoomAtPosition(loopPosition);
                usedPositions.Add(loopPosKey);
                spawnedRooms.Add(loopPosition);

                // Обновляем последнее направление при создании круга
                if (spawnedRooms.Count > 1)
                {
                    Vector3 lastRoom = spawnedRooms[spawnedRooms.Count - 2];
                    lastDirection = (loopPosition - lastRoom).normalized;
                }
                return;
            }
        }

        // Получаем возможные позиции для спауна
        List<Vector3> possiblePositions = GetPossiblePositions();

        if (possiblePositions.Count == 0) return;

        Vector3 chosenPosition;

        if (createDeadEnd)
        {
            // Для тупика выбираем позицию из любой существующей комнаты (не только из пути)
            Vector3 randomRoom = spawnedRooms[random.Next(spawnedRooms.Count)];
            List<Vector3> availablePositions = GetPossiblePositionsFromPosition(randomRoom);

            if (availablePositions.Count > 0)
            {
                chosenPosition = availablePositions[random.Next(availablePositions.Count)];
            }
            else
            {
                // Если из выбранной комнаты нет доступных позиций, выбираем случайную из всех возможных
                chosenPosition = possiblePositions[random.Next(possiblePositions.Count)];
            }
        }
        else
        {
            // Обычный выбор позиции - из всех возможных, но с приоритетом продолжения направления
            chosenPosition = GetPositionWithDirectionPriority(possiblePositions);
        }

        // Спавним комнату в выбранной позиции
        SpawnRoomAtPosition(chosenPosition);

        // Добавляем позицию в список использованных
        usedPositions.Add(new Vector2Int((int)chosenPosition.x, (int)chosenPosition.z));

        // Сохраняем позицию как уже созданную комнату
        spawnedRooms.Add(chosenPosition);

        // Обновляем последнее направление
        if (spawnedRooms.Count > 1)
        {
            Vector3 lastRoom = spawnedRooms[spawnedRooms.Count - 2];
            lastDirection = (chosenPosition - lastRoom).normalized;
        }

        // Добавляем в очередь путей, если это не тупик
        if (!createDeadEnd)
        {
            pathQueue.Enqueue(chosenPosition);

            // Ограничиваем размер очереди, чтобы предотвратить слишком длинные пути
            if (pathQueue.Count > numberOfRooms / 2)
            {
                pathQueue.Dequeue();
            }
        }
    }

    /// <summary>
    /// Спавнит комнату в заданной позиции
    /// </summary>
    /// <param name="position">Позиция для спауна комнаты</param>
    void SpawnRoomAtPosition(Vector3 position)
    {
        // Выбираем случайный префаб комнаты
        GameObject selectedRoom = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

        // Спавним комнату
        GameObject room = Instantiate(selectedRoom, position, Quaternion.identity);
    }

    /// <summary>
    /// Возвращает все возможные позиции для спауна новых комнат
    /// </summary>
    /// <returns>Список возможных позиций</returns>
    List<Vector3> GetPossiblePositions()
    {
        List<Vector3> possiblePositions = new List<Vector3>();

        // Для каждой уже созданной комнаты проверяем возможные направления
        foreach (Vector3 roomPos in spawnedRooms)
        {
            possiblePositions.AddRange(GetPossiblePositionsFromPosition(roomPos));
        }

        return possiblePositions;
    }

    /// <summary>
    /// Возвращает возможные позиции для спауна из заданной позиции
    /// </summary>
    /// <param name="roomPos">Позиция комнаты, из которой ищем направления</param>
    /// <returns>Список возможных позиций</returns>
    List<Vector3> GetPossiblePositionsFromPosition(Vector3 roomPos)
    {
        List<Vector3> possiblePositions = new List<Vector3>();

        // Проверяем направления: вперед, назад, влево, вправо
        Vector3[] directions = {
            roomPos + new Vector3(0, 0, roomSize.z),  // Вперед
            roomPos + new Vector3(0, 0, -roomSize.z), // Назад
            roomPos + new Vector3(roomSize.x, 0, 0),  // Вправо
            roomPos + new Vector3(-roomSize.x, 0, 0)  // Влево
        };

        foreach (Vector3 dir in directions)
        {
            Vector2Int posKey = new Vector2Int((int)dir.x, (int)dir.z);

            // Проверяем, не занята ли позиция
            if (!usedPositions.Contains(posKey))
            {
                possiblePositions.Add(dir);
            }
        }

        return possiblePositions;
    }

    /// <summary>
    /// Возвращает случайное направление из возможных
    /// </summary>
    /// <returns>Случайное направление</returns>
    Vector3 GetRandomDirection(Vector3 fromPosition)
    {
        Vector3[] directions = {
            new Vector3(0, 0, roomSize.z),   // Вперед
            new Vector3(0, 0, -roomSize.z),  // Назад
            new Vector3(roomSize.x, 0, 0),   // Вправо
            new Vector3(-roomSize.x, 0, 0)   // Влево
        };

        return directions[random.Next(directions.Length)];
    }

    /// <summary>
    /// Выбирает позицию с приоритетом продолжения текущего направления
    /// </summary>
    /// <param name="possiblePositions">Список возможных позиций</param>
    /// <returns>Выбранная позиция</returns>
    Vector3 GetPositionWithDirectionPriority(List<Vector3> possiblePositions)
    {
        if (possiblePositions.Count == 0) return Vector3.zero;

        // Если нет последнего направления (первый шаг), выбираем случайно
        if (!lastDirection.HasValue || spawnedRooms.Count < 2)
        {
            return possiblePositions[random.Next(possiblePositions.Count)];
        }

        // Получаем последнюю комнату
        Vector3 lastRoom = spawnedRooms[spawnedRooms.Count - 1];

        // Находим позиции, которые продолжают текущее направление
        List<Vector3> straightPositions = new List<Vector3>();
        List<Vector3> turnPositions = new List<Vector3>();

        foreach (Vector3 pos in possiblePositions)
        {
            Vector3 directionToPos = (pos - lastRoom).normalized;

            // Проверяем, совпадает ли направление с последним направлением (с небольшой погрешностью)
            if (Vector3.Dot(directionToPos, lastDirection.Value) > 0.9f) // ~25 градусов погрешности
            {
                straightPositions.Add(pos);
            }
            else
            {
                turnPositions.Add(pos);
            }
        }

        // Если есть позиции, продолжающие направление, выбираем из них с 70% вероятностью
        if (straightPositions.Count > 0 && random.NextDouble() < 0.7)
        {
            return straightPositions[random.Next(straightPositions.Count)];
        }
        // Если направление продолжения нет, но есть повороты - выбираем из поворотов
        else if (turnPositions.Count > 0)
        {
            return turnPositions[random.Next(turnPositions.Count)];
        }
        // Если нет поворотов, но есть прямые - выбираем из прямых
        else if (straightPositions.Count > 0)
        {
            return straightPositions[random.Next(straightPositions.Count)];
        }
        // Если нет ни того, ни другого - выбираем случайно
        else
        {
            return possiblePositions[random.Next(possiblePositions.Count)];
        }
    }
}
