#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Кнопка «Заполнить из спрайтов» в ассете Door Style.
// Находит в проекте спрайты с именами:
//   Door_<Тип>_<Up|Right|Down>_<номер кадра>   (тип - как в RoomType, плюс Entrance для входа)
//   Lock_<Up|Right|Down>_<номер кадра>
// и раскладывает их по полям, чтобы не перетаскивать 130 картинок руками.
// Этот файл должен лежать в папке с названием Editor.
[CustomEditor(typeof(DoorStyle))]
public class DoorStyleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Кнопка ищет в проекте спрайты Door_Тип_Сторона_Кадр и Lock_Сторона_Кадр " +
                                "(Сторона: Up, Right, Down) и заполняет все кадры.", MessageType.Info);
        if (GUILayout.Button("Заполнить из спрайтов", GUILayout.Height(28)))
            Fill((DoorStyle)target);
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    static void Fill(DoorStyle style)
    {
        // имя -> кадры по номеру
        var doors = new Dictionary<string, SortedDictionary<int, Sprite>>();
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!file.StartsWith("Door_") && !file.StartsWith("Lock_")) continue;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;
            int cut = file.LastIndexOf('_');
            if (cut < 0 || !int.TryParse(file.Substring(cut + 1), out int frame)) continue;
            string key = file.Substring(0, cut);                      // Door_Combat_Up / Lock_Right
            if (!doors.TryGetValue(key, out var frames)) doors[key] = frames = new SortedDictionary<int, Sprite>();
            frames[frame] = sprite;
        }

        Undo.RecordObject(style, "Заполнить двери");
        var list = new List<TypeDoorFrames>();
        foreach (RoomType t in Enum.GetValues(typeof(RoomType)))
        {
            DoorFrames f = Make(doors, "Door_" + t);
            if (f != null) list.Add(new TypeDoorFrames { type = t, frames = f });
        }
        style.doors = list.ToArray();
        style.entrance = Make(doors, "Door_Entrance") ?? new DoorFrames();
        style.lockFrames = Make(doors, "Lock") ?? new DoorFrames();
        EditorUtility.SetDirty(style);
        AssetDatabase.SaveAssets();
        Debug.Log($"Door Style: заполнено типов дверей - {list.Count}");
    }

    static DoorFrames Make(Dictionary<string, SortedDictionary<int, Sprite>> all, string prefix)
    {
        Sprite[] up = Get(all, prefix + "_Up"), side = Get(all, prefix + "_Right"), down = Get(all, prefix + "_Down");
        if (up == null && side == null && down == null) return null;
        return new DoorFrames { up = up, side = side, down = down };
    }

    static Sprite[] Get(Dictionary<string, SortedDictionary<int, Sprite>> all, string key)
    {
        return all.TryGetValue(key, out var frames) ? new List<Sprite>(frames.Values).ToArray() : null;
    }
}
#endif
