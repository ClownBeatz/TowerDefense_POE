// HitFlash.cs
using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    public Color flashColor = Color.white;
    public float flashDuration = 0.12f;

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine activeFlash;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
    }

    public void Flash()
    {
        if (activeFlash != null)
            StopCoroutine(activeFlash);

        activeFlash = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = originalColors[i];

        activeFlash = null;
    }
}