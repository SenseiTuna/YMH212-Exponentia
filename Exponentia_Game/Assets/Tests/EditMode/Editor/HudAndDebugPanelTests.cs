using Exponentia.Player;
using Exponentia.UI;
using NUnit.Framework;
using UnityEngine;

namespace Exponentia.Tests.EditMode
{
    public class HudAndDebugPanelTests
    {
        [Test]
        public void BuildGaugeText_ReturnsExpectedShape()
        {
            string result = PlayerHudController.BuildGaugeText("HP", 67.2f, 100f);
            Assert.AreEqual("HP: 68/100", result);
        }

        [Test]
        public void BuildInfoText_ReturnsExpectedShape()
        {
            string result = PlayerHudController.BuildInfoText(3, 40f, 120f);
            Assert.AreEqual("Level: 3  XP: 40/120", result);
        }

        [Test]
        public void BuildDebugInfo_ContainsCriticalFields()
        {
            GameObject player = new GameObject("PlayerForTest");
            PlayerStats stats = player.AddComponent<PlayerStats>();
            PlayerMechanics mechanics = player.AddComponent<PlayerMechanics>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();

            string info = DebugPanelController.BuildDebugInfo(
                0.016f,
                stats,
                mechanics,
                movement,
                new Vector3(2f, 5f, 0f));

            Assert.That(info, Does.Contain("FPS:"));
            Assert.That(info, Does.Contain("POS: (2.00, 5.00)"));
            Assert.That(info, Does.Contain("Level:"));
            Assert.That(info, Does.Contain("Input:"));

            Object.DestroyImmediate(player);
        }
    }
}
