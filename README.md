# Sand Tetris Online

砂テトリス(Sand Tetris)のUnity実装 + オンライン対戦版。
ライン消去で相手に妨害を送り合う1対1対戦を目指すプロジェクト。

前身のシングルプレイ版はこちら: (別リポジトリ or `v1/` ブランチなどにリンクを追記)

## アーキテクチャ

MVP(Model-View-Presenter)を軸に、Observer・State・Command・Factoryパターンを
組み合わせて設計しています。各パターンの採用理由は `docs/ARCHITECTURE.md`(追記予定)を参照。

```
Assets/Scripts/
├── Core/            # Unity非依存の純粋ロジック(Model)
│   ├── SandGrid.cs
│   └── FallingPiece.cs
├── Presentation/     # ルール・タイミングの司令塔(Presenter)
│   └── States/       # ゲームステート(Playing / LineClear / Freeze / GameOver)
├── Views/            # 描画専任(View)
├── Input/            # プレイヤー操作をCommandとしてカプセル化
├── Networking/       # Photon Fusionによる対戦同期
└── Audio/            # SE再生(イベント購読型)
```

## セットアップ

1. Unity Hubで本プロジェクトを開く(推奨バージョン: 2022 LTS以降)
2. Package Managerから Photon Fusion 2 をインポート
3. [Photon Engine](https://www.photonengine.com/) でアカウント作成 → Fusionアプリを作成 → App IDを取得
4. `Window > Fusion > Fusion Hub` からApp IDを設定
5. シーンを開いて再生

## 開発状況

- [x] 砂の物理シミュレーション(Core)
- [x] ミノの生成・移動・回転(バッグ方式ランダマイザー)
- [x] ライン消去判定(BFS)
- [ ] MVPへのリファクタリング
- [ ] Photon Fusion 統合
- [ ] 対戦時の妨害(ガベージ)システム
- [ ] マッチメイキングUI

## ライセンス

MIT License. `LICENSE` を参照。
