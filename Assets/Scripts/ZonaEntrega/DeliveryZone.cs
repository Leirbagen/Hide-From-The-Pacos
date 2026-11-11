using UnityEngine;
using TMPro;

public class DeliveryZone : MonoBehaviour
{
    [Header("Configuración de la Misión")]
    [Tooltip("Cuántos tragos debes llevar a la casa para ganar la partida")]
    public int tragosParaGanar = 1;
    private int tragosEntregados = 0;
    private bool partidaGanada = false;

    public TextMeshProUGUI textoContador; 
    public GameObject panelVictoria;      

    private void Start()
    {
        ActualizarUI();
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (partidaGanada) return;

        if (other.CompareTag("Trago"))
        {
            EntregarTrago(other.gameObject);
        }
    }

    private void EntregarTrago(GameObject tragoObj)
    {
        Rigidbody tragoRB = tragoObj.GetComponent<Rigidbody>();

        if (tragoRB != null)
        {
            HandGrabHandler[] manos = FindObjectsByType<HandGrabHandler>(FindObjectsSortMode.None);
            foreach (HandGrabHandler mano in manos)
            {
                mano.ForzarSoltarSiEs(tragoRB);
            }
        }

        tragosEntregados++;
        Destroy(tragoObj);

        ActualizarUI();

        if (tragosEntregados >= tragosParaGanar)
        {
            GanarPartida();
        }
    }

    private void ActualizarUI()
    {
        if (textoContador != null)
        {
            textoContador.text = $"Tragos en casa: {tragosEntregados} / {tragosParaGanar}";
        }
    }

    private void GanarPartida()
    {
        partidaGanada = true;

        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }

        PacoAI[] pacos = FindObjectsByType<PacoAI>(FindObjectsSortMode.None);
        foreach (PacoAI paco in pacos)
        {
            paco.enabled = false;
            if (paco.TryGetComponent(out UnityEngine.AI.NavMeshAgent agente))
            {
                agente.isStopped = true;
            }
        }
    }
}