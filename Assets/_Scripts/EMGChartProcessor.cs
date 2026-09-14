using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class EMGChartProcessor : MonoBehaviour
{
    private LineChart lineChart;
    [System.Serializable]
    public class EMGSample
    {
        public float time;
        public float exo_voltage;
        public float no_exo_voltage;
    }
    public List<EMGSample> data = new List<EMGSample>();
    private Serie emgSerie;
    private Serie emgSerie2;
    private XAxis x_axis;
    public int currentIndex = 5;
    public string fileName = "emg_sample";
    // Start is called before the first frame update
    void Start()
    {
        lineChart = GetComponent<LineChart>();
        TextAsset csvFile = Resources.Load<TextAsset>(fileName); // no extension
        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = lines[i].Split(',');
            float time = float.Parse(values[0]);
            float exo_voltage = float.Parse(values[1]);
            float no_exo_voltage = float.Parse(values[2]);
            data.Add(new EMGSample { time = time, exo_voltage = exo_voltage, no_exo_voltage = no_exo_voltage });
        }

        Debug.Log($"Loaded {data.Count} EMG samples.");

        lineChart.AddChartComponent<Legend>();

        emgSerie = lineChart.AddSerie<Line>();
        emgSerie.lineStyle.width = 2;
        emgSerie.symbol.show = false;
        emgSerie.serieName = "With Exo Muscle Activation";

        emgSerie2 = lineChart.AddSerie<Line>();
        emgSerie2.lineStyle.width = 2;
        emgSerie2.symbol.show = false;
        emgSerie2.serieName = "Without Exo Muscle Activation";


        x_axis = lineChart.GetChartComponent<XAxis>();

        for (int i = 0; i < currentIndex; i++)
        {
            var point = data[i];
            x_axis.AddData(point.time.ToString());
            emgSerie.AddData(i, point.exo_voltage);
            emgSerie2.AddData(i, -point.no_exo_voltage);
        }
        StartCoroutine(TickValues());
    }

    private IEnumerator TickValues()
    {
        while (currentIndex < data.Count)
        {
            yield return new WaitForSeconds(0.1f);
            var point = data[currentIndex];

            emgSerie.AddData(currentIndex, point.exo_voltage);
            emgSerie2.AddData(currentIndex, -point.no_exo_voltage);
            x_axis.AddData(point.time.ToString());

            emgSerie.RemoveData(0);
            emgSerie2.RemoveData(0);
            x_axis.RemoveData(0);

            currentIndex++;
        }
    }
}
