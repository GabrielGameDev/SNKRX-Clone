using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

namespace SNKRX.Editor
{
    public static class SetupUpgradeUIInScene
    {
        [MenuItem("Tools/SNKRX/Setup Upgrade UI in Scene")]
        public static void SetupUI()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[SetupUpgradeUIInScene] Nenhuma cena ativa carregada!");
                return;
            }

            // 1. Remove qualquer componente indevido de UpgradeSelectionUI no UpgradeManager
            UpgradeManager upgradeMgr = Object.FindAnyObjectByType<UpgradeManager>();
            if (upgradeMgr != null)
            {
                UpgradeSelectionUI misplaced = upgradeMgr.GetComponent<UpgradeSelectionUI>();
                if (misplaced != null)
                {
                    Undo.DestroyObjectImmediate(misplaced);
                }
            }

            // 2. Localiza o Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[SetupUpgradeUIInScene] Canvas não encontrado na cena!");
                return;
            }

            // 3. Remove se já houver um UpgradeSelectionUI existente para recriar limpo
            Transform existingUi = canvas.transform.Find("UpgradeSelectionUI");
            if (existingUi != null)
            {
                Undo.DestroyObjectImmediate(existingUi.gameObject);
            }

            // Também procura em outros objetos da cena
            UpgradeSelectionUI existingInScene = Object.FindAnyObjectByType<UpgradeSelectionUI>();
            if (existingInScene != null)
            {
                Undo.DestroyObjectImmediate(existingInScene.gameObject);
            }

            // 4. Carrega fonte do TextMeshPro
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (fontAsset == null)
            {
                fontAsset = TMP_Settings.defaultFontAsset;
            }

            // 5. Cria objeto raiz UpgradeSelectionUI sob o Canvas
            GameObject uiRoot = new GameObject("UpgradeSelectionUI", typeof(RectTransform), typeof(UpgradeSelectionUI));
            Undo.RegisterCreatedObjectUndo(uiRoot, "Create UpgradeSelectionUI");
            uiRoot.transform.SetParent(canvas.transform, false);

            RectTransform uiRootRt = uiRoot.GetComponent<RectTransform>();
            uiRootRt.anchorMin = Vector2.zero;
            uiRootRt.anchorMax = Vector2.one;
            uiRootRt.offsetMin = Vector2.zero;
            uiRootRt.offsetMax = Vector2.zero;

            UpgradeSelectionUI upgradeSelectionUI = uiRoot.GetComponent<UpgradeSelectionUI>();

            // 6. Cria ModalRoot (overlay escuro cobrindo a tela)
            GameObject modalRoot = new GameObject("ModalRoot", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(modalRoot, "Create ModalRoot");
            modalRoot.transform.SetParent(uiRoot.transform, false);

            RectTransform modalRootRt = modalRoot.GetComponent<RectTransform>();
            modalRootRt.anchorMin = Vector2.zero;
            modalRootRt.anchorMax = Vector2.one;
            modalRootRt.offsetMin = Vector2.zero;
            modalRootRt.offsetMax = Vector2.zero;

            Image modalBg = modalRoot.GetComponent<Image>();
            modalBg.color = new Color(0.04f, 0.04f, 0.08f, 0.92f);
            modalBg.raycastTarget = true;

            // 7. Cria ContentPanel centralizado
            GameObject contentPanel = new GameObject("ContentPanel", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(contentPanel, "Create ContentPanel");
            contentPanel.transform.SetParent(modalRoot.transform, false);

            RectTransform panelRt = contentPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(1080f, 680f);

            // 8. Cria HeaderTitle
            GameObject headerGo = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(headerGo, "Create HeaderTitle");
            headerGo.transform.SetParent(contentPanel.transform, false);

            RectTransform headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchoredPosition = new Vector2(0f, 280f);
            headerRt.sizeDelta = new Vector2(900f, 64f);

            TextMeshProUGUI headerText = headerGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) headerText.font = fontAsset;
            headerText.text = "LEVEL UP!";
            headerText.fontSize = 42;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = new Color(1f, 0.85f, 0.2f); // Ouro
            headerText.alignment = TextAlignmentOptions.Center;

            // 9. Cria Subtitle
            GameObject subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(subGo, "Create Subtitle");
            subGo.transform.SetParent(contentPanel.transform, false);

            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0f, 220f);
            subRt.sizeDelta = new Vector2(900f, 45f);

            TextMeshProUGUI subtitleText = subGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) subtitleText.font = fontAsset;
            subtitleText.text = "Choose an upgrade for your snake:";
            subtitleText.fontSize = 24;
            subtitleText.color = new Color(0.85f, 0.85f, 0.9f, 0.8f);
            subtitleText.alignment = TextAlignmentOptions.Center;

            // 10. Cria CardsContainer com HorizontalLayoutGroup
            GameObject containerGo = new GameObject("CardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(containerGo, "Create CardsContainer");
            containerGo.transform.SetParent(contentPanel.transform, false);

            RectTransform contRt = containerGo.GetComponent<RectTransform>();
            contRt.anchoredPosition = new Vector2(0f, -65f);
            contRt.sizeDelta = new Vector2(1024f, 500f);

            HorizontalLayoutGroup hlg = containerGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 11. Cria os 3 cards
            string[] sampleColors = { "#FF4545", "#3498DB", "#9B59B6" };
            string[] sampleNames = { "Red", "Blue", "Purple" };
            UpgradeCardUI[] cards = new UpgradeCardUI[3];
            for (int i = 0; i < 3; i++)
            {
                cards[i] = CreateCard(containerGo.transform, i, fontAsset, sampleColors[i], sampleNames[i]);
            }

            // 12. Atribui os campos serializados no UpgradeSelectionUI
            SerializedObject soUI = new SerializedObject(upgradeSelectionUI);
            soUI.FindProperty("modalRoot").objectReferenceValue = modalRoot;
            soUI.FindProperty("headerText").objectReferenceValue = headerText;
            soUI.FindProperty("subtitleText").objectReferenceValue = subtitleText;
            soUI.FindProperty("cardsContainer").objectReferenceValue = containerGo.transform;

            SerializedProperty cardSlotsProp = soUI.FindProperty("cardSlots");
            cardSlotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                cardSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }
            soUI.ApplyModifiedProperties();

            // 13. Conecta a referência no UIManager
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                SerializedObject soUIMgr = new SerializedObject(uiManager);
                SerializedProperty upUiProp = soUIMgr.FindProperty("upgradeSelectionUI");
                if (upUiProp != null)
                {
                    upUiProp.objectReferenceValue = upgradeSelectionUI;
                    soUIMgr.ApplyModifiedProperties();
                }
            }

            // Deixa ModalRoot ativo na cena do Editor para facilitar a edição visual pelo usuário
            modalRoot.SetActive(true);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SetupUpgradeUIInScene] UpgradeSelectionUI criado e configurado com sucesso na cena!");
        }

        private static UpgradeCardUI CreateCard(Transform parent, int index, TMP_FontAsset fontAsset, string hexColor, string colorName)
        {
            GameObject cardGo = new GameObject($"Card_{index}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UpgradeCardUI));
            Undo.RegisterCreatedObjectUndo(cardGo, $"Create Card_{index}");
            cardGo.transform.SetParent(parent, false);

            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(320f, 480f);

            Image bgImage = cardGo.GetComponent<Image>();
            bgImage.color = new Color(0.96f, 0.95f, 0.91f, 1f);

            Button btn = cardGo.GetComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            UpgradeCardUI card = cardGo.GetComponent<UpgradeCardUI>();

            // Borda
            GameObject borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(cardGo.transform, false);
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;
            Image borderImg = borderGo.GetComponent<Image>();
            ColorUtility.TryParseHtmlString(hexColor, out Color col);
            borderImg.color = new Color(col.r, col.g, col.b, 0.4f);
            borderImg.raycastTarget = false;

            // ColorOrb
            GameObject orbGo = new GameObject("ColorOrb", typeof(RectTransform), typeof(Image));
            orbGo.transform.SetParent(cardGo.transform, false);
            RectTransform orbRt = orbGo.GetComponent<RectTransform>();
            orbRt.anchoredPosition = new Vector2(0f, 135f);
            orbRt.sizeDelta = new Vector2(84f, 84f);
            Image orbImg = orbGo.GetComponent<Image>();
            orbImg.sprite = UpgradeCardUI.GetOrCreateCircleSprite();
            orbImg.color = col;
            orbImg.raycastTarget = false;

            // ColorName
            GameObject colorNameGo = new GameObject("ColorName", typeof(RectTransform), typeof(TextMeshProUGUI));
            colorNameGo.transform.SetParent(cardGo.transform, false);
            RectTransform cnRt = colorNameGo.GetComponent<RectTransform>();
            cnRt.anchoredPosition = new Vector2(0f, 77f);
            cnRt.sizeDelta = new Vector2(280f, 34f);
            TextMeshProUGUI cnText = colorNameGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) cnText.font = fontAsset;
            cnText.text = $"{colorName} Orb";
            cnText.color = col;
            cnText.fontSize = 22;
            cnText.fontStyle = FontStyles.Bold;
            cnText.alignment = TextAlignmentOptions.Center;
            cnText.raycastTarget = false;

            // Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(cardGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 27f);
            titleRt.sizeDelta = new Vector2(290f, 60f);
            TextMeshProUGUI titleText = titleGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleText.font = fontAsset;
            titleText.text = "? ? ?";
            titleText.color = Color.black;
            titleText.fontSize = 25;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.raycastTarget = false;

            // Description
            GameObject descGo = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(cardGo.transform, false);
            RectTransform descRt = descGo.GetComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(0f, -88f);
            descRt.sizeDelta = new Vector2(280f, 190f);
            TextMeshProUGUI descText = descGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) descText.font = fontAsset;
            descText.text = $"Mysterious Effect!\n\nChoose this upgrade to reveal its power for this run and add a {colorName.ToLower()} segment to the snake.";
            descText.fontSize = 18;
            descText.lineSpacing = 3f;
            descText.color = Color.black;
            descText.alignment = TextAlignmentOptions.Center;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.raycastTarget = false;

            // StatusBadge
            GameObject badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeGo.transform.SetParent(cardGo.transform, false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(0f, -205f);
            badgeRt.sizeDelta = new Vector2(260f, 32f);
            TextMeshProUGUI badgeText = badgeGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) badgeText.font = fontAsset;
            badgeText.text = "UNKNOWN";
            badgeText.fontSize = 16;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.color = Color.black;
            badgeText.raycastTarget = false;

            // Serializa no UpgradeCardUI
            SerializedObject soCard = new SerializedObject(card);
            soCard.FindProperty("selectButton").objectReferenceValue = btn;
            soCard.FindProperty("cardBorder").objectReferenceValue = borderImg;
            soCard.FindProperty("colorOrb").objectReferenceValue = orbImg;
            soCard.FindProperty("colorNameText").objectReferenceValue = cnText;
            soCard.FindProperty("titleText").objectReferenceValue = titleText;
            soCard.FindProperty("descriptionText").objectReferenceValue = descText;
            soCard.FindProperty("statusBadgeText").objectReferenceValue = badgeText;
            soCard.ApplyModifiedProperties();

            return card;
        }
    }
}
