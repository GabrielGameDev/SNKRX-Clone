# SNKRX Clone

A 2D arena survival game inspired by *SNKRX*. The player steers a snake through an arena while its segments automatically attack incoming enemies. Defeated enemies drop experience; each level-up opens a choice of upgrades that changes the snake's stats and composition. Waves continue until the player is defeated.

This README is a short guide for developers working on the Unity project: it describes the runtime flow, the main code areas, and where to change game data.

## Requirements

- Unity Editor **6000.6.4f1** (see `ProjectSettings/ProjectVersion.txt`).
- Packages are declared in `Packages/manifest.json`. The project uses the Input System, URP 2D, TextMesh Pro, and Unity Gaming Services Analytics.

## Getting started

1. Open the repository folder in Unity Hub and let Unity import the assets.
2. Open `Assets/Scenes/SampleScene.unity` and press **Play**.
3. Press any key or gamepad button to start a run. Move with **WASD**, the **arrow keys**, or a gamepad's left stick.
4. Collect XP drops and select an upgrade when the level-up choices appear. The snake attacks automatically.
5. After game over, press any key or gamepad button to reload the scene and start another run.

`SampleScene` is enabled in `ProjectSettings/EditorBuildSettings.asset` and is the project's current entry scene.

## Runtime flow

`GameFlowController` starts the scene on a paused title screen. Starting a run resumes time and initializes run tracking. `WaveManager` then builds successive waves, while `EnemySpawner` shows a spawn marker before instantiating each enemy. A wave ends after all pending spawns and living enemies are gone.

`SnakeMovement` steers the head and positions body segments using recorded path history, so segments follow the same route around turns. The arena supports configurable horizontal and vertical wrapping. Segment components handle their specialized behavior, such as automatic shooting or area damage. Enemies, projectiles, health, and XP pickups are implemented separately.

Experience and game-state communication use `GameEvents`, a static event channel. For example, `PlayerExperience` raises level-up and XP events, and UI/game-flow components listen for them. `UpgradeManager` owns upgrade state for the active run and applies the selected bonuses. On player death, `GameFlowController` pauses the game, displays the run summary, and handles restarting.

## Code map

| Location | Responsibility |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | Main playable scene and scene-level component setup. |
| `Assets/Scripts/SnakeMovement.cs`, `SnakeSegment.cs`, `InputController.cs` | Player movement, snake body, and input. |
| `Assets/Scripts/WaveManager.cs`, `EnemySpawner.cs` | Wave progression, enemy selection, and spawn timing. |
| `Assets/Scripts/Enemies/` | Enemy behavior, projectiles, and enemy death effects. |
| `Assets/Scripts/UpgradeManager.cs`, `UpgradeSelectionUI.cs`, `UpgradeCardUI.cs` | Upgrade progression and selection UI. |
| `Assets/Scripts/PlayerExperience.cs`, `GameEvents.cs` | XP progression and cross-system gameplay events. |
| `Assets/Scripts/GameFlowController.cs`, `UIManager.cs` | Run lifecycle and gameplay UI. |
| `Assets/Scripts/GameAnalyticsReporter.cs` | Unity Gaming Services initialization and gameplay event reporting. |
| `Assets/Prefabs/` | Snake segments, enemies, projectiles, pickups, spawn markers, and effects. |
| `Assets/Settings/` | Input actions, render pipeline, and wave difficulty asset. |

## Tuning game data

- **Wave pacing and enemy unlocks:** edit `Assets/Settings/DefaultWaveConfig.asset`. It controls enemy counts, health scaling, spawn intervals, time between waves, and the waves where Chaser, Kamikaze, and Tank enemies become available.
- **Upgrade definitions and starting values:** edit `Assets/Resources/UpgradeBalanceData.asset`. Each definition includes an upgrade type, display name, description, and per-level amount. `UpgradeManager` applies these values during a run.
- **Input bindings:** edit `Assets/Settings/InputSystem_Actions.inputactions`. `InputController` reads the `Move` action; title and game-over screens accept any key or gamepad button.
- **Scene and prefab references:** gameplay components are connected in `SampleScene` and the prefabs. If a component or asset reference changes, check both the scene and affected prefabs in the Inspector.

## Analytics

`GameAnalyticsReporter` initializes Unity Gaming Services, queues events until initialization completes, and attaches the active run ID to run events. The current event names and payload fields are documented in [`Assets/Scripts/ANALYTICS_EVENTS.md`](Assets/Scripts/ANALYTICS_EVENTS.md). Analytics initialization can fail when services are unavailable; gameplay should remain testable in the Editor without a successful service connection.

## Tests

The repository includes upgrade-system Edit Mode tests in `Assets/Scripts/Editor/UpgradeSystemTests.cs`. Run them from Unity's **Window > General > Test Runner** using the Edit Mode tab.
