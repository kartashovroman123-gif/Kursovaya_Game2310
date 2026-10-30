using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class characterController : MonoBehaviour
{
    [SerializeField] int speed = 20;
    private Rigidbody2D rb;
    private Vector2 direction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float dirX = Input.GetAxisRaw("Horizontal");
        float dirY = Input.GetAxisRaw("Vertical");
    
        direction = new Vector2(dirX, dirY) * speed * Time.deltaTime;
        rb.position += direction * speed * Time.deltaTime;
    }
}
