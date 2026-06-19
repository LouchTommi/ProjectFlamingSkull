using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
    public float timer;
    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > 1.5f)
            Destroy(gameObject);
    }
}
