using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.SceneManagement;

public class SensorLabNextScene : MonoBehaviour
{
    public GameObject endSelect;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private string node = "";
    [SerializeField] private string sceneName = "M2 - HRI Demo";
    private List<string> remainingStations = new List<string>(){"IMU", "EEG", "EMG"};
    
    public void VisitStation(string stationName)
    {
        remainingStations.Remove(stationName);
        if (remainingStations.Count == 0)
        {
            endSelect.SetActive(true);
            //dialogueRunner.StartDialogue(node);
        }
    }

    public void NextScene()
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
}
