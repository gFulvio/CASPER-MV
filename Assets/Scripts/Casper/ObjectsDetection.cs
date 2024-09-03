using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectsDetection : MonoBehaviour
{
    public List<GameObject> objects = new List<GameObject>();
    //public List<GameObject> oggetti = new List<GameObject>();
    public Collider[] hitCollider;
    RaycastHit hit;
    public int radius = 2;
    // Start is called before the first frame update
    void Start()
    {
        Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity);
        Debug.Log(hit.collider.name);
        objects.Add(hit.collider.gameObject);
        
        /*foreach(var hitCollider in hitCollider)
        {
            oggetti.Add(hitCollider.gameObject);
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        hitCollider = Physics.OverlapSphere(transform.position, radius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        //Use the same vars you use to draw your Overlap SPhere to draw your Wire Sphere.
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
