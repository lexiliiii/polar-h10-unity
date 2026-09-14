using Michsky.UI.Heat;
using UnityEngine;

public class UpdateModelAndDescription : MonoBehaviour
{
    [SerializeField] private HorizontalSelector selector;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float rotationX = 0f;
    [SerializeField] private float rotationY = 180f;
    [SerializeField] private float rotationZ = 0f;
    [SerializeField] private Transform descriptionParent;

    private Transform[] models;
    private GameObject[] descriptions;
    private int currentIndex = -1;

    void Start()
    {
        // Cache all models
        int childCount = transform.childCount;
        models = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            models[i] = transform.GetChild(i);
            models[i].gameObject.SetActive(false);
        }

        // Cache all descriptions
        int descCount = descriptionParent.childCount;
        descriptions = new GameObject[descCount];
        for (int i = 0; i < descCount; i++)
        {
            descriptions[i] = descriptionParent.GetChild(i).gameObject;
            descriptions[i].SetActive(false);
        }

        selector.onValueChanged.AddListener(UpdateActiveItem);
        UpdateActiveItem(selector.index);
    }

    void Update()
    {
        if (currentIndex >= 0 && currentIndex < models.Length)
        {
            models[currentIndex].Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void UpdateActiveItem(int index)
    {
        // Update models
        for (int i = 0; i < models.Length; i++)
            models[i].gameObject.SetActive(i == index);

        if (index >= 0 && index < models.Length)
        {
            models[index].rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
            currentIndex = index;
        }
        else currentIndex = -1;

        // Update descriptions
        for (int i = 0; i < descriptions.Length; i++)
            descriptions[i].SetActive(i == index);
    }
}