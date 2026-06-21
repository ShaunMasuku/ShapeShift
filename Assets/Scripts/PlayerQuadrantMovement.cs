using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles snapping player movement between four screen quadrants (Top Left, Top Right, Bottom Left, Bottom Right)
/// on the X/Y plane for a 3D endless runner setup. Z position remains completely constant.
/// </summary>
public class PlayerQuadrantMovement : MonoBehaviour
{
    [Header("Quadrant Settings")]
    [Tooltip("The spacing between the left/right columns and top/bottom rows.")]
    public float spacing = 3.0f;

    [Header("Debug Settings")]
    [Tooltip("If true, the debug quadrant visuals are displayed. If false, they are hidden.")]
    public bool showDebugQuadrants = false;

    [Tooltip("Reference to the GameObject containing debug quadrant visuals (like frames and panels).")]
    public GameObject debugQuadrantVisuals;

    // Logical position indicators
    // Column (X): 0 is Left, 1 is Right
    // Row (Y):    0 is Bottom, 1 is Top
    private int currentColumn = 0; // Starts at Left (0)
    private int currentRow = 0;    // Starts at Bottom (0)

    /// <summary>
    /// Gets the player's current quadrant based on the column and row.
    /// </summary>
    public PlayerQuadrant CurrentQuadrant
    {
        get
        {
            if (currentColumn == 0)
            {
                return (currentRow == 1) ? PlayerQuadrant.TopLeft : PlayerQuadrant.BottomLeft;
            }
            else
            {
                return (currentRow == 1) ? PlayerQuadrant.TopRight : PlayerQuadrant.BottomRight;
            }
        }
    }

    // Reference to the Input System's "Move" action
    private InputAction moveAction;

    // Flags to ensure we only register single discrete taps per press
    private bool wasHorizontalPressed = false;
    private bool wasVerticalPressed = false;

    private void Start()
    {
        // Automatically try to find DebugQuadrantVisuals under Environment if not assigned
        if (debugQuadrantVisuals == null)
        {
            GameObject env = GameObject.Find("GameplayRig/Environment") ?? GameObject.Find("Environment");
            if (env != null)
            {
                Transform visuals = env.transform.Find("DebugQuadrantVisuals");
                if (visuals != null)
                {
                    debugQuadrantVisuals = visuals.gameObject;
                }
            }
        }

        ApplyDebugVisualsState();

        // Bind to the project-wide "Move" action (default WASD/Arrows)
        if (InputSystem.actions != null)
        {
            moveAction = InputSystem.actions.FindAction("Move");
        }

        if (moveAction == null)
        {
            Debug.LogError("PlayerQuadrantMovement: Could not find 'Move' action! Ensure that the Input System is set up correctly in Project Settings.");
        }

        // Snap player to starting position (Bottom Left) at start
        UpdatePlayerPosition();
    }

    private void Update()
    {
        if (moveAction == null) return;

        // Read the movement vector:
        // W/Up Arrow    => Y is positive (move to Top Row)
        // S/Down Arrow  => Y is negative (move to Bottom Row)
        // A/Left Arrow  => X is negative (move to Left Column)
        // D/Right Arrow => X is positive (move to Right Column)
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Handle Horizontal Input (A/D or Left/Right Arrow)
        if (Mathf.Abs(input.x) > 0.1f)
        {
            if (!wasHorizontalPressed)
            {
                if (input.x > 0)
                {
                    currentColumn = 1; // Move to Right Column
                }
                else
                {
                    currentColumn = 0; // Move to Left Column
                }
                wasHorizontalPressed = true;
                UpdatePlayerPosition();
            }
        }
        else
        {
            wasHorizontalPressed = false;
        }

        // Handle Vertical Input (W/S or Up/Down Arrow)
        if (Mathf.Abs(input.y) > 0.1f)
        {
            if (!wasVerticalPressed)
            {
                if (input.y > 0)
                {
                    currentRow = 1; // Move to Top Row
                }
                else
                {
                    currentRow = 0; // Move to Bottom Row
                }
                wasVerticalPressed = true;
                UpdatePlayerPosition();
            }
        }
        else
        {
            wasVerticalPressed = false;
        }
    }

    /// <summary>
    /// Converts the current logical grid position (Column, Row) into a 3D position
    /// on the X/Y plane and updates the player's transform instantly.
    /// </summary>
    private void UpdatePlayerPosition()
    {
        // Map Column to X Coordinate:
        // Column 0 => -spacing / 2f
        // Column 1 => +spacing / 2f
        float targetX = (currentColumn == 0) ? -spacing / 2f : spacing / 2f;

        // Map Row to Y Coordinate:
        // Row 0 => -spacing / 2f
        // Row 1 => +spacing / 2f
        float targetY = (currentRow == 0) ? -spacing / 2f : spacing / 2f;

        // We snap immediately on X and Y, and keep the current Z completely constant.
        transform.position = new Vector3(targetX, targetY, transform.position.z);
    }

    /// <summary>
    /// Updates the active state of the debug quadrant visuals based on showDebugQuadrants.
    /// </summary>
    public void ApplyDebugVisualsState()
    {
        if (debugQuadrantVisuals != null)
        {
            debugQuadrantVisuals.SetActive(showDebugQuadrants);
        }
    }

    private void OnValidate()
    {
        ApplyDebugVisualsState();
    }
}
