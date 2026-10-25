using System;
using System.Collections.Generic;
using System.Globalization;

namespace Module3.PolarH10
{
    public enum PolarEffortLevel { Unavailable, Rest, Low, Moderate, High }
    public enum PolarCoachingKind { High, Moderate, NoRest }

    [Serializable]
    public class PolarChannelCalibration
    {
        public string channel;
        public double rest, restSD, max;
        public string status = "NotCalibrated";
    }

    public class PolarCoachingSettings
    {
        public string setup = "Overhead";
        public bool includeForearms;
        public int stage = 1;
        public bool finalBout;
        public bool continuous = true;
        public bool running = true;
        public Dictionary<string, PolarChannelCalibration> calibrations = new Dictionary<string, PolarChannelCalibration>();
    }

    public class PolarCoachingObservation
    {
        public string channel;
        public double rmsUv;
        // Device/signal time in milliseconds. Repeated times never extend exposure.
        public double signalTimestampMs;
        public int windowMs = 2000;
        public bool valid = true;
        public bool ecgBlanked;
    }

    public class PolarChannelRule
    {
        public readonly string channel, label, group, action;
        public readonly double moderateThreshold, highThreshold, highHoldMs;
        public readonly bool corrective, noRest, requiresEcgBlanking;
        internal PolarChannelRule(string channel, string label, string group, bool noRest = false)
        {
            this.channel = channel; this.label = label; this.group = group; this.noRest = noRest;
            bool shoulder = group == "shoulder";
            moderateThreshold = shoulder ? 10 : 15;
            highThreshold = shoulder ? 20 : 30;
            highHoldMs = shoulder ? 3000 : 5000;
            corrective = group != "thigh";
            requiresEcgBlanking = group == "back";
            action = shoulder ? "Lower your arm or bring work closer."
                : group == "back" ? "Bend your knees and keep the load close."
                : group == "forearm" ? "Loosen your grip on the tool." : null;
        }
    }

    public class PolarEffortReading
    {
        public PolarEffortLevel level;
        public double percentMVIC, restLine, signalTimestampMs;
    }

    public class PolarCoachingMessage
    {
        public string channel, text;
        public PolarCoachingKind kind;
        public PolarEffortLevel level;
        public double percentMVIC, restLine, durationMs, signalTimestampMs;
        public double shownAtSeconds, expiresAtSeconds;
        internal PolarCoachingMessage Copy() { return (PolarCoachingMessage)MemberwiseClone(); }
    }

    public class PolarChannelBoutSummary
    {
        public string channel;
        public double measuredSeconds, restPercent, lowPercent, moderatePercent, highPercent;
        public double longestHighSeconds, restMomentsPerMinute;
        public int restMoments;
        public PolarChannelBoutSummary baseline;
    }

    public class PolarBoutSummary
    {
        public int number, alerts, highAlerts, stage, nextStage;
        public bool finalBout, hasData;
        public double startedAtSeconds, endedAtSeconds;
        public Dictionary<string, PolarChannelBoutSummary> channels = new Dictionary<string, PolarChannelBoutSummary>();
    }

    public class PolarCoachingEvent
    {
        public string type, channel, reason, text;
        public PolarCoachingMessage message;
        public PolarBoutSummary summary;
    }

    public class PolarCoachingResult
    {
        public PolarCoachingMessage message;
        public string spokenReminder;
        public Dictionary<string, PolarEffortReading> levels;
        public List<PolarCoachingEvent> events;
        public PolarBoutSummary summary;
        public int stage, boutAlerts, boutHighAlerts, boutNumber;
        public bool dotsEnabled, liveEnabled, running;
    }

