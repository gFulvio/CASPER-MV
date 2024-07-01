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
        // Se la chiave è "endPos" e il nuovo valore è uguale a quello attuale di "endPos", non fare nulla
        if (newValue == userPositions["endPos"])
        {
            return;
        }

        // Altrimenti, aggiorna le posizioni
        userPositions["startPos"] = userPositions["endPos"];
        userPositions["endPos"] = newValue;
    }

    private void UpdateObjectPosition(string key, Vector3 value)
    {
        if (objectPositions.ContainsKey(key))
        {
            objectPositions[key] = value; // Aggiorna il valore esistente
        }
        else
        {
            objectPositions.Add(key, value); // Aggiunge una nuova coppia chiave-valore
        }
    }
}
