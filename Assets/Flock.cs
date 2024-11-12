using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class Flock : MonoBehaviour
{
    private float startTime;
    private int? highlightedDroneId = null;
    private int lastAssignedId = 0;
    public Drone agentPrefab;
    private Drone headDrone;
    public FlockBehavior behavior;
    float squareMaxSpeed;
    float squareNeighborRadius;
    float squareAvoidanceRadius;
    public float SquareAvoidanceRadius { get { return squareAvoidanceRadius; } }
    private StreamWriter fpsCsvWriter;
    private StreamWriter calculationsCsvWriter;
    private bool isActive = false;
    public Color highlightColor = Color.yellow;
    private Color defaultColor = Color.white;
    public TMP_Text operationTimeText;  // Reference to the UI text field

    [Range(10, 5000)]
    public int startingCount = 250;
    const float AgentDensity = 0.08f;
    public float timeFactor = 0.1f; // Adjustable factor for time calculation

    [Range(1f, 100f)]
    public float driveFactor = 10f;
    [Range(1f, 100f)]
    public float maxSpeed = 5f;
    [Range(1f, 10f)]
    public float neighborRadius = 1.5f;
    [Range(0f, 1f)]
    public float avoidanceRadiusMultiplier = 0.5f;

    private DroneCommunication tree1;
    private DroneCommunication tree2;

    // UI elements
    public TMP_InputField idInputField;
    public TMP_InputField attributeInputField;

    void Start()
    {
        squareMaxSpeed = maxSpeed * maxSpeed;
        squareNeighborRadius = neighborRadius * neighborRadius;
        squareAvoidanceRadius = squareNeighborRadius * avoidanceRadiusMultiplier * avoidanceRadiusMultiplier;

        StartFlock();
        InitializeCSV();
    }

    void Update()
    {
        if (!isActive) return;
        startTime = Time.time;

        Drone[] drones = ToArray();

        List<Drone> partition1, partition2;
        PartitionDronesByTemperature(drones, out partition1, out partition2);

        // Initialize BSTs for each partition
        tree1 = new DroneCommunication();
        tree2 = new DroneCommunication();

        foreach (var drone in partition1)
        {
            drone.LeftChild = null;
            drone.RightChild = null;
            tree1.InsertDrone(drone, d => d.Id); // Using Id as the key
        }

        foreach (var drone in partition2)
        {
            drone.LeftChild = null;
            drone.RightChild = null;
            tree2.InsertDrone(drone, d => d.Id); // Using Id as the key
        }

        // Update movement for all drones
        Drone currentDrone = headDrone;
        while (currentDrone != null)
        {
            if (currentDrone.gameObject.activeSelf)
            {
                List<Transform> context = GetNearbyObjects(currentDrone);
                Vector2 move = behavior.CalculateMove(currentDrone, context, this);
                move *= driveFactor;

                if (move.sqrMagnitude > squareMaxSpeed)
                {
                    move = move.normalized * maxSpeed;
                }

                currentDrone.Move(move);
            }
            else
            {
                RemoveDroneFromLinkedList(currentDrone);
            }

            currentDrone = currentDrone.NextDrone;
        }

        float deltaTime = Time.deltaTime;
        float fps = 1.0f / deltaTime;
        string currentTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        WriteFpsToCSV(currentTime, fps);
    }

    public void StartFlock()
    {
        Drone previousDrone = null;

        for (int i = 0; i < startingCount; i++)
        {
            Drone newAgent = Instantiate(
                agentPrefab,
                Random.insideUnitCircle * startingCount * AgentDensity,
                Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f)),
                transform
            );
            newAgent.Id = lastAssignedId++;
            newAgent.name = "Agent " + newAgent.Id;
            newAgent.Initialize(this);

            if (previousDrone == null)
            {
                headDrone = newAgent;
            }
            else
            {
                previousDrone.NextDrone = newAgent;
            }

            previousDrone = newAgent;
        }

        isActive = true;
    }

    public void StopFlock()
    {
        isActive = false;
        lastAssignedId = 0;
    }

    public float CalculateSimulatedTime(Vector2 position1, Vector2 position2)
    {
        float distance = Vector2.Distance(position1, position2);
        return distance * timeFactor;
    }

    public void RemoveDroneFromLinkedList(Drone droneToRemove)
    {
        if (headDrone == null || droneToRemove == null)
            return;

        if (headDrone == droneToRemove)
        {
            headDrone = headDrone.NextDrone;
            return;
        }

        Drone currentDrone = headDrone;
        while (currentDrone.NextDrone != null)
        {
            if (currentDrone.NextDrone == droneToRemove)
            {
                currentDrone.NextDrone = currentDrone.NextDrone.NextDrone;
                return;
            }
            currentDrone = currentDrone.NextDrone;
        }
    }

    public void SearchDroneById(int id)
    {
        highlightedDroneId = id;
        float totalSimulatedTime = 0f;

        Drone foundDrone = tree1.FindDrone(id, ref totalSimulatedTime, this);

        if (foundDrone == null)
        {
            foundDrone = tree2.FindDrone(id, ref totalSimulatedTime, this);
        }

        // Reset colors
        Drone[] drones = ToArray();
        foreach (var drone in drones)
        {
            if (drone.Id != highlightedDroneId)
            {
                drone.GetComponent<SpriteRenderer>().color = defaultColor;
            }
        }

        if (foundDrone != null)
        {
            foundDrone.GetComponent<SpriteRenderer>().color = highlightColor;
            UpdateOperationTimeUI("Search", totalSimulatedTime);
            LogCalculationData("Search", id, totalSimulatedTime, totalSimulatedTime);
            Debug.Log($"Drone with ID {id} found. Total simulated time: {totalSimulatedTime:F2} seconds.");
        }
        else
        {
            highlightedDroneId = null;
            UpdateOperationTimeUI("Search", totalSimulatedTime);
            Debug.Log($"Drone with ID {id} not found. Total simulated time: {totalSimulatedTime:F2} seconds.");
        }
    }

    public bool DeleteDroneById(int id)
    {
        float totalSimulatedTime = 0f;
        bool deleted = false;

        Drone foundDrone = tree1.FindDrone(id, ref totalSimulatedTime, this);
        if (foundDrone != null)
        {
            tree1.DeleteDroneById(id, this);
            deleted = true;
        }
        else
        {
            foundDrone = tree2.FindDrone(id, ref totalSimulatedTime, this);
            if (foundDrone != null)
            {
                tree2.DeleteDroneById(id, this);
                deleted = true;
            }
        }

        if (deleted)
        {
            UpdateOperationTimeUI("Delete", totalSimulatedTime);
            LogCalculationData("Delete", id, totalSimulatedTime, totalSimulatedTime);
            Debug.Log($"Drone with ID {id} deleted. Total simulated time: {totalSimulatedTime:F2} seconds.");
            return true;
        }
        else
        {
            UpdateOperationTimeUI("Delete", totalSimulatedTime);
            Debug.Log($"Drone with ID {id} not found for deletion. Total simulated time: {totalSimulatedTime:F2} seconds.");
            return false;
        }
    }

    public void OnSearchByIdButtonClicked()
    {
        int id;
        if (int.TryParse(idInputField.text, out id))
        {
            SearchDroneById(id);
        }
        else
        {
            Debug.LogError("Invalid ID input.");
        }
    }

    public void OnDeleteDroneButtonClicked()
    {
        int id;
        if (int.TryParse(idInputField.text, out id))
        {
            DeleteDroneById(id);
        }
        else
        {
            Debug.LogError("Invalid ID input.");
        }
    }

    public void OnSearchByAttributeButtonClicked()
    {
        int temp;
        if (int.TryParse(attributeInputField.text, out temp))
        {
            SearchDroneByAttribute(d => d.Temperature == temp);
        }
        else
        {
            Debug.LogError("Invalid attribute input.");
        }
    }

    private void UpdateOperationTimeUI(string operation, float totalSimulatedTime)
    {
        if (operationTimeText != null)
        {
            operationTimeText.text = $"{operation} Operation Time: {totalSimulatedTime:F2} seconds";
        }
    }

    void PartitionDronesByTemperature(Drone[] drones, out List<Drone> partition1, out List<Drone> partition2)
    {
        partition1 = new List<Drone>();
        partition2 = new List<Drone>();

        if (drones.Length == 0) return;

        float pivotTemperature = drones[0].Temperature;

        foreach (var drone in drones)
        {
            if (drone == null || !drone.gameObject.activeSelf)
                continue;

            // Only change color if not the highlighted drone
            if (drone.Id != highlightedDroneId)
            {
                if (drone.Temperature <= pivotTemperature)
                {
                    drone.GetComponent<SpriteRenderer>().color = Color.blue;
                }
                else
                {
                    drone.GetComponent<SpriteRenderer>().color = Color.red;
                }
            }

            // Add to partitions
            if (drone.Temperature <= pivotTemperature)
            {
                partition1.Add(drone);
            }
            else
            {
                partition2.Add(drone);
            }
        }
    }

    List<Transform> GetNearbyObjects(Drone agent)
    {
        List<Transform> context = new List<Transform>();
        Collider2D[] contextColliders = Physics2D.OverlapCircleAll(agent.transform.position, neighborRadius);
        foreach (Collider2D c in contextColliders)
        {
            if (c != agent.AgentCollider)
            {
                context.Add(c.transform);
            }
        }
        return context;
    }

    private void InitializeCSV()
    {
        string fpsFilePath = Path.Combine(Application.dataPath, "TimingResults.csv");
        fpsCsvWriter = new StreamWriter(fpsFilePath, false);
        fpsCsvWriter.WriteLine("Timestamp, FPS");

        string calculationsFilePath = Path.Combine(Application.dataPath, "DroneOperationCalculations.csv");
        calculationsCsvWriter = new StreamWriter(calculationsFilePath, false);
        calculationsCsvWriter.WriteLine("Operation Type, Drone ID, Step Simulated Time, Total Simulated Time");
    }

    private void WriteFpsToCSV(string timestamp, float fps)
    {
        fpsCsvWriter.WriteLine($"{timestamp}, {fps}");
        fpsCsvWriter.Flush();
    }

    private void LogCalculationData(string operationType, int droneId, float stepSimulatedTime, float totalSimulatedTime)
    {
        calculationsCsvWriter.WriteLine($"{operationType}, {droneId}, {stepSimulatedTime}, {totalSimulatedTime}");
        calculationsCsvWriter.Flush();
    }

    private void OnDestroy()
    {
        fpsCsvWriter.Close();
        calculationsCsvWriter.Close();
    }

    public Drone[] ToArray()
    {
        List<Drone> droneList = new List<Drone>();
        Drone currentDrone = headDrone;
        while (currentDrone != null)
        {
            if (currentDrone.gameObject.activeSelf)
            {
                droneList.Add(currentDrone);
            }
            else
            {
                RemoveDroneFromLinkedList(currentDrone);
            }
            currentDrone = currentDrone.NextDrone;
        }
        return droneList.ToArray();
    }

    // Exhaustive search for other attributes
    public void SearchDroneByAttribute(System.Func<Drone, bool> predicate)
    {
        highlightedDroneId = null;
        float totalSimulatedTime = 0f;
        Drone foundDrone = null;

        List<DroneCommunication> trees = new List<DroneCommunication> { tree1, tree2 };

        foreach (var tree in trees)
        {
            foundDrone = tree.ExhaustiveSearch(tree.Root, predicate, ref totalSimulatedTime, this);
            if (foundDrone != null)
            {
                break;
            }
        }

        // Reset colors
        Drone[] drones = ToArray();
        foreach (var drone in drones)
        {
            if (drone.Id != highlightedDroneId)
            {
                drone.GetComponent<SpriteRenderer>().color = defaultColor;
            }
        }

        if (foundDrone != null)
        {
            highlightedDroneId = foundDrone.Id;
            foundDrone.GetComponent<SpriteRenderer>().color = highlightColor;
            UpdateOperationTimeUI("Exhaustive Search", totalSimulatedTime);
            LogCalculationData("Exhaustive Search", foundDrone.Id, totalSimulatedTime, totalSimulatedTime);
            Debug.Log($"Drone found by attribute. Total simulated time: {totalSimulatedTime:F2} seconds.");
        }
        else
        {
            UpdateOperationTimeUI("Exhaustive Search", totalSimulatedTime);
            Debug.Log($"Drone not found by attribute. Total simulated time: {totalSimulatedTime:F2} seconds.");
        }
    }
}