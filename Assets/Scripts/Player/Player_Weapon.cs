using System;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectile;
    public Transform shotPoint;
    private float timeBetweenShots;
    public float startTimeBetweenShots;
    public enum WeaponDirection { Right, Left, Up, Down, UpRight, UpLeft, DownRight, DownLeft }
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
            case WeaponDirection.UpRight:
                return new Vector2(1, 1).normalized;
            case WeaponDirection.UpLeft:
                return new Vector2(-1, 1).normalized;
            case WeaponDirection.DownRight:
                return new Vector2(1, -1).normalized;
            case WeaponDirection.DownLeft:
                return new Vector2(-1, -1).normalized;
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
            weaponDirection = Player_Movement.IsFacingRight ? WeaponDirection.Right : WeaponDirection.Left;
            return;
        }

        if (inputDir.x > 0 && inputDir.y > 0)
        {
            weaponDirection = WeaponDirection.UpRight;
        }
        else if (inputDir.x < 0 && inputDir.y > 0)
        {
            weaponDirection = WeaponDirection.UpLeft;
        }
        else if (inputDir.x > 0 && inputDir.y < 0)
        {
            weaponDirection = WeaponDirection.DownRight;
        }
        else if (inputDir.x < 0 && inputDir.y < 0)
        {
            weaponDirection = WeaponDirection.DownLeft;
        }
        else if (Mathf.Abs(inputDir.x) > Mathf.Abs(inputDir.y))
        {
            weaponDirection = inputDir.x > 0 ? WeaponDirection.Right : WeaponDirection.Left;
        }
        else
        {
            weaponDirection = inputDir.y > 0 ? WeaponDirection.Up : WeaponDirection.Down;
        }
    }
}
