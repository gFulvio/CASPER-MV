using Microsoft.Extensions.Azure;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Planners;
using PlanningAi.Utils;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.Rendering.ReloadAttribute;

public class HighLevel : MonoBehaviour
{
	private Dictionary<string, DomainState> goalStates;
	private List<IDomainAction> actions;
    private Dictionary<string, List<string>> goalLibrary;

    public string predictedGoal;

    // Start is called before the first frame update
    void Start()
    {
		goalStates = new Dictionary<string, DomainState>();
		goalLibrary = new Dictionary<string, List<string>>();

        // Creazione dello stato iniziale
        DomainState currentState = DomainState.Empty
			.Set("Breakfast", false)
			.Set("Drink", false)
			.Set("Lunch", false)
			.Set("Clean", true)
			.Set("Warm", false)
			.Set("PrepareMeal", false);

		// Definizione dei Goal e dei subgoal
		DomainState breakfastGoal = DomainState.Empty
			.Set("Breakfast", true) // Goal
			.Set("Clean Plate", true); // SubGoal

		DomainState drinkGoal = DomainState.Empty
			.Set("Drink", true) // Goal
			.Set("Clean Glass", true); // SubGoal

		DomainState lunchGoal = DomainState.Empty
			.Set("Lunch", true) // Goal
			.Set("PrepareMeal", true) // SubGoal
			.Set("Warm", true) // SubSubGoal
			.Set("Clean Plate", true); // SubGoal

		// Aggiunta degli stati goal alla lista goalStates
		goalStates.Add("Breakfast", breakfastGoal);
		goalStates.Add("Drink", drinkGoal);
		goalStates.Add("Lunch", lunchGoal);

		// Creazione della lista delle azioni
		actions = new List<IDomainAction>
		{
			new PickAndPlace("Plate"),
			new PickAndPlace("Bottle"),
			new PickAndPlace("Meal"),
			new PickAndPlace("Bisquits"),
			new PickAndPlace("Glass"),
			new PrepareMeal("Meal"),
			new Cook(),
			new Eat("Bisquits"),
			new Eat("Meal"),
			new Sip(),
			new Wash("Plate"),
			new Wash("Glass")
		};

		// Creazione dei piani e inserimento nella libreria
		foreach (var goalState in goalStates)
		{
			CreatePlan(currentState, goalState.Key, goalState.Value, actions);
		}
	}

	public void CreatePlan(DomainState current, string goalName, DomainState goal, List<IDomainAction> actns)
	{
		// Crea i goal plans
		var planner = PlannerFactory.CreatePlanner();
		var actionSet = new ActionSet(actns);
		var plan = planner.GetPlan(current, goal, actionSet);
		List<string> plan2 = new List<string>();

		if (plan.Success)
		{
			foreach (var action in plan.Plan)
			{
				plan2.Add(action.ActionName);
			}

			if(!goalLibrary.ContainsKey(goalName))
			{
				goalLibrary.Add(goalName, plan2);
			}
		}
	}

