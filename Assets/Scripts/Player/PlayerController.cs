using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody rigidBody3D;
    [SerializeField] private ConfigurableJoint mainJoint;

    [Header("UI")]
    [SerializeField] private Slider barraCansancio;
    [SerializeField] private GameObject panelGameOver;

    //movimiento
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float walkForce = 40f;
    [SerializeField] private float runForce = 70f;

    //ragdoll
    [SerializeField] private float standSpringForce = 100f;
    [SerializeField] private float crouchSpringForce = 5f;
    [SerializeField] private float standUpSmoothness = 4f;

    //tropiezo
    [SerializeField] private float maxRunTime = 3f;
    [SerializeField] private float tripDuration = 5f;

    private Vector2 moveInputVector;
    private bool isJumpButtonPressed;
    private bool isGrounded;
    private bool isRunning;
    private bool isCrouching;
    private bool isTripped;
    private bool isArrested;

    private float currentActiveSpeed;
    private float currentActiveForce;
    private float currentSpringForce;
    private float runTimer;
    private float tripTimer;

    private void Start()
    {
        currentActiveSpeed = walkSpeed;
        currentActiveForce = walkForce;
        currentSpringForce = standSpringForce;
    }

    private void Update()
    {
        if (isArrested) return;

        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");
        moveInputVector = Vector2.ClampMagnitude(moveInputVector, 1f); // Evita que corra más rápido en diagonal

        if (Input.GetKeyDown(KeyCode.Space)) isJumpButtonPressed = true;
        isRunning = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.C);

        // verifica si se canso o no
        if (isTripped)
        {
            tripTimer -= Time.deltaTime;
            if (tripTimer <= 0f)
            {
                isTripped = false;
            }
        }
        else
        {
            // si corre y se esta moviendo, que suba el valor y se canse
            if (isRunning && moveInputVector.magnitude > 0f)
            {
                runTimer += Time.deltaTime;
                //si supera el tiempo permitido de correr se cae
                if (runTimer >= maxRunTime)
                {
                    isTripped = true;
                    tripTimer = tripDuration;
                    runTimer = 0f;
                }
            }
            else
            {
                // si no que recupere
                if (runTimer > 0f)
                {
                    runTimer -= Time.deltaTime;
                }
            }
        }

        if (barraCansancio != null)
        {
            if (isTripped)
            {
                //mientras esta en el suelo, que se vaya llenando de nuevo la barra
                barraCansancio.value = 1f - (tripTimer / tripDuration);
            }
            else
            {
                // si corre que se disminuya la barra
                barraCansancio.value = 1f - (runTimer / maxRunTime);
            }
        }
    }

    private void FixedUpdate()
    {
        bool isDown = isCrouching || isTripped || isArrested;

        if (isDown)
        {
            currentSpringForce = Mathf.Lerp(currentSpringForce, crouchSpringForce, Time.fixedDeltaTime * 15f);
            mainJoint.angularXMotion = ConfigurableJointMotion.Free;
            mainJoint.angularZMotion = ConfigurableJointMotion.Free;
        }
        else
        {
            currentSpringForce = Mathf.Lerp(currentSpringForce, standSpringForce, Time.fixedDeltaTime * standUpSmoothness);

            if (currentSpringForce > standSpringForce * 0.8f)
            {
                mainJoint.angularXMotion = ConfigurableJointMotion.Locked;
                mainJoint.angularZMotion = ConfigurableJointMotion.Locked;
            }
        }

        JointDrive drive = mainJoint.slerpDrive;
        drive.positionSpring = currentSpringForce;
        mainJoint.slerpDrive = drive;

        isGrounded = Physics.Raycast(rigidBody3D.position, Vector3.down, 0.6f);
        if (!isGrounded)
        {
            rigidBody3D.AddForce(Vector3.down * 10f);
        }

        if (moveInputVector.magnitude > 0f && !isArrested)
        {
            Vector3 direccionMirada = new Vector3(moveInputVector.x, 0f, -moveInputVector.y);
            Quaternion rotacionMeta = Quaternion.LookRotation(direccionMirada);
            mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, rotacionMeta, Time.fixedDeltaTime * 300f);
        }

        float targetSpeed = walkSpeed;
        float targetForce = walkForce;

        if (isDown)
        {
            targetSpeed = 2f;
            targetForce = 15f;
        }
        else if (isRunning)
        {
            targetSpeed = runSpeed;
            targetForce = runForce;
        }

        Vector3 velocidadPlana = new Vector3(rigidBody3D.linearVelocity.x, 0f, rigidBody3D.linearVelocity.z);

        if (velocidadPlana.magnitude < targetSpeed)
        {
            rigidBody3D.AddForce(transform.forward * moveInputVector.magnitude * targetForce);
        }

        if (isJumpButtonPressed)
        {
            if (isGrounded && !isDown)
            {
                rigidBody3D.AddForce(Vector3.up * 10f, ForceMode.Impulse);
            }
            isJumpButtonPressed = false;
        }
    }

    public void ArrestarJugador()
    {
        if (isArrested) return;
        isArrested = true;
        isRunning = false;
        moveInputVector = Vector2.zero;

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;                  
        }
    }
}