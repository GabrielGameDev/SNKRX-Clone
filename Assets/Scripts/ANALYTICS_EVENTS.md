# Unity Analytics events

Create these custom events and parameters in the Unity Dashboard Event Manager. Names and parameter keys are case-sensitive and must match the code. The game initializes UGS and records custom events through `AnalyticsService.Instance.RecordEvent(new CustomEvent(...))`; data collection remains subject to the Analytics consent state configured for the project/player.

All event payloads generated during a run include `run_id` (string), which lets you group upgrade, wave, explosion, and run summary records for a single attempt.

| Event | Parameters |
|---|---|
| `run_started` | `run_id` (string) |
| `upgrade_selected` | `run_id` (string), `upgrade_type` (string), `upgrade_level` (integer), `wave` (integer) |
| `wave_started` | `run_id` (string), `wave` (integer) |
| `wave_completed` | `run_id` (string), `wave` (integer) |
| `run_ended` | `run_id` (string), `duration_seconds` (number), `score` (integer), `highest_wave` (integer), `waves_completed` (integer), `player_level` (integer), `levels_gained` (integer), `xp_collected` (integer), `enemies_killed` (integer), `wanderers_killed` (integer), `chasers_killed` (integer), `kamikazes_killed` (integer), `tanks_killed` (integer), `damage_received` (number), `shooter_damage` (number), `bomber_damage` (number), `upgrade_shooter` (integer), `upgrade_damage` (integer), `upgrade_range` (integer), `upgrade_health` (integer), `upgrade_speed` (integer), `upgrade_magnet` (integer) |

`shooter_damage` and `bomber_damage` count actual enemy health removed, capped by each target's remaining health, and are sent once in the `run_ended` event.

At run end, the game requests an immediate Analytics SDK flush. To see actual upload results in the Unity Console, temporarily add `UNITY_ANALYTICS_EVENT_LOGS` to **Project Settings > Player > Other Settings > Scripting Define Symbols** for the build target. The SDK then logs messages such as `Events uploaded successfully!` or an upload failure and response code. Remove the define after diagnosing because it logs every uploaded event. The game's own `Flush requested` log only means the request was made, not that the server accepted it.
