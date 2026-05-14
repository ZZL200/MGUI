#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(MPolygon), true)]//指定我们要自定义编辑器的脚本 
[CanEditMultipleObjects]//支持同时选中多个对象修改参数
public class MPolygonEditor : GraphicEditor
{
    private SerializedProperty sides;
    private SerializedProperty radius;
    private SerializedProperty maxValue;
    private SerializedProperty dataValues;

    private MPolygon mPolygon;
    protected override void OnEnable()
    {
        base.OnEnable();

        mPolygon = (MPolygon)target;

        sides = serializedObject.FindProperty("sides");
        radius = serializedObject.FindProperty("radius");
        maxValue = serializedObject.FindProperty("maxValue");
        dataValues = serializedObject.FindProperty("dataValues");
    }

    protected override void OnDisable()
    {
        mPolygon = null;
        base.OnDisable();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space();//空行
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(sides);
        if(EditorGUI.EndChangeCheck())
        {
            mPolygon.sides = sides.intValue;
            while (mPolygon.dataValues.Count <= mPolygon.sides)
            {
                mPolygon.dataValues.Add(mPolygon.maxValue);
            }
            
            while (mPolygon.dataValues.Count > mPolygon.sides)
            {
                mPolygon.dataValues.RemoveAt(mPolygon.dataValues.Count - 1);
            }
        }
        
        EditorGUILayout.PropertyField(radius);
        mPolygon.radius = radius.floatValue;
        
        EditorGUILayout.PropertyField(maxValue);
        mPolygon.maxValue = maxValue.floatValue;
        
        EditorGUILayout.PropertyField(dataValues);
        
        EditorGUILayout.Space();//空行
        
        serializedObject.Update();
        
        serializedObject.ApplyModifiedProperties();

        mPolygon.SetVerticesDirty();
    }
}
#endif