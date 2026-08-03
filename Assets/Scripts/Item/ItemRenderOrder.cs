using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ItemRenderOrder
{
    private static int nextSortingOrder = 1;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void Initialize()
    {
        nextSortingOrder = 1;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void Assign(GameObject item)
    {
        SortingGroup sortingGroup =
            item.GetComponent<SortingGroup>();

        if (sortingGroup == null)
        {
            sortingGroup = item.AddComponent<SortingGroup>();
        }

        SpriteRenderer spriteRenderer =
            item.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
        {
            sortingGroup.sortingLayerID =
                spriteRenderer.sortingLayerID;
        }

        sortingGroup.sortingOrder =
            nextSortingOrder++;
    }

    public static void AssignSceneItems(Scene scene)
    {
        int itemLayer = LayerMask.NameToLayer("Item");

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] descendants =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform descendant in descendants)
            {
                if (descendant.gameObject.layer != itemLayer ||
                    HasItemLayerParent(descendant, itemLayer))
                {
                    continue;
                }

                Assign(descendant.gameObject);
            }
        }
    }

    private static bool HasItemLayerParent(
        Transform item,
        int itemLayer
    )
    {
        Transform parent = item.parent;

        while (parent != null)
        {
            if (parent.gameObject.layer == itemLayer)
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    private static void OnSceneLoaded(
        Scene scene,
        LoadSceneMode loadMode
    )
    {
        AssignSceneItems(scene);
    }
}
