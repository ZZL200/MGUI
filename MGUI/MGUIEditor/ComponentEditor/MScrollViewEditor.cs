#if UNITY_EDITOR
using UnityEditor;
using MGUI;

[CustomEditor(typeof(MScrollView), true)]//指定我们要自定义编辑器的脚本
public class MScrollViewEditor : UnityEditor.UI.ScrollRectEditor
{
    private MScrollView mScrollView;
    
    SerializedProperty mScrollType;
    SerializedProperty mSpacing;

    protected override void OnEnable()
    {
        base.OnEnable();
        mScrollView = (MScrollView)target;
        
        mScrollType = serializedObject.FindProperty("mScrollType");
        mSpacing = serializedObject.FindProperty("Spacing");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(mScrollType);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(mSpacing);
        
        serializedObject.ApplyModifiedProperties();
    }
    
    public void SavePerfab()
    {
        if (mScrollView==null || mScrollView.gameObject==null)
        {
            return;
        }

        if (EditorUtility.IsPersistent(mScrollView.gameObject))
        {
            EditorUtility.SetDirty(mScrollView.gameObject);
        }
        else
        { 
            string path = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(mScrollView.gameObject));
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.SetDirty(mScrollView.gameObject);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(mScrollView.gameObject, path, InteractionMode.UserAction);
            }
        }
    }
}
#endif