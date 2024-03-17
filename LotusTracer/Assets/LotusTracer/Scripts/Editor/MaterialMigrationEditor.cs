#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialMigration))]
public class MaterialMigrationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        if (GUILayout.Button("Migrate Materials"))
        {
            MaterialMigration migrator = (target as MaterialMigration);
            migrator.ConvertMaterialsToLotus();
        }
    }
}
#endif