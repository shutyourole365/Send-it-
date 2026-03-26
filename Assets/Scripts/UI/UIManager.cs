using UnityEngine;
using UnityEngine.UI;

namespace SendIt.UI
{
    /// <summary>
    /// Central manager for all UI screens and panels.
    /// Handles navigation between menus and HUD displays.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private Canvas mainCanvas;

        // UI Panels (will be created dynamically if not assigned)
        private GameObject mainMenuPanel;
        private GameObject garagePanel;
        private GameObject hudPanel;
        private GameObject pauseMenuPanel;

        private GameObject currentActivePanel;
        private bool isUIVisible = true;

        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize UI manager and create panels.
        /// </summary>
        public void Initialize()
        {
            // Find or create main canvas
            if (mainCanvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                mainCanvas = canvasObj.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create UI panels
            CreateMainMenuPanel();
            CreateGaragePanel();
            CreateHUDPanel();
            CreatePauseMenuPanel();

            Debug.Log("UIManager initialized");
        }

        /// <summary>
        /// Create main menu panel.
        /// </summary>
        private void CreateMainMenuPanel()
        {
            mainMenuPanel = new GameObject("MainMenuPanel");
            mainMenuPanel.transform.SetParent(mainCanvas.transform, false);
            mainMenuPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(mainMenuPanel.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "SEND IT";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 80;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 200);
            titleRect.sizeDelta = new Vector2(800, 200);

            // Menu buttons layout
            CreateMenuButton(mainMenuPanel, "Start Game", 0, () => ShowGameModeSelection());
            CreateMenuButton(mainMenuPanel, "Customize Vehicle", -100, () => ShowGarage());
            CreateMenuButton(mainMenuPanel, "Settings", -200, () => Debug.Log("Settings clicked"));
            CreateMenuButton(mainMenuPanel, "Quit", -300, () => Application.Quit());

            mainMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Create garage customization panel.
        /// </summary>
        private void CreateGaragePanel()
        {
            garagePanel = new GameObject("GaragePanel");
            garagePanel.transform.SetParent(mainCanvas.transform, false);
            garagePanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(garagePanel.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "GARAGE - Vehicle Customization";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 40;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(-800, 450);
            titleRect.sizeDelta = new Vector2(1600, 80);

            // Customization info
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(garagePanel.transform, false);
            Text infoText = infoObj.AddComponent<Text>();
            infoText.text = "Customize your vehicle:\n• Engine & Powertrain\n• Suspension & Handling\n• Paint & Visual Mods\n• Audio & Effects";
            infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            infoText.fontSize = 20;
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.color = Color.white;
            RectTransform infoRect = infoObj.GetComponent<RectTransform>();
            infoRect.anchoredPosition = Vector2.zero;
            infoRect.sizeDelta = new Vector2(1000, 400);

            // Buttons
            CreateMenuButton(garagePanel, "Physics Tuning", 100, () => Debug.Log("Physics tuning"));
            CreateMenuButton(garagePanel, "Graphics Customization", 0, () => Debug.Log("Graphics customization"));
            CreateMenuButton(garagePanel, "Back to Menu", -100, () => ShowMainMenu());

            garagePanel.SetActive(false);
        }

        /// <summary>
        /// Create in-game HUD panel.
        /// </summary>
        private void CreateHUDPanel()
        {
            hudPanel = new GameObject("HUDPanel");
            hudPanel.transform.SetParent(mainCanvas.transform, false);

            // Speed display (top left)
            GameObject speedObj = new GameObject("SpeedDisplay");
            speedObj.transform.SetParent(hudPanel.transform, false);
            Text speedText = speedObj.AddComponent<Text>();
            speedText.text = "0 km/h";
            speedText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            speedText.fontSize = 32;
            speedText.fontStyle = FontStyle.Bold;
            speedText.alignment = TextAnchor.UpperLeft;
            speedText.color = Color.white;
            RectTransform speedRect = speedObj.GetComponent<RectTransform>();
            speedRect.anchoredPosition = new Vector2(50, -50);
            speedRect.sizeDelta = new Vector2(300, 100);

            // RPM display
            GameObject rpmObj = new GameObject("RPMDisplay");
            rpmObj.transform.SetParent(hudPanel.transform, false);
            Text rpmText = rpmObj.AddComponent<Text>();
            rpmText.text = "RPM: 1000";
            rpmText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            rpmText.fontSize = 24;
            rpmText.alignment = TextAnchor.UpperLeft;
            rpmText.color = Color.white;
            RectTransform rpmRect = rpmObj.GetComponent<RectTransform>();
            rpmRect.anchoredPosition = new Vector2(50, -120);
            rpmRect.sizeDelta = new Vector2(300, 80);

            // Timer (top center)
            GameObject timerObj = new GameObject("Timer");
            timerObj.transform.SetParent(hudPanel.transform, false);
            Text timerText = timerObj.AddComponent<Text>();
            timerText.text = "00:00";
            timerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            timerText.fontSize = 48;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.UpperCenter;
            timerText.color = Color.white;
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchoredPosition = new Vector2(0, -50);
            timerRect.sizeDelta = new Vector2(300, 100);

            // Mode stats (bottom right)
            GameObject statsObj = new GameObject("ModeStats");
            statsObj.transform.SetParent(hudPanel.transform, false);
            Text statsText = statsObj.AddComponent<Text>();
            statsText.text = "Free Roam";
            statsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            statsText.fontSize = 20;
            statsText.alignment = TextAnchor.LowerRight;
            statsText.color = Color.cyan;
            RectTransform statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchoredPosition = new Vector2(-50, 50);
            statsRect.sizeDelta = new Vector2(400, 150);

            // Help text
            GameObject helpObj = new GameObject("HelpText");
            helpObj.transform.SetParent(hudPanel.transform, false);
            Text helpText = helpObj.AddComponent<Text>();
            helpText.text = "ESC: Pause | TAB: Toggle HUD";
            helpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            helpText.fontSize = 16;
            helpText.alignment = TextAnchor.LowerLeft;
            helpText.color = Color.gray;
            RectTransform helpRect = helpObj.GetComponent<RectTransform>();
            helpRect.anchoredPosition = new Vector2(50, 50);
            helpRect.sizeDelta = new Vector2(500, 80);

            hudPanel.SetActive(false);
        }

        /// <summary>
        /// Create pause menu panel.
        /// </summary>
        private void CreatePauseMenuPanel()
        {
            pauseMenuPanel = new GameObject("PauseMenuPanel");
            pauseMenuPanel.transform.SetParent(mainCanvas.transform, false);
            pauseMenuPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(pauseMenuPanel.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "PAUSED";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 60;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 100);
            titleRect.sizeDelta = new Vector2(800, 150);

            // Buttons
            CreateMenuButton(pauseMenuPanel, "Resume", 0, () => ResumeGame());
            CreateMenuButton(pauseMenuPanel, "Return to Garage", -100, () => ReturnToGarage());
            CreateMenuButton(pauseMenuPanel, "Main Menu", -200, () => ReturnToMainMenu());

            pauseMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Helper to create menu buttons.
        /// </summary>
        private void CreateMenuButton(GameObject parent, string text, float yOffset, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(text);
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 24;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(0, yOffset);
            buttonRect.sizeDelta = new Vector2(300, 70);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = buttonRect.sizeDelta;
        }

        /// <summary>
        /// Show main menu.
        /// </summary>
        public void ShowMainMenu()
        {
            HideAllPanels();
            mainMenuPanel.SetActive(true);
            currentActivePanel = mainMenuPanel;
        }

        /// <summary>
        /// Show game mode selection.
        /// </summary>
        public void ShowGameModeSelection()
        {
            // For now, just start free roam
            GameplayManager.Instance.StartGameSession(Gameplay.GameplayManager.GameMode.FreeRoam);
            ShowHUD();
        }

        /// <summary>
        /// Show garage customization.
        /// </summary>
        public void ShowGarage()
        {
            HideAllPanels();
            garagePanel.SetActive(true);
            currentActivePanel = garagePanel;
            GameplayManager.Instance.SetGameState(Gameplay.GameplayManager.GameState.Garage);
        }

        /// <summary>
        /// Show in-game HUD.
        /// </summary>
        public void ShowHUD()
        {
            HideAllPanels();
            hudPanel.SetActive(true);
            currentActivePanel = hudPanel;
        }

        /// <summary>
        /// Show pause menu.
        /// </summary>
        public void ShowPauseMenu()
        {
            pauseMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Hide pause menu.
        /// </summary>
        public void HidePauseMenu()
        {
            pauseMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Resume game from pause.
        /// </summary>
        public void ResumeGame()
        {
            HidePauseMenu();
            GameplayManager.Instance.TogglePause();
        }

        /// <summary>
        /// Return to garage.
        /// </summary>
        public void ReturnToGarage()
        {
            HideAllPanels();
            GameplayManager.Instance.ReturnToGarage();
            ShowGarage();
        }

        /// <summary>
        /// Return to main menu.
        /// </summary>
        public void ReturnToMainMenu()
        {
            HideAllPanels();
            GameplayManager.Instance.ReturnToMainMenu();
            ShowMainMenu();
        }

        /// <summary>
        /// Toggle HUD visibility.
        /// </summary>
        public void ToggleHUD()
        {
            isUIVisible = !isUIVisible;
            hudPanel.SetActive(isUIVisible);
        }

        /// <summary>
        /// Hide all panels.
        /// </summary>
        private void HideAllPanels()
        {
            mainMenuPanel.SetActive(false);
            garagePanel.SetActive(false);
            hudPanel.SetActive(false);
            pauseMenuPanel.SetActive(false);
        }

        public static void GoBack()
        {
            // Generic back button functionality
            if (Instance != null && Instance.currentActivePanel == Instance.garagePanel)
            {
                Instance.ShowMainMenu();
            }
        }
    }
}
