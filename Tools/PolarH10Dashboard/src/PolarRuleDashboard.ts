import {
  activeChannels,
  CHANNEL_RULES,
  classify,
  normalizeRms,
  PolarThresholdEvaluator,
  restLine,
  validateCalibration,
} from "./PolarRuleEngine.mjs";

type Setup = "Overhead" | "Bending";
type Calibration = { channel: string; rest: number; restSD: number; max: number; status: string };
type PolarPacket = {
  type: "polar_rms" | "polar_clear";
  region: string;
  rmsUv?: number;
  rms2sUv?: number;
  hasDecisionRms?: boolean;
  timestampMs?: number;
  ecgBlanked?: boolean;
  signalGap?: boolean;
  deviceName?: string;
};

const STORAGE_KEY = "polar-rule-dashboard-standalone-v1";
const STALE_AFTER_MS = 2_000;

export class PolarRuleDashboard {
  private root: HTMLElement;
  private cards!: HTMLElement;
  private prompt!: HTMLElement;
  private bridgeBadge!: HTMLElement;
  private valuesPanel!: HTMLElement;
  private forearmsRow!: HTMLElement;
  private evaluator = new PolarThresholdEvaluator();
  private packets = new Map<string, { packet: PolarPacket; receivedAt: number }>();
  private values = new Map<string, number>();
  private calibrations = new Map<string, Calibration>();
  private setup: Setup = "Overhead";
  private includeForearms = false;
  private showValues = false;
  private bridgeConnected = false;
  private sourceConnected = false;
  private renderTimer: number | null = null;
  private socket: WebSocket | null = null;
  private status = "Connect a Polar H10, select a body location, then enable EXG.";

  constructor(parent: HTMLElement) {
    this.load();
    this.root = document.createElement("aside");
    this.root.id = "polar-rule-dashboard";
    parent.appendChild(this.root);
    this.build();
    this.connect();
    window.setInterval(() => this.expire(), 250);
    this.render();
  }

  private build() {
    this.root.innerHTML = `
      <div class="prd-header">
        <div><div class="prd-eyebrow">RULE-BASED COACHING</div><h2>Muscle Effort</h2></div>
        <button class="prd-icon-button" type="button" aria-label="Collapse dashboard" data-action="collapse">−</button>
      </div>
      <div class="prd-body">
        <div class="prd-bridge-row"><span class="prd-status-dot"></span><span data-role="bridge">Bridge connecting…</span></div>
        <p class="prd-flow">Live RMS <span>→</span> %MVIC <span>→</span> document threshold <span>→</span> prompt</p>
        <div class="prd-segmented" role="group" aria-label="Task setup">
          <button type="button" data-setup="Overhead">Overhead</button>
          <button type="button" data-setup="Bending">Bending</button>
        </div>
        <label class="prd-check" data-role="forearms"><input type="checkbox" data-action="forearms"> Include optional forearm sensors</label>
        <div class="prd-prompt" data-role="prompt" hidden></div>
        <div class="prd-cards" data-role="cards"></div>
        <button class="prd-values-toggle" type="button" data-action="values"><span>Calibration values</span><span data-role="values-symbol">+</span></button>
        <div class="prd-values-panel" data-role="values-panel" hidden>
          <p>Enter each sensor's measured rest, restSD and max in µV, or load an existing File B JSON.</p>
          <div class="prd-value-actions">
            <button type="button" data-action="import">Load JSON</button>
            <button type="button" data-action="export">Export JSON</button>
            <input type="file" accept="application/json,.json" data-role="file" hidden>
          </div>
          <div data-role="calibration-rows"></div>
          <div class="prd-inline-status" data-role="status"></div>
        </div>
        <p class="prd-footnote">Trapezius: no qualifying rest for &gt;90 s. Rest gaps: ≥0.2 s. Conditions met → immediate prompt; below the rule → silence.</p>
      </div>`;
    this.cards = this.root.querySelector('[data-role="cards"]')!;
    this.prompt = this.root.querySelector('[data-role="prompt"]')!;
    this.bridgeBadge = this.root.querySelector('[data-role="bridge"]')!;
    this.valuesPanel = this.root.querySelector('[data-role="values-panel"]')!;
    this.forearmsRow = this.root.querySelector('[data-role="forearms"]')!;
    this.root.addEventListener("click", this.onClick);
    this.root.querySelector<HTMLInputElement>('[data-action="forearms"]')!.addEventListener("change", event => {
      this.includeForearms = (event.currentTarget as HTMLInputElement).checked;
      this.resetRules();
    });
    this.root.querySelector<HTMLInputElement>('[data-role="file"]')!.addEventListener("change", this.importFile);
  }

