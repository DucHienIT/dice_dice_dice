using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>Shared helpers for the one-shot game builder. Editor-only.</summary>
    public static class BuilderUtil
    {
        public static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo f = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogError($"[Builder] Field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }
            f.SetValue(target, value);
            if (target is Object obj) EditorUtility.SetDirty(obj);
        }

        public static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[Builder] Sprite not found: " + path);
            return sprite;
        }

        public static Sprite BuiltinUiSprite() =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            Debug.LogWarning("[Builder] Bad hex " + hex);
            return Color.magenta;
        }

        // ---------- world object helpers ----------

        public static GameObject NewChild(GameObject parent, string name,
            Vector3 localPos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static SpriteRenderer NewSpriteChild(GameObject parent, string name, int order,
            Vector3 localPos = default)
        {
            GameObject go = NewChild(parent, name, localPos);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        public static TextMeshPro NewWorldText(GameObject parent, string name, float size,
            Color color, TMP_FontAsset font, Material material, int sortingOrder,
            Vector3 localPos = default)
        {
            GameObject go = NewChild(parent, name, localPos);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            if (material != null) tmp.fontSharedMaterial = material;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(6f, 1.2f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sortingOrder = sortingOrder;
            return tmp;
        }

        // ---------- uGUI helpers (reference canvas 1080×1920, match width) ----------

        public static RectTransform NewUiChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f,
            float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image AddImage(RectTransform rt, Sprite sprite, Color color,
            bool raycast = false, bool sliced = true)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sliced && sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static TextMeshProUGUI AddTmp(RectTransform rt, string text, float size,
            Color color, TMP_FontAsset font, TextAlignmentOptions align, bool wrap = false)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button AddButton(RectTransform rt, Image targetGraphic)
        {
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = targetGraphic;
            targetGraphic.raycastTarget = true;
            var colors = btn.colors;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            btn.colors = colors;
            return btn;
        }
    }
}
