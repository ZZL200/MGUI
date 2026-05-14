#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using System;
using System.Reflection;
using UnityEngine;
using MGUI;

[CustomEditor(typeof(MButton))]//指定我们要自定义编辑器的脚本 
[CanEditMultipleObjects]//支持同时选中多个对象修改参数
public class MButtonEditor : ButtonEditor
{
    private SerializedProperty voiceName;
    private SerializedProperty isOpenClickedCD;
    private SerializedProperty clickedCDTime;
    private SerializedProperty mButtonGroup;
    private SerializedProperty selectShow;

    private static MButton m_Target;

    protected override void OnEnable()
    {
        base.OnEnable();
        m_Target = target as MButton;
        
        voiceName = serializedObject.FindProperty("voiceName");
        isOpenClickedCD = serializedObject.FindProperty("isOpenClickedCD");
        clickedCDTime = serializedObject.FindProperty("clickedCDTime");
        mButtonGroup = serializedObject.FindProperty("mButtonGroup");
        selectShow = serializedObject.FindProperty("selectShow");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        EditorGUILayout.PropertyField(voiceName);
        EditorGUILayout.Space();//空行
        
        EditorGUILayout.PropertyField(isOpenClickedCD);
        if (m_Target.isOpenClickedCD)
        {
            EditorGUILayout.PropertyField(clickedCDTime);
        }

        EditorGUILayout.Space();//空行
        EditorGUILayout.Space();//空行
        EditorGUILayout.Space();//空行
        EditorGUILayout.Space();//空行

        EditorGUILayout.LabelField("按钮当成Toggle使用");
        EditorGUILayout.PropertyField(mButtonGroup);
        EditorGUILayout.PropertyField(selectShow);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif