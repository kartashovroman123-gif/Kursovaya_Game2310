#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Кнопки для Level Preview в Inspector. Этот файл должен лежать в папке с названием Editor.
[CustomEditor(typeof(LevelPreview))]
public class LevelPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var p = (LevelPreview)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Сгенерировать уровень", GUILayout.Height(28))) p.Generate();
        if (GUILayout.Button("Сгенерировать весь забег (сводка в Console)")) p.GenerateWholeRun();

        if (p.Layout != null)
            EditorGUILayout.HelpBox(p.Layout.ToString(), MessageType.None);
    }
}
#endif
