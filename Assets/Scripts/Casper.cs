using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Casper : MonoBehaviour
{
    
    
    
    

    // Variabili per il rilevamento
    [SerializeField] private Transform user;  // Riferimento al transform dell'utente

    [SerializeField] private LayerMask objectLayer;  // Layer degli oggetti da rilevare

    [SerializeField] private float detectionRange = 5f;  // Raggio di rilevamento dell'agente

    private WorldState currentWorldState; // Stato del mondo

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

    // Informazioni sugli oggetti rilevati
    [System.Serializable]
    public class DetectedObjectInfo
    {
        public string name;  // Nome dell'oggetto rilevato
        public Vector3 position;  // Posizione dell'oggetto rilevato
    }
    public List<DetectedObjectInfo> detectedObjectsInfo;

    private void Start()
    {
        currentWorldState = new WorldState(user.position, user.position, "", new Vector3());
        detectedObjectsInfo = new List<DetectedObjectInfo>();

        observations = new List<string> { "Pick and Place", "Sip" };
    }

	private void Update()
    {
        //Osservazione dell'user
        transform.LookAt(new Vector3(user.position.x, transform.position.y, user.position.z));

        // Rilevamento oggetti
        GetDetectedObjects();

	    // Aggiornamento dello stato del mondo attuale
		foreach (DetectedObjectInfo objectInfo in detectedObjectsInfo)
		{
			currentWorldState.UpdateWorldState(user.position, objectInfo.name, objectInfo.position);
		}

		perception.StartPerception(currentWorldState, user);
		// Prendi i risultati di perception e passali a LowLevel
		var (qsrResults, objectPositions) = perception.GetLastResultsForObjects();
		lowLevel.FocusEstimation(qsrResults, user, objectPositions);
        target.text = lowLevel.target;

        lowLevel.MovementsClassifier();
        classifiedMovement.text = lowLevel.identifiedMovement;

        lowLevel.ActionPredictor();
        predictedAction.text = lowLevel.predictedAction;

        //highLevel.GoalR(lowLevel.observedActions);
        highLevel.GoalR(observations);
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