  private onClick = (event: MouseEvent) => {
    const button = (event.target as HTMLElement).closest<HTMLElement>("[data-action], [data-setup]");
    if (!button) return;
    if (button.dataset.setup) {
      this.setup = button.dataset.setup as Setup;
      if (this.setup === "Bending") this.includeForearms = false;
      this.resetRules();
      return;
    }
    switch (button.dataset.action) {
      case "collapse":
        this.root.classList.toggle("is-collapsed");
        button.textContent = this.root.classList.contains("is-collapsed") ? "+" : "−";
        button.setAttribute("aria-label", this.root.classList.contains("is-collapsed") ? "Expand dashboard" : "Collapse dashboard");
        break;
      case "values":
        this.showValues = !this.showValues;
        this.render();
        break;
      case "import":
        this.root.querySelector<HTMLInputElement>('[data-role="file"]')!.click();
        break;
      case "export": this.exportFile(); break;
      case "apply": this.applyCalibration(button.dataset.channel!); break;
      case "clear":
        this.calibrations.delete(button.dataset.channel!);
        this.evaluator.reset(button.dataset.channel!);
        this.status = `${button.dataset.channel} values cleared.`;
        this.save();
        this.render();
        break;
    }
  };

  private connect() {
    const host = window.location.hostname || "localhost";
    this.socket = new WebSocket(`ws://${host}:8765/dashboard`);
    this.socket.onopen = () => {
      this.bridgeConnected = true;
      this.render();
    };
    this.socket.onmessage = event => {
      try {
        const packet = JSON.parse(String(event.data));
        if (packet?.type === "polar_source") {
          this.sourceConnected = packet.connected === true;
          if (!this.sourceConnected) this.clearAllReadings();
          this.render();
        } else if (packet?.type === "polar_rms" || packet?.type === "polar_clear") {
          this.onPacket(packet);
        }
      } catch { /* Ignore malformed local bridge messages. */ }
    };
    this.socket.onclose = () => {
      this.bridgeConnected = false;
      this.sourceConnected = false;
      this.clearAllReadings();
      this.render();
      window.setTimeout(() => this.connect(), 1_000);
    };
    this.socket.onerror = () => this.socket?.close();
  }

  private clearAllReadings() {
      this.packets.clear();
      this.values.clear();
      this.evaluator.clear();
  }

  private onPacket(packet: PolarPacket) {
    if (!packet || !CHANNEL_RULES[packet.region]) return;
    if (packet.type === "polar_clear") {
      this.clearChannel(packet.region);
      return;
    }
    if (!activeChannels(this.setup, this.includeForearms).includes(packet.region)) return;
    if (packet.signalGap) this.evaluator.reset(packet.region);
    this.packets.set(packet.region, { packet, receivedAt: performance.now() });
    const calibration = this.calibrations.get(packet.region);
    const reason = validateCalibration(calibration);
    if (reason) {
      this.values.delete(packet.region);
      this.evaluator.reset(packet.region);
      this.scheduleRender();
      return;
    }
    const displayPercent = normalizeRms(packet.rmsUv, calibration);
    this.values.set(packet.region, displayPercent);
    const rule = CHANNEL_RULES[packet.region];
    if (rule.requiresEcgBlanking && !packet.ecgBlanked) {
      this.evaluator.reset(packet.region);
    } else if (packet.hasDecisionRms && Number.isFinite(packet.rms2sUv) && Number.isFinite(packet.timestampMs)) {
      this.evaluator.observe(packet.region, normalizeRms(packet.rms2sUv, calibration), packet.timestampMs, restLine(calibration));
    } else {
      this.evaluator.reset(packet.region);
    }
    this.scheduleRender();
  }

  private scheduleRender() {
    if (this.renderTimer !== null) return;
    this.renderTimer = window.setTimeout(() => {
      this.renderTimer = null;
      this.render();
    }, 100);
  }

