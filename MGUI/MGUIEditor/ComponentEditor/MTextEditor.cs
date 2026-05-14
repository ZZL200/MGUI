#if UNITY_EDITOR
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEditor.UI;

[CustomEditor(typeof(MText), true)]//指定我们要自定义编辑器的脚本
[CanEditMultipleObjects]//支持同时选中多个对象修改参数
public class MTextEditor : GraphicEditor
{
    private MText mText;
    
    SerializedProperty m_FontData;
    SerializedProperty _text;
    SerializedProperty lineHeight;
    SerializedProperty lineOffset;
    SerializedProperty lineColor;
    
    SerializedProperty gradientColor1;
    SerializedProperty gradientColor2;
    SerializedProperty gradientType;
    SerializedProperty gradientOffet;
    
    SerializedProperty shadowOffset;
    SerializedProperty shadowslant;
    SerializedProperty shadowColor;
    
    GUIContent inputGUIContent;
    
    string lastText;
    
    string[] colorText = { "白=255,255,255", "黑=0,0,0", "红=255,0,0", "绿=0,255,0" };

    protected override void OnEnable()
    {
        base.OnEnable();
        mText = (MText)target;
        
        lastText = "";
        inputGUIContent = new GUIContent("Input Text");
        
        m_FontData = serializedObject.FindProperty("m_FontData");
        _text = serializedObject.FindProperty("_text");
        lineHeight = serializedObject.FindProperty("lineHeight");
        lineOffset = serializedObject.FindProperty("lineOffset");
        lineColor = serializedObject.FindProperty("lineColor");
        
        gradientColor1 = serializedObject.FindProperty("gradientColor1");
        gradientColor2 = serializedObject.FindProperty("gradientColor2");
        gradientType = serializedObject.FindProperty("gradientType");
        gradientOffet = serializedObject.FindProperty("gradientOffet");
        
        shadowOffset = serializedObject.FindProperty("shadowOffset");
        shadowslant = serializedObject.FindProperty("shadowslant");
        shadowColor = serializedObject.FindProperty("shadowColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_text, inputGUIContent);
        EditorGUILayout.PropertyField(m_FontData);
        AppearanceControlsGUI();
        RaycastControlsGUI();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        DrawSeparatorLine();
        
        mText.UseLink = EditorGUILayout.Toggle("是否开启超链接", mText.UseLink);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        DrawSeparatorLine();
        
        mText.UseLine = EditorGUILayout.Toggle("是否开启横线", mText.UseLine);
        
        if (mText.UseLine)
        {
            EditorGUILayout.PropertyField(lineHeight);
            EditorGUILayout.PropertyField(lineOffset);
            EditorGUILayout.PropertyField(lineColor);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        DrawSeparatorLine();
        
        mText.UseGradient = EditorGUILayout.Toggle("是否开启渐变色", mText.UseGradient);
        if ( mText.UseGradient)
        {
            EditorGUILayout.PropertyField(gradientColor1);
            EditorGUILayout.PropertyField(gradientColor2);
            EditorGUILayout.PropertyField(gradientType);
            EditorGUILayout.PropertyField(gradientOffet);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        DrawSeparatorLine();
        
        mText.UseShadow = EditorGUILayout.Toggle("是否开启阴影", mText.UseShadow);
        if ( mText.UseShadow)
        {
            EditorGUILayout.PropertyField(shadowOffset);
            EditorGUILayout.PropertyField(shadowslant);
            EditorGUILayout.PropertyField(shadowColor);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        DrawSeparatorLine();
        
        EditorGUILayout.LabelField("预设颜色,美术确定几种颜色后选择即可.");//<color=#ff0000>颜色</color>
        
        for (int j = 0; j < colorText.Length; j++)
        {
            if (GUILayout.Button(colorText[j]))
            {
                string[] colorStrs = colorText[j].Split('=')[1].Split(',');
                mText.color = new Color(float.Parse(colorStrs[0],CultureInfo.InvariantCulture), float.Parse(colorStrs[1],CultureInfo.InvariantCulture), float.Parse(colorStrs[2],CultureInfo.InvariantCulture));
                SavePerfab();
            }
        }

        serializedObject.ApplyModifiedProperties();

        if (lastText!=_text.stringValue)
        {
            mText.text = _text.stringValue;
            lastText = _text.stringValue;
        }
    }
    
    //分割线
    public void DrawSeparatorLine(float height = 1) {
        Rect rect = EditorGUILayout.GetControlRect(false, height);
        rect.height = height;
        EditorGUI.DrawRect(rect, new Color32(42, 255, 0, 255));
    }
    
    public void SavePerfab()
    {
        if (mText==null || mText.gameObject==null)
        {
            return;
        }

        if (EditorUtility.IsPersistent(mText.gameObject))
        {
            EditorUtility.SetDirty(mText.gameObject);
        }
        else
        { 
            string path = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(mText.gameObject));
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.SetDirty(mText.gameObject);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(mText.gameObject, path, InteractionMode.UserAction);
            }
        }
    }
}
#endif