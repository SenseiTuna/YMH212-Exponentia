using UnityEngine;

[DisallowMultipleComponent]
public class CombatFeedbackController : MonoBehaviour
{
    [Header("Global Toggles")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private bool enableHitStop = true;
    [SerializeField] private bool enableScreenShake = true;

    [Header("Player Feedback")]
    [SerializeField] private float playerKnockbackForce = 4f;
    [SerializeField] private float playerKnockbackDuration = 0.12f;
    [SerializeField] private bool enablePlayerDamageShake = true;
    [SerializeField] private float playerDamageShakeDuration = 0.1f;
    [SerializeField] private float playerDamageShakeStrength = 0.08f;

    [Header("Enemy Feedback")]
    [SerializeField] private Color enemyHitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float enemyHitFlashDuration = 0.08f;
    [SerializeField] private Color enemyDeathFlashColor = Color.white;
    [SerializeField] private float enemyDeathFlashDuration = 0.05f;
    [SerializeField] private float enemyKnockbackForce = 2.5f;
    [SerializeField] private float enemyKnockbackDuration = 0.08f;
    [SerializeField] private bool enableEnemyHitStop = true;
    [SerializeField] private float enemyHitStopDuration = 0.02f;
    [SerializeField] private float enemyHitStopMinInterval = 0.06f;
    [SerializeField] private bool enableEnemyHitShake;
    [SerializeField] private float enemyHitShakeDuration = 0.04f;
    [SerializeField] private float enemyHitShakeStrength = 0.05f;

    private float lastEnemyHitStopTime = -10f;

    private void Awake()
    {
        EnsureManagers();
    }

    private void EnsureManagers()
    {
        if (enableHitStop && HitStopManager.Instance == null)
        {
            GameObject manager = new GameObject("HitStopManager");
            manager.AddComponent<HitStopManager>();
        }

        if (enableScreenShake && CameraShake2D.Instance == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                CameraShake2D shake = mainCamera.GetComponent<CameraShake2D>();
                if (shake == null)
                {
                    mainCamera.gameObject.AddComponent<CameraShake2D>();
                }
            }
        }
    }

    public void OnPlayerDamaged(
        PlayerMechanics player,
        DamageInfo info,
        float invulnerabilityDuration,
        bool blinkDuringInvulnerability,
        float blinkInterval,
        Color damageFlashColor,
        float damageFlashDuration)
    {
        if (player == null)
        {
            return;
        }

        DamageFlashFeedback flash = player.GetComponent<DamageFlashFeedback>();
        if (enableDamageFlash && flash != null)
        {
            flash.Flash(damageFlashColor, damageFlashDuration);
            if (blinkDuringInvulnerability && invulnerabilityDuration > 0f)
            {
                flash.StartBlink(damageFlashColor, invulnerabilityDuration, blinkInterval);
            }
        }

        if (enableKnockback)
        {
            KnockbackReceiver2D receiver = player.GetComponent<KnockbackReceiver2D>();
            if (receiver != null)
            {
                float force = info.knockbackForce > 0f ? info.knockbackForce : playerKnockbackForce;
                receiver.ApplyKnockback(info.hitDirection, force, playerKnockbackDuration);
            }
        }

        if (enableScreenShake && enablePlayerDamageShake && CameraShake2D.Instance != null)
        {
            CameraShake2D.Instance.Shake(playerDamageShakeDuration, playerDamageShakeStrength);
        }
    }

    public void OnEnemyDamaged(EnemyMechanics enemy, DamageInfo info, bool isLethalHit, bool disableKnockbackForThisEnemy = false)
    {
        if (enemy == null)
        {
            return;
        }

        DamageFlashFeedback flash = enemy.GetComponent<DamageFlashFeedback>();
        if (enableDamageFlash && flash != null)
        {
            if (isLethalHit)
            {
                flash.Flash(enemyDeathFlashColor, enemyDeathFlashDuration);
            }
            else
            {
                flash.Flash(enemyHitFlashColor, enemyHitFlashDuration);
            }
        }

        if (enableKnockback && !disableKnockbackForThisEnemy)
        {
            KnockbackReceiver2D receiver = enemy.GetComponent<KnockbackReceiver2D>();
            if (receiver != null)
            {
                float force = info.knockbackForce > 0f ? info.knockbackForce : enemyKnockbackForce;
                receiver.ApplyKnockback(info.hitDirection, force, enemyKnockbackDuration);
            }
        }

        if (enableHitStop && enableEnemyHitStop && !isLethalHit && HitStopManager.Instance != null)
        {
            float now = Time.unscaledTime;
            if (now - lastEnemyHitStopTime >= Mathf.Max(0f, enemyHitStopMinInterval))
            {
                lastEnemyHitStopTime = now;
                HitStopManager.Instance.DoHitStop(enemyHitStopDuration);
            }
        }

        if (enableScreenShake && enableEnemyHitShake && CameraShake2D.Instance != null)
        {
            CameraShake2D.Instance.Shake(enemyHitShakeDuration, enemyHitShakeStrength);
        }
    }
}
