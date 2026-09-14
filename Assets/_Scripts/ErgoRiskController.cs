using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErgoRiskController : MonoBehaviour
{
    [System.Serializable]
    public class ErgoRisk
    {
        public string key;
        public GameObject highlight;
    }

    [Header("Ergonomic Risks")]
    [SerializeField] private List<ErgoRisk> ergoRisks = new();

    private Dictionary<string, GameObject> toggleDict;

    private Color defaultColor = Color.grey;

    private void Awake()
    {
        toggleDict = new Dictionary<string, GameObject>();
        foreach (var risk in ergoRisks)
        {
            toggleDict.Add(risk.key, risk.highlight);
        }
    }

    public void ActivateHighlightByKey(string key)
    {
        if (toggleDict.ContainsKey(key))
        {
            GameObject highlight = toggleDict[key];
            highlight.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"Toggle key {key} not found or target is null.");
        }
    }

    public void DeactivateHighlightByKey(string key)
    {
        if (toggleDict.ContainsKey(key))
        {
            GameObject highlight = toggleDict[key];
            highlight.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"Toggle key {key} not found or target is null.");
        }
    }
}
