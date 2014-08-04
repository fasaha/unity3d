using UnityEditor;

namespace Fan.ResMgr.Editor.Inspector
{
    [CustomEditor(typeof(Fan.ResMgr.ResMgr))]
    public class ResMgrInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Fan.ResMgr.ResMgr resMgr = target as Fan.ResMgr.ResMgr;
            if (resMgr.ResDic != null)
            {
                EditorGUILayout.LabelField("ResDicCount", resMgr.ResDic.Count.ToString());
                int i = 0;
                foreach (var kv in resMgr.ResDic)
                {
                    EditorGUILayout.LabelField((++i).ToString(), kv.Key);
                }
            }
            
           
        }
    }
}
