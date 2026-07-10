using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LuckyRoulette
{
    public sealed class LuckyRouletteGame : MonoBehaviour
    {
        [SerializeField] private int startingCoins = 1200;
        [SerializeField] private int baseBet = 100;

        private readonly List<int> wheelValues = new List<int>(12);
        private readonly List<Button> betButtons = new List<Button>(12);
        private readonly List<Text> betButtonTexts = new List<Text>(12);
        private readonly List<Text> wheelLabelTexts = new List<Text>(12);
        private readonly List<ConfettiPiece> confetti = new List<ConfettiPiece>();

        private RectTransform shakeLayer;
        private RectTransform contentRoot;
        private RectTransform canvasRoot;
        private RectTransform wheelSpinRoot;
        private WheelGraphic wheelGraphic;
        private RectTransform pointer;
        private RectTransform hitFlash;
        private Text coinsText;
        private Text streakText;
        private Text betText;
        private Text titleText;
        private Text sequenceText;
        private Text promptText;
        private Text resultText;
        private Text explainText;
        private Text multiplierText;
        private Button spinButton;
        private Button nextButton;
        private Slider betSlider;
        private AudioSource audioSource;

        private SequenceRound currentRound;
        private int selectedIndex = -1;
        private int coins;
        private int bet;
        private int streak;
        private bool spinning;
        private float wheelAngle;
        private float shakeTime;
        private System.Random random;

        private static readonly Color[] SegmentColors =
        {
            new Color(0.64f, 0.18f, 0.22f),
            new Color(0.82f, 0.58f, 0.24f),
            new Color(0.18f, 0.48f, 0.42f),
            new Color(0.18f, 0.34f, 0.58f),
            new Color(0.40f, 0.30f, 0.58f),
            new Color(0.62f, 0.28f, 0.42f)
        };

        private void Awake()
        {
            random = new System.Random(Environment.TickCount);
            coins = startingCoins;
            bet = baseBet;
            EnsureAudioListener();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.62f;
            BuildInterface();
            NewRound();
        }

        private void Update()
        {
            if (titleText != null)
            {
                titleText.color = new Color(0.96f, 0.86f, 0.55f);
            }

            ApplyResponsiveScale();
            AnimateShake();
            AnimateConfetti();
        }

        private void BuildInterface()
        {
            if (Camera.main != null)
                Camera.main.backgroundColor = new Color(0.035f, 0.042f, 0.075f);

            EnsureInputSystemEventSystem();

            var canvas = NewObject<Canvas>("Lucky Roulette Canvas", transform);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            canvasRoot = canvas.GetComponent<RectTransform>();

            shakeLayer = NewPanel("Shake Layer", canvas.transform, Stretch(), new Color(0, 0, 0, 0));
            var background = NewObject<BackgroundGraphic>("Aurora Felt Background", shakeLayer);
            SetRect(background.rectTransform, Stretch());
            background.raycastTarget = false;
            contentRoot = NewPanel("Content Root", shakeLayer, Stretch(), new Color(0, 0, 0, 0));

            var topBar = NewPanel("Top Bar", contentRoot, TopStretch(28, 28, 20, 78), new Color(0.028f, 0.034f, 0.045f, 0.94f));
            AddOutline(topBar.gameObject, new Color(0.95f, 0.78f, 0.42f, 0.14f), new Vector2(0, -1));
            titleText = NewText("Title", topBar, "럭키 룰렛", 42, TextAnchor.MiddleLeft, new Color(0.96f, 0.86f, 0.55f));
            SetRect(titleText.rectTransform, Anchored(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(360, 68)));
            titleText.fontStyle = FontStyle.Bold;
            coinsText = NewText("Coins", topBar, "", 26, TextAnchor.MiddleRight, new Color(0.96f, 0.97f, 0.94f));
            SetRect(coinsText.rectTransform, Box(new Vector2(0.68f, 0), new Vector2(1, 1), new Vector2(0, 8), new Vector2(-84, -8)));
            coinsText.fontStyle = FontStyle.Bold;
            coinsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            streakText = NewText("Streak", topBar, "", 22, TextAnchor.MiddleRight, new Color(0.6f, 0.84f, 0.82f));
            SetRect(streakText.rectTransform, Box(new Vector2(0.52f, 0), new Vector2(0.68f, 1), new Vector2(0, 8), new Vector2(-18, -8)));

            var leftPanel = NewPanel("Sequence Table", contentRoot, Anchored(new Vector2(0, 0.5f), new Vector2(395, -28), new Vector2(700, 740)), new Color(0.055f, 0.066f, 0.082f, 0.96f));
            AddOutline(leftPanel.gameObject, new Color(0.58f, 0.76f, 0.72f, 0.14f), new Vector2(0, -1));
            var concept = NewText("Concept", leftPanel, "고2 수학 · 수열 베팅 테이블", 24, TextAnchor.MiddleLeft, new Color(0.58f, 0.84f, 0.8f));
            SetRect(concept.rectTransform, Anchored(new Vector2(0.5f, 1), new Vector2(0, -54), new Vector2(680, 54)));
            concept.fontStyle = FontStyle.Bold;
            sequenceText = NewText("Sequence", leftPanel, "", 44, TextAnchor.MiddleCenter, new Color(0.98f, 0.98f, 0.94f));
            SetRect(sequenceText.rectTransform, Anchored(new Vector2(0.5f, 1), new Vector2(0, -172), new Vector2(680, 118)));
            sequenceText.fontStyle = FontStyle.Bold;
            promptText = NewText("Prompt", leftPanel, "", 25, TextAnchor.UpperCenter, new Color(0.84f, 0.88f, 0.88f));
            SetRect(promptText.rectTransform, Anchored(new Vector2(0.5f, 1), new Vector2(0, -278), new Vector2(680, 82)));
            multiplierText = NewText("Multiplier", leftPanel, "", 28, TextAnchor.MiddleCenter, new Color(0.94f, 0.74f, 0.36f));
            SetRect(multiplierText.rectTransform, Anchored(new Vector2(0.5f, 1), new Vector2(0, -370), new Vector2(680, 70)));
            multiplierText.fontStyle = FontStyle.Bold;
            explainText = NewText("Explanation", leftPanel, "", 22, TextAnchor.UpperLeft, new Color(0.8f, 0.86f, 0.86f));
            SetRect(explainText.rectTransform, Anchored(new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(626, 220)));
            explainText.horizontalOverflow = HorizontalWrapMode.Wrap;
            explainText.verticalOverflow = VerticalWrapMode.Overflow;
            resultText = NewText("Result", leftPanel, "정답 포켓에 칩을 올리고 룰렛을 돌려라.", 28, TextAnchor.MiddleCenter, new Color(0.96f, 0.9f, 0.72f));
            SetRect(resultText.rectTransform, Anchored(new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(626, 76)));
            resultText.fontStyle = FontStyle.Bold;

            var wheelRoot = NewPanel("Wheel Root", contentRoot, Anchored(new Vector2(0.5f, 0.53f), new Vector2(220, -10), new Vector2(660, 660)), new Color(0, 0, 0, 0));
            wheelSpinRoot = NewPanel("Wheel Spin Root", wheelRoot, Stretch(), new Color(0, 0, 0, 0));
            var rim = NewObject<Image>("Outer Glow", wheelRoot);
            SetRect(rim.rectTransform, Stretch());
            rim.color = new Color(0.95f, 0.72f, 0.35f, 0.10f);
            rim.raycastTarget = false;
            wheelGraphic = NewObject<WheelGraphic>("Number Wheel", wheelSpinRoot);
            SetRect(wheelGraphic.rectTransform, StretchInset(26));
            wheelGraphic.raycastTarget = false;
            for (int i = 0; i < 12; i++)
            {
                var label = NewText("Wheel Number " + i, wheelSpinRoot, "", 28, TextAnchor.MiddleCenter, Color.white);
                label.fontStyle = FontStyle.Bold;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                SetRect(label.rectTransform, Anchored(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92, 48)));
                wheelLabelTexts.Add(label);
            }
            var hub = NewPanel("Hub", wheelSpinRoot, Anchored(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(156, 156)), new Color(0.033f, 0.04f, 0.052f, 0.96f));
            AddOutline(hub.gameObject, new Color(0.88f, 0.7f, 0.36f, 0.72f), new Vector2(0, -2));
            var hubText = NewText("Hub Text", hub, "SPIN", 32, TextAnchor.MiddleCenter, new Color(0.94f, 0.82f, 0.52f));
            SetRect(hubText.rectTransform, Stretch());
            hubText.fontStyle = FontStyle.Bold;
            pointer = NewPanel("Pointer", wheelRoot, Anchored(new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(92, 118)), new Color(1f, 0.92f, 0.22f, 1f));
            pointer.pivot = new Vector2(0.5f, 1f);
            pointer.rotation = Quaternion.Euler(0, 0, 180);
            AddOutline(pointer.gameObject, new Color(0.3f, 0.12f, 0.02f, 0.8f), new Vector2(0, -2));
            hitFlash = NewPanel("Hit Flash", wheelRoot, StretchInset(-24), new Color(1f, 0.92f, 0.35f, 0f));
            hitFlash.SetAsLastSibling();

            var betPanel = NewPanel("Bet Panel", contentRoot, Anchored(new Vector2(1, 0.5f), new Vector2(-255, -30), new Vector2(460, 740)), new Color(0.052f, 0.06f, 0.074f, 0.96f));
            AddOutline(betPanel.gameObject, new Color(0.88f, 0.7f, 0.36f, 0.14f), new Vector2(0, -1));
            var betTitle = NewText("Bet Title", betPanel, "포켓 선택", 26, TextAnchor.MiddleCenter, new Color(0.94f, 0.82f, 0.52f));
            SetRect(betTitle.rectTransform, Anchored(new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(420, 56)));
            betTitle.fontStyle = FontStyle.Bold;
            for (int i = 0; i < 12; i++)
            {
                int index = i;
                float x = -156 + (i % 3) * 156;
                float y = -140 - (i / 3) * 104;
                var button = NewButton("Pocket " + i, betPanel, "", 30, Anchored(new Vector2(0.5f, 1), new Vector2(x, y), new Vector2(128, 78)));
                button.onClick.AddListener(() => SelectPocket(index));
                betButtons.Add(button);
                betButtonTexts.Add(button.GetComponentInChildren<Text>());
            }
            betText = NewText("Bet Text", betPanel, "", 28, TextAnchor.MiddleCenter, Color.white);
            SetRect(betText.rectTransform, Anchored(new Vector2(0.5f, 0), new Vector2(0, 194), new Vector2(420, 54)));
            betSlider = NewObject<Slider>("Bet Slider", betPanel);
            SetRect(betSlider.GetComponent<RectTransform>(), Anchored(new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(390, 34)));
            betSlider.minValue = 50;
            betSlider.maxValue = 500;
            betSlider.wholeNumbers = true;
            betSlider.value = bet;
            betSlider.onValueChanged.AddListener(v =>
            {
                bet = Mathf.Clamp(Mathf.RoundToInt(v / 50f) * 50, 50, Mathf.Max(50, coins));
                UpdateHud();
            });
            StyleSlider(betSlider);
            spinButton = NewButton("Spin Button", betPanel, "룰렛 돌리기", 34, Anchored(new Vector2(0.5f, 0), new Vector2(0, 82), new Vector2(390, 72)));
            spinButton.onClick.AddListener(Spin);
            nextButton = NewButton("Next Button", betPanel, "다음 판", 30, Anchored(new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(390, 52)));
            nextButton.onClick.AddListener(NewRound);
            nextButton.interactable = false;
        }

        private void EnsureAudioListener()
        {
            if (FindObjectOfType<AudioListener>() != null)
                return;

            if (Camera.main != null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
                return;
            }

            gameObject.AddComponent<AudioListener>();
        }

        private void EnsureInputSystemEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            GameObject eventObject;
            if (eventSystem == null)
            {
                eventObject = new GameObject("EventSystem", typeof(EventSystem));
                eventObject.transform.SetParent(transform);
            }
            else
            {
                eventObject = eventSystem.gameObject;
            }

            var oldModule = eventObject.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
                Destroy(oldModule);

            if (eventObject.GetComponent<InputSystemUIInputModule>() == null)
                eventObject.AddComponent<InputSystemUIInputModule>();
        }

        private void NewRound()
        {
            spinning = false;
            selectedIndex = -1;
            currentRound = SequenceRound.Create(random, Mathf.Clamp(streak, 0, 5));
            BuildWheelValues();
            wheelGraphic.SetValues(wheelValues, SegmentColors);
            sequenceText.text = currentRound.SequenceLine;
            promptText.text = currentRound.Prompt;
            multiplierText.text = "정답 배당 x" + currentRound.Payout + "  ·  연승 보너스 +" + Mathf.Min(streak * 10, 50) + "%";
            explainText.text = "힌트: " + currentRound.Hint;
            resultText.text = "정답이라고 생각하는 숫자 포켓에 베팅하세요.";
            nextButton.interactable = false;
            spinButton.interactable = true;
            UpdateHud();
            UpdatePocketButtons();
            UpdateWheelLabels();
        }

        private void BuildWheelValues()
        {
            wheelValues.Clear();
            wheelValues.Add(currentRound.Answer);
            int guard = 0;
            while (wheelValues.Count < 12 && guard++ < 200)
            {
                int candidate = currentRound.MakeDecoy(random);
                if (!wheelValues.Contains(candidate))
                    wheelValues.Add(candidate);
            }
            while (wheelValues.Count < 12)
                wheelValues.Add(currentRound.Answer + wheelValues.Count * 3 + 1);
            for (int i = 0; i < wheelValues.Count; i++)
            {
                int swap = random.Next(i, wheelValues.Count);
                int temp = wheelValues[i];
                wheelValues[i] = wheelValues[swap];
                wheelValues[swap] = temp;
            }
        }

        private void SelectPocket(int index)
        {
            if (spinning)
                return;
            selectedIndex = index;
            resultText.text = wheelValues[index] + " 포켓에 " + bet + "코인 베팅.";
            PlayTone(520f, 0.05f, 0.18f);
            UpdatePocketButtons();
        }

        private void Spin()
        {
            if (spinning || selectedIndex < 0)
            {
                Pulse(resultText.rectTransform, 1.08f);
                resultText.text = "먼저 숫자 포켓을 선택하세요.";
                PlayTone(160f, 0.08f, 0.22f);
                return;
            }
            if (coins < bet)
            {
                resultText.text = "코인이 부족합니다. 베팅 금액을 낮추세요.";
                Pulse(coinsText.rectTransform, 1.1f);
                return;
            }
            StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            spinning = true;
            spinButton.interactable = false;
            nextButton.interactable = false;
            coins -= bet;
            UpdateHud();

            int landingIndex = random.Next(0, wheelValues.Count);
            float segmentAngle = 360f / wheelValues.Count;
            float landingCenter = landingIndex * segmentAngle + segmentAngle * 0.5f;
            float targetAngle = 360f * random.Next(5, 8) + (360f - landingCenter);
            float startAngle = wheelAngle;
            float duration = UnityEngine.Random.Range(3.2f, 4.2f);
            float lastTick = -1f;
            resultText.text = "룰렛 회전 중...";
            PlayTone(270f, 0.08f, 0.18f);

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                float eased = 1f - Mathf.Pow(1f - p, 4.2f);
                wheelAngle = startAngle + targetAngle * eased;
                wheelSpinRoot.localRotation = Quaternion.Euler(0, 0, wheelAngle);
                float tick = Mathf.Floor(wheelAngle / segmentAngle);
                if (!Mathf.Approximately(tick, lastTick))
                {
                    lastTick = tick;
                    pointer.localScale = Vector3.one * (1f + (1f - p) * 0.16f);
                    PlayTone(720f + UnityEngine.Random.Range(-70f, 70f), 0.025f, Mathf.Lerp(0.16f, 0.04f, p));
                }
                yield return null;
            }

            wheelAngle = startAngle + targetAngle;
            wheelSpinRoot.localRotation = Quaternion.Euler(0, 0, wheelAngle);
            pointer.localScale = Vector3.one;
            ResolveSpin(landingIndex);
        }

        private void ResolveSpin(int landingIndex)
        {
            int landedValue = wheelValues[landingIndex];
            bool won = selectedIndex == landingIndex && landedValue == currentRound.Answer;
            bool pickedAnswer = wheelValues[selectedIndex] == currentRound.Answer;
            if (won)
            {
                int bonusPercent = Mathf.Min(streak * 10, 50);
                int payout = Mathf.RoundToInt(bet * currentRound.Payout * (1f + bonusPercent / 100f));
                coins += payout;
                streak++;
                resultText.text = "JACKPOT! " + landedValue + " 적중  +" + payout + "코인";
                explainText.text = currentRound.Explanation;
                BurstConfetti(new Color(1f, 0.82f, 0.24f), 44);
                StartCoroutine(FlashRoutine(new Color(1f, 0.85f, 0.24f, 0.42f)));
                shakeTime = 0.38f;
                PlayWinChord();
            }
            else
            {
                streak = 0;
                resultText.text = pickedAnswer ? "정답 선택은 맞았지만 룰렛이 " + landedValue + "에 멈췄습니다." : "실패. 룰렛은 " + landedValue + ", 정답은 " + currentRound.Answer + ".";
                explainText.text = currentRound.Explanation;
                BurstConfetti(new Color(0.35f, 0.78f, 1f), 18);
                StartCoroutine(FlashRoutine(new Color(0.35f, 0.6f, 1f, 0.22f)));
                shakeTime = 0.18f;
                PlayTone(145f, 0.18f, 0.28f);
            }

            if (coins < 50)
            {
                coins += 300;
                resultText.text += "  ·  재도전 보너스 +300";
            }
            spinning = false;
            nextButton.interactable = true;
            UpdateHud();
            UpdatePocketButtons();
        }

        private void UpdateHud()
        {
            coinsText.text = "코인 " + coins.ToString("N0");
            streakText.text = "연승 " + streak;
            bet = Mathf.Clamp(bet, 50, Mathf.Max(50, coins));
            betText.text = "베팅 " + bet + " 코인";
            if (betSlider != null)
            {
                betSlider.maxValue = Mathf.Max(50, Mathf.Min(500, coins));
                betSlider.SetValueWithoutNotify(Mathf.Clamp(bet, 50, betSlider.maxValue));
            }
        }

        private void UpdateWheelLabels()
        {
            float radius = 252f;
            float step = 360f / Mathf.Max(1, wheelValues.Count);
            for (int i = 0; i < wheelLabelTexts.Count; i++)
            {
                bool active = i < wheelValues.Count;
                wheelLabelTexts[i].gameObject.SetActive(active);
                if (!active)
                    continue;

                float angle = Mathf.Deg2Rad * (i * step + step * 0.5f);
                Vector2 position = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                wheelLabelTexts[i].rectTransform.anchoredPosition = position;
                wheelLabelTexts[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -(i * step + step * 0.5f));
                wheelLabelTexts[i].text = wheelValues[i].ToString();
                wheelLabelTexts[i].color = wheelValues[i] == currentRound.Answer ? new Color(1f, 0.98f, 0.62f) : Color.white;
            }
        }

        private void UpdatePocketButtons()
        {
            for (int i = 0; i < betButtons.Count; i++)
            {
                var colors = betButtons[i].colors;
                bool selected = i == selectedIndex;
                colors.normalColor = selected ? new Color(1f, 0.78f, 0.24f) : new Color(0.14f, 0.18f, 0.28f);
                colors.highlightedColor = selected ? new Color(1f, 0.9f, 0.42f) : new Color(0.2f, 0.28f, 0.42f);
                colors.pressedColor = new Color(0.96f, 0.54f, 0.18f);
                colors.disabledColor = new Color(0.12f, 0.13f, 0.16f, 0.7f);
                betButtons[i].colors = colors;
                betButtons[i].interactable = !spinning;
                betButtonTexts[i].text = wheelValues.Count > i ? wheelValues[i].ToString() : "?";
                betButtonTexts[i].color = selected ? new Color(0.06f, 0.04f, 0.02f) : Color.white;
                betButtonTexts[i].fontStyle = FontStyle.Bold;
            }
        }

        private void AnimateShake()
        {
            if (shakeTime <= 0f)
            {
                if (shakeLayer != null)
                    shakeLayer.anchoredPosition = Vector2.zero;
                return;
            }
            shakeTime -= Time.deltaTime;
            shakeLayer.anchoredPosition = UnityEngine.Random.insideUnitCircle * (shakeTime * 18f);
        }

        private void ApplyResponsiveScale()
        {
            if (contentRoot == null)
                return;

            Rect sourceRect = canvasRoot != null ? canvasRoot.rect : new Rect(0, 0, Screen.width, Screen.height);
            float widthScale = sourceRect.width / 1920f;
            float heightScale = sourceRect.height / 1080f;
            float scale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.72f, 1f);
            contentRoot.localScale = Vector3.one * scale;
        }

        private void AnimateConfetti()
        {
            for (int i = confetti.Count - 1; i >= 0; i--)
            {
                ConfettiPiece piece = confetti[i];
                piece.Age += Time.deltaTime;
                if (piece.Age >= piece.Life)
                {
                    Destroy(piece.Rect.gameObject);
                    confetti.RemoveAt(i);
                    continue;
                }
                float p = piece.Age / piece.Life;
                piece.Velocity += new Vector2(0, -840f) * Time.deltaTime;
                piece.Rect.anchoredPosition += piece.Velocity * Time.deltaTime;
                piece.Rect.Rotate(0, 0, piece.Spin * Time.deltaTime);
                piece.Image.color = Color.Lerp(piece.Color, new Color(piece.Color.r, piece.Color.g, piece.Color.b, 0), p);
                confetti[i] = piece;
            }
        }

        private IEnumerator FlashRoutine(Color color)
        {
            Image image = hitFlash.GetComponent<Image>();
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                float a = Mathf.Lerp(color.a, 0f, t / 0.5f);
                image.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }
            image.color = new Color(color.r, color.g, color.b, 0f);
        }

        private void BurstConfetti(Color baseColor, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var image = NewObject<Image>("Confetti", shakeLayer);
                image.color = Color.Lerp(baseColor, SegmentColors[random.Next(SegmentColors.Length)], UnityEngine.Random.value * 0.55f);
                RectTransform rect = image.rectTransform;
                rect.sizeDelta = new Vector2(UnityEngine.Random.Range(10, 24), UnityEngine.Random.Range(14, 34));
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(250, 20) + UnityEngine.Random.insideUnitCircle * 90f;
                rect.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360));
                confetti.Add(new ConfettiPiece
                {
                    Rect = rect,
                    Image = image,
                    Velocity = new Vector2(UnityEngine.Random.Range(-520f, 520f), UnityEngine.Random.Range(440f, 980f)),
                    Spin = UnityEngine.Random.Range(-720f, 720f),
                    Color = image.color,
                    Life = UnityEngine.Random.Range(0.85f, 1.45f)
                });
            }
        }

        private void Pulse(RectTransform rect, float scale)
        {
            StartCoroutine(PulseRoutine(rect, scale));
        }

        private IEnumerator PulseRoutine(RectTransform rect, float scale)
        {
            for (float t = 0; t < 0.12f; t += Time.deltaTime)
            {
                rect.localScale = Vector3.one * Mathf.Lerp(1f, scale, t / 0.12f);
                yield return null;
            }
            for (float t = 0; t < 0.12f; t += Time.deltaTime)
            {
                rect.localScale = Vector3.one * Mathf.Lerp(scale, 1f, t / 0.12f);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        private void PlayWinChord()
        {
            PlayTone(540f, 0.1f, 0.2f);
            StartCoroutine(DelayedTone(0.06f, 680f, 0.1f, 0.18f));
            StartCoroutine(DelayedTone(0.13f, 880f, 0.18f, 0.22f));
        }

        private IEnumerator DelayedTone(float delay, float frequency, float duration, float volume)
        {
            yield return new WaitForSeconds(delay);
            PlayTone(frequency, duration, volume);
        }

        private void PlayTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Clamp01(1f - t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }
            var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            audioSource.PlayOneShot(clip);
        }

        private static T NewObject<T>(string name, Transform parent) where T : Component
        {
            var go = typeof(Graphic).IsAssignableFrom(typeof(T))
                ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T))
                : new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private static RectTransform NewPanel(string name, Transform parent, RectSpec spec, Color color)
        {
            var image = NewObject<Image>(name, parent);
            image.color = color;
            image.raycastTarget = false;
            SetRect(image.rectTransform, spec);
            return image.rectTransform;
        }

        private static Text NewText(string name, Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var label = NewObject<Text>(name, parent);
            label.text = text;
            label.font = GetUiFont();
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(size * 0.62f));
            label.resizeTextMaxSize = size;
            label.alignment = anchor;
            label.alignByGeometry = true;
            label.lineSpacing = 0.88f;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            AddShadow(label.gameObject, new Color(0, 0, 0, 0.55f), new Vector2(0, -2));
            return label;
        }

        private static Font GetUiFont()
        {
            string[] preferredFonts =
            {
                "Pretendard",
                "Noto Sans KR",
                "Malgun Gothic",
                "맑은 고딕",
                "Segoe UI"
            };

            try
            {
                Font dynamicFont = Font.CreateDynamicFontFromOSFont(preferredFonts, 16);
                if (dynamicFont != null)
                    return dynamicFont;
            }
            catch (ArgumentException)
            {
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Button NewButton(string name, Transform parent, string text, int size, RectSpec spec)
        {
            var image = NewObject<Image>(name, parent);
            image.color = new Color(0.14f, 0.18f, 0.28f);
            SetRect(image.rectTransform, spec);
            AddOutline(image.gameObject, new Color(1f, 1f, 1f, 0.08f), new Vector2(0, -2));
            var button = image.gameObject.AddComponent<Button>();
            var label = NewText("Text", image.transform, text, size, TextAnchor.MiddleCenter, Color.white);
            SetRect(label.rectTransform, StretchInset(4));
            label.fontStyle = FontStyle.Bold;
            return button;
        }

        private static void StyleSlider(Slider slider)
        {
            var background = NewObject<Image>("Background", slider.transform);
            background.color = new Color(0.11f, 0.13f, 0.18f);
            SetRect(background.rectTransform, Stretch());
            var fillArea = NewPanel("Fill Area", slider.transform, StretchInset(4), new Color(0, 0, 0, 0));
            var fill = NewObject<Image>("Fill", fillArea);
            fill.color = new Color(1f, 0.74f, 0.22f);
            SetRect(fill.rectTransform, Stretch());
            var handleArea = NewPanel("Handle Slide Area", slider.transform, StretchInset(-16), new Color(0, 0, 0, 0));
            var handle = NewObject<Image>("Handle", handleArea);
            handle.color = new Color(1f, 0.92f, 0.45f);
            SetRect(handle.rectTransform, Anchored(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38)));
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
        }

        private static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static RectSpec Stretch() => new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        private static RectSpec StretchInset(float inset) => new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(inset, inset), new Vector2(-inset, -inset));
        private static RectSpec TopStretch(float left, float right, float top, float height) => new RectSpec(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(left, -top - height), new Vector2(-right, -top));
        private static RectSpec Box(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) => new RectSpec(anchorMin, anchorMax, new Vector2(0.5f, 0.5f), offsetMin, offsetMax);
        private static RectSpec Anchored(Vector2 anchor, Vector2 position, Vector2 size) => new RectSpec(anchor, anchor, new Vector2(0.5f, 0.5f), position, size);

        private static void SetRect(RectTransform rect, RectSpec spec)
        {
            rect.anchorMin = spec.AnchorMin;
            rect.anchorMax = spec.AnchorMax;
            rect.pivot = spec.Pivot;
            if (spec.AnchorMin == spec.AnchorMax)
            {
                rect.anchoredPosition = spec.PositionOrMin;
                rect.sizeDelta = spec.SizeOrMax;
            }
            else
            {
                rect.offsetMin = spec.PositionOrMin;
                rect.offsetMax = spec.SizeOrMax;
            }
        }

        private struct RectSpec
        {
            public readonly Vector2 AnchorMin;
            public readonly Vector2 AnchorMax;
            public readonly Vector2 Pivot;
            public readonly Vector2 PositionOrMin;
            public readonly Vector2 SizeOrMax;

            public RectSpec(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 positionOrMin, Vector2 sizeOrMax)
            {
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                Pivot = pivot;
                PositionOrMin = positionOrMin;
                SizeOrMax = sizeOrMax;
            }
        }

        private struct ConfettiPiece
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Spin;
            public Color Color;
            public float Age;
            public float Life;
        }
    }

    public sealed class WheelGraphic : Graphic
    {
        private readonly List<int> values = new List<int>();
        private Color[] segmentColors = new Color[0];

        public void SetValues(List<int> source, Color[] colors)
        {
            values.Clear();
            values.AddRange(source);
            segmentColors = colors;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (values.Count == 0)
                return;

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.48f;
            float inner = radius * 0.28f;
            int segments = values.Count;
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                Color c = segmentColors.Length > 0 ? segmentColors[i % segmentColors.Length] : Color.white;
                AddWedge(vh, center, inner, radius, i * step, (i + 1) * step, c);
            }
        }

        private static void AddWedge(VertexHelper vh, Vector2 center, float inner, float outer, float startDeg, float endDeg, Color color)
        {
            int slices = 10;
            int startIndex = vh.currentVertCount;
            for (int i = 0; i <= slices; i++)
            {
                float angle = Mathf.Deg2Rad * Mathf.Lerp(startDeg, endDeg, i / (float)slices);
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                vh.AddVert(center + dir * outer, color, Vector2.zero);
                vh.AddVert(center + dir * inner, Color.Lerp(color, Color.black, 0.28f), Vector2.zero);
            }
            for (int i = 0; i < slices; i++)
            {
                int a = startIndex + i * 2;
                vh.AddTriangle(a, a + 2, a + 1);
                vh.AddTriangle(a + 2, a + 3, a + 1);
            }
        }
    }

    public sealed class BackgroundGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            vh.AddVert(new Vector2(rect.xMin, rect.yMin), new Color(0.015f, 0.09f, 0.075f), Vector2.zero);
            vh.AddVert(new Vector2(rect.xMin, rect.yMax), new Color(0.04f, 0.06f, 0.12f), Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMax), new Color(0.12f, 0.05f, 0.12f), Vector2.zero);
            vh.AddVert(new Vector2(rect.xMax, rect.yMin), new Color(0.02f, 0.12f, 0.1f), Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }

    [Serializable]
    public sealed class SequenceRound
    {
        public string SequenceLine;
        public string Prompt;
        public string Hint;
        public string Explanation;
        public int Answer;
        public int Payout;
        private int difficulty;
        private int type;
        private int d;
        private int r;

        public static SequenceRound Create(System.Random random, int streakLevel)
        {
            int type = random.Next(0, 4);
            int difficulty = Mathf.Clamp(1 + streakLevel / 2, 1, 3);
            if (type == 0) return Arithmetic(random, difficulty);
            if (type == 1) return Geometric(random, difficulty);
            if (type == 2) return DifferenceSequence(random, difficulty);
            return Recursive(random, difficulty);
        }

        public int MakeDecoy(System.Random random)
        {
            int spread = 3 + difficulty * 3;
            int offset = random.Next(-spread, spread + 1);
            if (offset == 0)
                offset = difficulty + 1;
            if (type == 1 && random.NextDouble() < 0.5)
                return Answer + offset * Math.Max(1, Math.Abs(r));
            if (type == 0 && random.NextDouble() < 0.55)
                return Answer + offset * Math.Max(1, Math.Abs(d));
            return Answer + offset;
        }

        private static SequenceRound Arithmetic(System.Random random, int difficulty)
        {
            int a = random.Next(2, 12);
            int d = random.Next(2 + difficulty, 7 + difficulty * 2);
            int[] terms = { a, a + d, a + 2 * d, a + 3 * d };
            int answer = a + 4 * d;
            return new SequenceRound
            {
                type = 0,
                difficulty = difficulty,
                d = d,
                Answer = answer,
                Payout = 3 + difficulty,
                SequenceLine = JoinTerms(terms) + ", ?",
                Prompt = "등차수열의 공차를 읽고 다음 항에 베팅하세요.",
                Hint = "이웃한 두 항의 차가 일정합니다.",
                Explanation = "등차수열 an = a1 + (n-1)d. 여기서는 공차 d = " + d + "이므로 다음 항은 " + terms[3] + " + " + d + " = " + answer + "입니다."
            };
        }

        private static SequenceRound Geometric(System.Random random, int difficulty)
        {
            int a = random.Next(1, 5 + difficulty);
            int r = random.Next(2, 4 + difficulty);
            int[] terms = { a, a * r, a * r * r, a * r * r * r };
            int answer = terms[3] * r;
            return new SequenceRound
            {
                type = 1,
                difficulty = difficulty,
                r = r,
                Answer = answer,
                Payout = 4 + difficulty,
                SequenceLine = JoinTerms(terms) + ", ?",
                Prompt = "등비수열의 공비를 찾아 다음 항에 베팅하세요.",
                Hint = "앞 항에 같은 수를 곱하면 다음 항이 됩니다.",
                Explanation = "등비수열 an = a1 r^(n-1). 공비 r = " + r + "이므로 다음 항은 " + terms[3] + " x " + r + " = " + answer + "입니다."
            };
        }

        private static SequenceRound DifferenceSequence(System.Random random, int difficulty)
        {
            int a = random.Next(1, 8);
            int startDiff = random.Next(2, 5 + difficulty);
            int diffStep = random.Next(1, 3 + difficulty);
            int[] terms = new int[4];
            terms[0] = a;
            int diff = startDiff;
            for (int i = 1; i < terms.Length; i++)
            {
                terms[i] = terms[i - 1] + diff;
                diff += diffStep;
            }
            int answer = terms[3] + diff;
            return new SequenceRound
            {
                type = 2,
                difficulty = difficulty,
                Answer = answer,
                Payout = 5 + difficulty,
                SequenceLine = JoinTerms(terms) + ", ?",
                Prompt = "계차수열의 증가 규칙을 찾아 다음 항에 베팅하세요.",
                Hint = "항 사이의 차이도 다시 수열을 이룹니다.",
                Explanation = "계차는 " + startDiff + ", " + (startDiff + diffStep) + ", " + (startDiff + diffStep * 2) + "처럼 " + diffStep + "씩 증가합니다. 다음 계차는 " + diff + ", 따라서 " + terms[3] + " + " + diff + " = " + answer + "입니다."
            };
        }

        private static SequenceRound Recursive(System.Random random, int difficulty)
        {
            int x = random.Next(1, 6);
            int y = random.Next(2, 8);
            int add = random.Next(1, 3 + difficulty);
            int[] terms = new int[5];
            terms[0] = x;
            terms[1] = y;
            for (int i = 2; i < terms.Length; i++)
                terms[i] = terms[i - 1] + terms[i - 2] + add;
            return new SequenceRound
            {
                type = 3,
                difficulty = difficulty,
                Answer = terms[4],
                Payout = 6 + difficulty,
                SequenceLine = terms[0] + ", " + terms[1] + ", " + terms[2] + ", " + terms[3] + ", ?",
                Prompt = "점화식 an = an-1 + an-2 + c 형태를 읽어 베팅하세요.",
                Hint = "앞의 두 항을 더한 뒤 같은 상수를 더합니다.",
                Explanation = "각 항은 앞의 두 항 합에 " + add + "를 더합니다. 다음 항은 " + terms[3] + " + " + terms[2] + " + " + add + " = " + terms[4] + "입니다."
            };
        }

        private static string JoinTerms(int[] terms)
        {
            return string.Join(", ", Array.ConvertAll(terms, t => t.ToString()));
        }
    }
}
