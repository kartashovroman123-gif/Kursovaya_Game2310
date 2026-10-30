using UnityEngine;

public class characterController : MonoBehaviour
{
    [SerializeField] int speed = 20;
    private Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        float dirX = Input.GetAxisRaw("Horizontal");
        float dirY = Input.GetAxisRaw("Vertical");

        rb.transform.position += new Vector3(dirX, dirY, 0) * speed * Time.deltaTime;
    }
}
