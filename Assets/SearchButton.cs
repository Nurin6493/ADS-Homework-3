using UnityEngine;
using TMPro; 

public class SearchButton : MonoBehaviour
{
    [SerializeField] private Flock _flock; 
    [SerializeField] private TMP_InputField _droneIdInput; 

    
    public void OnSearchButtonClick()
    {
        if (_flock == null || _droneIdInput == null)
        {
            Debug.LogError("One of the references (Flock or Drone ID Input) is not set in the Inspector.");
            return;
        }

        if (int.TryParse(_droneIdInput.text, out int id))
        {
            _flock.SearchDroneById(id);
        }
        else
        {
            Debug.Log("Invalid drone ID entered.");
        }

    }
}
