using UnityEngine;
using UnityEngine.UI;

public class MHole : MaskableGraphic, ICanvasRaycastFilter
{
    [SerializeField]
    private RectTransform _target;
    
    //扩充洞的大小
    public int _expandWidth = 0;
    public int ExpandWidth
    {
        get
        {
            return _expandWidth;
        }
        set
        {
            if (_expandWidth==value) {
                return;
            }
            _expandWidth = value;
            if (_target!=null) {
                SetTarget(_target,_offset);
            }
            else {
                SetAllDirty();
            }
        }
    }

    [SerializeField]
    private Vector2 _offset;
    public Vector2 Offset
    {
        get
        {
            return _offset;
        }
        set
        {
            if (_offset==value) {
                return;
            }
            _offset = value;
            if (_target!=null) {
                SetTarget(_target,_offset);
            }
            else {
                SetAllDirty();
            }
        }
    }
    

    private Vector3 _targetMin = Vector3.zero;
    private Vector3 _targetMax = Vector3.zero;

    private RectTransform _rectTransform = null;
 
    /// <summary>
    /// 设置镂空的目标
    /// </summary>
    /// <param name="target">镂空的目标</param>
    /// <param name="offset">洞整体偏移，例如Vector2(5,0):洞整体向右偏移5像素单位</param>
    public void SetTarget(RectTransform target,Vector2 offset = new Vector2())
    {
        if (_rectTransform==null) {
            _rectTransform = GetComponent<RectTransform>();
        }
        
        _offset = offset;
        _target = target;
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_rectTransform, _target);
        _targetMin = bounds.min;
        _targetMax = bounds.max;
        SetAllDirty();
    }
 
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if (_targetMin == Vector3.zero && _targetMax == Vector3.zero)
        {
            base.OnPopulateMesh(vh);
            return;
        }
 
        vh.Clear();
 
        // 填充顶点
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;
 
        Vector2 selfPiovt = rectTransform.pivot;
        Rect selfRect = rectTransform.rect;
        float outerLx = -selfPiovt.x * selfRect.width;
        float outerBy = -selfPiovt.y * selfRect.height;
        float outerRx = (1 - selfPiovt.x) * selfRect.width;
        float outerTy = (1 - selfPiovt.y) * selfRect.height;
        // 0 - 左上
        vert.position = new Vector3(outerLx, outerTy);
        vh.AddVert(vert);
        // 1 - 右上
        vert.position = new Vector3(outerRx, outerTy);
        vh.AddVert(vert);
        // 2 - 右下
        vert.position = new Vector3(outerRx, outerBy);
        vh.AddVert(vert);
        // 3 - 左下
        vert.position = new Vector3(outerLx, outerBy);
        vh.AddVert(vert);
        
        // 4 - 洞左上
        vert.position = new Vector3(_targetMin.x+_offset.x-_expandWidth, _targetMax.y+_offset.y+_expandWidth);
        vh.AddVert(vert);
        // 5 - 洞右上
        vert.position = new Vector3(_targetMax.x+_offset.x+_expandWidth, _targetMax.y+_offset.y+_expandWidth);
        vh.AddVert(vert);
        // 6 - 洞右下
        vert.position = new Vector3(_targetMax.x+_offset.x+_expandWidth, _targetMin.y+_offset.y-_expandWidth);
        vh.AddVert(vert);
        // 7 - 洞左下
        vert.position = new Vector3(_targetMin.x+_offset.x-_expandWidth, _targetMin.y+_offset.y-_expandWidth);
        vh.AddVert(vert);
        
        // 设定三角形
        vh.AddTriangle(4, 0, 1);
        vh.AddTriangle(4, 1, 5);
        vh.AddTriangle(5, 1, 2);
        vh.AddTriangle(5, 2, 6);
        vh.AddTriangle(6, 2, 3);
        vh.AddTriangle(6, 3, 7);
        vh.AddTriangle(7, 3, 0);
        vh.AddTriangle(7, 0, 4);
    }
 
    bool ICanvasRaycastFilter.IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
    {
        if (null == _target) return true;
        // 将目标对象范围内的事件镂空（使其穿过）
        return !RectTransformUtility.RectangleContainsScreenPoint(_target, screenPos, eventCamera);
    }
}