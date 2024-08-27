using System.Collections.Generic;
using UnityEngine;


internal class QSRLibrary
{
    // Dictionary containing, for each object, a dictionary of all QSRs from the beginning of the activity
    private Dictionary<string, Dictionary<string, string>> library = new Dictionary<string, Dictionary<string, string>>();

    /// <summary>
    /// Empty builder
    /// </summary>
    public QSRLibrary() { }

    /// <summary>
    /// Adds a QSR state to the specified object in the library
    /// </summary>
    /// <param name="objectName">The name of the object</param>
    /// <param name="qsrState">The qsr related to the object</param>
    public void AddQSRState(string objectName, Dictionary<string, string> qsrState)
    {
        if (!library.ContainsKey(objectName))
        {
            library[objectName] = new Dictionary<string, string>();
        }

        foreach (var qsr in qsrState)
        {
            library[objectName][qsr.Key] = qsr.Value;
        }
    }

    /// <summary>
    /// Returns all QSRs associated with an object since the start of the activity
    /// </summary>
    /// <param name="objectName">Object name</param>
    /// <returns>Returns a dictionary with all qsr related to the object. Returns an empty dictionary if the object is not present</returns>
    public Dictionary<string, string> GetAllQSRsForObject(string objectName)
    {
        if (library.ContainsKey(objectName))
        {
            return library[objectName];
        }

        return new Dictionary<string, string>();
    }

    /// <summary>
    /// Returns the last QSR description associated with an object in the library
    /// </summary>
    /// <param name="objectName">Object name</param>
    /// <returns>Returns a dictionary with the last qsr related to the object. Returns an empty dictionary if the object is not present</returns>
    public Dictionary<string, string> GetLastQSRStateForObject(string objectName)
    {
        if (library.ContainsKey(objectName))
        {
            return library[objectName];
        }

        return new Dictionary<string, string>();
    }

}

/// <summary>
/// QSREngine class within the Perception module.
/// </summary>
internal class QSREngine
{
    private Vector3 previousUserPosition;

    /// <summary>
    /// Class constructor. Initializes the previous position with the initial position
    /// </summary>
    public QSREngine()
    {
        previousUserPosition = Vector3.zero;
    }

    /// <summary>
    /// Method for calculating the distance between the user and the object (QDC)
    /// </summary>
    /// <param name="objectPosition">Location of the object</param>
    /// <param name="userPosition">User position</param>
    /// <returns>Returns a string indicating the distance between the user and the object</returns>
    public string CalculateQDC(Vector3 objectPosition, Vector3 userPosition)
    {
        float distance = Vector3.Distance(objectPosition, userPosition);

        if (distance >= 0.0f && distance <= 0.6f)
            return "Touch";
        else if (distance > 0.6f && distance <= 2.0f)
            return "Near";
        else if (distance > 2.0f && distance <= 3.0f)
            return "Medium";
        else if (distance > 3.0f && distance <= 5.0f)
            return "Far";
        else
            return "Ignoring";
    }

    /// <summary>
    /// Method to calculate the user's trajectory with respect to OOIs by considering the product between vectors
    /// </summary>
    /// <param name="userStartPos">Initial position of the user</param>
    /// <param name="userEndPos">User's final position</param>
    /// <param name="objectPos">Location of the object</param>
    /// <returns>A string indicating whether the user is approaching or moving away from the object</returns>
    public string CalculateQTC(Transform user, Vector3 objectPos)
    {
        Vector3 objectDirection = (objectPos - user.position).normalized;
        Vector3 userMovementDirection = (user.forward).normalized;
        float dotProduct = Vector3.Dot(userMovementDirection, objectDirection);

        if (dotProduct > 0)
        {
            return "Approaching";
        }
        else
        {
            return "Leaving";
        }
    }