    /// <summary>Source: Unity EMG Developer Manual (A), Calibration Guide for Enhui (B).
    /// File B takes precedence. Effort is calibrated %MVIC, never a heart-rate threshold.</summary>
    public static class PolarCoachingRules
    {
        public const int CorrectiveWindowMs = 2000;
        public const int AvatarWindowMs = 200;
        public const int EcgBlankingMs = 100;
        public const double ModerateHoldMs = 30000, NoRestHoldMs = 90000, RestGapMs = 200;
        public const double ChannelCooldownSeconds = 25, PauseWaitSeconds = 2, MessageHoldSeconds = 5;
        public const double ContinuousBoutSeconds = 120, SpokenReminderSeconds = 10;
        public const int AlertsPerBout = 3;
        public static readonly string[] DiscrepancyNotes = {
            "Shared effort thresholds agree. File B allows four overhead channels or six with optional forearms; this overrides File A's occasional six-dot wording.",
            "File B uses the middle 20 s of the 30 s rest recording.",
            "File B's main tense-test rule uses >=2x; its QA checklist says >2x. The main rule takes precedence.",
            "File B's explicit save rule allows OK or NotCalibrated, despite an all-OK illustration.",
            "File A leaves equality at 90 s without rest unspecified; coaching is emitted only beyond 90 s.",
            "File A does not quantify clearly worse for stage regression; the researcher decides regression."
        };
        public static string[] Discrepancies { get { return DiscrepancyNotes; } }
        public static readonly IReadOnlyDictionary<string, PolarChannelRule> Channels = BuildChannels();

        private static IReadOnlyDictionary<string, PolarChannelRule> BuildChannels()
        {
            var rules = new Dictionary<string, PolarChannelRule>();
            foreach (var side in new[] { "L", "R" }) {
                string label = side == "L" ? "left " : "right ";
                rules.Add(side + "Shoulder", new PolarChannelRule(side + "Shoulder", label + "shoulder", "shoulder", true));
                rules.Add(side + "FrontShoulder", new PolarChannelRule(side + "FrontShoulder", label + "front shoulder", "shoulder"));
                rules.Add(side + "LowerSpine", new PolarChannelRule(side + "LowerSpine", label + "lower back", "back"));
                rules.Add(side + "UpperBack", new PolarChannelRule(side + "UpperBack", label + "upper back", "back"));
                rules.Add(side + "LowerArm", new PolarChannelRule(side + "LowerArm", label + "forearm", "forearm"));
                rules.Add(side + "Thigh", new PolarChannelRule(side + "Thigh", label + "thigh", "thigh"));
            }
            return rules;
        }

        public static string[] ActiveChannels(string setup, bool includeForearms = false)
        {
            if (setup == "Bending") return new[] { "LLowerSpine", "RLowerSpine", "LUpperBack", "RUpperBack", "LThigh", "RThigh" };
            if (setup == "Overhead") return includeForearms
                ? new[] { "LShoulder", "RShoulder", "LFrontShoulder", "RFrontShoulder", "LLowerArm", "RLowerArm" }
                : new[] { "LShoulder", "RShoulder", "LFrontShoulder", "RFrontShoulder" };
            return new string[0];
        }

        public static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }

        public static bool ValidateCalibration(PolarChannelCalibration calibration, out string reason)
        {
            if (calibration == null || calibration.status != "OK") {
                reason = calibration != null && calibration.status == "NotCalibrated" ? "NotCalibrated" : "Missing calibration"; return false;
            }
            if (!Finite(calibration.rest) || !Finite(calibration.restSD) || !Finite(calibration.max)
                || calibration.rest < 0 || calibration.restSD < 0 || calibration.max <= calibration.rest) {
                reason = "Invalid calibration values"; return false;
            }
            if (calibration.max / calibration.rest < 3.0) { reason = "Weak: check sensor, then redo this group."; return false; }
            reason = null; return true;
        }

        public static double Normalize(double liveRms, PolarChannelCalibration calibration)
        {
            if (!Finite(liveRms) || liveRms < 0 || calibration == null || !Finite(calibration.rest) || !Finite(calibration.max)) return double.NaN;
            double denominatorSquared = calibration.max * calibration.max - calibration.rest * calibration.rest;
            if (denominatorSquared <= 0) return 0;
            double numerator = Math.Sqrt(Math.Max(liveRms * liveRms - calibration.rest * calibration.rest, 0));
            return Math.Min(150, Math.Max(0, 100 * numerator / Math.Sqrt(denominatorSquared)));
        }

        public static double RestLine(PolarChannelCalibration calibration)
        {
            string reason;
            return ValidateCalibration(calibration, out reason) ? Math.Max(0.5, Normalize(calibration.rest + 3 * calibration.restSD, calibration)) : double.NaN;
        }

        public static PolarEffortLevel Classify(string channel, double percentMVIC, double restLine)
        {
            PolarChannelRule rule;
            if (channel == null || !Channels.TryGetValue(channel, out rule) || !Finite(percentMVIC) || percentMVIC < 0 || !Finite(restLine)) return PolarEffortLevel.Unavailable;
            if (percentMVIC < restLine) return PolarEffortLevel.Rest;
            if (percentMVIC > rule.highThreshold) return PolarEffortLevel.High;
            if (percentMVIC >= rule.moderateThreshold) return PolarEffortLevel.Moderate;
            return PolarEffortLevel.Low;
        }

