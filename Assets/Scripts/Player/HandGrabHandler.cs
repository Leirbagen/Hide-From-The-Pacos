using UnityEngine;

public class HandGrabHandler : MonoBehaviour
{
    private Rigidbody rigidBody3D;
    private FixedJoint fixedJoint;
    private Transform playerRoot;
    private Rigidbody playerRootRB; // Referencia al Rigidbody principal para el salto

    [Header("Configuración de Interacción")]
    [SerializeField] private float fuerzaLanzamiento = 15f;
    [SerializeField] private float fuerzaSaltoTrepar = 8f; // Fuerza aplicada por CADA mano agarrada

    [Header("Animación Física (Alzar Brazo)")]
    [SerializeField] private ConfigurableJoint hombroJoint;
    [SerializeField] private Vector3 anguloLevantar = new Vector3(-90f, 0f, 0f);
    [SerializeField] private float velocidadLevantar = 10f;

    private Quaternion rotacionInicial;
    private Quaternion rotacionObjetivo;

    private void Start()
    {
        rigidBody3D = GetComponent<Rigidbody>();
        playerRoot = transform.root;
        playerRootRB = playerRoot.GetComponent<Rigidbody>(); // Obtenemos el Rigidbody central
        rigidBody3D.solverIterations = 255;

        if (hombroJoint != null)
        {
            rotacionInicial = hombroJoint.targetRotation;
            rotacionObjetivo = rotacionInicial;
        }
    }

    private void Update()
    {
        // 1. ANIMACIÓN FÍSICA
        if (hombroJoint != null)
        {
            if (Input.GetMouseButton(1))
            {
                rotacionObjetivo = rotacionInicial * Quaternion.Euler(anguloLevantar);
            }
            else
            {
                rotacionObjetivo = rotacionInicial;
            }
            hombroJoint.targetRotation = Quaternion.Slerp(hombroJoint.targetRotation, rotacionObjetivo, Time.deltaTime * velocidadLevantar);
        }

        // 2. LÓGICA DE AGARRE Y LANZAMIENTO
        if (Input.GetMouseButtonUp(1) && fixedJoint != null)
        {
            SoltarObjeto();
        }

        if (Input.GetMouseButtonDown(0) && fixedJoint != null)
        {
            LanzarObjeto();
        }

        // 3. LÓGICA DE TREPAR (Salto mientras se agarra)
        if (Input.GetKeyDown(KeyCode.Space) && fixedJoint != null)
        {
            // Impulsa el centro del personaje hacia arriba
            playerRootRB.AddForce(Vector3.up * fuerzaSaltoTrepar, ForceMode.Impulse);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Input.GetMouseButton(1) && fixedJoint == null)
        {
            if (collision.transform.root == playerRoot) return;

            if (collision.collider.TryGetComponent(out Rigidbody otroRB))
            {
                AgarrarObjeto(otroRB, collision.GetContact(0).point);
            }
        }
    }

    private void AgarrarObjeto(Rigidbody objetivoRB, Vector3 puntoDeContacto)
    {
        fixedJoint = gameObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = objetivoRB;
        fixedJoint.autoConfigureConnectedAnchor = false;
        fixedJoint.connectedAnchor = objetivoRB.transform.InverseTransformPoint(puntoDeContacto);
    }

    private void SoltarObjeto()
    {
        Destroy(fixedJoint);
        fixedJoint = null;
    }

    private void LanzarObjeto()
    {
        Rigidbody objetivoRB = fixedJoint.connectedBody;
        SoltarObjeto();

        if (objetivoRB != null)
        {
            Vector3 direccionLanzamiento = playerRoot.forward + (Vector3.up * 0.5f);
            objetivoRB.AddForce(direccionLanzamiento.normalized * fuerzaLanzamiento, ForceMode.Impulse);
        }
    }

    public void ForzarSoltarSiEs(Rigidbody objetivo)
    {
        if (fixedJoint != null && fixedJoint.connectedBody == objetivo)
        {
            SoltarObjeto();
        }
    }
}