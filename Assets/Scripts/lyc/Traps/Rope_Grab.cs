using UnityEngine;

public class Rope_Grab : MonoBehaviour
{
    public GameObject rock_rope;
    public GameObject Rock;
    public GameObject This_rope;
    public Vector3 initial_position;
    public rock_fall fallTrigger;

    [Header("Lift limit")]
    [Min(0f), Tooltip("Maximum rock height above its starting position, in world units.")]
    public float maxLiftHeight = 2f;

    private bool isPulling;
    private Vector3 lastPosition;
    private float initialRockY;
    private Vector3 initialRockRopePosition;
    private Vector3 initialRockRopeScale;

    public float LiftedHeight => Mathf.Max(0f, Rock.transform.position.y - initialRockY);

    private void Awake()
    {
        initial_position = This_rope.transform.position;
        initialRockY = Rock.transform.position.y;
        initialRockRopePosition = rock_rope.transform.position;
        initialRockRopeScale = rock_rope.transform.localScale;
    }

    public void Begin_Pull()
    {
        if (fallTrigger != null && !fallTrigger.CanPull)
        {
            isPulling = false;
            return;
        }
        if (fallTrigger != null) fallTrigger.Interrupted();
        lastPosition = This_rope.transform.position;
        isPulling = true;
    }

    public void Update_Pull()
    {
        if (!isPulling) return;
        if (fallTrigger != null && !fallTrigger.CanPull)
        {
            isPulling = false;
            return;
        }
        Vector3 position = This_rope.transform.position;
        float delta = Vector3.Dot(position - lastPosition, Vector3.down);
        MoveRock(delta / 2f);
        lastPosition = position;
    }

    // Shared by pulling and falling so the rope always matches the rock's height.
    public void MoveRock(float distance)
    {
        // The scene rope is a cylinder with a mesh height of two local units.
        float worldUnitsPerScale = rock_rope.transform.TransformVector(Vector3.up).magnitude /
            Mathf.Max(Mathf.Abs(rock_rope.transform.localScale.y), 0.0001f);
        float maxHeight = Mathf.Max(0f, initialRockRopeScale.y * 2f * worldUnitsPerScale - 0.01f);
        maxHeight = Mathf.Min(maxHeight, Mathf.Max(0f, maxLiftHeight));
        float height = Mathf.Clamp(LiftedHeight + distance, 0f, maxHeight);
        Vector3 position = Rock.transform.position;
        position.y = initialRockY + height;
        Rock.transform.position = position;
        rock_rope.transform.position = initialRockRopePosition + Vector3.up * (height / 2f);
        Vector3 scale = initialRockRopeScale;
        scale.y -= height / (2f * Mathf.Max(worldUnitsPerScale, 0.0001f));
        rock_rope.transform.localScale = scale;
    }

    public void End_Pull()
    {
        This_rope.transform.position = initial_position;
        if (!isPulling) return;
        isPulling = false;
        if (fallTrigger != null) fallTrigger.Set_timer();
    }
}
