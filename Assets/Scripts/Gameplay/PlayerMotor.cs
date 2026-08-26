using UnityEngine;
using UnityEngine.InputSystem;

namespace RealRail
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] LaneLayout lanes;
        [SerializeField] GameSession session;
        [SerializeField] float speed = 10f;

        InputAction _move;

        void OnEnable()
        {
            if (InputSystem.actions == null)
            {
                return;
            }

            _move = InputSystem.actions.FindAction("Move");
            _move?.Enable();
        }

        void OnDisable()
        {
            _move?.Disable();
        }

        void Update()
        {
            if (session != null && !session.IsPlaying)
            {
                return;
            }

            if (lanes == null)
            {
                return;
            }

            var input = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            var position = transform.position;
            position.x += input.x * speed * Time.deltaTime;
            position.x = lanes.ClampStrafe(position.x);
            position.y = lanes.ActorY;
            position.z = lanes.PlayerZ;
            transform.position = position;
        }
    }
}