        private static string Number(double value, params double[] boundaries)
        {
            double rounded = Math.Round(value, 2);
            foreach (double boundary in boundaries) if (rounded == boundary && value != boundary) return value.ToString("R", CultureInfo.InvariantCulture);
            return rounded.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string FormatMessage(PolarCoachingMessage message, int stage = 1)
        {
            var rule = Channels[message.channel];
            string value = Number(message.percentMVIC, rule.moderateThreshold, rule.highThreshold, message.restLine);
            string duration = Number(message.durationMs / 1000, rule.highHoldMs / 1000, 30, 90);
            string prefix = "Heads up: your " + rule.label + " sensor is at " + value + "% MVIC";
            string action = stage == 2 ? rule.group == "shoulder" ? "Arm down." : rule.group == "back" ? "Knees bent, load close." : "Loosen your grip." : rule.action;
            if (message.kind == PolarCoachingKind.High)
                return prefix + ", above the " + Number(rule.highThreshold) + "% MVIC High threshold for " + duration
                    + " s—longer than the " + Number(rule.highHoldMs / 1000) + " s trigger. " + action;
            if (message.kind == PolarCoachingKind.Moderate)
                return prefix + " in the " + Number(rule.moderateThreshold) + "–" + Number(rule.highThreshold)
                    + "% MVIC Moderate range and has stayed there for " + duration + " s, longer than the 30 s limit. " + (stage == 2 ? "Ease off." : "Ease off when you can.");
            return prefix + " and has gone " + duration + " s without a rest gap of at least 0.2 s, beyond the 90 s no-rest threshold. " + (stage == 2 ? "Quick break: arms hang." : "Let your arms hang for a breath.");
        }
    }

    /// <summary>Visual-only hysteresis from File A; never use this delayed level for coaching decisions.</summary>
    public class PolarEffortDisplay
    {
        private class State { public PolarEffortLevel level, pending; public double since, at; }
        private readonly Dictionary<string, State> states = new Dictionary<string, State>();
        public void Reset(string channel) { if (channel != null) states.Remove(channel); }
        public void Clear() { states.Clear(); }
        public PolarEffortLevel Update(string channel, double percentMVIC, double restLine, double timeSeconds)
        {
            var raw = PolarCoachingRules.Classify(channel, percentMVIC, restLine);
            if (raw == PolarEffortLevel.Unavailable || !PolarCoachingRules.Finite(timeSeconds)) { Reset(channel); return PolarEffortLevel.Unavailable; }
            State state;
            if (!states.TryGetValue(channel, out state) || timeSeconds < state.at) {
                states[channel] = new State { level = raw, pending = raw, since = timeSeconds, at = timeSeconds }; return raw;
            }
            state.at = timeSeconds;
            if (raw >= state.level) { state.level = raw; state.pending = raw; state.since = timeSeconds; return raw; }
            var rule = PolarCoachingRules.Channels[channel];
            var desired = state.level;
            if (percentMVIC < restLine) desired = PolarEffortLevel.Rest;
            else if (percentMVIC < .85 * rule.moderateThreshold) desired = PolarEffortLevel.Low;
            else if (state.level == PolarEffortLevel.High && percentMVIC < .85 * rule.highThreshold) desired = PolarEffortLevel.Moderate;
            if (desired == state.level) { state.pending = state.level; state.since = timeSeconds; return state.level; }
            if (desired != state.pending) { state.pending = desired; state.since = timeSeconds; }
            if (timeSeconds - state.since >= .5) state.level = desired;
            return state.level;
        }
    }

