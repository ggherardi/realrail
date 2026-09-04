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
        Vector2 _automatedInput;
        bool _hasAutomatedInput;

        /// <summary>True while an automated controller owns movement input.</summary>
        public bool HasAutomatedInput => _hasAutomatedInput;
        public Vector2 AutomatedInput => _automatedInput;

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

            var input = _hasAutomatedInput ? _automatedInput : (_move != null ? _move.ReadValue<Vector2>() : Vector2.zero);
            Move(input, Time.deltaTime);
        }

        /// <summary>Supplies movement through the same motor used by human input.</summary>
        public void SetAutomatedInput(Vector2 input)
        {
            _automatedInput = Vector2.ClampMagnitude(input, 1f);
            _hasAutomatedInput = true;
        }

        /// <summary>Returns control to the configured human input action.</summary>
        public void ClearAutomatedInput()
        {
            _automatedInput = Vector2.zero;
            _hasAutomatedInput = false;
        }

        public void Move(Vector2 input, float deltaTime)
        {
            if (session != null && !session.IsPlaying)
            {
                return;
            }

            if (lanes == null)
            {
                return;
            }

            var position = transform.position;
            position.x += Mathf.Clamp(input.x, -1f, 1f) * speed * deltaTime;
            position.x = lanes.ClampStrafe(position.x);
            position.y = lanes.ActorY;
            position.z = lanes.PlayerZ;
            transform.position = position;
        }

        public void ConfigureForTests(LaneLayout laneLayout, GameSession gameSession, float movementSpeed = 10f)
        {
            lanes = laneLayout;
            session = gameSession;
            speed = movementSpeed;
        }
    }
}
