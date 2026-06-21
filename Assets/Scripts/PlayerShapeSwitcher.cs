using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Defines the three possible shapes the player can shift into.
/// </summary>
public enum PlayerShapeType
{
    Cube,
    Sphere,
    Pyramid
}

/// <summary>
/// A beginner-friendly script that allows the player to cycle between three shapes:
/// Cube, Sphere, and Pyramid by pressing the Spacebar.
/// </summary>
public class PlayerShapeSwitcher : MonoBehaviour
{
    [Header("Active Shape")]
    [Tooltip("The current shape of the player.")]
    public PlayerShapeType currentShape = PlayerShapeType.Cube;

    /// <summary>
    /// Gets the player's current active shape.
    /// </summary>
    public PlayerShapeType CurrentShape => currentShape;

    [Header("Visual Child References")]
    public GameObject visualCube;
    public GameObject visualSphere;
    public GameObject visualPyramid;

    private void Start()
    {
        // Ensure that our shapes are properly configured and created
        SetupShapes();

        // Initialize the starting visual state
        ApplyShapeState();
    }

    private void Update()
    {
        // Listen to the Spacebar key using the New Input System.
        // Keyboard.current represents the active keyboard device.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CycleShape();
        }
    }

    /// <summary>
    /// Cycles to the next shape in the sequence: Cube -> Sphere -> Pyramid -> Cube
    /// </summary>
    public void CycleShape()
    {
        switch (currentShape)
        {
            case PlayerShapeType.Cube:
                currentShape = PlayerShapeType.Sphere;
                break;
            case PlayerShapeType.Sphere:
                currentShape = PlayerShapeType.Pyramid;
                break;
            case PlayerShapeType.Pyramid:
                currentShape = PlayerShapeType.Cube;
                break;
        }

        // Print the active shape to the Unity Console
        Debug.Log("Shape Shifted! Current Shape: " + currentShape);

        // Apply the changes to our child GameObjects
        ApplyShapeState();
    }

    /// <summary>
    /// Enables only the visual GameObject corresponding to our current shape,
    /// while disabling the other two.
    /// </summary>
    private void ApplyShapeState()
    {
        if (visualCube != null)
            visualCube.SetActive(currentShape == PlayerShapeType.Cube);

        if (visualSphere != null)
            visualSphere.SetActive(currentShape == PlayerShapeType.Sphere);

        if (visualPyramid != null)
            visualPyramid.SetActive(currentShape == PlayerShapeType.Pyramid);
    }

    /// <summary>
    /// Safely references the visual child objects or creates/configures them if needed.
    /// </summary>
    private void SetupShapes()
    {
        // 1. If we didn't assign the references manually in the inspector,
        // let's try to find them by name under our children.
        if (visualCube == null) visualCube = transform.Find("Visual_Cube")?.gameObject;
        if (visualSphere == null) visualSphere = transform.Find("Visual_Sphere")?.gameObject;
        if (visualPyramid == null) visualPyramid = transform.Find("Visual_Pyramid")?.gameObject;

        // 2. If the visualPyramid is found, let's verify if it has a Mesh assigned.
        // If not, we generate a beautiful custom 3D pyramid mesh right here!
        if (visualPyramid != null)
        {
            MeshFilter meshFilter = visualPyramid.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = CreatePyramidMesh();
            }
        }
    }

    /// <summary>
    /// Procedurally builds a perfect 4-sided pyramid mesh with flat shading normals.
    /// </summary>
    private Mesh CreatePyramidMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralPyramid";

        // Coordinates for a unit pyramid centered around (0,0,0)
        float h = 0.5f; // half width
        
        // Vertices representing the 5 corners:
        Vector3 apex = new Vector3(0, h, 0);
        Vector3 base0 = new Vector3(-h, -h, -h); // Bottom-Back-Left
        Vector3 base1 = new Vector3(h, -h, -h);  // Bottom-Back-Right
        Vector3 base2 = new Vector3(h, -h, h);   // Bottom-Front-Right
        Vector3 base3 = new Vector3(-h, -h, h);  // Bottom-Front-Left

        // To achieve clean flat shading (sharp edges), we must duplicate
        // the vertices so each face has its own set of normals.
        Vector3[] vertices = new Vector3[]
        {
            // Front Face (base3, base2, apex)
            base3, base2, apex,
            // Right Face (base2, base1, apex)
            base2, base1, apex,
            // Back Face (base1, base0, apex)
            base1, base0, apex,
            // Left Face (base0, base3, apex)
            base0, base3, apex,
            // Bottom Face (base0, base1, base2, base3)
            base0, base1, base2, base3
        };

        // Triangles array defines the order to connect vertices (clockwise is front-facing)
        int[] triangles = new int[]
        {
            // Front Face
            0, 1, 2,
            // Right Face
            3, 4, 5,
            // Back Face
            6, 7, 8,
            // Left Face
            9, 10, 11,
            // Bottom Face (made of two triangles)
            12, 14, 13,
            12, 15, 14
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        // Let Unity calculate correct lighting normals and bounds automatically
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