    /// <summary>Pure rule engine. Tick wall time is monotonic seconds; observations use signal milliseconds.
    /// Supply complete 2 s RMS values only. Call ResetChannel for known missing-data gaps, reconnects,
    /// sensor reassignment, or changed preprocessing; the engine never invents a gap timeout.</summary>
    public class PolarCoachingEngine
    {
        private class BoutMetrics
        {
            public readonly double[] elapsed = new double[5];
            public double longestHighMs;
            public int restMoments;
        }
        private class ChannelState
        {
            public double? at, highSince, moderateSince, noRestSince, restSince, pendingSinceSeconds;
            public double percentMVIC, restLine;
            public PolarEffortLevel level;
            public double lastMessageSeconds = double.NegativeInfinity;
            public double? reminderSignalMs, reminderShownSeconds;
            public PolarCoachingMessage eligible;
            public string skipReason;
            public bool countedRest;
            public double? metricHighSince;
            public BoutMetrics metrics = new BoutMetrics();
        }
        private PolarCoachingSettings settings;
        private HashSet<string> activeChannels;
        private readonly Dictionary<string, ChannelState> channels = new Dictionary<string, ChannelState>();
        private readonly List<PolarCoachingEvent> queuedEvents = new List<PolarCoachingEvent>();
        private PolarCoachingMessage activeMessage;
        private double? lastNowSeconds, boutStartedSeconds;
        private int boutNumber = 1, boutAlerts, boutHighAlerts, goodBouts;
        private bool boutHasData, spokenThisBout;
        private Dictionary<string, PolarChannelBoutSummary> baseline;
        public int Stage { get; private set; }
        public bool Running { get; private set; }
        public bool LiveEnabled { get { return Running && Stage < 3 && !settings.finalBout; } }

        public PolarCoachingEngine(PolarCoachingSettings settings)
        {
            this.settings = settings ?? new PolarCoachingSettings();
            Stage = ValidStage(this.settings.stage); Running = this.settings.running;
            activeChannels = new HashSet<string>(PolarCoachingRules.ActiveChannels(this.settings.setup, this.settings.includeForearms));
        }

        private static int ValidStage(int stage) { return stage >= 1 && stage <= 3 ? stage : 1; }

        public void Configure(PolarCoachingSettings next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            ResetSignal("Settings changed");
            int nextStage = ValidStage(next.stage);
            if (nextStage != Stage) goodBouts = 0;
            Stage = nextStage; settings = next;
            activeChannels = new HashSet<string>(PolarCoachingRules.ActiveChannels(settings.setup, settings.includeForearms));
        }

        public void ResetChannel(string channel, string reason = "Signal continuity reset")
        {
            if (string.IsNullOrEmpty(channel)) return;
            ChannelState previous;
            channels.TryGetValue(channel, out previous);
            if ((previous != null && previous.pendingSinceSeconds.HasValue) || (activeMessage != null && activeMessage.channel == channel))
                queuedEvents.Add(new PolarCoachingEvent { type = "cancelled", channel = channel, reason = reason });
            channels[channel] = new ChannelState { lastMessageSeconds = previous == null ? double.NegativeInfinity : previous.lastMessageSeconds,
                metrics = previous == null ? new BoutMetrics() : previous.metrics };
            if (activeMessage != null && activeMessage.channel == channel) activeMessage = null;
        }

        public void ResetSignal(string reason = "Signal continuity reset")
        {
            foreach (string channel in new List<string>(channels.Keys)) ResetChannel(channel, reason);
            activeMessage = null;
        }

        public void StartBout(double nowSeconds)
        {
            RequireTime(nowSeconds);
            ResetSignal("New task bout");
            boutStartedSeconds = nowSeconds; boutAlerts = 0; boutHighAlerts = 0;
            boutHasData = false; spokenThisBout = false; Running = true;
            foreach (var state in channels.Values) state.metrics = new BoutMetrics();
            queuedEvents.Add(new PolarCoachingEvent { type = "bout-started" });
        }

        public PolarBoutSummary EndBout(double nowSeconds)
        {
            RequireTime(nowSeconds);
            var summary = FinishBout(nowSeconds);
            ResetSignal("New task bout"); Running = false;
            return summary;
        }

