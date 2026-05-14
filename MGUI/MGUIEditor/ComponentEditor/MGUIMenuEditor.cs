#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MGUI;
using UnityEngine.UI;

public class MGUIMenuEditor : Editor
{
    [MenuItem("GameObject/MGUI/MImage", false, 1000)]
    static void CreateMImage()
    {
        GameObject go = new GameObject("MImage");
        if (Selection.activeGameObject != null)
        {
            go.transform.SetParent(Selection.activeGameObject.transform);
        }
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        var mImage = go.AddComponent<MImage>();
        mImage.raycastTarget = false;
    }
    
    [MenuItem("GameObject/MGUI/MRawImage", false, 1001)]
    static void CreateMRawImage()
    {
        GameObject go = new GameObject("MRawImage");
        if (Selection.activeGameObject != null)
        {
            go.transform.SetParent(Selection.activeGameObject.transform);
        }
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        var mImage = go.AddComponent<MRawImage>();
        mImage.raycastTarget = false;
    }
    
    [MenuItem("GameObject/MGUI/MText", false, 1002)]
    static void CreateMText()
    {
        GameObject go = new GameObject("MText");
        if (Selection.activeGameObject != null)
        {
            go.transform.SetParent(Selection.activeGameObject.transform);
        }
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        var mText = go.AddComponent<MText>();
        
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        mText.font = font;
        mText.text = "Text";
        mText.raycastTarget = false;
        mText.UseLine = false;
        mText.UseLink = false;
    }
    
    [MenuItem("GameObject/MGUI/MButton", false, 1003)]
    static void CreateMButton()
    {
        GameObject go = new GameObject("MButton");
        if (Selection.activeGameObject != null)
        {
            go.transform.SetParent(Selection.activeGameObject.transform);
        }
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        var mImage = go.AddComponent<MImage>();
        mImage.raycastTarget = true;
        go.AddComponent<MButton>();
    }
    
    [MenuItem("GameObject/MGUI/MScrollView", false, 1004)]
    static void CreateMScrollView()
    {
        GameObject go = new GameObject("MScrollView");
        if (Selection.activeGameObject != null)
        {
            go.transform.SetParent(Selection.activeGameObject.transform);
        }
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.AddComponent<CanvasRenderer>();
        var mImage = go.AddComponent<TransparentGraphic>();
        mImage.raycastTarget = true;
        go.AddComponent<RectMask2D>();
        var mScrollView = go.AddComponent<MScrollView>();
        mScrollView.horizontal = false;
        mScrollView.vertical = false;
        var rectTransform = go.GetComponent<RectTransform>();
        var content = new GameObject("Content");
        var content_RT = content.AddComponent<RectTransform>();
        content_RT.SetParent(rectTransform);
        content_RT.transform.localPosition = Vector3.zero;
        content_RT.transform.localScale = Vector3.one;
        mScrollView.content = content_RT;
    }
    
    [MenuItem("GameObject/MGUI/MPolygon", false, 1005)]
    static void CreateMPolygon()
    {
        GameObject go = new GameObject("MPolygon");
        if (Selection.activeGameObject != null)
        {
            go.transform.SetParent(Selection.activeGameObject.transform);
        }
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.AddComponent<CanvasRenderer>();
        var mPolygon = go.AddComponent<MPolygon>();
        mPolygon.raycastTarget = false;
        mPolygon.SetSides(mPolygon.sides);
    }
    
    [MenuItem("GameObject/MGUI/MHole", false, 1006)]
    static void CreateMMaskHoleImage()
    {
        GameObject go = new GameObject("MHole");
        if (Selection.activeGameObject != null)
        {
            go.transform.SetParent(Selection.activeGameObject.transform);
        }
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        var mImage = go.AddComponent<MHole>();
        mImage.raycastTarget = true;
    }
}
#endif