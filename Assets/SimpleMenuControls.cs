using UnityEngine;
using UnityEngine.UI;

public class MenuControllerInput : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button playButton;
    public Button extrasButton;
    public Button exitButton;

    [Header("Controller Settings")]
    public string actionButtonName = "Submit";  // The name of the button in the input settings (e.g., "Submit")
    public string navigateButtonName = "Horizontal";  // Axis for left/right navigation (optional)
    
    private Button selectedButton;  // Track the currently selected button

    private void Start()
    {
        // Set the default selected button, e.g., playButton
        SetSelectedButton(playButton);
    }

    private void Update()
    {
        // Check for controller input to "submit" (A button on Xbox, X on PlayStation, etc.)
        if (Input.GetButtonDown(actionButtonName))
        {
            // If a button is selected, trigger its onClick event
            selectedButton?.onClick.Invoke();
        }

        // Optional: Add horizontal navigation between buttons
        float navigateInput = Input.GetAxisRaw(navigateButtonName);
        if (navigateInput > 0.5f)
        {
            // Move to the next button (e.g., from Play to Extras)
            if (selectedButton == playButton)
                SetSelectedButton(extrasButton);
            else if (selectedButton == extrasButton)
                SetSelectedButton(exitButton);
        }
        else if (navigateInput < -0.5f)
        {
            // Move to the previous button (e.g., from Exit to Extras)
            if (selectedButton == exitButton)
                SetSelectedButton(extrasButton);
            else if (selectedButton == extrasButton)
                SetSelectedButton(playButton);
        }
    }

    private void SetSelectedButton(Button button)
    {
        selectedButton = button;
        selectedButton.Select();  // This highlights the button for controller navigation
    }
}
