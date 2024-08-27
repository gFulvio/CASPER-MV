using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Casper : MonoBehaviour
{
    // Variables for OOIs detection
    [SerializeField] private Transform user;  // Reference to user's transform

    [SerializeField] private LayerMask objectLayer;  // Layer of objects to be detected

    [SerializeField] private float detectionRange = 5f;  // Agent detection radius

    private WorldState currentWorldState; // State of the world

    // Modules
    public Perception perception;

    public LowLevel lowLevel;
    public TMP_Text target;
    public TMP_Text classifiedMovement;
    public TMP_Text predictedAction;

    public HighLevel highLevel;
    public TMP_Text predictedGoal;

    public Supervisor supervisor;
    public TMP_Text response;

    List<string> observations;

    // Information about detected objects
    [System.Serializable]
    public class DetectedObjectInfo
    {
        public string name;  // Name of detected object
        public Vector3 position;  // Location of the detected object
    }
    public List<DetectedObjectInfo> detectedObjectsInfo;

    private void Start()
    {
        currentWorldState = new WorldState(user.position, user.position, "", new Vector3());
        detectedObjectsInfo = new List<DetectedObjectInfo>();

        //observations = new List<string> { "Pick and Place Meal", "Cook" };
    }

	private void Update()
    {
        // User observation
        transform.LookAt(new Vector3(user.position.x, transform.position.y, user.position.z));

        // Object detection
        GetDetectedObjects();

        // Current world status update
        foreach (DetectedObjectInfo objectInfo in detectedObjectsInfo)
		{
			currentWorldState.UpdateWorldState(user.position, objectInfo.name, objectInfo.position);
		}

		perception.StartPerception(currentWorldState, user);
        // Take perception results and pass them to LowLevel
        var (qsrResults, objectPositions) = perception.GetLastResultsForObjects();
		lowLevel.FocusEstimation(qsrResults, user, objectPositions);
        target.text = lowLevel.target;

        lowLevel.MovementsClassifier();
        classifiedMovement.text = lowLevel.identifiedMovement;

        lowLevel.ActionPredictor();
        predictedAction.text = lowLevel.predictedAction;

        if(lowLevel.observedActions.Count > 0 )
        {
            highLevel.GoalReasoner(lowLevel.observedActions);
        }
        //highLevel.GoalReasoner(observations);
        predictedGoal.text = highLevel.predictedGoal;

        /*if(predictedGoal != null)
        {
			supervisor.PlanGenerator(currentWorldState, target, predictedGoal);
			supervisor.ProcessDirector();
		}*/
        
    }


    public float GetDetectionRange()
    {
        return detectionRange;
    }

    public List<DetectedObjectInfo> GetDetectedObjectsInfo()
    {
        return detectedObjectsInfo;
    }

    private Collider[] GetDetectedObjects()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, objectLayer);

        detectedObjectsInfo.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            GameObject detectedObject = hits[i].gameObject;
            DetectedObjectInfo objectInfo = new DetectedObjectInfo();
            objectInfo.name = detectedObject.name;
            objectInfo.position = detectedObject.transform.position;
            detectedObjectsInfo.Add(objectInfo);
            detectedObject.GetComponent<Renderer>().material.color = Color.red;
        }

        return hits;
    }
}
