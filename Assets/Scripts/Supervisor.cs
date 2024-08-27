using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Azure;
using Azure.AI.OpenAI;

public class Supervisor : MonoBehaviour
{
	[SerializeField, TextArea] private string systemPrompt;

	// Get Azure OpenAI Service credentials
	Uri azureOpenAIResourceUri = new("https://casper.openai.azure.com/");
	AzureKeyCredential azureOpenAIApiKey = new("Your Azure Openai API Key");

	OpenAIClient client;
	ChatCompletionsOptions options;

	private void Start()
	{
		// Configure OpenAI client
		client = new(azureOpenAIResourceUri, azureOpenAIApiKey);

		// Configure Chat Completion options
		options = new ChatCompletionsOptions
		{
			DeploymentName = "Supervisor",
			MaxTokens = 400,
			Temperature = 1f,
			FrequencyPenalty = 0.0f,
			PresencePenalty = 0.0f,
			NucleusSamplingFactor = 0.95f // Top P
		};

		//Inizialize Chat History
		options.Messages.Add(new ChatRequestSystemMessage(systemPrompt));

	}

	public async void PlanGenerator(WorldState current, string target, string goal)
    {
		var userPrompt = $"User position: {current.userPositions.Values}, object target: {target}, goal: {goal}, objects: {current.objectPositions.Keys}";
		options.Messages.Add(new ChatRequestSystemMessage(userPrompt));
		var assistantResponse = await client.GetChatCompletionsAsync(options);
		var response = assistantResponse.Value.Choices[0].Message.Content;
		Debug.Log($"Assistant: {response}");
		options.Messages.Add(new ChatRequestAssistantMessage(response));
	}

    public void ProcessDirector()
    {
        // To be implemented;
    }
}
