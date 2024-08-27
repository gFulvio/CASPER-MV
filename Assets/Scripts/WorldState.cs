using System.Collections.Generic;
using UnityEngine;

public class WorldState
{
    public Dictionary<string, Vector3> userPositions = new Dictionary<string, Vector3>();
    public Dictionary<string, Vector3> objectPositions = new Dictionary<string, Vector3>();

    public WorldState(Vector3 startPos, Vector3 endPos, string objectName, Vector3 objectPosition)
    {
        userPositions.Add("startPos", startPos);
        userPositions.Add("endPos", endPos);
        UpdateObjectPosition(objectName, objectPosition);
    }

    public void UpdateWorldState(Vector3 endPos, string objectName, Vector3 objectPosition)
    {
        UpdateUserPositions(endPos);
        UpdateObjectPosition(objectName, objectPosition);
    }

    private void UpdateUserPositions(Vector3 newValue)
    {
        // If the key is "endPos" and the new value is equal to the current value of "endPos," do nothing
        if (newValue == userPositions["endPos"])
        {
            return;
        }

        // Otherwise, update the positions
        userPositions["startPos"] = userPositions["endPos"];
        userPositions["endPos"] = newValue;
    }

    private void UpdateObjectPosition(string key, Vector3 value)
    {
        if (objectPositions.ContainsKey(key))
        {
            objectPositions[key] = value; // Update existing value
        }
        else
        {
            objectPositions.Add(key, value); // Adds a new key-value pair
        }
    }
}
