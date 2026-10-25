# Merged EMG coaching rules

This rule set combines the two supplied documents for learner coaching. File B takes precedence over File A wherever they conflict. The user's request controls response wording and requires silence when no coaching threshold is exceeded.

- File A: [Unity EMG Dashboard Developer Manual](/Users/enhuili/Downloads/Unity%20EMG%20Dashboard_Developer%20Manual.docx), especially sections 2–4.
- File B: [EMG Calibration Guide For Enhui](/Users/enhuili/Downloads/EMG%20Calibration%20Guide_For%20Enhui.docx), especially sections 3–5.

## Inputs and units

Effort thresholds use **%MVIC**, the participant's calibrated percentage of maximum voluntary isometric contraction. Raw µV readings are not directly comparable with these percentage limits. Neither document supplies a heart-rate threshold in bpm; the user's heart-rate example is a formatting example only.

Evaluate corrective feedback using the **2.0 s moving RMS** signal. The **200 ms RMS** window is for avatar colour updates. A single instantaneous value cannot establish that a duration requirement has been met.

Use each channel's saved `rest`, `restSD`, and `max` calibration values. File B records rest for **30 s**, discards the first and last **5 s**, and calculates the mean and standard deviation of the 2 s RMS windows over the remaining **20 s**. Maximum effort is the highest **1 s moving RMS** across **3 trials**, each **5 s**, with **60 s** rest between trials; File B adds one practice try at **50% effort**.

The shared normalization formula is:

```text
numerator   = sqrt(max(liveRMS² − rest², 0))
denominator = sqrt(max² − rest²)
if denominator <= 0: return 0
percentMVIC = clamp(100 × numerator / denominator, 0, 150)
restLine    = max(0.5, CalculatePercentMVIC(rest + 3 × restSD))
```

Here `max` in `max²` is the saved maximum effort; `max(a, b)` selects the larger value. The 150% clamp is a calculation limit, not a coaching trigger.

Back channels require **100 ms ECG R-wave blanking** before RMS calculation. Do not assume supplied raw data has already undergone this step.

## Channel levels and corrective triggers

Rest is **below the channel's Rest Line**. For active readings outside Rest, use the stated Low, Moderate, and High bands. Preserve the strict `>` conditions for High amplitude and High duration; equality does not satisfy them.

| Sensor channels | Low | Moderate | High | High coaching condition | Document action |
| --- | --- | --- | --- | --- | --- |
| LShoulder, RShoulder, LFrontShoulder, RFrontShoulder | <10% MVIC | 10–20% MVIC | >20% MVIC | High for >3 s | Lower your arm or bring work closer. |
| LLowerSpine, RLowerSpine, LUpperBack, RUpperBack | <15% MVIC | 15–30% MVIC | >30% MVIC | High for >5 s | Bend your knees and keep the load close. |
| LLowerArm, RLowerArm | <15% MVIC | 15–30% MVIC | >30% MVIC | High for >5 s | Loosen your grip on the tool. |
| LThigh, RThigh | <15% MVIC | 15–30% MVIC | >30% MVIC | No corrective alerts; avatar colour only | None. |

At exactly **20% MVIC** for shoulders or **30% MVIC** for back, forearms, and thighs, the reading is in Moderate, not High. At exactly **3 s**, **5 s**, or **30 s**, the corresponding strict duration condition has not been exceeded.

Additional coaching conditions:

- **Sustained Moderate:** Moderate for **>30 s**, on channels eligible for messages. Action: “Ease off when you can.”
- **No rest:** File A specifies **90 s** for upper trapezius channels **LShoulder and RShoulder**. A qualifying rest gap lasts **at least 0.2 s** below the Rest Line. Action: “Let your arms hang for a breath.” The document does not specify `>` versus `≥` at exactly 90 s; the implementation remains silent at equality under the user’s exceeds-only rule.
- **Persistent High reminder:** If High persists **10 s after a message**, the runtime permits one spoken reminder per bout: “Pause and rest your shoulders” for overhead work or “Pause and rest your back” for bending. It does not count toward the text-message cap.

Choose the highest exceeded level for the channel. Do not emit separate Moderate and High prompts for the same reading, and do not relabel the document's levels as Warning or Critical.

## Eligibility and runtime delivery

**Current implementation:** The user clarified that document duration conditions must stay and a prompt must appear immediately when they are met. The Unity dashboard evaluates connected channels from the selected Bending or Overhead setup automatically, with no Session start, extra pause wait, cooldown, alert cap, stage or bout controls. The delivery rules listed below describe File A’s broader study protocol; they are not enforced by the simplified `PolarThresholdCoach` entry point.

