//#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(MHole), true)] //指定我们要自定义编辑器的脚本 
[CanEditMultipleObjects] //支持同时选中多个对象修改参数
public class MHoleEditor : GraphicEditor
{
    private SerializedProperty _target;
    private SerializedProperty _expandWidth;
    private SerializedProperty _offset;

    private MHole mTarget;

    protected override void OnEnable()
    {
        base.OnEnable();

        mTarget = (MHole)target;
        _target = serializedObject.FindProperty("_target");
        _expandWidth = serializedObject.FindProperty("_expandWidth");
        _offset = serializedObject.FindProperty("_offset");
    }

    protected override void OnDisable()
    {
        mTarget = null;
        base.OnDisable();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space(); //空行
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(_target);

        EditorGUILayout.PropertyField(_expandWidth);
        mTarget.ExpandWidth = _expandWidth.intValue;

        EditorGUILayout.PropertyField(_offset);
        mTarget.Offset = _offset.vector2Value;
        
        EditorGUILayout.Space(); //空行

        serializedObject.ApplyModifiedProperties();
    }
}
//#endif