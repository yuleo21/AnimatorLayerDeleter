using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace AnimatorLayerDeleterTool
{
    [CustomEditor(typeof(AnimatorLayerDeleter))]
    public class AnimatorLayerDeleterEditor : Editor
    {
        // Playable Layer 種別の表示名
        private static readonly string[] PlayableLayerTypeNames = { "Base", "Additive", "Gesture", "Action", "FX" };

        public override void OnInspectorGUI()
        {
            var comp = (AnimatorLayerDeleter)target;
            var descriptor = comp.GetComponent<VRCAvatarDescriptor>();

            if (descriptor == null)
            {
                EditorGUILayout.HelpBox(
                    "VRCAvatarDescriptor が付いた GameObject に追加してください。",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "指定した Playable Layer のレイヤーをビルド時に非破壊で削除します。\n" +
                "元のアセットは一切変更されません。",
                MessageType.None);
            EditorGUILayout.Space(4);

            // エントリ一覧
            EditorGUILayout.LabelField("削除対象レイヤー", EditorStyles.boldLabel);

            for (int i = 0; i < comp.entries.Count; i++)
            {
                var entry = comp.entries[i];
                DrawEntry(comp, descriptor, entry, i);
            }

            EditorGUILayout.Space(4);

            if (GUILayout.Button("＋ レイヤーを追加"))
            {
                Undo.RecordObject(comp, "Add Layer Entry");
                comp.entries.Add(new AnimatorLayerDeleter.LayerEntry());
                EditorUtility.SetDirty(comp);
            }

            EditorGUILayout.Space(8);

            // サマリー表示
            DrawSummary(comp, descriptor);
        }

        private void DrawEntry(AnimatorLayerDeleter comp, VRCAvatarDescriptor descriptor,
            AnimatorLayerDeleter.LayerEntry entry, int index)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Playable Layer 種別
                    EditorGUI.BeginChangeCheck();
                    var newType = (AnimatorLayerDeleter.PlayableLayerType)EditorGUILayout.Popup(
                        "Playable Layer",
                        (int)entry.layerType,
                        PlayableLayerTypeNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(comp, "Change Playable Layer Type");
                        entry.layerType = newType;
                        entry.layerName = ""; // 種別変更時はレイヤー名をリセット
                        EditorUtility.SetDirty(comp);
                    }

                    // 削除ボタン
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        Undo.RecordObject(comp, "Remove Layer Entry");
                        comp.entries.RemoveAt(index);
                        EditorUtility.SetDirty(comp);
                        GUIUtility.ExitGUI();
                        return;
                    }
                }

                // レイヤー名（ドロップダウン or テキスト入力）
                var controller = GetControllerForType(descriptor, entry.layerType);
                if (controller != null)
                {
                    var layerNames = controller.layers.Select(l => l.name).ToArray();
                    if (layerNames.Length > 0)
                    {
                        int currentIndex = System.Array.IndexOf(layerNames, entry.layerName);
                        if (currentIndex < 0) currentIndex = 0;

                        EditorGUI.BeginChangeCheck();
                        int newIndex = EditorGUILayout.Popup("レイヤー名", currentIndex, layerNames);
                        if (EditorGUI.EndChangeCheck() || string.IsNullOrEmpty(entry.layerName))
                        {
                            Undo.RecordObject(comp, "Change Layer Name");
                            entry.layerName = layerNames[newIndex];
                            EditorUtility.SetDirty(comp);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("レイヤーがありません。", MessageType.Info);
                    }
                }
                else
                {
                    // コントローラーが未設定の場合はテキスト入力
                    EditorGUI.BeginChangeCheck();
                    var newName = EditorGUILayout.TextField("レイヤー名", entry.layerName);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(comp, "Change Layer Name");
                        entry.layerName = newName;
                        EditorUtility.SetDirty(comp);
                    }

                    EditorGUILayout.HelpBox(
                        $"{PlayableLayerTypeNames[(int)entry.layerType]} にコントローラーが設定されていません。",
                        MessageType.Warning);
                }
            }
        }

        private void DrawSummary(AnimatorLayerDeleter comp, VRCAvatarDescriptor descriptor)
        {
            if (comp.entries.Count == 0) return;

            var validEntries = comp.entries
                .Where(e => e != null && !string.IsNullOrEmpty(e.layerName))
                .ToList();

            if (validEntries.Count == 0) return;

            EditorGUILayout.LabelField("削除サマリー", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Playable Layer 種別ごとにグルーピング
                var grouped = validEntries
                    .GroupBy(e => e.layerType)
                    .OrderBy(g => (int)g.Key);

                foreach (var group in grouped)
                {
                    var typeName = PlayableLayerTypeNames[(int)group.Key];
                    var controller = GetControllerForType(descriptor, group.Key);
                    var layerNames = group.Select(e => e.layerName).Distinct().ToList();

                    EditorGUILayout.LabelField($"▸ {typeName}", EditorStyles.boldLabel);

                    foreach (var name in layerNames)
                    {
                        bool exists = controller != null &&
                                      controller.layers.Any(l => l.name == name);

                        var prev = GUI.color;
                        if (!exists) GUI.color = new Color(1f, 0.5f, 0.5f);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16);
                            GUILayout.Label(exists ? "✓" : "✗", GUILayout.Width(16));
                            GUILayout.Label(name);
                            if (!exists)
                                GUILayout.Label("(見つかりません)", EditorStyles.miniLabel);
                        }

                        GUI.color = prev;
                    }
                }
            }
        }

        /// <summary>
        /// PlayableLayerType に対応する AnimatorController を取得する。
        /// </summary>
        private static AnimatorController GetControllerForType(
            VRCAvatarDescriptor descriptor,
            AnimatorLayerDeleter.PlayableLayerType playableType)
        {
            var targetAnimLayerType = ToAnimLayerType(playableType);
            if (targetAnimLayerType == null) return null;

            foreach (var layer in descriptor.baseAnimationLayers)
            {
                if (layer.type == targetAnimLayerType.Value)
                    return layer.animatorController as AnimatorController;
            }
            foreach (var layer in descriptor.specialAnimationLayers)
            {
                if (layer.type == targetAnimLayerType.Value)
                    return layer.animatorController as AnimatorController;
            }

            return null;
        }

        private static VRCAvatarDescriptor.AnimLayerType? ToAnimLayerType(
            AnimatorLayerDeleter.PlayableLayerType playableType)
        {
            switch (playableType)
            {
                case AnimatorLayerDeleter.PlayableLayerType.Base:
                    return VRCAvatarDescriptor.AnimLayerType.Base;
                case AnimatorLayerDeleter.PlayableLayerType.Additive:
                    return VRCAvatarDescriptor.AnimLayerType.Additive;
                case AnimatorLayerDeleter.PlayableLayerType.Gesture:
                    return VRCAvatarDescriptor.AnimLayerType.Gesture;
                case AnimatorLayerDeleter.PlayableLayerType.Action:
                    return VRCAvatarDescriptor.AnimLayerType.Action;
                case AnimatorLayerDeleter.PlayableLayerType.FX:
                    return VRCAvatarDescriptor.AnimLayerType.FX;
                default:
                    return null;
            }
        }
    }
}
