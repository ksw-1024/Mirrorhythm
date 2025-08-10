using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Example controller for demonstrating the HorizontalObjectSlider functionality
/// </summary>
public class SliderDemoController : MonoBehaviour
{
    [SerializeField] private HorizontalObjectSlider objectSlider;
    
    [Header("UI Controls (Optional)")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button[] directSelectButtons;
    
    [Header("Input Settings")]
    [SerializeField] private bool useSwipeInput = true;
    [SerializeField] private float minSwipeDistance = 50f;
    
    // Touch detection variables
    private Vector2 touchStartPosition;
    private bool isTouching = false;
    
    private void Start()
    {
        // Set up button listeners if assigned
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);
            
        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousButtonClicked);
            
        // Set up direct selection buttons
        for (int i = 0; i < directSelectButtons.Length; i++)
        {
            int index = i; // Capture index for lambda
            if (directSelectButtons[i] != null)
                directSelectButtons[i].onClick.AddListener(() => OnDirectSelectButtonClicked(index));
        }
    }
    
    private Keyboard keyboard;
    
    private void Awake()
    {
        // Get keyboard device
        keyboard = Keyboard.current;
        
        // Enable enhanced touch support if using swipe
        if (useSwipeInput)
            EnhancedTouchSupport.Enable();
    }
    
    private void Update()
    {
        // Handle keyboard input for testing
        if (keyboard != null)
        {
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            {
                objectSlider.SlideToNext();
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            {
                objectSlider.SlideToPrevious();
            }
        }
        
        // Handle touch/swipe input if enabled
        if (useSwipeInput)
            HandleSwipeInput();
    }
    
    /// <summary>
    /// Handles swipe detection for mobile
    /// </summary>
    private void HandleSwipeInput()
    {
        // For mobile devices with enhanced touch
        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];
            
            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    touchStartPosition = touch.screenPosition;
                    isTouching = true;
                    break;
                    
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (isTouching)
                    {
                        Vector2 swipeDelta = touch.screenPosition - touchStartPosition;
                        
                        // Check if swipe distance is enough to count as a swipe
                        if (Mathf.Abs(swipeDelta.x) > minSwipeDistance)
                        {
                            if (swipeDelta.x > 0)
                            {
                                // Swipe right
                                objectSlider.SlideToPrevious();
                            }
                            else
                            {
                                // Swipe left
                                objectSlider.SlideToNext();
                            }
                        }
                        
                        isTouching = false;
                    }
                    break;
            }
        }
        // For mouse input (editor testing)
        else
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    touchStartPosition = mouse.position.ReadValue();
                    isTouching = true;
                }
                else if (mouse.leftButton.wasReleasedThisFrame && isTouching)
                {
                    Vector2 swipeDelta = mouse.position.ReadValue() - touchStartPosition;
                    
                    // Check if swipe distance is enough to count as a swipe
                    if (Mathf.Abs(swipeDelta.x) > minSwipeDistance)
                    {
                        if (swipeDelta.x > 0)
                        {
                            // Swipe right
                            objectSlider.SlideToPrevious();
                        }
                        else
                        {
                            // Swipe left
                            objectSlider.SlideToNext();
                        }
                    }
                    
                    isTouching = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Handler for Next button click
    /// </summary>
    private void OnNextButtonClicked()
    {
        objectSlider.SlideToNext();
    }
    
    /// <summary>
    /// Handler for Previous button click
    /// </summary>
    private void OnPreviousButtonClicked()
    {
        objectSlider.SlideToPrevious();
    }
    
    /// <summary>
    /// Handler for direct selection buttons
    /// </summary>
    private void OnDirectSelectButtonClicked(int index)
    {
        objectSlider.SlideToObject(index);
    }
}
