using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Microsoft.ML;
using Markov;
using Assembly_CSharp;
using System.Linq;
using Percolator.Matching;

internal class FocusEstimator
{
    private float w1 = 0.8f; // Peso arbitrario
    private float w2 = 0.2f; // Peso arbitrario

    public FocusEstimator()
    {

    }

    public Dictionary<string, Dictionary<string, float>> QSREncoder(Dictionary<string, Dictionary<string, string>> results)
    {
        // Creare un nuovo dizionario per i risultati codificati con valori numerici
        Dictionary<string, Dictionary<string, float>> encodedResults = new Dictionary<string, Dictionary<string, float>>();

        // Iterare attraverso gli oggetti nel dizionario da codificare
        foreach (var objectResult in results)
        {
            string objectName = objectResult.Key;
            Dictionary<string, float> encodedObjectQSRs = new Dictionary<string, float>();

            // Iterare attraverso le QSR per l'oggetto corrente
            foreach (var qsr in objectResult.Value)
            {
                string qsrType = qsr.Key;
                string qsrValue = qsr.Value;

                // Mappa il valore QSR a un valore numerico utilizzando la tua logica
                float numericValue = MapQSRToNumericValue(qsrType, qsrValue);

                // Aggiungi il risultato codificato al dizionario
                if(numericValue != -1.0f)
                {
                    encodedObjectQSRs.Add(qsrType, numericValue);
                }
                
            }

            // Aggiungi il risultato dell'oggetto codificato al dizionario principale
            encodedResults.Add(objectName, encodedObjectQSRs);
        }

        // Restituisci il dizionario completo con risultati codificati
        return encodedResults;
    }

    private float MapQSRToNumericValue(string qsrType, string qsrValue)
    {
        if (qsrType == "QDC")
        {
            switch (qsrValue)
            {
                case "Touch":
                    return 0.5f;
                case "Near":
                    return 0.25f;
                case "Medium":
                    return 0.125f;
                default:
                    return 0.0f;
            }
        }
        else if (qsrType == "QTC")
        {
            switch (qsrValue)
            {
                case "Approaching":
                    return 0.25f;
                case "Leaving":
                    return 0f;
                case "Stationary":
                    return 0.5f;
            }
        }

        // Se non si verifica nessuna corrispondenza, restituisci un valore di default
        return -1.0f;
    }

    public Dictionary<string, float> CalculateScores(Dictionary<string, Dictionary<string, float>> encodedResults, Transform user, Dictionary<string, Vector3> objectPositions)
    {
        Dictionary<string, float> scores = new Dictionary<string, float>();

        foreach (var objectResult in encodedResults)
        {
            string objectName = objectResult.Key;

            // Ottieni i valori numerici QDC e QTC dal dizionario codificato
            float qdcValue = objectResult.Value.ContainsKey("QDC") ? objectResult.Value["QDC"] : 0.0f;
            float qtcValue = objectResult.Value.ContainsKey("QTC") ? objectResult.Value["QTC"] : 0.0f;

            // Ottieni la posizione dell'oggetto
            Vector3 objectPosition = objectPositions[objectName];

            // Calcola l'angolo h tra la linea che collega l'utente all'oggetto e l'orientamento dell'utente
            Vector3 objPosOnPlane = new Vector3(objectPosition.x, user.position.y, objectPosition.z); //per avere i vettori alla stezza altezza
            Vector3 direction = Vector3.Normalize(objPosOnPlane - user.position);
            float angleH = Vector3.Angle(user.forward, direction);

            // Calcola il punteggio S(i) utilizzando l'equazione fornita
            float score = (w1 * qdcValue + w2 * qtcValue) / (1 + angleH);

            // Aggiungi il punteggio al dizionario dei punteggi
            scores[objectName] = score;
        }

        return scores;
    }
}


public class LowLevel : MonoBehaviour
{
    FocusEstimator focusEstimator;

    private Dictionary<string, Dictionary<string, string>> perceptionResults;

	public List<string> movements;
	public string target;
    public float targetScore;
    public string identifiedMovement;
    public int observationsNumber = 5;
    public float actionThreshold = 0.8f;
    public string predictedAction;

    List<string> movementObservations;
    Fuzzylator fuzzylator;
    public List<string> observedActions;

    [System.Serializable]
    public class Action
    {
        public string name;
        public List<List<int>> probabilities;
        public MarkovChain<string> markovChain;
        public string prediction;
        public double score;

        public Action(string name, List<List<int>> probabilities)
        {
            this.name = name;
			this.probabilities = probabilities;
			markovChain = new MarkovChain<string>(1);
			prediction = "";
            score = 0;
        }
    }

    public List<Action> actions;


