using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class StickyNoteBehavior : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// 現在選択中のアイテム（同じアイテムを再選択しないための静的参照）
    /// </summary>
    private static StickyNoteBehavior currentSelectedItem;
    private Coroutine generateRoutine;

    [System.Serializable]
    public class TypeSettings
    {
        [Header("Prefabs")]
        [Tooltip("RSのPrefabをアサインします")]
        public GameObject RSPrefab;

        [Tooltip("RSItemMassのPrefabをアサインします")]
        public GameObject RSItemMassPrefab;

        [Header("Parents")]
        [Tooltip("RSItemMassを配置する親GameObject（RSItemMasses）をアサインします")]
        public GameObject RSItemMasses;

        [Header("Sprite")]
        [Tooltip("このタイプのスプライト画像をアサインします")]
        public Sprite sprite;
    }

    [Header("Type Settings")]
    [Tooltip("Normalタイプの設定")]
    [SerializeField] private TypeSettings normalSettings = new TypeSettings();

    [Tooltip("Pickaxeタイプの設定")]
    [SerializeField] private TypeSettings pickaxeSettings = new TypeSettings();

    [Tooltip("Gravityタイプの設定")]
    [SerializeField] private TypeSettings gravitySettings = new TypeSettings();

    [Header("Selection")]
    [Tooltip("マウスホバー時に表示するSelectionオブジェクトをアサインします（タイプに関係なく同じオブジェクトを使用）")]
    [SerializeField] private GameObject selection;
    
    [Tooltip("選択時に表示するSelection_Backオブジェクトをアサインします（タイプに関係なく同じオブジェクトを使用）")]
    [SerializeField] private GameObject selectionBack;
    
    [Header("Item Info")]
    [Tooltip("アイテムの幅（W）")]
    [SerializeField] private int itemWidth = 0;
    
    [Tooltip("アイテムの高さ（H）")]
    [SerializeField] private int itemHeight = 0;

    [Header("Grid Settings")]
    [Tooltip("グリッド全体のスケール（デフォルト0.5倍）")]
    [SerializeField] private float gridScale = 0.5f;
    
    [Header("UI Elements")]
    [Tooltip("左クリックアイコン（自動検索でアサインされます）")]
    private GameObject leftClickIcon;
    
    [Tooltip("左クリックテキスト（自動検索でアサインされます）")]
    private TextMeshProUGUI leftClickText;
    
    [Tooltip("右クリックアイコン（自動検索でアサインされます）")]
    private GameObject rightClickIcon;
    
    [Tooltip("右クリックテキスト（自動検索でアサインされます）")]
    private TextMeshProUGUI rightClickText;
    
    [Tooltip("マウスホイールアイコン（自動検索でアサインされます）")]
    private GameObject mouseWheelIcon;
    
    [Tooltip("マウスホイールテキスト（自動検索でアサインされます）")]
    private TextMeshProUGUI mouseWheelText;

    [Header("UI Text Settings")]
    [Tooltip("何も選択していない状態の左クリックテキスト")]
    [SerializeField] private string noSelectionLeftClickText = "アイテム選択";
    
    [Tooltip("何も選択していない状態の右クリックテキスト")]
    [SerializeField] private string noSelectionRightClickText = "ー";
    
    [Tooltip("何も選択していない状態のマウスホイールテキスト")]
    [SerializeField] private string noSelectionMouseWheelText = "ー";

    [Header("Character Animator")]
    [Tooltip("CharacterAnimatorコンポーネントをアサインします（自動検索も可能）")]
    [SerializeField] private CharacterAnimator characterAnimator;
    
    // 論理サイズ（グリッド上のサイズ）を保持。未設定(0,0)の場合はtransform.localScaleを使用（互換性のため）
    private Vector2Int logicalSize = Vector2Int.zero;
    
    // このアイテムのインデックス（StickyNotesGeneratorで設定される）
    [SerializeField] private int itemIndex = -1;

    // このアイテムのタイプ（StickyNotesGeneratorで設定される）
    private StageDatabase.RSItemType itemType = StageDatabase.RSItemType.Normal;

    private void Start()
    {
        // 初期状態ではSelectionとSelection_Backを非表示にする
        if (selection != null)
        {
            selection.SetActive(false);
        }
        
        if (selectionBack != null)
        {
            selectionBack.SetActive(false);
        }

        // UI要素を自動検索でアサイン
        FindAndAssignUIElements();

        // UIの初期状態を設定（何も選択していない状態）
        UpdateUIForNoSelection();

        // CharacterAnimatorを自動検索（アサインされていない場合）
        if (characterAnimator == null)
        {
            characterAnimator = CharacterAnimator.Instance;
        }

        // タイプに応じてスプライトを設定
        ApplyTypeSettings();

        // RSItemMassを生成
        GenerateItemMasses();
    }

    /// <summary>
    /// UI要素を自動検索でアサインします
    /// </summary>
    private void FindAndAssignUIElements()
    {
        // まず子要素から検索
        Transform[] children = GetComponentsInChildren<Transform>(true);
        
        foreach (Transform child in children)
        {
            if (child.name == "LeftClickIcon" && leftClickIcon == null)
            {
                leftClickIcon = child.gameObject;
            }
            else if (child.name == "LeftClickText" && leftClickText == null)
            {
                leftClickText = child.GetComponent<TextMeshProUGUI>();
            }
            else if (child.name == "RightClickIcon" && rightClickIcon == null)
            {
                rightClickIcon = child.gameObject;
            }
            else if (child.name == "RightClickText" && rightClickText == null)
            {
                rightClickText = child.GetComponent<TextMeshProUGUI>();
            }
            else if ((child.name == "MauseWheelIcon" || child.name == "MouseWheelIcon") && mouseWheelIcon == null)
            {
                mouseWheelIcon = child.gameObject;
            }
            else if ((child.name == "MauseWheelText" || child.name == "MouseWheelText") && mouseWheelText == null)
            {
                mouseWheelText = child.GetComponent<TextMeshProUGUI>();
            }
        }
        
        // 子要素で見つからなかった場合は、Canvas配下を検索
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;
            
            // Canvas配下のすべての子要素を再帰的に検索
            SearchInChildren(canvas.transform);
        }
        
        // まだ見つからなかった場合は、シーン全体から検索（非アクティブも含む）
        if (leftClickIcon == null)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "LeftClickIcon")
                {
                    leftClickIcon = obj;
                    break;
                }
            }
        }
        
        if (leftClickText == null)
        {
            TextMeshProUGUI[] allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in allTMPs)
            {
                if (tmp.name == "LeftClickText")
                {
                    leftClickText = tmp;
                    break;
                }
            }
        }
        
        if (rightClickIcon == null)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "RightClickIcon")
                {
                    rightClickIcon = obj;
                    break;
                }
            }
        }
        
        if (rightClickText == null)
        {
            TextMeshProUGUI[] allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in allTMPs)
            {
                if (tmp.name == "RightClickText")
                {
                    rightClickText = tmp;
                    break;
                }
            }
        }
        
        if (mouseWheelIcon == null)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "MauseWheelIcon" || obj.name == "MouseWheelIcon")
                {
                    mouseWheelIcon = obj;
                    break;
                }
            }
        }
        
        if (mouseWheelText == null)
        {
            TextMeshProUGUI[] allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in allTMPs)
            {
                if (tmp.name == "MauseWheelText" || tmp.name == "MouseWheelText")
                {
                    mouseWheelText = tmp;
                    break;
                }
            }
        }
        
        // 見つからなかった場合は警告
        if (leftClickIcon == null) Debug.LogWarning("LeftClickIconが見つかりませんでした");
        if (leftClickText == null) Debug.LogWarning("LeftClickTextが見つかりませんでした");
        if (rightClickIcon == null) Debug.LogWarning("RightClickIconが見つかりませんでした");
        if (rightClickText == null) Debug.LogWarning("RightClickTextが見つかりませんでした");
        if (mouseWheelIcon == null) Debug.LogWarning("MauseWheelIcon/MouseWheelIconが見つかりませんでした");
        if (mouseWheelText == null) Debug.LogWarning("MauseWheelText/MouseWheelTextが見つかりませんでした");
    }

    /// <summary>
    /// 指定されたTransform配下の子要素を再帰的に検索します
    /// </summary>
    private void SearchInChildren(Transform parent)
    {
        if (parent == null) return;
        
        // このTransformのGameObjectをチェック
        if (parent.name == "LeftClickIcon" && leftClickIcon == null)
        {
            leftClickIcon = parent.gameObject;
        }
        else if (parent.name == "LeftClickText" && leftClickText == null)
        {
            leftClickText = parent.GetComponent<TextMeshProUGUI>();
        }
        else if (parent.name == "RightClickIcon" && rightClickIcon == null)
        {
            rightClickIcon = parent.gameObject;
        }
        else if (parent.name == "RightClickText" && rightClickText == null)
        {
            rightClickText = parent.GetComponent<TextMeshProUGUI>();
        }
        else if ((parent.name == "MauseWheelIcon" || parent.name == "MouseWheelIcon") && mouseWheelIcon == null)
        {
            mouseWheelIcon = parent.gameObject;
        }
        else if ((parent.name == "MauseWheelText" || parent.name == "MouseWheelText") && mouseWheelText == null)
        {
            mouseWheelText = parent.GetComponent<TextMeshProUGUI>();
        }
        
        // 子要素を再帰的に検索
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
            {
                SearchInChildren(child);
            }
        }
    }

    /// <summary>
    /// 何も選択していない状態のUIを更新します
    /// </summary>
    private void UpdateUIForNoSelection()
    {
        // LeftClickIconとLeftClickTextのみを非アクティブにする
        if (leftClickIcon != null) leftClickIcon.SetActive(false);
        if (leftClickText != null) leftClickText.gameObject.SetActive(false);
        // RightClickIcon、RightClickText、MauseWheelIcon、MauseWheelTextは常に表示

        // LeftClickIconとLeftClickTextのY座標を0に設定
        if (leftClickIcon != null)
        {
            RectTransform iconRect = leftClickIcon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                Vector2 pos = iconRect.anchoredPosition;
                pos.y = 0f;
                iconRect.anchoredPosition = pos;
            }
        }
        
        if (leftClickText != null)
        {
            RectTransform textRect = leftClickText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                Vector2 pos = textRect.anchoredPosition;
                pos.y = 0f;
                textRect.anchoredPosition = pos;
            }
        }

        // LeftClickIconとLeftClickTextのみを表示
        if (leftClickIcon != null)
        {
            leftClickIcon.SetActive(true);
        }
        
        if (leftClickText != null)
        {
            leftClickText.text = noSelectionLeftClickText;
            leftClickText.gameObject.SetActive(true);
        }
        
        // RightClickTextとMouseWheelTextもテキストを設定（常に表示）
        if (rightClickText != null)
        {
            rightClickText.text = noSelectionRightClickText;
            rightClickText.gameObject.SetActive(true);
        }
        
        if (mouseWheelText != null)
        {
            mouseWheelText.text = noSelectionMouseWheelText;
            mouseWheelText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 選択時のUIを更新します
    /// </summary>
    private void UpdateUIForSelection()
    {
        // LeftClickIconとLeftClickTextのY座標を0に設定
        if (leftClickIcon != null)
        {
            RectTransform iconRect = leftClickIcon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                Vector2 pos = iconRect.anchoredPosition;
                pos.y = 0f;
                iconRect.anchoredPosition = pos;
            }
        }
        
        if (leftClickText != null)
        {
            RectTransform textRect = leftClickText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                Vector2 pos = textRect.anchoredPosition;
                pos.y = 0f;
                textRect.anchoredPosition = pos;
            }
        }

        // すべてのUIを表示
        if (leftClickIcon != null)
        {
            leftClickIcon.SetActive(true);
        }
        
        if (leftClickText != null)
        {
            leftClickText.gameObject.SetActive(true);
        }
        
        if (rightClickIcon != null)
        {
            rightClickIcon.SetActive(true);
        }
        
        if (rightClickText != null)
        {
            rightClickText.gameObject.SetActive(true);
        }
        
        if (mouseWheelIcon != null)
        {
            mouseWheelIcon.SetActive(true);
        }
        
        if (mouseWheelText != null)
        {
            mouseWheelText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// タイプに応じた設定を適用します
    /// </summary>
    private void ApplyTypeSettings()
    {
        TypeSettings settings = GetTypeSettings(itemType);
        if (settings == null) return;

        // スプライトを設定
        if (settings.sprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = settings.sprite;
            }
        }
    }

    /// <summary>
    /// タイプに応じた設定を取得します
    /// </summary>
    private TypeSettings GetTypeSettings(StageDatabase.RSItemType type)
    {
        switch (type)
        {
            case StageDatabase.RSItemType.Normal:
                return normalSettings;
            case StageDatabase.RSItemType.Pickaxe:
                return pickaxeSettings;
            case StageDatabase.RSItemType.Gravity:
                return gravitySettings;
            default:
                return normalSettings;
        }
    }

    /// <summary>
    /// ポインター押下時の処理（新しいInputSystem対応）
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // 左クリック以外（右クリックなど）は無視
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // すでにこのアイテムが選択されている場合は何もしない
        if (currentSelectedItem == this)
        {
            return;
        }

        // RSの操作より前にParent配下を全破棄
        ClearRSParentChildren();

        // 以前選択されていたアイテムのSelection_Backを非表示にする
        if (currentSelectedItem != null && currentSelectedItem != this)
        {
            if (currentSelectedItem.selectionBack != null)
            {
                currentSelectedItem.selectionBack.SetActive(false);
            }
            // 以前選択されていたアイテムのUIを非選択状態に戻す
            currentSelectedItem.UpdateUIForNoSelection();
        }

        // 他のアイテムを選択した場合は、このアイテムを現在の選択として更新
        currentSelectedItem = this;

        // このアイテムのSelection_Backを表示
        if (selectionBack != null)
        {
            selectionBack.SetActive(true);
        }

        // 選択時のUIを更新
        UpdateUIForSelection();

        // タイプに応じてアニメーションを設定
        if (characterAnimator != null)
        {
            switch (itemType)
            {
                case StageDatabase.RSItemType.Normal:
                    characterAnimator.SetNSelect();
                    break;
                case StageDatabase.RSItemType.Pickaxe:
                    characterAnimator.SetPSelect();
                    break;
                case StageDatabase.RSItemType.Gravity:
                    characterAnimator.SetGSelect();
                    break;
            }
        }

        // 既存の生成待ちを止め、即時生成
        if (generateRoutine != null)
        {
            StopCoroutine(generateRoutine);
            generateRoutine = null;
        }
        GenerateRS();
    }

    /// <summary>
    /// マウスホバー開始時に呼び出されます
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selection != null)
        {
            selection.SetActive(true);
        }
    }

    /// <summary>
    /// マウスホバー終了時に呼び出されます
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (selection != null)
        {
            selection.SetActive(false);
        }
    }

    /// <summary>
    /// 指定のアイテムが選択中なら選択状態を解除します（キャンセル時など）
    /// </summary>
    public static void ClearCurrentSelection(StickyNoteBehavior item)
    {
        if (currentSelectedItem == item)
        {
            currentSelectedItem = null;
            // 選択解除時にSelection_Backを非表示にする
            if (item != null && item.selectionBack != null)
            {
                item.selectionBack.SetActive(false);
            }
            // 選択解除時にUIを非選択状態に戻す
            if (item != null)
            {
                item.UpdateUIForNoSelection();
            }
            // 選択解除時にアニメーションをIdleに戻す
            if (item != null && item.characterAnimator != null)
            {
                item.characterAnimator.SetIdle();
            }
        }
    }

    /// <summary>
    /// すべてのStickyNoteBehaviorのUIを非選択状態に更新します
    /// </summary>
    public static void UpdateAllStickyNotesUI()
    {
        StickyNoteBehavior[] allItems = Object.FindObjectsByType<StickyNoteBehavior>(FindObjectsSortMode.None);
        foreach (var stickyNote in allItems)
        {
            if (stickyNote != null && stickyNote != currentSelectedItem)
            {
                stickyNote.UpdateUIForNoSelection();
            }
        }
    }

    /// <summary>
    /// RSを生成します（RSParentを親として配置）
    /// </summary>
    private void GenerateRS()
    {
        TypeSettings settings = GetTypeSettings(itemType);
        if (settings == null)
        {
            Debug.LogWarning("タイプ設定が見つかりません");
            return;
        }

        if (settings.RSPrefab == null)
        {
            Debug.LogWarning("RSPrefabがアサインされていません");
            return;
        }

        // 既存のRS、RSP、またはRSGがあれば、選択をキャンセルしてから新しいものを生成（常に1つだけにする）
        var existingSelector = Object.FindFirstObjectByType<RSBehavior>();
        if (existingSelector != null)
        {
            existingSelector.CancelSelection();
        }
        var existingRSPSelector = Object.FindFirstObjectByType<RSPBehavior>();
        if (existingRSPSelector != null)
        {
            existingRSPSelector.CancelSelection();
        }
        var existingRSGSelector = Object.FindFirstObjectByType<RSGBehavior>();
        if (existingRSGSelector != null)
        {
            existingRSGSelector.CancelSelection();
        }

        // RSParentまたはRSPParentを探す（タイプに応じて）
        Transform parent = FindRSParent(itemType);
        if (parent == null)
        {
            string parentName = itemType == StageDatabase.RSItemType.Pickaxe ? "RSPParent" : "RSParent";
            Debug.LogWarning($"{parentName}が見つかりません");
            return;
        }

        // このオブジェクトのスケールからサイズを取得（論理サイズ優先）
        int width, height;
        if (logicalSize.x > 0 && logicalSize.y > 0)
        {
            width = logicalSize.x;
            height = logicalSize.y;
        }
        else
        {
            width = Mathf.RoundToInt(transform.localScale.x);
            height = Mathf.RoundToInt(transform.localScale.y);
        }

        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning($"StickyNoteBehavior: 無効なサイズです (width={width}, height={height})");
            return;
        }

        // RSParentを親としてRSを生成
        GameObject instance = Instantiate(settings.RSPrefab, parent);
        
        if (instance != null)
        {
            // グリッドの中心（ローカル座標(0,0)）に配置
            instance.transform.localPosition = Vector3.zero;
            
            // 論理サイズをそのままスケールに設定（gridScaleは無関係）
            instance.transform.localScale = new Vector3(width, height, 1f);
            instance.transform.localRotation = Quaternion.identity;
            instance.name = "RS";

            // Selectorに自身を登録（タイプに応じてRSBehavior、RSPBehavior、またはRSGBehaviorを使用）
            if (itemType == StageDatabase.RSItemType.Pickaxe)
            {
                var rspBehavior = instance.GetComponent<RSPBehavior>();
                if (rspBehavior != null)
                {
                    rspBehavior.SetSourceItem(this);
                }
            }
            else if (itemType == StageDatabase.RSItemType.Gravity)
            {
                var rsgBehavior = instance.GetComponent<RSGBehavior>();
                if (rsgBehavior != null)
                {
                    rsgBehavior.SetSourceItem(this);
                }
            }
            else
            {
                // NormalタイプはRSBehaviorを使用
                var behavior = instance.GetComponent<RSBehavior>();
                if (behavior != null)
                {
                    behavior.SetSourceItem(this);
                }
            }
            
            Debug.Log($"RSをグリッドに追加しました: サイズ({width}, {height})");
        }
    }

    /// <summary>
    /// RSItemMassesの中にRSItemMassをH×W個のグリッドで生成します
    /// </summary>
    public void GenerateItemMasses()
    {
        TypeSettings settings = GetTypeSettings(itemType);
        if (settings == null)
        {
            Debug.LogWarning("タイプ設定が見つかりません");
            return;
        }

        if (settings.RSItemMassPrefab == null)
        {
            Debug.LogWarning("RSItemMassPrefabがアサインされていません");
            return;
        }

        if (settings.RSItemMasses == null)
        {
            Debug.LogWarning("RSItemMassesがアサインされていません");
            return;
        }

        // 論理サイズを取得（H=height, W=width）
        int width, height;
        if (logicalSize.x > 0 && logicalSize.y > 0)
        {
            width = logicalSize.x;  // W
            height = logicalSize.y; // H
        }
        else
        {
            width = Mathf.RoundToInt(transform.localScale.x);
            height = Mathf.RoundToInt(transform.localScale.y);
        }

        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning($"StickyNoteBehavior: 無効なサイズです (width={width}, height={height})");
            return;
        }

        Transform parent = settings.RSItemMasses.transform;

        // 既存の子をクリア
        ClearRSItemMassesChildren(parent);

        // グリッドで敷き詰める（各マスのサイズは1.0と仮定）
        float cellSize = 1.0f;
        float scaledCellSize = cellSize * gridScale;
        int totalCount = height * width; // H × W

        // グリッド全体のサイズを計算
        float gridWidth = width * scaledCellSize;
        float gridHeight = height * scaledCellSize;

        // グリッドの中心が(0,0)になるように、左上の位置を計算
        float offsetX = -gridWidth / 2f + scaledCellSize / 2f;
        float offsetY = gridHeight / 2f - scaledCellSize / 2f;

        for (int i = 0; i < totalCount; i++)
        {
            // グリッド上の行・列を計算（左上から右→下の順）
            int row = i / width;
            int col = i % width;

            // ローカル座標を計算（グリッドの中心が(0,0)になるようにオフセット）
            float x = offsetX + col * scaledCellSize;
            float y = offsetY - row * scaledCellSize;
            Vector3 localPos = new Vector3(x, y, 0f);

            // RSItemMassを生成
            GameObject mass = Instantiate(settings.RSItemMassPrefab, parent);
            mass.transform.localPosition = localPos;
            mass.transform.localScale = Vector3.one * gridScale;
            mass.transform.localRotation = Quaternion.identity;
            string massNamePrefix = itemType == StageDatabase.RSItemType.Pickaxe ? "RSPItemMass" : "RSItemMass";
            mass.name = $"{massNamePrefix}_{row}_{col}";
        }

        string massTypeName = itemType == StageDatabase.RSItemType.Pickaxe ? "RSPItemMass" : "RSItemMass";
        Debug.Log($"{massTypeName}を {totalCount} 個生成しました (H={height}, W={width})");
    }

    /// <summary>
    /// RSItemMasses配下の子を全破棄します
    /// </summary>
    private void ClearRSItemMassesChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 指定フレーム待ってからRSを生成します
    /// </summary>
    private System.Collections.IEnumerator GenerateSelectorAfterFrames(int waitFrames)
    {
        for (int i = 0; i < waitFrames; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        GenerateRS();
        generateRoutine = null;
    }

    /// <summary>
    /// アイテムの論理サイズ（グリッド単位）を設定します。
    /// これを設定すると、見た目のスケールに関わらず、指定されたサイズでRSが生成されます。
    /// </summary>
    public void SetLogicalSize(int width, int height)
    {
        logicalSize = new Vector2Int(width, height);
        itemWidth = width;
        itemHeight = height;
    }

    /// <summary>
    /// アイテムのインデックスを設定します
    /// </summary>
    public void SetItemIndex(int index)
    {
        itemIndex = index;
    }

    /// <summary>
    /// アイテムのタイプを設定します
    /// </summary>
    public void SetItemType(StageDatabase.RSItemType type)
    {
        itemType = type;
        ApplyTypeSettings();
    }

    /// <summary>
    /// アイテムのインデックスを取得します
    /// </summary>
    public int GetItemIndex()
    {
        return itemIndex;
    }

    /// <summary>
    /// アイテムの透明度を設定します
    /// </summary>
    public void SetAlpha(float alpha)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    /// <summary>
    /// 現在のステージデータを取得します（ランタイムではDeepCopy済みを優先）
    /// </summary>
    private StageDatabase.StageData GetStageData()
    {
        // 1. CurrentGameStatus からランタイムデータを取得（DeepCopy済み）
        CurrentGameStatus currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentGameStatus != null)
        {
            var runtimeData = currentGameStatus.GetCurrentStageData();
            if (runtimeData != null)
            {
                return runtimeData;
            }

            // ランタイムデータがまだない場合は、StageDatabase から直接取得（読み取り専用）
            StageDatabase db = currentGameStatus.GetStageDatabase();
            if (db != null)
            {
                int idx = currentGameStatus.GetCurrentStageIndex();
                return db.GetStageData(idx);
            }
        }

        // 2. フォールバック：GridGenerator から StageDatabase を取得
        GridGenerator gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        if (gridGenerator != null)
        {
            var dbField = typeof(GridGenerator).GetField("stageDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dbField != null)
            {
                StageDatabase db = dbField.GetValue(gridGenerator) as StageDatabase;
                if (db != null)
                {
                    int idx = currentGameStatus != null ? currentGameStatus.GetCurrentStageIndex() : 0;
                    return db.GetStageData(idx);
                }
            }
        }

        Debug.LogWarning("ステージデータを取得できませんでした（StickyNoteBehavior）");
        return null;
    }

    /// <summary>
    /// RSParentまたはRSPParentを探し、Transformを返します（タイプに応じて）
    /// </summary>
    private Transform FindRSParent(StageDatabase.RSItemType type)
    {
        string parentName;
        switch (type)
        {
            case StageDatabase.RSItemType.Pickaxe:
                parentName = "RSPParent";
                break;
            case StageDatabase.RSItemType.Gravity:
                parentName = "RSParent"; // GravityタイプもRSParentを使用（必要に応じて変更可能）
                break;
            default:
                parentName = "RSParent";
                break;
        }
        
        var transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in transforms)
        {
            if (t != null && t.name == parentName)
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    /// RSParentまたはRSPParent配下の子を全破棄します
    /// </summary>
    private void ClearRSParentChildren()
    {
        // 両方の親を探してクリア
        Transform rsParent = FindRSParent(StageDatabase.RSItemType.Normal);
        if (rsParent != null)
        {
            for (int i = rsParent.childCount - 1; i >= 0; i--)
            {
                var child = rsParent.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        Transform rspParent = FindRSParent(StageDatabase.RSItemType.Pickaxe);
        if (rspParent != null)
        {
            for (int i = rspParent.childCount - 1; i >= 0; i--)
            {
                var child = rspParent.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}

