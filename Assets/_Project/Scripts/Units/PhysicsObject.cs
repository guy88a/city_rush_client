using System.Collections.Generic;
using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    [Header("Custom Physics Settings")]
    public float minGroundNormalY = 0.65f;
    public float gravityModifier = 1f;

    protected Vector2 targetVelocity;
    protected bool grounded;
    protected Vector2 groundNormal;
    protected Rigidbody2D rb;
    protected Vector2 velocity;
    protected ContactFilter2D contactFilter;
    protected RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
    protected List<RaycastHit2D> hitBufferList = new List<RaycastHit2D>(16);

    protected const float minMoveDistance = 0.001f;
    protected const float shellRadius = 0.01f;

    // When you move ticking to GameLoopState, set this to false and call Tick/FixedTick yourself.
    public bool UseUnityTicks { get; set; } = true;

    void OnEnable()
    {
        rb = GetComponent<Rigidbody2D>();

        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        contactFilter.useLayerMask = true;
    }

    // Wrapper for now (until GameLoopState owns it)
    void Update()
    {
        if (!UseUnityTicks) return;
        Tick();
    }

    // Wrapper for now (until GameLoopState owns it)
    void FixedUpdate()
    {
        if (!UseUnityTicks) return;
        FixedTick(Time.deltaTime);
    }

    public void Tick()
    {
        targetVelocity = Vector2.zero;
        ComputeVelocity();
    }

    public void FixedTick(float dt)
    {
        velocity += gravityModifier * Physics2D.gravity * dt;
        velocity.x = targetVelocity.x;

        grounded = false;

        Vector2 deltaPosition = velocity * dt;

        Vector2 moveAlongGround = new Vector2(groundNormal.y, -groundNormal.x);

        Vector2 move = moveAlongGround * deltaPosition.x;
        Movement(move, false);

        move = Vector2.up * deltaPosition.y;
        Movement(move, true);
    }

    protected virtual void ComputeVelocity() { }

    private void Movement(Vector2 move, bool yMovement)
    {
        float distance = move.magnitude;
        if (distance > minMoveDistance)
        {
            int count = rb.Cast(move, contactFilter, hitBuffer, distance + shellRadius);

            hitBufferList.Clear();
            for (int i = 0; i < count; i++)
                hitBufferList.Add(hitBuffer[i]);

            for (int i = 0; i < hitBufferList.Count; ++i)
            {
                Vector2 currentNormal = hitBufferList[i].normal;
                if (currentNormal.y > minGroundNormalY)
                {
                    grounded = true;
                    if (yMovement)
                    {
                        groundNormal = currentNormal;
                        currentNormal.x = 0;
                    }
                }

                float projection = Vector2.Dot(velocity, currentNormal);
                if (projection < 0)
                    velocity = velocity - projection * currentNormal;

                float modifiedDistance = hitBufferList[i].distance - shellRadius;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }
        }

        rb.position = rb.position + move.normalized * distance;
    }
}
