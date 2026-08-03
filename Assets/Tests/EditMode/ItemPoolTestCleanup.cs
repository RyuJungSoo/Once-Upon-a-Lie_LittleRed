using UnityEngine;

internal static class ItemPoolTestCleanup
{
    internal static void DestroyPoolObjects()
    {
        foreach (GameObject gameObject in
                 Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.name == "[ItemPool]")
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
