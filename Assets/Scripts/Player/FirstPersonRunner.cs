using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonRunner : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _forwardSpeed;
    [SerializeField] private float _laneDistance;
    [SerializeField] private float _laneChangeSpeed;

    [Header("Jumping and Gravity")]
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _gravity;

    [Header("Swipe Settings")]
    [SerializeField] private float _minimumSwipeDistance = 50f;

    [Header("UI References")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TMP_Text _coinsText;

    [Header("Difficulty Curve")]
    [SerializeField] private float _maxSpeed = 40f;          
    [SerializeField] private float _speedIncreaseRate = 0.2f;

    [Header("Camera Effects")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _baseFOV = 80f;   
    [SerializeField] private float _maxFOV = 110f; 

    [Header("Rolling Mechanics")]
    [SerializeField] private float _rollDuration = 0.8f;
    [SerializeField] private float _rollHeight = 0.5f;        // The collider height when rolling
    [SerializeField] private float _rollCameraY = 0.2f;       // Where the camera drops to
    
    private float normalHeight;            // To remember original height
    private float normalCameraY;           // To remember original camera position
    private bool isRolling = false;
    private float rollTimer = 0f;  

    private float startingSpeed;


    private bool isDead = false;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector2 startSwipePosition;
    private Vector2 endSwipePosition;
    private int currentLane = 1; //0 = Left, 1 = Mid, 2 = Right
    private int coins = 0;

    private PlayerControls controls;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();

        startingSpeed = _forwardSpeed;

        normalHeight = controller.height;
        if (_playerCamera != null) normalCameraY = _playerCamera.transform.localPosition.y;

        controls.Player.MoveLeft.performed += ctx => MoveLane(-1);
        controls.Player.MoveRight.performed += ctx => MoveLane(1);
        controls.Player.Jump.performed += ctx => Jump();
        controls.Player.Roll.performed += ctx => Roll();

        controls.Player.TouchPress.started += ctx => StartSwipe();
        controls.Player.TouchPress.canceled += ctx => EndSwipe();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void StartSwipe()
    {
        startSwipePosition = controls.Player.TouchPosition.ReadValue<Vector2>();
    }

    private void EndSwipe()
    {
        endSwipePosition = controls.Player.TouchPosition.ReadValue<Vector2>();
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        if(isDead) return;

        Vector2 swipeDelta = endSwipePosition - startSwipePosition;

        if (swipeDelta.magnitude > _minimumSwipeDistance)
        {
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                // Horizontal Swipe
                if (swipeDelta.x > 0)
                    MoveLane(1);  // Swiped Right
                else
                    MoveLane(-1); // Swiped Left
            }
            else
            {
                // Vertical Swipe
                if (swipeDelta.y > 0)
                    Jump(); 
                
                else if(swipeDelta.y < 0)
                {
                    Roll();
                }
            }
        }
    }

    private void MoveLane (int direction)
    {
        if(isDead) return;

        currentLane += direction;
        currentLane = Mathf.Clamp(currentLane, 0, 2);
    }

    private void Jump()
    {
        if(isDead) return;

        if (controller.isGrounded)
        {
            if(isRolling) StopRoll();

            velocity.y = Mathf.Sqrt(_jumpForce * -2f * _gravity);
        }
    }

    private void Roll()
    {
        if (isDead || isRolling) return;

        isRolling = true;
        rollTimer = _rollDuration;

        // Roll when in air
        if (!controller.isGrounded)
        {
            velocity.y = -15f; 
        }

        controller.height = _rollHeight;
        controller.center = new Vector3(0, _rollHeight / 2f, 0);

        if (_playerCamera != null)
        {
            Vector3 camPos = _playerCamera.transform.localPosition;
            camPos.y = _rollCameraY;
            _playerCamera.transform.localPosition = camPos;
        }
    }

    private void StopRoll()
    {
        isRolling = false;

        controller.height = normalHeight;
        controller.center = new Vector3(0, 0, 0);

        if (_playerCamera != null)
        {
            Vector3 camPos = _playerCamera.transform.localPosition;
            camPos.y = normalCameraY;
            _playerCamera.transform.localPosition = camPos;
        }
    }
    void Update()
    {
        if(isDead) return;

        if (_forwardSpeed < _maxSpeed)
        {
            _forwardSpeed += _speedIncreaseRate * Time.deltaTime; 
        }

        UpdateCameraFOV();

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
            {
                StopRoll();
            }
        }

        MovePlayer();
    }

    private void MovePlayer()
    {
        float targetX = (currentLane - 1) * _laneDistance;

        Vector3 moveVector = Vector3.zero;
        moveVector.x = (targetX - transform.position.x) * _laneChangeSpeed;
        moveVector.z = _forwardSpeed;

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        velocity.y += _gravity * Time.deltaTime;
        moveVector.y = velocity.y;

        controller.Move(moveVector * Time.deltaTime);
    }

    

    private void Crash()
    {
        if (isDead) return; 
        
        isDead = true;


        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
        }
        //Pause the game
        Time.timeScale = 0f; 

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        // Unpause the game before reloading
        Time.timeScale = 1f; 

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Crash();
        }
        else if (other.CompareTag("Collectable"))
        {
            CollectCoin(other.gameObject);
        }
    }

    private void CollectCoin( GameObject coin)
    {
        coins ++;

        if (_coinsText != null)
        {
            _coinsText.text = "COINS: " + coins.ToString();
        }

        Destroy(coin);
    }

    private void UpdateCameraFOV()
    {
        if (_playerCamera == null) return;

        // Calculating how close we are to max speed
        float speedPercentage = Mathf.InverseLerp(startingSpeed, _maxSpeed, _forwardSpeed);
        
        // Map that percentage to our FOV range
        float targetFOV = Mathf.Lerp(_baseFOV, _maxFOV, speedPercentage);
        
        // Smoothly transition the actual camera FOV so it doesn't jitter
        _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, targetFOV, Time.deltaTime * 5f);
    }
}
