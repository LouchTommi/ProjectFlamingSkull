using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#endif

public enum PauseMenuOptions { Controls, Quit, ControlWindow }
namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
        public bool crouch;
        public bool sprint;
        public bool interact;
		public bool menu;
		public bool light;
        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		public PauseMenuOptions pauseMenuOptions;
        public Text controlText;
        public Text quitText;
        public GameObject controlPanel;

        public AudioSource audioSource;
        public AudioClip selectClip;
        public AudioClip backClip;
        public AudioClip upClip;
        public AudioClip downClip;

        private void Start()
        {
            pauseMenuOptions = PauseMenuOptions.Controls;
            controlPanel.SetActive(false);
            SoundManager.instance.UnpauseGame();
        }

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
			menu = !menu;
			if(menu)
			{
                controlPanel.SetActive(false);
                pauseMenuOptions = PauseMenuOptions.Controls;
                controlText.color = Color.green;
                quitText.color = Color.black;
                audioSource.PlayOneShot(upClip);
                SoundManager.instance.PauseGame();
			}
			else
			{
                audioSource.PlayOneShot(downClip);
                SoundManager.instance.UnpauseGame();
            }
		}
        public void OnLight(InputValue value)
        {
            if (sprint)
                return;
            LightInput(value.isPressed);
            if (light)
            {
				if (SoundManager.instance.batteriesAmt > 0 && SoundManager.instance.gameState == GameState.Freeroam)
				{
                    SoundManager.instance.flashLight = true;
                    SoundManager.instance.flashLightText.text = "Flashlight: On";
                    SoundManager.instance.sfxAudioSource.PlayOneShot(SoundManager.instance.flashlightSFX, 0.2f);
                    SoundManager.instance.flashLightObj.SetActive(true);
                    SoundManager.instance.flashLightGO.SetActive(true);
                }
				else
				{
                    SoundManager.instance.flashLight = false;
                    SoundManager.instance.flashLightText.text = "Flashlight: Off";
                    SoundManager.instance.sfxAudioSource.PlayOneShot(SoundManager.instance.flashlightSFX, 0.2f);
                    SoundManager.instance.flashLightObj.SetActive(false);
                    SoundManager.instance.flashLightGO.SetActive(false);
                    light = false;
                }
            }
			else
			{
                SoundManager.instance.flashLight = false;
				SoundManager.instance.flashLightText.text = "Flashlight: Off";
                SoundManager.instance.sfxAudioSource.PlayOneShot(SoundManager.instance.flashlightSFX, 0.2f);
                SoundManager.instance.flashLightObj.SetActive(false);
                SoundManager.instance.flashLightGO.SetActive(false);
                light = false;
            }
        }
        public void OnCrouch(InputValue value)
        {
            CrouchInput();
        }
        public void OnBack(InputValue value)
        {
            if(menu)
            {
                if (pauseMenuOptions == PauseMenuOptions.ControlWindow)
                {
                    audioSource.PlayOneShot(backClip);
                    controlPanel.SetActive(false);
                    pauseMenuOptions = PauseMenuOptions.Controls;
                }
                else
                {
                    audioSource.PlayOneShot(downClip);
                    SoundManager.instance.UnpauseGame();
                    menu = false;
                    pauseMenuOptions = PauseMenuOptions.Controls;
                }
            }
        }
        public void OnDown(InputValue value)
        {
            if (menu)
            {
                if (pauseMenuOptions == PauseMenuOptions.Controls)
                {
                    audioSource.PlayOneShot(downClip);
                    controlText.color = Color.black;
                    quitText.color = Color.green;
                    pauseMenuOptions = PauseMenuOptions.Quit;
                }
            }
        }
        public void OnUp(InputValue value)
        {
            if (menu)
            {
                if (pauseMenuOptions == PauseMenuOptions.Quit)
                {
                    audioSource.PlayOneShot(upClip);
                    controlText.color = Color.green;
                    quitText.color = Color.black;
                    pauseMenuOptions = PauseMenuOptions.Controls;
                }
            }
        }

        public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
			crouch = false;
            SoundManager.instance.flashLight = false;
            SoundManager.instance.flashLightText.text = "Flashlight: Off";
            SoundManager.instance.flashLightObj.SetActive(false);
            SoundManager.instance.flashLightGO.SetActive(false);
            light = false;
        }
        public void OnInteract(InputValue value)
        {
            if(!menu)
            {
                InteractInput(value.isPressed);
                if (interact)
                {
                    if ((SoundManager.instance.currentInteractable != null) || (SoundManager.instance.hidingspot != null))
                    {
                        SoundManager.instance.Interact();
                    }
                    interact = false;
                }
            }
            else
            {
                if (pauseMenuOptions == PauseMenuOptions.Controls)
                {
                    audioSource.PlayOneShot(selectClip);
                    controlPanel.SetActive(true);
                    pauseMenuOptions = PauseMenuOptions.ControlWindow;
                }
                else if (pauseMenuOptions == PauseMenuOptions.Quit)
                {
                    audioSource.PlayOneShot(selectClip);
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }
#endif


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			//jump = newJumpState;
		}
        public void CrouchInput()
        {
			if(!sprint)
				crouch = !crouch;
        }

        public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}
        public void InteractInput(bool newInteractState)
        {
            interact = newInteractState;
        }
        public void LightInput(bool newInteractState)
        {
            light = !light;
        }

        private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}