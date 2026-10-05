# AnimatorLayerDeleter

A NDMF plugin that removes specified Animator layers from VRChat avatar Playable Layers at build time.  
指定した VRChat アバターの Playable Layer のレイヤーをビルド時に非破壊で削除する NDMF プラグインです。

## Installation

### Using VCC (VRChat Creator Companion) or ALCOM
1. Open [this link](https://yuleo21.github.io/21tools/)
2. Click "Add to VCC"
3. Click "Open with VCC (or ALCOM)"
4. Add **AnimatorLayerDeleter** to your avatar project.

## Usage

1. Add the **Animator Layer Deleter** component to the same GameObject as your `VRCAvatarDescriptor`.
2. Click "＋ レイヤーを追加" to add entries.
3. Select the Playable Layer type (Base / Additive / Gesture / Action / FX) and choose the layer to remove from the dropdown.
4. Build the avatar — the specified layers will be removed automatically.

## Features

- **Playable Layer Selection**: Select which Playable Layer type (Base, Additive, Gesture, Action, FX) to target.
- **Layer Dropdown**: Displays actual layer names from the AnimatorController for easy selection.
- **Multiple Entries**: Add as many layer deletion entries as needed.
- **Summary View**: Shows a summary of all layers to be deleted with existence checks.
- **NDMF Integration**: Runs before Modular Avatar during the build.
- **Non-Destructive**: Original assets are never modified; all changes happen at build time via cloning.

## Requirements

- VRChat Avatars SDK >= 3.5.0
- NDMF >= 1.5.0
