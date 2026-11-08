using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody rigidBody3D;
    [SerializeField] private ConfigurableJoint mainJoint;

    [Header("Interfaz de Usuario (UI)")]
    [SerializeField] private Slider barraCansancio;

    [Header("Configuración de Movimiento")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float walkForce = 40f;
    [SerializeField] private float runForce = 70f;
    [SerializeField] private float accelerationSmoothness = 5f;

    [Header("Configuración de Postura (Ragdoll)")]
    [SerializeField] private float standSpringForce = 100f;
    [SerializeField] private float crouchSpringForce = 5f;
    [SerializeField] private float standUpSmoothness = 4f;

    [Header("Configuración de Tropiezo")]
    [SerializeField] private float maxRunTime = 3f;
    [SerializeField] private float tripDuration = 5f;

    private Vector2 moveInputVector = Vector2.zero;
    private bool isJumpButtonPressed = false;
    private bool isGrounded = false;
    private RaycastHit[] raycastHits = new RaycastHit[10];

    private bool isRunning = false;
    private bool isCrouching = false;
    private bool isTripped = false;

    private float currentActiveSpeed;
    private float currentActiveForce;
    private float currentSpringForce;
    private float runTimer = 0f;
    private float tripTimer = 0f;

    private void Start()
    {
        currentActiveSpeed = walkSpeed;
        currentActiveForce = walkForce;
        currentSpringForce = standSpringForce;
    }

    private void Update()
    {
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");
        moveInputVector = Vector2.ClampMagnitude(moveInputVector, 1f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isJumpButtonPressed = true;
        }

        isRunning = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.C);

        // LÓGICA DE TROPIEZO Y CANSANCIO
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
            if (isRunning && moveInputVector.magnitude > 0)
            {
                runTimer += Time.deltaTime;
                if (runTimer >= maxRunTime)
                {
                    isTripped = true;
                    tripTimer = tripDuration;
                    runTimer = 0f;
                }
            }
            else
            {
                if (runTimer > 0)
                {
                    runTimer -= Time.deltaTime;
                }
            }
        }

        // ACTUALIZACIÓN VISUAL DE LA BARRA (Invertida como Energía)
        if (barraCansancio != null)
        {
            if (isTripped)
            {
                // Al estar caído, la barra empieza vacía (0) y se llena hasta 1 para levantarse
                barraCansancio.value = 1f - (tripTimer / tripDuration);
            }
            else
            {
                // Empieza llena (1) y se vacía hacia 0 mientras corre
                barraCansancio.value = 1f - (runTimer / maxRunTime);
            }
        }
    }

    private void FixedUpdate()
    {
        bool isDown = isCrouching || isTripped;

        // 1. LÓGICA DE POSTURA
        float targetSpring = isDown ? crouchSpringForce : standSpringForce;
        float smoothness = isDown ? 15f : standUpSmoothness;

        currentSpringForce = Mathf.Lerp(currentSpringForce, targetSpring, Time.fixedDeltaTime * smoothness);

        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = currentSpringForce;
        mainJoint.slerpDrive = jointDrive;

        // Liberar y bloquear ejes para evitar el salto
        if (isDown)
        {
            mainJoint.angularXMotion = ConfigurableJointMotion.Free;
            mainJoint.angularZMotion = ConfigurableJointMotion.Free;
        }
        else
        {
            // Solo bloqueamos los ejes cuando el personaje ya se haya levantado casi por completo (80% de la fuerza)
            if (currentSpringForce > standSpringForce * 0.8f)
            {
                mainJoint.angularXMotion = ConfigurableJointMotion.Locked;
                mainJoint.angularZMotion = ConfigurableJointMotion.Locked;
            }
        }

        // Comprobación de suelo
        isGrounded = false;
        int numberOfHits = Physics.SphereCastNonAlloc(rigidBody3D.position, 0.1f, Vector3.down, raycastHits, 0.5f);

        for (int i = 0; i < numberOfHits; i++)
        {
            if (raycastHits[i].transform.root == transform) continue;
            isGrounded = true;
            break;
        }

        if (!isGrounded)
        {
            rigidBody3D.AddForce(Vector3.down * 10f);
        }

        // 2. LÓGICA DE MOVIMIENTO Y ROTACIÓN
        float inputMagnitude = moveInputVector.magnitude;

        if (inputMagnitude > 0)
        {
            Vector3 decideDirection = new Vector3(moveInputVector.x, 0, -moveInputVector.y);
            Quaternion targetRotation = Quaternion.LookRotation(decideDirection, Vector3.up);
            mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, targetRotation, Time.fixedDeltaTime * 300f);
        }

        float targetMaxSpeed = (isRunning && !isDown) ? runSpeed : walkSpeed;
        float targetForce = (isRunning && !isDown) ? runForce : walkForce;

        if (isDown)
        {
            targetMaxSpeed = 2f;
            targetForce = 15f;
        }

        currentActiveSpeed = Mathf.Lerp(currentActiveSpeed, targetMaxSpeed, Time.fixedDeltaTime * accelerationSmoothness);
        currentActiveForce = Mathf.Lerp(currentActiveForce, targetForce, Time.fixedDeltaTime * accelerationSmoothness);

        Vector3 flatVelocity = new Vector3(rigidBody3D.linearVelocity.x, 0, rigidBody3D.linearVelocity.z);

        if (flatVelocity.magnitude < currentActiveSpeed)
        {
            rigidBody3D.AddForce(transform.forward * inputMagnitude * currentActiveForce);
        }

        // 3. LÓGICA DE SALTO
        if (isGrounded && isJumpButtonPressed && !isDown)
        {
            isJumpButtonPressed = false;
            rigidBody3D.AddForce(Vector3.up * 10f, ForceMode.Impulse);
        }
        else
        {
            isJumpButtonPressed = false;
        }
    }
}