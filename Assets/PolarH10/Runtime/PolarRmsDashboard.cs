using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Module3.PolarH10
{
    /// <summary>Sensor value, document rule and immediate feedback. No session or calibration wizard.</summary>
    [RequireComponent(typeof(PolarRmsReceiver))]
    public class PolarRmsDashboard : MonoBehaviour
    {
        public bool expanded=true;
        public static readonly Color MissingColor=new Color(.42f,.46f,.51f);
        public static Color SignalColor(float value)=>Color.Lerp(new Color(.12f,.65f,.72f),new Color(1f,.43f,.15f),value);
        private PolarRmsReceiver receiver;
        private PolarThresholdCoach coach;
        private Vector2 scroll;
        private GUIStyle heading,small,wrapped;
        private bool showValues;
        private string path="",inputStatus="";
        private readonly Dictionary<string,string[]> inputs=new Dictionary<string,string[]>();
        private void Awake()
        {
            receiver=GetComponent<PolarRmsReceiver>();
            // An older manually saved component must not compete for the learner prompt.
            var previous=GetComponent<PolarCoachingSession>();if(previous)previous.enabled=false;
            coach=GetComponent<PolarThresholdCoach>();if(!coach)coach=gameObject.AddComponent<PolarThresholdCoach>();
        }
        private void OnGUI()
        {
            if(!coach)return;
            if(heading==null){heading=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold};small=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=true};wrapped=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};}
            float width=Mathf.Min(440,Screen.width-24);
            GUILayout.BeginArea(new Rect(Screen.width-width-12,12,width,expanded?Screen.height-24:44),GUI.skin.box);
            GUILayout.BeginHorizontal();GUILayout.Label("Polar H10 · Muscle Effort",heading);if(GUILayout.Button(expanded?"−":"+",GUILayout.Width(28)))expanded=!expanded;GUILayout.EndHorizontal();
            if(expanded){
                scroll=GUILayout.BeginScrollView(scroll);
                GUILayout.Label("Live readings → document threshold → prompt",small);
                GUILayout.BeginHorizontal();
                bool overhead=GUILayout.Toggle(coach.setup=="Overhead","Overhead",GUI.skin.button);
                bool bending=GUILayout.Toggle(coach.setup=="Bending","Bending",GUI.skin.button);
                GUILayout.EndHorizontal();
                if(overhead && coach.setup!="Overhead")coach.ConfigureSetup("Overhead",coach.includeForearms);
                else if(bending && coach.setup!="Bending")coach.ConfigureSetup("Bending");
                if(coach.setup=="Overhead"){
                    bool forearms=GUILayout.Toggle(coach.includeForearms,"Include optional forearm sensors");
                    if(forearms!=coach.includeForearms)coach.ConfigureSetup("Overhead",forearms);
                }
                foreach(var pair in PolarCoachingRules.Channels){
                    string id=pair.Key;var rule=pair.Value;var region=Array.Find(receiver.regions,r=>r.id==id);
                    if(!coach.ActiveSetupChannel(id))continue;
                    double percent=coach.Percent(id);bool live=region!=null&&receiver.IsLive(region);
                    GUILayout.BeginVertical(GUI.skin.box);GUILayout.BeginHorizontal();
                    var color=GUI.color;GUI.color=coach.ColorFor(id);GUILayout.Label("●",GUILayout.Width(20));GUI.color=color;
                    GUILayout.Label(rule.label,wrapped);GUILayout.Label(!double.IsNaN(percent)?percent.ToString("0.##")+"% MVIC · "+coach.Level(id):live?region.rmsUv.ToString("0.##")+" µV":"—",small);
                    GUILayout.EndHorizontal();
                    GUILayout.Label(rule.corrective?"High: >"+rule.highThreshold+"% for >"+rule.highHoldMs/1000+" s · Moderate: "+rule.moderateThreshold+"–"+rule.highThreshold+"% for >30 s":"Thigh: colors only; no corrective prompt.",small);
                    if(live && double.IsNaN(percent))GUILayout.Label(rule.requiresEcgBlanking&&!region.ecgBlanked?"Waiting for heartbeat-cleaned RMS.":"%MVIC needs this sensor's rest/max values.",small);
                    foreach(var message in coach.Messages)if(message.channel==id)GUILayout.Label(message.text,wrapped);
                    GUILayout.EndVertical();
                }
                GUILayout.Label("Trapezius: no qualifying rest for >90 s. Rest gaps: ≥0.2 s. Conditions met → immediate prompt; below the rule → silence.",small);
                showValues=GUILayout.Toggle(showValues,"Rest/max values (for µV → %MVIC only)");
                if(showValues)Values();
                GUILayout.Label("Sources agree on effort thresholds. File B permits 4 overhead sensors or 6 with forearms; only connected locations are shown on the body.",small);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }
        private void Values()
        {
            GUILayout.Label("Load existing values, or enter each connected sensor's measured rest, restSD and max in µV. No recording steps or session start.",small);
            path=GUILayout.TextField(path);
            if(GUILayout.Button("Load values from JSON")){coach.TryLoad(path);inputs.Clear();}
            foreach(var pair in PolarCoachingRules.Channels){
                string id=pair.Key;var region=Array.Find(receiver.regions,r=>r.id==id);
                if(!coach.ActiveSetupChannel(id))continue;
                if(region==null || !receiver.IsLive(region))continue;
                if(!inputs.TryGetValue(id,out var fields)){
                    var c=coach.Calibration(id);fields=c!=null&&c.status=="OK"?new[]{c.rest.ToString("R",CultureInfo.InvariantCulture),c.restSD.ToString("R",CultureInfo.InvariantCulture),c.max.ToString("R",CultureInfo.InvariantCulture)}:new[]{"","",""};inputs[id]=fields;
                }
                GUILayout.Label(id+" · rest / restSD / max",small);GUILayout.BeginHorizontal();
                for(int i=0;i<3;i++)fields[i]=GUILayout.TextField(fields[i],GUILayout.MinWidth(45));
                if(GUILayout.Button("Apply",GUILayout.Width(55))){
                    try {
                        var c=new PolarChannelCalibration{channel=id,status="OK",rest=double.Parse(fields[0],CultureInfo.InvariantCulture),restSD=double.Parse(fields[1],CultureInfo.InvariantCulture),max=double.Parse(fields[2],CultureInfo.InvariantCulture)};
                        coach.SetCalibration(c);inputStatus=id+" values applied.";
                    } catch(Exception e){inputStatus=e.Message;}
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(inputStatus.Length>0?inputStatus:coach.Status,small);
        }
    }
}
