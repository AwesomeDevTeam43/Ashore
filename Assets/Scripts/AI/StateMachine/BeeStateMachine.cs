using UnityEngine;
using Ashore.AI.Utilities;

namespace Ashore.AI.StateMachine
{
    // Lightweight state-machine wrapper for the Giant Bee enemy.
    // Encapsulates the state enum and exposes helpers to drive animation flags and stinger toggles.
    public class BeeStateMachine
    {
        public SimpleEnemyState State { get; private set; } = SimpleEnemyState.Roaming;

        public Vector3 LungeStartPosition { get; private set; }
        public Vector3 AttackPoint { get; private set; }
        public Vector3 RetreatTarget { get; private set; }

        public void SetRoaming(Animator animator, string attackParam, string lungeParam, string retreatParam)
        {
            State = SimpleEnemyState.Roaming;
            SetAnim(animator, attackParam, false);
            SetAnim(animator, lungeParam, false);
            SetAnim(animator, retreatParam, false);
        }

        public void BeginWindup(Transform enemy, Animator animator, string attackParam, string lungeParam, string retreatParam, Collider2D playerCol)
        {
            LungeStartPosition = enemy.position;
            AttackPoint = AIHelpers.GetFeetPosition(playerCol, enemy);
            State = SimpleEnemyState.AttackWindup;
            SetAnim(animator, attackParam, true);
            SetAnim(animator, lungeParam, false);
            SetAnim(animator, retreatParam, false);
        }

        public void BeginLunge(Animator animator, string attackParam, string lungeParam)
        {
            State = SimpleEnemyState.Lunging;
            SetAnim(animator, attackParam, false);
            SetAnim(animator, lungeParam, true);
        }

        public void BeginRetreat(Transform enemy, float retreatRange, Animator animator, string lungeParam, string retreatParam)
        {
            Vector2 retreatDir = (LungeStartPosition - enemy.position).normalized;
            RetreatTarget = enemy.position + (Vector3)retreatDir * retreatRange;
            State = SimpleEnemyState.Retreating;
            SetAnim(animator, lungeParam, false);
            SetAnim(animator, retreatParam, true);
        }

        private void SetAnim(Animator animator, string param, bool value)
        {
            if (animator == null || string.IsNullOrEmpty(param)) return;
            animator.SetBool(param, value);
        }
    }
}
