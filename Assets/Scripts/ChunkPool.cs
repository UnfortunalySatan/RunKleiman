using System.Collections.Generic;
using UnityEngine;

public class ChunkPool : MonoBehaviour
{
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    // Получить чанк из пула (или создать новый, если пул пуст)
    public GameObject GetChunk(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        GameObject chunkToSpawn;

        // Если в пуле есть готовый неактивный объект — берем его
        if (poolDictionary[prefab].Count > 0)
        {
            chunkToSpawn = poolDictionary[prefab].Dequeue();
            chunkToSpawn.transform.position = position;
            chunkToSpawn.transform.rotation = rotation;
            chunkToSpawn.SetActive(true);
        }
        else
        {
            // Если пул пуст — создаем новый элемент
            chunkToSpawn = Instantiate(prefab, position, rotation);
        }

        return chunkToSpawn;
    }

    // Вернуть чанк в пул вместо его уничтожения
    public void ReturnChunk(GameObject prefab, GameObject chunkInstance)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        chunkInstance.SetActive(false);
        poolDictionary[prefab].Enqueue(chunkInstance);
    }

    // Полная очистка пула (нужна при перезапуске игры в MainMenu)
    public void ClearAllPools()
    {
        foreach (var keyValuePair in poolDictionary)
        {
            while (keyValuePair.Value.Count > 0)
            {
                GameObject obj = keyValuePair.Value.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
        poolDictionary.Clear();
    }
}
