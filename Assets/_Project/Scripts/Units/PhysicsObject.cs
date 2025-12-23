using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    public float gravityModifier = 1f;

    protected Rigidbody2D rb;
    protected Vector2 velocity;

    void OnEnable()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
        Vector2 deltaPosition = velocity * Time.deltaTime;
        Vector2 move = Vector2.up * deltaPosition.y;
        Movement(move);
    }

    private void Movement(Vector2 move)
    {
        rb.position = rb.position + move;
    }
}
