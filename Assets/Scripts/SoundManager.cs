using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;

public enum GameState { Freeroam, Paused, MainMenu, Hiding, Busy}
public class SoundManager : MonoBehaviour
{
    public GameState gameState;
    public Interactable currentInteractable;
    public Interactable hidingspot;
    public static SoundManager instance;
    public AudioClip[] dirtSfxWalk;
    public AudioClip[] dirtSfxRun;
    public AudioClip[] dirtSfxJump;
    public AudioSource sfxAudioSource;
    public AudioSource musicAudioSource;
    public AudioSource ambientAudioSource;
    [SerializeField] Image playerSpottedUi;
    [SerializeField] Image crouchedUI;
    bool playerspotted;
    float playerSpottedUiAlpha;
    float crouchedUiAlpha;
    float motionBlurValue;
    [SerializeField] Volume volume;
    private MotionBlur motionBlur;
    [SerializeField] AudioClip foundPlayerSFX;
    [SerializeField] AudioClip chaseLoop;
    [SerializeField] AudioClip exploreLoop;
    [SerializeField] AudioClip heartbeatLoop;
    [SerializeField] AudioSource crouchedAudioSoruce;
    bool prevspottedState;
    public bool crouched;
    bool prevCrouched;
    public CinemachineVirtualCamera cmvc;
    public GameObject player;
    public GameObject enemy;

    public int playerLives;
    public bool enemyStunned;
    public float enemyStunTimer;
    public float enemyStunTimerMax = 4;

    [SerializeField] GameObject healthBar1;
    [SerializeField] GameObject healthBar2;
    [SerializeField] Text interactText;
    [SerializeField] GameObject healthVignette;
    [SerializeField] public GameObject flashLightGO;
    [SerializeField] GameObject flashlightBat1;
    [SerializeField] GameObject flashlightBat2;
    [SerializeField] GameObject flashlightBat3;

    public float hidingSpotCamDefaultYRot;

    public Text batteries;
    public int batteriesAmt;
    public AudioClip pickUpSfx;

    public GameObject pauseMenuGO;

    public bool flashLight;
    public float batteryLife;
    public float maxBatteryLife = 20;
    public Text flashLightText;
    public GameObject flashLightObj;
    public Light flashLightLight;
    public float currentLightPower;
    private Exposure exposure;
    public GameObject vol;

    public GameObject popupParent;
    public GameObject healthFull;
    public GameObject flashlightFull;
    public GameObject doorLocked;