	// Start is called before the first frame update
	void Start()
    {
		focusEstimator = new FocusEstimator();
		movementObservations = new List<string>();
		actions = new List<Action>();
		fuzzylator = new Fuzzylator();

		List<List<int>> prob1 = new List<List<int>>();
		prob1.Add(new List<int> { 0, 3, 3, 90, 0 });
		prob1.Add(new List<int> { 10, 0, 0, 90, 0 });
		prob1.Add(new List<int> { 10, 0, 0, 0, 90 });
		prob1.Add(new List<int> { 5, 0, 90, 0, 3 });
		prob1.Add(new List<int> { 3, 3, 0, 90, 3 });
		Action pickAndPlace = new Action("Pick and Place", prob1);

		List<List<int>> prob2 = new List<List<int>>();
		prob2.Add(new List<int> { 0, 5, 5, 45, 45 });
		prob2.Add(new List<int> { 10, 0, 0, 90, 0 });
		prob2.Add(new List<int> { 10, 0, 0, 0, 90 });
		prob2.Add(new List<int> { 5, 0, 5, 0, 90 });
		prob2.Add(new List<int> { 5, 5, 0, 90, 0 });
		Action use = new Action("Use", prob2);

		List<List<int>> prob3 = new List<List<int>>();
		prob3.Add(new List<int> { 0, 90, 3, 3, 3 });
		prob3.Add(new List<int> { 90, 0, 0, 10, 0 });
		prob3.Add(new List<int> { 90, 0, 0, 0, 10 });
		prob3.Add(new List<int> { 90, 0, 5, 0, 5 });
		prob3.Add(new List<int> { 90, 5, 0, 5, 0 });
		Action relocate = new Action("Relocate", prob3);

		actions.Add(pickAndPlace);
        actions.Add(use);
        actions.Add(relocate);

        CreateChains();
	}

    // Metodo per ricevere i risultati da Perception
    public void FocusEstimation(Dictionary<string, Dictionary<string, string>> results, Transform user, Dictionary<string, Vector3> objectPositions)
    {
		perceptionResults = results;

		// Utilizza FocusEstimator per assegnare valori numerici alle QSR
		Dictionary<string, Dictionary<string, float>> encodedResults = focusEstimator.QSREncoder(results);

        // Calcola i punteggi utilizzando l'equazione fornita
        Dictionary<string, float> scores = focusEstimator.CalculateScores(encodedResults, user, objectPositions);

        foreach (var element in scores)
        {
            if(element.Value >= 0.5f)
            {
                target = element.Key;
                targetScore = element.Value;
			}
        }
    }

    public void MovementsClassifier()
    {
        foreach (var element in perceptionResults)
        {
            if (element.Key == target)
            {
				var qsrs = new MovementClassifier.ModelInput()
				{
					QDC = element.Value["QDC"],
					QTC = element.Value["QTC"],
					MOS = element.Value["MOS"],
					HOLD = element.Value["HOLD"],
				};

				// Movement prediction
				identifiedMovement = MovementClassifier.Predict(qsrs).PredictedLabel;

                // Inserimento nella lista delle osservazioni dei movimenti solo se il movimento è diverso da quello immediatamente precedente
                if (identifiedMovement != null)
                {
					if (movementObservations.Count == 0 || identifiedMovement != movementObservations[movementObservations.Count - 1])
					{
						movementObservations.Add(identifiedMovement);
					}
					if (movementObservations.Count > observationsNumber)
					{
						movementObservations.Clear();
					}
				}
			}
        }
	}

	void CreateChains()
	{
        foreach (Action item in actions)
        {
			for (int s = 0; s < movements.Count; s++)
			{
				for (int i = 0; i < movements.Count; i++)
				{
					item.markovChain.Add(new[] { movements[s] }, movements[i], item.probabilities[s][i]);
				}
			}
		}
	}

	public void ActionPredictor()
	{
        string firstState = "";
		string movements = "";

		if (movementObservations.Count >= 2)
		{
			firstState = movementObservations.ElementAt(0);
			movementObservations.ForEach(mov => movements += mov);

			foreach (Action action in actions)
			{
				action.prediction = "";
				action.markovChain.Chain(new[] { firstState }).Take(observationsNumber).ToList().ForEach(state => action.prediction += state);
                action.score = fuzzylator.GetScore(movements, action.prediction);
                if (action.score > actionThreshold)
                {
                    if (action.name == "Use")
                    {
                        predictedAction = Contextualizer(action);

                    }
                    else if (action.name == "Pick and Place")
                    {
                        predictedAction = action.name + " " + target ;
                    }
                    else
                    {
                        predictedAction = action.name;
                    }

                    // Aggiungi l'azione alla lista di azioni osservate solo se essa è diversa dall'azione precedente.
                    if(observedActions.Count == 0 || predictedAction != observedActions[observedActions.Count - 1])
                    {
                        observedActions.Add(predictedAction);
                        if (observedActions.Contains("Relocate"))
                        {
                            observedActions.Remove("Relocate");
                        } 
                    }
                }   
			}
		}
	}

    private string Contextualizer(Action action)
    {
        switch (target) 
        {
            case "Sink":
               return "Wash";
			case "Microwave":
                return "Cook";
			case "Meal":
                return "Eat";
            case "Bisquits":
                return "Eat";
            case "Glass":
                return "Sip";
            default:
                return action.name;
		}
    }
}
