using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrinkingMinigame : MonoBehaviour
{
    [Header("Elementos de Interfaz (UI)")]
    public GameObject promptText;
    public TextMeshProUGUI qteText;
    public Image pantallaFlash;

    [Header("Configuración del Minijuego")]
    public int longitudSecuencia = 3;
    public float tiempoPorLetra = 2f;
    public GameObject tragoPrefab;
    public Transform puntoDeSpawnTrago;

    [Header("Efectos Visuales (Pantalla)")]
    public Color colorExito = new Color(0f, 1f, 0f, 0.4f);
    public Color colorFallo = new Color(1f, 0f, 0f, 0.4f);
    public float duracionFlash = 0.5f; // Lo reduje a 0.5 para que los destellos continuos sean rápidos y dinámicos

    [Header("Efectos de Letras (Dinamismo)")]
    public float radioMovimiento = 100f; // Cuántos píxeles se puede alejar del centro
    public float multiplicadorEscala = 1.8f; // A qué tamaño "salta" la letra al acertar
    public float suavidadEscala = 15f; // Qué tan rápido vuelve al tamaño normal

    private bool jugadorEnZona = false;
    private bool minijuegoActivo = false;
    private int indiceSecuenciaActual = 0;
    private float temporizadorLetra = 0f;
    private KeyCode teclaActualRequerida;

    private float temporizadorFlash = 0f;
    private Color colorFlashActual;
    private Vector2 posicionOriginalLetra; // Para guardar el centro

    private KeyCode[] teclasPosibles = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Q, KeyCode.R, KeyCode.F };

    void Start()
    {
        promptText.SetActive(false);
        qteText.gameObject.SetActive(false);
        pantallaFlash.gameObject.SetActive(false);

        // Guardamos la posición central original del texto en la UI
        posicionOriginalLetra = qteText.rectTransform.anchoredPosition;
    }

    void Update()
    {
        if (jugadorEnZona && !minijuegoActivo && Input.GetKeyDown(KeyCode.E))
        {
            IniciarMinijuego();
            return;
        }

        if (minijuegoActivo)
        {
            ManejarQTE();
        }

        // Lógica del desvanecimiento de pantalla
        if (temporizadorFlash > 0)
        {
            temporizadorFlash -= Time.deltaTime;

            float alpha = (temporizadorFlash / duracionFlash) * colorFlashActual.a;
            Color c = pantallaFlash.color;
            c.a = alpha;
            pantallaFlash.color = c;

            if (temporizadorFlash <= 0)
            {
                pantallaFlash.gameObject.SetActive(false);
            }
        }

        // Lógica del "Latido" (Pop) de la letra
        if (qteText.gameObject.activeSelf)
        {
            // Reduce progresivamente la escala actual hacia Vector3.one (1, 1, 1)
            qteText.transform.localScale = Vector3.Lerp(qteText.transform.localScale, Vector3.one, Time.deltaTime * suavidadEscala);
        }
    }

    void IniciarMinijuego()
    {
        minijuegoActivo = true;
        promptText.SetActive(false);
        indiceSecuenciaActual = 0;

        temporizadorFlash = 0f;
        pantallaFlash.gameObject.SetActive(false);

        SiguienteLetra();
    }

    void SiguienteLetra()
    {
        if (indiceSecuenciaActual >= longitudSecuencia)
        {
            GanarMinijuego();
            return;
        }

        teclaActualRequerida = teclasPosibles[Random.Range(0, teclasPosibles.Length)];
        qteText.text = teclaActualRequerida.ToString();
        qteText.gameObject.SetActive(true);

        // 1. Aleatorizar posición basada en la original
        Vector2 desplazamientoAleatorio = Random.insideUnitCircle * radioMovimiento;
        qteText.rectTransform.anchoredPosition = posicionOriginalLetra + desplazamientoAleatorio;

        // 2. Darle el salto de escala (el Update se encargará de achicarlo suavemente)
        qteText.transform.localScale = Vector3.one * multiplicadorEscala;

        // Reiniciar transparencia
        Color c = qteText.color;
        c.a = 1f;
        qteText.color = c;

        temporizadorLetra = tiempoPorLetra;
    }

    void ManejarQTE()
    {
        temporizadorLetra -= Time.deltaTime;
        float alpha = temporizadorLetra / tiempoPorLetra;

        Color c = qteText.color;
        c.a = alpha;
        qteText.color = c;

        if (temporizadorLetra <= 0)
        {
            PerderMinijuego();
            return;
        }

        if (Input.anyKeyDown)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) return;

            if (Input.GetKeyDown(teclaActualRequerida))
            {
                // 3. Flash verde en CADA pulsación correcta
                MostrarFlash(colorExito);
                indiceSecuenciaActual++;
                SiguienteLetra();
            }
            else
            {
                PerderMinijuego();
            }
        }
    }

    void GanarMinijuego()
    {
        minijuegoActivo = false;
        qteText.gameObject.SetActive(false);

        // Ya no ponemos MostrarFlash aquí, porque se llamó justo antes al presionar la última tecla correcta.

        Instantiate(tragoPrefab, puntoDeSpawnTrago.position, Quaternion.identity);
    }

    void PerderMinijuego()
    {
        minijuegoActivo = false;
        qteText.gameObject.SetActive(false);

        MostrarFlash(colorFallo);

        Invoke("ReiniciarPrompt", 1.5f);
    }

    void MostrarFlash(Color color)
    {
        colorFlashActual = color;
        pantallaFlash.color = colorFlashActual;
        pantallaFlash.gameObject.SetActive(true);
        temporizadorFlash = duracionFlash;
    }

    void ReiniciarPrompt()
    {
        if (jugadorEnZona) promptText.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.GetComponent<PlayerController>() != null)
        {
            jugadorEnZona = true;
            if (!minijuegoActivo) promptText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root.GetComponent<PlayerController>() != null)
        {
            jugadorEnZona = false;
            promptText.SetActive(false);
            if (minijuegoActivo) PerderMinijuego();
        }
    }
}