using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(AnimatorLayerDeleterTool.AnimatorLayerDeleterPlugin))]

namespace AnimatorLayerDeleterTool
{
    public class AnimatorLayerDeleterPlugin : Plugin<AnimatorLayerDeleterPlugin>
    {
        public override string QualifiedName => "net.yuleo21.animatorlayerdeleter";
        public override string DisplayName => "Animator Layer Deleter";

        protected override void Configure()
        {
            // Modular Avatar がレイヤーを統合する前に実行する
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Animator Layer Deleter", Execute);
        }

        private static void Execute(BuildContext ctx)
        {
            var root = ctx.AvatarRootObject;
            var components = root.GetComponentsInChildren<AnimatorLayerDeleter>(true);
            if (components.Length == 0) return;

            var descriptor = ctx.AvatarDescriptor as VRCAvatarDescriptor;
            if (descriptor == null)
            {
                Debug.LogWarning(
                    "[Animator Layer Deleter] VRCAvatarDescriptor が見つかりません。処理をスキップします。");
                return;
            }

            // 全コンポーネントからエントリを集約
            var allEntries = components
                .SelectMany(c => c.entries)
                .Where(e => e != null && !string.IsNullOrEmpty(e.layerName))
                .ToList();

            if (allEntries.Count == 0) goto cleanup;

            // Playable Layer 種別ごとにグルーピング
            var grouped = allEntries
                .GroupBy(e => e.layerType)
                .ToDictionary(g => g.Key, g => g.Select(e => e.layerName).Distinct().ToHashSet());

            // 各 Playable Layer を処理
            ProcessLayers(ctx, descriptor, descriptor.baseAnimationLayers, grouped);
            ProcessLayers(ctx, descriptor, descriptor.specialAnimationLayers, grouped);

            cleanup:
            // コンポーネント自己削除
            foreach (var c in components)
            {
                Object.DestroyImmediate(c);
            }
        }

        /// <summary>
        /// AnimLayerType から PlayableLayerType へのマッピング。
        /// </summary>
        private static AnimatorLayerDeleter.PlayableLayerType? ToPlayableLayerType(
            VRCAvatarDescriptor.AnimLayerType animLayerType)
        {
            switch (animLayerType)
            {
                case VRCAvatarDescriptor.AnimLayerType.Base:
                    return AnimatorLayerDeleter.PlayableLayerType.Base;
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                    return AnimatorLayerDeleter.PlayableLayerType.Additive;
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                    return AnimatorLayerDeleter.PlayableLayerType.Gesture;
                case VRCAvatarDescriptor.AnimLayerType.Action:
                    return AnimatorLayerDeleter.PlayableLayerType.Action;
                case VRCAvatarDescriptor.AnimLayerType.FX:
                    return AnimatorLayerDeleter.PlayableLayerType.FX;
                default:
                    return null;
            }
        }

        /// <summary>
        /// baseAnimationLayers または specialAnimationLayers を処理する。
        /// </summary>
        private static void ProcessLayers(
            BuildContext ctx,
            VRCAvatarDescriptor descriptor,
            VRCAvatarDescriptor.CustomAnimLayer[] animLayers,
            Dictionary<AnimatorLayerDeleter.PlayableLayerType, HashSet<string>> grouped)
        {
            for (int i = 0; i < animLayers.Length; i++)
            {
                var controller = animLayers[i].animatorController as AnimatorController;
                if (controller == null) continue;

                var playableType = ToPlayableLayerType(animLayers[i].type);
                if (playableType == null) continue;

                if (!grouped.TryGetValue(playableType.Value, out var targetNames)) continue;
                if (targetNames.Count == 0) continue;

                // コントローラーを複製して処理（非破壊）
                var clone = Object.Instantiate(controller);
                clone.name = controller.name;
                ctx.AssetSaver.SaveAsset(clone);

                var layers = clone.layers.ToList();
                var removed = new List<string>();

                for (int j = layers.Count - 1; j >= 0; j--)
                {
                    if (targetNames.Contains(layers[j].name))
                    {
                        removed.Add(layers[j].name);
                        layers.RemoveAt(j);
                    }
                }

                if (removed.Count == 0) continue;

                clone.layers = layers.ToArray();

                // Descriptor のコントローラー参照を更新
                animLayers[i].animatorController = clone;

                Debug.Log(
                    $"[Animator Layer Deleter] {playableType.Value} ({clone.name}): レイヤーを削除しました → {string.Join(", ", removed)}");
            }

            // 構造体配列なので Descriptor に書き戻す
            if (animLayers == descriptor.baseAnimationLayers)
                descriptor.baseAnimationLayers = animLayers;
            else
                descriptor.specialAnimationLayers = animLayers;
        }
    }
}
