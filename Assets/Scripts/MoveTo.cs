using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MoveTo : MonoBehaviour
{
    private NavMeshAgent agent;

    public Transform fridge;
    public Transform meal;
    public Transform plate;
    public Transform microwave;
    public Transform sink;
    public Transform hand;
    public Transform bisquits;
    public Transform bottle;
    public Transform glass;
    public Transform handOrigin;

    private Transform target;
    private Animator animator;
    public LowLevel lowLevel;

    private bool isWalking = false;
    private bool isCoroutineRunning = false;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isCoroutineRunning)
        {
            Walk();
            Look(target);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(Lunch());
            lowLevel.observedActions.Clear();
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            StartCoroutine(Breakfast());
            lowLevel.observedActions.Clear();
        } else if (Input.GetKeyDown(KeyCode.D))
        {
            StartCoroutine(Drink());
            lowLevel.observedActions.Clear();
        }
    }

    private IEnumerator WaitForAgent()
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }
    }

    public IEnumerator Lunch()
    {
        isCoroutineRunning = true;

        // Pick Meal
        GoTo(meal);
        yield return WaitForAgent();
        isWalking = false;
        Walk();
        animator.SetTrigger("open");
        yield return new WaitForSeconds(10);

        // Place meal to microwave
        GoTo(microwave);
        yield return WaitForAgent();
        isWalking = false;
        Walk();
        Place(microwave, meal);
        meal.gameObject.SetActive(false);

        // Cook
        animator.SetTrigger("using");
        yield return Use(microwave);
        yield return new WaitForSeconds(5);

        // Pick Meal
        meal.gameObject.SetActive(true);
        PickUp(meal);

        // Place Meal into plate
        GoTo(plate);
        yield return WaitForAgent();
        isWalking = false;
        Walk();
        Place(plate, meal, Vector3.up * 0.01f);
        animator.SetTrigger("sit");

        // Eat
        yield return Use(plate);
        meal.gameObject.SetActive(false);
        animator.SetTrigger("standUp");

        // Pick plate
        PickUp(plate);

        // Go to sink
        GoTo(sink);
        yield return WaitForAgent();
        isWalking = false;
        Walk();

        Place(sink, plate, Vector3.right * 0.1f);

        // Wash plate
        yield return Use(sink);

        isCoroutineRunning = false;
    }

    public IEnumerator Breakfast()
    {
        isCoroutineRunning = true;

        // Pick bisquits
        GoTo(bisquits);
        yield return WaitForAgent();
        isWalking = false;
        Walk();

        PickUp(bisquits);

        // Place bisquits into plate
        GoTo(plate);
        yield return WaitForAgent();
        isWalking = false;
        Walk();

        Place(plate, bisquits, Vector3.up * 0.1f);
        animator.SetTrigger("sit");

        // Eat
        yield return Use(plate);
        bisquits.gameObject.SetActive(false);
        animator.SetTrigger("standUp");

        // Pick plate
        PickUp(plate);

        // Go to sink
        GoTo(sink);
        yield return WaitForAgent();
        isWalking = false;
        Walk();

        Place(sink, plate, Vector3.right * 0.1f);

        // Wash
        yield return Use(sink);

        isCoroutineRunning = false;
    }

    public IEnumerator Drink()
    {
        isCoroutineRunning=true;

        // Pick bottle
        GoTo(bottle);
        yield return WaitForAgent();
        isWalking = false;
        Walk();

        // Drink
        yield return Use(glass);

        // Pick glass
        PickUp(glass);

        // Go to sink
        GoTo(sink);
        yield return WaitForAgent();
        isWalking = false;
        Walk();

        Place(sink, glass, Vector3.right * 0.1f);

        // Wash
        yield return Use(sink);

        isCoroutineRunning = false;
    }

    private void GoTo(Transform destination)
    {
        agent.SetDestination(destination.position);
        isWalking = true;
        Walk();
        this.target = destination;
    }

    private void Walk()
    {
        animator.SetBool("isWalking", isWalking);
    }

    public void TakeMeal()
    {
        PickUp(meal);
    }

    private void PickUp(Transform item)
    {
        if (item.gameObject.name == meal.gameObject.name)
        {
            this.target = fridge;
        }
        item.SetParent(hand);
        item.localPosition = Vector3.zero;
        item.localRotation = Quaternion.identity;
    }

    private void Place(Transform location, Transform item, Vector3 offset = default(Vector3))
    {
        item.SetParent(null);
        item.position = location.position + offset;
        item.localRotation = Quaternion.identity;
    }

    private IEnumerator Use(Transform item)
    {
        this.target = item;
        for (int i = 0; i < 10; i++) {
            hand.SetParent(item);
            hand.localPosition = Vector3.zero;
            hand.localRotation = Quaternion.identity;
            hand.localScale = Vector3.one;
            yield return new WaitForSeconds(0.5f);
            hand.SetParent(handOrigin);
            hand.localPosition = Vector3.zero;
            hand.localRotation = Quaternion.identity;
            hand.localScale = Vector3.one;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Look(Transform item)
    {
        agent.transform.LookAt(new Vector3(item.position.x, transform.position.y, item.position.z));
    }
}

