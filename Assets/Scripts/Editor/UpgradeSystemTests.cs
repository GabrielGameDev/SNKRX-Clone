using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class UpgradeSystemTests
{
    private GameObject upgradeManagerGo;
    private UpgradeManager upgradeManager;

    [SetUp]
    public void Setup()
    {
        upgradeManagerGo = new GameObject("TestUpgradeManager");
        upgradeManager = upgradeManagerGo.AddComponent<UpgradeManager>();
    }

    [TearDown]
    public void Teardown()
    {
        if (upgradeManagerGo != null)
        {
            Object.DestroyImmediate(upgradeManagerGo);
        }
    }

    [Test]
    public void TestPaletteAssignment_HasSixUniqueColors()
    {
        upgradeManager.StartNewRun();

        HashSet<string> assignedColorNames = new HashSet<string>();
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            RunUpgradeState state = upgradeManager.GetUpgradeState(type);
            Assert.IsNotNull(state, $"Upgrade state for {type} should not be null.");
            Assert.IsNotNull(state.AssignedColor, $"Assigned color for {type} should not be null.");
            Assert.IsFalse(state.IsDiscovered, $"Upgrade {type} should start undiscovered.");
            Assert.IsTrue(assignedColorNames.Add(state.AssignedColor.colorName), $"Duplicate color {state.AssignedColor.colorName} assigned!");
        }

        Assert.AreEqual(6, assignedColorNames.Count, "All 6 upgrades must have unique colors assigned.");
    }

    [Test]
    public void TestGetRandomChoices_ReturnsThreeUniqueUpgrades()
    {
        List<RunUpgradeState> choices = upgradeManager.GetRandomChoices(3);

        Assert.AreEqual(3, choices.Count, "Should return exactly 3 choices.");
        HashSet<UpgradeType> types = new HashSet<UpgradeType>();
        foreach (var choice in choices)
        {
            Assert.IsTrue(types.Add(choice.Type), $"Choice {choice.Type} was duplicated in the selection!");
        }
    }

    [Test]
    public void TestDiscoveryFlow_RevealsOnlySelectedUpgrade()
    {
        upgradeManager.StartNewRun();

        RunUpgradeState damageState = upgradeManager.GetUpgradeState(UpgradeType.Damage);
        RunUpgradeState speedState = upgradeManager.GetUpgradeState(UpgradeType.Speed);

        Assert.IsFalse(damageState.IsDiscovered);
        Assert.IsFalse(speedState.IsDiscovered);

        // Player picks damage
        upgradeManager.SelectUpgrade(UpgradeType.Damage);

        Assert.IsTrue(damageState.IsDiscovered, "Selected upgrade should now be discovered.");
        Assert.AreEqual(1, damageState.Level, "Level should increment to 1.");
        Assert.IsFalse(speedState.IsDiscovered, "Unselected upgrade should remain undiscovered.");
    }

    [Test]
    public void TestStatMultipliers_AppliedCorrectly()
    {
        upgradeManager.StartNewRun();

        float initialDamage = upgradeManager.GlobalDamageMultiplier;
        float initialRange = upgradeManager.RangeMultiplier;
        float initialMagnet = upgradeManager.MagnetRadius;

        upgradeManager.SelectUpgrade(UpgradeType.Damage);
        Assert.Greater(upgradeManager.GlobalDamageMultiplier, initialDamage);

        upgradeManager.SelectUpgrade(UpgradeType.Range);
        Assert.Greater(upgradeManager.RangeMultiplier, initialRange);

        upgradeManager.SelectUpgrade(UpgradeType.Magnet);
        Assert.Greater(upgradeManager.MagnetRadius, initialMagnet);
    }

    [Test]
    public void TestStartNewRun_ResetsDiscoveryAndStats()
    {
        upgradeManager.SelectUpgrade(UpgradeType.Damage);
        upgradeManager.SelectUpgrade(UpgradeType.Speed);

        // Start new run
        upgradeManager.StartNewRun();

        Assert.AreEqual(1.0f, upgradeManager.GlobalDamageMultiplier);
        Assert.IsFalse(upgradeManager.GetUpgradeState(UpgradeType.Damage).IsDiscovered);
        Assert.IsFalse(upgradeManager.GetUpgradeState(UpgradeType.Speed).IsDiscovered);
        Assert.AreEqual(0, upgradeManager.GetUpgradeState(UpgradeType.Damage).Level);
    }

    [Test]
    public void TestAddingMoreThanThreeSegments_AllSegmentsExist()
    {
        GameObject snakeGo = new GameObject("TestSnake");
        SnakeMovement snake = snakeGo.AddComponent<SnakeMovement>();

        // Start with 1 segment
        Assert.AreEqual(0, snake.Segments.Count);

        // Add 6 segments
        for (int i = 0; i < 6; i++)
        {
            snake.AddSegment(null, Color.red);
        }

        Assert.AreEqual(6, snake.Segments.Count, "Snake should support more than 3 segments.");
        for (int i = 0; i < 6; i++)
        {
            Assert.IsNotNull(snake.Segments[i], $"Segment at index {i} should not be null.");
            Assert.AreEqual(i, snake.Segments[i].SegmentIndex);
        }

        Object.DestroyImmediate(snakeGo);
    }
}
