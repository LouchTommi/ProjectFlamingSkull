using UnityEngine;

public class Story_One : MonoBehaviour
{
    public bool hasTriggered;

    private void Start()
    {
        hasTriggered = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if(!hasTriggered)
        {
            if (other.CompareTag("Player"))
            {
                StartCoroutine(Storyline.instance.PlayStoryOne());
                hasTriggered = true;
            }
        }
    }
}
