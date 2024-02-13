using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleTwoSprites : MonoBehaviour
{
    public Image offSprite;
    public Image onSprite;

    void Awake()
    {
        var toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnValueChanged);
        bool on = toggle.isOn;
        offSprite.enabled = !on;
        onSprite.enabled = on;
    }

    private void OnValueChanged(bool on)
    {
        offSprite.enabled = !on;
        onSprite.enabled = on;
    }
}
