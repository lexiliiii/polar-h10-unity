export const MODERATE_HOLD_MS = 30_000;
export const NO_REST_HOLD_MS = 90_000;
export const REST_GAP_MS = 200;

const rules = {};
for (const [side, sideLabel] of [["L", "left "], ["R", "right "]]) {
  const add = (suffix, label, group, noRest = false) => {
    const shoulder = group === "shoulder";
    rules[side + suffix] = Object.freeze({
      channel: side + suffix,
      label: sideLabel + label,
      group,
      noRest,
      moderateThreshold: shoulder ? 10 : 15,
      highThreshold: shoulder ? 20 : 30,
      highHoldMs: shoulder ? 3_000 : 5_000,
      corrective: group !== "thigh",
      requiresEcgBlanking: group === "back",
      action: shoulder
        ? "Lower your arm or bring work closer."
        : group === "back"
          ? "Bend your knees and keep the load close."
          : group === "forearm"
            ? "Loosen your grip on the tool."
            : "",
    });
  };
  add("Shoulder", "shoulder", "shoulder", true);
  add("FrontShoulder", "front shoulder", "shoulder");
  add("LowerSpine", "lower back", "back");
  add("UpperBack", "upper back", "back");
  add("LowerArm", "forearm", "forearm");
  add("Thigh", "thigh", "thigh");
}

export const CHANNEL_RULES = Object.freeze(rules);

export function activeChannels(setup, includeForearms = false) {
  if (setup === "Bending") {
    return ["LLowerSpine", "RLowerSpine", "LUpperBack", "RUpperBack", "LThigh", "RThigh"];
  }
  if (setup === "Overhead") {
    return includeForearms
      ? ["LShoulder", "RShoulder", "LFrontShoulder", "RFrontShoulder", "LLowerArm", "RLowerArm"]
      : ["LShoulder", "RShoulder", "LFrontShoulder", "RFrontShoulder"];
  }
  return [];
}

export function validateCalibration(calibration) {
  if (!calibration || calibration.status !== "OK") {
    return calibration?.status === "NotCalibrated" ? "NotCalibrated" : "Missing calibration";
  }
  const { rest, restSD, max } = calibration;
  if (![rest, restSD, max].every(Number.isFinite) || rest < 0 || restSD < 0 || max <= rest) {
    return "Invalid calibration values";
  }
  if (max / rest < 3) return "Weak: check sensor, then redo this group.";
  return "";
}

export function normalizeRms(liveRms, calibration) {
  if (!Number.isFinite(liveRms) || liveRms < 0 || validateCalibration(calibration)) return NaN;
  const denominatorSquared = calibration.max ** 2 - calibration.rest ** 2;
  if (denominatorSquared <= 0) return 0;
  const numerator = Math.sqrt(Math.max(liveRms ** 2 - calibration.rest ** 2, 0));
  return Math.min(150, Math.max(0, 100 * numerator / Math.sqrt(denominatorSquared)));
}

export function restLine(calibration) {
  return validateCalibration(calibration)
    ? NaN
    : Math.max(0.5, normalizeRms(calibration.rest + 3 * calibration.restSD, calibration));
}

export function classify(channel, percentMVIC, line) {
  const rule = CHANNEL_RULES[channel];
  if (!rule || !Number.isFinite(percentMVIC) || percentMVIC < 0 || !Number.isFinite(line)) return "Unavailable";
  if (percentMVIC < line) return "Rest";
  if (percentMVIC > rule.highThreshold) return "High";
  if (percentMVIC >= rule.moderateThreshold) return "Moderate";
  return "Low";
}

function displayNumber(value, ...boundaries) {
  const rounded = Math.round((value + Number.EPSILON) * 100) / 100;
  if (boundaries.some(boundary => rounded === boundary && value !== boundary)) return String(value);
  return rounded.toFixed(2).replace(/\.0+$|(?<=\.[0-9])0$/, "");
}

export function formatMessage(message) {
  const rule = CHANNEL_RULES[message.channel];
  const value = displayNumber(message.percentMVIC, rule.moderateThreshold, rule.highThreshold, message.restLine);
  const duration = displayNumber(message.durationMs / 1000, rule.highHoldMs / 1000, 30, 90);
  const prefix = `Heads up: your ${rule.label} sensor is at ${value}% MVIC`;
  if (message.kind === "High") {
    return `${prefix}, above the ${displayNumber(rule.highThreshold)}% MVIC High threshold for ${duration} s—longer than the ${displayNumber(rule.highHoldMs / 1000)} s trigger. ${rule.action}`;
  }
  if (message.kind === "Moderate") {
    return `${prefix} in the ${displayNumber(rule.moderateThreshold)}–${displayNumber(rule.highThreshold)}% MVIC Moderate range and has stayed there for ${duration} s, longer than the 30 s limit. Ease off when you can.`;
  }
  return `${prefix} and has gone ${duration} s without a rest gap of at least 0.2 s, beyond the 90 s no-rest threshold. Let your arms hang for a breath.`;
}

export class PolarThresholdEvaluator {
  constructor() { this.states = new Map(); }
  clear() { this.states.clear(); }
  reset(channel) { if (channel) this.states.delete(channel); }
  messages() {
    const order = { High: 0, Moderate: 1, NoRest: 2 };
    return [...this.states.values()]
      .map(state => state.message)
      .filter(Boolean)
      .sort((a, b) => order[a.kind] - order[b.kind] || a.channel.localeCompare(b.channel));
  }
  observe(channel, percent, atMs, line = NaN) {
    const rule = CHANNEL_RULES[channel];
    if (!rule || !Number.isFinite(percent) || percent < 0 || !Number.isFinite(atMs)) {
      this.reset(channel);
      return null;
    }
    let state = this.states.get(channel);
    if (!state) {
      state = { at: NaN, highSince: null, moderateSince: null, noRestSince: null, restSince: null, message: null };
      this.states.set(channel, state);
    }
    if (Number.isFinite(state.at) && atMs <= state.at) return state.message;
    state.at = atMs;
    const resting = Number.isFinite(line) && percent < line;
    const high = !resting && percent > rule.highThreshold;
    const moderate = !resting && percent >= rule.moderateThreshold && percent <= rule.highThreshold;
    state.highSince = high ? (state.highSince ?? atMs) : null;
    state.moderateSince = moderate ? (state.moderateSince ?? atMs) : null;
    if (resting) {
      state.restSince ??= atMs;
      if (atMs - state.restSince >= REST_GAP_MS) state.noRestSince = null;
    } else {
      if (state.restSince !== null && atMs - state.restSince >= REST_GAP_MS) state.noRestSince = null;
      state.restSince = null;
      state.noRestSince ??= atMs;
    }
    state.message = null;
    if (!rule.corrective || resting) return null;
    let kind = "";
    let durationMs = 0;
    if (state.highSince !== null && atMs - state.highSince > rule.highHoldMs) {
      kind = "High";
      durationMs = atMs - state.highSince;
    } else if (state.moderateSince !== null && atMs - state.moderateSince > MODERATE_HOLD_MS) {
      kind = "Moderate";
      durationMs = atMs - state.moderateSince;
    } else if (rule.noRest && Number.isFinite(line) && state.noRestSince !== null && atMs - state.noRestSince > NO_REST_HOLD_MS) {
      kind = "NoRest";
      durationMs = atMs - state.noRestSince;
    } else {
      return null;
    }
    state.message = { channel, kind, level: high ? "High" : moderate ? "Moderate" : "Low", percentMVIC: percent, restLine: line, durationMs, signalTimestampMs: atMs };
    state.message.text = formatMessage(state.message);
    return state.message;
  }
}
