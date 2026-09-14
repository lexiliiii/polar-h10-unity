package com.roblab.tts;

import android.content.Context;
import android.speech.tts.TextToSpeech;
import android.speech.tts.TextToSpeech.EngineInfo;
import android.os.Bundle;

import java.util.List;
import java.util.Locale;
import java.util.concurrent.atomic.AtomicBoolean;

public class UnityAndroidTTS {
    private TextToSpeech tts;
    private final AtomicBoolean ready = new AtomicBoolean(false);
    private String lastError = "";

    public void init(final Context context, final String localeTag) {
        if (tts != null) return;

        tts = new TextToSpeech(context.getApplicationContext(), status -> {
            if (status == TextToSpeech.SUCCESS) {
                Locale locale = Locale.forLanguageTag(localeTag != null ? localeTag : "en-US");
                int res = tts.setLanguage(locale);
                if (res == TextToSpeech.LANG_MISSING_DATA || res == TextToSpeech.LANG_NOT_SUPPORTED) {
                    lastError = "Language missing/not supported: " + locale.toLanguageTag();
                    ready.set(false);
                } else {
                    ready.set(true);
                }
            } else {
                lastError = "TTS init failed: " + status;
                ready.set(false);
            }
        });
    }

    public boolean isReady() { return ready.get(); }
    public String getLastError() { return lastError != null ? lastError : ""; }

    public String[] getEngines() {
        if (tts == null) return new String[0];
        List<EngineInfo> engines = tts.getEngines();
        if (engines == null) return new String[0];
        String[] names = new String[engines.size()];
        for (int i = 0; i < engines.size(); i++) names[i] = engines.get(i).name;
        return names;
    }

    public void speak(String text, String utteranceId, float pitch, float rate) {
        if (tts == null || !ready.get() || text == null) return;

        tts.setPitch(pitch <= 0f ? 1.0f : pitch);
        tts.setSpeechRate(rate <= 0f ? 1.0f : rate);

        if (android.os.Build.VERSION.SDK_INT >= 21) {
            Bundle params = new Bundle();
            tts.speak(text, TextToSpeech.QUEUE_FLUSH, params, utteranceId);
        } else {
            java.util.HashMap<String, String> params = new java.util.HashMap<>();
            params.put(TextToSpeech.Engine.KEY_PARAM_UTTERANCE_ID, utteranceId);
            tts.speak(text, TextToSpeech.QUEUE_FLUSH, params);
        }
    }

    public void stop() { if (tts != null) tts.stop(); }

    public void shutdown() {
        if (tts != null) {
            tts.stop();
            tts.shutdown();
            tts = null;
        }
        ready.set(false);
    }
}