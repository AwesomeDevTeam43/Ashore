using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectile;
    public Transform shotPoint;
    private float timeBetweenShots;
    public float startTimeBetweenShots;

    [SerializeField] private Player_Movement Player_Movement;
    [SerializeField] private Player_InputHandler player_InputHandler;

    private void Update()
    {
        if (timeBetweenShots <= 0)
        {
            if (player_InputHandler.RangeAttackTriggered)
            {
                GameObject newProjectile = Instantiate(projectile, shotPoint.position, shotPoint.rotation);
                Projectile proj = newProjectile.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.SetDirection(Player_Movement.IsFacingRight ? Vector2.right : Vector2.left);
                }
            }
            timeBetweenShots = startTimeBetweenShots;
        }
        else
        {
            timeBetweenShots -= Time.deltaTime;
        }
    }

}
