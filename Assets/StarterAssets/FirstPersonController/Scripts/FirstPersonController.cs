using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 2.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		//SFX
		public bool walking;
		public bool running;
		bool prevWalkingstate;
		bool prevRunningstate;
		float walkTimer;
		float runTimer;
		bool startWalkTimer;
		bool startRunTimer;
		bool prevGrounded;
        public float soundOutput;
		public bool jumpDetection;
		bool startedStopJumpDetection;

        [SerializeField] CinemachineVirtualCamera cmvc;
		public Vector3 crouchVector;
		public float crouchYFloat;
	
#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		public StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		public bool canSprint;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
        }

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

            prevWalkingstate = walking;
            prevRunningstate = running;
			prevGrounded = Grounded;
			soundOutput = 0;
			jumpDetection = false;
			SoundManager.instance.cmvc = cmvc;
        }

		private void Update()
		{
			if(SoundManager.instance.gameState == GameState.Freeroam)
			{
                JumpAndGravity();
                GroundedCheck();
                Move();
                Crouch();
                SFX();
            }
			else if(SoundManager.instance.gameState == GameState.Hiding)
			{
				var cam = SoundManager.instance.hidingspot.cameraObj;
				if(cam != null)
				{
					if(_input.look.x < 0 && (cam.transform.eulerAngles.y > (SoundManager.instance.hidingSpotCamDefaultYRot - 20)))
					{
                        cam.transform.Rotate(Vector3.up * -75 * Time.deltaTime);
                    }
                    if (_input.look.x > 0 && (cam.transform.eulerAngles.y < (SoundManager.instance.hidingSpotCamDefaultYRot + 20)))
                    {
                        cam.transform.Rotate(Vector3.up * 75 * Time.deltaTime);
                    }
				}
			}
			if (soundOutput != 0)
			{
				soundOutput = 0;
			}
			if (jumpDetection && !startedStopJumpDetection)
				StartCoroutine(StopJumpDetection());

			if(_input.move.y <= 0)
				canSprint = false;
			else
				canSprint = true;

            Debug.DrawRay(_mainCamera.transform.position, _mainCamera.transform.forward * 3, Color.cyan);
            RaycastHit hit;
            SoundManager.instance.currentInteractable = null;
            if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out hit, 3))
			{
                if (hit.collider != null)
				{
					var interactable = hit.collider.gameObject.GetComponent<Interactable>();
					if(interactable != null)
					{
						SoundManager.instance.currentInteractable = interactable;
                    }
				}
			};
        }
		IEnumerator StopJumpDetection()
		{
			startedStopJumpDetection = true;
			yield return new WaitForSeconds(0.5f);
            jumpDetection = false;
			startedStopJumpDetection = false;
        }
		private void LateUpdate()
		{
			if(SoundManager.instance.gameState == GameState.Freeroam)
				CameraRotation();

            var thirdPersonFollow = cmvc.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            thirdPersonFollow.ShoulderOffset = crouchVector;
        }

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed;
			if (!_input.crouch)
			{
				if(canSprint)
					targetSpeed = _input.sprint ? SprintSpeed : 2;
                else
                    targetSpeed = 2;
            }
			else
				targetSpeed = 1;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }
		void Crouch()
		{
			SoundManager.instance.crouched = _input.crouch;
            crouchVector = new Vector3(0.0f, crouchYFloat, 0.0f);
            if (_input.crouch && crouchYFloat > -0.5)
                crouchYFloat -= Time.deltaTime * 2;
            if (!_input.crouch && crouchYFloat < 0)
                crouchYFloat += Time.deltaTime * 2;
            if (_input.crouch && crouchYFloat < -0.5)
                crouchYFloat = -0.5f;
            if (!_input.crouch && crouchYFloat > 0)
                crouchYFloat = 0;
        }
		void SFX()
		{
            if (Grounded)
            {
                if (_speed > 0)
                {
                    if (_speed <= 4)
                    {
                        walking = true;
                        running = false;
                    }
                    else
                    {
                        walking = false;
                        running = true;
                    }
                }
                else
                {
                    walking = false;
                    running = false;
                }
            }
            else
            {
                walking = false;
                running = false;
            }

            if (prevWalkingstate != walking)
            {
				CheckSFX();
                prevWalkingstate = walking;
            }
            if (prevRunningstate != running)
            {
				CheckSFX();
                prevRunningstate = running;
            }
			RunSFX();
        }
		void CheckSFX()
		{
			if(walking)
			{
				startWalkTimer = true;
				startRunTimer = false;
			}
			else if(running)
			{
                startWalkTimer = false;
                startRunTimer = true;
            }
			else
			{
				startWalkTimer = false;
				startRunTimer = false;
            }
		}
		void OutputSound(float volume)
		{
			soundOutput = volume;
		}
		void RunSFX()
		{
			if (startWalkTimer && !_input.crouch)
				walkTimer += Time.deltaTime * _speed * 2;
			else if(startWalkTimer && _input.crouch)
                walkTimer += Time.deltaTime * _speed * 2;
            else
				walkTimer = 0;
			if (startRunTimer)
				runTimer += Time.deltaTime * _speed;
			else
				runTimer = 0;

			if(walkTimer > 3)
			{
				SoundManager.instance.PlayFootstepWalk();
                if (_input.crouch) 
					OutputSound(0.5f);
				else
                    OutputSound(1); 
				walkTimer = 0;
			}	
			if(runTimer > 2)
			{
                SoundManager.instance.PlayFootstepRun();
                OutputSound(2);
                runTimer = 0;
			}
			if(prevGrounded != Grounded)
			{
				if (Grounded)
				{
                    SoundManager.instance.PlayJumpSFX();
					jumpDetection = true;
                    OutputSound(2);
                }
				prevGrounded = Grounded;
			}
		}
		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}