        private PolarBoutSummary FinishBout(double nowSeconds)
        {
            var summary = new PolarBoutSummary { number = boutNumber, alerts = boutAlerts, highAlerts = boutHighAlerts,
                stage = Stage, finalBout = settings.finalBout, hasData = boutHasData,
                startedAtSeconds = boutStartedSeconds ?? nowSeconds, endedAtSeconds = nowSeconds };
            foreach (var entry in channels) {
                if (!activeChannels.Contains(entry.Key)) continue;
                var metrics = entry.Value.metrics; double total = 0;
                foreach (double elapsed in metrics.elapsed) total += elapsed;
                var item = new PolarChannelBoutSummary { channel = entry.Key, measuredSeconds = total / 1000,
                    longestHighSeconds = metrics.longestHighMs / 1000, restMoments = metrics.restMoments,
                    restMomentsPerMinute = total > 0 ? metrics.restMoments * 60000 / total : 0,
                    restPercent = total > 0 ? 100 * metrics.elapsed[(int)PolarEffortLevel.Rest] / total : 0,
                    lowPercent = total > 0 ? 100 * metrics.elapsed[(int)PolarEffortLevel.Low] / total : 0,
                    moderatePercent = total > 0 ? 100 * metrics.elapsed[(int)PolarEffortLevel.Moderate] / total : 0,
                    highPercent = total > 0 ? 100 * metrics.elapsed[(int)PolarEffortLevel.High] / total : 0 };
                PolarChannelBoutSummary original;
                if (baseline != null && baseline.TryGetValue(entry.Key, out original)) item.baseline = original;
                summary.channels.Add(entry.Key, item);
            }
            if (boutNumber == 1) baseline = summary.channels;
            if (boutHasData && !settings.finalBout && Stage < 3) {
                goodBouts = boutHighAlerts <= 1 ? goodBouts + 1 : 0;
                if (goodBouts >= 2) { Stage++; goodBouts = 0; }
            }
            boutNumber++; boutStartedSeconds = nowSeconds; boutAlerts = 0; boutHighAlerts = 0;
            boutHasData = false; spokenThisBout = false; activeMessage = null;
            foreach (var state in channels.Values) {
                state.pendingSinceSeconds = null; state.reminderSignalMs = null; state.reminderShownSeconds = null; state.skipReason = null;
                state.metrics = new BoutMetrics(); state.at = null; state.metricHighSince = null;
            }
            summary.nextStage = Stage;
            queuedEvents.Add(new PolarCoachingEvent { type = "bout-ended", summary = summary });
            return summary;
        }

        private static void RequireTime(double nowSeconds)
        {
            if (!PolarCoachingRules.Finite(nowSeconds)) throw new ArgumentException("A finite monotonic wall time in seconds is required.");
        }

        private bool Observe(PolarCoachingObservation observation)
        {
            if (observation == null || string.IsNullOrEmpty(observation.channel)) return false;
            string channel = observation.channel;
            PolarChannelRule rule; PolarChannelCalibration calibration = null; string reason;
            if (settings.calibrations != null) settings.calibrations.TryGetValue(channel, out calibration);
            if (!PolarCoachingRules.Channels.TryGetValue(channel, out rule) || !activeChannels.Contains(channel)
                || !PolarCoachingRules.ValidateCalibration(calibration, out reason) || !observation.valid
                || observation.windowMs != PolarCoachingRules.CorrectiveWindowMs
                || !PolarCoachingRules.Finite(observation.rmsUv) || observation.rmsUv < 0
                || !PolarCoachingRules.Finite(observation.signalTimestampMs)
                || (rule.requiresEcgBlanking && !observation.ecgBlanked)) {
                ResetChannel(channel, "Unavailable, uncalibrated, or unprocessed signal"); return false;
            }
            ChannelState state;
            if (!channels.TryGetValue(channel, out state)) { state = new ChannelState(); channels.Add(channel, state); }
            double at = observation.signalTimestampMs;
            if (state.at.HasValue && at < state.at.Value) { ResetChannel(channel, "Signal time moved backwards"); state = channels[channel]; }
            if (state.at.HasValue && at == state.at.Value) return false;
            if (state.at.HasValue) {
                state.metrics.elapsed[(int)state.level] += at - state.at.Value;
                if (state.metricHighSince.HasValue) state.metrics.longestHighMs = Math.Max(state.metrics.longestHighMs, at - state.metricHighSince.Value);
            }
            state.at = at; boutHasData = true;
            state.percentMVIC = PolarCoachingRules.Normalize(observation.rmsUv, calibration);
            state.restLine = PolarCoachingRules.RestLine(calibration);
            state.level = PolarCoachingRules.Classify(channel, state.percentMVIC, state.restLine);
            state.highSince = state.level == PolarEffortLevel.High ? state.highSince ?? at : (double?)null;
            state.metricHighSince = state.level == PolarEffortLevel.High ? state.metricHighSince ?? at : (double?)null;
            state.moderateSince = state.level == PolarEffortLevel.Moderate ? state.moderateSince ?? at : (double?)null;
            if (state.level == PolarEffortLevel.Rest) {
                state.restSince = state.restSince ?? at;
                if (at - state.restSince.Value >= PolarCoachingRules.RestGapMs) {
                    state.noRestSince = null;
                    if (rule.noRest && !state.countedRest) { state.metrics.restMoments++; state.countedRest = true; }
                }
            } else {
                if (state.restSince.HasValue && at - state.restSince.Value >= PolarCoachingRules.RestGapMs) {
                    state.noRestSince = null;
                    if (rule.noRest && !state.countedRest) state.metrics.restMoments++;
                }
                state.countedRest = false; state.restSince = null; state.noRestSince = state.noRestSince ?? at;
            }
            if (state.level != PolarEffortLevel.High) { state.reminderSignalMs = null; state.reminderShownSeconds = null; }
            var previous = state.eligible;
            state.eligible = Candidate(channel, state);
            if ((previous == null) != (state.eligible == null) || (previous != null && state.eligible != null && previous.kind != state.eligible.kind)) {
                if (state.pendingSinceSeconds.HasValue) queuedEvents.Add(new PolarCoachingEvent { type = "cancelled", channel = channel, reason = "Trigger no longer applies" });
                state.pendingSinceSeconds = null; state.skipReason = null;
            }
            if (activeMessage != null && activeMessage.channel == channel) {
                if (state.eligible == null || state.eligible.kind != activeMessage.kind) activeMessage = null;
                else {
                    var current = state.eligible.Copy(); current.shownAtSeconds = activeMessage.shownAtSeconds; current.expiresAtSeconds = activeMessage.expiresAtSeconds;
                    current.text = PolarCoachingRules.FormatMessage(current, Stage); activeMessage = current;
                }
            }
            return true;
        }

