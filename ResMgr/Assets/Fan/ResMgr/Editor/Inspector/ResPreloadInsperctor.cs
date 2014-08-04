using UnityEditor;

namespace Fan.ResMgr.Editor.Inspector
{
    [CustomEditor(typeof(Fan.ResMgr.ResPreload))]
    public class ResPreloadInsperctor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Fan.ResMgr.ResPreload resPreload = target as Fan.ResMgr.ResPreload;
            EditorGUILayout.LabelField("TotalLength", resPreload.TotalLength.ToString());
        }
    }
}
