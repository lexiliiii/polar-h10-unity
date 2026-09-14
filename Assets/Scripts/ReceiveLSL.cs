using LSL;
using System.Collections;
using UnityEngine;

public class ReceiveLSL : MonoBehaviour
{
    public string StreamName;

    private ContinuousResolver resolver;

    private StreamInlet inlet;
    private float[] sample_buffer;
    private int n_channels;

    public float[] emg_8_channels = new float[8];

    void Start()
    {
        if (!string.IsNullOrEmpty(StreamName))
        {
            Debug.Log($"LSL Stream- starting input for stream \"{StreamName}\"");
            resolver = new ContinuousResolver("name", StreamName);
        }
        else
        {
            this.enabled = false;
            return;
        }

        StartCoroutine(ResolveExpectedStream());
    }

    IEnumerator ResolveExpectedStream()
    {
        var results = resolver.results();

        while (results.Length == 0)
        {
            yield return new WaitForSeconds(0.1f);
            results = resolver.results();
        }

        Debug.Log($"LSL Stream- \"{StreamName}\" found.");
        inlet = new StreamInlet(results[0]);

        n_channels = inlet.info().channel_count();
        sample_buffer = new float[n_channels];

        Debug.Log($"LSL Stream- stream info: {n_channels} channels.");
    }

    void Update()
    {
        if (inlet != null)
        {
            double timestamp = inlet.pull_sample(sample_buffer, 0.0);

            if (timestamp != 0.0)
            {
                if (n_channels >= 8)
                {
                    emg_8_channels = sample_buffer;

                }
                else
                {
                    Debug.LogWarning($"LSL Stream- not enough channels- found {n_channels}, need >= 3");
                }
            }
        }
        else
        {
            Debug.LogWarning("LSL Stream- no inlet connected yet");
        }
    }
}
