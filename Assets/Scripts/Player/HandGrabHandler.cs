using UnityEngine;

public class HandGrabHandler : MonoBehaviour
{
    private FixedJoint fixedJoint; // Guarda la unión física cuando agarramos algo
    private Transform playerRoot;  // Referencia al objeto raíz del jugador
    private Rigidbody playerRootRB; // Rigidbody principal del cuerpo para impulsarlo al trepar

    [SerializeField] private float fuerzaLanzamiento = 15f;
    [SerializeField] private float fuerzaSaltoTrepar = 8f;

    //alzar el brazo
    [SerializeField] private ConfigurableJoint hombroJoint;
    [SerializeField] private Vector3 anguloLevantar = new Vector3(-90f, 0f, 0f);
    [SerializeField] private float velocidadLevantar = 10f;

    private Quaternion rotacionInicial; // Guarda la pose natural del brazo al empezar

    private void Start()
    {
        // raíz del personaje y su Rigidbody principal
        playerRoot = transform.root;
        playerRootRB = playerRoot.GetComponent<Rigidbody>();

        // precision para que los objetos que se agarren o de lo que este agarrado no tiemble. 
        GetComponent<Rigidbody>().solverIterations = 255;

        //rotación original del hombro para saber cómo bajar el brazo luego
        if (hombroJoint != null)
        {
            rotacionInicial = hombroJoint.targetRotation;
        }
    }

    private void Update()
    {
        // calcula el ángulo levantado, si no vuelve al inicial
        if (hombroJoint != null)
        {
            Quaternion meta = rotacionInicial;

            if (Input.GetMouseButton(1))
            {
                meta = rotacionInicial * Quaternion.Euler(anguloLevantar);
            }

            hombroJoint.targetRotation = Quaternion.Slerp(hombroJoint.targetRotation, meta, Time.deltaTime * velocidadLevantar);
        }

        if (fixedJoint == null) return;

        if (Input.GetMouseButtonUp(1))
        {
            SoltarObjeto();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Rigidbody objetoAgarrado = fixedJoint.connectedBody; 
            SoltarObjeto(); 

            if (objetoAgarrado != null)
            {
                Vector3 direccion = (playerRoot.forward + Vector3.up * 0.5f).normalized;
                objetoAgarrado.AddForce(direccion * fuerzaLanzamiento, ForceMode.Impulse);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            playerRootRB.AddForce(Vector3.up * fuerzaSaltoTrepar, ForceMode.Impulse);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Input.GetMouseButton(1) && fixedJoint == null && collision.transform.root != playerRoot)
        {
            if (collision.collider.TryGetComponent(out Rigidbody otroRB))
            {
                fixedJoint = gameObject.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = otroRB; 
            }
        }
    }

    public void SoltarObjeto()
    {
        if (fixedJoint != null)
        {
            Destroy(fixedJoint); 
            fixedJoint = null;   
        }
    }
}