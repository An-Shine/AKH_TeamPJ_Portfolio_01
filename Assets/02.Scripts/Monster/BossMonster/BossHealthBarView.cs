using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _02.Scripts.Monster.BossMonster
{
    public sealed class BossHealthBarView : MonoBehaviour
    {
        private sealed class Layout
        {
            public RectTransform Rect;
            public Vector2 Position;
            public Vector2 Size;
        }

        private readonly List<Layout> _layouts = new List<Layout>(8);
        private global::BossMonster _owner;
        private string _displayName;
        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _panel;
        private RectTransform _fill;
        private RectTransform _trail;
        private RectTransform _charge;
        private Image _fillImage;
        private Image _chargeImage;
        private TMP_Text _label;
        private TMP_Text _detail;
        private Color _fillColor;
        private Color _textColor = new Color(1f, 0.9411765f, 0.80784315f);
        private float _lastRatio = 1f;
        private float _trailRatio = 1f;
        private float _flashUntil;
        private float _referenceWidth = 1280f;

        public static BossHealthBarView Create(global::BossMonster owner, string displayName,
            TMP_FontAsset font, Color fallbackColor)
        {
            GameObject root = new GameObject(owner.name + "_HealthBar", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup), typeof(BossHealthBarView));
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            BossHealthBarView view = root.GetComponent<BossHealthBarView>();
            view._owner = owner;
            view._displayName = displayName;
            view.Build(font, fallbackColor);
            view.Refresh();
            return view;
        }

        private void Build(TMP_FontAsset font, Color fallbackColor)
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            HealthUI playerHud = null;
            foreach (HealthUI candidate in FindObjectsByType<HealthUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene == _owner.gameObject.scene)
                {
                    playerHud = candidate;
                    break;
                }
            }

            Image sourceFrame = FindImage(playerHud, "HealthFrame");
            Image sourceTrack = FindImage(playerHud, "TrackBackground");
            Image sourceTrail = FindImage(playerHud, "ChangeTrail");
            Slider slider = playerHud != null ? playerHud.GetComponentInChildren<Slider>(true) : null;
            Image sourceFill = slider != null && slider.fillRect != null
                ? slider.fillRect.GetComponent<Image>() : null;
            TMP_Text sourceText = playerHud != null ? playerHud.GetComponentInChildren<TMP_Text>(true) : null;
            if (sourceText != null)
            {
                font = sourceText.font;
                _textColor = sourceText.color;
            }

            CanvasScaler playerScaler = playerHud != null ? playerHud.GetComponentInParent<CanvasScaler>() : null;
            if (playerScaler != null && playerScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.referenceResolution = playerScaler.referenceResolution;
                scaler.matchWidthOrHeight = playerScaler.matchWidthOrHeight;
            }

            _referenceWidth = Mathf.Max(1f, scaler.referenceResolution.x);
            _fillColor = sourceFill != null ? sourceFill.color : fallbackColor;

            _panel = new GameObject("PlayerStyleBossHUD", typeof(RectTransform)).GetComponent<RectTransform>();
            _panel.SetParent(transform, false);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 1f);
            _panel.pivot = new Vector2(0.5f, 1f);
            _panel.anchoredPosition = new Vector2(0f, -24f);

            Image frame = CreateImage("BossHealthFrame", _panel, sourceFrame, Color.white,
                "Monster/BossHitUpgrade/PlayerStyleFrame");
            Stretch(frame.rectTransform);

            // The source frame is 576 x 128. Keep its proportions and the fill's placement inside it.
            RectTransform track = new GameObject("BossFillArea", typeof(RectTransform)).GetComponent<RectTransform>();
            track.SetParent(_panel, false);
            track.anchorMin = track.anchorMax = Vector2.one * 0.5f;
            track.anchoredPosition = new Vector2(47f, -3f) / 576f;
            track.sizeDelta = new Vector2(406f, 16f) / 576f;
            _layouts.Add(new Layout { Rect = track, Position = track.anchoredPosition, Size = track.sizeDelta });

            Image background = CreateImage("BossTrack", track, sourceTrack,
                new Color(0.14117648f, 0.10980392f, 0.2f), "Monster/BossHitUpgrade/PlayerStyleFill");
            Stretch(background.rectTransform);
            Image trail = CreateImage("BossDamageTrail", track, sourceTrail,
                new Color(0.74509805f, 0.32156864f, 0.59607846f), "Monster/BossHitUpgrade/PlayerStyleFill");
            trail.color = new Color(0.74509805f, 0.32156864f, 0.59607846f);
            _trail = trail.rectTransform;
            _fillImage = CreateImage("BossHealthFill", track, sourceFill, _fillColor,
                "Monster/BossHitUpgrade/PlayerStyleFill");
            _fill = _fillImage.rectTransform;

            _label = CreateText("BossNameAndHP", _panel, font, 22f);
            SetTextArea(_label.rectTransform, new Vector2(-138f, 10f), new Vector2(388f, 35f));
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 12f;
            _label.fontSizeMax = 22f;

            TMP_Text socket = CreateText("BossFrameBadge", _panel, font, 14f);
            socket.text = "BOSS";
            socket.alignment = TextAlignmentOptions.Center;
            SetTextArea(socket.rectTransform, new Vector2(-258f, -27f), new Vector2(64f, 54f));

            Sprite portraitSprite = FindBodySprite();
            if (portraitSprite != null)
            {
                Image portrait = CreateImage("BossPortrait", _panel, null, Color.white, string.Empty);
                portrait.sprite = portraitSprite;
                portrait.preserveAspect = true;
                SetTextArea(portrait.rectTransform, new Vector2(-259f, -27f), new Vector2(64f, 64f));
                socket.fontSize = 10f;
                SetTextArea(socket.rectTransform, new Vector2(-262f, -45f), new Vector2(70f, 18f));
            }

            _detail = CreateText("BossPattern", _panel, font, 11f);
            SetTextArea(_detail.rectTransform, new Vector2(-138f, -43f), new Vector2(388f, 23f));
            _detail.enableAutoSizing = true;
            _detail.fontSizeMin = 8f;
            _detail.fontSizeMax = 11f;

            Image chargeTrack = CreateImage("BossChargeTrack", _panel, sourceTrack,
                new Color(0.14117648f, 0.10980392f, 0.2f), "Monster/BossHitUpgrade/PlayerStyleFill");
            SetTextArea(chargeTrack.rectTransform, new Vector2(-156f, -14f), new Vector2(406f, 2f));
            _chargeImage = CreateImage("BossCharge", chargeTrack.transform, sourceFill, _fillColor,
                "Monster/BossHitUpgrade/PlayerStyleFill");
            _charge = _chargeImage.rectTransform;

            _lastRatio = _trailRatio = Mathf.Clamp01(_owner.CurrentHealth / _owner.MaximumHealth);
        }

        private static Image FindImage(HealthUI source, string imageName)
        {
            if (source != null)
            {
                foreach (Image image in source.GetComponentsInChildren<Image>(true))
                {
                    if (image.name == imageName)
                    {
                        return image;
                    }
                }
            }

            return null;
        }

        private Sprite FindBodySprite()
        {
            Sprite sprite = null;
            float largestArea = 0f;
            foreach (SpriteRenderer renderer in _owner.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || !renderer.enabled)
                {
                    continue;
                }

                float area = renderer.bounds.size.x * renderer.bounds.size.y;
                if (area > largestArea)
                {
                    largestArea = area;
                    sprite = renderer.sprite;
                }
            }

            return sprite;
        }

        private static Image CreateImage(string imageName, Transform parent, Image source, Color color, string fallbackPath)
        {
            Image image = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = source != null ? source.sprite : string.IsNullOrEmpty(fallbackPath)
                ? null : Resources.Load<Sprite>(fallbackPath);
            image.color = source != null ? source.color : color;
            image.material = source != null ? source.material : null;
            image.type = source != null ? source.type : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = source != null ? source.pixelsPerUnitMultiplier : 1f;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateText(string textName, Transform parent, TMP_FontAsset font, float size)
        {
            TMP_Text text = new GameObject(textName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = _textColor;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private void SetTextArea(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position / 576f;
            rect.sizeDelta = size / 576f;
            foreach (Layout layout in _layouts)
            {
                if (layout.Rect == rect)
                {
                    layout.Position = rect.anchoredPosition;
                    layout.Size = rect.sizeDelta;
                    return;
                }
            }

            _layouts.Add(new Layout { Rect = rect, Position = rect.anchoredPosition, Size = rect.sizeDelta });
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void SetRatio(RectTransform rect, float ratio)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public void Refresh()
        {
            if (_owner == null || _canvas == null)
            {
                return;
            }

            UpdateVisibility();
            if (!_canvas.enabled)
            {
                return;
            }

            UpdateLayout();
            float ratio = Mathf.Clamp01(_owner.CurrentHealth / _owner.MaximumHealth);
            if (ratio < _lastRatio)
            {
                _flashUntil = Time.time + 0.2f;
            }

            _lastRatio = ratio;
            _trailRatio = ratio > _trailRatio ? ratio : Time.time < _flashUntil
                ? _trailRatio : Mathf.MoveTowards(_trailRatio, ratio, Time.deltaTime * 0.8f);
            SetRatio(_fill, ratio);
            SetRatio(_trail, _trailRatio);
            _fillImage.color = Time.time < _flashUntil ? Color.white : _fillColor;
            SetRatio(_charge, _owner.HealthChargeProgress);
            _chargeImage.color = _fillColor;
            _label.text = $"{_displayName}  {_owner.CurrentHealth:0} / {_owner.MaximumHealth:0}";
            _detail.text = _owner.IsExecutionCharging ? "EXECUTION — ENTER SAFE ZONE"
                : string.IsNullOrEmpty(_owner.CurrentPattern) ? "PHASE " + _owner.Phase : _owner.CurrentPattern;
        }

        private void UpdateLayout()
        {
            float virtualWidth = Screen.width / Mathf.Max(0.001f, _canvas.scaleFactor);
            float width = Mathf.Min(526f * _referenceWidth / 1280f, virtualWidth * 0.72f);
            _panel.sizeDelta = new Vector2(width, width * 128f / 576f);
            foreach (Layout layout in _layouts)
            {
                layout.Rect.anchoredPosition = layout.Position * width;
                layout.Rect.sizeDelta = layout.Size * width;
            }
        }

        private void UpdateVisibility()
        {
            if (_canvas == null || _owner == null)
            {
                return;
            }

            Transform player = _owner.Player;
            bool visible = _owner.ShowHealthBar && _owner.isActiveAndEnabled && _owner.CurrentHealth > 0f && Time.timeScale > 0f &&
                !PlayerManager.IsInputBlocked && player != null && player.gameObject.activeInHierarchy &&
                (_owner.Room != null ? _owner.Room.Contains(player.position, 0f) : _owner.Arena.Contains(player.position));
            _canvas.enabled = visible;
            _group.alpha = visible ? 1f : 0f;
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += UpdateVisibility;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= UpdateVisibility;
            if (_group != null)
            {
                _group.alpha = 0f;
            }
        }
    }
}
