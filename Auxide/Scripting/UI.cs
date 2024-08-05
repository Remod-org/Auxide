using System.Globalization;
using UnityEngine;

namespace Auxide
{
    public static class UI
    {
        public static CuiElementContainer Container(string panel, string color, string min, string max, bool useCursor = false, string parent = "Overlay")
        {
            return new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color },
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
        }

        public static void Panel(ref CuiElementContainer container, string panel, string color, string min, string max, bool cursor = false)
        {
            container.Add(new CuiPanel
            {
                Image = { Color = color },
                RectTransform = { AnchorMin = min, AnchorMax = max },
                CursorEnabled = cursor
            },
            panel);
        }

        public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, TextAnchor align = TextAnchor.MiddleCenter)
        {
            container.Add(new CuiLabel
            {
                Text = { Color = color, FontSize = size, Align = align, Text = text },
                RectTransform = { AnchorMin = min, AnchorMax = max }
            },
            panel);
        }

        public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
        {
            container.Add(new CuiButton
            {
                Button = { Color = color, Command = command, FadeIn = 0f },
                RectTransform = { AnchorMin = min, AnchorMax = max },
                Text = { Text = text, FontSize = size, Align = align }
            },
            panel);
        }

        public static void Input(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
        {
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = align,
                            CharsLimit = 30,
                            Color = color,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent { AnchorMin = min, AnchorMax = max },
                        new CuiNeedsCursorComponent()
                    }
            });
        }

        public static void Icon(ref CuiElementContainer container, string panel, string color, string imageurl, string min, string max)
        {
            container.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panel,
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = imageurl,
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            Color = color
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = min,
                            AnchorMax = max
                        }
                    }
            });
        }

        public static string Color(string hexColor, float alpha)
        {
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor.Substring(1);
            }
            int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
        }
    }

}
