# CASPER-MV
 First Unity Native Cognitive Architecture for Virtual Agents based on CASPER (Cognitive Architecture for Social Perception and Engagement in Robots)

Architecture modules are in the Scripts folder. 
To better understand modules please refer to 
Vinanzi, S., & Cangelosi, A. (2024). CASPER: Cognitive Architecture for Social Perception and Engagement in Robots. International Journal of Social Robotics, 1-19.
You can also use the original architecture here, installing Webots, here: https://github.com/samvinanzi/CASPER

## This version

### Description
The architecture is based on Qualitative Spatial Relations between the user and the objects of interest (OOI):
- Qualitative Distance Calculus (QDC): Far, medium, near, touch;
- Qualitative Trajectory Calculus (QTC): approaching or leaving;
- Moving or Stationary (MOS);
- Holding object or not (HOOLD);

The architecture is composed of four modules:
- Perception: uses information about detected objects to calculate QSRs;
- Low-Level: uses QSRs to identify a movement and a series of movements to identify the user's actions;
- High-Level: uses a sequence of actions to identify the user's goal;
- Supervisor: sends all information to GPT-4 to advise the user (You need your own AZURE OPENAI API paid account credentials to use this or you can change it with an API of your choice (llama, openai, groq, ecc)!!!!);

These modules correspond to four classes that you will find in the Scripts folder. Information is passed from one module to another through the CASPER Script in the GameObject Agent. 

### Test scene
The test scene is set in a kitchen. The Agent can recognize one of the following user goals:
Breakfast (get the cookies, go to the table, eat, take the plate to wash);
Lunck (take food from the refrigerator, put it in the microwave, take it to the table, eat, take the plate to wash);
Drink (take the bottle, drink from the glass, take the glass to wash);
Use the GameObject Capsule with the MoveTo script to move automatically (press L to perform the Lunch task, B for Breakfast, and D for Drink).

### Object detection
Object detection is done through an Overlap Sphere in the CASPER class. You are free to use another method such as raycasting.

### Perception Class
Vector operations are used to calculate QSRs in a QSREngine object. The results are stored as dictionaries through a QSRLibrary object.

### Low-Level Class
The Focus Estimator method identifies the user's action target.
The Movement Classifier method uses a Decision Tree made with Visual Studio's Model Builder to identify a movement based on combinations of QSRs. You can use any other decision tree from the Nuget package.
A sequence of movements is compared through the Ratcliff-Obershelp algorithm (the NuGet package is https://www.nuget.org/packages/PercolatorMatching/1.1.0) with three Markov-chain Finite State Machines (https://github.com/otac0n/markov) describing and action (a sequence of movement. The matrices describing the transition probabilities for FSMs from one state to another are easily modified in the Start method of the Low-Level class itself.

### High-Level Class
Uses the PlanningAI package (https://github.com/rubenwe/PlanningAI) to create a Goal Library for the previously mentioned tasks: Breakfast, Lunch, Drink. Feel free to add more tasks. Instructions for using the planner are in its link. 
A particular algorithm (described in CASPER's paper) in the GoalReasoner method compares a sequence of actions from the previous module with the plans in the Goal Library and chooses the most likely one.

### Supervisor Class
User and object positions, target, and goal are inserted as variables in a sentence describing the scene. The sentence is sent to a GPT-4 module through Azure OpenAI API. The initial prompt is in the inspector of the Agent GameObject in the Supervisor component. 
You need your own paid account credentials to use the GPT model. Then the model proposes a way to help the user in its task.
