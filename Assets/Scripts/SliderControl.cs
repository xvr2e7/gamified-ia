using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SliderControl : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float changeSpeedPerSecond = 0.2f;

    [Header("Trackpad Input Actions")]
    [SerializeField] private InputActionReference leftTrackpadClick;
    [SerializeField] private InputActionReference leftTrackpadPosition;
    [SerializeField] private InputActionReference rightTrackpadClick;
    [SerializeField] private InputActionReference rightTrackpadPosition;

    [Header("Trackpad Settings")]
    [SerializeField] private float horizontalThreshold = 0.3f;

    private bool leftSidePressed = false;
    private bool rightSidePressed = false;

    void Start()
    {
        if (leftTrackpadClick != null) leftTrackpadClick.action.Enable();
        if (leftTrackpadPosition != null) leftTrackpadPosition.action.Enable();
        if (rightTrackpadClick != null) rightTrackpadClick.action.Enable();
        if (rightTrackpadPosition != null) rightTrackpadPosition.action.Enable();
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
            if (leftPos.x < -horizontalThreshold) currentLeftSide = true;
            else if (leftPos.x > horizontalThreshold) currentRightSide = true;
        }

        if (rightClicked)
        {
            if (rightPos.x < -horizontalThreshold) currentLeftSide = true;
            else if (rightPos.x > horizontalThreshold) currentRightSide = true;
        }

        leftSidePressed = currentLeftSide;
        rightSidePressed = currentRightSide;

        float changeDirection = 0f;
        if (leftSidePressed && !rightSidePressed) changeDirection = -1f; // Decrease
        else if (rightSidePressed && !leftSidePressed) changeDirection = 1f; // Increase

        if (changeDirection != 0f)
        {
            float range = activeSlider.maxValue - activeSlider.minValue;
            float changeAmount = changeDirection * changeSpeedPerSecond * Time.deltaTime * range;

            activeSlider.value = Mathf.Clamp(
                activeSlider.value + changeAmount,
                activeSlider.minValue,
                activeSlider.maxValue
            );
        }
    }

    void OnDestroy()
    {
        if (leftTrackpadClick != null) leftTrackpadClick.action.Disable();
        if (leftTrackpadPosition != null) leftTrackpadPosition.action.Disable();
        if (rightTrackpadClick != null) rightTrackpadClick.action.Disable();
        if (rightTrackpadPosition != null) rightTrackpadPosition.action.Disable();
    }
}