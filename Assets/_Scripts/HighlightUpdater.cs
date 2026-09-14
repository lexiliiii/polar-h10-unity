using TMPro;
using UnityEngine;
using System.IO;

//Used to update ergo highlights when exosuits are applied
public class HighlightUpdater : MonoBehaviour
{
    [SerializeField] Color supportedColor;
    [SerializeField] Color unSupportedColor;
    [SerializeField] GameObject ergoHighlight;
    [SerializeField] TextMeshProUGUI labelText;

    [SerializeField] private bool debugOverride = false;
    private Renderer rend;
    private Material runtimeMat;
    private bool hasSuit;

    private float[,] storedSensorValuesFromCSV;
    private int frameNum = 0;
    private void Start()
    {
       if (supportedColor == null)
       {
            Debug.Log("Please assign an exosuit supported color");
       }

        if (unSupportedColor == null)
        {
            Debug.Log("Please assign an unsupported color");
        }

        rend = ergoHighlight.GetComponent<Renderer>();
        runtimeMat = new Material(rend.material);
        rend.material = runtimeMat;
        hasSuit = false;
        FetchCSVData("sample.csv");
    }
    void Update()
    {
        if (RobotSelectionManager.Instance != null)
        {
            hasSuit = RobotSelectionManager.Instance.HasEquippedSuit();
        }
        else
        {
            Debug.LogWarning("RobotSelectionManager not initialized");
        }

        if (debugOverride || hasSuit)
        {
            ergoHighlight.GetComponent<Renderer>().material.SetColor("_Color", supportedColor);
            ergoHighlight.GetComponent<Renderer>().material.SetColor("_EmissionColor", supportedColor);
            labelText.color = supportedColor;
            labelText.text = "Low Risk";
        }
        else
        {
            ergoHighlight.GetComponent<Renderer>().material.SetColor("_Color", unSupportedColor);
            ergoHighlight.GetComponent<Renderer>().material.SetColor("_EmissionColor", unSupportedColor);
            labelText.color = unSupportedColor;
            labelText.text = "High Risk";
        }
    }

    public float[,] FetchCSVData(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("CSV file not found at: " + path);
            return null;
        }

        string[] allLines = File.ReadAllLines(path);
        int rows = allLines.Length;
        int columns = 8;
        float[,] data = new float[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            string[] values = allLines[i].Split(',');

            if (values.Length != columns)
            {
                Debug.LogError("Incorrect number of columns in row " + i);
                continue;
            }

            for (int j = 0; j < columns; j++)
            {
                if (float.TryParse(values[j], out float result))
                {
                    data[i, j] = result;
                }
                else
                {
                    Debug.LogWarning("Failed to parse value at row " + i + ", column " + j);
                }
            }
        }

        return data;
    }
}
