using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GripControlledSlider : MonoBehaviour
{
    [Header("Slider Reference")]
    [SerializeField] private Slider targetSlider;

    [Header("Speed Settings")]
    [SerializeField] private float changeSpeed = 2.0f;
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 1f, 2f, 3f);
    [SerializeField] private float maxSpeedMultiplier = 3f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftGripActionReference;
    [SerializeField] private InputActionReference rightGripActionReference;

    private bool leftGripPressed = false;
    private bool rightGripPressed = false;
    private float gripHoldTime = 0f;

    void Start()
    {
        if (targetSlider == null)
            targetSlider = GetComponent<Slider>();

        if (targetSlider == null)
        {
            Debug.LogError("No Slider component found!");
            enabled = false;
            return;
        }

        if (leftGripActionReference != null)
            leftGripActionReference.action.Enable();
        if (rightGripActionReference != null)
            rightGripActionReference.action.Enable();
    }

    void Update()
    {
        if (leftGripActionReference == null || rightGripActionReference == null)
            return;

        float leftGripValue = leftGripActionReference.action.ReadValue<float>();
        float rightGripValue = rightGripActionReference.action.ReadValue<float>();

        bool currentLeftGrip = leftGripValue > 0.5f;
        bool currentRightGrip = rightGripValue > 0.5f;

        leftGripPressed = currentLeftGrip;
        rightGripPressed = currentRightGrip;

        // Calculate change direction and update hold time
        float changeDirection = 0f;
        if (leftGripPressed && !rightGripPressed)
        {
            changeDirection = -1f;
            gripHoldTime += Time.deltaTime;
        }
        else if (rightGripPressed && !leftGripPressed)
        {
            changeDirection = 1f;
            gripHoldTime += Time.deltaTime;
        }
        else
        {
            gripHoldTime = 0f; // Reset hold time when no grip or both grips
        }

        if (changeDirection != 0f)
        {
            // Calculate speed multiplier based on hold time
            float speedMultiplier = accelerationCurve.Evaluate(gripHoldTime);
            speedMultiplier = Mathf.Clamp(speedMultiplier, 1f, maxSpeedMultiplier);

            float changeAmount = changeDirection * changeSpeed * speedMultiplier * Time.deltaTime;

            targetSlider.value = Mathf.Clamp(
                targetSlider.value + changeAmount * (targetSlider.maxValue - targetSlider.minValue),
                targetSlider.minValue,
                targetSlider.maxValue
            );
        }
    }

    void OnDestroy()
    {
        if (leftGripActionReference != null)
            leftGripActionReference.action.Disable();
        if (rightGripActionReference != null)
            rightGripActionReference.action.Disable();
    }
}