using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("State Names")]
    [SerializeField] private string idleState = "idle";
    [SerializeField] private string movePrefix = "move_";
    [SerializeField] private string attackPrefix = "attack_";
    [SerializeField] private string fallbackAttackState = "attack";

    [Header("Timing")]
    [SerializeField] private float crossFadeDuration = 0.03f;
    [SerializeField] private float attackLockDuration = 0.22f;

    private string currentState;
    private Vector2 lastFacingDirection = Vector2.down;
    private float attackLockUntil;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (playerMovement != null)
        {
            playerMovement.OnAttackPressed += HandleAttackPressed;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnAttackPressed -= HandleAttackPressed;
        }
    }

    private void Update()
    {
        ResolveReferences();

        if (animator == null)
        {
            return;
        }

        Vector2 moveInput = playerMovement != null ? playerMovement.CurrentMoveInput : Vector2.zero;
        if (moveInput.sqrMagnitude > 0.001f)
        {
            lastFacingDirection = moveInput.normalized;
        }
        else if (playerMovement != null && playerMovement.LastMoveDirection.sqrMagnitude > 0.001f)
        {
            lastFacingDirection = playerMovement.LastMoveDirection.normalized;
        }

        if (Time.time < attackLockUntil)
        {
            return;
        }

        if (moveInput.sqrMagnitude > 0.001f)
        {
            PlayStateIfPresent(movePrefix + ResolveDirectionSuffix(moveInput));
            return;
        }

        PlayStateIfPresent(idleState);
    }

    private void HandleAttackPressed()
    {
        ResolveReferences();

        if (animator == null)
        {
            return;
        }

        string suffix = ResolveDirectionSuffix(lastFacingDirection);
        if (PlayStateIfPresent(attackPrefix + suffix, fallbackAttackState, "attack"))
        {
            attackLockUntil = Time.time + Mathf.Max(0.01f, attackLockDuration);
        }
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    private bool PlayStateIfPresent(params string[] stateNames)
    {
        if (animator == null || stateNames == null)
        {
            return false;
        }

        for (int i = 0; i < stateNames.Length; i++)
        {
            string stateName = stateNames[i];
            if (string.IsNullOrWhiteSpace(stateName))
            {
                continue;
            }

            int stateHash = Animator.StringToHash(stateName);
            int baseLayerHash = Animator.StringToHash("Base Layer." + stateName);
            int playableHash = 0;
            if (animator.HasState(0, stateHash))
            {
                playableHash = stateHash;
            }
            else if (animator.HasState(0, baseLayerHash))
            {
                playableHash = baseLayerHash;
            }
            else
            {
                continue;
            }

            if (currentState == stateName)
            {
                return true;
            }

            currentState = stateName;
            if (crossFadeDuration > 0f)
            {
                animator.CrossFade(playableHash, crossFadeDuration, 0, 0f);
            }
            else
            {
                animator.Play(playableHash, 0, 0f);
            }

            return true;
        }

        return false;
    }

    private string ResolveDirectionSuffix(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = lastFacingDirection.sqrMagnitude > 0.001f ? lastFacingDirection : Vector2.down;
        }

        direction.Normalize();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        if (angle >= 337.5f || angle < 22.5f)
        {
            return "r";
        }

        if (angle < 67.5f)
        {
            return "ur";
        }

        if (angle < 112.5f)
        {
            return "u";
        }

        if (angle < 157.5f)
        {
            return "ul";
        }

        if (angle < 202.5f)
        {
            return "l";
        }

        if (angle < 247.5f)
        {
            return "dl";
        }

        if (angle < 292.5f)
        {
            return "d";
        }

        return "dr";
    }
}
