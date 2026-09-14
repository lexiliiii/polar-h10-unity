using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class IMUChartProcessor : MonoBehaviour
{
    private LineChart lineChart;
    [System.Serializable]
    public class IMU
    {
        public float timestamp;
        public float data_x;
        public float data_y;
        public float data_z;
        public float data_x_exo;
        public float data_y_exo;
        public float data_z_exo;
    }
    public List<IMU> data = new List<IMU>();
    private Serie x;
    private Serie y;
    private Serie z;
    private Serie x_exo;
    private Serie y_exo;
    private Serie z_exo;
    private XAxis x_axis;
    public int currentIndex = 5;
    // Start is called before the first frame update
    void Start()
    {
        lineChart = GetComponent<LineChart>();
        TextAsset csvFile = Resources.Load<TextAsset>("accelerometer"); // no extension
        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = lines[i].Split(',');
            float time = float.Parse(values[0]);
            float data_x = float.Parse(values[1]);
            float data_y = float.Parse(values[2]);
            float data_z = float.Parse(values[3]);
            float data_x_exo = float.Parse(values[4]);
            float data_y_exo = float.Parse(values[5]);
            float data_z_exo = float.Parse(values[6]);
            data.Add(new IMU {
                timestamp = time,
                data_x = data_x,
                data_y = data_y,
                data_z = data_z,
                data_x_exo = data_x_exo,
                data_y_exo = data_y_exo,
                data_z_exo = data_z_exo
            });
        }

        Debug.Log($"Loaded {data.Count} IMU samples.");

        lineChart.AddChartComponent<Legend>();

        x = lineChart.AddSerie<Line>();
        x.lineStyle.width = 2;
        x.symbol.show = false;
        x.serieName = "X (No Exo)";

        y = lineChart.AddSerie<Line>();
        y.lineStyle.width = 2;
        y.symbol.show = false;
        y.serieName = "Y (No Exo)";

        z = lineChart.AddSerie<Line>();
        z.lineStyle.width = 2;
        z.symbol.show = false;
        z.serieName = "Z (No Exo)";

        x_exo = lineChart.AddSerie<Line>();
        x_exo.lineStyle.width = 2;
        x_exo.symbol.show = false;
        x_exo.serieName = "X (Exo)";

        y_exo = lineChart.AddSerie<Line>();
        y_exo.lineStyle.width = 2;
        y_exo.symbol.show = false;
        y_exo.serieName = "Y (Exo)";

        z_exo = lineChart.AddSerie<Line>();
        z_exo.lineStyle.width = 2;
        z_exo.symbol.show = false;
        z_exo.serieName = "Z (Exo)";


        x_axis = lineChart.GetChartComponent<XAxis>();

        for (int i = 0; i < currentIndex; i++)
        {
            var point = data[i];
            x_axis.AddData(point.timestamp.ToString());
            x.AddData(i, point.data_x);
            y.AddData(i, point.data_y);
            z.AddData(i, point.data_z);
            x_exo.AddData(i, point.data_x_exo);
            y_exo.AddData(i, point.data_y_exo);
            z_exo.AddData(i, point.data_z_exo);
        }
        StartCoroutine(TickValues());
    }

    private IEnumerator TickValues()
    {
        while (currentIndex < data.Count)
        {
            yield return new WaitForSeconds(0.1f);
            var point = data[currentIndex];

            x.AddData(currentIndex, point.data_x);
            y.AddData(currentIndex, point.data_y);
            z.AddData(currentIndex, point.data_z);
            x_exo.AddData(currentIndex, point.data_x_exo);
            y_exo.AddData(currentIndex, point.data_y_exo);
            z_exo.AddData(currentIndex, point.data_z_exo);
            x_axis.AddData(point.timestamp.ToString());

            x.RemoveData(0);
            y.RemoveData(0);
            z.RemoveData(0);
            x_exo.RemoveData(0);
            y_exo.RemoveData(0);
            z_exo.RemoveData(0);
            x_axis.RemoveData(0);

            currentIndex++;
        }
    }
}
