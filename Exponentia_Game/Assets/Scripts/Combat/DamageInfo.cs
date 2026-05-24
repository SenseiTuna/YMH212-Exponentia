using UnityEngine;

public struct DamageInfo
{
    public float amount;
    public Vector2 hitPoint;
    public Vector2 hitDirection;
    public GameObject source;
    public bool ignoreInvulnerability;
    public float knockbackForce;

    public DamageInfo(
        float amount,
        Vector2 hitPoint,
        Vector2 hitDirection,
        GameObject source,
        bool ignoreInvulnerability = false,
        float knockbackForce = 0f)
    {
        this.amount = amount;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection;
        this.source = source;
        this.ignoreInvulnerability = ignoreInvulnerability;
        this.knockbackForce = knockbackForce;
    }

    public static DamageInfo FromSource(
        float amount,
        Vector3 targetPosition,
        GameObject source,
        bool ignoreInvulnerability = false,
        float knockbackForce = 0f)
    {
        Vector2 direction = Vector2.zero;
        Vector2 hitPoint = targetPosition;

        if (source != null)
        {
            Vector2 sourcePosition = source.transform.position;
            direction = ((Vector2)targetPosition - sourcePosition).normalized;
            hitPoint = sourcePosition;
        }

        return new DamageInfo(amount, hitPoint, direction, source, ignoreInvulnerability, knockbackForce);
    }
}
