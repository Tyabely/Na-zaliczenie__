using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill;

    public void SetMaxHealth(int health)
    {
        if (slider != null)
        {
            slider.maxValue = health;
            slider.value = health;
        }

        if (fill != null && gradient != null)
        {
            fill.color = gradient.Evaluate(1f);
        }
    }

    public void SetHealth(int health)
    {
        if (slider != null)
        {
            slider.value = health;
        }

        if (fill != null && gradient != null)
        {
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }
    }
}