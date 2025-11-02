# OpenXR_Meta_Multiplay_Prototype

## 概要
このプロジェクトは、**OpenXR Metaパッケージ** と **AR Foundation** をベースに、**Meta Questの共有空間アンカー（Shared Spatial Anchors）** 機能を活用し  
複数のデバイス間で空間位置を正確に同期させることを目的とした **マルチプレイプロトタイプ** です。

さらに、**Photon Fusion 2** を組み合わせることで、ネットワーク越しに複数プレイヤー間のリアルタイム通信と位置合わせを実現しています。

本プロジェクトは、**MRアプリケーション開発における「位置合わせ」と「ネットワーク同期」技術の実験的実装**として設計されています。

---

## 主な機能
現時点で、以下の機能を実装しています。

- 共有空間アンカーの作成・保存・読み込み
- **Photon Fusion 2** のホストモードを利用し、以下の同期を実装
    - ヘッドセット、両手・指の位置同期
    - ホストユーザーが生成した **Kinematic / Physics オブジェクト** のGrab操作と同期

---

## 利用方法
このプロジェクトを利用するには、以下のアセットをインポートする必要があります。

- [Photon Fusion 2](https://assetstore.unity.com/packages/tools/network/photon-fusion-267958)
- [Physicsアドオン](https://doc.photonengine.com/ja-jp/fusion/current/addons/physics-addon-2.0)

---

## 動作フロー
1. （事前準備）ホストユーザーがアプリを起動し、共有空間アンカーを作成
2. ホストユーザーが共有空間アンカーを読み込み、ホストとしてセッションを開始
3. クライアントユーザーが同じアンカーを読み込み、クライアントとしてセッションに参加

---

## デモ動画
[![OpenXR_Meta_Multiplay_Prototype](https://img.youtube.com/vi/tOqE8UgqFVk/0.jpg)](https://youtube.com/shorts/tOqE8UgqFVk?feature=share)  
（↑クリックでYouTubeショート動画を再生）

---

## 解説記事
本プロジェクトの詳細解説は順次公開予定です。

- [OpenXRとMeta Questではじめる！AR/MRアプリの現実世界との位置合わせ - ARアンカー編 -](https://zenn.dev/meson_tech_blog/articles/openxr-meta-anchor)

---

## 権利表記
### UniTask
MIT License (MIT)  
Copyright (c) 2019 Yoshifumi Kawai / Cysharp, Inc.

---

## ライセンス
このプロジェクトは **MIT License** のもとで公開されています。  
自由に使用・改変・再配布が可能です。

詳細は [LICENSE](./LICENSE) ファイルを参照してください。

---

## 作者
[b0bmat0ca](https://github.com/b0bmat0ca)