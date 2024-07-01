using System.Collections.Generic;
using UnityEngine;


internal class QSRLibrary
{
    // Dizionario che contiene, per ogni oggetto, un dizionario di tutte le QSR sin dall'inizio dell'attività
    private Dictionary<string, Dictionary<string, string>> library = new Dictionary<string, Dictionary<string, string>>();

    /// <summary>
    /// Costruttore vuoto
    /// </summary>
    public QSRLibrary() { }

	/// <summary>
	/// // Aggiunge uno stato QSR all'oggetto specificato nella libreria
	/// </summary>
	/// <param name="objectName">Il nome dell'oggetto</param>
	/// <param name="qsrState">I qsr relativi all'oggetto</param>
	public void AddQSRState(string objectName, Dictionary<string, string> qsrState)
    {
        if (!library.ContainsKey(objectName))
        {
            library[objectName] = new Dictionary<string, string>();
        }

        foreach (var qsr in qsrState)
        {
            library[objectName][qsr.Key] = qsr.Value;
        }
    }

	/// <summary>
	/// Restituisce tutte le QSR associate a un oggetto dall'inizio dell'attività
	/// </summary>
	/// <param name="objectName">Nome dell'oggetto</param>
	/// <returns>Restituisce un dizionario con tutti i qsr relativi all'oggetto. Restituisce un dizionario vuoto se l'oggetto non è presente</returns>
	public Dictionary<string, string> GetAllQSRsForObject(string objectName)
    {
        if (library.ContainsKey(objectName))
        {
            return library[objectName];
        }

        return new Dictionary<string, string>();
    }

	/// <summary>
	/// Restituisce l'ultima descrizione QSR associata a un oggetto presente nella libreria
	/// </summary>
	/// <param name="objectName">Nome dell'oggetto</param>
	/// <returns>Restituisce un dizionario con gli ultimi qsr relativi all'oggetto. Restituisce un dizionario vuoto se l'oggetto non è presente</returns>
	public Dictionary<string, string> GetLastQSRStateForObject(string objectName)
    {
        if (library.ContainsKey(objectName))
        {
            return library[objectName];
        }

        return new Dictionary<string, string>();
    }

}

/// <summary>
/// Classe QSREngine all'interno del modulo Perception
/// </summary>
internal class QSREngine
{
    private Vector3 previousUserPosition;

	/// <summary>
	/// Costruttore della classe. Inizializza la posizione precedente con la posizione iniziale
	/// </summary>
	public QSREngine()
    {
        previousUserPosition = Vector3.zero;
    }

	/// <summary>
	/// Metodo per calcolare la distanza tra l'utente e l'oggetto (QDC)
	/// </summary>
	/// <param name="objectPosition">Posizione dell'oggetto</param>
	/// <param name="userPosition">Posizione dell'utente</param>
	/// <returns>Restituisce una stringa che indica la distanza tra l'utente e l'oggetto</returns>
	public string CalculateQDC(Vector3 objectPosition, Vector3 userPosition)
    {
        float distance = Vector3.Distance(objectPosition, userPosition);

        if (distance >= 0.0f && distance <= 0.6f)
            return "Touch";
        else if (distance > 0.6f && distance <= 2.0f)
            return "Near";
        else if (distance > 2.0f && distance <= 3.0f)
            return "Medium";
        else if (distance > 3.0f && distance <= 5.0f)
            return "Far";
        else
            return "Ignoring";
    }

	/// <summary>
	/// Metodo per calcolare la traiettoria dell'utente rispetto agli OOI considerando il prodotto tra vettori
	/// </summary>
	/// <param name="userStartPos">Posizione iniziale dell'utente</param>
	/// <param name="userEndPos">Posizione finale dell'utente</param>
	/// <param name="objectPos">Posizione dell'oggetto</param>
	/// <returns>Una stringa che indica se l'utente si sta avvicinando o allontanando dall'oggetto</returns>
	public string CalculateQTC(Transform user, Vector3 objectPos)
    {
        Vector3 objectDirection = (objectPos - user.position).normalized;
        Vector3 userMovementDirection = (user.forward).normalized;
        float dotProduct = Vector3.Dot(userMovementDirection, objectDirection);

        if (dotProduct > 0)
        {
            return "Approaching";
        }
        else
        {
            return "Leaving";
        }
    }