Only channels in the active task setup can trigger coaching:

- **Bending:** LLowerSpine, RLowerSpine, LUpperBack, RUpperBack, LThigh, RThigh. Thighs remain colour-only.
- **Overhead:** LShoulder, RShoulder, LFrontShoulder, RFrontShoulder; optionally LLowerArm and RLowerArm when forearm sensors are used for hand tools.

File B specifies that **NotCalibrated** channels log raw µV but suppress avatar colour changes and learner text. Missing calibration or missing duration evidence cannot be replaced with assumed values.

File A additionally describes these delivery rules (excluded from the current simplified flow by the user’s clarification):

- Show one message at a time; High comes first if triggers coincide.
- Enforce a **25 s** lockout on the triggered channel and **3 alerts per bout**.
- A bout is one completed task, or **120 s** for a continuous task.
- Show a message at the next movement pause, waiting at most **2.0 s**, and hold it for **5.0 s**.
- Stages 1 and 2 permit live messages; Stage 3 is summary-only. The final evaluation bout has no dots, messages, or sounds.
- Stage 2 unlocks after **2 consecutive bouts with ≤1 High alert**; Stage 3 unlocks after **2 more such bouts**. File A's fallback rule is “clearly worse than the learner's best,” with no numerical criterion; do not invent one.

Avatar hysteresis is separate from coaching duration: drop to Rest after **< Rest Line for 0.5 s**; drop to Low after **below 85% of the Moderate limit for 0.5 s**; drop to Moderate after **below 85% of the High limit for 0.5 s**. Upward transitions are immediate when the relevant threshold is crossed.

The user's silence rule excludes File A's recovery praise. No learner message is emitted for Rest, Low, a short peak that fails the duration condition, an excluded channel, or a suppressed trigger. Describe these values as muscle effort, not safety or risk.

## Learner wording

Use 1–2 friendly sentences addressed to the learner. Name the actual channel's body part and side, report the supplied value and applicable threshold, and use only the document's action. These are templates, not readings or triggered messages:

**High**

> Heads up: your {side and body part} sensor is at {value}% MVIC, above the {20 or 30}% MVIC High threshold for {duration} s—longer than the {3 or 5} s trigger. {Matching action from the table}

**Sustained Moderate**

> Heads up: your {side and body part} sensor is at {value}% MVIC in the {10–20 or 15–30}% MVIC Moderate range and has stayed there for {duration} s, longer than the 30 s limit. Ease off when you can.

**No rest, when the known duration is beyond 90 s**

> Heads up: your {side} shoulder sensor has gone {duration} s without a rest gap of at least 0.2 s, beyond the 90 s no-rest threshold. Let your arms hang for a breath.

Do not present hypothetical or document-example values as live readings. Do not supply a learner prompt when the necessary channel, calibrated value, and qualifying duration are unavailable.

## Discrepancies and unresolved source details

**Threshold discrepancy check:** There are no conflicting shared activation thresholds, units, normalization formulas, or signal-quality ratio limits; File B specifies the middle 20 s of the 30 s rest recording for baseline calculations.

**Setup discrepancy:** File A's headset text sometimes says to show 6 dots for the chosen task; File B explicitly permits 4 overhead channels without forearms and 6 with them. Follow File B.

**Internal File B ambiguity:** The tense-test main rule uses **≥2× quiet level**, followed by a return to baseline; its QA checklist says **exceeds 2× rest amplitude**. File B precedence cannot resolve this internal equality discrepancy. The implementation follows the explicit main rule ≥2× and flags this discrepancy in the researcher view.

**Internal File B save-state discrepancy:** Its calibration illustration says all channels must be OK to save, while its explicit Step 4 and pain-bypass rules allow **OK or NotCalibrated**. The explicit state rules include the pain exception; retain it and flag the illustration for correction.

**Unspecified no-rest boundary:** File A supplies the 90 s trigger but no explicit comparison at equality. Durations beyond 90 s exceed it; the implementation remains silent at exactly 90 s, following the user’s exceeds-only rule.

File B's sensor-check and calibration events are researcher messages, not learner effort alerts: no data for **3 s**; failed tense test; visible heartbeat spikes; movement during rest; **max ÷ rest <3.0**; and reported pain. A ratio **≥3.0** passes. The actions are respectively checking connection, repositioning and repeating the check, moving the sensor and repeating the check, repeating rest, checking the sensor and redoing the group, and stopping the group as NotCalibrated. Do not convert these into invented upper-limit coaching thresholds.
