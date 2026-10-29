using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    /// <summary>
    /// Tools > First Jobber > Build Game Scene.
    /// Generates Assets/Scenes/Game.unity with the Canvas, all panels, the ChoiceButton prefab, the managers
    /// and every Inspector reference already assigned. Placeholder UI only - restyle freely afterwards.
    /// </summary>
    public static class GameSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Game.unity";
        const string PrefabFolder = "Assets/Prefabs";
        const string PrefabPath = "Assets/Prefabs/ChoiceButton.prefab";
        const string DatabasePath = "Assets/Data/GameDatabase.asset";

        static readonly Color Bg = new Color(0.07f, 0.07f, 0.09f, 1f);
        static readonly Color Card = new Color(0.13f, 0.13f, 0.17f, 1f);
        static readonly Color Paper = new Color(0.93f, 0.9f, 0.8f, 1f);
        static readonly Color Ink = new Color(0.1f, 0.1f, 0.12f, 1f);
        static readonly Color Accent = new Color(0.98f, 0.82f, 0.35f, 1f);
        static readonly Color ButtonGreen = new Color(0.2f, 0.45f, 0.3f, 1f);
        static readonly Color ButtonGrey = new Color(0.28f, 0.28f, 0.33f, 1f);

        [MenuItem("Tools/First Jobber/2. Build Game Scene")]
        public static void Build()
        {
            if (TMPro.TMP_Settings.instance == null || TMPro.TMP_Settings.defaultFontAsset == null)
            {
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                EditorUtility.DisplayDialog("First Jobber",
                    "TextMeshPro Essential Resources are not imported yet.\n\nImport them in the dialog that just opened, then run Tools > First Jobber > 2. Build Game Scene again.",
                    "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath) == null) SampleDataGenerator.Generate();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("First Jobber", ScenePath + " already exists and will be replaced.", "Replace", "Cancel")) return;

            EnsureFolder(PrefabFolder);
            var choicePrefab = BuildChoicePrefab();
            if (choicePrefab == null) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupCamera();
            BuildEventSystem();

            // ---------- canvas ----------
            var canvasGo = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRt = (RectTransform)canvasGo.transform;

            var backdrop = NewRect("Background", canvasRt);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = Bg;
            backdrop.GetComponent<Image>().raycastTarget = false;
            backdrop.gameObject.AddComponent<ScrollingBackground>();
            MovingBackgroundTools.Configure(backdrop.gameObject);

            // ---------- panels ----------
            var mainMenuPanel = BuildMainMenu(canvasRt, out var mainMenu);
            var createPanel = BuildCreateApplicant(canvasRt, out var createApplicant);
            var resumePanel = BuildResumeEditor(canvasRt, choicePrefab, out var resumeEditor);
            Set(resumeEditor, "introStartFrom", mainMenu.PaperRect); // the editor zooms out from the menu paper
            var companyPanel = BuildCompany(canvasRt, choicePrefab, out var companyController);
            var interviewPanel = BuildInterview(canvasRt, choicePrefab, out var interview, out var hr);
            var eventPanel = BuildDailyEvent(canvasRt, out var eventPanelUI, out var eventChoices);
            var finalPanel = BuildFinalDecision(canvasRt, choicePrefab, out var finalDecision);
            var endingPanel = BuildEnding(canvasRt, out var endingController);

            // ---------- dialogue + overlay (always on top) ----------
            var dialogueUI = BuildDialogueBox(canvasRt);
            var overlay = BuildOverlay(canvasRt, out var overlayText);

            // ---------- systems ----------
            var systems = new GameObject("GameSystems");
            var dialogue = systems.AddComponent<DialogueManager>();
            Set(dialogue, "ui", dialogueUI);

            var ui = systems.AddComponent<UIManager>();
            Set(ui, "mainMenu", mainMenuPanel);
            Set(ui, "createApplicant", createPanel);
            Set(ui, "resumeEditor", resumePanel);
            Set(ui, "company", companyPanel);
            Set(ui, "interview", interviewPanel);
            Set(ui, "dailyEvent", eventPanel);
            Set(ui, "finalDecision", finalPanel);
            Set(ui, "ending", endingPanel);
            Set(ui, "overlay", overlay);
            Set(ui, "overlayText", overlayText);

            var events = systems.AddComponent<DailyEventManager>();
            Set(events, "database", db);
            Set(events, "dialogue", dialogue);
            Set(events, "eventPanel", eventPanelUI);
            Set(events, "choiceList", eventChoices);

            Set(hr, "dialogue", dialogue);

            var flow = systems.AddComponent<GameFlowManager>();
            Set(flow, "database", db);
            Set(flow, "dialogue", dialogue);
            Set(flow, "ui", ui);
            Set(flow, "dailyEvents", events);
            Set(flow, "mainMenu", mainMenu);
            Set(flow, "createApplicant", createApplicant);
            Set(flow, "resumeEditor", resumeEditor);
            Set(flow, "companySelection", companyController);
            Set(flow, "interview", interview);
            Set(flow, "finalDecision", finalDecision);
            Set(flow, "ending", endingController);

            AtmosphereTools.AddToOpenScene();
            UiPhaseTools.BindCommon(canvasRt);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log("[First Jobber] Scene built: " + ScenePath + ". Press Play.");
        }

        // ============================================================ panels

        static CanvasGroup BuildMainMenu(RectTransform canvas, out MainMenuController controller)
        {
            var panel = NewPanel(canvas, "MainMenuPanel");
            controller = panel.gameObject.AddComponent<MainMenuController>();
            PopulateMainMenu(panel, controller);
            return panel.GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Main menu drawn as a big resume paper (name + gender + one Submit button). Also used by
        /// Tools > First Jobber > 9. Convert Main Menu To Resume Style to rebuild the menu of an existing scene.
        /// </summary>
        internal static void PopulateMainMenu(RectTransform panel, MainMenuController controller)
        {
            var paper = NewRect("MenuPaper", panel);
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0.53f);
            paper.pivot = new Vector2(0.5f, 0.5f);
            paper.sizeDelta = new Vector2(950f, 860f);
            paper.anchoredPosition = Vector2.zero;
            paper.gameObject.AddComponent<Image>().color = Paper;

            NewText(paper, "Title", "FIRST JOBBER", 92, Ink, TextAlignmentOptions.Center, FontStyles.Bold, 0.05f, 0.8f, 0.95f, 0.95f);
            NewText(paper, "Subtitle", "Every rejection takes something with it.", 30, new Color(0.35f, 0.35f, 0.35f),
                TextAlignmentOptions.Center, FontStyles.Italic, 0.05f, 0.72f, 0.95f, 0.8f);
            NewText(paper, "NameLabel", "Name", 32, new Color(0.3f, 0.3f, 0.3f), TextAlignmentOptions.BottomLeft, FontStyles.Normal, 0.12f, 0.63f, 0.88f, 0.69f);
            var nameInput = NewInput(paper, "NameInput", "Enter your name...", 0.12f, 0.53f, 0.88f, 0.63f);
            NewText(paper, "GenderLabel", "Gender", 32, new Color(0.3f, 0.3f, 0.3f), TextAlignmentOptions.BottomLeft, FontStyles.Normal, 0.12f, 0.46f, 0.88f, 0.52f);
            var genderDropdown = NewDropdown(paper, "GenderDropdown", 0.12f, 0.36f, 0.88f, 0.46f);
            var error = NewText(paper, "ErrorText", "", 28, new Color(0.75f, 0.15f, 0.15f), TextAlignmentOptions.Center, FontStyles.Normal, 0.1f, 0.27f, 0.9f, 0.34f);
            var submit = NewButton(paper, "SubmitButton", "Submit", ButtonGreen, 0.3f, 0.1f, 0.7f, 0.24f, 44);

            Set(controller, "paperRect", paper);
            Set(controller, "nameInput", nameInput);
            Set(controller, "genderDropdown", genderDropdown);
            Set(controller, "errorText", error);
            Set(controller, "submitButton", submit);
            AddLanguageSelector(panel);
        }

        /// <summary>
        /// English / Thai selector in the top-right corner of the main menu panel. Only forwards to LocalizationManager
        /// (see LanguageDropdown). Safe to call twice: nothing is added when the panel already has one.
        /// </summary>
        internal static LanguageDropdown AddLanguageSelector(RectTransform panel)
        {
            var existing = panel.GetComponentInChildren<LanguageDropdown>(true);
            if (existing != null) return existing;
            var dropdown = NewDropdown(panel, "LanguageDropdown", 0.80f, 0.925f, 0.97f, 0.985f);
            dropdown.ClearOptions();
            dropdown.captionText.fontSize = 30;
            dropdown.itemText.fontSize = 28;
            var lang = dropdown.gameObject.AddComponent<LanguageDropdown>();
            Set(lang, "dropdown", dropdown);
            return lang;
        }

        static CanvasGroup BuildCreateApplicant(RectTransform canvas, out CreateApplicantController controller)
        {
            var panel = NewPanel(canvas, "CreateApplicantPanel");
            NewText(panel, "Title", "Create Applicant", 72, Accent, TextAlignmentOptions.Center, FontStyles.Bold, 0.2f, 0.8f, 0.8f, 0.95f);
            NewText(panel, "NameLabel", "Name", 34, Color.white, TextAlignmentOptions.BottomLeft, FontStyles.Normal, 0.3f, 0.7f, 0.7f, 0.76f);
            var nameInput = NewInput(panel, "NameInput", "Enter your name...", 0.3f, 0.6f, 0.7f, 0.7f);
            NewText(panel, "GenderLabel", "Gender", 34, Color.white, TextAlignmentOptions.BottomLeft, FontStyles.Normal, 0.3f, 0.5f, 0.7f, 0.56f);
            var genderDropdown = NewDropdown(panel, "GenderDropdown", 0.3f, 0.4f, 0.7f, 0.5f);
            var error = NewText(panel, "ErrorText", "", 30, new Color(1f, 0.4f, 0.4f), TextAlignmentOptions.Center, FontStyles.Normal, 0.3f, 0.32f, 0.7f, 0.39f);
            var submit = NewButton(panel, "SubmitButton", "Confirm", ButtonGreen, 0.4f, 0.28f, 0.6f, 0.37f);
            // move the button below the error text
            Anchor((RectTransform)submit.transform, 0.4f, 0.27f, 0.6f, 0.35f);
            Anchor((RectTransform)error.transform, 0.3f, 0.35f, 0.7f, 0.4f);

            controller = panel.gameObject.AddComponent<CreateApplicantController>();
            Set(controller, "nameInput", nameInput);
            Set(controller, "genderDropdown", genderDropdown);
            Set(controller, "errorText", error);
            Set(controller, "submitButton", submit);
            return panel.GetComponent<CanvasGroup>();
        }

        static CanvasGroup BuildResumeEditor(RectTransform canvas, ChoiceButtonUI prefab, out ResumeEditorController controller)
        {
            var panel = NewPanel(canvas, "ResumeEditorPanel");

            // LEFT: resume preview (paper) with category rows
            var previewRt = NewRect("ResumePreview", panel);
            Anchor(previewRt, 0.03f, 0.28f, 0.46f, 0.97f);
            previewRt.gameObject.AddComponent<Image>().color = Paper;
            var nameText = NewText(previewRt, "NameText", "Name", 56, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, 0.05f, 0.86f, 0.95f, 0.97f);
            var genderText = NewText(previewRt, "GenderText", "Gender", 32, new Color(0.3f, 0.3f, 0.3f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal, 0.05f, 0.79f, 0.95f, 0.86f);
            var categoryList = NewChoiceList(previewRt, "CategoryList", SectionButton("ResumeCategoryButton"), new Color(0, 0, 0, 0), 0.04f, 0.04f, 0.96f, 0.77f);
            var previewUI = previewRt.gameObject.AddComponent<ResumePreviewUI>();
            Set(previewUI, "nameText", nameText);
            Set(previewUI, "genderText", genderText);
            Set(previewUI, "categoryList", categoryList);

            // RIGHT: editor
            var editorRt = NewRect("EditorGroup", panel);
            Anchor(editorRt, 0.5f, 0.28f, 0.86f, 0.97f); // right of this column: the confidence bar
            var editorGroup = editorRt.gameObject.AddComponent<CanvasGroup>();
            var dayText = NewText(editorRt, "DayText", "Day 1", 36, Color.gray, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, 0f, 0.93f, 0.4f, 1f);
            var title = NewText(editorRt, "CategoryTitle", "Category", 46, Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, 0f, 0.84f, 1f, 0.93f);
            var choiceList = NewChoiceList(editorRt, "ChoiceList", SectionButton("ResumeChoiceButton"), Card, 0f, 0.12f, 1f, 0.83f);
            var confirm = NewButton(editorRt, "ConfirmButton", "Confirm Resume", ButtonGreen, 0.55f, 0f, 1f, 0.1f);

            var confidenceUI = ConfidenceTools.BuildConfidencePanel(panel);

            controller = panel.gameObject.AddComponent<ResumeEditorController>();
            Set(controller, "confidenceUI", confidenceUI);
            Set(controller, "preview", previewUI);
            Set(controller, "previewRect", previewRt);
            Set(controller, "editorGroup", editorGroup);
            Set(controller, "dayText", dayText);
            Set(controller, "categoryTitleText", title);
            Set(controller, "choiceList", choiceList);
            Set(controller, "confirmButton", confirm);
            return panel.GetComponent<CanvasGroup>();
        }

        static CanvasGroup BuildCompany(RectTransform canvas, ChoiceButtonUI prefab, out CompanySelectionController controller)
        {
            var panel = NewPanel(canvas, "CompanyPanel");
            NewText(panel, "Header", "Choose a company", 44, Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, 0.03f, 0.92f, 0.58f, 0.99f);
            var list = NewChoiceList(panel, "CompanyList", SectionButton("CompanySelectionButton"), Card, 0.03f, 0.28f, 0.58f, 0.92f);

            var details = NewRect("DetailsCard", panel);
            Anchor(details, 0.62f, 0.4f, 0.97f, 0.97f);
            details.gameObject.AddComponent<Image>().color = Card;
            var detailsText = NewText(details, "DetailsText", "Select a company to see its details.", 34, Color.white,
                TextAlignmentOptions.TopLeft, FontStyles.Normal, 0.05f, 0.05f, 0.95f, 0.95f);
            var viewport = NewRect("DescriptionViewport", details);
            Anchor(viewport, .05f, .05f, .95f, .95f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("DescriptionContent", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var descriptionLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            descriptionLayout.childControlWidth = descriptionLayout.childControlHeight = true;
            descriptionLayout.childForceExpandWidth = true; descriptionLayout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            detailsText.rectTransform.SetParent(content, false);
            var descriptionScroll = details.gameObject.AddComponent<ScrollRect>();
            descriptionScroll.viewport = viewport; descriptionScroll.content = content;
            descriptionScroll.horizontal = false; descriptionScroll.vertical = true;
            descriptionScroll.movementType = ScrollRect.MovementType.Clamped;
            descriptionScroll.scrollSensitivity = 40;

            var back = NewButton(panel, "BackButton", "Back", ButtonGrey, 0.62f, 0.28f, 0.78f, 0.37f);
            var apply = NewButton(panel, "ApplyButton", "Apply", ButtonGreen, 0.8f, 0.28f, 0.97f, 0.37f);

            controller = panel.gameObject.AddComponent<CompanySelectionController>();
            Set(controller, "companyList", list);
            Set(controller, "detailsText", detailsText);
            Set(controller, "detailsScroll", descriptionScroll);
            Set(controller, "detailsContent", content);
            Set(controller, "applyButton", apply);
            Set(controller, "backButton", back);
            return panel.GetComponent<CanvasGroup>();
        }

        static CanvasGroup BuildInterview(RectTransform canvas, ChoiceButtonUI prefab, out InterviewController controller, out HRReactionController hr)
        {
            var panel = NewPanel(canvas, "InterviewPanel");

            // LEFT
            var questionCard = NewRect("QuestionCard", panel);
            Anchor(questionCard, 0.03f, 0.62f, 0.55f, 0.97f);
            questionCard.gameObject.AddComponent<Image>().color = Card;
            var questionText = NewText(questionCard, "QuestionText", "", 40, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, 0.04f, 0.05f, 0.96f, 0.95f);
            var answers = NewChoiceList(panel, "AnswerList", SectionButton("InterviewAnswerButton"), Card, 0.03f, 0.28f, 0.55f, 0.6f);

            // RIGHT: HR
            var hrArea = NewRect("HRArea", panel);
            Anchor(hrArea, 0.58f, 0.28f, 0.97f, 0.97f);
            hrArea.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.05f);
            hrArea.gameObject.AddComponent<RectMask2D>();

            var portraitRt = NewRect("HRPortrait", hrArea);
            Anchor(portraitRt, 0.2f, 0.02f, 0.8f, 0.72f);
            var portraitImg = portraitRt.gameObject.AddComponent<Image>();
            portraitImg.color = new Color(0.35f, 0.35f, 0.4f);
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            var label = NewText(portraitRt, "PlaceholderLabel", "Neutral", 40, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);

            var fan = NewRect("CeilingFan", hrArea);
            fan.anchorMin = fan.anchorMax = new Vector2(0.5f, 0.86f);
            fan.sizeDelta = new Vector2(260, 260);
            fan.anchoredPosition = Vector2.zero;
            for (int i = 0; i < 2; i++)
            {
                var blade = NewRect("Blade" + i, fan);
                blade.anchorMin = blade.anchorMax = new Vector2(0.5f, 0.5f);
                blade.sizeDelta = new Vector2(260, 18);
                blade.localRotation = Quaternion.Euler(0, 0, i * 90f);
                var bi = blade.gameObject.AddComponent<Image>();
                bi.color = new Color(0.25f, 0.25f, 0.28f);
                bi.raycastTarget = false;
            }
            var hub = NewRect("Hub", fan);
            hub.anchorMin = hub.anchorMax = new Vector2(0.5f, 0.5f);
            hub.sizeDelta = new Vector2(40, 40);
            var hubImg = hub.gameObject.AddComponent<Image>();
            hubImg.color = new Color(0.4f, 0.4f, 0.45f);
            hubImg.raycastTarget = false;

            var flicker = NewRect("Flicker", hrArea);
            Stretch(flicker);
            var flickerImg = flicker.gameObject.AddComponent<Image>();
            flickerImg.color = new Color(1, 1, 1, 0);
            flickerImg.raycastTarget = false;

            hr = hrArea.gameObject.AddComponent<HRReactionController>();
            Set(hr, "portrait", portraitImg);
            Set(hr, "placeholderLabel", label);
            Set(hr, "swayTarget", portraitRt);
            Set(hr, "fan", fan);
            Set(hr, "flickerOverlay", flickerImg);
            SetEmotionVisuals(hr);

            controller = panel.gameObject.AddComponent<InterviewController>();
            Set(controller, "questionText", questionText);
            Set(controller, "answerList", answers);
            Set(controller, "hr", hr);
            return panel.GetComponent<CanvasGroup>();
        }

        static CanvasGroup BuildDailyEvent(RectTransform canvas, out DailyEventPanelUI ui, out ChoiceListUI choices)
        {
            var panel = NewPanel(canvas, "DailyEventPanel");
            var bg = NewRect("EventBackground", panel);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.04f, 0.06f);
            bgImg.raycastTarget = false;
            var caption = NewText(panel, "Caption", "", 100, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0.1f, 0.5f, 0.9f, 0.85f);

            ui = panel.gameObject.AddComponent<DailyEventPanelUI>();
            Set(ui, "background", bgImg);
            Set(ui, "captionText", caption);
            choices = NewChoiceList(panel, "DailyEventChoices", SectionButton("EventChoiceButton"), Card, .2f, .28f, .8f, .83f);
            choices.gameObject.SetActive(false);
            return panel.GetComponent<CanvasGroup>();
        }

        static CanvasGroup BuildFinalDecision(RectTransform canvas, ChoiceButtonUI prefab, out FinalDecisionController controller)
        {
            var panel = NewPanel(canvas, "FinalDecisionPanel");
            var title = NewText(panel, "Title", "", 60, Accent, TextAlignmentOptions.Center, FontStyles.Bold, 0.15f, 0.85f, 0.85f, 0.97f);
            var list = NewChoiceList(panel, "EndingList", SectionButton("EndingChoiceButton"), Card, 0.2f, 0.28f, 0.8f, 0.83f);

            controller = panel.gameObject.AddComponent<FinalDecisionController>();
            Set(controller, "titleText", title);
            Set(controller, "endingList", list);
            Set(controller, "actionButtonPrefab", SectionButton("EndingActionButton"));
            return panel.GetComponent<CanvasGroup>();
        }

        static CanvasGroup BuildEnding(RectTransform canvas, out EndingController controller)
        {
            var panel = NewPanel(canvas, "EndingPanel");
            var bg = NewRect("EndingBackground", panel);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = false;
            var title = NewText(panel, "Title", "", 84, Accent, TextAlignmentOptions.Center, FontStyles.Bold, 0.1f, 0.78f, 0.9f, 0.96f);
            var final = NewText(panel, "FinalText", "", 44, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0.1f, 0.45f, 0.9f, 0.76f);

            controller = panel.gameObject.AddComponent<EndingController>();
            var director = panel.gameObject.AddComponent<UnityEngine.Playables.PlayableDirector>();
            director.playOnAwake = false;
            Set(controller, "director", director);
            var audio = panel.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            Set(controller, "background", bgImg);
            Set(controller, "titleText", title);
            Set(controller, "finalText", final);
            Set(controller, "audioSource", audio);
            var cutsceneRoot = NewRect("CutsceneRoot", panel);
            Stretch(cutsceneRoot); cutsceneRoot.SetSiblingIndex(1); cutsceneRoot.gameObject.SetActive(false);
            var videoRoot = NewRect("FullscreenVideo", panel);
            Stretch(videoRoot);
            videoRoot.gameObject.AddComponent<Image>().color = Color.black;
            var videoRect = NewRect("VideoImage", videoRoot);
            Stretch(videoRect);
            var videoImage = videoRect.gameObject.AddComponent<RawImage>(); videoImage.raycastTarget = false;
            var videoAspect = videoRect.gameObject.AddComponent<AspectRatioFitter>();
            videoAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; videoAspect.aspectRatio = 16f / 9f;
            videoRoot.gameObject.SetActive(false);
            var fade = NewRect("PresentationFade", panel);
            Stretch(fade); fade.gameObject.AddComponent<Image>().color = Color.black;
            var fadeGroup = fade.gameObject.AddComponent<CanvasGroup>(); fadeGroup.alpha = 0; fadeGroup.blocksRaycasts = false;
            Set(controller, "cutsceneRoot", cutsceneRoot);
            Set(controller, "videoRoot", videoRoot.gameObject);
            Set(controller, "videoImage", videoImage);
            Set(controller, "videoAspect", videoAspect);
            Set(controller, "fadeOverlay", fadeGroup);
            return panel.GetComponent<CanvasGroup>();
        }

        static DialogueUI BuildDialogueBox(RectTransform canvas)
        {
            var box = NewRect("DialogueBox", canvas);
            Anchor(box, 0.03f, 0.025f, 0.97f, 0.25f);
            box.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.96f);
            var group = box.gameObject.AddComponent<CanvasGroup>();

            var click = NewRect("ClickArea", box);
            Stretch(click);
            var clickImg = click.gameObject.AddComponent<Image>();
            clickImg.color = new Color(0, 0, 0, 0);
            var clickBtn = click.gameObject.AddComponent<Button>();
            clickBtn.transition = Selectable.Transition.None;

            var speaker = NewText(box, "SpeakerText", "Speaker", 34, Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, 0.02f, 0.72f, 0.6f, 0.97f);
            var body = NewText(box, "BodyText", "", 36, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal, 0.02f, 0.08f, 0.97f, 0.72f);
            var indicator = NewText(box, "ContinueIndicator", ">>", 34, Accent, TextAlignmentOptions.BottomRight, FontStyles.Bold, 0.9f, 0.02f, 0.99f, 0.3f);

            var ui = box.gameObject.AddComponent<DialogueUI>();
            Set(ui, "group", group);
            Set(ui, "speakerText", speaker);
            Set(ui, "bodyText", body);
            Set(ui, "continueIndicator", indicator.gameObject);
            Set(ui, "clickArea", clickBtn);
            return ui;
        }

        static CanvasGroup BuildOverlay(RectTransform canvas, out TMP_Text text)
        {
            var rt = NewRect("TransitionOverlay", canvas);
            Stretch(rt);
            rt.gameObject.AddComponent<Image>().color = Color.black;
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            text = NewText(rt, "OverlayText", "", 80, Color.white, TextAlignmentOptions.Center, FontStyles.Normal, 0.1f, 0.4f, 0.9f, 0.6f);
            return group;
        }

        // ============================================================ prefab + shared widgets

        static ChoiceButtonUI SectionButton(string role)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/" + role + ".prefab");
            var view = asset != null ? asset.GetComponent<ChoiceButtonUI>() : null;
            if (view == null) throw new System.InvalidOperationException("Missing section button: " + role + ". Repair its existing prefab reference.");
            return view;
        }

        static ChoiceButtonUI BuildChoicePrefab()
        {
            // Scene rebuilding must not overwrite the artist-authored shared prefab.
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                var view = existing.GetComponent<ChoiceButtonUI>();
                if (view == null) Debug.LogError("[SceneBuilder] Existing ChoiceButton is missing ChoiceButtonUI. Repair it in place.", existing);
                return view;
            }
            var root = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(ChoiceButtonUI));
            var bg = root.GetComponent<Image>();
            bg.color = new Color(0.88f, 0.88f, 0.84f);
            root.GetComponent<Button>().targetGraphic = bg;
            root.GetComponent<LayoutElement>().minHeight = 76;

            var h = root.GetComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(16, 16, 10, 10);
            h.spacing = 14;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            var icon = NewRect("Icon", (RectTransform)root.transform);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var iconLe = icon.gameObject.AddComponent<LayoutElement>();
            iconLe.minWidth = 64; iconLe.preferredWidth = 64; iconLe.minHeight = 64; iconLe.preferredHeight = 64;
            icon.gameObject.SetActive(false);

            var texts = NewRect("Texts", (RectTransform)root.transform);
            var v = texts.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            texts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var title = NewTextFlow(texts, "Title", 32, Ink, FontStyles.Bold);
            var subtitle = NewTextFlow(texts, "Subtitle", 24, new Color(0.3f, 0.3f, 0.3f), FontStyles.Normal);

            var badge = ConfidenceTools.AddBadge((RectTransform)root.transform);

            var comp = root.GetComponent<ChoiceButtonUI>();
            Set(comp, "badgeText", badge);
            Set(comp, "button", root.GetComponent<Button>());
            Set(comp, "background", bg);
            Set(comp, "titleText", title);
            Set(comp, "subtitleText", subtitle);
            Set(comp, "iconImage", iconImg);

            ConfidenceStarTools.ConfigurePrefabRoot(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(PrefabPath);
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var result = saved != null ? saved.GetComponent<ChoiceButtonUI>() : null;
            if (result == null) Debug.LogError("[SceneBuilder] ChoiceButton prefab has no ChoiceButtonUI component. Fix script compile errors, then run the builder again.");
            return result;
        }

        static ChoiceListUI NewChoiceList(RectTransform parent, string name, ChoiceButtonUI prefab, Color bgColor,
            float x0, float y0, float x1, float y1)
        {
            var root = NewRect(name, parent);
            Anchor(root, x0, y0, x1, y1);
            root.gameObject.AddComponent<Image>().color = bgColor;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var scroll = root.gameObject.AddComponent<ScrollRect>();

            var viewport = NewRect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.004f);

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 12, 12);
            v.spacing = 10;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // scrollbar
            var sbRt = NewRect("Scrollbar", root);
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = new Vector2(1, 1);
            sbRt.pivot = new Vector2(1, 0.5f);
            sbRt.sizeDelta = new Vector2(14, 0);
            sbRt.anchoredPosition = Vector2.zero;
            sbRt.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);
            var sb = sbRt.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            var area = NewRect("SlidingArea", sbRt);
            Stretch(area);
            var handle = NewRect("Handle", area);
            Stretch(handle);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = new Color(0.75f, 0.75f, 0.75f, 0.9f);
            sb.handleRect = handle;
            sb.targetGraphic = handleImg;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = -3f;

            var empty = NewText(root, "EmptyText", "", 34, new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center, FontStyles.Italic, 0.05f, 0.3f, 0.95f, 0.7f);
            empty.gameObject.SetActive(false);

            var list = root.gameObject.AddComponent<ChoiceListUI>();
            Set(list, "scrollRect", scroll);
            Set(list, "content", content);
            Set(list, "itemPrefab", prefab);
            Set(list, "emptyState", empty.gameObject);
            Set(list, "emptyText", empty);
            Set(list, "canvasGroup", group);
            return list;
        }

        static void SetEmotionVisuals(HRReactionController hr)
        {
            var so = new SerializedObject(hr);
            var list = so.FindProperty("visuals");
            var entries = new (HREmotion e, Color c)[]
            {
                (HREmotion.Neutral, new Color(0.35f, 0.35f, 0.4f)),
                (HREmotion.Interested, new Color(0.3f, 0.5f, 0.4f)),
                (HREmotion.Disappointed, new Color(0.5f, 0.3f, 0.3f)),
                (HREmotion.Uncomfortable, new Color(0.5f, 0.45f, 0.25f)),
            };
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var el = list.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("emotion").enumValueIndex = (int)entries[i].e;
                el.FindPropertyRelative("tint").colorValue = entries[i].c;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ============================================================ scene plumbing

        static void SetupCamera()
        {
            var light = GameObject.Find("Directional Light");
            if (light != null) Object.DestroyImmediate(light);
            var cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        // ============================================================ tiny UI toolkit

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static RectTransform NewPanel(RectTransform parent, string name)
        {
            var rt = NewRect(name, parent);
            Stretch(rt);
            rt.gameObject.AddComponent<CanvasGroup>();
            return rt;
        }

        internal static TextMeshProUGUI NewTextFlow(RectTransform parent, string name, float size, Color color, FontStyles style)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = TMP_Settings.defaultFontAsset;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        internal static TextMeshProUGUI NewText(RectTransform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align, FontStyles style, float x0, float y0, float x1, float y1)
        {
            var t = NewTextFlow(parent, name, size, color, style);
            t.text = text;
            t.alignment = align;
            Anchor((RectTransform)t.transform, x0, y0, x1, y1);
            return t;
        }

        static Button NewButton(RectTransform parent, string name, string label, Color color, float x0, float y0, float x1, float y1, float fontSize = 36)
        {
            var rt = NewRect(name, parent);
            Anchor(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            NewText(rt, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 0f, 0f, 1f, 1f);
            return btn;
        }

        /// <summary>TMP dropdown with built-in Unity UI sprites. Options are filled at runtime from GameDatabase.genders.</summary>
        internal static TMP_Dropdown NewDropdown(RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            Sprite Builtin(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
            var resources = new TMP_DefaultControls.Resources
            {
                standard = Builtin("UI/Skin/UISprite.psd"),
                background = Builtin("UI/Skin/Background.psd"),
                inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
                knob = Builtin("UI/Skin/Knob.psd"),
                checkmark = Builtin("UI/Skin/Checkmark.psd"),
                dropdown = Builtin("UI/Skin/DropdownArrow.psd"),
                mask = Builtin("UI/Skin/UIMask.psd"),
            };
            var go = TMP_DefaultControls.CreateDropdown(resources);
            go.name = name;
            go.transform.SetParent(parent, false);
            Anchor((RectTransform)go.transform, x0, y0, x1, y1);
            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.captionText.fontSize = 34;
            dropdown.itemText.fontSize = 30;
            dropdown.AddOptions(new System.Collections.Generic.List<string> { "Select gender..." });
            return dropdown;
        }

        static TMP_InputField NewInput(RectTransform parent, string name, string placeholder, float x0, float y0, float x1, float y1)
        {
            var rt = NewRect(name, parent);
            Anchor(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 0.92f);
            var input = rt.gameObject.AddComponent<TMP_InputField>();

            var area = NewRect("Text Area", rt);
            Stretch(area);
            area.offsetMin = new Vector2(14, 6);
            area.offsetMax = new Vector2(-14, -6);
            area.gameObject.AddComponent<RectMask2D>();

            var ph = NewText(area, "Placeholder", placeholder, 34, new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.MidlineLeft, FontStyles.Italic, 0, 0, 1, 1);
            var tx = NewText(area, "Text", "", 34, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, 0, 0, 1, 1);

            input.textViewport = area;
            input.textComponent = tx;
            input.placeholder = ph;
            input.targetGraphic = img;
            return input;
        }

        /// <summary>Assigns a private [SerializeField] field by name.</summary>
        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError("[SceneBuilder] " + target.GetType().Name + " has no serialized field '" + field + "'.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
