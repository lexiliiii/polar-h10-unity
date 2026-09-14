using System.IO;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Yarn;
using Yarn.Unity;

public class StudentDecisionTaskController : MonoBehaviour
{
    
    public class Task
    {
        public string sensor;
        public List<string> questions = new List<string>();
        public List<string> answers = new List<string>();
        public Task(string sensor)
        {
            this.sensor = sensor;
            questions.Add($"Why did you choose the {sensor}?");
            questions.Add($"What would you measure with an {sensor}?");
            questions.Add($"How would the results from an {sensor} help improve wearabe robot design or use?");
            for (int i = 0; i < questions.Count; i++)
            {
                answers.Add("");
            }
        }
    }
    public List<Task> tasks = new List<Task>();
    public int taskIndex = 0;
    public int questionIndex = 0;
    [SerializeField]
    private TMP_Text questionText;
    [SerializeField]
    private TMP_Text answerText;
    [SerializeField]
    private DialogueRunner dialogueRunner;
    [SerializeField]
    private GameObject taskCompleteUI;
    private int tasksCompleted = 0;

    void Start()
    {
        tasks.Add(new Task("EMG"));
        tasks.Add(new Task("EEG"));
        tasks.Add(new Task("IMU"));
    }
    public void SetTask(int index)
    {
        taskIndex = index;
    }
    public void SetQuestion(int index)
    {
        questionIndex = index;
    }
    public void NextQuestion()
    {
        questionIndex++;
    }
    public void SaveResponse()
    {
        tasks[taskIndex].answers[questionIndex] = answerText.text;
        Debug.Log(tasks[taskIndex].answers[questionIndex]);
    }
    public void DisplayQuestion()
    {
        if(questionIndex < tasks[taskIndex].questions.Count)
            questionText.text = tasks[taskIndex].questions[questionIndex];
        else
        {
            questionText.text = "No more questions.";
            answerText.text = "Please select another sensor.";
            tasksCompleted++;
            if (tasksCompleted == tasks.Count - 1)
            {
                Debug.Log("Minimum tasks completed.");
                dialogueRunner.StartDialogue("StudentDecisionTaskClosingMessage");
                taskCompleteUI.SetActive(true);
            }
            if (tasksCompleted == tasks.Count)
            {
                Debug.Log("All tasks completed.");
                dialogueRunner.StartDialogue("StudentDecisionTaskSecretMessage");
            }
        }     
    }
    
    public void SaveTasks()
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            SaveTaskToText(tasks[i], $"{tasks[i].sensor}_responses.txt");
        }
    }

    public void SaveTaskToText(Task task, string fileName)
    {
        // Choose a safe, cross-platform location
        string path = Path.Combine(Application.persistentDataPath, fileName);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Sensor: {task.sensor}");
        sb.AppendLine();

        for (int i = 0; i < task.questions.Count; i++)
        {
            sb.AppendLine($"Q{i + 1}: {task.questions[i]}");
            sb.AppendLine($"A{i + 1}: {task.answers[i]}");
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());

        Debug.Log($"Task saved to: {path}");
    }
}
