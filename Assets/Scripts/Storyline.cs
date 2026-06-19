using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class Storyline : MonoBehaviour
{
    public GameObject radio;

    public static Storyline instance;
    public Text objectiveText;
    public Text subtitleText;
    public GameObject storylineBox;
    public AudioSource sfxSource;
    public GameObject lightSFX;
    public AudioClip powerDownClip;

    public AudioClip doorsLocked;
    public AudioClip basement;
    private void Awake()
    {
        if (instance == null)
            instance = this;

        if (instance != this)
            Destroy(gameObject);

        radio.SetActive(false);
        storylineBox.SetActive(false);
    }
    public IEnumerator PlayStoryOne()
    {
        subtitleText.text = "The doors locked, you need to find the key!";
        objectiveText.text = "Locate the key for the front door.";
        yield return new WaitForSeconds(1);
        sfxSource.PlayOneShot(doorsLocked);
        radio.SetActive(true);
        storylineBox.SetActive(true);
        yield return new WaitForSeconds(4);
        radio.SetActive(false);
        storylineBox.SetActive(false);
    }
    public IEnumerator PlayStoryTwo()
    {
        SoundManager.instance.LightsOff(true);
        lightSFX.SetActive(false);
        sfxSource.PlayOneShot(powerDownClip, 1f);
        SoundManager.instance.ChangeFog(12);
        subtitleText.text = "The doctor must have shut down the power! You'll need to turn the generator back on to access the main doors, unfortunately for you, it's locked down in the basement!";
        objectiveText.text = "Head to the basement to find the generator.";
        yield return new WaitForSeconds(1);
        sfxSource.PlayOneShot(basement);
        radio.SetActive(true);
        storylineBox.SetActive(true);
        yield return new WaitForSeconds(9);
        radio.SetActive(false);
        storylineBox.SetActive(false);
    }
}
