using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PacoAI : MonoBehaviour
{
    public enum Estado { Patrullando, Persiguiendo, Buscando, ArrestoCompletado }
    public Estado estadoActual = Estado.Patrullando;

    [Header("Navegación y Velocidades")]
    public float radioDePatrulla = 15f;
    public float velocidadPatrulla = 1.8f;
    public float velocidadPersecucion = 3.2f; 
    public float velocidadBusqueda = 2.5f;
    private NavMeshAgent agente;

    [Header("Tiempos de Escape")]
    public float tiempoParaPerderJugador = 4f; // Segundos fuera de vista para que se rinda
    public float velocidadParpadeo = 8f;       // Velocidad del parpadeo naranja
    private float temporizadorBusqueda = 0f;
    private Vector3 ultimaPosicionVista;

    [Header("Configuración del Cono de Visión")]
    public float distanciaVision = 7f;
    [Range(10, 180)] public float anguloVision = 75f;
    public int resolucionCono = 30;
    public LayerMask capaObstaculos;
    public MeshFilter mallaConoFiltro;

    [Header("Colores del Cono")]
    public Color colorPatrulla = new Color(1f, 0.9f, 0f, 0.35f);   // Amarillo
    public Color colorAlerta = new Color(1f, 0f, 0f, 0.5f);        // Rojo
    public Color colorBusqueda = new Color(1f, 0.45f, 0f, 0.45f);  // Naranja

    [Header("Referencias del Jugador")]
    public Transform objetivoJugador;
    public PlayerController controladorJugador;

    private Mesh mallaVision;
    private Color colorActualCono;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        mallaVision = new Mesh();
        mallaVision.name = "MallaConoVision";

        if (mallaConoFiltro != null)
        {
            mallaConoFiltro.mesh = mallaVision;

            MeshRenderer rendererCono = mallaConoFiltro.GetComponent<MeshRenderer>();
            if (rendererCono == null)
            {
                rendererCono = mallaConoFiltro.gameObject.AddComponent<MeshRenderer>();
            }

            rendererCono.material = new Material(Shader.Find("Sprites/Default"));
            rendererCono.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rendererCono.receiveShadows = false;
        }

        colorActualCono = colorPatrulla;
        IrAPuntoAleatorio();
    }

    void Update()
    {
        if (estadoActual == Estado.ArrestoCompletado) return;

        bool viendoAlJugador = EstaViendoAlJugador();

        // 1. LÓGICA DE ESTADOS SEGÚN VISIÓN
        if (viendoAlJugador)
        {
            // Si lo ve (desde Patrulla o desde Búsqueda), lo persigue en ROJO
            estadoActual = Estado.Persiguiendo;
            ultimaPosicionVista = objetivoJugador.position;
            temporizadorBusqueda = tiempoParaPerderJugador;
            colorActualCono = colorAlerta;
        }
        else
        {
            // Si lo estaba persiguiendo y acaba de salir del cono o cubrirse tras una pared
            if (estadoActual == Estado.Persiguiendo)
            {
                estadoActual = Estado.Buscando;
                agente.SetDestination(ultimaPosicionVista); // Va al último lugar donde lo vio
            }
        }

        // 2. COMPORTAMIENTO DE CADA ESTADO
        if (estadoActual == Estado.Patrullando)
        {
            agente.speed = velocidadPatrulla;
            colorActualCono = colorPatrulla;

            if (!agente.pathPending && agente.remainingDistance < 0.5f)
            {
                IrAPuntoAleatorio();
            }
        }
        else if (estadoActual == Estado.Persiguiendo)
        {
            agente.speed = velocidadPersecucion;
            if (objetivoJugador != null)
            {
                agente.SetDestination(objetivoJugador.position);
            }
        }
        else if (estadoActual == Estado.Buscando)
        {
            agente.speed = velocidadBusqueda;
            temporizadorBusqueda -= Time.deltaTime;

            // Efecto de parpadeo naranja (alterna entre naranja brillante y naranja muy tenue)
            float onda = (Mathf.Sin(Time.time * velocidadParpadeo) + 1f) * 0.5f;
            Color naranjaTenue = new Color(colorBusqueda.r, colorBusqueda.g, colorBusqueda.b, 0.12f);
            colorActualCono = Color.Lerp(naranjaTenue, colorBusqueda, onda);

            // Si llega a la última posición vista, mira un poco alrededor y si se acaba el tiempo vuelve a patrullar
            if (temporizadorBusqueda <= 0f)
            {
                estadoActual = Estado.Patrullando;
                colorActualCono = colorPatrulla;
                IrAPuntoAleatorio();
            }
        }

        // 3. Dibujar el cono actualizado
        DibujarConoDeVision();
    }

    // Devuelve TRUE solo si el jugador está dentro del rango, dentro del ángulo y SIN paredes en medio
    bool EstaViendoAlJugador()
    {
        if (objetivoJugador == null) return false;

        Vector3 direccionAlJugador = (objetivoJugador.position - transform.position);
        direccionAlJugador.y = 0;
        float distancia = direccionAlJugador.magnitude;

        if (distancia > distanciaVision) return false;

        float angulo = Vector3.Angle(transform.forward, direccionAlJugador.normalized);
        if (angulo > anguloVision * 0.5f) return false;

        Vector3 origenRayo = transform.position + Vector3.up * 0.5f;
        Vector3 destinoRayo = objetivoJugador.position + Vector3.up * 0.5f;
        Vector3 dirRayo = (destinoRayo - origenRayo).normalized;
        float distanciaReal = Vector3.Distance(origenRayo, destinoRayo);

        // Si el rayo choca con una pared de la capa Obstaculos, NO lo está viendo
        if (Physics.Raycast(origenRayo, dirRayo, distanciaReal, capaObstaculos))
        {
            return false;
        }

        return true;
    }

    void IrAPuntoAleatorio()
    {
        Vector3 direccionAleatoria = Random.insideUnitSphere * radioDePatrulla;
        direccionAleatoria += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioDePatrulla, NavMesh.AllAreas))
        {
            agente.SetDestination(hit.position);
        }
    }

    void DibujarConoDeVision()
    {
        if (mallaConoFiltro == null) return;

        int pasos = Mathf.RoundToInt(resolucionCono);
        float pasoAngulo = anguloVision / pasos;

        Vector3[] vertices = new Vector3[pasos + 2];
        Color[] colores = new Color[vertices.Length];
        int[] triangulos = new int[pasos * 3];

        vertices[0] = Vector3.zero;
        colores[0] = colorActualCono;

        for (int i = 0; i <= pasos; i++)
        {
            float anguloActual = -anguloVision * 0.5f + pasoAngulo * i;
            Vector3 dirGlobal = Quaternion.Euler(0, anguloActual, 0) * transform.forward;
            Vector3 puntoFinal;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, dirGlobal, out hit, distanciaVision, capaObstaculos))
            {
                puntoFinal = mallaConoFiltro.transform.InverseTransformPoint(hit.point);
            }
            else
            {
                Vector3 puntoLejano = transform.position + dirGlobal * distanciaVision;
                puntoFinal = mallaConoFiltro.transform.InverseTransformPoint(puntoLejano);
            }

            vertices[i + 1] = puntoFinal;
            colores[i + 1] = colorActualCono;

            if (i < pasos)
            {
                triangulos[i * 3] = 0;
                triangulos[i * 3 + 1] = i + 1;
                triangulos[i * 3 + 2] = i + 2;
            }
        }

        mallaVision.Clear();
        mallaVision.vertices = vertices;
        mallaVision.triangles = triangulos;
        mallaVision.colors = colores;
        mallaVision.RecalculateNormals();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (estadoActual == Estado.ArrestoCompletado) return;

        PlayerController player = collision.transform.root.GetComponent<PlayerController>();
        if (player != null)
        {
            estadoActual = Estado.ArrestoCompletado;
            agente.isStopped = true;
            player.ArrestarJugador();
        }
    }
}