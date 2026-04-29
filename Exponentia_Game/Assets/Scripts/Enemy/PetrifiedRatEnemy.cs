using UnityEngine;

public class PetrifiedRatEnemy : EnemyMechanics
{
    private void Reset()
    {
        ApplyDefaultSetup(
            "Petrified Rat",
            18f,
            3.6f,
            8f,
            6f,
            true,
            0.1f,
            new Color(0.45f, 0.45f, 0.45f),
            new Vector2(0.55f, 0.55f));
    }
}
