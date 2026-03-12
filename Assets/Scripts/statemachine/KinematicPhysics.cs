using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class KinematicPhysics : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 velocity;
    [SerializeField] private Vector3 gravity=Vector3.down*20f;
    [SerializeField] private float skinWidth = 0.02f;
    [SerializeField] private float MaxSlopeAngle=80f;

    [Header("Layer masks")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask waterLayer;

    //position states and related
    public bool grounded{get; private set;}
    public float groundSlopeAngle{get; private set;}
    public Vector3 groundSlopeNormal {get; private set;}

    public bool wallStick=false;
    public bool dashing=false;

    //other stuff only for use here
    private SphereCollider sphereCollider;
    private const int MaxBounces=3;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }
    void FixedUpdate()
    {
        GroundCheck();
        Move(velocity * Time.fixedDeltaTime);
    }

    private void Move(Vector3 movement)
    {
        ApplyGravity();
        Vector3 remainingMovement = movement;
        
        // Loop allows for sliding off multiple surfaces in one frame (e.g., corners)
        for (int i = 0; i < MaxBounces; i++)
        {
            float distance = remainingMovement.magnitude;
            if (distance < 0.001f) break;

            if (Physics.SphereCast(transform.TransformPoint(sphereCollider.center),sphereCollider.radius,remainingMovement.normalized,out RaycastHit hit, distance + skinWidth, collisionLayers))
            {
                // Move up to the hit
                float castDistance = Mathf.Max(0, hit.distance - skinWidth);
                transform.position += remainingMovement.normalized * castDistance;

                // Calculate slide vector
                remainingMovement = Vector3.ProjectOnPlane(remainingMovement.normalized * (distance - castDistance), hit.normal);
                
                // Also project velocity so we don't keep pushing into the wall
                velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
            }
            else
            {
                transform.position += remainingMovement;
                break;
            }
        }
    }

    private void ApplyGravity()
    {
        if (!grounded)
        {
            velocity += gravity * Time.fixedDeltaTime;
        }
        else if (velocity.y < 0)
        {
            // Small snap-to-ground force
            velocity.y = -2f;
        }
    }

    void GroundCheck()
    {
        // Start the cast from slightly above the bottom sphere (p2)
        // This gives the SphereCast space to actually "hit" the floor 
        // even if we are flush against it.
        Vector3 castOrigin = transform.TransformPoint(sphereCollider.center) + transform.up * 0.1f; 
        float castDistance = 0.1f + skinWidth + 0.05f; // Extra padding

        bool Rayhit = Physics.SphereCast(
            castOrigin,
            sphereCollider.radius,
            -transform.up,
            out RaycastHit hit,
            castDistance,
            collisionLayers // Use collisionLayers to ensure it hits anything solid
        );
        if (!Rayhit)
        {
            grounded=false;
            groundSlopeNormal=Vector3.up;
            return;
        }
        groundSlopeNormal=hit.normal;
        groundSlopeAngle=Vector3.Angle(groundSlopeNormal,Vector3.up);

        if (groundSlopeAngle > MaxSlopeAngle)
        {
            grounded=false;
            return;
        }
        int layer=hit.collider.gameObject.layer;
        int mask=1<<layer;
        if ((groundLayer & mask) != 0)
        {
            grounded=true;
            return;
        }
        //if ((waterLayer & mask) != 0) check water
    }
}