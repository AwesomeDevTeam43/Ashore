using UnityEngine;

public class Enemy : MonoBehaviour
{

    public int health;
    public float speed;
    //public GameObject bloodEffect;

    private Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.Translate(Vector2.left * speed * Time.deltaTime);

        }
    }


    public void TakeDamage(int damage)
    {
        //Instantiate(bloodEffect, transform.position, Quaternion.identity);
        health -= damage;
        Debug.Log("Enemy took " + damage + " damage.");
    }

}