  private clearChannel(channel: string) {
    this.packets.delete(channel);
    this.values.delete(channel);
    this.evaluator.reset(channel);
    this.render();
  }

  private expire() {
    const now = performance.now();
    let changed = false;
    for (const [channel, reading] of this.packets) {
      if (now - reading.receivedAt >= STALE_AFTER_MS) {
        this.packets.delete(channel);
        this.values.delete(channel);
        this.evaluator.reset(channel);
        changed = true;
      }
    }
    if (changed) this.render();
  }

  private resetRules() {
    this.evaluator.clear();
    this.values.clear();
    this.save();
    this.render();
  }

  private applyCalibration(channel: string) {
    const row = this.root.querySelector<HTMLElement>(`.prd-calibration-row[data-channel="${channel}"]`)!;
    const read = (name: string) => Number(row.querySelector<HTMLInputElement>(`[data-field="${name}"]`)!.value);
    const calibration = { channel, status: "OK", rest: read("rest"), restSD: read("restSD"), max: read("max") };
    const reason = validateCalibration(calibration);
    if (reason) {
      this.status = `${channel}: ${reason}`;
    } else {
      this.calibrations.set(channel, calibration);
      this.evaluator.reset(channel);
      this.status = `${channel} values applied.`;
      this.save();
    }
    this.render();
  }

  private importFile = async (event: Event) => {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    input.value = "";
    if (!file) return;
    try {
      const root = JSON.parse(await file.text());
      if (!root.channels || typeof root.channels !== "object") throw new Error("The JSON needs a channels object with rest, restSD, max and status.");
      const loaded = new Map<string, Calibration>();
      for (const [channel, raw] of Object.entries<any>(root.channels)) {
        if (!CHANNEL_RULES[channel]) continue;
        const calibration = { channel, status: raw.status ?? "OK", rest: Number(raw.rest), restSD: Number(raw.restSD), max: Number(raw.max) };
        if (calibration.status !== "NotCalibrated") {
          const reason = validateCalibration(calibration);
          if (reason) throw new Error(`${channel}: ${reason}`);
        }
        loaded.set(channel, calibration);
      }
      if (!loaded.size) throw new Error("No document sensor locations found.");
      this.calibrations = loaded;
      if (root.setup === "Overhead" || root.setup === "Bending") this.setup = root.setup;
      this.includeForearms = this.setup === "Overhead" && (loaded.has("LLowerArm") || loaded.has("RLowerArm"));
      this.status = `${file.name} loaded.`;
      this.resetRules();
    } catch (error) {
      this.status = error instanceof Error ? error.message : "Could not load calibration JSON.";
      this.render();
    }
  };

  private exportFile() {
    const channels = Object.fromEntries(this.calibrations);
    const blob = new Blob([JSON.stringify({ setup: this.setup, channels }, null, 2)], { type: "application/json" });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = `polar-calibration-${this.setup.toLowerCase()}.json`;
    link.click();
    URL.revokeObjectURL(link.href);
    this.status = "Calibration JSON exported.";
    this.render();
  }

