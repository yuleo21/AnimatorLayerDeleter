using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace AnimatorLayerDeleterTool
{
    /// <summary>
    /// Avatar Descriptor と同じ GameObject に付けて使用する。
    /// 指定した Playable Layer 種別のレイヤーをビルド時に非破壊で削除する。
    /// コンポーネント自身もビルド時に削除される。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Animator Layer Deleter")]
    public class AnimatorLayerDeleter : MonoBehaviour, IEditorOnly
    {
        /// <summary>
        /// 削除対象のエントリ一覧。
        /// 各エントリは Playable Layer 種別とレイヤー名を指定する。
        /// </summary>
        public List<LayerEntry> entries = new List<LayerEntry>();

        [Serializable]
        public class LayerEntry
        {
            /// <summary>
            /// 対象の Playable Layer 種別。
            /// </summary>
            public PlayableLayerType layerType = PlayableLayerType.FX;

            /// <summary>
            /// 削除するレイヤー名。
            /// </summary>
            public string layerName = "";
        }

        /// <summary>
        /// Playable Layer 種別。VRCAvatarDescriptor.AnimLayerType と対応する。
        /// </summary>
        public enum PlayableLayerType
        {
            Base = 0,
            Additive = 1,
            Gesture = 2,
            Action = 3,
            FX = 4,
        }
    }
}