	public void GoalR(List<string> sigma)
	{
		Debug.Log("Start Reasoning.");
        // Initialize P with the unmarked plans T in L
        Dictionary<string, List<string>> P = new Dictionary<string, List<string>>(goalLibrary);

        List<string> Pkeys = P.Keys.ToList();
        Debug.Log("PKeys count: " + Pkeys.Count);

		float scoresSum = 0;

        // Verifica il contenuto di P
        foreach (var entry in P)
        {
            Debug.Log("Initial P - Key: " + entry.Key + ", Value: " + string.Join(", ", entry.Value));
        }

        Debug.Log("Observations: " + sigma.Count + " with elements: " + string.Join(", ", sigma));
        // foreach s in sigma
        foreach (string s in sigma)
        {
            Debug.Log("Processing sigma element: " + s);

            // Initialize P' to empty
            Dictionary<string, List<string>> Pprimo = new Dictionary<string, List<string>>();

			// while P is not empty pop p from P
            foreach (string key in Pkeys)
			{
                
                List<string> p = P[key];
                Debug.Log("Processing plan: " + key + " with elements: " + string.Join(", ", p));

                // foreach unobserved node n in p named s
                if (p.Exists(n => n == s))
				{
					Debug.Log("Match found");
                    // Generate a copy p' of p
                    List<string> pPrimo = new List<string>(p);

                    // Mark n as observed in p'
                    for (int n = 0; n < p.Count; n++)
					{
						if (p[n] == s)
						{
							pPrimo[n] = "observed";
						}
					}

                    // Insert p' in P'
                    Pprimo.Add(key, pPrimo);
                }
            }

            // Insert P' in P
            foreach (string key in Pprimo.Keys)
			{
				P[key] = Pprimo[key];
			}
        }

		// Claculate the score for each plan
		Dictionary<string, float> scores = new Dictionary<string, float>();
		foreach(var p in P)
		{
			float score = 0;
			List<string> observed = p.Value.FindAll(n => n == "observed");
			List<string> missed = new List<string>();
			if(observed.Count > 0)
			{
                int nIndex = p.Value.FindLastIndex(n => n == "observed");
                List<string> missing = p.Value.GetRange(0, nIndex);
				missed = missing.FindAll(m => m != "observed");
            }
			score = ((float)observed.Count/((float)p.Value.Count) * (1 - ((float)missed.Count/(float)p.Value.Count)));
			Debug.Log(p.Value.Count);
			Debug.Log($"Punteggio per {p.Key} = {(float)observed.Count / (float)p.Value.Count} * (1 - {(float)missed.Count / (float)p.Value.Count}) = {score}");
			scores.Add(p.Key, score);
		}

        scoresSum = scores.Sum(score => score.Value);

		List<string> keys = scores.Keys.ToList();
		foreach(string key in keys)
		{
			scores[key] /= scoresSum;

			Debug.Log(key + ": " + scores[key]);
		}

        // Restituisci il piano con probabilità maggiore
        predictedGoal = scores.MaxBy(entry => entry.Value).Key;
		Debug.Log("Predicted goal: " + predictedGoal);

		Debug.Log("End Reasoning");
    }
}


// Actions
public class PickAndPlace : DomainActionBase
{
	public readonly string _target;
    public override string ActionName => "Pick and Place";
    public string stringName = "Pick and Place";
	public PickAndPlace(string target)
	{
		_target = target;
		Effects.Add("Transport", target);
	}
}

public class PrepareMeal : DomainActionBase
{
    public readonly string _target;
    public override string ActionName => "Pick and Place";
    public string stringName = "Pick and Place";

	public PrepareMeal(string target)
	{
        _target = target;
		Preconditions.Add("Warm", true);
        Effects.Add("Transport", target);
		Effects.Add("PrepareMeal", true);
    }
}

public class Eat : DomainActionBase
{
    public readonly string _target;
	public string stringName = "Eat";

	public Eat(string target)
	{
		_target = target;
		if (target == "Meal")
		{
			Preconditions.Add("PrepareMeal", true);
			Effects.Add("Transport", false);
			Effects.Add("Lunch", true);
		}
		if (target == "Bisquits")
		{
            Preconditions.Add("Transport", "Bisquits");
            Effects.Add("Transport", false);
            Effects.Add("Breakfast", true);
        }
		Effects.Add("Clean Plate", false);
	}
}

public class Sip : DomainActionBase
{
	public string stringName = "Sip";
	public Sip()
	{
		Preconditions.Add("Transport", "Bottle");
        Effects.Add("Transport", false);
        Effects.Add("Drink", true);
		Effects.Add("Clean Glass", false);
	}
}

public class Wash : DomainActionBase
{
	public readonly string _target;
	public override string ActionName => "Wash";
	public string stringName = "Wash";
	public Wash(string target)
	{
		_target = target;
		Preconditions.Add("Transport", target);
        Effects.Add("Transport", false);
        Effects.Add($"Clean {target}", true);
	}
}

public class Cook : DomainActionBase
{
	public string stringName = "Cook";
	public Cook()
	{
		Preconditions.Add("Transport", "Meal");
        Effects.Add("Transport", false);
        Effects.Add("Warm", true);
	}
}