    public bool basementDoorLocked;
    public AudioClip flashlightSFX;
    private Fog fog;
    public int cassettesFound;
    public Text cassetteFoundText;
    private void Awake()
    {
        if(instance == null)
            instance = this;

        if (instance != this)
            Destroy(gameObject);
    }
    private void Start()
    {
        playerspotted = false;
        volume.GetComponent<Volume>();
        volume.profile.TryGet<MotionBlur>(out motionBlur);
        musicAudioSource.clip = exploreLoop;
        musicAudioSource.Play();
        prevspottedState = playerspotted;
        prevCrouched = crouched;
        playerLives = 2;
        enemyStunTimer = enemyStunTimerMax;
        healthBar1.SetActive(true);
        healthBar2.SetActive(true);
        gameState = GameState.Freeroam;
        batteries.text = "Batteries: " + batteriesAmt;
        pauseMenuGO.SetActive(false);
        batteryLife = maxBatteryLife;
        flashLightText.text = "Flashlight: Off";
        flashLightObj.SetActive(false);
        healthVignette.SetActive(false);
        flashLightGO.SetActive(false);
        flashlightBat1.SetActive(false);
        flashlightBat2.SetActive(false);
        flashlightBat3.SetActive(false);
        basementDoorLocked = true;
        volume.profile.TryGet<Fog>(out fog);
        ChangeFog(6);
        cassettesFound = 0;
        cassetteFoundText.text = "Cassettes found: " + cassettesFound + "/7";
    }
    public void DrainBatteries()
    {
        if(batteriesAmt > 0)
        {
            batteryLife -= Time.deltaTime;
            flashLightObj.SetActive(true);
            flashLightGO.SetActive(true);
            if (batteryLife > 15)
                currentLightPower = 13000;
            else if (batteryLife > 10)
                currentLightPower = 9000;
            else if (currentLightPower > 5)
                currentLightPower = 4000;
            else
                currentLightPower = 500;

            flashLightLight.intensity = currentLightPower;

            if (batteryLife <= 0)
            {
                batteriesAmt--;
                batteries.text = "Batteries: " + batteriesAmt + "/3";
                batteryLife = maxBatteryLife;
            }

            if(batteriesAmt == 3)
            {
                flashlightBat1.SetActive(true);
                flashlightBat2.SetActive(true);
                flashlightBat3.SetActive(true);
            }
            else if(batteriesAmt == 2)
            {
                flashlightBat1.SetActive(true);
                flashlightBat2.SetActive(true);
                flashlightBat3.SetActive(false);
            }
            else if (batteriesAmt == 1)
            {
                flashlightBat1.SetActive(true);
                flashlightBat2.SetActive(false);
                flashlightBat3.SetActive(false);
            }
            else if (batteriesAmt == 0)
            {
                flashlightBat1.SetActive(false);
                flashlightBat2.SetActive(false);
                flashlightBat3.SetActive(false);
            }

        }
        else
        {
            //No batteries;
            flashLight = false;
            flashLightObj.SetActive(false);
            flashLightGO.SetActive(false);
            flashlightBat1.SetActive(false);
            flashlightBat2.SetActive(false);
            flashlightBat3.SetActive(false);
            flashLightText.text = "Flashlight: Off";
        }
    }
    public void PauseGame()
    {
        foreach (Transform child in popupParent.transform)
            Destroy(child.gameObject);

        pauseMenuGO.SetActive(true);
        Time.timeScale = 0;
    }
    public void UnpauseGame()
    {
        Time.timeScale = 1;
        pauseMenuGO.SetActive(false);
    }
    private void Update()
    {

        EnemyStunned();

        if(flashLight)
            DrainBatteries();

        if(prevspottedState != playerspotted)
        {
            if(playerspotted)
            {
                if (Gamepad.current != null) 
                    Gamepad.current.SetMotorSpeeds(0.1f, 0.5f);
                cmvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().FrequencyGain = 70f;
                sfxAudioSource.PlayOneShot(foundPlayerSFX);
                musicAudioSource.clip = chaseLoop;
                musicAudioSource.Play();
                ambientAudioSource.clip = heartbeatLoop;
                ambientAudioSource.Play();
            }
            else
            {
                if (Gamepad.current != null) 
                    Gamepad.current.SetMotorSpeeds(0f, 0f);
                cmvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0.5f;
                cmvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().FrequencyGain = 0.3f;
                musicAudioSource.clip = exploreLoop;
                musicAudioSource.Play();
                ambientAudioSource.Stop();
            }
            prevspottedState = playerspotted;
        }
        if (playerspotted)
        {
            if(volume != null)
            {
                if(motionBlurValue < 100)
                    motionBlurValue += Time.deltaTime;
                if (motionBlurValue > 100)
                    motionBlurValue = 100;
                if(motionBlur != null)
                    motionBlur.intensity.value = motionBlurValue;
            }

            if (Vector3.Distance(player.transform.position, enemy.transform.position) <= 15)
            {
                if (Vector3.Distance(player.transform.position, enemy.transform.position) <= 10)
                {
                    if (Vector3.Distance(player.transform.position, enemy.transform.position) <= 3 && !enemyStunned && gameState != GameState.Hiding)
                        PlayerCaught();

                    cmvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0.2f;
                }
                else
                {
                    cmvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0.1f;
                }
            }
            else
            {
                cmvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0.05f;
            }

            Color c = playerSpottedUi.color;
            if (playerSpottedUiAlpha < 1)
                playerSpottedUiAlpha += (Time.deltaTime);
            if (playerSpottedUiAlpha > 1)
                playerSpottedUiAlpha = 1;
            c.a = playerSpottedUiAlpha;
            playerSpottedUi.color = c;

        }
        else
        {
            Color c = playerSpottedUi.color;
            if (playerSpottedUiAlpha > 0)
                playerSpottedUiAlpha -= (Time.deltaTime);
            if (playerSpottedUiAlpha < 0)
                playerSpottedUiAlpha = 0;
            c.a = playerSpottedUiAlpha;
            playerSpottedUi.color = c;

            if (volume != null)
            {
                if (motionBlurValue > 2)
                    motionBlurValue -= Time.deltaTime;
                if (motionBlurValue < 2)
                    motionBlurValue = 2;
                if (motionBlur != null)
                    motionBlur.intensity.value = motionBlurValue;
            }
        }
        if(crouched)
        {
            Color c = crouchedUI.color;
            if (crouchedUiAlpha < 0.5f)
                crouchedUiAlpha += (Time.deltaTime);
            if (crouchedUiAlpha > 0.5f)
                crouchedUiAlpha = 0.5f;
            c.a = crouchedUiAlpha;
            crouchedUI.color = c;
        }
        else
        {
            Color c = crouchedUI.color;
            if (crouchedUiAlpha > 0)
                crouchedUiAlpha -= (Time.deltaTime);
            if (crouchedUiAlpha < 0)
                crouchedUiAlpha = 0;
            c.a = crouchedUiAlpha;
            crouchedUI.color = c;
        }
        if(prevCrouched != crouched)
        {
            if(crouched)
                crouchedAudioSoruce.Play();
            else
                crouchedAudioSoruce.Stop();
            prevCrouched = crouched;
        }

        if(hidingspot != null)
        {
            interactText.text = "";
        }
        else if (currentInteractable != null)
        {
            interactText.text = "" + currentInteractable.interactableName;
        }
        else
        {
            interactText.text = "";
        }
    }
    public void PlayFootstepWalk()
    {
        var index = Random.Range(0, dirtSfxWalk.Length - 1);
        sfxAudioSource.PlayOneShot(dirtSfxWalk[index]);
    }

