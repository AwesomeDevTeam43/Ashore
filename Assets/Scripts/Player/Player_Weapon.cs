using System;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectile;
    public Transform shotPoint;
    private float timeBetweenShots;
    public float startTimeBetweenShots;
    public enum WeaponDirection { Right, Left, Up, Down }
    public WeaponDirection weaponDirection;
    [SerializeField] private Player_Movement Player_Movement;
    [SerializeField] private Player_InputHandler player_InputHandler;
    public Vector2 GetShootDirection()
    {
        switch (weaponDirection)
        {
            case WeaponDirection.Right:
                return Vector2.right;
            case WeaponDirection.Left:
                return Vector2.left;
            case WeaponDirection.Up:
                return Vector2.up;
            case WeaponDirection.Down:
                return Vector2.down;
            default: return Vector2.right;
        }
    }

    private void OnEnable()
    {
        player_InputHandler.On8DirectionChanged += UpdateWeaponState;
    }

    private void OnDisable()
    {
        player_InputHandler.On8DirectionChanged -= UpdateWeaponState;
    }

    private void Update()
    {
        if (timeBetweenShots <= 0)
        {
            if (player_InputHandler.RangeAttackTriggered)
            {
                GameObject newProjectile = Instantiate(projectile, shotPoint.position, Quaternion.identity);
                Projectile proj = newProjectile.GetComponent<Projectile>();

                if (proj != null)
                {
                    // Define direção do disparo conforme o estado atual da weapon
                    Vector2 shootDir = GetShootDirection();
                    proj.SetDirection(shootDir);
                }
            }
            timeBetweenShots = startTimeBetweenShots;
        }
        else
        {
            timeBetweenShots -= Time.deltaTime;
        }
    }

    private void UpdateWeaponState(Vector2 inputDir)
    {
        if (inputDir == Vector2.zero)
        {
            // Se não tem input, segue orientação do player (esquerda/direita)
            weaponDirection = Player_Movement.IsFacingRight ? WeaponDirection.Right : WeaponDirection.Left;
            return;
        }

        // Decide o estado conforme direção dominante
        if (Mathf.Abs(inputDir.x) > Mathf.Abs(inputDir.y))
        {
            weaponDirection = inputDir.x > 0 ? WeaponDirection.Right : WeaponDirection.Left;
        }
        else
        {
            weaponDirection = inputDir.y > 0 ? WeaponDirection.Up : WeaponDirection.Down;
        }
    }
}
