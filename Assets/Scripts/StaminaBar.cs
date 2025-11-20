using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fillS; // ZMIENIONE: fillS zamiast fill

    public void SetMaxStamina(int stamina)
    {
        if (slider != null)
        {
            slider.maxValue = stamina;
            slider.value = stamina;
        }

        if (fillS != null && gradient != null)
        {
            fillS.color = gradient.Evaluate(1f);
        }
    }

    public void SetStamina(int stamina)
    {
        if (slider != null)
        {
            slider.value = stamina;
        }

        if (fillS != null && gradient != null)
        {
            fillS.color = gradient.Evaluate(slider.normalizedValue);
        }
    }
}