using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// States for the TestWall obstacle.
/// </summary>
public enum WallState
{
    Approaching,
    Passed,
    Failed
}

/// <summary>
/// Controls a single test wall obstacle, moving it toward the player on the Z axis,
/// evaluating if the player has the correct shape and is in the correct quadrant
/// when the wall passes the player's position, and continuing past the player on success.
/// </summary>
public class TestWallController : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("The shape the player must be when the wall passes.")]
    public PlayerShapeType requiredShape = PlayerShapeType.Cube;

    [Tooltip("The quadrant the player must occupy when the wall passes.")]
    public PlayerQuadrant requiredQuadrant = PlayerQuadrant.TopRight;

    [Header("Movement")]
    [Tooltip("How fast the wall moves along the Z axis (negative Z direction).")]
    public float moveSpeed = 5.0f;

    [Tooltip("The starting Z position when the wall is spawned or reset.")]
    public float startZ = 25.0f;

    [Tooltip("The Z position where the player is checked. Usually 0 (where the player is located).")]
    public float checkZ = 0.0f;

    [Tooltip("The Z position behind the player where the wall stops and hides.")]
    public float endZ = -10.0f;

    [Header("Visual Colors")]
    [Tooltip("The neutral wall color (dark grey).")]
    public Color neutralColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);

    [Tooltip("The color the wall flashes briefly upon a successful pass.")]
    public Color passFlashColor = new Color(0.1f, 0.8f, 0.1f, 1.0f);

    [Tooltip("The color the wall turns permanently upon failure.")]
    public Color failColor = new Color(0.8f, 0.1f, 0.1f, 1.0f);

    [Tooltip("How long the success flash stays bright green before fading back to neutral.")]
    public float passFlashDuration = 0.6f;

    [Header("Cutout Indicator Settings")]
    [Tooltip("Base transparent material for the cutout indicator (e.g. TransparentQuadrant).")]
    public Material transparentMaterial;

    [Tooltip("Color of the cutout indicator guide.")]
    public Color cutoutIndicatorColor = new Color(0.9f, 0.9f, 0.9f, 0.4f);

    [Tooltip("How much larger the cutout indicator is compared to the player.")]
    public float cutoutScaleMultiplier = 1.2f;

    [Header("References")]
    [Tooltip("Reference to the player GameObject (containing quadrant and shape switcher scripts).")]
    public GameObject playerReference;

    [Tooltip("Reference to the child GameObject representing the cutout indicator parent.")]
    public GameObject indicatorParent;

    [Header("Indicator Child Visuals")]
    public GameObject squareIndicator;
    public GameObject circleIndicator;
    public GameObject triangleIndicator;

    private MeshRenderer wallRenderer;
    private Material wallMaterialInstance;
    private Material indicatorMaterialInstance;
    
    private WallState currentState = WallState.Approaching;
    private float stateTransitionTime = 0.0f;

    private void Start()
    {
        wallRenderer = GetComponent<MeshRenderer>();
        if (wallRenderer != null)
        {
            // Instantiate to prevent modifying the project's shared material asset
            wallMaterialInstance = new Material(wallRenderer.sharedMaterial);
            wallRenderer.sharedMaterial = wallMaterialInstance;
            wallMaterialInstance.color = neutralColor;
        }

        // Try to automatically find indicator parent if not assigned
        if (indicatorParent == null)
        {
            indicatorParent = transform.Find("WallCutoutIndicator_TopRight")?.gameObject;
            if (indicatorParent == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("Indicator"))
                    {
                        indicatorParent = child.gameObject;
                        break;
                    }
                }
            }
        }

        // Setup the indicators and scale them correctly
        UpdateIndicatorVisuals();

        // Snap wall to starting Z position
        transform.position = new Vector3(transform.position.x, transform.position.y, startZ);
        currentState = WallState.Approaching;
    }

    private void Update()
    {
        // Check for manual reset key 'R'
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetWall();
        }

        switch (currentState)
        {
            case WallState.Approaching:
                // Move wall toward player (negative Z direction)
                transform.Translate(0, 0, -moveSpeed * Time.deltaTime);

                // Once it reaches or passes checkZ, perform evaluation
                if (transform.position.z <= checkZ)
                {
                    EvaluateWall();
                }
                break;

            case WallState.Passed:
                // Continue moving past the player
                transform.Translate(0, 0, -moveSpeed * Time.deltaTime);

                // Smoothly fade the success green color back to neutral wall color
                stateTransitionTime += Time.deltaTime;
                float t = Mathf.Clamp01(stateTransitionTime / passFlashDuration);
                if (wallMaterialInstance != null)
                {
                    wallMaterialInstance.color = Color.Lerp(passFlashColor, neutralColor, t);
                }

                // Once wall passes endZ, hide it off-screen
                if (transform.position.z <= endZ)
                {
                    SetVisualsActive(false);
                }
                break;

            case WallState.Failed:
                // Stop movement for debugging
                break;
        }
    }

    /// <summary>
    /// Checks if the player's shape and quadrant match the requirements.
    /// </summary>
    private void EvaluateWall()
    {
        if (playerReference == null)
        {
            Debug.LogError("TestWallController: Player reference is missing! Cannot evaluate result.");
            currentState = WallState.Failed;
            return;
        }

        PlayerQuadrantMovement quadMovement = playerReference.GetComponent<PlayerQuadrantMovement>();
        PlayerShapeSwitcher shapeSwitcher = playerReference.GetComponent<PlayerShapeSwitcher>();

        if (quadMovement == null || shapeSwitcher == null)
        {
            Debug.LogError("TestWallController: Player components missing!");
            currentState = WallState.Failed;
            return;
        }

        PlayerQuadrant playerQuadrant = quadMovement.CurrentQuadrant;
        PlayerShapeType playerShape = shapeSwitcher.CurrentShape;

        bool quadrantMatch = playerQuadrant == requiredQuadrant;
        bool shapeMatch = playerShape == requiredShape;

        if (quadrantMatch && shapeMatch)
        {
            Debug.Log($"PASS: Player matched {requiredShape} in {requiredQuadrant}.");
            currentState = WallState.Passed;
            stateTransitionTime = 0.0f;
            if (wallMaterialInstance != null)
            {
                wallMaterialInstance.color = passFlashColor;
            }
        }
        else
        {
            Debug.Log($"FAIL: Required {requiredShape} in {requiredQuadrant}, but player was {playerShape} in {playerQuadrant}.");
            currentState = WallState.Failed;
            if (wallMaterialInstance != null)
            {
                wallMaterialInstance.color = failColor;
            }
            // Clamp precisely to checkZ on fail for clear visual alignment
            transform.position = new Vector3(transform.position.x, transform.position.y, checkZ);
        }
    }

    /// <summary>
    /// Resets the wall to start position and re-enables approaching state.
    /// </summary>
    public void ResetWall()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, startZ);
        currentState = WallState.Approaching;
        stateTransitionTime = 0.0f;

        if (wallMaterialInstance != null)
        {
            wallMaterialInstance.color = neutralColor;
        }

        SetVisualsActive(true);
        UpdateIndicatorVisuals();

        Debug.Log($"Test Wall reset. Moving from Z = {startZ}. Match required: {requiredShape} in {requiredQuadrant}.");
    }

    /// <summary>
    /// Controls showing/hiding the wall meshes cleanly.
    /// </summary>
    private void SetVisualsActive(bool active)
    {
        if (wallRenderer != null)
        {
            wallRenderer.enabled = active;
        }
        if (indicatorParent != null)
        {
            indicatorParent.SetActive(active);
        }
    }

    /// <summary>
    /// Positions and scales indicators, assigning clean semi-transparent materials.
    /// </summary>
    private void UpdateIndicatorVisuals()
    {
        if (indicatorParent == null) return;

        // Position the indicator parent based on quadrant
        float spacing = 3.0f;
        float worldX = 0f;
        float worldY = 0f;

        switch (requiredQuadrant)
        {
            case PlayerQuadrant.TopLeft:
                worldX = -spacing / 2f;
                worldY = spacing / 2f;
                break;
            case PlayerQuadrant.TopRight:
                worldX = spacing / 2f;
                worldY = spacing / 2f;
                break;
            case PlayerQuadrant.BottomLeft:
                worldX = -spacing / 2f;
                worldY = -spacing / 2f;
                break;
            case PlayerQuadrant.BottomRight:
                worldX = spacing / 2f;
                worldY = -spacing / 2f;
                break;
        }

        float localX = worldX / transform.localScale.x;
        float localY = worldY / transform.localScale.y;
        indicatorParent.transform.localPosition = new Vector3(localX, localY, -0.6f);

        // Apply 1.2x player-size scaling in world coordinates
        float playerSize = 1.1f;
        if (playerReference != null)
        {
            playerSize = playerReference.transform.localScale.x;
        }
        float targetWorldSize = playerSize * cutoutScaleMultiplier;
        
        float localScaleX = targetWorldSize / transform.localScale.x;
        float localScaleY = targetWorldSize / transform.localScale.y;
        float localScaleZ = 0.2f;
        
        indicatorParent.transform.localScale = new Vector3(localScaleX, localScaleY, localScaleZ);

        SetupIndicatorChildShapes();

        if (squareIndicator != null)
            squareIndicator.SetActive(requiredShape == PlayerShapeType.Cube);
        if (circleIndicator != null)
            circleIndicator.SetActive(requiredShape == PlayerShapeType.Sphere);
        if (triangleIndicator != null)
            triangleIndicator.SetActive(requiredShape == PlayerShapeType.Pyramid);
    }

    private void SetupIndicatorChildShapes()
    {
        if (indicatorParent == null) return;

        if (squareIndicator == null)
        {
            Transform t = indicatorParent.transform.Find("Square");
            if (t != null) squareIndicator = t.gameObject;
        }
        if (circleIndicator == null)
        {
            Transform t = indicatorParent.transform.Find("Circle");
            if (t != null) circleIndicator = t.gameObject;
        }
        if (triangleIndicator == null)
        {
            Transform t = indicatorParent.transform.Find("Triangle");
            if (t != null) triangleIndicator = t.gameObject;
        }

        // Get material
        Material indicatorMaterial = null;
        if (transparentMaterial != null)
        {
            if (indicatorMaterialInstance == null)
            {
                indicatorMaterialInstance = new Material(transparentMaterial);
            }
            indicatorMaterialInstance.color = cutoutIndicatorColor;
            indicatorMaterial = indicatorMaterialInstance;
        }
        else
        {
            MeshRenderer parentRenderer = indicatorParent.GetComponent<MeshRenderer>();
            if (parentRenderer != null)
            {
                indicatorMaterial = parentRenderer.sharedMaterial;
            }
        }

        // Hide main parent block
        MeshRenderer mainParentRenderer = indicatorParent.GetComponent<MeshRenderer>();
        if (mainParentRenderer != null)
        {
            mainParentRenderer.enabled = false;
        }

        if (squareIndicator == null)
        {
            squareIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            squareIndicator.name = "Square";
            squareIndicator.transform.SetParent(indicatorParent.transform, false);
            squareIndicator.transform.localPosition = Vector3.zero;
            squareIndicator.transform.localScale = new Vector3(1.0f, 1.0f, 0.1f);
            if (indicatorMaterial != null)
                squareIndicator.GetComponent<MeshRenderer>().sharedMaterial = indicatorMaterial;
            RemoveCollider(squareIndicator);
        }

        if (circleIndicator == null)
        {
            circleIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            circleIndicator.name = "Circle";
            circleIndicator.transform.SetParent(indicatorParent.transform, false);
            circleIndicator.transform.localPosition = Vector3.zero;
            circleIndicator.transform.localScale = new Vector3(1.0f, 1.0f, 0.1f);
            if (indicatorMaterial != null)
                circleIndicator.GetComponent<MeshRenderer>().sharedMaterial = indicatorMaterial;
            RemoveCollider(circleIndicator);
        }

        if (triangleIndicator == null)
        {
            triangleIndicator = new GameObject("Triangle");
            triangleIndicator.name = "Triangle";
            triangleIndicator.transform.SetParent(indicatorParent.transform, false);
            triangleIndicator.transform.localPosition = Vector3.zero;
            triangleIndicator.transform.localScale = Vector3.one;

            MeshFilter mf = triangleIndicator.AddComponent<MeshFilter>();
            MeshRenderer mr = triangleIndicator.AddComponent<MeshRenderer>();

            mf.sharedMesh = CreatePyramidMesh();
            if (indicatorMaterial != null)
                mr.sharedMaterial = indicatorMaterial;
        }
    }

    private void RemoveCollider(GameObject obj)
    {
        if (obj == null) return;
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                col.enabled = false;
            }
        }
    }

    private Mesh CreatePyramidMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralIndicatorPyramid";

        float h = 0.5f;
        Vector3 apex = new Vector3(0, h, 0);
        Vector3 base0 = new Vector3(-h, -h, -h);
        Vector3 base1 = new Vector3(h, -h, -h);
        Vector3 base2 = new Vector3(h, -h, h);
        Vector3 base3 = new Vector3(-h, -h, h);

        Vector3[] vertices = new Vector3[]
        {
            base3, base2, apex,
            base2, base1, apex,
            base1, base0, apex,
            base0, base3, apex,
            base0, base1, base2, base3
        };

        int[] triangles = new int[]
        {
            0, 1, 2,
            3, 4, 5,
            6, 7, 8,
            9, 10, 11,
            12, 14, 13,
            12, 15, 14
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (wallRenderer == null) wallRenderer = GetComponent<MeshRenderer>();
        UpdateIndicatorVisuals();
    }
}
