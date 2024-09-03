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
    private float w1 = 0.8f; // Arbitrary weight
    private float w2 = 0.2f; // Arbitrary weight

    public FocusEstimator()
    {

    }

    public Dictionary<string, Dictionary<string, float>> QSREncoder(Dictionary<string, Dictionary<string, string>> results)
    {
        // Create a new dictionary for results coded with numeric values
        Dictionary<string, Dictionary<string, float>> encodedResults = new Dictionary<string, Dictionary<string, float>>();

        // Iterate through objects in the dictionary to be encoded
        foreach (var objectResult in results)
        {
            string objectName = objectResult.Key;
            Dictionary<string, float> encodedObjectQSRs = new Dictionary<string, float>();

            // Iterate through the QSRs for the current object
            foreach (var qsr in objectResult.Value)
            {
                string qsrType = qsr.Key;
                string qsrValue = qsr.Value;

                // Map the QSR value to a numerical value
                float numericValue = MapQSRToNumericValue(qsrType, qsrValue);

                // Add the coded result to the dictionary
                if (numericValue != -1.0f)
                {
                    encodedObjectQSRs.Add(qsrType, numericValue);
                }
                
            }

            // Add the result of the coded object to the main dictionary
            encodedResults.Add(objectName, encodedObjectQSRs);
        }

        // Return the full dictionary with coded results
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

        // If no match occurs, return a default value
        return -1.0f;
    }

    public Dictionary<string, float> CalculateScores(Dictionary<string, Dictionary<string, float>> encodedResults, Transform user, Dictionary<string, Vector3> objectPositions)
    {
        Dictionary<string, float> scores = new Dictionary<string, float>();

        foreach (var objectResult in encodedResults)
        {
            string objectName = objectResult.Key;

            // Get QDC and QTC numeric values from the coded dictionary
            float qdcValue = objectResult.Value.ContainsKey("QDC") ? objectResult.Value["QDC"] : 0.0f;
            float qtcValue = objectResult.Value.ContainsKey("QTC") ? objectResult.Value["QTC"] : 0.0f;

            // Get the location of the object
            Vector3 objectPosition = objectPositions[objectName];

            // Calculates the angle h between the line connecting the user to the object and the user's orientation
            Vector3 objPosOnPlane = new Vector3(objectPosition.x, user.position.y, objectPosition.z); //per avere i vettori alla stezza altezza
            Vector3 direction = Vector3.Normalize(objPosOnPlane - user.position);
            float angleH = Vector3.Angle(user.forward, direction);

            // Calculate the score S(i) using the equation given
            float score = (w1 * qdcValue + w2 * qtcValue) / (1 + angleH);

            // Add the score to the score dictionary
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

    // Method for receiving results from Perception
    public void FocusEstimation(Dictionary<string, Dictionary<string, string>> results, Transform user, Dictionary<string, Vector3> objectPositions)
    {
		perceptionResults = results;

        // Use FocusEstimator to assign numerical values to QSRs
        Dictionary<string, Dictionary<string, float>> encodedResults = focusEstimator.QSREncoder(results);

        // Calculate the scores using the equation provided
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

                // Entry in the list of movement observations only if the movement is different from the one immediately preceding it
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

                    // Add the action to the list of observed actions only if it is different from the previous action.
                    if (observedActions.Count == 0 || predictedAction != observedActions[observedActions.Count - 1])
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
