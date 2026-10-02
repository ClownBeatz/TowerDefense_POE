using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Generates the HUD icon sprites.</summary>
public static class IconFactory
{
    private const string Folder = "Assets/Textures/UI";
    private const int Size = 128;
    private const int Samples = 4;

    private static readonly Color Outline = new Color(0.08f, 0.08f, 0.1f);

    public static Dictionary<string, Sprite> BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Textures", "UI");

        return new Dictionary<string, Sprite>
        {
            ["coin"] = Save("Coin",
                (p => Circle(p, Vector2.zero, 0.92f), Outline),
                (p => Circle(p, Vector2.zero, 0.82f), new Color(0.85f, 0.55f, 0.05f)),
                (p => Circle(p, Vector2.zero, 0.68f), new Color(1f, 0.8f, 0.2f)),
                (p => Ring(p, Vector2.zero, 0.5f, 0.06f), new Color(0.85f, 0.55f, 0.05f)),
                (p => Circle(p, new Vector2(-0.3f, 0.32f), 0.14f), new Color(1f, 0.95f, 0.65f))),

            ["heart"] = Save("Heart",
                (p => Heart(p, 1.0f), Outline),
                (p => Heart(p, 0.85f), new Color(0.9f, 0.2f, 0.25f)),
                (p => Circle(p, new Vector2(-0.35f, 0.3f), 0.13f), new Color(1f, 0.6f, 0.6f))),

            ["gunner"] = Save("GunnerIcon",
                (p => Segment(p, new Vector2(0f, -0.5f), new Vector2(0f, 0.45f), 0.45f), Outline),
                (p => Segment(p, new Vector2(0f, -0.5f), new Vector2(0f, 0.45f), 0.36f), new Color(0.2f, 0.5f, 1f)),
                (p => Box(p, new Vector2(0.45f, 0.15f), new Vector2(0.35f, 0.1f), 0f), Outline),
                (p => Circle(p, new Vector2(0.05f, 0.4f), 0.12f), Color.white)),

            ["mortar"] = Save("MortarIcon",
                (p => Box(p, new Vector2(0.2f, 0.3f), new Vector2(0.24f, 0.5f), -35f), Outline),
                (p => Box(p, new Vector2(0.2f, 0.3f), new Vector2(0.16f, 0.42f), -35f), new Color(0.35f, 0.37f, 0.42f)),
                (p => Box(p, new Vector2(0f, -0.55f), new Vector2(0.8f, 0.28f), 0f), Outline),
                (p => Box(p, new Vector2(0f, -0.55f), new Vector2(0.72f, 0.2f), 0f), new Color(0.35f, 0.37f, 0.42f)),
                (p => Circle(p, new Vector2(0f, -0.2f), 0.42f), Outline),
                (p => Circle(p, new Vector2(0f, -0.2f), 0.34f), new Color(0.45f, 0.47f, 0.52f)),
                (p => Box(p, new Vector2(0f, -0.2f), new Vector2(0.35f, 0.06f), 0f), new Color(1f, 0.55f, 0.1f))),

            ["frost"] = Save("FrostIcon",
                (p => Diamond(p, 0.95f), Outline),
                (p => Diamond(p, 0.82f), new Color(0.35f, 0.85f, 1f)),
                (p => Diamond(p, 0.42f), new Color(0.85f, 0.97f, 1f))),

            ["enemy"] = Save("EnemyIcon",
                (p => Circle(p, Vector2.zero, 0.9f), Outline),
                (p => Circle(p, Vector2.zero, 0.8f), new Color(0.9f, 0.2f, 0.2f)),
                (p => Circle(p, new Vector2(-0.3f, 0.15f), 0.18f), Outline),
                (p => Circle(p, new Vector2(0.3f, 0.15f), 0.18f), Outline),
                (p => Box(p, new Vector2(0f, -0.35f), new Vector2(0.35f, 0.07f), 0f), Outline)),

            ["clock"] = Save("ClockIcon",
                (p => Circle(p, Vector2.zero, 0.92f), Outline),
                (p => Circle(p, Vector2.zero, 0.8f), Color.white),
                (p => Box(p, new Vector2(0f, 0.25f), new Vector2(0.07f, 0.3f), 0f), Outline),
                (p => Box(p, new Vector2(0.2f, 0f), new Vector2(0.25f, 0.07f), 0f), Outline)),

            ["pause"] = Save("PauseIcon",
                (p => Box(p, new Vector2(-0.35f, 0f), new Vector2(0.2f, 0.8f), 0f), Color.white),
                (p => Box(p, new Vector2(0.35f, 0f), new Vector2(0.2f, 0.8f), 0f), Color.white))
        };
    }

    //Shapes (p is in -1..1 space, y up)

    private static bool Circle(Vector2 p, Vector2 centre, float radius) => (p - centre).sqrMagnitude <= radius * radius;

    private static bool Ring(Vector2 p, Vector2 centre, float radius, float halfWidth) =>
        Mathf.Abs((p - centre).magnitude - radius) <= halfWidth;

    private static bool Diamond(Vector2 p, float size) => Mathf.Abs(p.x) * 0.8f + Mathf.Abs(p.y) <= size;

    private static bool Box(Vector2 p, Vector2 centre, Vector2 halfSize, float angleDegrees)
    {
        Vector2 local = Quaternion.Euler(0f, 0f, -angleDegrees) * (p - centre);
        return Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.y) <= halfSize.y;
    }

    private static bool Segment(Vector2 p, Vector2 a, Vector2 b, float radius)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).sqrMagnitude <= radius * radius;
    }

    // Classic implicit heart curve: (x^2 + y^2 - 1)^3 - x^2 y^3 <= 0
    private static bool Heart(Vector2 p, float scale)
    {
        float x = p.x / (scale * 0.85f);
        float y = (p.y + 0.1f) / (scale * 0.85f);
        float a = x * x + y * y - 1f;
        return a * a * a - x * x * y * y * y <= 0f;
    }

    //Rendering

    private static Sprite Save(string name, params (Func<Vector2, bool> shape, Color color)[] layers)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = SamplePixel(x, y, layers);
        }

        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Supersamples each pixel so the shape edges come out smooth.
    private static Color SamplePixel(int x, int y, (Func<Vector2, bool> shape, Color color)[] layers)
    {
        Color sum = Color.clear;

        for (int sy = 0; sy < Samples; sy++)
        {
            for (int sx = 0; sx < Samples; sx++)
            {
                Vector2 p = new Vector2(
                    (x + (sx + 0.5f) / Samples) / Size * 2f - 1f,
                    (y + (sy + 0.5f) / Samples) / Size * 2f - 1f);

                Color sample = Color.clear;
                foreach ((Func<Vector2, bool> shape, Color color) layer in layers)
                {
                    if (layer.shape(p))
                        sample = layer.color;
                }

                sum += sample;
            }
        }

        return sum / (Samples * Samples);
    }
}
