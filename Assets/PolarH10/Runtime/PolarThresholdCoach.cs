using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Module3.PolarH10
{
    /// <summary>Only the document's amplitude/duration conditions. No delivery delay or session gates.</summary>
    public sealed class PolarThresholdEvaluator
    {
        private sealed class State
        {
            public double at=double.NaN;
            public double? highSince, moderateSince, noRestSince, restSince;
            public PolarCoachingMessage message;
        }
        private readonly Dictionary<string,State> states=new Dictionary<string,State>();
        public void Clear()=>states.Clear();
        public void Reset(string channel){if(channel!=null)states.Remove(channel);}
        public IEnumerable<PolarCoachingMessage> Messages=>states.Values.Where(s=>s.message!=null).Select(s=>s.message).OrderBy(m=>m.kind).ThenBy(m=>m.channel,StringComparer.Ordinal);

        public PolarCoachingMessage Observe(string channel,double percent,double atMs,double restLine=double.NaN)
        {
            if(string.IsNullOrEmpty(channel) || !PolarCoachingRules.Channels.TryGetValue(channel,out var rule) || !PolarCoachingRules.Finite(percent) || percent<0 || !PolarCoachingRules.Finite(atMs)) {Reset(channel);return null;}
            if(!states.TryGetValue(channel,out var s))states[channel]=s=new State();
            if(!double.IsNaN(s.at) && atMs<=s.at)return s.message;
            s.at=atMs;
            bool resting=PolarCoachingRules.Finite(restLine) && percent<restLine;
            bool high=!resting && percent>rule.highThreshold;
            bool moderate=!resting && percent>=rule.moderateThreshold && percent<=rule.highThreshold;
            s.highSince=high?s.highSince??atMs:null;
            s.moderateSince=moderate?s.moderateSince??atMs:null;
            if(resting) {
                s.restSince=s.restSince??atMs;
                if(atMs-s.restSince.Value>=PolarCoachingRules.RestGapMs)s.noRestSince=null;
            } else {
                if(s.restSince.HasValue && atMs-s.restSince.Value>=PolarCoachingRules.RestGapMs)s.noRestSince=null;
                s.restSince=null;s.noRestSince=s.noRestSince??atMs;
            }
            s.message=null;
            if(!rule.corrective || resting)return null;
            PolarCoachingKind kind; double duration;
            if(s.highSince.HasValue && atMs-s.highSince.Value>rule.highHoldMs){kind=PolarCoachingKind.High;duration=atMs-s.highSince.Value;}
            else if(s.moderateSince.HasValue && atMs-s.moderateSince.Value>PolarCoachingRules.ModerateHoldMs){kind=PolarCoachingKind.Moderate;duration=atMs-s.moderateSince.Value;}
            else if(rule.noRest && PolarCoachingRules.Finite(restLine) && s.noRestSince.HasValue && atMs-s.noRestSince.Value>PolarCoachingRules.NoRestHoldMs){kind=PolarCoachingKind.NoRest;duration=atMs-s.noRestSince.Value;}
            else return null;
            s.message=new PolarCoachingMessage{channel=channel,kind=kind,level=high?PolarEffortLevel.High:moderate?PolarEffortLevel.Moderate:PolarEffortLevel.Low,percentMVIC=percent,restLine=restLine,durationMs=duration,signalTimestampMs=atMs};
            s.message.text=PolarCoachingRules.FormatMessage(s.message);
            return s.message;
        }
    }

    [DisallowMultipleComponent,RequireComponent(typeof(PolarRmsReceiver))]
    public sealed class PolarThresholdCoach : MonoBehaviour
    {
        [Tooltip("Only channels in this document setup can drive colours or learner prompts.")]
        public string setup="Overhead";
        [Tooltip("Overhead only: include the optional forearm sensors used with hand tools.")]
        public bool includeForearms;
        [Tooltip("Existing rest/max values in µV. Required only when converting raw RMS to %MVIC.")]
        public List<PolarChannelCalibration> calibrations=new List<PolarChannelCalibration>();
        [Tooltip("Optional File B calibration JSON; no wizard or session start is required.")]
        public string calibrationFilePath="";
        public string Status {get;private set;}="Listening for sensor readings.";
        public string CurrentPrompt {get;private set;}="";
        public IEnumerable<PolarCoachingMessage> Messages=>evaluator.Messages;
        private readonly PolarThresholdEvaluator evaluator=new PolarThresholdEvaluator();
        private readonly Dictionary<string,double> values=new Dictionary<string,double>();
        private readonly Dictionary<string,double> lines=new Dictionary<string,double>();
        private readonly Dictionary<string,double> seenAt=new Dictionary<string,double>();
        private PolarRmsReceiver receiver;
        private PolarLearnerPrompt view;

        private void Awake()
        {
            receiver=GetComponent<PolarRmsReceiver>();receiver.EnsureRegions();
            view=GetComponent<PolarLearnerPrompt>();if(!view)view=gameObject.AddComponent<PolarLearnerPrompt>();
            // Old saved values can still be reused; the previous training controller is not started.
            string savedSetup=PlayerPrefs.GetString("Polar.Setup",setup);
            if(savedSetup=="Bending" || savedSetup=="Overhead")setup=savedSetup;
            includeForearms=setup=="Overhead" && PlayerPrefs.GetInt("Polar.Forearms",includeForearms?1:0)==1;
            if(string.IsNullOrEmpty(calibrationFilePath))calibrationFilePath=PlayerPrefs.GetString("Polar.ThresholdCalibrationPath","");
            if(!string.IsNullOrEmpty(calibrationFilePath))TryLoad(calibrationFilePath);
            else {
                string id=PlayerPrefs.GetString("Polar.Participant","");
                string setup=PlayerPrefs.GetString("Polar.Setup","Overhead");
                string safe=string.Concat(id.Select(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'?c:'_'));
                string path=Path.Combine(Application.persistentDataPath,"PolarCalibration",safe+"_"+setup+"_"+DateTime.Now.ToString("yyyy-MM-dd")+".json");
                if(id.Length>0 && File.Exists(path))TryLoad(path);
            }
        }
        private void OnEnable(){if(!receiver)receiver=GetComponent<PolarRmsReceiver>();receiver.SampleReceived+=OnSample;}
        private void OnDisable(){if(receiver)receiver.SampleReceived-=OnSample;evaluator.Clear();values.Clear();lines.Clear();seenAt.Clear();RefreshPrompt();}
        private void Update()
        {
            foreach(var id in seenAt.Keys.ToArray())if(Time.realtimeSinceStartupAsDouble-seenAt[id]>=receiver.staleAfterSeconds)ClearChannel(id);
        }
        public PolarChannelCalibration Calibration(string id)=>calibrations.Find(c=>c!=null && c.channel==id);
        public void SetCalibration(PolarChannelCalibration calibration)
        {
            if(calibration==null || !PolarCoachingRules.Channels.ContainsKey(calibration.channel))throw new ArgumentException("Unknown sensor location.");
            if(calibration.status!="NotCalibrated" && !PolarCoachingRules.ValidateCalibration(calibration,out var reason))throw new ArgumentException(reason);
            calibrations.RemoveAll(c=>c!=null && c.channel==calibration.channel);calibrations.Add(calibration);ClearChannel(calibration.channel);
        }
        public void TryLoad(string path)
        {
            try {
                var root=JObject.Parse(File.ReadAllText(path));var source=root["channels"] as JObject;
                if(source==null)throw new FormatException("The JSON needs a channels object with rest, restSD, max and status.");
                string fileSetup=(string)root["setup"];
                if(fileSetup=="Bending" || fileSetup=="Overhead")
                    ConfigureSetup(fileSetup,fileSetup=="Overhead" && (source["LLowerArm"]!=null || source["RLowerArm"]!=null));
                var loaded=new List<PolarChannelCalibration>();
                foreach(var item in source.Properties()) {
                    if(!PolarCoachingRules.Channels.ContainsKey(item.Name))continue;
                    var c=item.Value.ToObject<PolarChannelCalibration>();c.channel=item.Name;
                    if(c.status!="NotCalibrated" && !PolarCoachingRules.ValidateCalibration(c,out var reason))throw new FormatException(item.Name+": "+reason);
                    loaded.Add(c);
                }
                if(loaded.Count==0)throw new FormatException("No document sensor locations found.");
                foreach(var c in loaded)SetCalibration(c);
                calibrationFilePath=path;PlayerPrefs.SetString("Polar.ThresholdCalibrationPath",path);
                Status="Rest/max values loaded. Prompts follow incoming readings automatically.";
            } catch(Exception e){Status=e.Message;}
        }
        private void OnSample(PolarRegion r)
        {
            if(!PolarCoachingRules.Channels.TryGetValue(r.id,out var rule))return;
            if(!ActiveSetupChannel(r.id)){ClearChannel(r.id);return;}
            if(!receiver.IsLive(r)){ClearChannel(r.id);return;}
            if(r.signalGap)ClearChannel(r.id);
            var c=Calibration(r.id);
            if(!PolarCoachingRules.ValidateCalibration(c,out _) || (rule.requiresEcgBlanking && !r.ecgBlanked)){ClearChannel(r.id);return;}
            values[r.id]=PolarCoachingRules.Normalize(r.rmsUv,c);lines[r.id]=PolarCoachingRules.RestLine(c);seenAt[r.id]=Time.realtimeSinceStartupAsDouble;
            if(r.hasDecisionRms)evaluator.Observe(r.id,PolarCoachingRules.Normalize(r.rms2sUv,c),r.timestampMs,lines[r.id]);
            else evaluator.Reset(r.id);
            RefreshPrompt();
        }
        /// <summary>For a Unity data source that already supplies document-normalized 2 s RMS in %MVIC.
        /// Send each fresh reading; call ClearChannel on gaps/disconnects. Rest line is needed only for no-rest feedback.</summary>
        public void SubmitPercentMVIC(string channel,double percent,double signalTimestampMs,double restLine=double.NaN)
        {
            if(string.IsNullOrEmpty(channel) || !ActiveSetupChannel(channel) || !PolarCoachingRules.Finite(percent) || percent<0 || !PolarCoachingRules.Finite(signalTimestampMs)){ClearChannel(channel);return;}
            values[channel]=percent;lines[channel]=restLine;seenAt[channel]=Time.realtimeSinceStartupAsDouble;
            evaluator.Observe(channel,percent,signalTimestampMs,restLine);RefreshPrompt();
        }
        public void ConfigureSetup(string nextSetup,bool forearms=false)
        {
            if(nextSetup!="Bending" && nextSetup!="Overhead")throw new ArgumentException("Select Bending or Overhead.");
            setup=nextSetup;includeForearms=nextSetup=="Overhead" && forearms;
            evaluator.Clear();values.Clear();lines.Clear();seenAt.Clear();RefreshPrompt();
        }
        public bool ActiveSetupChannel(string id)=>PolarCoachingRules.ActiveChannels(setup,includeForearms).Contains(id);
        public void ClearChannel(string id){if(id==null)return;evaluator.Reset(id);values.Remove(id);lines.Remove(id);seenAt.Remove(id);RefreshPrompt();}
        private void RefreshPrompt(){CurrentPrompt=evaluator.Messages.FirstOrDefault()?.text??"";if(view){if(CurrentPrompt.Length>0)view.Show(CurrentPrompt);else view.Hide();}}
        public bool ActiveChannel(string id)=>ActiveSetupChannel(id) && (values.ContainsKey(id) || receiver.regions.Any(r=>r.id==id && receiver.IsLive(r)));
        public double Percent(string id)=>values.TryGetValue(id,out var value)?value:double.NaN;
        public PolarEffortLevel Level(string id){double p=Percent(id);if(double.IsNaN(p))return PolarEffortLevel.Unavailable;var rule=PolarCoachingRules.Channels[id];if(lines.TryGetValue(id,out var line)&&p<line)return PolarEffortLevel.Rest;return p>rule.highThreshold?PolarEffortLevel.High:p>=rule.moderateThreshold?PolarEffortLevel.Moderate:PolarEffortLevel.Low;}
        public Color ColorFor(string id){switch(Level(id)){case PolarEffortLevel.High:return new Color(.95f,.2f,.2f);case PolarEffortLevel.Moderate:return new Color(1f,.72f,.15f);case PolarEffortLevel.Low:return new Color(.25f,.8f,.4f);default:return PolarRmsDashboard.MissingColor;}}
    }
}