    /// <summary>
    /// It calculates whether the user is moving or stationary.
    /// </summary>
    /// <param name="userEndPos">Latest user posture</param>
    /// <returns>A string indicating whether or not the user is in motion</returns>
    public string CalculateMOS(Vector3 userEndPos)
    {
        float velocityThreshold = 0.01f;  // Set the velocity threshold

        // Calculates the velocity based on the distance between the current and previous position
        float velocity = Vector3.Distance(userEndPos, previousUserPosition);

        // Update previous position
        previousUserPosition = userEndPos;

        // Check whether the velocity exceeds the threshold
        if (velocity > velocityThreshold)
        {
            return "Moving";
        }
        else
        {
            return "Stationary";
        }
    }

    /// <summary>
    /// Checks whether the user is holding an object or not.
    /// </summary>
    /// <returns>Yes or No</returns>
    public string CalculateHOLD(Vector3 objectPos, Vector3 handPos)
    {
        if(Vector3.Distance(objectPos, handPos) == 0)
        {
            return "Yes";
        }
        else
        {
            return "No";
        }
    }
}

/// <summary>
/// Main class Perception
/// </summary>
public class Perception : MonoBehaviour
{
    private QSREngine currentQSR;
    private QSRLibrary qsrlibrary;
    private WorldState currentWorldState;
    public Transform hand;

    [System.Serializable]
    public class QSREngineResults
    {
        public string name;
        public Vector3 position;
        public string qdc;
        public string qtc;
        public string mos;
        public string hold;
    }

    public List<QSREngineResults> qsrEngineResults;

    private void Start()
    {
        currentQSR = new QSREngine();
        qsrlibrary = new QSRLibrary();
        qsrEngineResults = new List<QSREngineResults>();
    }

    /// <summary>
    /// Start the calculation of QSRs
    /// </summary>
    /// <param name="worldState">The state of the world with the positions of the user and detected objects</param>
    public void StartPerception(WorldState worldState, Transform user)
    {
		currentWorldState = worldState;

        string mosResult = currentQSR.CalculateMOS(currentWorldState.userPositions["endPos"]);

        qsrEngineResults.Clear();

        foreach (var objectPosition in currentWorldState.objectPositions)
        {
            Dictionary<string, string> objectQSRs = new Dictionary<string, string>();

            string qdcResult = currentQSR.CalculateQDC(objectPosition.Value, currentWorldState.userPositions["endPos"]);
            string qtcResult = currentQSR.CalculateQTC(user, objectPosition.Value);
            string holdResult = currentQSR.CalculateHOLD(objectPosition.Value, hand.position);

            // Library update and display in the Inspector
            objectQSRs.Add("QDC", qdcResult);
            if(mosResult == "Stationary")
            {
                objectQSRs.Add("QTC", mosResult);
            } else
            {
                objectQSRs.Add("QTC", qtcResult);
            }
            objectQSRs.Add("MOS", mosResult);
            objectQSRs.Add("HOLD", holdResult);

            QSREngineResults currentObject = new QSREngineResults();
            currentObject.name = objectPosition.Key;
            currentObject.position = objectPosition.Value;
            currentObject.qdc = qdcResult;
            currentObject.qtc = qtcResult;
            currentObject.mos = mosResult;
            currentObject.hold = holdResult;
            qsrEngineResults.Add(currentObject);

            qsrlibrary.AddQSRState(objectPosition.Key, objectQSRs);
        }
    }

    /// <summary>
    /// Gets the latest results added to the library
    /// </summary>
    /// <returns>QSRs associated with each object</returns>
    public (Dictionary<string, Dictionary<string, string>>, Dictionary<string, Vector3>) GetLastResultsForObjects()
    {
        Dictionary<string, Dictionary<string, string>> qsrResults = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, Vector3> objectPositions = new Dictionary<string, Vector3>();

        // Aggiungi gli ultimi risultati QSR e le posizioni per ogni oggetto
        foreach (var objectPosition in currentWorldState.objectPositions)
        {
            qsrResults[objectPosition.Key] = qsrlibrary.GetLastQSRStateForObject(objectPosition.Key);
            objectPositions[objectPosition.Key] = objectPosition.Value;
        }

        return (qsrResults, objectPositions);
    }


}