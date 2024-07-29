using Microsoft.Extensions.Azure;
using PlanningAi.Planning;
using PlanningAi.Planning.Actions;
using PlanningAi.Planning.Planners;
using PlanningAi.Utils;
using System;
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
		// Initialize P with the unmarked plans T in L
		Dictionary<string, List<string>> P = new Dictionary<string, List<string>>(goalLibrary);

		foreach (string s in sigma)
		{
			Debug.Log("Observation list: ("+string.Join(", ", sigma)+")");
			Debug.Log($"- Processing observation: {s}");

			// Initialize P′ to empty
			Dictionary<string, List<string>> Pprimo = new Dictionary<string, List<string>>();

			// while P is not empty do
			while (P.Count > 0)
			{
				// Pop p from P
				var plan = P.First();
				List<string> p = plan.Value;
				P.Remove(plan.Key);
				Debug.Log($"-- Processing plan: {plan.Key}");

				if (!p.Contains(s))
				{
					Debug.Log($"--- No match for plan {plan.Key}");
				}

				// foreach unobserved node n in p named σi do
				for (int n = 0; n < p.Count; n++)
				{
					if (p[n] == s)
					{
						Debug.Log($"--- Match found at position {n + 1}");
						Debug.Log($"---- ({string.Join(", ", plan.Value)})");
						// Generate a copy p′ of p
						List<string> pPrimo = new List<string>(p);
						pPrimo[n] = "observed";

						// if there are any unobserved nodes on the left of n in p′
						if (n != 0)
						{
							for (int n1 = 0; n1 < n; n1++)
							{
								// Mark them as missed
								if (pPrimo[n1] != "observed")
								{
									pPrimo[n1] = "missed";
								}
							}
						}
						// Insert p′ in P′
						Pprimo.Add(plan.Key + "(with " + s + " at position " + (n + 1) + ")", pPrimo);
					}
				}
			}
			Debug.Log($"- Explanations for action {s}: {Pprimo.Count}");
			Debug.Log("");
			// Insert P′ in P
			foreach (var item in Pprimo)
			{
				P.Add(item.Key, item.Value);
			}
		}

		Debug.Log("");
		// Calculating score for each plan
		Debug.Log("Calculating score for each remaining plan (observed * (1 - missed))");
		Dictionary<string, float> scores = new Dictionary<string, float>();
		Dictionary<string, float> normalizedScores = new Dictionary<string, float>();
		foreach (var p in P)
		{
			List<string> observed = p.Value.FindAll(n => n == "observed");
			List<string> missed = p.Value.FindAll(n => n == "missed");

			float observed100 = (float)observed.Count / (float)p.Value.Count;
			float missed100 = (float)missed.Count / (float)p.Value.Count;

			float score = observed100 * (1 - missed100);
			scores.Add(p.Key, score);

			Debug.Log($"{p.Key} score: {observed100} * (1 - {missed100}) = {score}");
		}
		Debug.Log("");
		Debug.Log("Normalizing scores");
		float scoresSum = scores.Values.Sum();
        Debug.Log($"Scores sum: {scoresSum}");
        foreach (var item in scores)
		{
			float normalizedScore = item.Value / scoresSum;
			normalizedScores.Add(item.Key, normalizedScore);
			Debug.Log($"{item.Key} normalized score: {item.Value}/{scoresSum} = {normalizedScore}");
		}
		Debug.Log("");
		if(normalizedScores.Count > 0)
		{
            predictedGoal = normalizedScores.MaxBy(entry => entry.Value).Key;
            Debug.Log($"The predicted goal is: {predictedGoal} at {normalizedScores.MaxBy(entry => entry.Value).Value * 100}%");
		}
		else
		{
            predictedGoal = "Unpredicted";
            Debug.Log($"The predicted goal is: {predictedGoal}");
        }
    }
}


// Actions
public class PickAndPlace : DomainActionBase
{
	public readonly string _target;
    public override string ActionName => $"Pick and Place {_target}";
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
    public override string ActionName => $"Pick and Place {_target}";
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
