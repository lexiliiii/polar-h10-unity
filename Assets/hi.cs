using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

public class hi : MonoBehaviour
{
    private ReceiveLSL receiver;
    public Material[] highlightMaterial;
    public int highlightOffTimer = 10;
    private float[] lastNonRepeatTime;
    private float[] lastNumber;
    private Boolean[] activeParts;
    private int amountOfParts;

    public String CSVFilename;
    private float[,] storedSensorValuesFromCSV;
    private int frameNum = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        receiver = GetComponent<ReceiveLSL>();
        highlightMaterial[0].renderQueue = 3500;
        amountOfParts = highlightMaterial.Length;
        lastNumber = new float[amountOfParts];
        activeParts = new Boolean[amountOfParts];
        lastNonRepeatTime = new float[amountOfParts];
        for (int bodyPartNumber = 0; bodyPartNumber < amountOfParts; bodyPartNumber++)
        {
            activeParts[bodyPartNumber] = true;
        }
        if (CSVFilename.Length > 0)
        {
            TextAsset csvData = Resources.Load<TextAsset>(CSVFilename);
            Debug.Log("The CSV DATA: " + csvData);

            if (csvData != null)
            {
                storedSensorValuesFromCSV = FetchCSVDataFromText(csvData.text);
            }
            else
            {
                Debug.LogError("CSV file not found in Resources: " + CSVFilename);
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        //float time = Mathf.PingPong(Time.time, 2f);
        //float offset = Mathf.Lerp(0.05f, 0.95f, time / 2f);
        for (int bodyPartNumber = 0; bodyPartNumber < amountOfParts; bodyPartNumber++)
        {
            if (CSVFilename.Length > 0)
            {
                Debug.Log("Value from CSV: " + storedSensorValuesFromCSV[frameNum, bodyPartNumber]);
                highlightMaterial[bodyPartNumber].SetColor("_BaseColor", new Color(1, 1, 1, 1));
                float offset = Mathf.Lerp(0.1f, 1f, storedSensorValuesFromCSV[frameNum, bodyPartNumber]);
                highlightMaterial[bodyPartNumber].SetTextureOffset("_DetailAlbedoMap", new Vector2(offset, 0f));
            }
            else
            {
                if (activeParts[bodyPartNumber] == true)
                {
                    if (lastNumber[bodyPartNumber] != receiver.emg_8_channels[bodyPartNumber])
                    {
                        highlightMaterial[bodyPartNumber].SetColor("_BaseColor", new Color(1, 1, 1, 1));
                        float offset = Mathf.Lerp(0.1f, 1f, receiver.emg_8_channels[bodyPartNumber]);
                        highlightMaterial[bodyPartNumber].SetTextureOffset("_DetailAlbedoMap", new Vector2(offset, 0f));
                        lastNonRepeatTime[bodyPartNumber] = Time.time;
                        lastNumber[bodyPartNumber] = receiver.emg_8_channels[bodyPartNumber];
                    }
                    else if (Time.time - lastNonRepeatTime[bodyPartNumber] > highlightOffTimer)
                    {
                        activeParts[bodyPartNumber] = false;
                        highlightMaterial[bodyPartNumber].SetColor("_BaseColor", new Color(0, 0, 0, 0));
                    }
                }
                else
                {
                    if (lastNumber[bodyPartNumber] != receiver.emg_8_channels[bodyPartNumber])
                    {
                        activeParts[bodyPartNumber] = true;
                    }
                }
            }
        }
        if (CSVFilename.Length > 0)
        {
            frameNum = (frameNum + 1) % storedSensorValuesFromCSV.GetLength(0);
            Debug.Log(frameNum);
        }
    }


    public float[,] FetchCSVDataFromText(string csvText)
    {
        // Split CSV by newlines
        string[] allLines = csvText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int rows = allLines.Length;
        int columns = amountOfParts; // number of sensors
        float[,] data = new float[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            string[] values = allLines[i].Split(',');

            values = values.Select(val => val.Trim()).ToArray();

            if (values.Length != columns)
            {
                Debug.LogError("Row " + i + " does not have the correct number of columns (" + values.Length + "). Expected " + columns + " columns");
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
                    Debug.LogWarning("Failed to parse value at row " + i + ", column " + j + ": " + values[j]);
                }
            }
        }

        return data;
    }

}