        private static PolarCoachingMessage Candidate(string channel, ChannelState state)
        {
            var rule = PolarCoachingRules.Channels[channel];
            if (!rule.corrective || state.level == PolarEffortLevel.Rest || state.level == PolarEffortLevel.Unavailable) return null;
            PolarCoachingKind kind; double duration;
            if (state.highSince.HasValue && state.at.Value - state.highSince.Value > rule.highHoldMs) {
                kind = PolarCoachingKind.High; duration = state.at.Value - state.highSince.Value;
            } else if (state.moderateSince.HasValue && state.at.Value - state.moderateSince.Value > PolarCoachingRules.ModerateHoldMs) {
                kind = PolarCoachingKind.Moderate; duration = state.at.Value - state.moderateSince.Value;
            } else if (rule.noRest && state.noRestSince.HasValue && state.at.Value - state.noRestSince.Value > PolarCoachingRules.NoRestHoldMs) {
                kind = PolarCoachingKind.NoRest; duration = state.at.Value - state.noRestSince.Value;
            } else return null;
            return new PolarCoachingMessage { channel = channel, kind = kind, level = state.level,
                percentMVIC = state.percentMVIC, restLine = state.restLine, durationMs = duration, signalTimestampMs = state.at.Value };
        }

        public PolarCoachingResult Tick(IReadOnlyList<PolarCoachingObservation> observations, double nowSeconds, bool movementPause = false)
        {
            RequireTime(nowSeconds);
            if (lastNowSeconds.HasValue && nowSeconds < lastNowSeconds.Value) throw new ArgumentException("Coaching wall time must not move backwards.");
            lastNowSeconds = nowSeconds; boutStartedSeconds = boutStartedSeconds ?? nowSeconds;
            int count = observations == null ? 0 : observations.Count;
            if (Running && settings.continuous && nowSeconds - boutStartedSeconds.Value >= PolarCoachingRules.ContinuousBoutSeconds) FinishBout(nowSeconds);
            var fresh = new HashSet<string>();
            if (Running) for (int i = 0; i < count; i++) if (Observe(observations[i])) fresh.Add(observations[i].channel);
            if (!LiveEnabled || (activeMessage != null && nowSeconds >= activeMessage.expiresAtSeconds)) activeMessage = null;
            string spokenReminder = null;
            var pending = new List<PolarCoachingMessage>();
            foreach (var entry in channels) {
                string channel = entry.Key; var state = entry.Value;
                if (!LiveEnabled) {
                    state.pendingSinceSeconds = null; state.reminderSignalMs = null; state.reminderShownSeconds = null;
                    if (fresh.Contains(channel) && state.eligible != null) {
                        string reason = settings.finalBout ? "Final evaluation bout" : "Stage 3 summary only";
                        if (state.skipReason != reason) queuedEvents.Add(new PolarCoachingEvent { type = "skipped", channel = channel, reason = reason, message = state.eligible.Copy() });
                        state.skipReason = reason;
                    }
                    continue;
                }
                if (!fresh.Contains(channel)) continue;
                if (!spokenThisBout && state.reminderSignalMs.HasValue && state.reminderShownSeconds.HasValue && state.level == PolarEffortLevel.High
                    && state.at.Value - state.reminderSignalMs.Value >= PolarCoachingRules.SpokenReminderSeconds * 1000
                    && nowSeconds - state.reminderShownSeconds.Value >= PolarCoachingRules.SpokenReminderSeconds) {
                    spokenReminder = settings.setup == "Bending" ? "Pause and rest your back" : "Pause and rest your shoulders";
                    spokenThisBout = true; queuedEvents.Add(new PolarCoachingEvent { type = "spoken-reminder", channel = channel, text = spokenReminder });
                }
                if (state.eligible == null) continue;
                string skipped = boutAlerts >= PolarCoachingRules.AlertsPerBout ? "Bout message cap"
                    : nowSeconds - state.lastMessageSeconds < PolarCoachingRules.ChannelCooldownSeconds ? "Channel cooldown" : null;
                if (skipped != null) {
                    state.pendingSinceSeconds = null;
                    if (state.skipReason != skipped) queuedEvents.Add(new PolarCoachingEvent { type = "skipped", channel = channel, reason = skipped, message = state.eligible.Copy() });
                    state.skipReason = skipped; continue;
                }
                state.skipReason = null;
                if (!state.pendingSinceSeconds.HasValue) {
                    state.pendingSinceSeconds = nowSeconds;
                    queuedEvents.Add(new PolarCoachingEvent { type = "triggered", channel = channel, message = state.eligible.Copy() });
                }
                pending.Add(state.eligible);
            }
            pending.Sort((a, b) => {
                int priority = a.kind.CompareTo(b.kind);
                if (priority != 0) return priority;
                int age = channels[a.channel].pendingSinceSeconds.Value.CompareTo(channels[b.channel].pendingSinceSeconds.Value);
                return age != 0 ? age : string.CompareOrdinal(a.channel, b.channel);
            });
            if (activeMessage == null && pending.Count > 0) {
                var chosen = pending[0]; var state = channels[chosen.channel];
                if (movementPause || nowSeconds >= state.pendingSinceSeconds.Value + PolarCoachingRules.PauseWaitSeconds) {
                    activeMessage = chosen.Copy(); activeMessage.text = PolarCoachingRules.FormatMessage(activeMessage, Stage);
                    activeMessage.shownAtSeconds = nowSeconds; activeMessage.expiresAtSeconds = nowSeconds + PolarCoachingRules.MessageHoldSeconds;
                    state.lastMessageSeconds = nowSeconds; state.pendingSinceSeconds = null; boutAlerts++;
                    if (chosen.kind == PolarCoachingKind.High) { boutHighAlerts++; state.reminderSignalMs = state.at; state.reminderShownSeconds = nowSeconds; }
                    queuedEvents.Add(new PolarCoachingEvent { type = "new-message", channel = chosen.channel, message = activeMessage.Copy() });
                }
            }
            var levels = new Dictionary<string, PolarEffortReading>();
            foreach (var entry in channels) if (activeChannels.Contains(entry.Key)) {
                var state = entry.Value;
                levels.Add(entry.Key, new PolarEffortReading { level = state.level, percentMVIC = state.percentMVIC, restLine = state.restLine,
                    signalTimestampMs = state.at ?? double.NaN });
            }
            PolarBoutSummary latestSummary = null;
            foreach (var item in queuedEvents) if (item.type == "bout-ended") latestSummary = item.summary;
            var result = new PolarCoachingResult { message = activeMessage == null ? null : activeMessage.Copy(), spokenReminder = spokenReminder,
                levels = levels, events = new List<PolarCoachingEvent>(queuedEvents), summary = latestSummary, stage = Stage,
                boutNumber = boutNumber, boutAlerts = boutAlerts, boutHighAlerts = boutHighAlerts, dotsEnabled = !settings.finalBout,
                liveEnabled = LiveEnabled, running = Running };
            queuedEvents.Clear();
            return result;
        }
    }
}
