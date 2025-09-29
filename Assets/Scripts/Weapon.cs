using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectile;
    public Transform shotPoint;
    private float timeBetweenShots;
    public float startTimeBetweenShots;

    [SerializeField] private Player_Movement Player_Movement;

    private void Update()
    {
        if (timeBetweenShots <= 0)
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                GameObject newProjectile = Instantiate(projectile, shotPoint.position, shotPoint.rotation);
                Projectile proj = newProjectile.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.SetDirection(Player_Movement.IsFacingRight ? Vector2.right : Vector2.left);
                }
            }
        }
        else
        {
            timeBetweenShots -= Time.deltaTime;
        }
    }
    
}
