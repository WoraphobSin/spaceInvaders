using UnityEngine;

// A short-lived explosion picture. It deletes itself after "lifetime" seconds.
public class Explosion : MonoBehaviour
{
    public float lifetime = 0.3f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public static void Spawn(GameObject prefab, Vector3 position, Color color)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, position, Quaternion.identity);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }
}
