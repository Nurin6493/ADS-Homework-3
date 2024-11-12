using UnityEngine;

public class DroneCommunication
{
    public Drone Root { get; private set; }

    public void InsertDrone(Drone newDrone, System.Func<Drone, int> keySelector)
    {
        Root = InsertRecursive(Root, newDrone, keySelector);
    }

    private Drone InsertRecursive(Drone current, Drone newDrone, System.Func<Drone, int> keySelector)
    {
        if (current == null)
        {
            return newDrone;
        }

        int newKey = keySelector(newDrone);
        int currentKey = keySelector(current);

        if (newKey < currentKey)
        {
            current.LeftChild = InsertRecursive(current.LeftChild, newDrone, keySelector);
        }
        else
        {
            current.RightChild = InsertRecursive(current.RightChild, newDrone, keySelector);
        }

        return current;
    }

    public Drone FindDrone(int id, ref float totalSimulatedTime, Flock flock)
    {
        return FindDroneRecursive(Root, id, ref totalSimulatedTime, flock);
    }

    private Drone FindDroneRecursive(Drone current, int id, ref float totalSimulatedTime, Flock flock)
    {
        if (current == null || !current.gameObject.activeSelf)
        {
            Debug.Log("Drone not found.");
            return null;
        }

        Debug.Log($"Checking drone with ID: {current.Id}");

        if (current.Id == id)
        {
            return current;
        }

        Drone nextDrone = (id < current.Id) ? current.LeftChild : current.RightChild;

        if (nextDrone != null && nextDrone.gameObject.activeSelf)
        {
            float stepTime = flock.CalculateSimulatedTime(current.transform.position, nextDrone.transform.position);
            totalSimulatedTime += stepTime;
        }

        if (id < current.Id)
        {
            return FindDroneRecursive(current.LeftChild, id, ref totalSimulatedTime, flock);
        }
        else
        {
            return FindDroneRecursive(current.RightChild, id, ref totalSimulatedTime, flock);
        }
    }

    public void DeleteDroneById(int id, Flock flock)
    {
        Root = DeleteRecursive(Root, id, flock);
    }

    private Drone DeleteRecursive(Drone current, int id, Flock flock)
    {
        if (current == null)
        {
            // Drone not found
            return null;
        }

        if (id < current.Id)
        {
            current.LeftChild = DeleteRecursive(current.LeftChild, id, flock);
        }
        else if (id > current.Id)
        {
            current.RightChild = DeleteRecursive(current.RightChild, id, flock);
        }
        else
        {
            // Drone found
            // Remove from linked list and destroy GameObject
            flock.RemoveDroneFromLinkedList(current);
            GameObject.Destroy(current.gameObject);

            // Node with only one child or no child
            if (current.LeftChild == null)
            {
                return current.RightChild;
            }
            else if (current.RightChild == null)
            {
                return current.LeftChild;
            }

            // Node with two children
            // Find the in-order successor
            Drone successor = FindMin(current.RightChild);

            // Replace current node's data with successor's data
            current.Id = successor.Id;
            current.Temperature = successor.Temperature;
            current.name = successor.name;
            // Copy other necessary fields here

            // Delete the successor node
            current.RightChild = DeleteRecursive(current.RightChild, successor.Id, flock);
        }

        return current;
    }

    private Drone FindMin(Drone node)
    {
        while (node.LeftChild != null)
        {
            node = node.LeftChild;
        }
        return node;
    }

    // Exhaustive search for other attributes
    public Drone ExhaustiveSearch(Drone current, System.Func<Drone, bool> predicate, ref float totalSimulatedTime, Flock flock)
    {
        if (current == null || !current.gameObject.activeSelf)
        {
            return null;
        }

        // Check current node
        if (predicate(current))
        {
            return current;
        }

        // Visit left child
        if (current.LeftChild != null)
        {
            float stepTime = flock.CalculateSimulatedTime(current.transform.position, current.LeftChild.transform.position);
            totalSimulatedTime += stepTime;

            Drone found = ExhaustiveSearch(current.LeftChild, predicate, ref totalSimulatedTime, flock);
            if (found != null)
            {
                return found;
            }
        }

        // Visit right child
        if (current.RightChild != null)
        {
            float stepTime = flock.CalculateSimulatedTime(current.transform.position, current.RightChild.transform.position);
            totalSimulatedTime += stepTime;

            Drone found = ExhaustiveSearch(current.RightChild, predicate, ref totalSimulatedTime, flock);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}