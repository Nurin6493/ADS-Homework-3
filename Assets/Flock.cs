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
        PartitionDronesByTemperature(drones);

        Drone currentDrone = headDrone;
        while (currentDrone != null)
        {
            List<Transform> context = GetNearbyObjects(currentDrone);
            Vector2 move = behavior.CalculateMove(currentDrone, context, this);
            move *= driveFactor;

            if (move.sqrMagnitude > squareMaxSpeed)
            {
                move = move.normalized * maxSpeed;
            }

            currentDrone.Move(move);
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

    private float CalculateSimulatedTime(Vector2 position1, Vector2 position2)
    {
        float distance = Vector2.Distance(position1, position2);
        return distance * timeFactor;
    }

    public void SearchDroneById(int id)
    {
        highlightedDroneId = id;
        float totalSimulatedTime = 0f;

        Drone currentDrone = headDrone;
        while (currentDrone != null)
        {
            currentDrone.GetComponent<SpriteRenderer>().color = defaultColor;

            if (currentDrone.Id == id)
            {
                currentDrone.GetComponent<SpriteRenderer>().color = highlightColor;
                UpdateOperationTimeUI("Search", totalSimulatedTime);
                LogCalculationData("Search", id, totalSimulatedTime, totalSimulatedTime);
                Debug.Log($"Drone with ID {id} found. Total simulated time: {totalSimulatedTime:F2} seconds.");
                return;
            }

            if (currentDrone.NextDrone != null)
            {
                float stepTime = CalculateSimulatedTime(currentDrone.transform.position, currentDrone.NextDrone.transform.position);
                totalSimulatedTime += stepTime;
                LogCalculationData("Search", id, stepTime, totalSimulatedTime);
            }

            currentDrone = currentDrone.NextDrone;
        }

        highlightedDroneId = null;
        UpdateOperationTimeUI("Search", totalSimulatedTime);
        Debug.Log($"Drone with ID {id} not found. Total simulated time: {totalSimulatedTime:F2} seconds.");
    }

    public bool DeleteDroneById(int id)
    {
        Drone currentDrone = headDrone;
        Drone previousDrone = null;
        float totalSimulatedTime = 0f;

        while (currentDrone != null)
        {
            if (currentDrone.Id == id)
            {
                if (previousDrone == null)
                {
                    headDrone = currentDrone.NextDrone;
                }
                else
                {
                    previousDrone.NextDrone = currentDrone.NextDrone;
                }

                Destroy(currentDrone.gameObject);
                UpdateOperationTimeUI("Delete", totalSimulatedTime);
                LogCalculationData("Delete", id, totalSimulatedTime, totalSimulatedTime);
                Debug.Log($"Drone with ID {id} deleted. Total simulated time: {totalSimulatedTime:F2} seconds.");
                return true;
            }

            if (currentDrone.NextDrone != null)
            {
                float stepTime = CalculateSimulatedTime(currentDrone.transform.position, currentDrone.NextDrone.transform.position);
                totalSimulatedTime += stepTime;
                LogCalculationData("Delete", id, stepTime, totalSimulatedTime);
            }

            previousDrone = currentDrone;
            currentDrone = currentDrone.NextDrone;
        }

        UpdateOperationTimeUI("Delete", totalSimulatedTime);
        Debug.Log($"Drone with ID {id} not found for deletion. Total simulated time: {totalSimulatedTime:F2} seconds.");
        return false;
    }

    private void UpdateOperationTimeUI(string operation, float totalSimulatedTime)
    {
        if (operationTimeText != null)
        {
            operationTimeText.text = $"{operation} Operation Time: {totalSimulatedTime:F2} seconds";
        }
    }

    void PartitionDronesByTemperature(Drone[] drones)
    {
        if (drones.Length == 0) return;

        float pivotTemperature = drones[0].Temperature;

        Drone currentDrone = headDrone;
        while (currentDrone != null)
        {
            if (highlightedDroneId.HasValue && currentDrone.Id == highlightedDroneId.Value)
            {
                currentDrone = currentDrone.NextDrone;
                continue;
            }

            currentDrone.GetComponent<SpriteRenderer>().color = currentDrone.Temperature <= pivotTemperature ? Color.blue : Color.red;
            currentDrone = currentDrone.NextDrone;
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
            droneList.Add(currentDrone);
            currentDrone = currentDrone.NextDrone;
        }
        return droneList.ToArray();
    }
}
