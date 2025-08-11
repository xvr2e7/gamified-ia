using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SliderControl : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float changeSpeed = 1.0f;
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 1f, 2f, 3f);
    [SerializeField] private float maxSpeedMultiplier = 3f;

    [Header("Trackpad Input Actions")]
    [SerializeField] private InputActionReference leftTrackpadClick;
    [SerializeField] private InputActionReference leftTrackpadPosition;
    [SerializeField] private InputActionReference rightTrackpadClick;
    [SerializeField] private InputActionReference rightTrackpadPosition;

    [Header("Trackpad Settings")]
    [SerializeField] private float horizontalThreshold = 0.3f; // How far left/right to register

    private bool leftSidePressed = false;
    private bool rightSidePressed = false;
    private float holdTime = 0f;

    void Start()
    {
        // Enable all input actions
        if (leftTrackpadClick != null)
            leftTrackpadClick.action.Enable();
        if (leftTrackpadPosition != null)
            leftTrackpadPosition.action.Enable();
        if (rightTrackpadClick != null)
            rightTrackpadClick.action.Enable();
        if (rightTrackpadPosition != null)
            rightTrackpadPosition.action.Enable();
    }

    void Update()
    {
        if (leftTrackpadClick == null || rightTrackpadClick == null ||
            leftTrackpadPosition == null || rightTrackpadPosition == null)
            return;

        // Find the currently active slider in the scene
        Slider activeSlider = FindObjectOfType<Slider>();
        if (activeSlider == null)
            return;

        // Check trackpad states
        bool leftClicked = leftTrackpadClick.action.IsPressed();
        bool rightClicked = rightTrackpadClick.action.IsPressed();

        Vector2 leftPos = leftTrackpadPosition.action.ReadValue<Vector2>();
        Vector2 rightPos = rightTrackpadPosition.action.ReadValue<Vector2>();

        // Determine if left or right side is pressed
        bool currentLeftSide = false;
        bool currentRightSide = false;

        if (leftClicked)
        {
            if (leftPos.x < -horizontalThreshold)
                currentLeftSide = true;
            else if (leftPos.x > horizontalThreshold)
                currentRightSide = true;
        }

        if (rightClicked)
        {
            if (rightPos.x < -horizontalThreshold)
                currentLeftSide = true;
            else if (rightPos.x > horizontalThreshold)
                currentRightSide = true;
        }

        leftSidePressed = currentLeftSide;
        rightSidePressed = currentRightSide;

        // Calculate change direction and update hold time
        float changeDirection = 0f;
        if (leftSidePressed && !rightSidePressed)
        {
            changeDirection = -1f; // Decrease slider
            holdTime += Time.deltaTime;
        }
        else if (rightSidePressed && !leftSidePressed)
        {
            changeDirection = 1f; // Increase slider
            holdTime += Time.deltaTime;
        }
        else
        {
            holdTime = 0f; // Reset hold time when no press or both sides
        }

        if (changeDirection != 0f)
        {
            // Calculate speed multiplier based on hold time
            float speedMultiplier = accelerationCurve.Evaluate(holdTime);
            speedMultiplier = Mathf.Clamp(speedMultiplier, 1f, maxSpeedMultiplier);

            float changeAmount = changeDirection * changeSpeed * speedMultiplier * Time.deltaTime;

            activeSlider.value = Mathf.Clamp(
                activeSlider.value + changeAmount * (activeSlider.maxValue - activeSlider.minValue),
                activeSlider.minValue,
                activeSlider.maxValue
            );
        }
    }

    void OnDestroy()
    {
        // Disable all input actions
        if (leftTrackpadClick != null)
            leftTrackpadClick.action.Disable();
        if (leftTrackpadPosition != null)
            leftTrackpadPosition.action.Disable();
        if (rightTrackpadClick != null)
            rightTrackpadClick.action.Disable();
        if (rightTrackpadPosition != null)
            rightTrackpadPosition.action.Disable();
    }
}