    /// <summary>
    /// Calcola se l'utente è in movimento o stazionario.
    /// </summary>
    /// <param name="userEndPos">Ultima posizone dell'utente</param>
    /// <returns>Una stringa che indica se l'utente è o meno in movimento</returns>
    public string CalculateMOS(Vector3 userEndPos)
    {
        float velocityThreshold = 0.01f;  // Imposta la soglia di velocità

        // Calcola la velocità in base alla distanza tra la posizione corrente e precedente
        float velocity = Vector3.Distance(userEndPos, previousUserPosition);

        // Aggiorna la posizione precedente
        previousUserPosition = userEndPos;

        // Verifica se la velocità supera la soglia
        if (velocity > velocityThreshold)
        {
            return "Moving";
        }
        else
        {
            return "Stationary";
        }
    }

    /// <summary>
    /// Verifica se l'utente tiene in mano un oggetto o no. DA IMPLEMENTARE
    /// </summary>
    /// <returns>Si o no</returns>
    public string CalculateHOLD(Vector3 objectPos, Vector3 handPos)
    {
        if(Vector3.Distance(objectPos, handPos) == 0)
        {
            return "Yes";
        }
        else
        {
            return "No";
        }
    }
}

/// <summary>
/// Classe principale Perception
/// </summary>
public class Perception : MonoBehaviour
{
    private QSREngine currentQSR;
    private QSRLibrary qsrlibrary;
    private WorldState currentWorldState;
    public Transform hand;

    [System.Serializable]
    public class QSREngineResults
    {
        public string name;
        public Vector3 position;
        public string qdc;
        public string qtc;
        public string mos;
        public string hold;
    }

    public List<QSREngineResults> qsrEngineResults;

    private void Start()
    {
        currentQSR = new QSREngine();
        qsrlibrary = new QSRLibrary();
        qsrEngineResults = new List<QSREngineResults>();
    }

	/// <summary>
	/// Avvia il calcolo delle QSRs
	/// </summary>
	/// <param name="worldState">Lo stato del mondo con le posizioni dell'utente e degli oggetti rilevati</param>
	public void StartPerception(WorldState worldState, Transform user)
    {
		currentWorldState = worldState;

        string mosResult = currentQSR.CalculateMOS(currentWorldState.userPositions["endPos"]);

        qsrEngineResults.Clear();

        foreach (var objectPosition in currentWorldState.objectPositions)
        {
            Dictionary<string, string> objectQSRs = new Dictionary<string, string>();

            string qdcResult = currentQSR.CalculateQDC(objectPosition.Value, currentWorldState.userPositions["endPos"]);
            string qtcResult = currentQSR.CalculateQTC(user, objectPosition.Value);
            string holdResult = currentQSR.CalculateHOLD(objectPosition.Value, hand.position);

            // Aggiornamento della libreria e visualizzazione nell'Inspector
            objectQSRs.Add("QDC", qdcResult);
            if(mosResult == "Stationary")
            {
                objectQSRs.Add("QTC", mosResult);
            } else
            {
                objectQSRs.Add("QTC", qtcResult);
            }
            objectQSRs.Add("MOS", mosResult);
            objectQSRs.Add("HOLD", holdResult);

            QSREngineResults currentObject = new QSREngineResults();
            currentObject.name = objectPosition.Key;
            currentObject.position = objectPosition.Value;
            currentObject.qdc = qdcResult;
            currentObject.qtc = qtcResult;
            currentObject.mos = mosResult;
            currentObject.hold = holdResult;
            qsrEngineResults.Add(currentObject);

            qsrlibrary.AddQSRState(objectPosition.Key, objectQSRs);
        }
    }

    /// <summary>
    /// Ottiene gli utlimi risultati aggiunti alla libreria
    /// </summary>
    /// <returns>QSRs associate a ogni oggetto</returns>
    public (Dictionary<string, Dictionary<string, string>>, Dictionary<string, Vector3>) GetLastResultsForObjects()
    {
        Dictionary<string, Dictionary<string, string>> qsrResults = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, Vector3> objectPositions = new Dictionary<string, Vector3>();

        // Aggiungi gli ultimi risultati QSR e le posizioni per ogni oggetto
        foreach (var objectPosition in currentWorldState.objectPositions)
        {
            qsrResults[objectPosition.Key] = qsrlibrary.GetLastQSRStateForObject(objectPosition.Key);
            objectPositions[objectPosition.Key] = objectPosition.Value;
        }

        return (qsrResults, objectPositions);
    }


}