using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class EEGChartProcessor : MonoBehaviour
{
    private LineChart lineChart;
    [System.Serializable]
    public class EEGSample
    {
        public float time;
        public float voltage;
    }
    public List<EEGSample> data = new List<EEGSample>();
    private Serie eegSerie;
    private Serie eegSerie2;
    private XAxis x_axis;
    public int currentIndex = 5;
    // Start is called before the first frame update
    void Start()
    {
        lineChart = GetComponent<LineChart>();
        TextAsset csvFile = Resources.Load<TextAsset>("test_eeg"); // no extension
        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = lines[i].Split(',');
            float time = float.Parse(values[0]);
            float voltage = float.Parse(values[1]);
            data.Add(new EEGSample { time = time, voltage = voltage });
        }

        Debug.Log($"Loaded {data.Count} EEG samples.");

        lineChart.AddChartComponent<Legend>();

        eegSerie = lineChart.AddSerie<Line>();
        eegSerie.lineStyle.width = 2;
        eegSerie.symbol.show = false;
        eegSerie.serieName = "With Exo";

        eegSerie2 = lineChart.AddSerie<Line>();
        eegSerie2.lineStyle.width = 2;
        eegSerie2.symbol.show = false;
        eegSerie2.serieName = "Without Exo";


        x_axis = lineChart.GetChartComponent<XAxis>();

        for (int i = 0; i < currentIndex; i++)
        {
            var point = data[i];
            x_axis.AddData(point.time.ToString());
            eegSerie.AddData(i, point.voltage);
            eegSerie2.AddData(i, -point.voltage);
        }
        StartCoroutine(TickValues());
    }

    private IEnumerator TickValues()
    {
        while (currentIndex < data.Count)
        {
            yield return new WaitForSeconds(0.1f);
            var point = data[currentIndex];

            eegSerie.AddData(currentIndex, point.voltage);
            eegSerie2.AddData(currentIndex, -point.voltage);
            x_axis.AddData(point.time.ToString());

            eegSerie.RemoveData(0);
            eegSerie2.RemoveData(0);
            x_axis.RemoveData(0);

            currentIndex++;
        }
    }
}
