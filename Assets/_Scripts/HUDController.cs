using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    public Transform leftHandAnchor;
    public GameObject ActivityMenu;
    public GameObject ActivityHUDCanvas;
    private Canvas progressCanvas;
    public GameObject MenuRayInteraction;
    public GameObject ModalRayInteraction;

    [SerializeField] private float offsetX = 0f;
    [SerializeField] private float offsetY = 0.5f;
    [SerializeField] private float offsetZ = 0f;

    private bool isActivityMenuActive = false;

    private bool hudHidden = false;

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Start")
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            ActivityHUDCanvas.SetActive(true);
            ActivityMenu.SetActive(false);
            SetRayInteraction(MenuRayInteraction);

            if (leftHandAnchor != null)
            {
                Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);
                ActivityMenu.transform.SetParent(leftHandAnchor);
                ActivityMenu.transform.localPosition = offset;
            }

            TryFindProgressHUDCanvas();
            if (progressCanvas != null)
                progressCanvas.enabled = true;
        }
    }

    void Update()
    {
        // Y button toggles between Activity Menu and HUD (only if HUD is visible)
        if (OVRInput.GetDown(OVRInput.Button.Four) && !hudHidden)
        {
            ToggleActivityMenuAndHUD();
        }

        // X button toggles entire HUD (only if Activity Menu is not currently active)
        if (OVRInput.GetDown(OVRInput.Button.Three) && !isActivityMenuActive)
        {
            ToggleEntireHUD();
        }
    }

    private void ToggleActivityMenuAndHUD()
    {
        isActivityMenuActive = !isActivityMenuActive;

        // Reset ray interaction first
        SetRayInteraction(null);

        ActivityMenu.SetActive(isActivityMenuActive);
        ActivityHUDCanvas.SetActive(!isActivityMenuActive);

        if (progressCanvas != null)
            progressCanvas.enabled = !isActivityMenuActive;

        SetRayInteraction(isActivityMenuActive ? MenuRayInteraction : MenuRayInteraction);
    }

    private void ToggleEntireHUD()
    {
        hudHidden = !hudHidden;

        SetRayInteraction(null); // Clear all interaction

        ActivityHUDCanvas.SetActive(!hudHidden);
        ActivityMenu.SetActive(false);
        if (progressCanvas != null)
            progressCanvas.enabled = !hudHidden;

        if (!hudHidden)
        {
            SetRayInteraction(MenuRayInteraction);
        }

        isActivityMenuActive = false; // Force reset menu state
    }

    private void TryFindProgressHUDCanvas()
    {
        ProgressHUDController hud = FindObjectOfType<ProgressHUDController>();
        if (hud != null)
            progressCanvas = hud.GetComponentInChildren<Canvas>();
    }

    public void OnReturnToMainMenuFromModal()
    {
        Debug.Log("Modal button pressed ? Returning to Main Menu");
        SceneManager.LoadSceneAsync("Start");
    }

    public void OnModalOpened()
    {
        Debug.Log("Switching to Modal Ray Interaction");

        SetRayInteraction(ModalRayInteraction);
    }

    public void OnModalClosed()
    {
        Debug.Log("Switching to Modal Ray Interaction");

        SetRayInteraction(MenuRayInteraction);
    }

    private void SetRayInteraction(GameObject target)
    {
        MenuRayInteraction.SetActive(false);
        ModalRayInteraction.SetActive(false);

        if (target != null)
        {
            target.SetActive(true);
        }
    }
}