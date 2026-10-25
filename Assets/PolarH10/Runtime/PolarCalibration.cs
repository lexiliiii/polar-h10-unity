using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Module3.PolarH10
{
    /// <summary>File B calibration capture, driven by accepted sample timestamps.</summary>
    public sealed class PolarCalibrationCapture
    {
        public string Channel, Phase = "Idle", Message = "";
        public double Rest, RestSD, Quiet;
        public bool Checked, Rose;
        public double CheckAt = double.NegativeInfinity, LastAt = double.NaN;
        public int TrialCount => maxima.Count;
        public double RemainingSeconds { get; private set; }
        public bool CanRedoMaximum => baselineCaptured && Checked && (double.IsNaN(LastAt) || CheckAt > LastAt);
        private double started = double.NaN, until, maximum;
        private long segment;
        private bool baselineCaptured;
        private PolarChannelCalibration loadedCalibration;
        private readonly List<double> restValues = new List<double>();
        private readonly List<double> maxima = new List<double>();

        public PolarCalibrationCapture(string channel) { Channel = channel; }

        public void RestoreBaseline(PolarChannelCalibration calibration)
        {
            if (!PolarCoachingRules.ValidateCalibration(calibration, out var reason)) throw new ArgumentException(reason);
            Rest = calibration.rest;
            RestSD = calibration.restSD;
            baselineCaptured = true;
            loadedCalibration = calibration;
            Phase = "Loaded";
        }

        public void BeginCheck(double quiet)
        {
            if (!Finite(quiet) || quiet <= 0) throw new InvalidOperationException("Wait for a current quiet signal before the tense test.");
            Quiet = quiet;
            Checked = Rose = false;
            CheckAt = double.NegativeInfinity;
            Phase = "Sensor check";
            RemainingSeconds = 0;
            Message = "Tense the muscle, then relax back to the quiet level.";
        }

        public void BeginRest()
        {
            if (!Checked) throw new InvalidOperationException("Complete the signal check first.");
            restValues.Clear();
            maxima.Clear();
            baselineCaptured = false;
            loadedCalibration = null;
            Begin("Rest");
            RemainingSeconds = 30;
        }

        private void Begin(string phase) { Phase = phase; Message = ""; started = LastAt = double.NaN; }

        public void Practice()
        {
            if (Phase != "Practice") throw new InvalidOperationException("Finish rest calibration first.");
            Phase = "Ready";
        }

        public void BeginMaximum(double now)
        {
            Tick(now);
            if (Phase != "Ready") throw new InvalidOperationException("Complete practice and the 60 s rest first.");
            maximum = double.NegativeInfinity;
            Begin("Maximum");
            RemainingSeconds = 5;
        }

        public void RedoMaximum()
        {
            if (!CanRedoMaximum) throw new InvalidOperationException("Complete rest calibration and repeat the signal check before redoing maximum trials.");
            maxima.Clear();
            loadedCalibration = null;
            Begin("Practice");
        }

        public void Stop()
        {
            Phase = "NotCalibrated";
            Message = "Group stopped. Marked NotCalibrated.";
            RemainingSeconds = 0;
            Checked = Rose = baselineCaptured = false;
            loadedCalibration = null;
        }

        public void Interrupt()
        {
            if (Phase == "Rest" || Phase == "Maximum")
            {
                Phase = "Interrupted";
                Message = "Signal interrupted; repeat this recording.";
            }
            Checked = Rose = false;
            RemainingSeconds = 0;
        }

        public void Tick(double now)
        {
            if (Phase != "Rest between trials") return;
            RemainingSeconds = Math.Max(0, until - now);
            if (now >= until) Phase = "Ready";
        }

        public void Sample(PolarRegion r, double now)
        {
            if (r == null || !Finite(r.timestampMs) || !Finite(r.rmsUv) || r.rmsUv < 0) { Interrupt(); return; }
            if (Phase == "Sensor check" && Quiet > 0 && !Checked)
            {
                if (r.rmsUv >= 2 * Quiet) Rose = true;
                else if (Rose && r.rmsUv <= Quiet) { Checked = true; CheckAt = r.timestampMs; Message = "Signal check passed."; }
            }
            if (Phase != "Rest" && Phase != "Maximum") return;
            if (!double.IsNaN(LastAt) && r.timestampMs <= LastAt) return;
            if (!double.IsNaN(LastAt) && (r.signalGap || r.signalSegment != segment || r.timestampMs - LastAt > 1000.0 / 130 + 1))
            {
                Interrupt();
                return;
            }
            if (double.IsNaN(started)) { started = r.timestampMs; segment = r.signalSegment; }
            LastAt = r.timestampMs;
            double elapsed = r.timestampMs - started;
            if (Phase == "Rest")
            {
                RemainingSeconds = Math.Max(0, 30 - elapsed / 1000);
                // Only complete 2 s windows wholly inside the retained middle 20 s.
                if (elapsed >= 7000 && elapsed <= 25000)
                {
                    if (!r.hasDecisionRms || !Finite(r.rms2sUv) || r.rms2sUv < 0) { Interrupt(); return; }
                    restValues.Add(r.rms2sUv);
                }
                if (elapsed >= 30000)
                {
                    if (restValues.Count == 0) { Interrupt(); return; }
                    Rest = restValues.Average();
                    RestSD = Math.Sqrt(restValues.Average(x => (x - Rest) * (x - Rest)));
                    baselineCaptured = true;
                    Phase = "Practice";
                }
            }
            else
            {
                RemainingSeconds = Math.Max(0, 5 - elapsed / 1000);
                if (elapsed >= 1000 && elapsed <= 5000)
                {
                    if (!r.hasCalibrationRms || !Finite(r.rms1sUv) || r.rms1sUv < 0) { Interrupt(); return; }
                    maximum = Math.Max(maximum, r.rms1sUv);
                }
                if (elapsed >= 5000)
                {
                    if (double.IsNegativeInfinity(maximum)) { Interrupt(); return; }
                    maxima.Add(maximum);
                    if (maxima.Count == 3)
                    {
                        Phase = MaximumResult().status;
                        if (Phase == "Weak") Checked = false;
                    }
                    else
                    {
                        Phase = "Rest between trials";
                        until = now + 60;
                        RemainingSeconds = 60;
                    }
                }
            }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private PolarChannelCalibration MaximumResult()
        {
            double max = maxima.Max();
            var calibration = new PolarChannelCalibration { channel = Channel, rest = Rest, restSD = RestSD, max = max, status = "OK" };
            if (!PolarCoachingRules.ValidateCalibration(calibration, out _)) calibration.status = "Weak";
            return calibration;
        }

        public PolarChannelCalibration Result()
        {
            if (Phase == "NotCalibrated") return new PolarChannelCalibration { channel = Channel, status = "NotCalibrated" };
            if (Phase == "Loaded") return loadedCalibration;
            return Phase == "OK" || Phase == "Weak" ? MaximumResult() : null;
        }
    }

    public static class PolarCalibrationFile
    {
        public static Dictionary<string, PolarChannelCalibration> Parse(string json, string participant, string setup, IEnumerable<string> active, DateTime day)
        {
            var root = JObject.Parse(json);
            if (string.IsNullOrWhiteSpace(participant) || (string)root["participantId"] != participant) throw new FormatException("Calibration participant does not match.");
            if ((setup != "Bending" && setup != "Overhead") || (string)root["setup"] != setup) throw new FormatException("Calibration task does not match.");
            if (!DateTime.TryParse((string)root["time"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) || time.ToLocalTime().Date != day.Date)
                throw new FormatException("Load calibration from the current local day.");
            var source = root["channels"] as JObject;
            if (source == null) throw new FormatException("Missing channel calibration.");
            var result = new Dictionary<string, PolarChannelCalibration>();
            foreach (var id in active)
            {
                var row = source[id] as JObject;
                if (row == null) throw new FormatException("Calibrate or mark NotCalibrated: " + id);
                string status = (string)row["status"];
                if (status == "NotCalibrated") { result[id] = new PolarChannelCalibration { channel = id, status = status }; continue; }
                if (status != "OK" || !Number(row["rest"], out double rest) || !Number(row["restSD"], out double restSD) || !Number(row["max"], out double max))
                    throw new FormatException("Invalid calibration: " + id);
                var c = new PolarChannelCalibration { channel = id, status = status, rest = rest, restSD = restSD, max = max };
                if (!PolarCoachingRules.ValidateCalibration(c, out var reason)) throw new FormatException(id + ": " + reason);
                result[id] = c;
            }
            return result;
        }

        private static bool Number(JToken token, out double value)
        {
            value = 0;
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)) return false;
            value = (double)token;
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static string Serialize(string participant, string setup, Dictionary<string, PolarChannelCalibration> calibrations)
        {
            if (string.IsNullOrWhiteSpace(participant)) throw new FormatException("Enter a participant ID.");
            if (setup != "Bending" && setup != "Overhead") throw new FormatException("Select Bending or Overhead.");
            var channels = new JObject();
            foreach (var entry in calibrations)
            {
                var c = entry.Value;
                bool ok = c != null && c.status == "OK";
                if (c == null || (!ok && c.status != "NotCalibrated") || (ok && !PolarCoachingRules.ValidateCalibration(c, out _)))
                    throw new FormatException("Finish calibration or mark NotCalibrated: " + entry.Key);
                channels[entry.Key] = new JObject
                {
                    ["rest"] = ok ? new JValue(c.rest) : JValue.CreateNull(),
                    ["restSD"] = ok ? new JValue(c.restSD) : JValue.CreateNull(),
                    ["max"] = ok ? new JValue(c.max) : JValue.CreateNull(),
                    ["status"] = c.status
                };
            }
            return new JObject { ["participantId"] = participant, ["setup"] = setup, ["time"] = DateTimeOffset.Now.ToString("o"), ["channels"] = channels, ["sensorPhotos"] = false }.ToString();
        }
    }
}