  private load() {
    try {
      const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || "{}");
      if (saved.setup === "Overhead" || saved.setup === "Bending") this.setup = saved.setup;
      this.includeForearms = this.setup === "Overhead" && saved.includeForearms === true;
      for (const [channel, calibration] of Object.entries<Calibration>(saved.calibrations || {})) {
        if (CHANNEL_RULES[channel] && !validateCalibration(calibration)) this.calibrations.set(channel, calibration);
      }
    } catch { /* Ignore damaged browser-local settings. */ }
  }

  private save() {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ setup: this.setup, includeForearms: this.includeForearms, calibrations: Object.fromEntries(this.calibrations) }));
  }

  private render() {
    this.bridgeBadge.textContent = !this.bridgeConnected
      ? "Local bridge connecting…"
      : this.sourceConnected ? "Acquisition stream connected" : "Waiting for the acquisition page…";
    this.root.classList.toggle("bridge-online", this.bridgeConnected && this.sourceConnected);
    this.root.querySelectorAll<HTMLElement>("[data-setup]").forEach(button => button.classList.toggle("is-active", button.dataset.setup === this.setup));
    const forearms = this.root.querySelector<HTMLInputElement>('[data-action="forearms"]')!;
    forearms.checked = this.includeForearms;
    this.forearmsRow.hidden = this.setup !== "Overhead";
    const messages = this.evaluator.messages();
    this.prompt.hidden = messages.length === 0;
    this.prompt.replaceChildren(...messages.map(message => {
      const item = document.createElement("div");
      item.textContent = message.text;
      return item;
    }));
    this.cards.replaceChildren(...activeChannels(this.setup, this.includeForearms).map(channel => this.renderCard(channel, messages)));
    this.valuesPanel.hidden = !this.showValues;
    this.root.querySelector<HTMLElement>('[data-role="values-symbol"]')!.textContent = this.showValues ? "−" : "+";
    if (this.showValues) this.renderCalibrationRows();
    this.root.querySelector<HTMLElement>('[data-role="status"]')!.textContent = this.status;
  }

  private renderCard(channel: string, messages: any[]) {
    const rule = CHANNEL_RULES[channel];
    const reading = this.packets.get(channel)?.packet;
    const percent = this.values.get(channel);
    const line = restLine(this.calibrations.get(channel));
    const level = percent === undefined ? "Unavailable" : classify(channel, percent, line);
    const card = document.createElement("article");
    card.className = `prd-card level-${level.toLowerCase()}`;
    const top = document.createElement("div");
    top.className = "prd-card-top";
    const identity = document.createElement("div");
    identity.className = "prd-card-identity";
    identity.innerHTML = `<span class="prd-level-dot"></span><span>${rule.label}</span>`;
    const value = document.createElement("strong");
    value.textContent = percent !== undefined && Number.isFinite(percent)
      ? `${percent.toFixed(2)}% MVIC · ${level}`
      : reading ? `${reading.rmsUv?.toFixed(2) ?? "—"} µV` : "—";
    top.append(identity, value);
    const ruleText = document.createElement("p");
    ruleText.className = "prd-rule";
    ruleText.textContent = rule.corrective
      ? `High: >${rule.highThreshold}% for >${rule.highHoldMs / 1000} s · Moderate: ${rule.moderateThreshold}–${rule.highThreshold}% for >30 s`
      : "Thigh: colors only; no corrective prompt.";
    card.append(top, ruleText);
    if (reading && percent === undefined) {
      const hint = document.createElement("p");
      hint.className = "prd-hint";
      hint.textContent = rule.requiresEcgBlanking && !reading.ecgBlanked
        ? "Waiting for heartbeat-cleaned RMS."
        : "%MVIC needs this sensor's rest/max values.";
      card.appendChild(hint);
    }
    for (const message of messages.filter(message => message.channel === channel)) {
      const text = document.createElement("p");
      text.className = "prd-card-message";
      text.textContent = message.text;
      card.appendChild(text);
    }
    return card;
  }

  private renderCalibrationRows() {
    const container = this.root.querySelector<HTMLElement>('[data-role="calibration-rows"]')!;
    container.replaceChildren(...activeChannels(this.setup, this.includeForearms).map(channel => {
      const calibration = this.calibrations.get(channel);
      const row = document.createElement("div");
      row.className = "prd-calibration-row";
      row.dataset.channel = channel;
      const label = document.createElement("div");
      label.className = "prd-calibration-label";
      label.textContent = channel;
      row.appendChild(label);
      for (const [field, placeholder] of [["rest", "rest"], ["restSD", "restSD"], ["max", "max"]]) {
        const input = document.createElement("input");
        input.type = "number";
        input.step = "any";
        input.min = "0";
        input.placeholder = placeholder;
        input.dataset.field = field;
        const saved = calibration?.[field as "rest" | "restSD" | "max"];
        if (Number.isFinite(saved)) input.value = String(saved);
        row.appendChild(input);
      }
      const apply = document.createElement("button");
      apply.type = "button";
      apply.dataset.action = "apply";
      apply.dataset.channel = channel;
      apply.textContent = "Apply";
      row.appendChild(apply);
      if (calibration) {
        const clear = document.createElement("button");
        clear.type = "button";
        clear.className = "prd-clear";
        clear.dataset.action = "clear";
        clear.dataset.channel = channel;
        clear.textContent = "×";
        clear.setAttribute("aria-label", `Clear ${channel} calibration`);
        row.appendChild(clear);
      }
      return row;
    }));
  }
}
