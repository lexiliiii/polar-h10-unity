# Polar H10: direct threshold prompts in the browser and Unity

The default dashboard now does one thing: **sensor reading → the location's document rule → immediate learner prompt**. The document's duration conditions are retained, as confirmed by the user. There is no Session start, pause wait, cooldown, alert cap, training stage, final-bout switch or calibration wizard in this flow.

## Run

1. Run `node Tools/PolarH10Bridge/server.mjs`. It starts two separate local websites.
2. Open the acquisition page at `http://localhost:8080` in Chrome or Edge. Connect H10 sensors, select their exact body-part labels, and enable EXG. Keep **highpass / 200 ms RMS** selected. This page remains the original sensor and waveform interface; it does not contain the rule dashboard.
3. Open the independent dashboard at `http://localhost:8081`. Select **Overhead** or **Bending**; for Overhead, enable optional forearms only when those hand-tool sensors are in use. Expand **Calibration values** to enter rest/restSD/max manually or load File B JSON. Values and the setup are saved in dashboard-local browser storage. Incoming readings are evaluated automatically, with status colours and immediate prompts shown on this separate page.
4. Unity is optional. To also use the 3D body view or XR learner prompt, open the project with Unity **2022.3.56f1**, choose **Tools > Polar H10 > Open Dashboard Scene**, and enter Play mode.

No participant ID or session start is required. A setup selection is required because the documents allow prompts only from channels used by the current task. Within that setup, one sensor can prompt independently; other sensors need not be configured.

## Rules used

| Locations | High | Sustained Moderate | Action |
| --- | --- | --- | --- |
| L/RShoulder, L/RFrontShoulder | >20% MVIC for >3 s | 10–20% MVIC for >30 s | Lower your arm or bring work closer. |
| L/RLowerSpine, L/RUpperBack | >30% MVIC for >5 s | 15–30% MVIC for >30 s | Bend your knees and keep the load close. |
| L/RLowerArm | >30% MVIC for >5 s | 15–30% MVIC for >30 s | Loosen your grip on the tool. |
| L/RThigh | Color only | Color only | No corrective prompt, per the document. |

Sustained Moderate uses “Ease off when you can.” L/RShoulder additionally prompt after >90 s without a qualifying rest gap (≥0.2 s below the calculated Rest Line), using “Let your arms hang for a breath.” Only the highest eligible condition per sensor is used; the learner card prioritizes High if several sensors qualify. The panel shows all qualifying sensors.

**Timing example:** 25% MVIC at the left shoulder for exactly 3 s is silent. The first fresh reading after 3 s displays the prompt immediately, with no extra 2 s wait. A below-rule reading clears it immediately. Repeating the action can prompt again immediately after its duration condition is met; there is no 25 s lockout or three-message limit. Disconnection/gaps clear the cue and restart duration tracking.

## Units: %MVIC is not µV

The supplied documents specify **%MVIC**, while the H10 bridge supplies **µV**. No µV threshold or heart-rate/bpm threshold was supplied, so those units are never substituted.

For raw H10 data, expand **Calibration values** on the website (or **Rest/max values** in Unity) and load existing File B JSON, or enter the sensor's measured rest, restSD and max directly. The same document formula converts RMS to %MVIC. The website can also export the current browser-local values as JSON. No timed calibration workflow is required by this dashboard.

For an existing Unity source already producing normalized 2 s RMS, send each fresh sample directly:

```csharp
coach.SubmitPercentMVIC(channelId, percentMVIC, signalTimestampMs, restLinePercent);
// A disconnect, reassignment, or known signal gap:
coach.ClearChannel(channelId);
```

The rest-line parameter is optional for High/Moderate; no-rest feedback requires a known Rest Line. Supply normalized data from the actual source, not a relabeled raw µV value. Inputs expire when stale.

Back raw RMS still requires the documents' 100 ms R-wave blanking before RMS. The repository's ordinary browser stream has no R-wave detector, so uncleaned back data is shown but not used for coaching. A real upstream cleaning stage must supply `ecg_blanked: true` and `ecg_blanking_window_ms: 100`; these fields do not perform cleaning themselves.

## Implementation and source differences

The Unity entry point is `Assets/PolarH10/Runtime/PolarThresholdCoach.cs`: the small `PolarThresholdEvaluator` checks amplitude and duration, setup gating restricts evaluation to the current Bending or Overhead sensor list, and the component sends the resulting text directly to `PolarLearnerPrompt`. `PolarRmsDashboard` attaches it automatically. The independent browser app lives under `Tools/PolarH10Dashboard`, with `PolarRuleDashboard.ts` backed by the DOM-free `PolarRuleEngine.mjs`. `server.mjs` broadcasts each validated adapter packet only to dashboard WebSocket clients while continuing to forward it to Unity over UDP, so the acquisition UI stays separate and both coaching views use the same RMS windows and metadata validation. The previous `PolarCoachingSession` is no longer started by the dashboard; an old manually attached instance is disabled to avoid competing prompts. Legacy workflow helpers remain unused by this entry point.

Threshold values and units agree between File A (**Unity EMG Dashboard_Developer Manual.docx**) and priority File B (**EMG Calibration Guide_For Enhui.docx**). **Discrepancy:** File A occasionally says six overhead dots, while File B permits four or six; connected source locations are shown. See [merged source rules](EMG_COACHING_RULES.md). Per the user's latest clarification, duration conditions stay, but session/delivery restrictions are excluded.
