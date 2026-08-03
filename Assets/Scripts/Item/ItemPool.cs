using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class ItemPool : MonoBehaviour
{
    private const string PoolObjectName = "[ItemPool]";

    private static ItemPool instance;
    private static bool isQuitting;

    private readonly Dictionary<GameObject, Stack<GameObject>>
        availableByPrefab =
            new Dictionary<GameObject, Stack<GameObject>>();

    private readonly Dictionary<GameObject, GameObject>
        prefabByInstance =
            new Dictionary<GameObject, GameObject>();

    private readonly HashSet<GameObject> availableInstances =
        new HashSet<GameObject>();

    private readonly List<GameObject> destroyedInstances =
        new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    public static GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation
    )
    {
        return Spawn(
            prefab,
            position,
            rotation,
            SceneManager.GetActiveScene()
        );
    }

    public static GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Scene destinationScene
    )
    {
        if (prefab == null)
        {
            throw new ArgumentNullException(nameof(prefab));
        }

        ItemPool pool = GetOrCreate();
        GameObject item = pool.Take(prefab);

        item.transform.SetParent(null);

        if (destinationScene.IsValid() &&
            destinationScene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(
                item,
                destinationScene
            );
        }

        item.transform.SetPositionAndRotation(
            position,
            rotation
        );
        item.SetActive(true);

        return item;
    }

    public static bool Release(GameObject item)
    {
        if (item == null ||
            instance == null ||
            !instance.prefabByInstance.TryGetValue(
                item,
                out GameObject prefab
            ) ||
            !instance.availableInstances.Add(item))
        {
            return false;
        }

        item.SetActive(false);
        item.transform.SetParent(
            instance.transform,
            false
        );
        instance.GetAvailable(prefab).Push(item);

        return true;
    }

    private static ItemPool GetOrCreate()
    {
        if (isQuitting)
        {
            throw new InvalidOperationException(
                "Items cannot be spawned while the " +
                "application is quitting."
            );
        }

        if (instance != null)
        {
            return instance;
        }

        GameObject poolObject =
            new GameObject(PoolObjectName);
        instance = poolObject.AddComponent<ItemPool>();

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(poolObject);
        }

        return instance;
    }

    private GameObject Take(GameObject prefab)
    {
        Stack<GameObject> available =
            GetAvailable(prefab);
        GameObject item = null;

        while (available.Count > 0 &&
               item == null)
        {
            item = available.Pop();
            availableInstances.Remove(item);
        }

        if (item != null)
        {
            return item;
        }

        item = Instantiate(prefab);
        prefabByInstance.Add(item, prefab);
        return item;
    }

    private Stack<GameObject> GetAvailable(
        GameObject prefab
    )
    {
        if (availableByPrefab.TryGetValue(
                prefab,
                out Stack<GameObject> available
            ))
        {
            return available;
        }

        available = new Stack<GameObject>();
        availableByPrefab.Add(prefab, available);
        return available;
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded +=
            RemoveDestroyedSceneItems;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -=
            RemoveDestroyedSceneItems;
    }

    private void RemoveDestroyedSceneItems(Scene _)
    {
        destroyedInstances.Clear();

        foreach (GameObject item in
                 prefabByInstance.Keys)
        {
            if (item == null)
            {
                destroyedInstances.Add(item);
            }
        }

        foreach (GameObject item in
                 destroyedInstances)
        {
            prefabByInstance.Remove(item);
            availableInstances.Remove(item);
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
