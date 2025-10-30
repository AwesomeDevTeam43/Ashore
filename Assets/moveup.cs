using UnityEngine;

public class moveup : MonoBehaviour
{
    bool entered = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (entered)
            this.transform.position += new Vector3(0, 1f * Time.deltaTime, 0);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Equipment"))
        {
            entered = true;
        }
    }
}
