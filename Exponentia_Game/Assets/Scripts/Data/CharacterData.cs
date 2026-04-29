/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: ScriptableObject definition for playable character data.
 */

using UnityEngine;

namespace Exponentia.Data
{
    [CreateAssetMenu(fileName = "CH_NewCharacter", menuName = "Exponentia/Data/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;
        public string characterName;
        [TextArea(3, 6)]
        public string description;

        [Header("Visual")]
        public Sprite gameplaySprite;
        public Color characterColor = Color.white;
        public Sprite portrait;
        public Sprite icon;

        [Header("Base Stats")]
        public StatBlock baseStats = new StatBlock();

        [Header("Starting Loadout")]
        public WeaponData startingWeapon;
        public string startingAbilityId;

        [Header("Animation References")]
        public AnimationClip idleAnimation;
        public AnimationClip moveAnimation;
        public AnimationClip attackAnimation;
        public AnimationClip hurtAnimation;
        public AnimationClip deathAnimation;
    }
}
