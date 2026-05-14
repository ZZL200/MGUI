using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGUI
{
    public class MButton : Button
    {
        [Header("按钮点击音效")] public string voiceName = "";

        [Header("是否开启点击CD")] public bool isOpenClickedCD = true;

        [Header("点击CD")] public float clickedCDTime = 0.3f;

        [Header("Toggle集合")] [SerializeField] public MButtonGroup mButtonGroup;

        [Header("按钮点击后需要显示的物体")] [SerializeField]
        public GameObject selectShow;

        private bool canClicked = false;

        //按钮可携带参数
        public int paramInt;
        public string paramString = null;
        public object paramObject = null;

        public float longPressDuration = 1.0f; // 长按?秒后触发长按事件
        private bool isButtonPressed = false; // 按钮是否被按下

        private Vector2 pointerDownPos = Vector2.zero; //获取点击位置

        private Action<MButton> onPointerDown = null;
        private Action<MButton> onPointerUp = null;
        private Action<MButton> onPointerEnter = null;
        private Action<MButton> onLongPress = null;
        private Action<int> onSlideEnd = null;

        public enum Slidedirection
        {
            None = 0,
            Horizontal = 1,
            Vertical = 2
        }

        private Slidedirection slidedirection = Slidedirection.None;

        public void AddSlideEndListener(Slidedirection slidedirection, Action<int> callBack)
        {
            this.slidedirection = slidedirection;
            onSlideEnd = null;
            onSlideEnd = callBack;
        }

        public void AddPointerDownListener(Action<MButton> callBack)
        {
            onPointerDown = null;
            onPointerDown = callBack;
        }

        public void AddPointerEnterListener(Action<MButton> callBack)
        {
            onPointerEnter = null;
            onPointerEnter = callBack;
        }

        public void AddLongPressListener(Action<MButton> callBack, float duration = 0.5f)
        {
            onLongPress = null;
            onLongPress = callBack;
            longPressDuration = duration;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (canClicked) {
                base.OnPointerClick(eventData);

                //todo 调用播放音效单例
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            isButtonPressed = true;
            ToOnLongPress();

            if (onPointerDown != null) {
                onPointerDown(this);
            }

            if (onPointerEnter != null) {
                pointerDownPos = eventData.position;
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            isButtonPressed = false;

            if (onPointerUp != null) {
                onPointerUp(this);
            }

            if (onSlideEnd != null) {
                if (slidedirection == Slidedirection.Horizontal) {
                    int dir = 0;
                    if (eventData.position.x - pointerDownPos.x <= -20) {
                        dir = 1; //右滑，下一个
                    }
                    else if (eventData.position.x - pointerDownPos.x >= 20) {
                        dir = -1; //左滑，上一个
                    }

                    if (dir != 0) {
                        onSlideEnd(dir);
                    }
                }
                else if (slidedirection == Slidedirection.Vertical) {
                    int dir = 0;
                    if (eventData.position.y - pointerDownPos.y <= -20) {
                        dir = 1; //下滑，下一个
                    }
                    else if (eventData.position.y - pointerDownPos.y >= 20) {
                        dir = -1; //上滑，上一个
                    }

                    if (dir != 0) {
                        onSlideEnd(dir);
                    }
                }
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (onPointerEnter != null) {
                onPointerEnter(this);
            }
        }

        private void ToOnLongPress()
        {
            Invoke("OnLongPress", longPressDuration);
        }

        private void OnLongPress()
        {
            if (isButtonPressed) {
                // 触发长按事件
                if (onLongPress != null) {
                    onLongPress(this);
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (mButtonGroup != null) {
                if (!mButtonGroup.buttonGroup.Contains(this)) {
                    mButtonGroup.buttonGroup.Add(this);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnClickedTime();
        }

        private void OnClickedTime()
        {
            canClicked = false;
            Invoke("OnClickCDEnd", clickedCDTime);
        }

        private void OnClickCDEnd()
        {
            canClicked = true;
        }

        private Action<MButton> onClickCallBack = null;

        public void RemoveAllListeners()
        {
            onClickCallBack = null;
            onClick.RemoveAllListeners();
        }

        public void AddListener(Action<MButton> callBack)
        {
            RemoveAllListeners();
            onClickCallBack = callBack;
            onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (canClicked) {
                if (isOpenClickedCD) {
                    OnClickedTime();
                }

                if (onClickCallBack != null) {
                    onClickCallBack(this);
                }
            }
        }

        public bool HasBeenClicked() //返回true直接跳出;返回false可继续执行按钮逻辑
        {
            if (mButtonGroup != null) {
                if (mButtonGroup.currMButton == this) {
                    return true;
                }

                if (mButtonGroup.currMButton != null && mButtonGroup.currMButton.selectShow != null) {
                    mButtonGroup.currMButton.selectShow.SetActive(false);
                }

                mButtonGroup.currMButton = this;

                if (selectShow != null) {
                    selectShow.SetActive(true);
                }
            }

            return false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            onClick.RemoveAllListeners();
        }
    }
}