using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Michsky.UI.Heat;

public class RobotSelectionManager : MonoBehaviour
{
    public static RobotSelectionManager Instance { get; private set; }

    [Header("Equipped (confirmed) suit")]
    public ExosuitMaterialSet equippedSet;

    [Header("Currently previewing suit")]
    public ExosuitMaterialSet previewSet;

    [Header("UI")]
    public ModalWindowManager WrongRobotModal;
    public GameObject exo3DModel;

    // Event to notify buttons when a suit is applied / previewed
    public UnityEvent<ExosuitMaterialSet> onSuitApplied = new UnityEvent<ExosuitMaterialSet>();
    public UnityEvent<ExosuitMaterialSet> onSuitPreviewed = new UnityEvent<ExosuitMaterialSet>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PreviewSuit(ExosuitMaterialSet set)
    {
        previewSet = set;
        Debug.Log("Previewing suit: " + set.suitType);
        onSuitPreviewed.Invoke(previewSet);
    }

    public void ApplySuit()
    {
        if (previewSet == null)
        {
            Debug.LogWarning("No suit previewed to apply.");
            return;
        }

        var stage = ProgressManager.Instance?.CurrentStage ?? ProgressManager.Stage.StartWalkthrough;
        bool valid = false;

        // Restrict suit application by current stage
        switch (stage)
        {
            case ProgressManager.Stage.RobotCarpentry:
                valid = previewSet.suitType == ExosuitType.Back;
                if (valid) ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotCarpentryActivitySelect);
                break;

            case ProgressManager.Stage.RobotConstructionLabor:
                valid = previewSet.suitType == ExosuitType.FullBody;
                if (valid) ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotConstructionLaborActivitySelect);
                break;

            case ProgressManager.Stage.RobotDrywall:
            case ProgressManager.Stage.RobotElectrician:
                valid = previewSet.suitType == ExosuitType.Shoulder;
                if (valid)
                {
                    var nextStage = (stage == ProgressManager.Stage.RobotDrywall)
                        ? ProgressManager.Stage.RobotDrywallActivitySelect
                        : ProgressManager.Stage.RobotElectricianActivitySelect;
                    ProgressManager.Instance.AdvanceToStage(nextStage);
                }
                break;

            case ProgressManager.Stage.RobotMasonry:
                valid = previewSet.suitType == ExosuitType.Back;
                if (valid) ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotMasonryActivitySelect);
                break;
        }

        if (!valid)
        {
            Debug.LogWarning($"Incorrect suit type for {stage}. Opening error modal.");
            if (WrongRobotModal != null)
            {
                if (exo3DModel != null)
                    exo3DModel.SetActive(false);

                WrongRobotModal.gameObject.SetActive(true);
                WrongRobotModal.OpenWindow();
                UIController uiController = FindObjectOfType<UIController>();
                if (uiController != null)
                    uiController.OnModalOpened("WRL");
            }
            return;
        }

        // Apply suit
        equippedSet = previewSet;
        Debug.Log("Applied suit: " + equippedSet.suitType);
        onSuitApplied.Invoke(equippedSet);
    }

    public void UnequipSuit()
    {
        equippedSet = null;
        previewSet = null;
        onSuitApplied.Invoke(null);
    }

    public bool HasEquippedSuit() => equippedSet != null;

    private void OnEnable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Start")
        {
            UnequipSuit(); // RESET WHEN RETURNING TO "Start" SCENE
            Debug.Log("[RobotSelectionManager] Reset equipped and preview suit on Start scene load.");
        }

        GameObject modalObject = GameObject.FindWithTag("WrongRobotModal");
        if (modalObject != null)
        {
            WrongRobotModal = modalObject.GetComponent<ModalWindowManager>();
            WrongRobotModal.onClose.RemoveAllListeners();
            WrongRobotModal.onClose.AddListener(() =>
            {
                if (exo3DModel != null)
                    exo3DModel.SetActive(true);
            });
        }
        else
        {
            Debug.LogWarning("[RobotSelectionManager] WrongRobotModal not found in scene.");
        }

        exo3DModel = GameObject.FindWithTag("Exoskeleton");
    }
}