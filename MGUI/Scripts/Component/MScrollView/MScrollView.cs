using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGUI
{
    public class MScrollView : ScrollRect
    {
        public class ItemInfo
        {
            public GameObject itemGO;
            public RectTransform itemRectTransform;
            public int itemIndex = 0;
        }

        public class CellInfo
        {
            public GameObject cellGO;
            public RectTransform cellRectTransform;
            public List<ItemInfo> itemList;
            public int cellIndex = 0;
            public float pos = 0; //随着位置改变
            public float width = 0; //长度,随着变动改变
        }

        public enum ScrollType
        {
            Horizontal = 0,
            Vertical = 1,
        }

        public ScrollType mScrollType = ScrollType.Vertical;
        public float Spacing = 0;

        private RectTransform rectTransform;

        private Rect rectSize;

        private CircleArray<CellInfo> cellList; //用于循环的cell，不是所有cell

        private Vector2 item_size; //默认item长、高

        private int lineItemNum = 0; //一行/列有几个item
        private int dataLen = 0; //数据长度
        private int cellLen = 0; //cell总数量

        private List<float> allCellWidth; //所有cell宽度

        private float oldDir = 0;

        private Action<ItemInfo> refreshItemCallBack; //item刷新回调

        private Action scrollEndCallBack; //滑动到底的回调，主要用来处理分页
        private bool canTriggerScrollEndCallBack = false; //是否能触发滚动到底的回调。一次拖拽只能触发一次回调

        protected override void Awake()
        {
            base.Awake();
            if (rectTransform == null) {
                rectTransform = GetComponent<RectTransform>();
                rectSize = rectTransform.rect;
            }

            if (mScrollType == ScrollType.Horizontal) {
                horizontal = true;
                vertical = false;
            }
            else {
                horizontal = false;
                vertical = true;
            }
        }

        /// <summary>
        /// 循环ScrollView
        /// </summary>
        /// <param name="item">item预制体</param>
        /// <param name="lineItemNum">1行或者1列有多少个item</param>
        /// <param name="dataLen">数据长度</param>
        /// <param name="refreshItemCallBack">item刷新回调</param>
        /// <param name="scrollEndCallBack">scrollEndCallBack!=null代表分页。滑动到底回调，主要用来处理分页等特殊情况</param>
        private void InitForPage(GameObject item, int lineItemNum, int dataLen, Action<ItemInfo> refreshItemCallBack,
            Action scrollEndCallBack = null)
        {
            this.refreshItemCallBack = null;
            this.refreshItemCallBack = refreshItemCallBack;
            this.scrollEndCallBack = null;
            this.scrollEndCallBack = scrollEndCallBack;

            if (rectTransform == null) {
                rectTransform = GetComponent<RectTransform>();
            }

            if (cellList != null && cellList.Count > 0 && lineItemNum == this.lineItemNum &&
                Mathf.Abs(rectSize.width - rectTransform.rect.width) <= 1f &&
                Mathf.Abs(rectSize.height - rectTransform.rect.height) <= 1f) {
                if (this.scrollEndCallBack != null) //分页
                {
                    if (this.dataLen != dataLen) {
                        this.lineItemNum = lineItemNum;

                        //总长度,并不是cellList的长度。(如果每行/列只有一个item，那么这个值=dataLen)
                        cellLen = dataLen / lineItemNum;
                        if (dataLen % lineItemNum > 0) {
                            cellLen++;
                        }

                        //分页变长后，allCellWidth数量更新
                        int difference = this.dataLen - dataLen;
                        if (difference > 0) {
                            // for (int i = 0; i < difference; i++)
                            // {
                            //     allCellWidth.RemoveAt(allCellWidth.Count - 1);
                            // }

                            int startIndex = allCellWidth.Count - difference;
                            if (startIndex >= 0) {
                                allCellWidth.RemoveRange(startIndex, difference);
                            }

                            //正常分页数据不会变短，数据变短代表分页重置了，所以重置content的位置(回到列表开头)
                            resetContentPos();
                        }
                        else if (difference < 0) {
                            difference = -difference;

                            // 预分配容量，避免多次扩容
                            if (allCellWidth.Capacity < allCellWidth.Count + difference) {
                                allCellWidth.Capacity = allCellWidth.Count + difference;
                            }

                            float itemWidth = (mScrollType == ScrollType.Horizontal) ? item_size.x : item_size.y;

                            float[] newWidths = new float[difference];
                            for (int i = 0; i < difference; i++) {
                                newWidths[i] = itemWidth;
                            }

                            allCellWidth.AddRange(newWidths);

                            // for (int i = 0; i < difference; i++)
                            // {
                            //     allCellWidth.Add(itemWidth);
                            // }
                        }

                        this.dataLen = dataLen;
                    }

                    refresh();
                }
                else {
                    if (this.dataLen != dataLen) {
                        initCell(item, lineItemNum, dataLen, refreshItemCallBack);
                    }
                    else {
                        refresh();
                    }
                }
            }
            else {
                initCell(item, lineItemNum, dataLen, refreshItemCallBack);
            }
        }

        /// <summary>
        /// 非循环ScrollView,和原本ScrollView使用没区别，建议直接使用原本ScrollView
        /// </summary>
        /// <param name="item">item预制体</param>
        /// <param name="lineItemNum">1行或者1列有多少个item</param>
        /// <param name="dataLen">数据长度</param>
        /// <param name="refreshItemCallBack">item刷新回调</param>
        /// <param name="isResetContentPos">是否重置content的位置(回到列表开头)。根据具体使用传值</param>
        public void InitForNotLoop(GameObject item, int lineItemNum, int dataLen, Action<ItemInfo> refreshItemCallBack,
            Func<int, float> refreshItemWidthCallBack = null, bool isResetContentPos = false)
        {
            if (rectTransform == null) {
                rectTransform = GetComponent<RectTransform>();
            }

            if (cellList != null && cellList.Count > 0 && lineItemNum == this.lineItemNum && dataLen == this.dataLen) {
                this.refreshItemCallBack = null;
                this.refreshItemCallBack = refreshItemCallBack;
                this.scrollEndCallBack = null;

                if (isResetContentPos) {
                    content.anchoredPosition = Vector2.zero;

                    if (mScrollType == ScrollType.Horizontal) {
                        horizontalNormalizedPosition = 0;
                    }
                    else {
                        verticalNormalizedPosition = 1;
                    }
                }

                refresh();
            }
            else {
                Clear();
                if (lineItemNum <= 0 || dataLen <= 0 || item == null) {
                    return;
                }

                this.lineItemNum = lineItemNum;
                this.dataLen = dataLen;
                this.refreshItemCallBack = null;
                this.refreshItemCallBack = refreshItemCallBack;
                this.scrollEndCallBack = null;

                rectSize = rectTransform.rect;

                //总长度,并不是cellList的长度。(如果每行/列只有一个item，那么这个值=dataLen)
                cellLen = dataLen / lineItemNum;
                if (dataLen % lineItemNum > 0) {
                    cellLen++;
                }

                if (cellList == null || cellList.Count != cellLen) {
                    cellList = new CircleArray<CellInfo>(cellLen);
                }

                if (allCellWidth == null) {
                    allCellWidth = new List<float>();
                }

                allCellWidth.Clear();

                var item_RT = item.GetComponent<RectTransform>();
                item_size = new Vector2(item_RT.rect.width, item_RT.rect.height);

                if (content == null) {
                    var content_go = new GameObject("Content");
                    content = content_go.AddComponent<RectTransform>();
                    content.SetParent(rectTransform);
                }

                var cellGO = new GameObject("cell");
                var cellGO_RT = cellGO.gameObject.AddComponent<RectTransform>();

                if (mScrollType == ScrollType.Horizontal) {
                    for (int i = 0; i < cellLen; i++) {
                        allCellWidth.Add(item_size.x + Spacing);
                    }

                    cellGO_RT.pivot = new Vector2(0, 0.5f);
                    cellGO_RT.sizeDelta = new Vector2(item_size.x + Spacing, item_size.y * lineItemNum);
                    cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
                    cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 0);
                    cellGO_RT.anchorMin = new Vector2(0, 0);
                    cellGO_RT.anchorMax = new Vector2(0, 1);

                    item_RT.pivot = cellGO_RT.pivot;

                    content.pivot = cellGO_RT.pivot;
                    content.sizeDelta = new Vector2(item_size.x * cellLen + (cellLen - 1) * Spacing,
                        rectTransform.rect.height);
                    content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
                    content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 0);
                    content.anchorMin = new Vector2(0, 0);
                    content.anchorMax = new Vector2(0, 1);
                    content.anchoredPosition = Vector2.zero;

                    float pos_x = 0;
                    for (int i = 0; i < cellLen; i++) {
                        CellInfo cellInfo;
                        if (i < cellList.Count) {
                            cellInfo = cellList[i];
                            if (cellInfo == null) {
                                cellInfo = new CellInfo();
                                cellInfo.cellGO = Instantiate(cellGO, content);
                            }
                        }
                        else {
                            cellInfo = new CellInfo();
                            cellInfo.cellGO = Instantiate(cellGO, content);
                        }

                        cellInfo.cellIndex = i;
                        cellInfo.cellRectTransform = cellInfo.cellGO.GetComponent<RectTransform>();
                        cellInfo.cellGO.transform.localScale = Vector3.one;
                        cellInfo.cellGO.transform.localPosition = new Vector3(pos_x, 0, 0);
                        cellInfo.pos = pos_x;
                        cellInfo.cellGO.name = $"cell{i}";
                        cellInfo.width = item_size.x + Spacing;

                        List<ItemInfo> itemList;
                        if (cellInfo.itemList != null) {
                            itemList = cellInfo.itemList;
                        }
                        else {
                            itemList = new List<ItemInfo>();
                        }

                        for (int j = 0; j < lineItemNum; j++) {
                            ItemInfo itemInfo;
                            if (cellInfo.itemList != null && j < cellInfo.itemList.Count) {
                                itemInfo = cellInfo.itemList[j];
                            }
                            else {
                                itemInfo = new ItemInfo();
                                itemInfo.itemGO = Instantiate(item, cellInfo.cellGO.transform);
                                itemList.Add(itemInfo);
                            }

                            itemInfo.itemRectTransform = itemInfo.itemGO.GetComponent<RectTransform>();
                            itemInfo.itemGO.transform.localPosition = new Vector3(0,
                                -j * item_size.y + lineItemNum * item_size.y / 2 - item_size.y / 2, 0);
                            itemInfo.itemGO.SetActive(true);
                        }

                        cellInfo.itemList = itemList;

                        cellList[i] = cellInfo;
                        refreshCell(cellInfo);
                        cellInfo.cellGO.SetActive(true);

                        pos_x += cellInfo.width;
                    }

                    horizontal = true;
                    vertical = false;
                }
                else {
                    for (int i = 0; i < cellLen; i++) {
                        allCellWidth.Add(item_size.y + Spacing);
                    }

                    cellGO_RT.pivot = new Vector2(0.5f, 1);
                    cellGO_RT.sizeDelta = new Vector2(item_size.x * lineItemNum, item_size.y + Spacing);
                    cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
                    cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 0);
                    cellGO_RT.anchorMin = new Vector2(0, 1);
                    cellGO_RT.anchorMax = new Vector2(1, 1);

                    item_RT.pivot = cellGO_RT.pivot;

                    content.pivot = cellGO_RT.pivot;
                    content.sizeDelta =
                        new Vector2(rectTransform.rect.width, item_size.y * cellLen + (cellLen - 1) * Spacing);
                    content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
                    content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 0);
                    content.anchorMin = new Vector2(0, 1);
                    content.anchorMax = new Vector2(1, 1);
                    content.anchoredPosition = Vector2.zero;

                    float pos_y = 0;
                    for (int i = 0; i < cellLen; i++) {
                        CellInfo cellInfo;
                        if (i < cellList.Count) {
                            cellInfo = cellList[i];
                            if (cellInfo == null) {
                                cellInfo = new CellInfo();
                                cellInfo.cellGO = Instantiate(cellGO, content);
                            }
                        }
                        else {
                            cellInfo = new CellInfo();
                            cellInfo.cellGO = Instantiate(cellGO, content);
                        }

                        cellInfo.cellIndex = i;
                        cellInfo.cellRectTransform = cellInfo.cellGO.GetComponent<RectTransform>();
                        cellInfo.cellGO.transform.localScale = Vector3.one;
                        cellInfo.cellGO.transform.localPosition = new Vector3(0, pos_y, 0);
                        cellInfo.pos = pos_y;
                        cellInfo.cellGO.name = $"cell{i}";
                        cellInfo.width = item_size.y + Spacing;

                        List<ItemInfo> itemList;
                        if (cellInfo.itemList != null) {
                            itemList = cellInfo.itemList;
                        }
                        else {
                            itemList = new List<ItemInfo>();
                        }

                        for (int j = 0; j < lineItemNum; j++) {
                            ItemInfo itemInfo;
                            if (cellInfo.itemList != null && j < cellInfo.itemList.Count) {
                                itemInfo = cellInfo.itemList[j];
                            }
                            else {
                                itemInfo = new ItemInfo();
                                itemInfo.itemGO = Instantiate(item, cellInfo.cellGO.transform);
                                itemList.Add(itemInfo);
                            }

                            itemInfo.itemRectTransform = itemInfo.itemGO.GetComponent<RectTransform>();
                            itemInfo.itemGO.transform.localPosition =
                                new Vector3(j * item_size.x - lineItemNum * item_size.x / 2 + item_size.x / 2, 0, 0);
                            itemInfo.itemGO.SetActive(true);
                        }

                        cellInfo.itemList = itemList;

                        cellList[i] = cellInfo;
                        refreshCell(cellInfo);
                        cellInfo.cellGO.SetActive(true);

                        pos_y -= cellInfo.width;
                    }

                    vertical = true;
                    horizontal = false;
                }

                Destroy(cellGO);
            }

            if (refreshItemWidthCallBack != null) {
                for (int i = 0; i < dataLen; i++) {
                    var dataIndex = i + 1;
                    int cellIndex = dataIndex / lineItemNum;
                    if (dataIndex % lineItemNum == 0) {
                        cellIndex--;
                    }

                    allCellWidth[cellIndex] = refreshItemWidthCallBack(i) + Spacing;
                }

                adjustContent();
            }
        }

        /// <summary>
        /// 循环ScrollView
        /// </summary>
        /// <param name="item">item预制体</param>
        /// <param name="lineItemNum">1行或者1列有多少个item</param>
        /// <param name="dataLen">数据长度</param>
        /// <param name="refreshItemCallBack">item刷新回调</param>
        /// <param name="refreshItemWidthCallBack">计算每个item宽度</param>
        /// <param name="isResetContentPos">是否重置content的位置(回到列表开头)</param>
        public void InitForLoop(GameObject item, int lineItemNum, int dataLen, Action<ItemInfo> refreshItemCallBack,
            Func<int, float> refreshItemWidthCallBack = null, bool isResetContentPos = false)
        {
            scrollEndCallBack = null;

            if (rectTransform == null) {
                rectTransform = GetComponent<RectTransform>();
            }

            if (cellList != null && cellList.Count > 0 && lineItemNum == this.lineItemNum &&
                Mathf.Abs(rectSize.width - rectTransform.rect.width) <= 1f &&
                Mathf.Abs(rectSize.height - rectTransform.rect.height) <= 1f) {
                this.refreshItemCallBack = null;
                this.refreshItemCallBack = refreshItemCallBack;

                if (this.dataLen != dataLen) {
                    initCell(item, lineItemNum, dataLen, refreshItemCallBack);
                }
                else {
                    if (isResetContentPos) {
                        resetContentPos();
                    }

                    refresh();
                }

                if (refreshItemWidthCallBack != null) {
                    for (int i = 0; i < dataLen; i++) {
                        var dataIndex = i;
                        int cellIndex = dataIndex / lineItemNum;
                        allCellWidth[cellIndex] = refreshItemWidthCallBack(i) + Spacing;
                    }

                    adjustContent();
                }
            }
            else {
                initCell(item, lineItemNum, dataLen, refreshItemCallBack);

                if (refreshItemWidthCallBack != null) {
                    for (int i = 0; i < dataLen; i++) {
                        var dataIndex = i;
                        int cellIndex = dataIndex / lineItemNum;
                        allCellWidth[cellIndex] = refreshItemWidthCallBack(i) + Spacing;
                    }

                    adjustContent();
                }
            }
        }

        //重置content的位置(回到列表开头)
        private void resetContentPos()
        {
            //this.DOKill();
            StopMovement();

            content.anchoredPosition = Vector2.zero;

            if (mScrollType == ScrollType.Horizontal) {
                horizontalNormalizedPosition = 0;

                float pos_x = 0;
                int len = cellList.Count;
                for (int i = 0; i < len; i++) {
                    var cellInfo = cellList[i];
                    cellInfo.cellIndex = i;
                    cellInfo.cellGO.transform.localPosition = new Vector3(pos_x, 0, 0);
                    cellInfo.pos = pos_x;
                    cellInfo.cellGO.name = $"cell{i}";
                    if (cellInfo.cellIndex < allCellWidth.Count) {
                        pos_x += allCellWidth[cellInfo.cellIndex];
                    }
                    else {
                        pos_x += cellInfo.width;
                    }
                }
            }
            else {
                verticalNormalizedPosition = 1;

                float pos_y = 0;
                int len = cellList.Count;
                for (int i = 0; i < len; i++) {
                    var cellInfo = cellList[i];
                    cellInfo.cellIndex = i;
                    cellInfo.cellGO.transform.localPosition = new Vector3(0, pos_y, 0);
                    cellInfo.pos = pos_y;
                    cellInfo.cellGO.name = $"cell{i}";
                    if (cellInfo.cellIndex < allCellWidth.Count) {
                        pos_y -= allCellWidth[cellInfo.cellIndex];
                    }
                    else {
                        pos_y -= cellInfo.width;
                    }
                }
            }
        }

        private void initCell(GameObject item, int lineItemNum, int dataLen, Action<ItemInfo> refreshItemCallBack = null)
        {
            Clear();
            if (lineItemNum <= 0 || dataLen <= 0 || item == null) {
                return;
            }

            this.lineItemNum = lineItemNum;
            this.dataLen = dataLen;
            this.refreshItemCallBack = null;
            this.refreshItemCallBack = refreshItemCallBack;

            rectSize = rectTransform.rect;

            //总长度,并不是cellList的长度。(如果每行/列只有一个item，那么这个值=dataLen)
            cellLen = dataLen / lineItemNum;
            if (dataLen % lineItemNum > 0) {
                cellLen++;
            }

            if (allCellWidth == null) {
                allCellWidth = new List<float>();
            }

            allCellWidth.Clear();

            var item_RT = item.GetComponent<RectTransform>();
            item_size = new Vector2(item_RT.rect.width, item_RT.rect.height);

            if (content == null) {
                var content_go = new GameObject("Content");
                content = content_go.AddComponent<RectTransform>();
                content.SetParent(rectTransform);
            }

            var cellGO = new GameObject("cell");
            var cellGO_RT = cellGO.gameObject.AddComponent<RectTransform>();

            if (mScrollType == ScrollType.Horizontal) {
                for (int i = 0; i < cellLen; i++) {
                    allCellWidth.Add(item_size.x + Spacing);
                }

                cellGO_RT.pivot = new Vector2(0, 0.5f);
                cellGO_RT.sizeDelta = new Vector2(item_size.x + Spacing, item_size.y * lineItemNum);
                cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
                cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 0);
                cellGO_RT.anchorMin = new Vector2(0, 0);
                cellGO_RT.anchorMax = new Vector2(0, 1);

                item_RT.pivot = cellGO_RT.pivot;

                content.pivot = cellGO_RT.pivot;
                content.sizeDelta = new Vector2(item_size.x * cellLen + (cellLen - 1) * Spacing, rectTransform.rect.height);
                content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
                content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 0);
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(0, 1);
                content.anchoredPosition = Vector2.zero;

                float pos_x = 0;
                int loopItemNum = (int)Math.Ceiling(rectTransform.rect.width / item_size.x) + 1;
                if (cellList == null || cellList.Count != loopItemNum) {
                    cellList = new CircleArray<CellInfo>(loopItemNum);
                }

                for (int i = 0; i < loopItemNum; i++) {
                    CellInfo cellInfo;
                    if (i < cellList.Count) {
                        cellInfo = cellList[i];
                        if (cellInfo == null) {
                            cellInfo = new CellInfo();
                            cellInfo.cellGO = Instantiate(cellGO, content);
                        }
                    }
                    else {
                        cellInfo = new CellInfo();
                        cellInfo.cellGO = Instantiate(cellGO, content);
                    }

                    cellInfo.cellIndex = i;
                    cellInfo.cellRectTransform = cellInfo.cellGO.GetComponent<RectTransform>();
                    cellInfo.cellGO.transform.localScale = Vector3.one;
                    cellInfo.cellGO.transform.localPosition = new Vector3(pos_x, 0, 0);
                    cellInfo.pos = pos_x;
                    cellInfo.cellGO.name = $"cell{i}";
                    cellInfo.width = item_size.x + Spacing;

                    List<ItemInfo> itemList;
                    if (cellInfo.itemList != null) {
                        itemList = cellInfo.itemList;
                    }
                    else {
                        itemList = new List<ItemInfo>();
                    }

                    for (int j = 0; j < lineItemNum; j++) {
                        ItemInfo itemInfo;
                        if (cellInfo.itemList != null && j < cellInfo.itemList.Count) {
                            itemInfo = cellInfo.itemList[j];
                        }
                        else {
                            itemInfo = new ItemInfo();
                            itemInfo.itemGO = Instantiate(item, cellInfo.cellGO.transform);
                            itemList.Add(itemInfo);
                        }

                        itemInfo.itemRectTransform = itemInfo.itemGO.GetComponent<RectTransform>();
                        itemInfo.itemGO.transform.localPosition = new Vector3(0,
                            -j * item_size.y + lineItemNum * item_size.y / 2 - item_size.y / 2, 0);
                        itemInfo.itemGO.SetActive(true);
                    }

                    cellInfo.itemList = itemList;

                    cellList[i] = cellInfo;
                    refreshCell(cellInfo);
                    cellInfo.cellGO.SetActive(true);

                    pos_x += cellInfo.width;
                }

                horizontal = true;
                vertical = false;
            }
            else {
                for (int i = 0; i < cellLen; i++) {
                    allCellWidth.Add(item_size.y + Spacing);
                }

                cellGO_RT.pivot = new Vector2(0.5f, 1);
                cellGO_RT.sizeDelta = new Vector2(item_size.x * lineItemNum, item_size.y + Spacing);
                cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
                cellGO_RT.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 0);
                cellGO_RT.anchorMin = new Vector2(0, 1);
                cellGO_RT.anchorMax = new Vector2(1, 1);

                item_RT.pivot = cellGO_RT.pivot;

                content.pivot = cellGO_RT.pivot;
                content.sizeDelta = new Vector2(rectTransform.rect.width, item_size.y * cellLen + (cellLen - 1) * Spacing);
                content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
                content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 0);
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.anchoredPosition = Vector2.zero;

                float pos_y = 0;
                int loopItemNum = (int)Math.Ceiling(rectTransform.rect.height / item_size.y) + 1;
                if (cellList == null || cellList.Count != loopItemNum) {
                    cellList = new CircleArray<CellInfo>(loopItemNum);
                }

                for (int i = 0; i < loopItemNum; i++) {
                    CellInfo cellInfo;
                    if (i < cellList.Count) {
                        cellInfo = cellList[i];
                        if (cellInfo == null) {
                            cellInfo = new CellInfo();
                            cellInfo.cellGO = Instantiate(cellGO, content);
                        }
                    }
                    else {
                        cellInfo = new CellInfo();
                        cellInfo.cellGO = Instantiate(cellGO, content);
                    }

                    cellInfo.cellIndex = i;
                    cellInfo.cellRectTransform = cellInfo.cellGO.GetComponent<RectTransform>();
                    cellInfo.cellGO.transform.localScale = Vector3.one;
                    cellInfo.cellGO.transform.localPosition = new Vector3(0, pos_y, 0);
                    cellInfo.pos = pos_y;
                    cellInfo.cellGO.name = $"cell{i}";
                    cellInfo.width = item_size.y + Spacing;

                    List<ItemInfo> itemList;
                    if (cellInfo.itemList != null) {
                        itemList = cellInfo.itemList;
                    }
                    else {
                        itemList = new List<ItemInfo>();
                    }

                    for (int j = 0; j < lineItemNum; j++) {
                        ItemInfo itemInfo;
                        if (cellInfo.itemList != null && j < cellInfo.itemList.Count) {
                            itemInfo = cellInfo.itemList[j];
                        }
                        else {
                            itemInfo = new ItemInfo();
                            itemInfo.itemGO = Instantiate(item, cellInfo.cellGO.transform);
                            itemList.Add(itemInfo);
                        }

                        itemInfo.itemRectTransform = itemInfo.itemGO.GetComponent<RectTransform>();
                        itemInfo.itemGO.transform.localPosition =
                            new Vector3(j * item_size.x - lineItemNum * item_size.x / 2 + item_size.x / 2, 0, 0);
                        itemInfo.itemGO.SetActive(true);
                    }

                    cellInfo.itemList = itemList;

                    cellList[i] = cellInfo;
                    refreshCell(cellInfo);
                    cellInfo.cellGO.SetActive(true);

                    pos_y -= cellInfo.width;
                }

                vertical = true;
                horizontal = false;
            }

            Destroy(cellGO);
            onValueChanged.RemoveAllListeners();
            onValueChanged.AddListener(drageChange);
        }

        private void drageChange(Vector2 vec2)
        {
            int len = cellList.Count;
            var cell1 = cellList[0];
            var cell2 = cellList[len - 1];

            if (mScrollType == ScrollType.Horizontal) //水平
            {
                var contentX = content.anchoredPosition.x;
                if (vec2.x - oldDir > 0) {
                    // 原代码在这里有递归调用，改为while循环
                    while (cell1.pos + contentX <= -cell1.width) {
                        int index = cell1.cellIndex + len;
                        if (index < 0 || index > cellLen - 1)
                            break;

                        cell1.pos = cell2.pos + cell2.width;
                        cell1.cellGO.transform.localPosition = new Vector3(cell1.pos, 0, 0);
                        cell1.cellIndex = index;
                        refreshCell(cell1);

                        cellList.MoveHeadToTail();

                        // 更新cell1和cell2的引用
                        cell1 = cellList[0];
                        cell2 = cellList[len - 1];
                    }
                }
                else if (vec2.x - oldDir < 0) {
                    // 原代码在这里有递归调用，改为while循环
                    while (cell2.pos + contentX >= rectTransform.rect.width) {
                        int index = cell2.cellIndex - len;
                        if (index < 0 || index > cellLen - 1)
                            break;

                        cell2.cellIndex = index;
                        refreshCell(cell2);
                        cell2.pos = cell1.pos - cell2.width;
                        cell2.cellGO.transform.localPosition = new Vector3(cell2.pos, 0, 0);

                        cellList.MoveTailToHead();

                        // 更新cell1和cell2的引用
                        cell1 = cellList[0];
                        cell2 = cellList[len - 1];
                    }
                }

                oldDir = vec2.x;

                if (horizontalNormalizedPosition >= 0.9f && canTriggerScrollEndCallBack) {
                    canTriggerScrollEndCallBack = false;
                    if (scrollEndCallBack != null) {
                        scrollEndCallBack();
                        scrollEndCallBack = null;
                    }
                }
            }
            else //垂直
            {
                var contentY = content.anchoredPosition.y;
                if (vec2.y - oldDir < 0) {
                    // 原代码在这里有递归调用，改为while循环
                    while (cell1.pos + contentY >= cell1.width) {
                        int index = cell1.cellIndex + len;
                        if (index < 0 || index > cellLen - 1)
                            break;

                        cell1.pos = cell2.pos - cell2.width;
                        cell1.cellGO.transform.localPosition = new Vector3(0, cell1.pos, 0);
                        cell1.cellIndex = index;
                        refreshCell(cell1);

                        cellList.MoveHeadToTail();

                        // 更新cell1和cell2的引用
                        cell1 = cellList[0];
                        cell2 = cellList[len - 1];
                    }
                }
                else if (vec2.y - oldDir > 0) {
                    // 原代码在这里有递归调用，改为while循环
                    while (cell2.pos + contentY <= -rectTransform.rect.height) {
                        int index = cell2.cellIndex - len;
                        if (index < 0 || index > cellLen - 1)
                            break;

                        cell2.cellIndex = index;
                        refreshCell(cell2);
                        cell2.pos = cell1.pos + cell2.width;
                        cell2.cellGO.transform.localPosition = new Vector3(0, cell2.pos, 0);

                        cellList.MoveTailToHead();

                        // 更新cell1和cell2的引用
                        cell1 = cellList[0];
                        cell2 = cellList[len - 1];
                    }
                }

                oldDir = vec2.y;

                if (verticalNormalizedPosition <= 0.1f && canTriggerScrollEndCallBack) {
                    canTriggerScrollEndCallBack = false;
                    if (scrollEndCallBack != null) {
                        scrollEndCallBack();
                        scrollEndCallBack = null;
                    }
                }
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            canTriggerScrollEndCallBack = true;
        }

        private void refreshCell(CellInfo cellInfo)
        {
            int indexStart = cellInfo.cellIndex * lineItemNum;

            for (int i = 0; i < cellInfo.itemList.Count; i++) {
                var itemInfo = cellInfo.itemList[i];
                itemInfo.itemIndex = i + indexStart;
                if (itemInfo.itemIndex >= dataLen) {
                    itemInfo.itemGO.SetActive(false);
                }
                else {
                    refreshItem(itemInfo);
                    itemInfo.itemGO.SetActive(true);
                }
            }

            adjustCellWidth(cellInfo);
        }

        private void refreshItem(ItemInfo itemInfo)
        {
            if (refreshItemCallBack != null) {
                refreshItemCallBack(itemInfo);
            }
        }

        //item变长后需要调整ScrollView
        //item变长，直接取cellInfo.itemList[0]，然后进行cellInfo.cellRectTransform大小调整
        private void adjustCellWidth(CellInfo cellInfo)
        {
            if (cellInfo != null) {
                var itemSize = cellInfo.itemList[0].itemRectTransform.rect;

                var cellSize = cellInfo.cellRectTransform.sizeDelta;
                if (mScrollType == ScrollType.Horizontal) {
                    cellInfo.width = itemSize.width + Spacing;
                    cellInfo.cellRectTransform.sizeDelta = new Vector2(itemSize.width + Spacing, cellSize.y);
                }
                else {
                    cellInfo.width = itemSize.height + Spacing;
                    cellInfo.cellRectTransform.sizeDelta = new Vector2(cellSize.x, itemSize.height + Spacing);
                }

                if (cellInfo.cellIndex < allCellWidth.Count) {
                    allCellWidth[cellInfo.cellIndex] = cellInfo.width;
                }

                adjustContent();
            }
        }

        private void adjustContent()
        {
            float lentgh = 0;
            int len = allCellWidth.Count;
            for (int i = 0; i < len; i++) {
                lentgh += allCellWidth[i];
            }

            var content_sizeDelta = content.sizeDelta;
            if (mScrollType == ScrollType.Horizontal) {
                content.sizeDelta = new Vector2(lentgh, content_sizeDelta.y);
            }
            else {
                content.sizeDelta = new Vector2(content_sizeDelta.x, lentgh);
            }
        }

        //根据dataIndex刷新单个item
        public void RefreshByItemIndex(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex >= dataLen) {
                return;
            }

            var cellIndex = dataIndex / lineItemNum;

            int startIndex = -1;
            float startPos = -1;
            int len = cellList.Count;
            for (int i = 0; i < len; i++) {
                if (cellList[i].cellIndex == cellIndex) {
                    startIndex = i;
                    startPos = cellList[i].pos;
                    adjustCellWidth(cellList[i]);
                    break;
                }
            }

            if (startIndex >= 0) {
                if (mScrollType == ScrollType.Horizontal) {
                    for (int i = startIndex; i < len; i++) {
                        var cellInfo = cellList[i];
                        cellInfo.pos = startPos;
                        cellInfo.cellRectTransform.anchoredPosition = new Vector2(cellInfo.pos, 0);
                        startPos += cellInfo.width;
                    }
                }
                else {
                    for (int i = startIndex; i < len; i++) {
                        var cellInfo = cellList[i];
                        cellInfo.pos = startPos;
                        cellInfo.cellRectTransform.anchoredPosition = new Vector2(0, cellInfo.pos);
                        startPos -= cellInfo.width;
                    }
                }
            }
        }

        /// <summary>
        /// 判断数据index的item在不在视口内，0表示在视口内，-1表示在视口左面(上面)，1表示在视口右面(下面).-99代表没找到
        /// </summary>
        /// <param name="dataIndex">数据index</param>
        /// <returns></returns>
        public int ItemInView(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex >= dataLen) {
                return -99;
            }

            if (cellList != null) {
                var len = cellList.Count;
                if (len > 0) {
                    int cellIndex = dataIndex / lineItemNum;

                    if (cellIndex < cellList[0].cellIndex) {
                        return -1;
                    }
                    else if (cellIndex > cellList[len - 1].cellIndex) {
                        return 1;
                    }
                    else {
                        CellInfo cellInfo = null;
                        for (int i = 0; i < cellList.Count; i++) {
                            if (cellList[i].cellIndex == cellIndex) {
                                cellInfo = cellList[i];
                            }
                        }

                        if (cellInfo != null) {
                            if (mScrollType == ScrollType.Horizontal) {
                                var contentX = content.anchoredPosition.x;
                                if (cellInfo.pos + contentX <= -cellInfo.width + Spacing) {
                                    return -1;
                                }
                                else if (cellInfo.pos + contentX >= rectTransform.rect.width - Spacing) {
                                    return 1;
                                }
                                else {
                                    return 0;
                                }
                            }
                            else {
                                var contentY = content.anchoredPosition.y;
                                if (cellInfo.pos + contentY >= cellInfo.width - Spacing) {
                                    return -1;
                                }
                                else if (cellInfo.pos + contentY <= -rectTransform.rect.height + Spacing) {
                                    return 1;
                                }
                                else {
                                    return 0;
                                }
                            }
                        }
                    }
                }
            }

            return -99;
        }

        /// <summary>
        /// 定位到第几个数据.注意：存在变长item时，需要在初始化时传入计算所有item长度的函数 refreshItemWidthCallBack
        /// </summary>
        /// <param name="dataIndex">数据index</param>
        public void Locate(int dataIndex)
        {
            if (cellList == null || cellLen <= 0 || content == null) {
                return;
            }

            if (dataIndex > dataLen || dataIndex < 0) {
                return;
            }

            var cellIndex = dataIndex / lineItemNum;

            float scrollLenght = 0;
            for (int i = 0; i < cellIndex; i++) {
                scrollLenght += allCellWidth[i];
            }

            if (mScrollType == ScrollType.Horizontal) {
                var scrollDic = scrollLenght / (content.sizeDelta.x - rectTransform.rect.width);

                horizontalNormalizedPosition = scrollDic;

                if (horizontalNormalizedPosition > 1) {
                    horizontalNormalizedPosition = 1;

                    float w = 0;
                    for (int i = allCellWidth.Count - 1; i >= 0; i--) {
                        w += allCellWidth[i];
                        if (w >= rectTransform.rect.width) {
                            cellIndex = i;
                            break;
                        }
                    }

                    scrollLenght = content.sizeDelta.x - rectTransform.rect.width - (allCellWidth.Count - cellIndex) * Spacing;
                }

                if (horizontalNormalizedPosition < 0) {
                    horizontalNormalizedPosition = 0;
                    cellIndex = 0;
                }

                float pos_x = scrollLenght;
                int len = cellList.Count;
                for (int i = 0; i < len; i++) {
                    var cellInfo = cellList[i];
                    cellInfo.cellIndex = cellIndex + i;
                    cellInfo.cellGO.transform.localPosition = new Vector3(pos_x, 0, 0);
                    cellInfo.pos = pos_x;
                    cellInfo.cellGO.name = $"cell{i}";
                    if (cellInfo.cellIndex < allCellWidth.Count) {
                        pos_x += allCellWidth[cellInfo.cellIndex];
                    }
                }

                drageChange(new Vector2(-1, 0));
            }
            else {
                var scrollDic = 1 - scrollLenght / (content.sizeDelta.y - rectTransform.rect.height);

                verticalNormalizedPosition = scrollDic;

                if (verticalNormalizedPosition > 1) {
                    verticalNormalizedPosition = 1;
                    cellIndex = 0;
                }

                if (verticalNormalizedPosition < 0) {
                    verticalNormalizedPosition = 0;

                    float w = 0;
                    for (int i = allCellWidth.Count - 1; i >= 0; i--) {
                        w += allCellWidth[i];
                        if (w >= rectTransform.rect.height) {
                            cellIndex = i;
                            break;
                        }
                    }

                    scrollLenght = content.sizeDelta.y - rectTransform.rect.height - (allCellWidth.Count - cellIndex) * Spacing;
                }

                float pos_y = -scrollLenght;
                int len = cellList.Count;
                for (int i = 0; i < len; i++) {
                    var cellInfo = cellList[i];
                    cellInfo.cellIndex = cellIndex + i;
                    cellInfo.cellGO.transform.localPosition = new Vector3(0, pos_y, 0);
                    cellInfo.pos = pos_y;
                    cellInfo.cellGO.name = $"cell{i}";
                    if (cellInfo.cellIndex < allCellWidth.Count) {
                        pos_y -= allCellWidth[cellInfo.cellIndex];
                    }
                }

                drageChange(new Vector2(0, 1));
            }

            refresh();
        }

        /// <summary>
        /// 滚动到第几个数据.注意：存在变长item时，需要在初始化时传入计算所有item长度的函数 refreshItemWidthCallBack
        /// </summary>
        /// <param name="dataIndex">滚动到的数据index</param>
        /// <param name="duration">滚动花费时间</param>
        public void ScrollTo(int dataIndex, float duration = 0.5f)
        {
            if (cellLen <= 0 || content == null) {
                return;
            }

            if (dataIndex > dataLen || dataIndex < 0) {
                return;
            }

            var cellIndex = dataIndex / lineItemNum;

            float scrollLenght = 0;
            for (int i = 0; i < cellIndex; i++) {
                scrollLenght += allCellWidth[i];
            }

            if (mScrollType == ScrollType.Horizontal) {
                var scrollDic = scrollLenght / (content.sizeDelta.x - rectTransform.rect.width);
                if (scrollDic > 1) {
                    scrollDic = 1;
                }
                //this.DOHorizontalNormalizedPos(scrollDic, duration); //此处使用dotween做动画，也可根据项目自行实现
            }
            else {
                var scrollDic = 1 - scrollLenght / (content.sizeDelta.y - rectTransform.rect.height);
                if (scrollDic < 0) {
                    scrollDic = 0;
                }
                //this.DOVerticalNormalizedPos(scrollDic, duration); //此处使用dotween做动画，也可根据项目自行实现
            }
        }

        public List<ItemInfo> GetAllItemInfos()
        {
            List<ItemInfo> itemInfos = new List<ItemInfo>();
            int len = cellList.Count;
            for (int i = 0; i < len; i++) {
                var cellInfo = cellList[i];
                for (int j = 0; j < cellInfo.itemList.Count; j++) {
                    var itemInfo = cellInfo.itemList[j];
                    if (itemInfo.itemGO.activeSelf) {
                        itemInfos.Add(itemInfo);
                    }
                }
            }

            return itemInfos;
        }

        public ItemInfo GetItemInfoByIndex(int dataIndex)
        {
            int len = cellList.Count;
            for (int i = 0; i < len; i++) {
                var cellInfo = cellList[i];
                for (int j = 0; j < cellInfo.itemList.Count; j++) {
                    var itemInfo = cellInfo.itemList[j];
                    if (itemInfo.itemIndex == dataIndex) {
                        return itemInfo;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 刷新ScrollView
        /// </summary>
        private void refresh()
        {
            int len = cellList.Count;
            for (int i = 0; i < len; i++) {
                var cellInfo = cellList[i];
                refreshCell(cellInfo);
            }
        }

        //交互变长，N个变长(不互斥)
        public void InteractiveElongate()
        {
            //this.DOKill();
            StopMovement();

            if (cellList != null) {
                int len = cellList.Count;
                if (len > 0) {
                    for (int i = 0; i < len; i++) {
                        var cellInfo = cellList[i];
                        refreshCell(cellInfo);
                    }

                    len = allCellWidth.Count;
                    if (mScrollType == ScrollType.Horizontal) {
                        float startPos = 0;
                        for (int i = 0; i < len; i++) {
                            for (int j = 0; j < cellList.Count; j++) {
                                var cellInfo = cellList[j];
                                if (i == cellInfo.cellIndex) {
                                    cellInfo.cellGO.transform.localPosition = new Vector3(startPos, 0, 0);
                                    cellInfo.pos = startPos;
                                    break;
                                }
                            }

                            startPos += allCellWidth[i];
                        }
                    }
                    else {
                        float startPos = 0;
                        for (int i = 0; i < len; i++) {
                            for (int j = 0; j < cellList.Count; j++) {
                                var cellInfo = cellList[j];
                                if (i == cellInfo.cellIndex) {
                                    cellInfo.cellGO.transform.localPosition = new Vector3(0, startPos, 0);
                                    cellInfo.pos = startPos;
                                    break;
                                }
                            }

                            startPos -= allCellWidth[i];
                        }
                    }
                }
            }
        }

        //交互变长，只有一个是变长，其他都是默认长度(互斥)
        public void InteractiveElongateOne()
        {
            //this.DOKill();
            StopMovement();

            if (cellList != null) {
                int len = cellList.Count;
                if (len > 0) {
                    len = allCellWidth.Count;
                    if (mScrollType == ScrollType.Horizontal) {
                        for (int i = 0; i < len; i++) {
                            allCellWidth[i] = item_size.x + Spacing;
                        }
                    }
                    else {
                        for (int i = 0; i < len; i++) {
                            allCellWidth[i] = item_size.y + Spacing;
                        }
                    }

                    len = cellList.Count;
                    for (int i = 0; i < len; i++) {
                        var cellInfo = cellList[i];
                        refreshCell(cellInfo);
                    }

                    len = allCellWidth.Count;
                    if (mScrollType == ScrollType.Horizontal) {
                        float startPos = 0;
                        for (int i = 0; i < len; i++) {
                            for (int j = 0; j < cellList.Count; j++) {
                                var cellInfo = cellList[j];
                                if (i == cellInfo.cellIndex) {
                                    cellInfo.cellGO.transform.localPosition = new Vector3(startPos, 0, 0);
                                    cellInfo.pos = startPos;
                                    break;
                                }
                            }

                            startPos += allCellWidth[i];
                        }
                    }
                    else {
                        float startPos = 0;
                        for (int i = 0; i < len; i++) {
                            for (int j = 0; j < cellList.Count; j++) {
                                var cellInfo = cellList[j];
                                if (i == cellInfo.cellIndex) {
                                    cellInfo.cellGO.transform.localPosition = new Vector3(0, startPos, 0);
                                    cellInfo.pos = startPos;
                                    break;
                                }
                            }

                            startPos -= allCellWidth[i];
                        }
                    }
                }
            }
        }

        //还原所有交互变长 至 默认长度
        public void ResetInteractiveElongate()
        {
            int len = allCellWidth.Count;
            if (mScrollType == ScrollType.Horizontal) {
                for (int i = 0; i < len; i++) {
                    allCellWidth[i] = item_size.x + Spacing;
                }
            }
            else {
                for (int i = 0; i < len; i++) {
                    allCellWidth[i] = item_size.y + Spacing;
                }
            }

            if (cellList != null) {
                len = cellList.Count;
                if (len > 0) {
                    for (int i = 0; i < len; i++) {
                        var cellInfo = cellList[i];

                        int indexStart = cellInfo.cellIndex * lineItemNum;
                        for (int j = 0; j < cellInfo.itemList.Count; j++) {
                            var itemInfo = cellInfo.itemList[j];
                            itemInfo.itemIndex = j + indexStart;
                            if (itemInfo.itemIndex >= dataLen) {
                                itemInfo.itemGO.SetActive(false);
                            }
                            else {
                                refreshItem(itemInfo);
                                itemInfo.itemGO.SetActive(true);
                            }

                            if (mScrollType == ScrollType.Horizontal) {
                                Vector2 sizeDelta = itemInfo.itemRectTransform.sizeDelta;
                                itemInfo.itemRectTransform.sizeDelta = new Vector2(item_size.x, sizeDelta.y);
                            }
                            else {
                                Vector2 sizeDelta = itemInfo.itemRectTransform.sizeDelta;
                                itemInfo.itemRectTransform.sizeDelta = new Vector2(sizeDelta.x, item_size.y);
                            }
                        }

                        adjustCellWidth(cellInfo);
                    }
                }
            }

            len = allCellWidth.Count;
            if (mScrollType == ScrollType.Horizontal) {
                float startPos = 0;
                for (int i = 0; i < len; i++) {
                    for (int j = 0; j < cellList.Count; j++) {
                        var cellInfo = cellList[j];
                        if (i == cellInfo.cellIndex) {
                            cellInfo.cellGO.transform.localPosition = new Vector3(startPos, 0, 0);
                            cellInfo.pos = startPos;
                            break;
                        }
                    }

                    startPos += allCellWidth[i];
                }
            }
            else {
                float startPos = 0;
                for (int i = 0; i < len; i++) {
                    for (int j = 0; j < cellList.Count; j++) {
                        var cellInfo = cellList[j];
                        if (i == cellInfo.cellIndex) {
                            cellInfo.cellGO.transform.localPosition = new Vector3(0, startPos, 0);
                            cellInfo.pos = startPos;
                            break;
                        }
                    }

                    startPos -= allCellWidth[i];
                }
            }
        }

        public void Clear()
        {
            lineItemNum = 0;
            dataLen = 0;
            cellLen = 0;
            scrollEndCallBack = null;
            refreshItemCallBack = null;
            onValueChanged.RemoveAllListeners();

            if (allCellWidth != null) {
                allCellWidth.Clear();
            }

            allCellWidth = null;

            if (cellList != null) {
                for (int i = cellList.Count - 1; i >= 0; i--) {
                    var cellInfo = cellList[i];
                    DestroyImmediate(cellInfo.cellGO);
                    cellInfo = null;
                }
            }

            cellList = null;
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }
    }
}