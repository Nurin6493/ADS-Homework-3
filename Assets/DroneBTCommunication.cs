using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class DroneBTCommunication : MonoBehaviour
{
    public TMP_InputField inputField; // TMP input field for user input
    public TMP_Text resultText;       // TMP text field to display results

    private DroneCommunication droneCommunication;
    private Flock flock;

    void Awake()
    {
        // Get a reference to the Flock component in the scene
        flock = FindObjectOfType<Flock>();

        // Initialize the DroneCommunication instance
        droneCommunication = new DroneCommunication();

        // Build the BST with existing drones
        InitializeDroneCommunication();
    }

    void InitializeDroneCommunication()
    {
        Drone[] drones = flock.ToArray();
        foreach (var drone in drones)
        {
            droneCommunication.InsertDrone(drone, d => d.Id); // Using Id as the key
        }
    }

    public void OnSearchButtonClick()
    {
        if (int.TryParse(inputField.text, out int droneId))
        {
            float totalSimulatedTime = 0f;
            Drone result = droneCommunication.FindDrone(droneId, ref totalSimulatedTime, flock);
            if (result != null)
            {
                resultText.text = $"Drone {droneId} found at position: ({result.transform.position.x}, {result.transform.position.y})";
                // Highlight the found drone
                result.SetColor(Color.yellow);
            }
            else
            {
                resultText.text = $"Drone {droneId} not found.";
            }
        }
        else
        {
            resultText.text = "Please enter a valid Drone ID.";
        }
    }

    public void OnSelfDestructButtonClick()
    {
        if (int.TryParse(inputField.text, out int droneId))
        {
            float totalSimulatedTime = 0f;
            Drone targetDrone = droneCommunication.FindDrone(droneId, ref totalSimulatedTime, flock);
            if (targetDrone != null)
            {
                // Remove drone from Flock's linked list and BST
                flock.RemoveDroneFromLinkedList(targetDrone);

                // Destroy the drone's GameObject
                GameObject.Destroy(targetDrone.gameObject);

                // Remove drone from DroneCommunication BST
                droneCommunication.DeleteDroneById(droneId, flock);

                resultText.text = $"Drone {droneId} has been self-destructed.";
            }
            else
            {
                resultText.text = $"Drone {droneId} not found for self-destruction.";
            }
        }
        else
        {
            resultText.text = "Please enter a valid Drone ID.";
        }
    }
}