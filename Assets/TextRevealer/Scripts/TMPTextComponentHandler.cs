using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TMPTextComponentHandler : TextComponentHandler
{
    private const string ColorFieldName = "m_fontColor.a";
    private TMP_Text textComponent;

    public TMPTextComponentHandler(TMP_Text textComponent)
    {
        this.textComponent = textComponent;
    }

    public override List<Transform> CreateSlices(Transform slicedParent)
    {
        List<Transform> slices = new List<Transform>();
        List<CharacterInfo> characterInfos = GetCharacterInfo();

        int i = 0;
        foreach (var characterInfo in characterInfos)
        {
            if (char.IsWhiteSpace(characterInfo.TextValue) || characterInfo.TextValue == '\n')
            {
                continue;
            }

            GameObject slice = new GameObject(textComponent.gameObject.name + "_slice_" + i++.ToString());
            slice.transform.SetParent(slicedParent);

            TMP_Text sliceText = this.textComponent is TextMeshPro ? (TMP_Text) slice.AddComponent<TextMeshPro>() : (TMP_Text) slice.AddComponent<TextMeshProUGUI>();
            sliceText.text = characterInfo.Style.ToString() + characterInfo.TextValue.ToString();
            sliceText.alignment = TextAlignmentOptions.MidlineGeoAligned;
            slice.transform.localPosition = characterInfo.Position;
            CopyTextFields(sliceText, textComponent);

            slice.transform.localRotation = Quaternion.identity;
            slice.transform.localScale = Vector3.one;
            slices.Add(slice.transform);
        }

        return slices;
    }

    public override void TurnOffTextAlpha()
    {
        OriginalOpacity = textComponent.color.a;
        textComponent.color = GetTransparentColor();
    }

    public override void RevertTextAlpha()
    {
        Color currentColor = textComponent.color;
        textComponent.color = new Color(currentColor.r, currentColor.g, currentColor.b, OriginalOpacity);
    }

    public override void SetColorCurve(AnimationClip clip, string sliceName, AnimationCurve curveColor)
    {
        clip.SetCurve(sliceName, typeof(TMP_Text), ColorFieldName, curveColor);
    }

    // private List<CharacterInfo> GetCharacterInfo()
    // {
    //     List<CharacterInfo> indexes = new List<CharacterInfo>();
    //     textComponent.ForceMeshUpdate();
    //
    //     for (int index = 0; index < textComponent.text.Length; index++)
    //     {
    //         if (!char.IsWhiteSpace(textComponent.textInfo.characterInfo[index].character))
    //         {
    //             Vector3 locUpperLeft = textComponent.textInfo.characterInfo[index].topLeft;
    //             Vector3 locBottomRight = textComponent.textInfo.characterInfo[index].bottomRight;
    //
    //             Vector3 mid = new Vector3((locUpperLeft.x + locBottomRight.x) / 2.0f, (locUpperLeft.y + locBottomRight.y) / 2.0f, (locUpperLeft.z + locBottomRight.z) / 2.0f);
    //
    //             indexes.Add(new CharacterInfo() { Position = mid, TextValue = textComponent.textInfo.characterInfo[index].character });
    //         }
    //     }
    //
    //     return indexes;
    // }

    private List<CharacterInfo> GetCharacterInfo()
    {
        List<CharacterInfo> characterInfos = new List<CharacterInfo>();

        // Make sure the mesh and text info is up-to-date.
        textComponent.ForceMeshUpdate();

        // Parse the raw text to associate visible characters with active style tags.
        string rawText = textComponent.text;
        List<(char character, string style, int rawIndex)> parsedCharacters = new List<(char, string, int)>();
        Stack<string> activeStyles = new Stack<string>();
        bool inTag = false;
        string tagBuffer = "";
        int actualCharCounter = 0;
        
        // Loop over each character in the raw text.
        for (int i = 0; i < rawText.Length; i++)
        {
            char c = rawText[i];

            if (c == '<')
            {
                inTag = true;
                tagBuffer = "<";
                continue;
            }

            if (inTag)
            {
                tagBuffer += c;
                if (c == '>')
                {
                    inTag = false;
                    // Determine if the tag is an opening or closing tag.
                    if (tagBuffer.StartsWith("</"))
                    {
                        // Example: </color>
                        // Extract the tag name (without the "/" and angle brackets)
                        string tagName = tagBuffer.Substring(2, tagBuffer.Length - 3).Trim();
                        // Pop from the stack if the tag matches the most recent opening tag.
                        if (activeStyles.Count > 0)
                        {
                            // Get the most recent tag (e.g. "<color=red>")
                            string lastTag = activeStyles.Peek();
                            // Extract the tag name from the opening tag.
                            int startIndex = 1; // skip '<'
                            int endIndex = lastTag.IndexOfAny(new char[] { '=', '>' });
                            if (endIndex > startIndex)
                            {
                                string openTagName = lastTag.Substring(startIndex, endIndex - startIndex).Trim();
                                if (openTagName == tagName)
                                {
                                    activeStyles.Pop();
                                }
                            }
                        }
                    }
                    else
                    {
                        // This is an opening tag. We assume tags like <color=...> or others.
                        activeStyles.Push(tagBuffer);
                    }
                    tagBuffer = "";
                }
                continue;
            }

            // If not inside a tag, then this is a visible character.
            // Join the active tags (if any) to form the style string.
            // Here we simply concatenate them in the order they were added.
            string currentStyle = activeStyles.Count > 0 ? string.Join("", activeStyles.Reverse()) : "";
            parsedCharacters.Add((c, currentStyle, actualCharCounter));
            actualCharCounter++;
        }

        // Now, the parsedCharacters list should match the visible characters in the textInfo.
        // Make sure they have the same count.
        TMP_TextInfo textInfo = textComponent.textInfo;

        // Loop through the TMP character info (which excludes markup)
        foreach (var p in parsedCharacters)
        {
            TMP_CharacterInfo tmpCharInfo = textInfo.characterInfo[p.rawIndex];
            // Optionally skip whitespace as in your original code.
            if (char.IsWhiteSpace(tmpCharInfo.character))
                continue;

            // Compute the mid point between topLeft and bottomRight.
            Vector3 locUpperLeft = tmpCharInfo.topLeft;
            Vector3 locBottomRight = tmpCharInfo.bottomRight;
            Vector3 mid = (locUpperLeft + locBottomRight) / 2.0f;
            
            characterInfos.Add(new CharacterInfo()
            {
                Position = mid,
                TextValue = p.character,
                Style = p.style
            });
        }

        return characterInfos;
    }
    
    private Color GetTransparentColor()
    {
        Color currentColor = textComponent.color;
        return new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
    }

    private void CopyTextFields(TMP_Text sliceText, TMP_Text textComponent)
    {
        sliceText.font = textComponent.font;
        sliceText.fontSize = textComponent.fontSize;
        sliceText.fontStyle = textComponent.fontStyle;
        sliceText.color = textComponent.color;
        sliceText.fontSharedMaterial = textComponent.fontSharedMaterial;
        sliceText.colorGradient = textComponent.colorGradient;
        sliceText.colorGradientPreset = textComponent.colorGradientPreset;
        sliceText.enableVertexGradient = textComponent.enableVertexGradient;
        sliceText.lineSpacing = textComponent.lineSpacing;
        sliceText.material = textComponent.material;
        sliceText.overflowMode = TextOverflowModes.Overflow;
        sliceText.enableWordWrapping = true;
    }
}
