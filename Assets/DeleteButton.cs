using UnityEngine;
using TMPro;

public class DeleteButton : MonoBehaviour
{
    public Flock flock;  // Reference to the Flock class
    public TMP_InputField idInputField;  // Input field to enter the drone ID to delete
    public TMP_Text outputText;  // Output text to show messages to the user

    // Only keep one DeleteDrone method. You can rename this if needed.
    public void DeleteDrone()
    {
        if (int.TryParse(idInputField.text, out int id))
        {
            bool success = flock.DeleteDroneById(id);
            if (success)
            {
                outputText.text = "Drone with ID " + id + " has been deleted.";
            }
            else
            {
                outputText.text = "Drone with ID " + id + " not found.";
            }
        }
        else
        {
            outputText.text = "Please enter a valid ID.";
        }
    }
}
