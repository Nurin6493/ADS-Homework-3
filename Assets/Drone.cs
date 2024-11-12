using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Drone : MonoBehaviour
{
    public int Temperature { get; set; } = 0;
    public int Id; // Unique ID for the drone

    // For BST communication
    public Drone LeftChild { get; set; }
    public Drone RightChild { get; set; }

    // Next drone in linked list (for movement update loop)
    private Drone nextDrone;
    public Drone NextDrone
    {
        get { return nextDrone; }
        set { nextDrone = value; }
    }

    private Flock agentFlock;
    public Flock AgentFlock => agentFlock;

    private Collider2D agentCollider;
    public Collider2D AgentCollider => agentCollider;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        agentCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Initialize temperature
        Temperature = Random.Range(0, 100);
        Debug.Log($"Drone {Id} initialized with Temperature {Temperature}.");
    }

    void Update()
    {
        // Randomly update temperature
        Temperature = Random.Range(0, 100);
    }

    public void Initialize(Flock flock)
    {
        agentFlock = flock;
    }

    public void Move(Vector2 velocity)
    {
        transform.up = velocity;
        transform.position += (Vector3)velocity * Time.deltaTime;
    }

    public void ReceiveMessage(string message)
    {
        if (message == "self-destruct")
        {
            gameObject.SetActive(false);
            Debug.Log($"Drone {Id} has self-destructed.");
        }
        else
        {
            Debug.Log($"Drone {Id} received message: {message}");
        }
    }

    // Optional: Method to change color without allocating new components
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}