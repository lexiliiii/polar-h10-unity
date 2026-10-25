using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Module3.PolarH10
{
    [RequireComponent(typeof(PolarRmsReceiver))]
    public class PolarCoachingSession : MonoBehaviour
    {
        public string participantId="", setup="Overhead";
        public bool includeForearms, finalEvaluation, continuous=true;
        [Range(1,3)] public int feedbackStage=1;
        public UnityEvent<string> spokenReminder=new UnityEvent<string>();
        public string Status {get;private set;}="Enter a participant ID and calibrate or load today's baseline.";
        public string Summary {get;private set;}="";
        public bool Running => engine!=null && engine.Running;
        public PolarCoachingResult Latest {get;private set;}
        public Dictionary<string,PolarChannelCalibration> Calibrations {get;private set;}=new Dictionary<string,PolarChannelCalibration>();
        public readonly Dictionary<string,PolarCalibrationCapture> Captures=new Dictionary<string,PolarCalibrationCapture>();
        private PolarRmsReceiver receiver;
        private PolarCoachingEngine engine;
        private PolarLearnerPrompt prompt;
        private readonly List<PolarCoachingObservation> pending=new List<PolarCoachingObservation>();
        private readonly Dictionary<string,long> segments=new Dictionary<string,long>();
        private readonly List<string> log=new List<string>();
        private string scope;
        private AudioSource speaker;
        private AudioClip tone;
        private bool dirty;
        private readonly PolarEffortDisplay display=new PolarEffortDisplay();
        private readonly Dictionary<string,PolarEffortLevel> displayLevels=new Dictionary<string,PolarEffortLevel>();
        public bool MovementPaused {get;set;}
        public PolarBoutSummary LastSummary {get;private set;}
        public string[] Active => PolarCoachingRules.ActiveChannels(setup,includeForearms).ToArray();
        public bool ActiveChannel(string id) => Array.IndexOf(Active,id)>=0;
        public string SavePath => Path.Combine(Application.persistentDataPath,"PolarCalibration",Safe(participantId)+"_"+setup+"_"+DateTime.Now.ToString("yyyy-MM-dd")+".json");
        private static string Safe(string s) => string.Concat(s.Select(c=>char.IsLetterOrDigit(c) || c=='-' || c=='_' ? c : '_'));
        private double Now => Time.realtimeSinceStartupAsDouble;
        private void Awake() {
            receiver=GetComponent<PolarRmsReceiver>(); receiver.EnsureRegions();
            prompt=GetComponent<PolarLearnerPrompt>(); if(!prompt) prompt=gameObject.AddComponent<PolarLearnerPrompt>();
            speaker=gameObject.AddComponent<AudioSource>(); speaker.playOnAwake=false;
            tone=AudioClip.Create("Polar soft prompt tone",6615,1,44100,false);
            var samples=new float[6615]; for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*440/44100)*.04f*(1-i/(float)samples.Length); tone.SetData(samples,0);
        }
        private void OnEnable() { if(!receiver) receiver=GetComponent<PolarRmsReceiver>(); receiver.SampleReceived+=OnSample; }
        private void Start() {
            participantId=PlayerPrefs.GetString("Polar.Participant",participantId);
            setup=PlayerPrefs.GetString("Polar.Setup",setup); includeForearms=PlayerPrefs.GetInt("Polar.Forearms",includeForearms?1:0)==1;
            ChangeScope();
        }
        public void ChangeScope() {
            string next=participantId+"|"+setup+"|"+includeForearms;
            if(scope==next) return; EndBout(); scope=next; engine=null; Latest=null; pending.Clear(); segments.Clear(); display.Clear(); displayLevels.Clear(); Captures.Clear(); Calibrations.Clear(); dirty=false;
            PlayerPrefs.SetString("Polar.Participant",participantId); PlayerPrefs.SetString("Polar.Setup",setup); PlayerPrefs.SetInt("Polar.Forearms",includeForearms?1:0);
            if(!string.IsNullOrWhiteSpace(participantId) && File.Exists(SavePath)) Try(()=>ImportFile(SavePath));
            else Status="Calibrate active channels, or load today's calibration. Unused channels must be NotCalibrated.";
        }
        public bool Ready(PolarRegion r) => r!=null && receiver.IsLive(r) && r.hasDecisionRms && (!IsBack(r.id) || r.ecgBlanked);
        public static bool IsBack(string id) => id=="LLowerSpine" || id=="RLowerSpine" || id=="LUpperBack" || id=="RUpperBack";
        public static string Group(string id) => id.Contains("FrontShoulder") ? "Front shoulder" : id.Contains("Shoulder") ? "Shoulder" : id.Contains("Thigh") ? "Thigh" : id.Contains("LowerArm") ? "Forearm" : "Back";
        private PolarRegion Region(string id) => Array.Find(receiver.regions,r=>r.id==id);
        public PolarCalibrationCapture Capture(string id) {
            if(!Captures.TryGetValue(id,out var c)) { c=new PolarCalibrationCapture(id); if(Calibrations.TryGetValue(id,out var saved)) {if(saved.status=="OK") c.RestoreBaseline(saved); else if(saved.status=="NotCalibrated") c.Stop();} Captures[id]=c; } return c;
        }
        public void Check(string id) {
            var r=Region(id); if(!Ready(r) || r.rmsUv<=0) throw new InvalidOperationException(IsBack(id) ? "Back channels require verified 100 ms heartbeat-cleaned data." : "Wait for a current complete signal window.");
            EndBout(); dirty=true; var capture=Capture(id); Calibrations.Remove(id); capture.BeginCheck(r.rmsUv); Status="Tense "+id+", then relax back to the quiet level.";
        }
        public void Skip(string id) {EndBout();dirty=true;Capture(id).Stop();Calibrations[id]=Capture(id).Result();Status=id+" marked NotCalibrated.";}
        public void Rest() {
            RequireParticipant(); var targets=Active.Where(id=>!Calibrations.TryGetValue(id,out var c) || c.status!="NotCalibrated").ToArray();
            if(targets.Length==0) throw new InvalidOperationException("Connect and check at least one channel.");
            foreach(var id in targets) if(!Ready(Region(id)) || !Capture(id).Checked) throw new InvalidOperationException("Check "+id+", or mark it NotCalibrated.");
            EndBout(); dirty=true; foreach(var id in targets) Capture(id).BeginRest(); Status="Stand still and relaxed, arms hanging by your sides. Don't talk for 30 seconds.";
        }
        private IEnumerable<PolarCalibrationCapture> GroupCaptures(string group) => Active.Where(id=>Group(id)==group).Select(Capture).Where(c=>c.Phase!="NotCalibrated");
        public void Practice(string group) { var cs=GroupCaptures(group).ToArray();if(cs.Length==0 || cs.Any(c=>c.Phase!="Practice")) throw new InvalidOperationException("Complete rest calibration for this group first.");foreach(var c in cs)c.Practice();Status="Half-effort practice complete."; }
        public void Maximum(string group) { var cs=GroupCaptures(group).ToArray();foreach(var c in cs)c.Tick(Now);if(cs.Length==0 || cs.Any(c=>c.Phase!="Ready"))throw new InvalidOperationException("Complete practice or wait for the 60 s rest countdown.");foreach(var c in cs)c.BeginMaximum(Now);Status="Push, push, push, push, relax."; }
        public void Redo(string group) {var cs=GroupCaptures(group).ToArray();if(cs.Length==0 || cs.Any(c=>!c.CanRedoMaximum))throw new InvalidOperationException("Repeat the signal checks first.");EndBout();dirty=true;foreach(var c in cs)c.RedoMaximum();Status="Repeat this group's practice and maximum trials.";}
        public void StopGroup(string group) {foreach(var id in Active.Where(id=>Group(id)==group))Skip(id);Status="Group stopped due to pain. Continue with other groups.";}
        private void RequireParticipant() {if(string.IsNullOrWhiteSpace(participantId))throw new InvalidOperationException("Enter a participant ID.");}
        public void Save() {
            RequireParticipant();var values=new Dictionary<string,PolarChannelCalibration>();
            foreach(var id in Active) {var c=Captures.TryGetValue(id,out var capture)?capture.Result():Calibrations.TryGetValue(id,out var saved)?saved:null;
                if(c==null || (c.status!="NotCalibrated" && !PolarCoachingRules.ValidateCalibration(c,out _)))throw new InvalidOperationException("Finish calibration or mark NotCalibrated: "+id); values[id]=c;}
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));File.WriteAllText(SavePath,PolarCalibrationFile.Serialize(participantId,setup,values));Calibrations=values;dirty=false;Status="Saved calibration: "+SavePath;
        }
        public void ImportFile(string path) {RequireParticipant();var values=PolarCalibrationFile.Parse(File.ReadAllText(path),participantId,setup,Active,DateTime.Now);EndBout();Calibrations=values;Captures.Clear();display.Clear();displayLevels.Clear();segments.Clear();dirty=false;engine=null;Directory.CreateDirectory(Path.GetDirectoryName(SavePath));if(Path.GetFullPath(path)!=Path.GetFullPath(SavePath))File.WriteAllText(SavePath,PolarCalibrationFile.Serialize(participantId,setup,values));Status="Loaded today's calibration.";}
        public void StartBout() {
            RequireParticipant();if(Running)throw new InvalidOperationException("End the current bout before starting another.");if(dirty || Active.Any(id=>!Calibrations.TryGetValue(id,out var c) || c==null || (c.status!="NotCalibrated" && !PolarCoachingRules.ValidateCalibration(c,out _))))throw new InvalidOperationException("Save or load valid calibration before starting.");
            if(!Active.Any(id=>Calibrations[id].status=="OK" && Ready(Region(id))))throw new InvalidOperationException("Wait for a calibrated channel with a current complete signal.");
            var settings=new PolarCoachingSettings {setup=setup,includeForearms=includeForearms,calibrations=Calibrations,stage=feedbackStage,finalBout=finalEvaluation,continuous=continuous};
            if(engine==null)engine=new PolarCoachingEngine(settings);else engine.Configure(settings);
            pending.Clear();Latest=null;LastSummary=null;prompt.Hide();engine.StartBout(Now);Summary="";Status="Bout started.";Log("calibration",Calibrations);Log("bout-start",new {stage=feedbackStage,finalEvaluation,continuous});
        }
        public void EndBout() {if(Running){var summary=engine.EndBout(Now);feedbackStage=engine.Stage;SetSummary(summary);Log("bout-end",summary);}pending.Clear();Latest=null;if(prompt)prompt.Hide();}
        private void OnSample(PolarRegion r) {
            if(!ActiveChannel(r.id))return;
            bool valid=Ready(r);
            if(!valid || r.signalGap || (segments.TryGetValue(r.id,out var previous) && previous!=r.signalSegment)) {
                engine?.ResetChannel(r.id,"Signal gap or unavailable");
                pending.RemoveAll(p=>p.channel==r.id);display.Reset(r.id);displayLevels.Remove(r.id);
                if(Latest!=null){Latest.levels?.Remove(r.id);if(Latest.message!=null && Latest.message.channel==r.id){Latest.message=null;if(prompt)prompt.Hide();}}
                if(Captures.TryGetValue(r.id,out var c) && (!receiver.IsLive(r) || r.signalGap || (IsBack(r.id) && !r.ecgBlanked)))c.Interrupt();
            }
            segments[r.id]=r.signalSegment;
            if(receiver.IsLive(r) && (!IsBack(r.id) || r.ecgBlanked) && Captures.TryGetValue(r.id,out var cal))cal.Sample(r,Now);
            if(Running && valid)pending.Add(new PolarCoachingObservation {channel=r.id,rmsUv=r.rms2sUv,signalTimestampMs=r.timestampMs,windowMs=2000,valid=true,ecgBlanked=r.ecgBlanked});
            if(valid && Calibrations.TryGetValue(r.id,out var baseline) && PolarCoachingRules.ValidateCalibration(baseline,out _)) displayLevels[r.id]=display.Update(r.id,PolarCoachingRules.Normalize(r.rmsUv,baseline),PolarCoachingRules.RestLine(baseline),r.timestampMs/1000);
            if(Running)Log("sample",new {channel=r.id,timestampMs=r.timestampMs,rmsUv=r.rmsUv,rms2sUv=r.hasDecisionRms?(double?)r.rms2sUv:null,percentMVIC=double.IsNaN(Percent(r.id))?(double?)null:Percent(r.id),level=Level(r.id).ToString(),ecgBlanked=r.ecgBlanked,stage=feedbackStage});
        }
        private void LateUpdate() {
            foreach(var c in Captures.Values)c.Tick(Now);
            if(!Running)return;
            Latest=engine.Tick(pending,Now,MovementPaused);pending.Clear();feedbackStage=Latest.stage;
            if(Latest.message!=null)prompt.Show(Latest.message.text);else prompt.Hide();
            foreach(var e in Latest.events) {Log(e.type,e);if(e.type=="new-message" && !finalEvaluation)speaker.PlayOneShot(tone);}
            if(!string.IsNullOrEmpty(Latest.spokenReminder))spokenReminder.Invoke(Latest.spokenReminder);
            if(Latest.summary!=null)SetSummary(Latest.summary);
        }
        public double Percent(string id) {var r=Region(id);return Ready(r) && Calibrations.TryGetValue(id,out var c) && c.status=="OK" ? PolarCoachingRules.Normalize(r.rmsUv,c) : double.NaN;}
        public PolarEffortLevel Level(string id) {return !double.IsNaN(Percent(id)) && displayLevels.TryGetValue(id,out var level)?level:PolarEffortLevel.Unavailable;}
        public Color ColorFor(string id) {switch(Level(id)){case PolarEffortLevel.Low:return new Color(.25f,.8f,.4f);case PolarEffortLevel.Moderate:return new Color(1f,.72f,.15f);case PolarEffortLevel.High:return new Color(.95f,.2f,.2f);default:return PolarRmsDashboard.MissingColor;}}
        private void Log(string type,object data){log.Add(new Newtonsoft.Json.Linq.JObject{["time"]=DateTimeOffset.Now.ToString("o"),["type"]=type,["participantId"]=participantId,["setup"]=setup,["data"]=Newtonsoft.Json.Linq.JToken.FromObject(data)}.ToString(Newtonsoft.Json.Formatting.None));}
        private void SetSummary(PolarBoutSummary summary){LastSummary=summary;Summary="Bout "+summary.number+" finished · "+summary.alerts+" alerts · "+summary.highAlerts+" High · next stage "+summary.nextStage;}
        public PolarChannelCalibration CurrentCalibration(string id) => Captures.TryGetValue(id,out var capture) ? capture.Result() : Calibrations.TryGetValue(id,out var saved) ? saved : null;
        public bool CanSave => !string.IsNullOrWhiteSpace(participantId) && Active.Length>0 && Active.All(id=>{var c=CurrentCalibration(id);return c!=null && (c.status=="NotCalibrated" || PolarCoachingRules.ValidateCalibration(c,out _));});
        public bool CanRecordRest => !string.IsNullOrWhiteSpace(participantId) && Active.Any(id=>Capture(id).Phase!="NotCalibrated") && Active.Where(id=>Capture(id).Phase!="NotCalibrated").All(id=>Ready(Region(id)) && Capture(id).Checked);
        public void ExportLog(){string p=Path.Combine(Application.persistentDataPath,"Polar_"+Safe(participantId)+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".jsonl");File.WriteAllLines(p,log);Status="Log saved: "+p;}
        public void Try(Action action){try{action();}catch(Exception e){Status=e.Message;}}
        private void OnDisable(){if(receiver)receiver.SampleReceived-=OnSample;EndBout();}
        private void OnDestroy(){if(tone){if(Application.isPlaying)Destroy(tone);else DestroyImmediate(tone);}}
    }
}
