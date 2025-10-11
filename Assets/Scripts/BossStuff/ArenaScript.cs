using UnityEngine;

public class ArenaScript : MonoBehaviour
{
    //ja nao sei mais o que tou a fazer, se funciona? funciona. se ta bem feito? nao sei

    bool hasSet = false;
    [SerializeField] private Animator animator;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasSet)
        {
            animator.SetBool("playerenter", true);
            hasSet = true;
        }
    }
}
