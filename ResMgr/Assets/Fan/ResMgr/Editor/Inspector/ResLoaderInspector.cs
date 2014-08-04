using UnityEditor;
namespace Fan.ResMgr.Editor.Inspector
{
    [CustomEditor(typeof(Fan.ResMgr.ResLoader))]
    public class ResLoaderInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Fan.ResMgr.ResLoader resLoader = target as Fan.ResMgr.ResLoader;
            EditorGUILayout.LabelField("ResObj", resLoader.ResObj.ToString());
            EditorGUILayout.Toggle("FormLocal", resLoader.FormLocal);
            EditorGUILayout.LabelField("ReLoadNum", resLoader.ReLoadNum.ToString());
            EditorGUILayout.LabelField("State", resLoader.State.ToString());
            EditorGUILayout.LabelField("CurrentProgress", resLoader.CurrentProgress.ToString());
        }
    }

}
