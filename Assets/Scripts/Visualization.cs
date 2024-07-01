using UnityEngine;

public class Visualization : MonoBehaviour
{
    [SerializeField]
    private GameObject userObject;

    [SerializeField]
    private Casper casper;

    [SerializeField]
    private float arrowLength = 1.0f;

    private void OnDrawGizmos()
    {
        if (casper == null)
        {
            Debug.LogWarning("Agent non assegnato. Collegare l'oggetto Agent nell'Inspector.");
            return;
        }

        if (userObject == null)
        {
            Debug.LogWarning("UserObject non assegnato. Collegare l'oggetto User nell'Inspector.");
            return;
        }

        // Disegna Gizmos per la zona di rilevamento dell'agente
        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawSphere(casper.transform.position, casper.GetDetectionRange());
        Gizmos.DrawWireSphere(casper.transform.position, casper.GetDetectionRange());

        // Disegna Gizmos per gli oggetti individuati dall'agente
        Gizmos.color = Color.green;
        foreach (Casper.DetectedObjectInfo objectInfo in casper.GetDetectedObjectsInfo())
        {
            Gizmos.DrawWireCube(objectInfo.position, Vector3.one * 0.2f);
            Gizmos.DrawLine(userObject.transform.position, objectInfo.position);
        }

        // Disegna Gizmos per la direzione del movimento dell'utente
        Gizmos.color = Color.red;
        Vector3 userDirection = GetUserDirection();
        Gizmos.DrawRay(userObject.transform.position, userDirection * arrowLength);
        
    }

    private Vector3 GetUserDirection()
    {
        // Calcola la direzione in avanti dell'utente
        return userObject.transform.forward;
    }
}    