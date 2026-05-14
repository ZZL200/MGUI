#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using MGUI;

[CustomEditor(typeof(MRawImage), true)]//指定我们要自定义编辑器的脚本 
[CanEditMultipleObjects]//支持同时选中多个对象修改参数
public class MRawImageEditor : RawImageEditor
{
    private SerializedProperty isCircle;
    private SerializedProperty circleSegments;
    private SerializedProperty circleFeather;
    private SerializedProperty angle;
    private SerializedProperty flip;

    private MRawImage mImage;
    protected override void OnEnable()
    {
        base.OnEnable();

        mImage = (MRawImage)target;
        
        isCircle = serializedObject.FindProperty("isCircle");
        circleSegments = serializedObject.FindProperty("circleSegments");
        circleFeather = serializedObject.FindProperty("circleFeather");
        angle = serializedObject.FindProperty("angle");
        flip = serializedObject.FindProperty("flip");
    }

    protected override void OnDisable()
    {
        mImage = null;
        base.OnDisable();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        
        EditorGUILayout.Space();//空行
        EditorGUILayout.Space();//空行
        EditorGUILayout.Space();//空行
        
        EditorGUILayout.PropertyField(isCircle);
        EditorGUILayout.Space();//空行
        EditorGUILayout.PropertyField(circleSegments);
        EditorGUILayout.PropertyField(circleFeather);
        EditorGUILayout.Space();//空行
        
        EditorGUILayout.PropertyField(angle);
        
        EditorGUILayout.Space();//空行
        
        
        EditorGUILayout.PropertyField(flip);
        mImage.Flip = (ImageFlip)flip.intValue;
        
        EditorGUILayout.Space();//空行
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif