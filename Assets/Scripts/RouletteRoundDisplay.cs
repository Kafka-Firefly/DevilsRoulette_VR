using UnityEngine;

/// <summary>Creates a small, readable round display standing on the table in The Pit.</summary>
public sealed class RouletteRoundDisplay : MonoBehaviour
{
    private TextMesh displayText;

    private void Awake()
    {
        Build();
    }

    /// <summary>Creates the placard. Awake calls this; the demo capture calls it explicitly in Edit mode.</summary>
    public void Build()
    {
        if (displayText != null)
            return;
        CreateDisplay();
    }

    public void Show(int round, int spentChambers, int chamberCount, int targetHits, string feedback)
    {
        if (displayText == null)
            return;

        displayText.text = "THE PIT\n" +
                           "ROUND " + round + "  |  CHAMBER " + spentChambers + " / " + chamberCount + "\n" +
                           "TARGET HITS: " + targetHits + "\n" +
                           Wrap(feedback, 24) + "\n" +
                           "GRIP  \u00b7  TRIGGER: FIRE";
    }

    private static string Wrap(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text;

        var builder = new System.Text.StringBuilder(text.Length + 4);
        int lineLength = 0;
        foreach (string word in text.Split(' '))
        {
            if (lineLength > 0 && lineLength + word.Length + 1 > maxChars)
            {
                builder.Append('\n');
                lineLength = 0;
            }
            else if (lineLength > 0)
            {
                builder.Append(' ');
                lineLength++;
            }

            builder.Append(word);
            lineLength += word.Length;
        }

        return builder.ToString();
    }

    private void CreateDisplay()
    {
        var boardRoot = new GameObject("Round Status Display");
        GameObject tabletop = GameObject.Find("Tabletop");

        Vector3 anchor = tabletop != null
            ? tabletop.transform.position
            : transform.position;

        // Stands on the back-left corner of the table, tilted up towards the player.
        boardRoot.transform.position = anchor + new Vector3(-0.42f, 0.30f, 0.30f);
        boardRoot.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backing.name = "Display Backing";
        backing.transform.SetParent(boardRoot.transform, false);
        backing.transform.localScale = new Vector3(0.62f, 0.30f, 0.03f);
        SetUnlitColor(backing.GetComponent<Renderer>(), new Color(0.030f, 0.038f, 0.046f));
        Destroy(backing.GetComponent<Collider>());

        GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        accent.name = "Display Accent";
        accent.transform.SetParent(boardRoot.transform, false);
        accent.transform.localPosition = new Vector3(0f, 0.138f, -0.019f);
        accent.transform.localScale = new Vector3(0.52f, 0.011f, 0.006f);
        SetUnlitColor(accent.GetComponent<Renderer>(), new Color(0.72f, 0.055f, 0.035f));
        Destroy(accent.GetComponent<Collider>());

        var textObject = new GameObject("Display Text");
        textObject.transform.SetParent(boardRoot.transform, false);
        textObject.transform.localPosition = new Vector3(-0.272f, 0.118f, -0.020f);
        displayText = textObject.AddComponent<TextMesh>();
        displayText.anchor = TextAnchor.UpperLeft;
        displayText.alignment = TextAlignment.Left;
        displayText.fontSize = 42;
        displayText.characterSize = 0.0088f;
        displayText.lineSpacing = 1.05f;
        displayText.color = new Color(0.91f, 0.84f, 0.68f);

        // World-space TextMesh uses the GUI text shader, which ignores depth by default.
        if (displayText.font != null && displayText.font.material != null)
            displayText.font.material.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);

        // Two small legs so the placard looks mounted instead of floating.
        for (int x = -1; x <= 1; x += 2)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Display Leg";
            leg.transform.SetParent(boardRoot.transform, false);
            leg.transform.localPosition = new Vector3(x * 0.22f, -0.19f, 0.02f);
            leg.transform.localScale = new Vector3(0.016f, 0.045f, 0.016f);
            SetUnlitColor(leg.GetComponent<Renderer>(), new Color(0.36f, 0.27f, 0.14f));
            Destroy(leg.GetComponent<Collider>());
        }
    }

    private static void SetUnlitColor(Renderer renderer, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            renderer.sharedMaterial = material;
        }
        else if (renderer.sharedMaterial != null)
        {
            renderer.material.color = color;
        }
    }
}