    public void PlayFootstepRun()
    {
        var index = Random.Range(0, dirtSfxRun.Length - 1);
        sfxAudioSource.PlayOneShot(dirtSfxRun[index]);
    }
    public void ChangeFog(float value)
    {
        if (fog != null)
            fog.meanFreePath.value = value;
    }
    public void PlayJumpSFX()
    {
        var index = Random.Range(0, dirtSfxJump.Length - 1);
        sfxAudioSource.PlayOneShot(dirtSfxJump[index]);
    }
    public void PlayerSpotted()
    {
        playerspotted = true;
    }
    public void PlayerLost()
    {
        playerspotted = false;
    }
    private void OnApplicationQuit()
    {
        if (Gamepad.current != null) 
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
    private void OnApplicationPause(bool pause)
    {
        if (Gamepad.current != null) 
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
    public void PlayerCaught()
    {
        playerLives--;
        enemyStunned = true;
        Animator anim = enemy.GetComponent<Patrol>().anim;
        anim.Play("Attack");
        if (playerLives <= 0)
        {
            //Play death animation
            healthBar1.SetActive(false);
            playerLives = 0;
            if(Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(0f, 0f);
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            //Play caught animation
            healthVignette.SetActive(true);
            healthBar2.SetActive(false);
        }
    }
    void EnemyStunned()
    {
        if(enemyStunned)
        {
            enemyStunTimer -= Time.deltaTime;
        }
        if(enemyStunTimer <= 0)
        {
            enemyStunned = false;
            enemyStunTimer = enemyStunTimerMax;
        }
    }
    public void Interact()
    {
        if(currentInteractable != null && gameState == GameState.Freeroam)
        {
            if (currentInteractable.intercatableType == IntercatableType.HidingSpot)
                Hide();
            else if (currentInteractable.intercatableType == IntercatableType.Pickup)
                PickUp(currentInteractable.isBatteries);
            else if (currentInteractable.intercatableType == IntercatableType.Cassette)
                PlayCassette();
            else if (currentInteractable.intercatableType == IntercatableType.Basement)
                Basement();
            else if (currentInteractable.intercatableType == IntercatableType.Key)
            {
                basementDoorLocked = false;
                StartCoroutine(Storyline.instance.PlayStoryTwo());
                sfxAudioSource.PlayOneShot(pickUpSfx, 0.7f);
                Destroy(currentInteractable.gameObject);
            }
        }
        else if(hidingspot != null && gameState == GameState.Hiding)
            Unhide();
    }
    public void Basement()
    {
        if(basementDoorLocked)
        {
            sfxAudioSource.PlayOneShot(currentInteractable.audio);
            foreach (Transform child in popupParent.transform)
            {
                Destroy(child.gameObject);
            }
            var popup = Instantiate(doorLocked);
            popup.transform.SetParent(popupParent.transform);
            var vec = new Vector3(0, 0, 0);
            popup.transform.localPosition = vec;
        }
        else
        {
            sfxAudioSource.PlayOneShot(currentInteractable.audio);
            SceneManager.LoadScene("EndScene");
        }
    }
    public void Hide()
    {
        hidingSpotCamDefaultYRot = currentInteractable.cameraObj.transform.eulerAngles.y;
        currentInteractable.cameraObj.SetActive(true);
        hidingspot = currentInteractable;
        gameState = GameState.Hiding;
        flashLight = false;
        flashLightText.text = "Flashlight: Off";
        flashLightObj.SetActive(false);
        flashLightGO.SetActive(false);
        sfxAudioSource.PlayOneShot(currentInteractable.audio);
    }
    public void Unhide()
    {
        hidingspot.cameraObj.transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, hidingSpotCamDefaultYRot, transform.rotation.z));
        hidingspot.cameraObj.SetActive(false);
        sfxAudioSource.PlayOneShot(hidingspot.audio);
        hidingspot = null;
        gameState = GameState.Freeroam;
    }
    public void PlayCassette()
    {
        if(currentInteractable.audio != null)
            sfxAudioSource.PlayOneShot(currentInteractable.audio);
        cassettesFound++;
        cassetteFoundText.text = "Cassettes found: " + cassettesFound + "/7";
        Destroy(currentInteractable.gameObject);
    }
    public void PickUp(bool isBatteries)
    {
        if(isBatteries)
        {
            if(batteriesAmt < 3)
            {
                sfxAudioSource.PlayOneShot(pickUpSfx,0.7f);
                batteriesAmt++;
                batteries.text = "Batteries: " + batteriesAmt + "/3";
                Destroy(currentInteractable.gameObject);
            }
            else
            {
                foreach (Transform child in popupParent.transform)
                {
                    Destroy(child.gameObject);
                }
                var popup = Instantiate(flashlightFull);
                popup.transform.SetParent(popupParent.transform);
                var vec = new Vector3(0, 0, 0);
                popup.transform.localPosition = vec;
            }
        }
        else
        {
            if(playerLives == 1)
            {
                playerLives = 2;
                healthVignette.SetActive(false);
                healthBar2.SetActive(true);
                sfxAudioSource.PlayOneShot(pickUpSfx, 0.7f);
                Destroy(currentInteractable.gameObject);
            }
            else
            {
                foreach (Transform child in popupParent.transform)
                {
                    Destroy(child.gameObject);
                }
                var popup = Instantiate(healthFull);
                popup.transform.SetParent(popupParent.transform);
                var vec = new Vector3(0, 0, 0);
                popup.transform.localPosition = vec;
            }
        }
    }

    public void LightsOff(bool off)
    {
        Volume volume = vol.GetComponent<Volume>();
        if(off)
        {
            if (volume.profile.TryGet<Exposure>(out exposure))
                exposure.fixedExposure.value = 13.8f;
        }
        else
        {
            if (volume.profile.TryGet<Exposure>(out exposure))
                exposure.fixedExposure.value = 11f;
        }

    }
}
