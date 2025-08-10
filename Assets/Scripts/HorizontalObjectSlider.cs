using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls horizontal sliding of objects to display them one at a time
/// </summary>
public class HorizontalObjectSlider : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private List<GameObject> slideObjects = new List<GameObject>();
    [SerializeField] private Transform objectContainer;
    
    // Store original positions of objects
    private List<Vector3> originalPositions = new List<Vector3>();
    
    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 10f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Navigation")]
    [SerializeField] private bool wrapAround = true;
    [SerializeField] private bool startAtFirstObject = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip slideSound;
    [SerializeField] private float volume = 1.0f;
    
    // Private variables
    private int currentIndex = 0;
    private bool isSliding = false;
    private AudioSource audioSource;
    
    void Start()
    {
        // Initialize audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        
        // Store original positions
        StoreOriginalPositions();
        
        // Setup initial positions
        if (slideObjects.Count > 0)
        {
            SetupInitialPositions();
            
            // If not starting at first object, move to desired starting position
            if (!startAtFirstObject && currentIndex != 0)
            {
                SnapToIndex(currentIndex);
            }
        }
        else
        {
            Debug.LogWarning("No slide objects assigned to HorizontalObjectSlider!");
        }
    }
    
    /// <summary>
    /// Stores the original positions of all objects
    /// </summary>
    private void StoreOriginalPositions()
    {
        originalPositions.Clear();
        
        foreach (GameObject obj in slideObjects)
        {
            if (obj != null)
            {
                originalPositions.Add(obj.transform.position);
            }
            else
            {
                originalPositions.Add(Vector3.zero); // Placeholder for null objects
            }
        }
    }
    
    /// <summary>
    /// Sets up the initial positions of all objects in the slider
    /// </summary>
    private void SetupInitialPositions()
    {
        if (objectContainer == null)
        {
            // If no container specified, use this transform as container
            objectContainer = transform;
        }
        
        // Original positions have already been stored, no need to modify them here
        // Just ensure objects are visible according to current index
        UpdateObjectVisibility();
    }
    
    /// <summary>
    /// Updates the visibility of objects based on the current index
    /// </summary>
    private void UpdateObjectVisibility()
    {
        for (int i = 0; i < slideObjects.Count; i++)
        {
            if (slideObjects[i] != null)
            {
                // Only show current object, hide others
                slideObjects[i].SetActive(i == currentIndex);
            }
        }
    }
    
    /// <summary>
    /// Returns the position for a specific object index during animation
    /// </summary>
    private Vector3 GetPositionForIndex(int index)
    {
        if (index >= 0 && index < originalPositions.Count)
        {
            return originalPositions[index];
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Instantly sets the view to the specified index without animation
    /// </summary>
    public void SnapToIndex(int index)
    {
        if (slideObjects.Count == 0) return;
        
        // Validate index
        if (wrapAround)
        {
            // Handle wrap-around for index
            index = (index % slideObjects.Count + slideObjects.Count) % slideObjects.Count;
        }
        else
        {
            // Clamp index to valid range
            index = Mathf.Clamp(index, 0, slideObjects.Count - 1);
        }
        
        // Update current index
        currentIndex = index;
        
        // Update object visibility
        UpdateObjectVisibility();
    }
    
    /// <summary>
    /// Slides to the next object
    /// </summary>
    public void SlideToNext()
    {
        if (isSliding || slideObjects.Count <= 1) return;
        
        int nextIndex = currentIndex + 1;
        if (nextIndex >= slideObjects.Count)
        {
            if (wrapAround)
                nextIndex = 0;
            else
                return; // Can't go past the end if not wrapping
        }
        
        StartCoroutine(SlideToIndex(nextIndex));
    }
    
    /// <summary>
    /// Slides to the previous object
    /// </summary>
    public void SlideToPrevious()
    {
        if (isSliding || slideObjects.Count <= 1) return;
        
        int prevIndex = currentIndex - 1;
        if (prevIndex < 0)
        {
            if (wrapAround)
                prevIndex = slideObjects.Count - 1;
            else
                return; // Can't go before the first if not wrapping
        }
        
        StartCoroutine(SlideToIndex(prevIndex));
    }
    
    /// <summary>
    /// Slides to a specific object index with animation
    /// </summary>
    public void SlideToObject(int index)
    {
        if (isSliding || slideObjects.Count == 0) return;
        
        // Validate index
        if (wrapAround)
        {
            // Handle wrap-around for index
            index = (index % slideObjects.Count + slideObjects.Count) % slideObjects.Count;
        }
        else
        {
            // Clamp index to valid range
            index = Mathf.Clamp(index, 0, slideObjects.Count - 1);
        }
        
        // Don't slide if already at this index
        if (index == currentIndex) return;
        
        StartCoroutine(SlideToIndex(index));
    }
    
    /// <summary>
    /// Coroutine to animate sliding to the target index
    /// </summary>
    private IEnumerator SlideToIndex(int targetIndex)
    {
        // Set sliding flag to prevent multiple slides at once
        isSliding = true;
        
        // Play sound if assigned
        if (slideSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(slideSound, volume);
        }
        
        // Make both objects visible during the transition
        if (currentIndex >= 0 && currentIndex < slideObjects.Count && 
            targetIndex >= 0 && targetIndex < slideObjects.Count)
        {
            if (slideObjects[currentIndex] != null)
                slideObjects[currentIndex].SetActive(true);
                
            if (slideObjects[targetIndex] != null)
                slideObjects[targetIndex].SetActive(true);
            
            // Hide all other objects
            for (int i = 0; i < slideObjects.Count; i++)
            {
                if (i != currentIndex && i != targetIndex && slideObjects[i] != null)
                {
                    slideObjects[i].SetActive(false);
                }
            }
            
            // Get current positions
            Vector3 currentObjPos = slideObjects[currentIndex].transform.position;
            Vector3 targetObjPos = slideObjects[targetIndex].transform.position;
            
            // Determine direction of slide
            bool slideRight = targetIndex > currentIndex;
            float direction = slideRight ? -1 : 1;
            
            // Move current object off screen
            Vector3 currentTargetPos = currentObjPos + new Vector3(direction * slideDistance, 0, 0);
            
            // Move new object on screen
            Vector3 startPos = targetObjPos + new Vector3(-direction * slideDistance, 0, 0);
            slideObjects[targetIndex].transform.position = startPos;
            
            // Animate the movement
            float elapsedTime = 0f;
            while (elapsedTime < slideDuration)
            {
                float normalizedTime = elapsedTime / slideDuration;
                float curveValue = slideCurve.Evaluate(normalizedTime);
                
                // Update positions
                slideObjects[currentIndex].transform.position = Vector3.Lerp(currentObjPos, currentTargetPos, curveValue);
                slideObjects[targetIndex].transform.position = Vector3.Lerp(startPos, targetObjPos, curveValue);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure objects reach their final positions
            slideObjects[currentIndex].transform.position = currentObjPos;
            slideObjects[targetIndex].transform.position = targetObjPos;
            
            // Update current index after the transition
            currentIndex = targetIndex;
            
            // Update object visibility for the final state
            UpdateObjectVisibility();
        }
        
        // Reset sliding flag
        isSliding = false;
    }
    
    /// <summary>
    /// Returns the currently active object
    /// </summary>
    public GameObject GetCurrentObject()
    {
        if (slideObjects.Count == 0 || currentIndex < 0 || currentIndex >= slideObjects.Count)
            return null;
            
        return slideObjects[currentIndex];
    }
    
    /// <summary>
    /// Returns the current index
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentIndex;
    }
    
    /// <summary>
    /// Adds an object to the slider
    /// </summary>
    public void AddObject(GameObject obj)
    {
        if (obj != null)
        {
            slideObjects.Add(obj);
            
            // Position the new object
            int newIndex = slideObjects.Count - 1;
            obj.transform.position = GetPositionForIndex(newIndex);
        }
    }
}
